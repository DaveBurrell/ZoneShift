using System.ComponentModel;

namespace TimezoneConverter;

/// <summary>
/// Two-option pill toggle (e.g. 12h | 24h).
/// </summary>
internal sealed class SegmentedToggle : Panel
{
    private readonly Button _left;
    private readonly Button _right;
    private bool _rightSelected;
    private bool _ready;

    public event EventHandler? SelectionChanged;

    public SegmentedToggle(string leftText, string rightText)
    {
        DoubleBuffered = true;
        BackColor = UiTheme.CardFace;
        BorderStyle = BorderStyle.None;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        _left = MakeSegment(leftText);
        _right = MakeSegment(rightText);
        _left.Click += (_, _) => SetRightSelected(false);
        _right.Click += (_, _) => SetRightSelected(true);

        Controls.Add(_left);
        Controls.Add(_right);

        Size = new Size(200, 34);
        _ready = true;
        LayoutSegments();
        ApplyVisuals();

        UiTheme.ThemeChanged += OnThemeChanged;
        Disposed += (_, _) => UiTheme.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged()
    {
        if (IsDisposed) return;
        BackColor = UiTheme.CardFace;
        ApplyVisuals();
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        UiPaint.FillGutter(e.Graphics, ClientRectangle, BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var r = ClientRectangle;
        UiPaint.FillGutter(g, r, BackColor);
        UiPaint.EnableQuality(g);
        using (var bg = new SolidBrush(UiTheme.SegmentIdle))
            UiPaint.FillRoundRect(g, bg, r, 6);
        using (var edge = new Pen(UiTheme.CardBorder, 1f))
            UiPaint.DrawRoundRect(g, edge, r, 6);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool RightSelected
    {
        get => _rightSelected;
        set
        {
            if (_rightSelected == value)
                return;
            _rightSelected = value;
            ApplyVisuals();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool LeftSelected
    {
        get => !_rightSelected;
        set => SetRightSelected(!value);
    }

    private void SetRightSelected(bool right)
    {
        if (_rightSelected == right)
            return;
        _rightSelected = right;
        ApplyVisuals();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private Button MakeSegment(string text)
    {
        var btn = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8.5f),
            Cursor = Cursors.Hand,
            TabStop = false,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(2, 0, 2, 0),
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.BorderColor = UiTheme.SegmentIdle;
        return btn;
    }

    private void LayoutSegments()
    {
        if (!_ready || Width < 8 || Height < 8)
            return;

        var half = Width / 2;
        _left.SetBounds(3, 3, half - 5, Height - 6);
        _right.SetBounds(half + 1, 3, half - 5, Height - 6);
        _left.FlatAppearance.BorderSize = 0;
        _right.FlatAppearance.BorderSize = 0;
    }

    private void ApplyVisuals()
    {
        if (!_ready)
            return;

        Style(_left, !_rightSelected);
        Style(_right, _rightSelected);
    }

    private static void Style(Button btn, bool active)
    {
        var back = active ? UiTheme.SegmentActive : UiTheme.SegmentIdle;
        btn.BackColor = back;
        btn.ForeColor = active ? UiTheme.SegmentActiveText : UiTheme.SegmentIdleText;
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.BorderColor = back;
        btn.FlatAppearance.MouseOverBackColor = active
            ? UiTheme.AccentHover
            : UiTheme.SegmentIdleHover;
        btn.FlatAppearance.MouseDownBackColor = active
            ? UiTheme.AccentPressed
            : UiTheme.SegmentIdle;
        btn.UseVisualStyleBackColor = false;
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        LayoutSegments();
    }
}
