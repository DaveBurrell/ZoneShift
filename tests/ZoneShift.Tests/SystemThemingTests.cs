using System.Windows.Forms;
using TimezoneConverter;
using Xunit;

namespace ZoneShift.Tests;

/// <summary>
/// The native date picker, checkbox glyphs, combo borders, scroll bars, and title bar follow the
/// process color mode rather than the palette. This pins the mapping between the two.
/// </summary>
public class SystemThemingTests
{
    [Fact]
    public void Dark_themes_map_to_dark_mode()
    {
        Assert.Equal(SystemColorMode.Dark, SystemTheming.ModeFor(isDark: true));
    }

    /// <summary>
    /// Light themes must pin to Classic rather than System: a light ZoneShift theme should stay
    /// light even when Windows itself is set to dark.
    /// </summary>
    [Fact]
    public void Light_themes_pin_to_classic_not_system()
    {
        Assert.Equal(SystemColorMode.Classic, SystemTheming.ModeFor(isDark: false));
        Assert.NotEqual(SystemColorMode.System, SystemTheming.ModeFor(isDark: false));
    }

    [Fact]
    public void Every_palette_maps_to_a_mode_matching_its_lightness()
    {
        foreach (var palette in ThemePalette.All)
        {
            var expected = palette.IsDark ? SystemColorMode.Dark : SystemColorMode.Classic;
            Assert.Equal(expected, SystemTheming.ModeFor(palette.IsDark));
        }
    }

    /// <summary>
    /// The color mode is only re-applied when a theme switch crosses the light/dark boundary,
    /// so both bands must be populated for that path to ever be exercised.
    /// </summary>
    [Fact]
    public void Both_lightness_bands_are_populated_so_a_cross_is_reachable()
    {
        Assert.Contains(ThemePalette.All, t => t.IsDark);
        Assert.Contains(ThemePalette.All, t => !t.IsDark);
    }
}
