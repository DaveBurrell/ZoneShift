namespace TimezoneConverter;

/// <summary>
/// Shared GDI+ helpers. Surfaces pull live colors from <see cref="UiTheme"/>.
/// </summary>
internal static class UiPaint
{
    public static void EnableQuality(Graphics g)
    {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
    }

    public static void FillGutter(Graphics g, Rectangle bounds, Color color)
    {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
        using var b = new SolidBrush(color);
        g.FillRectangle(b, bounds);
    }

    public static void FillRoundRect(Graphics g, Brush brush, Rectangle rect, int radius)
    {
        if (rect.Width < 2 || rect.Height < 2)
            return;
        using var path = RoundRect(rect, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRoundRect(Graphics g, Pen pen, Rectangle rect, int radius)
    {
        if (rect.Width < 2 || rect.Height < 2)
            return;
        var inset = Rectangle.Inflate(rect, -1, -1);
        if (inset.Width < 2 || inset.Height < 2)
            return;
        using var path = RoundRect(inset, Math.Max(1, radius - 1));
        g.DrawPath(pen, path);
    }

    public static System.Drawing.Drawing2D.GraphicsPath RoundRect(Rectangle rect, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        if (rect.Width < 2 || rect.Height < 2)
        {
            path.AddRectangle(rect);
            return path;
        }

        var r = Math.Max(1, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2));
        var d = r * 2;
        var x = rect.X;
        var y = rect.Y;
        var w = rect.Width;
        var h = rect.Height;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        path.AddArc(x, y + h - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void PaintStudioCard(Graphics g, Rectangle bounds, Color gutter)
    {
        // Full rectangular gutter first (parent app color)
        FillGutter(g, bounds, gutter);
        EnableQuality(g);

        // Slight inset so rounded stroke never clips against the outer panel edge
        var body = Rectangle.Inflate(bounds, -1, -1);
        if (body.Width < 8 || body.Height < 8)
            body = bounds;

        using (var bg = new System.Drawing.Drawing2D.LinearGradientBrush(
                   body, UiTheme.CardTop, UiTheme.CardBottom, 90f))
            FillRoundRect(g, bg, body, 10);

        using var edge = new Pen(UiTheme.CardBorder, 1f);
        DrawRoundRect(g, edge, body, 10);

        using var hi = new Pen(Color.FromArgb(UiTheme.IsDark ? 36 : 80, 255, 255, 255), 1f);
        g.DrawLine(hi, body.X + 14, body.Y + 1, body.Right - 15, body.Y + 1);
    }

    public static void PaintWallBackground(Graphics g, Rectangle bounds)
    {
        FillGutter(g, bounds, UiTheme.WallBack);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

        var gridAlpha = UiTheme.IsDark ? 22 : 35;
        using var grid = new Pen(Color.FromArgb(gridAlpha, UiTheme.TextSecondary), 1f);
        for (var x = bounds.X; x < bounds.Right; x += 28)
            g.DrawLine(grid, x, bounds.Y, x, bounds.Bottom);
        for (var y = bounds.Y; y < bounds.Bottom; y += 28)
            g.DrawLine(grid, bounds.X, y, bounds.Right, y);
    }

    public static void PaintHeaderBar(Graphics g, Rectangle bounds)
    {
        FillGutter(g, bounds, UiTheme.HeaderBack);
        using var bg = new System.Drawing.Drawing2D.LinearGradientBrush(
            bounds, UiTheme.HeaderTop, UiTheme.HeaderBottom, 90f);
        g.FillRectangle(bg, bounds);

        using var accent = new SolidBrush(UiTheme.Accent);
        g.FillRectangle(accent, bounds.X, bounds.Bottom - 2, bounds.Width, 2);
    }

    public static void DrawGlowText(
        Graphics g,
        string text,
        Font font,
        Color glow,
        Color core,
        Rectangle layout,
        StringFormat format)
    {
        if (string.IsNullOrEmpty(text))
            return;

        using (var b = new SolidBrush(Color.FromArgb(70, glow)))
        {
            var r = layout;
            r.Offset(0, 1);
            g.DrawString(text, font, b, r, format);
        }

        using (var b = new SolidBrush(core))
            g.DrawString(text, font, b, layout, format);
    }

    public static void PaintLivePill(Graphics g, Rectangle bounds, bool live, Color gutter)
    {
        FillGutter(g, bounds, gutter);
        EnableQuality(g);

        var fill = live ? UiTheme.LivePillFill : UiTheme.CustomPillFill;
        var border = live ? UiTheme.LivePillBorder : UiTheme.CustomPillBorder;
        using (var b = new SolidBrush(fill))
            FillRoundRect(g, b, bounds, bounds.Height / 2);
        using (var p = new Pen(border, 1f))
            DrawRoundRect(g, p, bounds, bounds.Height / 2);

        var dot = new Rectangle(bounds.X + 8, bounds.Y + (bounds.Height - 7) / 2, 7, 7);
        using (var b = new SolidBrush(live ? UiTheme.Success : UiTheme.TextMuted))
            g.FillEllipse(b, dot);

        var text = live ? "LIVE" : "CUSTOM";
        using var font = new Font("Segoe UI Semibold", 8f);
        using var tb = new SolidBrush(live ? UiTheme.Success : UiTheme.TextPrimary);
        var textRect = new Rectangle(bounds.X + 20, bounds.Y, bounds.Width - 26, bounds.Height);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap
        };
        g.DrawString(text, font, tb, textRect, sf);
    }

    public static void PaintClockTile(Graphics g, Rectangle bounds, Color gutter)
    {
        FillGutter(g, bounds, gutter);
        EnableQuality(g);

        // Leave a 1px breathing room for AA against the wall grid
        var body = Rectangle.Inflate(bounds, -1, -1);
        if (body.Width < 8 || body.Height < 8)
            body = bounds;

        using (var bg = new System.Drawing.Drawing2D.LinearGradientBrush(
                   body, UiTheme.TileTop, UiTheme.TileBottom, 90f))
            FillRoundRect(g, bg, body, 10);

        using var edge = new Pen(UiTheme.CardBorder, 1f);
        DrawRoundRect(g, edge, body, 10);

        using var accent = new Pen(Color.FromArgb(180, UiTheme.Accent), 1f);
        g.DrawLine(accent, body.X + 16, body.Y + 3, body.Right - 17, body.Y + 3);
    }
}
