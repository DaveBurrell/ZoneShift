using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace TimezoneConverter.Services;

public sealed record UpdateCheckResult(
    bool UpdateAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string? ReleaseUrl,
    string? SetupDownloadUrl,
    string? SetupFileName,
    string? Message,
    string? SetupSha256 = null);

/// <summary>
/// Checks GitHub Releases and downloads the matching setup for silent install.
/// </summary>
public static class UpdateChecker
{
    private const string LatestApi =
        "https://api.github.com/repos/DaveBurrell/ZoneShift/releases/latest";

    /// <summary>Installers are only ever taken from this repository's release downloads.</summary>
    private const string ReleaseDownloadPrefix = "/DaveBurrell/ZoneShift/releases/download/";

    private static readonly TimeSpan ApiTimeout = TimeSpan.FromSeconds(20);

    // HttpClient.Timeout stops applying once headers arrive, so the body read has its own limit.
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(10);

    // The download can legitimately take minutes; the API call gets its own short timeout.
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        var version = typeof(UpdateChecker).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ZoneShift", version));
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return c;
    }

    public static string CurrentArchitectureLabel =>
        RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()
        };

    public static async Task<UpdateCheckResult> CheckAsync(string currentVersion, CancellationToken ct = default)
    {
        currentVersion = NormalizeVersion(currentVersion);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(ApiTimeout);
        try
        {
            using var response = await Http.GetAsync(LatestApi, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(false, currentVersion, null, null, null, null,
                    $"Update check failed (HTTP {(int)response.StatusCode}).");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token).ConfigureAwait(false);
            var root = doc.RootElement;

            var tag = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() : null;
            var htmlUrl = root.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() : null;
            var latest = NormalizeVersion(tag ?? "");

            if (string.IsNullOrWhiteSpace(latest))
                return new UpdateCheckResult(false, currentVersion, null, htmlUrl, null, null,
                    "Could not read latest version.");

            var (setupUrl, setupName, setupSha256) = PickSetupAssetFromRelease(root, CurrentArchitectureLabel);
            var newer = CompareVersions(latest, currentVersion) > 0;

            return new UpdateCheckResult(
                newer,
                currentVersion,
                latest,
                htmlUrl ?? "https://github.com/DaveBurrell/ZoneShift/releases",
                setupUrl,
                setupName,
                newer
                    ? $"Version {latest} is available (you have {currentVersion})."
                    : $"You are on the latest version ({currentVersion}).",
                setupSha256);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            AppLog.Warn("Update check timed out.");
            return new UpdateCheckResult(false, currentVersion, null, null, null, null,
                "Update check timed out. Try again later.");
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Update check failed: {ex.Message}");
            return new UpdateCheckResult(false, currentVersion, null, null, null, null,
                $"Update check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// True for an https URL under this repository's GitHub release downloads. GitHub then
    /// redirects to its asset CDN, which <see cref="IsTrustedDownloadHost"/> covers.
    /// </summary>
    public static bool IsTrustedReleaseUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
        uri.AbsolutePath.StartsWith(ReleaseDownloadPrefix, StringComparison.Ordinal) &&
        string.IsNullOrEmpty(uri.UserInfo);

    private static bool IsTrustedDownloadHost(Uri? uri) =>
        uri is not null &&
        uri.Scheme == Uri.UriSchemeHttps &&
        (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Downloads the setup EXE, verifies it against the SHA-256 digest GitHub publishes for the
    /// asset, and launches a silent install. The caller exits the app afterwards.
    /// <para>
    /// The installer runs silently and possibly elevated, so it only runs when it came from this
    /// repository over https, matches its digest, and has been held open since it was verified,
    /// in a fresh private folder, so nothing can swap it before it starts.
    /// </para>
    /// </summary>
    public static async Task InstallUpdateAsync(
        string setupDownloadUrl,
        string? preferredFileName,
        string? expectedSha256,
        IProgress<string>? status,
        CancellationToken ct = default)
    {
        if (!IsTrustedReleaseUrl(setupDownloadUrl))
            throw new InvalidOperationException("The update download is not a ZoneShift release on GitHub.");
        if (!IsSha256Hex(expectedSha256))
            throw new InvalidOperationException("The release does not publish a checksum for its installer.");

        var fileName = string.IsNullOrWhiteSpace(preferredFileName)
            ? "ZoneShift-Setup.exe"
            : preferredFileName;

        // sanitize file name
        foreach (var c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c, '_');

        var folder = Directory.CreateTempSubdirectory("ZoneShift-update-");
        var tempPath = Path.Combine(folder.FullName, fileName);
        status?.Report("Downloading update...");
        AppLog.Info($"Downloading update from {setupDownloadUrl} -> {tempPath}");

        FileStream? locked = null;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(DownloadTimeout);
            using (var response = await Http.GetAsync(
                       setupDownloadUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                if (!IsTrustedDownloadHost(response.RequestMessage?.RequestUri))
                    throw new InvalidOperationException("The update download was redirected to an unexpected host.");

                await using var remote = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                await using var local = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await remote.CopyToAsync(local, timeout.Token).ConfigureAwait(false);
            }

            // Hold the verified file open, denying writers, until the installer process has started.
            locked = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(locked, ct).ConfigureAwait(false));
            if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                AppLog.Error($"Update checksum mismatch: expected {expectedSha256}, got {actual}.");
                throw new InvalidOperationException("The downloaded update failed its checksum and was not installed.");
            }
        }
        catch
        {
            // Never leave a partial or unverified installer behind.
            locked?.Dispose();
            try { folder.Delete(recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            throw;
        }

        await using var _ = locked;

        status?.Report("Installing update (ZoneShift will reopen)...");
        AppLog.Info($"Launching silent installer: {tempPath}");

        // VERYSILENT: no UI. CLOSEAPPLICATIONS: unlock ZoneShift.exe if still running.
        // Installer [Run] (skipifnotsilent) relaunches the app after install.
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = tempPath,
            Arguments =
                "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART " +
                "/CLOSEAPPLICATIONS /FORCECLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
            UseShellExecute = true
        };

        var proc = System.Diagnostics.Process.Start(psi);
        if (proc is null)
            throw new InvalidOperationException("Could not start the installer process.");

        AppLog.Info($"Installer process started (pid={proc.Id}). App will exit so install can finish.");
    }

    /// <summary>
    /// Picks the installer for <paramref name="arch"/> from a GitHub release JSON object, with the
    /// SHA-256 from the asset's <c>digest</c> ("sha256:" + hex). Assets outside this
    /// repository's release downloads are ignored.
    /// </summary>
    internal static (string? Url, string? Name, string? Sha256) PickSetupAssetFromRelease(JsonElement root, string arch)
    {
        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return (null, null, null);

        var list = new List<(string Name, string Url)>();
        var digests = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
            var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
            if (string.IsNullOrWhiteSpace(name) || url is null || !IsTrustedReleaseUrl(url))
                continue;

            list.Add((name, url));
            var digest = asset.TryGetProperty("digest", out var d) && d.ValueKind == JsonValueKind.String
                ? d.GetString()
                : null;
            digests[url] = digest is not null && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
                ? digest["sha256:".Length..]
                : null;
        }

        var (pickedUrl, pickedName) = PickSetupAsset(list, arch);
        var sha = pickedUrl is not null && digests.TryGetValue(pickedUrl, out var hex) && IsSha256Hex(hex) ? hex : null;
        return (pickedUrl, pickedName, sha);
    }

    private static bool IsSha256Hex(string? value) =>
        value is { Length: 64 } && value.All(Uri.IsHexDigit);

    /// <summary>Public for unit tests — pick setup asset name/url for an architecture.</summary>
    public static (string? Url, string? Name) PickSetupAsset(
        IReadOnlyList<(string Name, string Url)> assets,
        string arch)
    {
        string? fallbackUrl = null;
        string? fallbackName = null;

        foreach (var (name, url) in assets)
        {
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!name.Contains("Setup", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("ZoneShift", StringComparison.OrdinalIgnoreCase))
                continue;

            fallbackUrl ??= url;
            fallbackName ??= name;

            var isX64 = arch.Equals("x64", StringComparison.OrdinalIgnoreCase);
            var isArm = arch.Equals("arm64", StringComparison.OrdinalIgnoreCase);

            if (isArm && (name.Contains("arm64", StringComparison.OrdinalIgnoreCase) ||
                          name.Contains("win-arm64", StringComparison.OrdinalIgnoreCase)))
                return (url, name);

            if (isX64 && (name.Contains("x64", StringComparison.OrdinalIgnoreCase) ||
                          name.Contains("win-x64", StringComparison.OrdinalIgnoreCase)) &&
                !name.Contains("arm", StringComparison.OrdinalIgnoreCase))
                return (url, name);

            if (name.Contains(arch, StringComparison.OrdinalIgnoreCase))
                return (url, name);
        }

        return (fallbackUrl, fallbackName);
    }

    private static string NormalizeVersion(string v)
    {
        v = v.Trim();
        if (v.StartsWith('v') || v.StartsWith('V'))
            v = v[1..];
        var plus = v.IndexOf('+');
        if (plus >= 0)
            v = v[..plus];
        return v;
    }

    public static int CompareVersions(string a, string b)
    {
        if (!Version.TryParse(Pad(NormalizeVersion(a)), out var va))
            va = new Version(0, 0, 0, 0);
        if (!Version.TryParse(Pad(NormalizeVersion(b)), out var vb))
            vb = new Version(0, 0, 0, 0);
        return va.CompareTo(vb);
    }

    private static string Pad(string v)
    {
        var parts = v.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var nums = new int[4];
        for (var i = 0; i < Math.Min(4, parts.Length); i++)
            nums[i] = int.TryParse(parts[i], out var n) ? n : 0;
        return $"{nums[0]}.{nums[1]}.{nums[2]}.{nums[3]}";
    }
}
