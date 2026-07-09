using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace TimezoneConverter.Services;

public sealed record UpdateCheckResult(
    bool UpdateAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string? ReleaseUrl,
    string? SetupDownloadUrl,
    string? SetupFileName,
    string? Message);

/// <summary>
/// Checks GitHub Releases and downloads the matching setup for silent install.
/// </summary>
public static class UpdateChecker
{
    private const string LatestApi =
        "https://api.github.com/repos/DaveBurrell/ZoneShift/releases/latest";

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ZoneShift", "1.4"));
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
        try
        {
            using var response = await Http.GetAsync(LatestApi, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(false, currentVersion, null, null, null, null,
                    $"Update check failed (HTTP {(int)response.StatusCode}).");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var root = doc.RootElement;

            var tag = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() : null;
            var htmlUrl = root.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() : null;
            var latest = NormalizeVersion(tag ?? "");

            if (string.IsNullOrWhiteSpace(latest))
                return new UpdateCheckResult(false, currentVersion, null, htmlUrl, null, null,
                    "Could not read latest version.");

            var (setupUrl, setupName) = PickSetupAsset(root, CurrentArchitectureLabel);
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
                    : $"You are on the latest version ({currentVersion}).");
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Update check failed: {ex.Message}");
            return new UpdateCheckResult(false, currentVersion, null, null, null, null,
                $"Update check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Downloads the setup EXE and launches a silent install, then exits the app.
    /// </summary>
    public static async Task InstallUpdateAsync(
        string setupDownloadUrl,
        string? preferredFileName,
        IProgress<string>? status,
        CancellationToken ct = default)
    {
        var fileName = string.IsNullOrWhiteSpace(preferredFileName)
            ? "ZoneShift-Setup.exe"
            : preferredFileName;

        // sanitize file name
        foreach (var c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c, '_');

        var tempPath = Path.Combine(Path.GetTempPath(), fileName);
        status?.Report("Downloading update...");
        AppLog.Info($"Downloading update from {setupDownloadUrl} -> {tempPath}");

        await using (var remote = await Http.GetStreamAsync(setupDownloadUrl, ct).ConfigureAwait(false))
        await using (var local = File.Create(tempPath))
        {
            await remote.CopyToAsync(local, ct).ConfigureAwait(false);
        }

        status?.Report("Starting installer...");
        AppLog.Info($"Launching silent installer: {tempPath}");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = tempPath,
            // Close ZoneShift, install silently, do not restart PC
            Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /FORCECLOSEAPPLICATIONS",
            UseShellExecute = true
        };

        System.Diagnostics.Process.Start(psi);
    }

    private static (string? Url, string? Name) PickSetupAsset(JsonElement root, string arch)
    {
        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return (null, null);

        string? fallbackUrl = null;
        string? fallbackName = null;

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
            var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
                continue;
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!name.Contains("Setup", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("ZoneShift", StringComparison.OrdinalIgnoreCase))
                continue;

            fallbackUrl ??= url;
            fallbackName ??= name;

            if (name.Contains(arch, StringComparison.OrdinalIgnoreCase) ||
                (arch == "x64" && (name.Contains("x64", StringComparison.OrdinalIgnoreCase) ||
                                   name.Contains("win-x64", StringComparison.OrdinalIgnoreCase))) ||
                (arch == "arm64" && (name.Contains("arm64", StringComparison.OrdinalIgnoreCase) ||
                                     name.Contains("win-arm64", StringComparison.OrdinalIgnoreCase))))
            {
                return (url, name);
            }
        }

        // Legacy single installer without arch suffix
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
