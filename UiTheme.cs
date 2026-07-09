namespace TimezoneConverter;

internal static class UiTheme
{
    public static readonly Color AppBackground = Color.FromArgb(241, 245, 249);
    public static readonly Color CardBackground = Color.White;
    public static readonly Color CardBorder = Color.FromArgb(226, 232, 240);
    public static readonly Color Accent = Color.FromArgb(79, 70, 229);       // indigo
    public static readonly Color AccentSoft = Color.FromArgb(224, 231, 255);
    public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);
    public static readonly Color TextSecondary = Color.FromArgb(100, 116, 139);
    public static readonly Color TextMuted = Color.FromArgb(148, 163, 184);
    public static readonly Color ClockBack = Color.FromArgb(15, 23, 42);
    public static readonly Color ClockFore = Color.FromArgb(52, 211, 153);    // emerald
    public static readonly Color ClockDim = Color.FromArgb(51, 65, 85);
    public static readonly Color Success = Color.FromArgb(16, 185, 129);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Color SegmentIdle = Color.FromArgb(241, 245, 249);
    public static readonly Color SegmentActive = Color.FromArgb(79, 70, 229);

    public static Font TitleFont => new("Segoe UI Semibold", 18f);
    public static Font SectionFont => new("Segoe UI Semibold", 11f);
    public static Font BodyFont => new("Segoe UI", 9.5f);
    public static Font CaptionFont => new("Segoe UI", 8.25f);
    public static Font ClockLargeFont => new("Consolas", 20f, FontStyle.Bold);
    public static Font ClockRowFont => new("Consolas", 14f, FontStyle.Bold);
}
