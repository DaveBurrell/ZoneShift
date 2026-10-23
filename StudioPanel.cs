using System.ComponentModel;

namespace TimezoneConverter;

/// <summary>
/// Card panel with painted frame. Always clears gutter to parent color first.
/// </summary>
internal sealed class StudioPanel : Panel
{
    private readonly bool _wallGrid;

    public StudioPanel(bool wallGrid = false)
    {
        _wallGrid = wallGrid;
        DoubleBuffered = true;
        BackColor = wallGrid ? UiTheme.WallBack : UiTheme.AppBackground;
        BorderStyle = BorderStyle.None;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        UiTheme.ThemeChanged += OnThemeChanged;
        Disposed += (_, _) => UiTheme.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged()
    {
        if (IsDisposed) return;
        BackColor = _wallGrid ? UiTheme.WallBack : UiTheme.AppBackground;
        Invalidate(true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        UiPaint.FillGutter(e.Graphics, ClientRectangle, BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_wallGrid)
            UiPaint.PaintWallBackground(e.Graphics, ClientRectangle);
        else
            UiPaint.PaintStudioCard(e.Graphics, ClientRectangle, UiTheme.AppBackground);
    }
}

/// <summary>
/// Header strip with amber accent and gradient.
/// </summary>
internal sealed class StudioHeader : Panel
{
    public StudioHeader()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.HeaderBack;
        BorderStyle = BorderStyle.None;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        UiTheme.ThemeChanged += OnThemeChanged;
        Disposed += (_, _) => UiTheme.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged()
    {
        if (IsDisposed) return;
        BackColor = UiTheme.HeaderBack;
        Invalidate(true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        UiPaint.FillGutter(e.Graphics, ClientRectangle, UiTheme.HeaderBack);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiPaint.PaintHeaderBar(e.Graphics, ClientRectangle);
    }
}

/// <summary>
/// LIVE / CUSTOM pill with status dot (opaque; no transparent corners).
/// </summary>
internal sealed class LiveBadgeControl : Control
{
    private bool _live = true;

    public LiveBadgeControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        Size = new Size(78, 22);
        BackColor = UiTheme.HeaderBack;
        TabStop = false;

        UiTheme.ThemeChanged += OnThemeChanged;
        Disposed += (_, _) => UiTheme.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged()
    {
        if (IsDisposed) return;
        BackColor = UiTheme.HeaderBack;
        Invalidate();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool IsLive
    {
        get => _live;
        set
        {
            if (_live == value) return;
            _live = value;
            Invalidate();
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        UiPaint.FillGutter(e.Graphics, ClientRectangle, UiTheme.HeaderBack);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiPaint.PaintLivePill(e.Graphics, ClientRectangle, _live, UiTheme.HeaderBack);
    }
}

/// <summary>
/// Clock wall tile frame; gutter matches wall so no black corner marks.
/// </summary>
internal sealed class ClockTilePanel : Panel
{
    public ClockTilePanel()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.WallBack;
        BorderStyle = BorderStyle.None;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        UiTheme.ThemeChanged += OnThemeChanged;
        Disposed += (_, _) => UiTheme.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged()
    {
        if (IsDisposed) return;
        BackColor = UiTheme.WallBack;
        Invalidate(true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        UiPaint.FillGutter(e.Graphics, ClientRectangle, UiTheme.WallBack);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        UiPaint.PaintClockTile(e.Graphics, ClientRectangle, UiTheme.WallBack);
    }
}
