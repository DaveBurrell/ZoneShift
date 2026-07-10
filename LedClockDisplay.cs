using System.ComponentModel;

namespace TimezoneConverter;

/// <summary>
/// Custom-painted newsroom LED clock module with bezel, ghost digits, and glow.
/// Corners are always filled with <see cref="GutterColor"/> so AA never fringes.
/// </summary>
internal sealed class LedClockDisplay : Control
{
    private string _timeText = "--:--";
    private string _zoneText = "";
    private string _captionText = "";
    private bool _large;
    private bool _colonOn = true;
    private Color _gutterColor = UiTheme.TileBack;
    private readonly System.Windows.Forms.Timer? _blinkTimer;

    public LedClockDisplay(bool large = false)
    {
        _large = large;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        // Opaque matching parent — never Transparent (causes black/white flashes)
        BackColor = UiTheme.TileBack;
        _gutterColor = UiTheme.TileBack;
        TabStop = false;
        Size = large ? new Size(280, 140) : new Size(180, 56);

        UiTheme.ThemeChanged += OnThemeChanged;
        Disposed += (_, _) =>
        {
            UiTheme.ThemeChanged -= OnThemeChanged;
            _blinkTimer?.Dispose();
        };

        if (large)
        {
            _blinkTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _blinkTimer.Tick += (_, _) =>
            {
                _colonOn = !_colonOn;
                Invalidate();
            };
            _blinkTimer.Start();
        }
    }

    private void OnThemeChanged()
    {
        if (IsDisposed) return;
        Invalidate();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public Color GutterColor
    {
        get => _gutterColor;
        set
        {
            _gutterColor = value;
            BackColor = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string TimeText
    {
        get => _timeText;
        set
        {
            var v = value ?? "--:--";
            if (_timeText == v) return;
            _timeText = v;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string ZoneText
    {
        get => _zoneText;
        set
        {
            var v = value ?? "";
            if (_zoneText == v) return;
            _zoneText = v;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string CaptionText
    {
        get => _captionText;
        set
        {
            var v = value ?? "";
            if (_captionText == v) return;
            _captionText = v;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool BlinkColons
    {
        get => _blinkTimer?.Enabled == true;
        set
        {
            if (_blinkTimer is null) return;
            if (value) _blinkTimer.Start();
            else
            {
                _blinkTimer.Stop();
                _colonOn = true;
                Invalidate();
            }
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Full solid clear — prevents black residual marks between blinks
        UiPaint.FillGutter(e.Graphics, ClientRectangle, _gutterColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var bounds = ClientRectangle;
        if (bounds.Width < 8 || bounds.Height < 8)
            return;

        // Gutter again in paint (double-buffer safe)
        UiPaint.FillGutter(g, bounds, _gutterColor);
        UiPaint.EnableQuality(g);

        var radius = _large ? 10 : 7;

        // Outer metal bezel — fill full rect first as square under-layer so corners match
        using (var under = new SolidBrush(UiTheme.BezelDark))
            g.FillRectangle(under, bounds);

        using (var metal = new System.Drawing.Drawing2D.LinearGradientBrush(
                   bounds, UiTheme.BezelLight, UiTheme.BezelDark, 90f))
        {
            UiPaint.FillRoundRect(g, metal, bounds, radius);
        }

        using (var hi = new Pen(Color.FromArgb(50, UiTheme.TextSecondary), 1f))
            UiPaint.DrawRoundRect(g, hi, bounds, radius);

        // Inner chassis
        var pad = _large ? 5 : 3;
        var chassis = Rectangle.Inflate(bounds, -pad, -pad);
        using (var ch = new SolidBrush(UiTheme.ClockBack))
            UiPaint.FillRoundRect(g, ch, chassis, Math.Max(2, radius - 2));

        // Recessed glass
        var glass = Rectangle.Inflate(chassis, _large ? -4 : -2, _large ? -4 : -2);
        using (var glassBrush = new SolidBrush(UiTheme.GlassBack))
            UiPaint.FillRoundRect(g, glassBrush, glass, Math.Max(2, radius - 3));

        using (var edge = new Pen(Color.FromArgb(40, UiTheme.ClockFore), 1f))
            UiPaint.DrawRoundRect(g, edge, glass, Math.Max(2, radius - 3));

        // Scanlines (very subtle)
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        using (var scan = new Pen(Color.FromArgb(12, UiTheme.TextSecondary), 1f))
        {
            for (var y = glass.Y + 2; y < glass.Bottom - 1; y += 4)
                g.DrawLine(scan, glass.X + 3, y, glass.Right - 4, y);
        }
        UiPaint.EnableQuality(g);

        // Content layout
        var zoneH = _large ? 20 : (!string.IsNullOrWhiteSpace(_zoneText) ? 15 : 0);
        var capH = _large ? 18 : (!string.IsNullOrWhiteSpace(_captionText) ? 13 : 0);
        var content = Rectangle.Inflate(glass, _large ? -6 : -3, _large ? -4 : -2);

        using var sfCenter = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        if (zoneH > 0 && !string.IsNullOrWhiteSpace(_zoneText))
        {
            var zoneRect = new Rectangle(content.X, content.Y, content.Width, zoneH);
            using var zoneFont = new Font("Segoe UI Semibold", _large ? 9.5f : 8f);
            using var zoneBrush = new SolidBrush(UiTheme.Accent);
            g.DrawString(_zoneText.ToUpperInvariant(), zoneFont, zoneBrush, zoneRect, sfCenter);
        }

        var timeTop = content.Y + zoneH;
        var timeH = Math.Max(18, content.Height - zoneH - capH);
        var timeRect = new Rectangle(content.X, timeTop, content.Width, timeH);

        var ghost = BuildGhostMask(_timeText);
        using (var ghostFont = CreateTimeFont(timeRect))
        using (var ghostBrush = new SolidBrush(Color.FromArgb(_large ? 28 : 20, UiTheme.ClockDim)))
            g.DrawString(ghost, ghostFont, ghostBrush, timeRect, sfCenter);

        var displayTime = ApplyColonBlink(_timeText);
        using (var timeFont = CreateTimeFont(timeRect))
        {
            UiPaint.DrawGlowText(
                g,
                displayTime,
                timeFont,
                UiTheme.ClockFore,
                UiTheme.ClockCore,
                timeRect,
                sfCenter);
        }

        if (capH > 0 && !string.IsNullOrWhiteSpace(_captionText))
        {
            var capRect = new Rectangle(content.X, content.Bottom - capH, content.Width, capH);
            using var capFont = new Font("Segoe UI Semibold", _large ? 7.5f : 7f);
            using var capBrush = new SolidBrush(UiTheme.LedCaption);
            g.DrawString(_captionText, capFont, capBrush, capRect, sfCenter);
        }
    }

    private static readonly string[] MonoFamilies =
        ["Consolas", "Cascadia Mono", "Lucida Console", "Courier New"];

    /// <summary>
    /// Resolves a monospace face. The digits and the blinking colon rely on a fixed advance
    /// width, so an installed family is probed via <see cref="FontFamily"/> — the
    /// <see cref="Font"/> constructor silently substitutes a proportional face instead of
    /// throwing when the name is unknown.
    /// </summary>
    private Font CreateTimeFont(Rectangle timeRect)
    {
        var size = _large
            ? Math.Clamp(timeRect.Height * 0.52f, 16f, 34f)
            : Math.Clamp(timeRect.Height * 0.58f, 11f, 20f);

        foreach (var family in MonoFamilies)
        {
            try
            {
                new FontFamily(family).Dispose();
            }
            catch (ArgumentException)
            {
                continue;
            }

            return new Font(family, size, FontStyle.Bold, GraphicsUnit.Point);
        }

        return new Font(FontFamily.GenericMonospace, size, FontStyle.Bold);
    }

    private string ApplyColonBlink(string text)
    {
        if (_colonOn || _blinkTimer is null || !_blinkTimer.Enabled)
            return text;

        // CreateTimeFont only ever resolves a monospace family, so ' ' and ':' share an
        // advance width and blanking the colon cannot shift the digits. The ghost "88:88"
        // mask still shows a dim colon underneath, which is how an unlit LED segment reads.
        return text.Replace(':', ' ');
    }

    private static string BuildGhostMask(string timeText)
    {
        if (string.IsNullOrWhiteSpace(timeText) || timeText.Contains("--", StringComparison.Ordinal))
            return "88:88";

        var chars = timeText.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (char.IsDigit(c))
                chars[i] = '8';
            else if (c is ':' or ' ')
                chars[i] = c;
            else if (char.IsLetter(c))
                chars[i] = c; // keep AM/PM width
        }

        return new string(chars);
    }
}
