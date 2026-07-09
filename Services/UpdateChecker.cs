using System.Net.Http.Headers;
using System.Text.Json;

namespace TimezoneConverter.Services;

public sealed record UpdateCheckResult(
    bool UpdateAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string? ReleaseUrl,
    string? Message);

/// <summary>
/// Checks GitHub Releases for a newer ZoneShift version.
/// </summary>
public static class UpdateChecker
{
    private const string LatestApi =
        "https://api.github.com/repos/DaveBurrell/ZoneShift/releases/latest";

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ZoneShift", "1.3"));
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return c;
    }

    public static async Task<UpdateCheckResult> CheckAsync(string currentVersion, CancellationToken ct = default)
    {
        currentVersion = NormalizeVersion(currentVersion);
        try
        {
            using var response = await Http.GetAsync(LatestApi, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(false, currentVersion, null, null,
                    $"Update check failed (HTTP {(int)response.StatusCode}).");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var root = doc.RootElement;

            var tag = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() : null;
            var htmlUrl = root.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() : null;
            var latest = NormalizeVersion(tag ?? "");

            if (string.IsNullOrWhiteSpace(latest))
                return new UpdateCheckResult(false, currentVersion, null, htmlUrl, "Could not read latest version.");

            var newer = CompareVersions(latest, currentVersion) > 0;
            return new UpdateCheckResult(
                newer,
                currentVersion,
                latest,
                htmlUrl ?? "https://github.com/DaveBurrell/ZoneShift/releases",
                newer
                    ? $"Version {latest} is available (you have {currentVersion})."
                    : $"You are on the latest version ({currentVersion}).");
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Update check failed: {ex.Message}");
            return new UpdateCheckResult(false, currentVersion, null, null,
                $"Update check failed: {ex.Message}");
        }
    }

    private static string NormalizeVersion(string v)
    {
        v = v.Trim();
        if (v.StartsWith('v') || v.StartsWith('V'))
            v = v[1..];
        // strip +build metadata / commit hashes
        var plus = v.IndexOf('+');
        if (plus >= 0)
            v = v[..plus];
        return v;
    }

    /// <returns>positive if a &gt; b</returns>
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

