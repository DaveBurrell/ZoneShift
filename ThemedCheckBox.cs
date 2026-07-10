namespace TimezoneConverter;

/// <summary>
/// Check box whose glyph is painted from the palette.
/// <para>
/// WinForms draws the standard glyph from <see cref="SystemColors"/>, which renders as a white box
/// on the dark themes. <c>Application.SetColorMode</c> does not reach it, and switching to
/// <see cref="FlatStyle.System"/> only borrows Windows' blue accent — a fifth color belonging to no
/// ZoneShift theme. So we draw it.
/// </para>
/// </summary>
internal sealed class ThemedCheckBox : CheckBox
{
    private const int GlyphSize = 15;
    private const int GlyphTextGap = 7;

    private bool _hovered;

    public ThemedCheckBox()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        AutoSize = false;
        FlatStyle = FlatStyle.Flat;
        Cursor = Cursors.Hand;
        TextAlign = ContentAlignment.MiddleLeft;

        UiTheme.ThemeChanged += OnThemeChanged;
        Disposed += (_, _) => UiTheme.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged()
    {
        if (IsDisposed) return;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        Invalidate();
        base.OnCheckedChanged(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    /// <summary>Width the control needs for its glyph, gap, and full text.</summary>
    public Size MeasureNaturalSize()
    {
        var text = TextRenderer.MeasureText(Text, Font);
        return new Size(GlyphSize + GlyphTextGap + text.Width + 2, Math.Max(GlyphSize + 2, text.Height));
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        UiPaint.FillGutter(e.Graphics, ClientRectangle, BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        UiPaint.FillGutter(g, ClientRectangle, BackColor);
        UiPaint.EnableQuality(g);

        var glyph = new Rectangle(0, (Height - GlyphSize) / 2, GlyphSize, GlyphSize);

        var border = !Enabled
            ? UiTheme.TextMuted
            : Checked
                ? UiTheme.Accent
                : _hovered
                    ? UiTheme.Accent
                    : UiTheme.InputBorder;

        if (Checked)
        {
            using var fill = new SolidBrush(Enabled ? UiTheme.Accent : UiTheme.TextMuted);
            UiPaint.FillRoundRect(g, fill, glyph, 3);
        }
        else
        {
            using var fill = new SolidBrush(UiTheme.InputBack);
            UiPaint.FillRoundRect(g, fill, glyph, 3);
        }

        using (var pen = new Pen(border, 1f))
            UiPaint.DrawRoundRect(g, pen, glyph, 3);

        if (Checked)
            DrawTick(g, glyph, Enabled ? UiTheme.TextOnAccent : UiTheme.InputBack);

        var textColor = Enabled ? ForeColor : UiTheme.TextMuted;
        var textRect = new Rectangle(
            glyph.Right + GlyphTextGap, 0, Math.Max(0, Width - glyph.Right - GlyphTextGap), Height);

        TextRenderer.DrawText(
            g, Text, Font, textRect, textColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix |
            TextFormatFlags.EndEllipsis);

        if (Focused && ShowFocusCues)
        {
            var focus = Rectangle.Inflate(glyph, 2, 2);
            using var pen = new Pen(UiTheme.Accent, 1f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
            g.DrawRectangle(pen, focus);
        }
    }

    private static void DrawTick(Graphics g, Rectangle glyph, Color color)
    {
        using var pen = new Pen(color, 1.9f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };

        var l = glyph.Left;
        var t = glyph.Top;
        var s = glyph.Width;
        g.DrawLines(pen,
        [
            new PointF(l + s * 0.24f, t + s * 0.52f),
            new PointF(l + s * 0.43f, t + s * 0.71f),
            new PointF(l + s * 0.77f, t + s * 0.30f)
        ]);
    }
}
