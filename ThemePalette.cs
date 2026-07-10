namespace TimezoneConverter;

/// <summary>Built-in visual themes. Persisted by name, never by value.</summary>
public enum AppThemeId
{
    Studio = 0,   // dark slate, amber LEDs
    Classic = 1,  // light cool-gray, indigo accent, emerald LEDs
    NightOps = 2, // dark terminal, phosphor green
    Meridian = 3  // light warm-white, teal accent, coral LEDs
}

/// <summary>
/// Full color set for one visual theme.
/// <para>
/// Token roles are not interchangeable. <see cref="ClockFore"/> and <see cref="ClockCore"/>
/// are tuned for glowing digits on <see cref="GlassBack"/> and will not meet contrast
/// requirements as text on a card — Classic's emerald reaches only 1.92:1 on white. Use
/// <see cref="ClockTextOnSurface"/> for digits drawn as ordinary text,
/// <see cref="SectionHeading"/> for headings, and <see cref="BrandText"/> for the wordmark.
/// <c>ThemeContrastTests</c> enforces this.
/// </para>
/// </summary>
public sealed class ThemePalette
{
    public required AppThemeId Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Tagline { get; init; }

    /// <summary>One-line summary shown as the View &gt; Theme menu tooltip.</summary>
    public required string Description { get; init; }

    public required bool IsDark { get; init; }

    // Surfaces
    public required Color AppBackground { get; init; }
    public required Color WallBack { get; init; }
    public required Color HeaderBack { get; init; }
    public required Color FooterBack { get; init; }
    public required Color CardBackground { get; init; }
    public required Color CardFace { get; init; }
    public required Color CardBorder { get; init; }
    public required Color CardTop { get; init; }
    public required Color CardBottom { get; init; }
    public required Color TileBack { get; init; }
    public required Color TileTop { get; init; }
    public required Color TileBottom { get; init; }
    public required Color HeaderTop { get; init; }
    public required Color HeaderBottom { get; init; }
    public required Color InputBack { get; init; }
    public required Color InputBorder { get; init; }

    // Text
    public required Color TextPrimary { get; init; }
    public required Color TextSecondary { get; init; }
    public required Color TextMuted { get; init; }
    public required Color TextOnAccent { get; init; }

    /// <summary>Wordmark in the brand strip, drawn on <see cref="HeaderBack"/>.</summary>
    public required Color BrandText { get; init; }

    /// <summary>Section headings drawn on <see cref="CardFace"/>.</summary>
    public required Color SectionHeading { get; init; }

    // Accent
    public required Color Accent { get; init; }
    public required Color AccentHover { get; init; }
    public required Color AccentPressed { get; init; }
    public required Color AccentSoft { get; init; }
    public required Color AccentSoftBorder { get; init; }
    public required Color AccentSoftHover { get; init; }
    public required Color AccentSoftPressed { get; init; }

    // LED — for glowing digits on GlassBack only
    public required Color ClockBack { get; init; }
    public required Color ClockFore { get; init; }
    public required Color ClockCore { get; init; }
    public required Color ClockDim { get; init; }
    public required Color ClockBezel { get; init; }
    public required Color BezelLight { get; init; }
    public required Color BezelDark { get; init; }
    public required Color GlassBack { get; init; }
    public required Color LedCaption { get; init; }

    /// <summary>Clock digits drawn as flat text on <see cref="CardFace"/> (overlay window).</summary>
    public required Color ClockTextOnSurface { get; init; }

    // Semantic
    public required Color Success { get; init; }
    public required Color Danger { get; init; }
    public required Color DangerSoft { get; init; }
    public required Color DangerHover { get; init; }
    public required Color DangerPressed { get; init; }
    public required Color Warning { get; init; }

    // Segments
    public required Color SegmentIdle { get; init; }
    public required Color SegmentIdleText { get; init; }
    public required Color SegmentActive { get; init; }
    public required Color SegmentActiveText { get; init; }
    public required Color SegmentIdleHover { get; init; }

    // Menu
    public required Color MenuHover { get; init; }

    // Live pill
    public required Color LivePillFill { get; init; }
    public required Color LivePillBorder { get; init; }
    public required Color CustomPillFill { get; init; }
    public required Color CustomPillBorder { get; init; }

    /// <summary>Dark newsroom wall, amber LEDs. The default.</summary>
    public static ThemePalette Studio { get; } = new()
    {
        Id = AppThemeId.Studio,
        DisplayName = "Studio",
        Tagline = "STUDIO  ·  WORLD CLOCK WALL",
        Description = "Dark newsroom wall with amber LEDs",
        IsDark = true,
        AppBackground = Color.FromArgb(22, 25, 34),
        WallBack = Color.FromArgb(18, 21, 30),
        HeaderBack = Color.FromArgb(18, 21, 30),
        FooterBack = Color.FromArgb(18, 21, 30),
        CardBackground = Color.FromArgb(40, 46, 60),
        CardFace = Color.FromArgb(40, 46, 60),
        CardBorder = Color.FromArgb(78, 88, 110),
        CardTop = Color.FromArgb(48, 55, 72),
        CardBottom = Color.FromArgb(36, 42, 56),
        TileBack = Color.FromArgb(48, 55, 72),
        TileTop = Color.FromArgb(56, 64, 84),
        TileBottom = Color.FromArgb(42, 48, 64),
        HeaderTop = Color.FromArgb(32, 36, 48),
        HeaderBottom = Color.FromArgb(18, 21, 30),
        InputBack = Color.FromArgb(28, 32, 44),
        InputBorder = Color.FromArgb(90, 100, 124),
        TextPrimary = Color.FromArgb(248, 250, 252),
        TextSecondary = Color.FromArgb(196, 205, 220),
        TextMuted = Color.FromArgb(160, 172, 192),
        TextOnAccent = Color.FromArgb(28, 20, 6),
        BrandText = Color.FromArgb(251, 191, 36),
        SectionHeading = Color.FromArgb(251, 191, 36),
        Accent = Color.FromArgb(251, 191, 36),
        AccentHover = Color.FromArgb(252, 211, 77),
        AccentPressed = Color.FromArgb(217, 140, 10),
        AccentSoft = Color.FromArgb(72, 55, 22),
        AccentSoftBorder = Color.FromArgb(160, 120, 40),
        AccentSoftHover = Color.FromArgb(90, 70, 28),
        AccentSoftPressed = Color.FromArgb(60, 45, 18),
        ClockBack = Color.FromArgb(12, 14, 20),
        ClockFore = Color.FromArgb(255, 200, 70),
        ClockCore = Color.FromArgb(255, 220, 140),
        ClockDim = Color.FromArgb(55, 48, 28),
        ClockBezel = Color.FromArgb(70, 78, 96),
        BezelLight = Color.FromArgb(62, 70, 84),
        BezelDark = Color.FromArgb(36, 40, 52),
        GlassBack = Color.FromArgb(8, 10, 14),
        LedCaption = Color.FromArgb(210, 220, 235),
        ClockTextOnSurface = Color.FromArgb(255, 200, 70),
        Success = Color.FromArgb(74, 222, 168),
        Danger = Color.FromArgb(252, 129, 129),
        DangerSoft = Color.FromArgb(80, 32, 36),
        DangerHover = Color.FromArgb(100, 40, 44),
        DangerPressed = Color.FromArgb(70, 28, 32),
        Warning = Color.FromArgb(253, 224, 71),
        SegmentIdle = Color.FromArgb(52, 60, 78),
        SegmentIdleText = Color.FromArgb(230, 236, 245),
        SegmentActive = Color.FromArgb(245, 158, 11),
        SegmentActiveText = Color.FromArgb(28, 20, 6),
        SegmentIdleHover = Color.FromArgb(68, 78, 100),
        MenuHover = Color.FromArgb(60, 50, 28),
        LivePillFill = Color.FromArgb(20, 70, 52),
        LivePillBorder = Color.FromArgb(60, 200, 140),
        CustomPillFill = Color.FromArgb(48, 54, 68),
        CustomPillBorder = Color.FromArgb(100, 112, 132)
    };

    /// <summary>
    /// Light cool-gray canvas, indigo accents, emerald LEDs.
    /// The brand strip is a light surface rather than an indigo slab, so the wordmark reads
    /// as text instead of competing with the accent.
    /// </summary>
    public static ThemePalette Classic { get; } = new()
    {
        Id = AppThemeId.Classic,
        DisplayName = "Classic",
        Tagline = "Convert times  ·  live digital clocks",
        Description = "Light cool-gray UI with indigo accents and emerald LEDs",
        IsDark = false,
        AppBackground = Color.FromArgb(241, 245, 249),
        WallBack = Color.FromArgb(226, 232, 240),
        HeaderBack = Color.FromArgb(248, 250, 252),
        FooterBack = Color.FromArgb(241, 245, 249),
        CardBackground = Color.White,
        CardFace = Color.White,
        CardBorder = Color.FromArgb(203, 213, 225),
        CardTop = Color.FromArgb(255, 255, 255),
        CardBottom = Color.FromArgb(248, 250, 252),
        TileBack = Color.White,
        TileTop = Color.FromArgb(255, 255, 255),
        TileBottom = Color.FromArgb(241, 245, 249),
        HeaderTop = Color.FromArgb(255, 255, 255),
        HeaderBottom = Color.FromArgb(238, 242, 248),
        InputBack = Color.White,
        InputBorder = Color.FromArgb(148, 163, 184),
        TextPrimary = Color.FromArgb(15, 23, 42),
        TextSecondary = Color.FromArgb(71, 85, 105),
        TextMuted = Color.FromArgb(100, 116, 139),
        TextOnAccent = Color.White,
        BrandText = Color.FromArgb(15, 23, 42),
        SectionHeading = Color.FromArgb(79, 70, 229),
        Accent = Color.FromArgb(79, 70, 229),
        AccentHover = Color.FromArgb(99, 102, 241),
        AccentPressed = Color.FromArgb(55, 48, 163),
        AccentSoft = Color.FromArgb(224, 231, 255),
        AccentSoftBorder = Color.FromArgb(165, 180, 252),
        AccentSoftHover = Color.FromArgb(199, 210, 254),
        AccentSoftPressed = Color.FromArgb(165, 180, 252),
        ClockBack = Color.FromArgb(15, 23, 42),
        ClockFore = Color.FromArgb(52, 211, 153),
        ClockCore = Color.FromArgb(167, 243, 208),
        ClockDim = Color.FromArgb(30, 58, 48),
        ClockBezel = Color.FromArgb(51, 65, 85),
        BezelLight = Color.FromArgb(71, 85, 105),
        BezelDark = Color.FromArgb(30, 41, 59),
        GlassBack = Color.FromArgb(10, 14, 22),
        LedCaption = Color.FromArgb(148, 163, 184),
        // Emerald-700: the LED hue darkened until it clears AA on a white card.
        ClockTextOnSurface = Color.FromArgb(4, 120, 87),
        Success = Color.FromArgb(16, 185, 129),
        Danger = Color.FromArgb(220, 38, 38),
        DangerSoft = Color.FromArgb(254, 226, 226),
        DangerHover = Color.FromArgb(254, 202, 202),
        DangerPressed = Color.FromArgb(252, 165, 165),
        Warning = Color.FromArgb(217, 119, 6),
        SegmentIdle = Color.FromArgb(241, 245, 249),
        SegmentIdleText = Color.FromArgb(71, 85, 105),
        SegmentActive = Color.FromArgb(79, 70, 229),
        SegmentActiveText = Color.White,
        SegmentIdleHover = Color.FromArgb(226, 232, 240),
        MenuHover = Color.FromArgb(224, 231, 255),
        LivePillFill = Color.FromArgb(209, 250, 229),
        LivePillBorder = Color.FromArgb(16, 185, 129),
        CustomPillFill = Color.FromArgb(241, 245, 249),
        CustomPillBorder = Color.FromArgb(148, 163, 184)
    };

    /// <summary>Dark terminal chassis, phosphor green throughout.</summary>
    public static ThemePalette NightOps { get; } = new()
    {
        Id = AppThemeId.NightOps,
        DisplayName = "Night Ops",
        Tagline = "ZONESHIFT  ·  TERMINAL CLOCKS",
        Description = "Dark terminal console with phosphor green digits",
        IsDark = true,
        AppBackground = Color.FromArgb(10, 14, 16),
        WallBack = Color.FromArgb(8, 12, 14),
        HeaderBack = Color.FromArgb(12, 16, 18),
        FooterBack = Color.FromArgb(10, 14, 16),
        CardBackground = Color.FromArgb(18, 24, 26),
        CardFace = Color.FromArgb(18, 24, 26),
        CardBorder = Color.FromArgb(36, 52, 48),
        CardTop = Color.FromArgb(24, 32, 34),
        CardBottom = Color.FromArgb(14, 20, 22),
        TileBack = Color.FromArgb(16, 22, 24),
        TileTop = Color.FromArgb(22, 30, 32),
        TileBottom = Color.FromArgb(14, 18, 20),
        HeaderTop = Color.FromArgb(20, 28, 26),
        HeaderBottom = Color.FromArgb(10, 14, 16),
        InputBack = Color.FromArgb(12, 18, 20),
        InputBorder = Color.FromArgb(40, 70, 58),
        TextPrimary = Color.FromArgb(210, 240, 220),
        TextSecondary = Color.FromArgb(120, 170, 140),
        TextMuted = Color.FromArgb(96, 140, 116),
        TextOnAccent = Color.FromArgb(8, 16, 12),
        BrandText = Color.FromArgb(70, 230, 140),
        SectionHeading = Color.FromArgb(70, 230, 140),
        Accent = Color.FromArgb(70, 230, 140),
        AccentHover = Color.FromArgb(110, 245, 170),
        AccentPressed = Color.FromArgb(40, 180, 100),
        AccentSoft = Color.FromArgb(20, 48, 36),
        AccentSoftBorder = Color.FromArgb(50, 140, 90),
        AccentSoftHover = Color.FromArgb(28, 60, 44),
        AccentSoftPressed = Color.FromArgb(16, 40, 30),
        ClockBack = Color.FromArgb(6, 10, 12),
        ClockFore = Color.FromArgb(80, 255, 160),
        ClockCore = Color.FromArgb(180, 255, 210),
        ClockDim = Color.FromArgb(20, 50, 35),
        ClockBezel = Color.FromArgb(28, 40, 36),
        BezelLight = Color.FromArgb(40, 55, 48),
        BezelDark = Color.FromArgb(12, 18, 16),
        GlassBack = Color.FromArgb(4, 8, 8),
        LedCaption = Color.FromArgb(90, 180, 130),
        ClockTextOnSurface = Color.FromArgb(80, 255, 160),
        Success = Color.FromArgb(70, 230, 140),
        Danger = Color.FromArgb(255, 90, 100),
        DangerSoft = Color.FromArgb(50, 20, 24),
        DangerHover = Color.FromArgb(70, 28, 32),
        DangerPressed = Color.FromArgb(40, 14, 18),
        Warning = Color.FromArgb(220, 200, 60),
        SegmentIdle = Color.FromArgb(22, 32, 30),
        SegmentIdleText = Color.FromArgb(140, 190, 160),
        SegmentActive = Color.FromArgb(70, 230, 140),
        SegmentActiveText = Color.FromArgb(8, 16, 12),
        SegmentIdleHover = Color.FromArgb(32, 44, 40),
        MenuHover = Color.FromArgb(20, 48, 36),
        LivePillFill = Color.FromArgb(14, 40, 28),
        LivePillBorder = Color.FromArgb(70, 230, 140),
        CustomPillFill = Color.FromArgb(22, 32, 30),
        CustomPillBorder = Color.FromArgb(50, 80, 65)
    };

    /// <summary>Light warm-white workspace, teal chrome, coral LEDs.</summary>
    public static ThemePalette Meridian { get; } = new()
    {
        Id = AppThemeId.Meridian,
        DisplayName = "Meridian",
        Tagline = "World clock wall  ·  aligned hours",
        Description = "Light warm-white workspace with teal chrome and coral LEDs",
        IsDark = false,
        AppBackground = Color.FromArgb(242, 246, 248),
        WallBack = Color.FromArgb(232, 240, 242),
        HeaderBack = Color.FromArgb(255, 255, 255),
        FooterBack = Color.FromArgb(242, 246, 248),
        CardBackground = Color.FromArgb(255, 255, 255),
        CardFace = Color.FromArgb(255, 255, 255),
        CardBorder = Color.FromArgb(210, 224, 228),
        CardTop = Color.FromArgb(255, 255, 255),
        CardBottom = Color.FromArgb(246, 252, 252),
        TileBack = Color.FromArgb(255, 255, 255),
        TileTop = Color.FromArgb(255, 255, 255),
        TileBottom = Color.FromArgb(240, 248, 248),
        HeaderTop = Color.FromArgb(255, 255, 255),
        HeaderBottom = Color.FromArgb(236, 250, 250),
        InputBack = Color.FromArgb(255, 255, 255),
        InputBorder = Color.FromArgb(180, 210, 214),
        TextPrimary = Color.FromArgb(24, 40, 48),
        TextSecondary = Color.FromArgb(80, 110, 118),
        TextMuted = Color.FromArgb(118, 142, 150),
        TextOnAccent = Color.White,
        BrandText = Color.FromArgb(24, 40, 48),
        SectionHeading = Color.FromArgb(0, 128, 128),
        // Teal-700 rather than the mockup's bright teal: white label text needs 4.5:1.
        Accent = Color.FromArgb(0, 128, 128),
        AccentHover = Color.FromArgb(0, 144, 144),
        AccentPressed = Color.FromArgb(0, 105, 105),
        AccentSoft = Color.FromArgb(220, 245, 245),
        AccentSoftBorder = Color.FromArgb(120, 200, 200),
        AccentSoftHover = Color.FromArgb(200, 238, 238),
        AccentSoftPressed = Color.FromArgb(170, 220, 220),
        ClockBack = Color.FromArgb(18, 28, 34),
        ClockFore = Color.FromArgb(255, 120, 80),
        ClockCore = Color.FromArgb(255, 190, 160),
        ClockDim = Color.FromArgb(50, 40, 36),
        ClockBezel = Color.FromArgb(40, 55, 62),
        BezelLight = Color.FromArgb(55, 72, 80),
        BezelDark = Color.FromArgb(22, 32, 38),
        GlassBack = Color.FromArgb(10, 16, 20),
        LedCaption = Color.FromArgb(120, 210, 170),
        // Burnt coral: the LED hue darkened until it clears AA on a white card.
        ClockTextOnSurface = Color.FromArgb(191, 64, 32),
        Success = Color.FromArgb(17, 138, 103),
        Danger = Color.FromArgb(200, 45, 55),
        DangerSoft = Color.FromArgb(255, 232, 234),
        DangerHover = Color.FromArgb(255, 214, 218),
        DangerPressed = Color.FromArgb(250, 180, 188),
        Warning = Color.FromArgb(180, 80, 20),
        SegmentIdle = Color.FromArgb(236, 246, 246),
        SegmentIdleText = Color.FromArgb(70, 100, 108),
        SegmentActive = Color.FromArgb(0, 128, 128),
        SegmentActiveText = Color.White,
        SegmentIdleHover = Color.FromArgb(220, 238, 238),
        MenuHover = Color.FromArgb(220, 245, 245),
        LivePillFill = Color.FromArgb(220, 250, 240),
        LivePillBorder = Color.FromArgb(17, 138, 103),
        CustomPillFill = Color.FromArgb(236, 246, 246),
        CustomPillBorder = Color.FromArgb(160, 200, 205)
    };

    public static IReadOnlyList<ThemePalette> All { get; } =
    [
        Studio,
        Classic,
        NightOps,
        Meridian
    ];

    public static ThemePalette FromId(AppThemeId id) =>
        id switch
        {
            AppThemeId.Classic => Classic,
            AppThemeId.NightOps => NightOps,
            AppThemeId.Meridian => Meridian,
            _ => Studio
        };

    /// <summary>
    /// Resolves a persisted theme name. Retired theme names map to their nearest surviving
    /// relative so an upgrade never silently resets a user's choice to the default.
    /// </summary>
    public static ThemePalette FromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Studio;

        var trimmed = name.Trim();

        // Reject bare integers before Enum.TryParse would happily coerce them. Nothing ever
        // persists an ordinal, and enum values have been reused, so "2" is meaningless.
        if (!trimmed.All(char.IsAsciiDigit))
        {
            if (Enum.TryParse<AppThemeId>(trimmed, ignoreCase: true, out var id))
                return FromId(id);
        }

        return trimmed.ToLowerInvariant() switch
        {
            "classic" or "original" or "light" => Classic,
            "night ops" or "nightops" or "terminal" or "ops" => NightOps,
            "meridian" or "hours" => Meridian,
            // Retired in 1.7.0. Neon Pulse was a dark chassis with saturated glowing digits;
            // Night Ops is its closest surviving relative.
            "neon" or "neonpulse" or "neon pulse" or "cyber" => NightOps,
            _ => Studio
        };
    }
}
