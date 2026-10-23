using System.Text.Json;
using TimezoneConverter.Services;
using Xunit;

namespace ZoneShift.Tests;

public class UpdateCheckerTests
{
    [Theory]
    [InlineData("1.3.0", "1.2.0", 1)]
    [InlineData("1.2.0", "1.3.0", -1)]
    [InlineData("1.3.0", "1.3.0", 0)]
    [InlineData("v1.3.0", "1.3.0", 0)]
    public void CompareVersions_orders_semver(string a, string b, int expectedSign)
    {
        var cmp = UpdateChecker.CompareVersions(a, b);
        Assert.Equal(Math.Sign(expectedSign), Math.Sign(cmp));
    }

    [Fact]
    public void PickSetupAsset_prefers_matching_arch()
    {
        var assets = new List<(string Name, string Url)>
        {
            ("ZoneShift-Setup-1.4.1-arm64.exe", "https://example/arm"),
            ("ZoneShift-Setup-1.4.1-x64.exe", "https://example/x64"),
            ("notes.txt", "https://example/notes")
        };

        var (xUrl, xName) = UpdateChecker.PickSetupAsset(assets, "x64");
        Assert.Equal("https://example/x64", xUrl);
        Assert.Equal("ZoneShift-Setup-1.4.1-x64.exe", xName);

        var (aUrl, aName) = UpdateChecker.PickSetupAsset(assets, "arm64");
        Assert.Equal("https://example/arm", aUrl);
        Assert.Equal("ZoneShift-Setup-1.4.1-arm64.exe", aName);
    }

    [Fact]
    public void PickSetupAsset_falls_back_to_any_setup_exe()
    {
        var assets = new List<(string Name, string Url)>
        {
            ("ZoneShift-Setup-1.0.0.exe", "https://example/legacy")
        };

        var (url, name) = UpdateChecker.PickSetupAsset(assets, "x64");
        Assert.Equal("https://example/legacy", url);
        Assert.Equal("ZoneShift-Setup-1.0.0.exe", name);
    }

    [Theory]
    [InlineData("https://github.com/DaveBurrell/ZoneShift/releases/download/v1.8.0/ZoneShift-Setup-1.8.0-x64.exe", true)]
    [InlineData("http://github.com/DaveBurrell/ZoneShift/releases/download/v1.8.0/ZoneShift-Setup-1.8.0-x64.exe", false)]
    [InlineData("https://github.com/someone-else/ZoneShift/releases/download/v1.8.0/ZoneShift-Setup-1.8.0-x64.exe", false)]
    [InlineData("https://github.com.evil.example/DaveBurrell/ZoneShift/releases/download/v1/x.exe", false)]
    [InlineData("https://user@github.com/DaveBurrell/ZoneShift/releases/download/v1/x.exe", false)]
    [InlineData("https://example.com/ZoneShift-Setup.exe", false)]
    [InlineData("not a url", false)]
    [InlineData(null, false)]
    public void Only_this_repository_release_downloads_are_trusted(string? url, bool trusted) =>
        Assert.Equal(trusted, UpdateChecker.IsTrustedReleaseUrl(url));

    [Fact]
    public void Release_assets_carry_their_published_digest_and_foreign_assets_are_ignored()
    {
        const string sha = "144ca1d4909d6de2a15ea6234dc0570519038020db95ff7cb5d830b97caa077e";
        using var doc = JsonDocument.Parse($$"""
            {"assets": [
              {"name": "ZoneShift-Setup-1.8.0-x64.exe",
               "browser_download_url": "https://example.com/ZoneShift-Setup-1.8.0-x64.exe",
               "digest": "sha256:{{sha}}"},
              {"name": "ZoneShift-Setup-1.8.0-x64.exe",
               "browser_download_url": "https://github.com/DaveBurrell/ZoneShift/releases/download/v1.8.0/ZoneShift-Setup-1.8.0-x64.exe",
               "digest": "sha256:{{sha}}"},
              {"name": "ZoneShift-Setup-1.8.0-arm64.exe",
               "browser_download_url": "https://github.com/DaveBurrell/ZoneShift/releases/download/v1.8.0/ZoneShift-Setup-1.8.0-arm64.exe"}
            ]}
            """);

        var x64 = UpdateChecker.PickSetupAssetFromRelease(doc.RootElement, "x64");
        Assert.StartsWith("https://github.com/DaveBurrell/", x64.Url);
        Assert.Equal(sha, x64.Sha256);

        // No digest published: the asset is still offered, but not for a silent install.
        var arm = UpdateChecker.PickSetupAssetFromRelease(doc.RootElement, "arm64");
        Assert.Equal("ZoneShift-Setup-1.8.0-arm64.exe", arm.Name);
        Assert.Null(arm.Sha256);
    }

    [Fact]
    public async Task Installing_refuses_untrusted_or_unverifiable_downloads()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            UpdateChecker.InstallUpdateAsync("https://example.com/setup.exe", "setup.exe", new string('a', 64), null, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            UpdateChecker.InstallUpdateAsync(
                "https://github.com/DaveBurrell/ZoneShift/releases/download/v1.8.0/ZoneShift-Setup-1.8.0-x64.exe",
                "setup.exe", expectedSha256: null, status: null, TestContext.Current.CancellationToken));
    }
}
