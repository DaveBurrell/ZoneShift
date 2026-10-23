using System.ComponentModel;

namespace TimezoneConverter;

/// <summary>One row of a <see cref="PopupList"/>: emphasised lead text, optional detail, optional star.</summary>
internal readonly record struct PopupItem(string Lead, string? Detail = null, bool Starred = false);

/// <summary>
/// Suggestion list shown under a <see cref="DropDownField"/>.
/// <para>
/// A native combo box owns its list: it sizes it only when it opens, autocompletes the edit text
/// when it drops, and moves its own selection while items change underneath it. Filtering one in
/// place left an oversized popup, swallowed typing and reset the caret. This window belongs to
/// the app, so it can be refiltered and resized on every keystroke.
/// </para>
/// <para>
/// It never activates or takes focus. The field's editor keeps the caret while the user types,
/// scrolls or clicks a row.
/// </para>
/// </summary>
internal sealed class PopupList : Form
{
    private const int WmMouseActivate = 0x0021;
    private const int MaNoActivate = 3;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private const int CsDropShadow = 0x00020000;
    private const int ScrollBarWidth = 10;
    private const int TextPadX = 8;
    private const int MinThumbHeight = 18;

    private static readonly Font LeadFont = new("Segoe UI Semibold", 9f);

    private IReadOnlyList<PopupItem> _items = [];
    private Control? _anchor;
    // Rows that fit between the field and the screen edge, set when the list is placed.
    private int _rowLimit = int.MaxValue;
    private int _highlight = -1;
    private int _top;
    private int _pressedRow = -1;
    private bool _draggingThumb;
    private int _thumbGrabOffset;

    public PopupList()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        Font = UiTheme.BodyFont;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    /// <summary>Raised with the row index when the user clicks a row.</summary>
    public event EventHandler<int>? ItemChosen;

    /// <summary>
    /// Raised when the highlight moves by keyboard or because the rows changed (not on hover),
    /// so the field can announce the row to screen readers.
    /// </summary>
    public event EventHandler? HighlightMoved;

    /// <summary>The highlighted row as one line of text, or the empty-list message.</summary>
    public string? HighlightText => _highlight >= 0 && _highlight < _items.Count
        ? string.IsNullOrEmpty(_items[_highlight].Detail)
            ? _items[_highlight].Lead
            : $"{_items[_highlight].Lead} {_items[_highlight].Detail}"
        : _items.Count == 0 ? EmptyText : null;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int RowHeight { get; set; } = 24;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int MaxVisibleRows { get; set; } = 12;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string EmptyText { get; set; } = "No matches";

    public int Count => _items.Count;
    public int TopIndex => _top;
    public int VisibleRows => Math.Clamp(_items.Count, 1, Math.Max(1, Math.Min(MaxVisibleRows, _rowLimit)));
    public int PageSize => Math.Max(1, VisibleRows - 1);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int HighlightIndex
    {
        get => _highlight;
        set
        {
            var next = _items.Count == 0 ? -1 : Math.Clamp(value, -1, _items.Count - 1);
            if (next == _highlight)
                return;
            _highlight = next;
            EnsureVisible(next);
            Invalidate();
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WsExNoActivate | WsExToolWindow;
            cp.ClassStyle |= CsDropShadow;
            return cp;
        }
    }

    protected override void WndProc(ref Message m)
    {
        // Clicking a row must not pull activation (and with it, focus) away from the editor.
        if (m.Msg == WmMouseActivate)
        {
            m.Result = MaNoActivate;
            return;
        }

        base.WndProc(ref m);
    }

    /// <summary>Replaces the rows. With <paramref name="center"/>, scrolls the highlight to mid-list.</summary>
    public void SetItems(IReadOnlyList<PopupItem> items, int highlight, bool center = false)
    {
        _items = items;
        _pressedRow = -1;
        _draggingThumb = false;
        _top = 0;
        _highlight = items.Count == 0 ? -1 : Math.Clamp(highlight, -1, items.Count - 1);
        if (center && _highlight >= 0)
            _top = ClampTop(_highlight - VisibleRows / 2);
        else
            EnsureVisible(_highlight);

        if (Visible)
            ResizeToFit();
        Invalidate();
        HighlightMoved?.Invoke(this, EventArgs.Empty);
    }

    public void MoveHighlight(int delta)
    {
        if (_items.Count == 0)
            return;
        var before = _highlight;
        HighlightIndex = _highlight < 0
            ? (delta > 0 ? 0 : _items.Count - 1)
            : Math.Clamp(_highlight + delta, 0, _items.Count - 1);
        if (_highlight != before)
            HighlightMoved?.Invoke(this, EventArgs.Empty);
    }

    public void ScrollBy(int rows)
    {
        var next = ClampTop(_top + rows);
        if (next == _top)
            return;
        _top = next;
        Invalidate();
    }

    /// <summary>Shows the list under <paramref name="anchor"/>, or above it when the screen runs out.</summary>
    public void ShowFor(Control anchor)
    {
        _anchor = anchor;
        ResizeToFit();
        if (!Visible)
        {
            var owner = anchor.FindForm();
            if (owner is not null && !ReferenceEquals(owner, this))
                Show(owner);
            else
                Show();
        }
    }

    private void ResizeToFit()
    {
        if (_anchor is null || _anchor.IsDisposed)
            return;

        var field = _anchor.RectangleToScreen(_anchor.ClientRectangle);
        var area = Screen.FromRectangle(field).WorkingArea;

        // Below the field when the list fits there; otherwise on whichever side has more room,
        // trimmed to that room so it never runs off the screen.
        var below = area.Bottom - field.Bottom - 1;
        var above = field.Top - 1 - area.Top;
        var wanted = Math.Clamp(_items.Count, 1, MaxVisibleRows) * RowHeight + 2;
        var placeAbove = wanted > below && above > below;
        _rowLimit = Math.Max(1, ((placeAbove ? above : below) - 2) / RowHeight);
        _top = ClampTop(_top);
        EnsureVisible(_highlight);

        var size = new Size(Math.Max(field.Width, 120), VisibleRows * RowHeight + 2);
        var y = placeAbove ? field.Top - size.Height - 1 : field.Bottom + 1;
        var x = Math.Clamp(field.Left, area.Left, Math.Max(area.Left, area.Right - size.Width));

        Bounds = new Rectangle(new Point(x, y), size);
    }

    private int ClampTop(int top) => Math.Clamp(top, 0, Math.Max(0, _items.Count - VisibleRows));

    private void EnsureVisible(int index)
    {
        if (index < 0)
            return;
        if (index < _top)
            _top = index;
        else if (index >= _top + VisibleRows)
            _top = index - VisibleRows + 1;
        _top = ClampTop(_top);
    }

    private bool HasScrollBar => _items.Count > VisibleRows;

    private Rectangle TrackBounds =>
        new(ClientSize.Width - 1 - ScrollBarWidth, 1, ScrollBarWidth, Math.Max(1, ClientSize.Height - 2));

    private Rectangle ThumbBounds
    {
        get
        {
            var track = TrackBounds;
            var height = Math.Max(MinThumbHeight, track.Height * VisibleRows / Math.Max(1, _items.Count));
            var range = Math.Max(1, _items.Count - VisibleRows);
            var y = track.Top + (track.Height - height) * _top / range;
            return new Rectangle(track.Left + 2, y, track.Width - 4, height);
        }
    }

    private int RowAt(Point point)
    {
        if (point.Y < 1 || (HasScrollBar && point.X >= TrackBounds.Left))
            return -1;
        var index = _top + (point.Y - 1) / RowHeight;
        return index < _items.Count && index < _top + VisibleRows ? index : -1;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;

        if (HasScrollBar && e.X >= TrackBounds.Left)
        {
            var thumb = ThumbBounds;
            if (thumb.Contains(e.Location))
            {
                _draggingThumb = true;
                _thumbGrabOffset = e.Y - thumb.Top;
                Capture = true;
            }
            else
            {
                ScrollBy(e.Y < thumb.Top ? -PageSize : PageSize);
            }
            return;
        }

        _pressedRow = RowAt(e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_draggingThumb)
        {
            var track = TrackBounds;
            var travel = Math.Max(1, track.Height - ThumbBounds.Height);
            var ratio = (double)(e.Y - _thumbGrabOffset - track.Top) / travel;
            var top = ClampTop((int)Math.Round(ratio * (_items.Count - VisibleRows)));
            if (top != _top)
            {
                _top = top;
                Invalidate();
            }
            return;
        }

        var row = RowAt(e.Location);
        if (row >= 0 && row != _highlight)
        {
            _highlight = row;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_draggingThumb)
        {
            _draggingThumb = false;
            Capture = false;
            return;
        }

        var row = RowAt(e.Location);
        var pressed = _pressedRow;
        _pressedRow = -1;
        if (e.Button == MouseButtons.Left && row >= 0 && row == pressed)
            ItemChosen?.Invoke(this, row);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        ScrollBy(-Math.Sign(e.Delta) * 3);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Painted entirely in OnPaint.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var client = ClientRectangle;
        using (var back = new SolidBrush(UiTheme.InputBack))
            g.FillRectangle(back, client);

        var textRight = client.Width - 1 - (HasScrollBar ? ScrollBarWidth : 0);
        if (_items.Count == 0)
        {
            TextRenderer.DrawText(g, EmptyText, Font,
                new Rectangle(1 + TextPadX, 1, textRight - TextPadX - 1, RowHeight),
                UiTheme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        for (var row = 0; row < VisibleRows && _top + row < _items.Count; row++)
        {
            var index = _top + row;
            var bounds = new Rectangle(1, 1 + row * RowHeight, textRight - 1, RowHeight);
            PaintRow(g, _items[index], bounds, index == _highlight);
        }

        if (HasScrollBar)
        {
            using var thumb = new SolidBrush(_draggingThumb ? UiTheme.Accent : UiTheme.InputBorder);
            UiPaint.FillRoundRect(g, thumb, ThumbBounds, 3);
        }

        using var border = new Pen(UiTheme.InputBorder);
        g.DrawRectangle(border, 0, 0, client.Width - 1, client.Height - 1);
    }

    private void PaintRow(Graphics g, PopupItem item, Rectangle bounds, bool highlighted)
    {
        if (highlighted)
        {
            using var fill = new SolidBrush(UiTheme.SegmentActive);
            g.FillRectangle(fill, bounds);
        }

        const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                                      TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding |
                                      TextFormatFlags.SingleLine;
        var lead = highlighted ? UiTheme.TextOnAccent : UiTheme.TextPrimary;
        var detail = highlighted ? UiTheme.TextOnAccent : UiTheme.TextSecondary;
        var x = bounds.Left + TextPadX;
        var right = bounds.Right - TextPadX;

        if (item.Starred)
        {
            const string star = "★ ";
            TextRenderer.DrawText(g, star, LeadFont, new Rectangle(x, bounds.Top, right - x, bounds.Height),
                highlighted ? UiTheme.TextOnAccent : UiTheme.Accent, flags);
            x += TextRenderer.MeasureText(g, star, LeadFont, Size.Empty, flags).Width;
        }

        var leadWidth = Math.Min(right - x,
            TextRenderer.MeasureText(g, item.Lead, LeadFont, Size.Empty, flags).Width);
        TextRenderer.DrawText(g, item.Lead, LeadFont, new Rectangle(x, bounds.Top, leadWidth, bounds.Height),
            lead, flags | TextFormatFlags.EndEllipsis);
        x += leadWidth + 10;

        if (!string.IsNullOrEmpty(item.Detail) && x < right)
        {
            TextRenderer.DrawText(g, item.Detail, Font, new Rectangle(x, bounds.Top, right - x, bounds.Height),
                detail, flags | TextFormatFlags.EndEllipsis);
        }
    }
}
