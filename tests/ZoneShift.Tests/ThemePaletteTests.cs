using TimezoneConverter;
using Xunit;

namespace ZoneShift.Tests;

public class ThemePaletteTests
{
    [Fact]
    public void All_themes_have_unique_ids_and_names()
    {
        Assert.Equal(4, ThemePalette.All.Count);
        Assert.Equal(4, ThemePalette.All.Select(t => t.Id).Distinct().Count());
        Assert.Equal(4, ThemePalette.All.Select(t => t.DisplayName).Distinct().Count());
    }

    [Fact]
    public void Theme_set_is_balanced_between_light_and_dark()
    {
        Assert.Equal(2, ThemePalette.All.Count(t => t.IsDark));
        Assert.Equal(2, ThemePalette.All.Count(t => !t.IsDark));
    }

    [Theory]
    [InlineData("Studio", AppThemeId.Studio)]
    [InlineData("Classic", AppThemeId.Classic)]
    [InlineData("original", AppThemeId.Classic)]
    [InlineData("light", AppThemeId.Classic)]
    [InlineData("NightOps", AppThemeId.NightOps)]
    [InlineData("night ops", AppThemeId.NightOps)]
    [InlineData("terminal", AppThemeId.NightOps)]
    [InlineData("Meridian", AppThemeId.Meridian)]
    [InlineData("meridian", AppThemeId.Meridian)]
    public void FromName_resolves_aliases(string name, AppThemeId expected)
    {
        Assert.Equal(expected, ThemePalette.FromName(name).Id);
    }

    /// <summary>
    /// Neon Pulse shipped in 1.6.x. Users who chose it must land on the closest surviving
    /// theme rather than being silently reset to the default.
    /// </summary>
    [Theory]
    [InlineData("NeonPulse")]
    [InlineData("neon")]
    [InlineData("neon pulse")]
    [InlineData("cyber")]
    public void Retired_neon_pulse_migrates_to_night_ops(string persistedName)
    {
        Assert.Equal(AppThemeId.NightOps, ThemePalette.FromName(persistedName).Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NotATheme")]
    [InlineData("ClassicRefined")] // never shipped
    [InlineData("ModernMinimal")]  // never shipped
    [InlineData("Timeline")]       // never shipped; renamed to Meridian pre-release
    public void FromName_falls_back_to_studio(string? name)
    {
        Assert.Equal(AppThemeId.Studio, ThemePalette.FromName(name).Id);
    }

    /// <summary>
    /// Enum values were reused when themes were retired, so a bare ordinal no longer means
    /// what it once did. Nothing persists ordinals, and FromName must not resurrect them.
    /// </summary>
    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("99")]
    public void FromName_ignores_bare_ordinals(string ordinal)
    {
        Assert.Equal(AppThemeId.Studio, ThemePalette.FromName(ordinal).Id);
    }

    [Fact]
    public void FromId_covers_all_enum_values()
    {
        foreach (var id in Enum.GetValues<AppThemeId>())
        {
            var p = ThemePalette.FromId(id);
            Assert.Equal(id, p.Id);
            Assert.False(string.IsNullOrWhiteSpace(p.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(p.Tagline));
            Assert.False(string.IsNullOrWhiteSpace(p.Description));
        }
    }

    [Fact]
    public void Every_theme_in_All_round_trips_through_its_display_name()
    {
        foreach (var p in ThemePalette.All)
            Assert.Equal(p.Id, ThemePalette.FromName(p.DisplayName).Id);
    }

    /// <summary>
    /// The name persisted by <c>SelectTheme</c> is <c>AppThemeId.ToString()</c>, so that exact
    /// string must survive a settings round-trip.
    /// </summary>
    [Fact]
    public void Persisted_enum_name_round_trips()
    {
        foreach (var id in Enum.GetValues<AppThemeId>())
            Assert.Equal(id, ThemePalette.FromName(id.ToString()).Id);
    }

    [Fact]
    public void Themes_are_visually_distinct()
    {
        Assert.Equal(4, ThemePalette.All.Select(t => t.Accent).Distinct().Count());
        Assert.Equal(4, ThemePalette.All.Select(t => t.ClockFore).Distinct().Count());

        Assert.True(ThemePalette.Studio.IsDark);
        Assert.True(ThemePalette.NightOps.IsDark);
        Assert.False(ThemePalette.Classic.IsDark);
        Assert.False(ThemePalette.Meridian.IsDark);
    }
}
