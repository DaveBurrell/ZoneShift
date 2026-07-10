using System.Drawing;
using TimezoneConverter;
using Xunit;

namespace ZoneShift.Tests;

/// <summary>
/// Guards every text-on-surface pairing in every palette against WCAG 2.1 contrast minima.
/// <para>
/// This exists because ZoneShift 1.6.x drew the brand title, section headings, and the
/// overlay's clock digits in <c>ClockFore</c> — a color tuned for glowing digits on near-black
/// glass. On the light Classic theme that rendered emerald text on white at 1.92:1.
/// </para>
/// </summary>
public class ThemeContrastTests
{
    // WCAG 2.1 SC 1.4.3
    private const double NormalText = 4.5;
    private const double LargeText = 3.0;

    public static TheoryData<string> AllThemes()
    {
        var data = new TheoryData<string>();
        foreach (var p in ThemePalette.All)
            data.Add(p.DisplayName);
        return data;
    }

    private static ThemePalette Palette(string displayName) =>
        ThemePalette.All.Single(p => p.DisplayName == displayName);

    /// <summary>WCAG relative luminance of an opaque sRGB color.</summary>
    private static double RelativeLuminance(Color c)
    {
        static double Channel(byte v)
        {
            var s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    /// <summary>WCAG contrast ratio, in the range [1, 21].</summary>
    internal static double ContrastRatio(Color a, Color b)
    {
        var la = RelativeLuminance(a);
        var lb = RelativeLuminance(b);
        var (lighter, darker) = la >= lb ? (la, lb) : (lb, la);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static void AssertContrast(
        string theme, string pair, Color fore, Color back, double minimum)
    {
        var ratio = ContrastRatio(fore, back);
        Assert.True(
            ratio >= minimum,
            $"{theme}: {pair} is {ratio:F2}:1, below the {minimum:F1}:1 minimum. " +
            $"fore=#{fore.R:X2}{fore.G:X2}{fore.B:X2} back=#{back.R:X2}{back.G:X2}{back.B:X2}");
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Body_text_clears_AA_on_every_surface(string themeName)
    {
        var p = Palette(themeName);

        foreach (var (surfaceName, surface) in new[]
                 {
                     ("CardFace", p.CardFace),
                     ("AppBackground", p.AppBackground),
                     ("TileBack", p.TileBack)
                 })
        {
            AssertContrast(themeName, $"TextPrimary on {surfaceName}", p.TextPrimary, surface, NormalText);
            AssertContrast(themeName, $"TextSecondary on {surfaceName}", p.TextSecondary, surface, NormalText);
            // Muted is reserved for captions and hints, which are decorative supporting text.
            AssertContrast(themeName, $"TextMuted on {surfaceName}", p.TextMuted, surface, LargeText);
        }
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Headings_and_brand_clear_AA(string themeName)
    {
        var p = Palette(themeName);

        AssertContrast(themeName, "SectionHeading on CardFace", p.SectionHeading, p.CardFace, NormalText);
        // The wordmark is 15pt semibold, which qualifies as large text.
        AssertContrast(themeName, "BrandText on HeaderBack", p.BrandText, p.HeaderBack, LargeText);
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Text_on_filled_controls_clears_AA(string themeName)
    {
        var p = Palette(themeName);

        AssertContrast(themeName, "TextOnAccent on Accent", p.TextOnAccent, p.Accent, NormalText);
        AssertContrast(themeName, "SegmentActiveText on SegmentActive", p.SegmentActiveText, p.SegmentActive, NormalText);
        AssertContrast(themeName, "SegmentIdleText on SegmentIdle", p.SegmentIdleText, p.SegmentIdle, NormalText);
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Led_module_digits_clear_AA_on_glass(string themeName)
    {
        var p = Palette(themeName);

        AssertContrast(themeName, "ClockFore on GlassBack", p.ClockFore, p.GlassBack, NormalText);
        AssertContrast(themeName, "ClockCore on GlassBack", p.ClockCore, p.GlassBack, NormalText);
        AssertContrast(themeName, "LedCaption on GlassBack", p.LedCaption, p.GlassBack, NormalText);
    }

    /// <summary>
    /// The overlay draws clock digits as flat text on a card, not as glowing pixels on glass.
    /// This is the pairing that regressed in 1.6.x.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Overlay_clock_digits_clear_AA_on_card(string themeName)
    {
        var p = Palette(themeName);
        AssertContrast(themeName, "ClockTextOnSurface on CardFace", p.ClockTextOnSurface, p.CardFace, NormalText);
    }

    [Fact]
    public void ContrastRatio_matches_known_reference_values()
    {
        Assert.Equal(21.0, ContrastRatio(Color.Black, Color.White), precision: 2);
        Assert.Equal(1.0, ContrastRatio(Color.White, Color.White), precision: 2);
        // The historical defect: emerald LED digits on a white card, far under the 4.5:1 floor.
        Assert.Equal(1.92, ContrastRatio(Color.FromArgb(52, 211, 153), Color.White), precision: 2);
    }

    /// <summary>
    /// The LED hue is deliberately unusable as flat text on light cards. If a light theme ever
    /// sets ClockTextOnSurface = ClockFore, the pairing test above should be what catches it —
    /// this documents why the two tokens must stay distinct.
    /// </summary>
    [Fact]
    public void Light_themes_darken_the_led_hue_for_flat_text()
    {
        foreach (var p in ThemePalette.All.Where(t => !t.IsDark))
        {
            Assert.NotEqual(p.ClockFore, p.ClockTextOnSurface);
            Assert.True(
                ContrastRatio(p.ClockFore, p.CardFace) < NormalText,
                $"{p.DisplayName}: ClockFore unexpectedly passes AA on CardFace, so the " +
                "separate ClockTextOnSurface token may no longer be justified.");
        }
    }
}
