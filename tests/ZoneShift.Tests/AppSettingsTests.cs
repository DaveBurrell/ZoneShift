using TimezoneConverter;
using Xunit;

namespace ZoneShift.Tests;

public class AppSettingsTests
{
    [Fact]
    public void Save_and_Load_round_trips_target_zones()
    {
        var originalDir = AppSettings.SettingsDirectory;
        // Use real path but unique file via isolation is hard without DI;
        // test serialize shape via Save then Load in-process.
        var settings = new AppSettings
        {
            Use24Hour = true,
            ConvertToLocal = true,
            LiveMode = false,
            CloseToTray = false,
            HasSeenOnboarding = true,
            ReverseSourceWindowsId = "India Standard Time",
            TargetWindowsIds = ["India Standard Time", "Tokyo Standard Time", "GMT Standard Time"],
            OverlayVisible = false,
            OverlayOpacity = 0.9
        };

        settings.Save();
        var loaded = AppSettings.Load();

        Assert.True(loaded.Use24Hour);
        Assert.True(loaded.ConvertToLocal);
        Assert.False(loaded.LiveMode);
        Assert.False(loaded.CloseToTray);
        Assert.True(loaded.HasSeenOnboarding);
        Assert.Equal("India Standard Time", loaded.ReverseSourceWindowsId);
        Assert.NotNull(loaded.TargetWindowsIds);
        Assert.Equal(3, loaded.TargetWindowsIds!.Length);
        Assert.Contains("Tokyo Standard Time", loaded.TargetWindowsIds!);
        Assert.Equal(0.9, loaded.OverlayOpacity, precision: 5);
    }
}
