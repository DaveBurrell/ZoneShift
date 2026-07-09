namespace TimezoneConverter;

/// <summary>Built-in visual themes.</summary>
public enum AppThemeId
{
    Studio = 0,   // current newsroom amber
    Classic = 1,  // original light indigo
    NeonPulse = 2 // creative cyberpunk
}

/// <summary>Full color set for one visual theme.</summary>
public sealed class ThemePalette
{
    public required AppThemeId Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Tagline { get; init; }
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

    // Accent
    public required Color Accent { get; init; }
    public required Color AccentHover { get; init; }
    public required Color AccentPressed { get; init; }
    public required Color AccentSoft { get; init; }
    public required Color AccentSoftBorder { get; init; }
    public required Color AccentSoftHover { get; init; }
    public required Color AccentSoftPressed { get; init; }

    // LED
    public required Color ClockBack { get; init; }
    public required Color ClockFore { get; init; }
    public required Color ClockCore { get; init; }
    public required Color ClockDim { get; init; }
    public required Color ClockBezel { get; init; }
    public required Color BezelLight { get; init; }
    public required Color BezelDark { get; init; }
    public required Color GlassBack { get; init; }
    public required Color LedCaption { get; init; }

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

    public static ThemePalette Studio { get; } = new()
    {
        Id = AppThemeId.Studio,
        DisplayName = "Studio",
        Tagline = "STUDIO  ·  WORLD CLOCK WALL",
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

    /// <summary>Original ZoneShift light UI: cool gray canvas, indigo accents, emerald LEDs.</summary>
    public static ThemePalette Classic { get; } = new()
    {
        Id = AppThemeId.Classic,
        DisplayName = "Classic",
        Tagline = "Convert times  ·  live digital clocks",
        IsDark = false,
        AppBackground = Color.FromArgb(241, 245, 249),
        WallBack = Color.FromArgb(226, 232, 240),
        HeaderBack = Color.FromArgb(79, 70, 229),
        FooterBack = Color.FromArgb(241, 245, 249),
        CardBackground = Color.White,
        CardFace = Color.White,
        CardBorder = Color.FromArgb(203, 213, 225),
        CardTop = Color.FromArgb(255, 255, 255),
        CardBottom = Color.FromArgb(248, 250, 252),
        TileBack = Color.White,
        TileTop = Color.FromArgb(255, 255, 255),
        TileBottom = Color.FromArgb(241, 245, 249),
        HeaderTop = Color.FromArgb(99, 102, 241),
        HeaderBottom = Color.FromArgb(67, 56, 202),
        InputBack = Color.White,
        InputBorder = Color.FromArgb(148, 163, 184),
        TextPrimary = Color.FromArgb(15, 23, 42),
        TextSecondary = Color.FromArgb(71, 85, 105),
        TextMuted = Color.FromArgb(100, 116, 139),
        TextOnAccent = Color.White,
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

    /// <summary>
    /// Neon Pulse — creative cyberpunk: void purple, electric cyan LEDs, hot magenta accents.
    /// </summary>
    public static ThemePalette NeonPulse { get; } = new()
    {
        Id = AppThemeId.NeonPulse,
        DisplayName = "Neon Pulse",
        Tagline = "NIGHT CIRCUIT  ·  SYNTH CLOCKS",
        IsDark = true,
        AppBackground = Color.FromArgb(12, 8, 24),
        WallBack = Color.FromArgb(8, 4, 18),
        HeaderBack = Color.FromArgb(10, 6, 22),
        FooterBack = Color.FromArgb(10, 6, 22),
        CardBackground = Color.FromArgb(28, 16, 48),
        CardFace = Color.FromArgb(28, 16, 48),
        CardBorder = Color.FromArgb(120, 40, 140),
        CardTop = Color.FromArgb(42, 22, 68),
        CardBottom = Color.FromArgb(22, 12, 40),
        TileBack = Color.FromArgb(34, 18, 58),
        TileTop = Color.FromArgb(48, 24, 78),
        TileBottom = Color.FromArgb(28, 14, 48),
        HeaderTop = Color.FromArgb(40, 12, 70),
        HeaderBottom = Color.FromArgb(10, 6, 22),
        InputBack = Color.FromArgb(16, 10, 32),
        InputBorder = Color.FromArgb(180, 60, 200),
        TextPrimary = Color.FromArgb(245, 240, 255),
        TextSecondary = Color.FromArgb(200, 180, 230),
        TextMuted = Color.FromArgb(160, 140, 190),
        TextOnAccent = Color.FromArgb(20, 4, 28),
        Accent = Color.FromArgb(255, 46, 166),          // hot magenta
        AccentHover = Color.FromArgb(255, 105, 190),
        AccentPressed = Color.FromArgb(200, 20, 130),
        AccentSoft = Color.FromArgb(70, 20, 60),
        AccentSoftBorder = Color.FromArgb(220, 60, 160),
        AccentSoftHover = Color.FromArgb(95, 28, 80),
        AccentSoftPressed = Color.FromArgb(55, 14, 48),
        ClockBack = Color.FromArgb(6, 4, 16),
        ClockFore = Color.FromArgb(34, 255, 240),       // electric cyan
        ClockCore = Color.FromArgb(180, 255, 250),
        ClockDim = Color.FromArgb(20, 60, 70),
        ClockBezel = Color.FromArgb(80, 40, 120),
        BezelLight = Color.FromArgb(100, 50, 150),
        BezelDark = Color.FromArgb(30, 15, 55),
        GlassBack = Color.FromArgb(4, 8, 18),
        LedCaption = Color.FromArgb(160, 240, 255),
        Success = Color.FromArgb(57, 255, 20),           // phosphor green
        Danger = Color.FromArgb(255, 80, 120),
        DangerSoft = Color.FromArgb(70, 16, 40),
        DangerHover = Color.FromArgb(100, 24, 55),
        DangerPressed = Color.FromArgb(55, 10, 30),
        Warning = Color.FromArgb(255, 230, 60),
        SegmentIdle = Color.FromArgb(40, 22, 70),
        SegmentIdleText = Color.FromArgb(230, 210, 255),
        SegmentActive = Color.FromArgb(255, 46, 166),
        SegmentActiveText = Color.FromArgb(20, 4, 28),
        SegmentIdleHover = Color.FromArgb(60, 32, 100),
        MenuHover = Color.FromArgb(60, 20, 70),
        LivePillFill = Color.FromArgb(12, 50, 40),
        LivePillBorder = Color.FromArgb(57, 255, 20),
        CustomPillFill = Color.FromArgb(40, 22, 70),
        CustomPillBorder = Color.FromArgb(160, 80, 200)
    };

    public static IReadOnlyList<ThemePalette> All { get; } =
        [Studio, Classic, NeonPulse];

    public static ThemePalette FromId(AppThemeId id) =>
        id switch
        {
            AppThemeId.Classic => Classic,
            AppThemeId.NeonPulse => NeonPulse,
            _ => Studio
        };

    public static ThemePalette FromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Studio;
        if (Enum.TryParse<AppThemeId>(name, ignoreCase: true, out var id))
            return FromId(id);
        return name.Trim().ToLowerInvariant() switch
        {
            "classic" or "original" or "light" => Classic,
            "neon" or "neonpulse" or "neon pulse" or "cyber" => NeonPulse,
            _ => Studio
        };
    }
}
