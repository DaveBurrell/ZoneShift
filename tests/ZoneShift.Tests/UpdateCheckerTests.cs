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
}
