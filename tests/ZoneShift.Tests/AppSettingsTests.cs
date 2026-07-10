using TimezoneConverter;
using Xunit;

namespace ZoneShift.Tests;

/// <summary>
/// Redirects the settings directory to a scratch folder for the lifetime of the class.
/// <para>
/// Before 1.7.0 these tests called <c>Save()</c> against the real
/// <c>%AppData%\ZoneShift\settings.json</c>, so running the suite destroyed the developer's own
/// preferences and rotated the backup on top of them.
/// </para>
/// </summary>
public sealed class AppSettingsTests : IDisposable
{
    private readonly string _scratchDir;
    private readonly string? _previousOverride;

    public AppSettingsTests()
    {
        _previousOverride = Environment.GetEnvironmentVariable(AppSettings.DirectoryOverrideVariable);
        _scratchDir = Path.Combine(Path.GetTempPath(), "ZoneShift.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_scratchDir);
        Environment.SetEnvironmentVariable(AppSettings.DirectoryOverrideVariable, _scratchDir);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(AppSettings.DirectoryOverrideVariable, _previousOverride);
        try
        {
            Directory.Delete(_scratchDir, recursive: true);
        }
        catch (IOException)
        {
            // A locked scratch file must never fail the suite.
        }
    }

    [Fact]
    public void Settings_directory_honours_the_override()
    {
        Assert.Equal(_scratchDir, AppSettings.SettingsDirectory);
        Assert.StartsWith(_scratchDir, AppSettings.SettingsPath, StringComparison.Ordinal);
    }

    [Fact]
    public void Save_writes_inside_the_override_and_never_the_real_profile()
    {
        var realProfile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZoneShift");

        new AppSettings { Use24Hour = true }.Save();

        Assert.True(File.Exists(Path.Combine(_scratchDir, "settings.json")));
        Assert.NotEqual(realProfile, AppSettings.SettingsDirectory);
    }

    [Fact]
    public void Save_and_Load_round_trips_target_zones()
    {
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

    [Fact]
    public void Theme_round_trips_and_retired_names_migrate()
    {
        new AppSettings { Theme = nameof(AppThemeId.Meridian) }.Save();
        Assert.Equal(AppThemeId.Meridian, ThemePalette.FromName(AppSettings.Load().Theme).Id);

        new AppSettings { Theme = "NeonPulse" }.Save();
        Assert.Equal(AppThemeId.NightOps, ThemePalette.FromName(AppSettings.Load().Theme).Id);
    }

    [Fact]
    public void Out_of_range_overlay_opacity_is_clamped_on_load()
    {
        new AppSettings { OverlayOpacity = 12.0 }.Save();
        Assert.Equal(0.94, AppSettings.Load().OverlayOpacity, precision: 5);
    }
}
