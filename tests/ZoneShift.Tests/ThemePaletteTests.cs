using TimezoneConverter;
using Xunit;

namespace ZoneShift.Tests;

public class ThemePaletteTests
{
    [Fact]
    public void All_themes_have_unique_ids_and_names()
    {
        Assert.Equal(3, ThemePalette.All.Count);
        Assert.Equal(3, ThemePalette.All.Select(t => t.Id).Distinct().Count());
        Assert.Equal(3, ThemePalette.All.Select(t => t.DisplayName).Distinct().Count());
    }

    [Theory]
    [InlineData("Studio", AppThemeId.Studio)]
    [InlineData("Classic", AppThemeId.Classic)]
    [InlineData("NeonPulse", AppThemeId.NeonPulse)]
    [InlineData("neon", AppThemeId.NeonPulse)]
    [InlineData("original", AppThemeId.Classic)]
    public void FromName_resolves_aliases(string name, AppThemeId expected)
    {
        Assert.Equal(expected, ThemePalette.FromName(name).Id);
    }

    [Fact]
    public void Classic_and_Neon_differ_visually()
    {
        Assert.NotEqual(ThemePalette.Studio.AppBackground, ThemePalette.Classic.AppBackground);
        Assert.NotEqual(ThemePalette.Studio.ClockFore, ThemePalette.NeonPulse.ClockFore);
        Assert.False(ThemePalette.Classic.IsDark);
        Assert.True(ThemePalette.NeonPulse.IsDark);
        Assert.True(ThemePalette.Studio.IsDark);
    }
}
