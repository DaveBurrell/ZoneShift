namespace TimezoneConverter;

/// <summary>
/// Active theme facade. All colors resolve from the current <see cref="ThemePalette"/>.
/// Subscribe to <see cref="ThemeChanged"/> to repaint chrome when the user switches themes.
/// </summary>
internal static class UiTheme
{
    private static ThemePalette _current = ThemePalette.Studio;

    public static ThemePalette Current => _current;
    public static AppThemeId CurrentId => _current.Id;

    /// <summary>Raised on the UI thread after <see cref="SetTheme"/>.</summary>
    public static event Action? ThemeChanged;

    public static void SetTheme(AppThemeId id, bool raiseEvent = true)
    {
        var next = ThemePalette.FromId(id);
        if (ReferenceEquals(_current, next) && raiseEvent == false)
        {
            _current = next;
            return;
        }

        if (_current.Id == next.Id && raiseEvent)
        {
            // still raise so first load can sync, but skip if identical and already applied
        }

        _current = next;
        if (raiseEvent)
            ThemeChanged?.Invoke();
    }

    public static void SetTheme(string? name, bool raiseEvent = true) =>
        SetTheme(ThemePalette.FromName(name).Id, raiseEvent);

    // --- Surfaces ---
    public static Color AppBackground => _current.AppBackground;
    public static Color WallBack => _current.WallBack;
    public static Color HeaderBack => _current.HeaderBack;
    public static Color FooterBack => _current.FooterBack;
    public static Color CardBackground => _current.CardBackground;
    public static Color CardFace => _current.CardFace;
    public static Color CardBorder => _current.CardBorder;
    public static Color CardTop => _current.CardTop;
    public static Color CardBottom => _current.CardBottom;
    public static Color TileBack => _current.TileBack;
    public static Color TileTop => _current.TileTop;
    public static Color TileBottom => _current.TileBottom;
    public static Color HeaderTop => _current.HeaderTop;
    public static Color HeaderBottom => _current.HeaderBottom;
    public static Color InputBack => _current.InputBack;
    public static Color InputBorder => _current.InputBorder;

    // --- Text ---
    public static Color TextPrimary => _current.TextPrimary;
    public static Color TextSecondary => _current.TextSecondary;
    public static Color TextMuted => _current.TextMuted;
    public static Color TextOnAccent => _current.TextOnAccent;

    // --- Accent ---
    public static Color Accent => _current.Accent;
    public static Color AccentHover => _current.AccentHover;
    public static Color AccentPressed => _current.AccentPressed;
    public static Color AccentSoft => _current.AccentSoft;
    public static Color AccentSoftBorder => _current.AccentSoftBorder;
    public static Color AccentSoftHover => _current.AccentSoftHover;
    public static Color AccentSoftPressed => _current.AccentSoftPressed;

    // --- LED ---
    public static Color ClockBack => _current.ClockBack;
    public static Color ClockFore => _current.ClockFore;
    public static Color ClockCore => _current.ClockCore;
    public static Color ClockDim => _current.ClockDim;
    public static Color ClockBezel => _current.ClockBezel;
    public static Color BezelLight => _current.BezelLight;
    public static Color BezelDark => _current.BezelDark;
    public static Color GlassBack => _current.GlassBack;
    public static Color LedCaption => _current.LedCaption;

    // --- Semantic ---
    public static Color Success => _current.Success;
    public static Color Danger => _current.Danger;
    public static Color DangerSoft => _current.DangerSoft;
    public static Color DangerHover => _current.DangerHover;
    public static Color DangerPressed => _current.DangerPressed;
    public static Color Warning => _current.Warning;

    // --- Segments ---
    public static Color SegmentIdle => _current.SegmentIdle;
    public static Color SegmentIdleText => _current.SegmentIdleText;
    public static Color SegmentActive => _current.SegmentActive;
    public static Color SegmentActiveText => _current.SegmentActiveText;
    public static Color SegmentIdleHover => _current.SegmentIdleHover;

    // --- Menu / pills ---
    public static Color MenuHover => _current.MenuHover;
    public static Color LivePillFill => _current.LivePillFill;
    public static Color LivePillBorder => _current.LivePillBorder;
    public static Color CustomPillFill => _current.CustomPillFill;
    public static Color CustomPillBorder => _current.CustomPillBorder;

    public static bool IsDark => _current.IsDark;
    public static string Tagline => _current.Tagline;
    public static string DisplayName => _current.DisplayName;

    public static Font TitleFont => new("Segoe UI Semibold", 16f);
    public static Font SectionFont => new("Segoe UI Semibold", 10f);
    public static Font BodyFont => new("Segoe UI", 9f);
    public static Font CaptionFont => new("Segoe UI", 8.25f);
    public static Font ClockHeroFont => new("Consolas", 28f, FontStyle.Bold);
    public static Font ClockLargeFont => new("Consolas", 22f, FontStyle.Bold);
    public static Font ClockRowFont => new("Consolas", 18f, FontStyle.Bold);
    public static Font ZoneLabelFont => new("Segoe UI Semibold", 11f);

    public static void StylePrimaryButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.UseVisualStyleBackColor = false;
        btn.BackColor = Accent;
        btn.ForeColor = TextOnAccent;
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.BorderColor = Accent;
        btn.FlatAppearance.MouseOverBackColor = AccentHover;
        btn.FlatAppearance.MouseDownBackColor = AccentPressed;
        btn.Cursor = Cursors.Hand;
        if (btn.Font.Style != FontStyle.Bold)
            btn.Font = new Font("Segoe UI Semibold", btn.Font.Size);
    }

    public static void StyleSecondaryButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.UseVisualStyleBackColor = false;
        btn.BackColor = AccentSoft;
        btn.ForeColor = Accent;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = AccentSoftBorder;
        btn.FlatAppearance.MouseOverBackColor = AccentSoftHover;
        btn.FlatAppearance.MouseDownBackColor = AccentSoftPressed;
        btn.Cursor = Cursors.Hand;
    }

    public static void StyleDangerButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.UseVisualStyleBackColor = false;
        btn.BackColor = DangerSoft;
        btn.ForeColor = Danger;
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.BorderColor = DangerSoft;
        btn.FlatAppearance.MouseOverBackColor = DangerHover;
        btn.FlatAppearance.MouseDownBackColor = DangerPressed;
        btn.Cursor = Cursors.Hand;
    }

    public static void StyleInput(Control control)
    {
        control.BackColor = InputBack;
        control.ForeColor = TextPrimary;
        if (control is ComboBox cb)
            cb.FlatStyle = FlatStyle.Flat;
    }
}
