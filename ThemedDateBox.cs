using System.ComponentModel;
using System.Globalization;

namespace TimezoneConverter;

/// <summary>
/// Date field painted from the palette, with a calendar drop-down.
/// <para>
/// Replaces <see cref="DateTimePicker"/>, a native common control with no dark mode: it ignores
/// <see cref="Control.BackColor"/>, is unaffected by <c>Application.SetColorMode</c>, and does not
/// respond to <c>SetWindowTheme</c>. It rendered as a white box on the dark themes.
/// </para>
/// <para>
/// Text remains typeable. Unparseable input reverts to the last accepted value on commit, so the
/// control can never hold a date the rest of the app cannot read.
/// </para>
/// </summary>
internal sealed class ThemedDateBox : Control
{
    private const int ButtonWidth = 24;
    private const int TextInsetX = 7;

    private readonly TextBox _text;
    private DateTime _value = DateTime.Today;
    private bool _syncingText;
    private bool _buttonHot;
    private MonthCalendar? _calendar;
    private ToolStripDropDown? _calendarDropDown;
    // Set when a click on the button itself closes the calendar, so that click does not reopen it.
    private bool _swallowNextButtonDown;

    private static DateTime MinimumDate => new(Math.Max(
        DateTimePicker.MinimumDateTime.Date.Ticks,
        CultureInfo.CurrentCulture.DateTimeFormat.Calendar.MinSupportedDateTime.Date.Ticks));
    private static DateTime MaximumDate => new(Math.Min(
        DateTimePicker.MaximumDateTime.Date.Ticks,
        CultureInfo.CurrentCulture.DateTimeFormat.Calendar.MaxSupportedDateTime.Date.Ticks));

    public event EventHandler? ValueChanged;

    /// <summary>True while the text box holds typing that differs from the committed date.</summary>
    [Browsable(false)]
    public bool IsEditing => _text.Text != _value.ToString("d", CultureInfo.CurrentCulture);

    /// <summary>What screen readers call the field (its caption).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string? FieldLabel
    {
        get => _text.AccessibleName;
        set
        {
            AccessibleName = value;
            _text.AccessibleName = value;
        }
    }

    public ThemedDateBox()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        _text = new TextBox
        {
            BorderStyle = BorderStyle.None,
            TabStop = true
        };
        _text.KeyDown += OnTextKeyDown;
        _text.Leave += (_, _) => CommitText();
        _text.GotFocus += (_, _) => Invalidate();
        _text.LostFocus += (_, _) => Invalidate();
        Controls.Add(_text);

        // After _text exists: assigning Size raises OnResize, which lays the child out.
        Size = new Size(148, 26);

        SyncTextFromValue();
        ApplyPalette();

        UiTheme.ThemeChanged += OnThemeChanged;
        Disposed += (_, _) => UiTheme.ThemeChanged -= OnThemeChanged;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public DateTime Value
    {
        get => _value;
        set
        {
            var next = ClampDate(value.Date);
            if (_value.Date == next)
                return;

            _value = next;
            SyncTextFromValue();
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnThemeChanged()
    {
        if (IsDisposed) return;
        ApplyPalette();
        Invalidate();
    }

    private Color FieldFill => UiPaint.FieldFill(Enabled);

    private void ApplyPalette()
    {
        BackColor = FieldFill;
        _text.BackColor = FieldFill;
        _text.ForeColor = Enabled ? UiTheme.TextPrimary : UiTheme.TextMuted;
        _text.Font = Font;
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        if (!Enabled)
            _calendarDropDown?.Close();
        _text.Enabled = Enabled;
        ApplyPalette();
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        if (_text is not null)
            _text.Font = Font;

        LayoutChildren();
        base.OnFontChanged(e);
    }

    protected override void OnResize(EventArgs e)
    {
        LayoutChildren();
        base.OnResize(e);
    }

    private void LayoutChildren()
    {
        // Base-class virtuals (OnResize, OnFontChanged) can fire before the field is assigned.
        if (_text is null)
            return;

        var textHeight = Math.Max(1, _text.PreferredHeight);
        _text.SetBounds(
            TextInsetX,
            Math.Max(1, (Height - textHeight) / 2),
            Math.Max(1, Width - ButtonWidth - TextInsetX - 4),
            textHeight);
    }

    private void SyncTextFromValue()
    {
        _syncingText = true;
        try
        {
            _text.Text = _value.ToString("d", CultureInfo.CurrentCulture);
        }
        finally
        {
            _syncingText = false;
        }
    }

    /// <summary>Accepts typed text, or restores the last good value when it cannot be parsed.</summary>
    private void CommitText()
    {
        if (_syncingText)
            return;

        if (DateTime.TryParse(
                _text.Text,
                CultureInfo.CurrentCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            Value = parsed.Date;
            SyncTextFromValue();
        }
        else
        {
            SyncTextFromValue();
        }
    }

    private void OnTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F4 || (e.Alt && e.KeyCode == Keys.Down))
        {
            ShowCalendar();
            e.SuppressKeyPress = true;
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Enter:
                CommitText();
                e.SuppressKeyPress = true;
                break;
            case Keys.Escape:
                SyncTextFromValue();
                e.SuppressKeyPress = true;
                break;
            case Keys.Up:
                Step(1);
                e.SuppressKeyPress = true;
                break;
            case Keys.Down:
                Step(-1);
                e.SuppressKeyPress = true;
                break;
            case Keys.PageUp:
                StepMonths(1);
                e.SuppressKeyPress = true;
                break;
            case Keys.PageDown:
                StepMonths(-1);
                e.SuppressKeyPress = true;
                break;
        }
    }

    private void Step(int days)
    {
        var target = _value.Date;
        if (days > 0 && target > MaximumDate.AddDays(-days)) { Value = MaximumDate; return; }
        if (days < 0 && target < MinimumDate.AddDays(-days)) { Value = MinimumDate; return; }
        Value = target.AddDays(days);
    }

    private void StepMonths(int months)
    {
        var target = _value.Date;
        if (months > 0 && target > MaximumDate.AddMonths(-months)) { Value = MaximumDate; return; }
        if (months < 0 && target < MinimumDate.AddMonths(-months)) { Value = MinimumDate; return; }
        Value = target.AddMonths(months);
    }

    private static DateTime ClampDate(DateTime value) =>
        value < MinimumDate ? MinimumDate : value > MaximumDate ? MaximumDate : value;

    private Rectangle ButtonBounds =>
        new(Width - ButtonWidth - 1, 1, ButtonWidth, Math.Max(1, Height - 2));

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (Enabled && ButtonBounds.Contains(e.Location))
        {
            if (_swallowNextButtonDown)
                _swallowNextButtonDown = false;
            else
                ShowCalendar();
        }
        else
        {
            _text.Focus();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var hot = ButtonBounds.Contains(e.Location);
        if (hot != _buttonHot)
        {
            _buttonHot = hot;
            Invalidate(ButtonBounds);
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (_buttonHot)
        {
            _buttonHot = false;
            Invalidate(ButtonBounds);
        }

        base.OnMouseLeave(e);
    }

    /// <summary>
    /// The drop-down hosts a native <see cref="MonthCalendar"/>. It is transient and follows the
    /// system theme, exactly as the old picker's calendar did.
    /// </summary>
    private void ShowCalendar()
    {
        if (!Enabled || IsDisposed || Disposing)
            return;

        CommitText();
        if (!Enabled || IsDisposed || Disposing)
            return;
        if (_calendarDropDown is null)
        {
            _calendar = new MonthCalendar
            {
                MaxSelectionCount = 1,
                MinDate = MinimumDate,
                MaxDate = MaximumDate
            };
            var host = new ToolStripControlHost(_calendar) { Margin = Padding.Empty, Padding = Padding.Empty };
            _calendarDropDown = new ToolStripDropDown
            {
                Padding = Padding.Empty,
                AutoClose = true,
                DropShadowEnabled = true
            };
            _calendarDropDown.Items.Add(host);
            _calendarDropDown.Closed += (_, args) =>
            {
                // AutoClose shuts the calendar on mouse-down before this control sees the same
                // click. Without this, clicking the button to close the calendar reopened it.
                _swallowNextButtonDown = args.CloseReason == ToolStripDropDownCloseReason.AppClicked &&
                                         ButtonBounds.Contains(PointToClient(Cursor.Position));
                // That click is dispatched straight after this handler. If it never reaches the
                // button, the flag must not swallow a later, genuine click.
                if (_swallowNextButtonDown && IsHandleCreated)
                    BeginInvoke(() => _swallowNextButtonDown = false);
                Invalidate();
            };
            _calendar.KeyDown += (_, args) =>
            {
                if (args.KeyCode == Keys.Escape)
                    _calendarDropDown.Close();
            };
            _calendar.DateSelected += (_, args) =>
            {
                // Keep the native calendar alive until its owner is disposed. Destroying it
                // inside DateSelected/Closed interrupts the notification still being handled.
                _calendarDropDown.Close();
                Value = args.Start.Date;
            };
        }

        _swallowNextButtonDown = false;
        _calendar!.SetDate(_value);
        _calendarDropDown.Show(this, new Point(0, Height));
        _calendar.Focus();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _calendarDropDown?.Dispose();
            _calendarDropDown = null;
            _calendar = null;
        }
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var button = ButtonBounds;
        var focused = _text.Focused || _calendarDropDown is { Visible: true };
        UiPaint.PaintFieldChrome(g, ClientSize, button, Enabled, focused, _buttonHot);
        DrawCalendarGlyph(g, button, Enabled ? UiTheme.Accent : UiTheme.TextMuted);
    }

    private static void DrawCalendarGlyph(Graphics g, Rectangle area, Color color)
    {
        var w = 12;
        var h = 11;
        var rect = new Rectangle(area.Left + (area.Width - w) / 2, area.Top + (area.Height - h) / 2, w, h);

        using var pen = new Pen(color, 1f);
        g.DrawRectangle(pen, rect);
        g.DrawLine(pen, rect.Left, rect.Top + 3, rect.Right, rect.Top + 3);
        g.DrawLine(pen, rect.Left + 3, rect.Top - 2, rect.Left + 3, rect.Top);
        g.DrawLine(pen, rect.Right - 3, rect.Top - 2, rect.Right - 3, rect.Top);

        using var dot = new SolidBrush(color);
        for (var row = 0; row < 2; row++)
        for (var col = 0; col < 3; col++)
            g.FillRectangle(dot, rect.Left + 2 + col * 4, rect.Top + 6 + row * 3, 2, 2);
    }
}
