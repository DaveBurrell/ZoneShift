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
        BackColor = UiTheme.SegmentIdle;
        BorderStyle = BorderStyle.FixedSingle;

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

    private Button MakeSegment(string text) =>
        new()
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8.5f),
            Cursor = Cursors.Hand,
            TabStop = false,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(2, 0, 2, 0)
        };

    private void LayoutSegments()
    {
        if (!_ready || Width < 8 || Height < 8)
            return;

        var half = Width / 2;
        _left.SetBounds(1, 1, half - 2, Height - 2);
        _right.SetBounds(half, 1, half - 2, Height - 2);
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
        btn.BackColor = active ? UiTheme.SegmentActive : UiTheme.SegmentIdle;
        btn.ForeColor = active ? Color.White : UiTheme.TextSecondary;
        btn.FlatAppearance.MouseOverBackColor = active
            ? Color.FromArgb(67, 56, 202)
            : Color.FromArgb(226, 232, 240);
        btn.FlatAppearance.MouseDownBackColor = active
            ? Color.FromArgb(55, 48, 163)
            : Color.FromArgb(203, 213, 225);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        LayoutSegments();
    }
}
