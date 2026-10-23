using System.ComponentModel;

namespace TimezoneConverter;

/// <summary>
/// Themed single-line text field with a drop button and a <see cref="PopupList"/>.
/// <para>
/// Replaces editable <see cref="ComboBox"/> controls, whose native edit field ignores the
/// palette and whose list cannot be filtered while the user types without glitching (see
/// <see cref="PopupList"/>). Subclasses decide what the list holds and what a commit means.
/// </para>
/// </summary>
internal abstract class DropDownField : Control
{
    private const int ButtonWidth = 24;
    private const int TextInsetX = 7;

    private readonly PopupList _popup = new();
    private readonly OutsideClickFilter _outsideClicks;
    private Form? _hookedForm;
    private bool _settingText;
    private bool _buttonHot;
    private bool _selectAllOnMouseUp;

    protected DropDownField()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        // The editor takes focus, never the frame around it.
        SetStyle(ControlStyles.Selectable, false);

        Editor = new TextBox { BorderStyle = BorderStyle.None, TabStop = true };
        Editor.TextChanged += (_, _) =>
        {
            if (!_settingText)
                OnEditorTextEdited();
        };
        Editor.KeyDown += (s, e) =>
        {
            _selectAllOnMouseUp = false;
            OnEditorKeyDown(s, e);
        };
        Editor.GotFocus += (_, _) =>
        {
            // Focusing selects the whole value so typing replaces it. A click that gave focus then
            // places the caret, collapsing that selection, so the first mouse-up restores it
            // (unless the user dragged out a selection of their own).
            Editor.SelectAll();
            _selectAllOnMouseUp = true;
            Invalidate();
        };
        Editor.LostFocus += (_, _) =>
        {
            ShowTextStart();
            Invalidate();
        };
        Editor.MouseUp += (_, _) =>
        {
            if ((_selectAllOnMouseUp || SelectAllOnClick) && Editor.SelectionLength == 0)
                Editor.SelectAll();
            _selectAllOnMouseUp = false;
        };
        Editor.MouseWheel += (_, e) =>
        {
            if (!_popup.Visible && Editor.Focused)
                OnEditorWheel(Math.Sign(e.Delta));
        };
        Controls.Add(Editor);

        _outsideClicks = new OutsideClickFilter(this);
        _popup.ItemChosen += (_, index) => OnPopupItemChosen(index);
        _popup.HighlightMoved += (_, _) => AnnounceHighlight();
        AccessibleRole = AccessibleRole.ComboBox;

        // After Editor exists: assigning Size raises OnResize, which lays it out.
        Size = new Size(148, 26);
        ApplyPalette();

        UiTheme.ThemeChanged += OnThemeChanged;
    }

    protected internal TextBox Editor { get; }

    protected internal PopupList Popup => _popup;

    public bool IsDroppedDown => _popup.Visible;

    /// <summary>
    /// What screen readers call the field (its caption). The list rows are painted, so the
    /// editor also announces the highlighted row as it moves.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string? FieldLabel
    {
        get => Editor.AccessibleName;
        set
        {
            AccessibleName = value;
            Editor.AccessibleName = value;
        }
    }

    public override string Text
    {
        get => Editor?.Text ?? "";
#pragma warning disable CS8765 // Matches Control.Text, which accepts null.
        set => SetEditorText(value ?? "");
#pragma warning restore CS8765
    }

    /// <summary>Whether a click inside an already-focused editor selects all its text.</summary>
    protected virtual bool SelectAllOnClick => false;

    /// <summary>Called after the user edits the text (never for programmatic changes).</summary>
    protected virtual void OnEditorTextEdited() { }

    /// <summary>Handles a key in the editor. Return true to consume it.</summary>
    protected virtual bool OnEditorKey(KeyEventArgs e) => false;

    /// <summary>One wheel notch over the focused editor while the list is closed (+1 is up).</summary>
    protected virtual void OnEditorWheel(int direction) { }

    protected abstract void OnDropButtonClicked();

    protected abstract void OnPopupItemChosen(int index);

    /// <summary>Focus has left the field. Runs before the list closes, so it can read the highlight.</summary>
    protected virtual void OnFieldLeave() { }

    /// <summary>Replaces the editor text without raising <see cref="OnEditorTextEdited"/>.</summary>
    protected void SetEditorText(string text)
    {
        if (Editor.Text == text)
            return;

        // A live clock rewrites this text every second; keep a whole-field selection intact.
        var allSelected = Editor.Focused && Editor.TextLength > 0 && Editor.SelectionLength == Editor.TextLength;
        _settingText = true;
        try
        {
            Editor.Text = text;
            if (allSelected)
                Editor.SelectAll();
            else if (Editor.Focused)
                Editor.Select(text.Length, 0);
            else
                ShowTextStart();
        }
        finally
        {
            _settingText = false;
        }
    }

    /// <summary>
    /// A narrow field scrolls to keep the caret visible. Parking it at the start keeps a long
    /// zone name reading "GMT - Greenwich…" rather than "…eenwich Mean Time (London)".
    /// </summary>
    private void ShowTextStart() => Editor.Select(0, 0);

    protected void ShowPopup()
    {
        if (IsDisposed || Disposing || !Enabled || !Visible || !IsHandleCreated)
            return;

        var opening = !_popup.Visible;
        if (opening)
            HookCloseTriggers();
        _popup.ShowFor(this);
        if (opening)
            AnnounceHighlight();
        Invalidate();
    }

    protected void HidePopup()
    {
        UnhookCloseTriggers();
        if (_popup.Visible)
            _popup.Hide();
        Invalidate();
    }

    private void HookCloseTriggers()
    {
        Application.AddMessageFilter(_outsideClicks);
        _hookedForm = FindForm();
        if (_hookedForm is not null)
        {
            _hookedForm.Deactivate += OnHostFormChanged;
            _hookedForm.Move += OnHostFormChanged;
            _hookedForm.Resize += OnHostFormChanged;
        }
    }

    private void UnhookCloseTriggers()
    {
        Application.RemoveMessageFilter(_outsideClicks);
        if (_hookedForm is not null)
        {
            _hookedForm.Deactivate -= OnHostFormChanged;
            _hookedForm.Move -= OnHostFormChanged;
            _hookedForm.Resize -= OnHostFormChanged;
            _hookedForm = null;
        }
    }

    private void OnHostFormChanged(object? sender, EventArgs e) => HidePopup();

    private void AnnounceHighlight()
    {
        if (!_popup.Visible || !Editor.Focused || _popup.HighlightText is not { } text)
            return;
        Editor.AccessibilityObject.RaiseAutomationNotification(
            System.Windows.Forms.Automation.AutomationNotificationKind.Other,
            System.Windows.Forms.Automation.AutomationNotificationProcessing.ImportantMostRecent,
            text);
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        // Plain F4 or Alt+Up/Down toggles the list, as in a combo box. Alt+F4 must still close the window.
        var handled = (e.KeyCode == Keys.F4 && e.Modifiers == Keys.None) ||
                      (e.Modifiers == Keys.Alt && e.KeyCode is Keys.Down or Keys.Up);
        if (handled)
        {
            if (_popup.Visible)
                HidePopup();
            else
                OnDropButtonClicked();
        }
        else
        {
            handled = OnEditorKey(e);
        }

        if (handled)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    protected override void OnLeave(EventArgs e)
    {
        OnFieldLeave();
        HidePopup();
        base.OnLeave(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        if (!Enabled)
            HidePopup();
        Editor.Enabled = Enabled;
        ApplyPalette();
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        if (!Visible)
            HidePopup();
        base.OnVisibleChanged(e);
    }

    protected override void OnParentChanged(EventArgs e)
    {
        HidePopup();
        base.OnParentChanged(e);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        HidePopup();
        base.OnHandleDestroyed(e);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        if (Editor is not null)
            Editor.Font = Font;
        LayoutEditor();
        base.OnFontChanged(e);
    }

    protected override void OnResize(EventArgs e)
    {
        LayoutEditor();
        base.OnResize(e);
    }

    private void LayoutEditor()
    {
        // Base-class virtuals (OnResize, OnFontChanged) can fire before the editor is assigned.
        if (Editor is null)
            return;

        var height = Math.Max(1, Editor.PreferredHeight);
        Editor.SetBounds(
            TextInsetX,
            Math.Max(1, (Height - height) / 2),
            Math.Max(1, Width - ButtonWidth - TextInsetX - 4),
            height);
    }

    private void OnThemeChanged()
    {
        if (IsDisposed)
            return;
        ApplyPalette();
        _popup.Invalidate();
        Invalidate();
    }

    private void ApplyPalette()
    {
        var fill = UiPaint.FieldFill(Enabled);
        BackColor = fill;
        Editor.BackColor = fill;
        Editor.ForeColor = Enabled ? UiTheme.TextPrimary : UiTheme.TextMuted;
        Editor.Font = Font;
    }

    private Rectangle ButtonBounds => new(Width - ButtonWidth - 1, 1, ButtonWidth, Math.Max(1, Height - 2));

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Enabled || e.Button != MouseButtons.Left)
            return;

        Editor.Focus();
        // The matching mouse-up arrives here, not at the editor; selection is decided below.
        _selectAllOnMouseUp = false;
        if (ButtonBounds.Contains(e.Location))
        {
            if (_popup.Visible)
                HidePopup();
            else
                OnDropButtonClicked();
        }
        else
        {
            Editor.SelectAll();
        }
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

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Painted entirely in OnPaint.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var button = ButtonBounds;
        var focused = Editor.Focused || _popup.Visible;
        UiPaint.PaintFieldChrome(e.Graphics, ClientSize, button, Enabled, focused, _buttonHot);
        UiPaint.DrawChevronDown(e.Graphics, button, Enabled ? UiTheme.Accent : UiTheme.TextMuted);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UiTheme.ThemeChanged -= OnThemeChanged;
            UnhookCloseTriggers();
            _popup.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Closes the list when the user presses a mouse button anywhere outside the field and list
    /// (focus does not move when they click a label or empty card), and routes the mouse wheel to
    /// the list wherever the pointer is, as a native drop-down does.
    /// </summary>
    private sealed class OutsideClickFilter(DropDownField owner) : IMessageFilter
    {
        private const int WmLButtonDown = 0x0201;
        private const int WmRButtonDown = 0x0204;
        private const int WmMButtonDown = 0x0207;
        private const int WmNcLButtonDown = 0x00A1;
        private const int WmNcRButtonDown = 0x00A4;
        private const int WmMouseWheel = 0x020A;

        public bool PreFilterMessage(ref Message m)
        {
            switch (m.Msg)
            {
                case WmMouseWheel when owner._popup.Visible:
                    var delta = (short)((m.WParam.ToInt64() >> 16) & 0xffff);
                    owner._popup.ScrollBy(-Math.Sign(delta) * 3);
                    return true;

                case WmLButtonDown or WmRButtonDown or WmMButtonDown or WmNcLButtonDown or WmNcRButtonDown:
                    var target = Control.FromChildHandle(m.HWnd);
                    var inside = target is not null &&
                                 (ReferenceEquals(target, owner._popup) || ReferenceEquals(target, owner) ||
                                  owner.Contains(target));
                    if (!inside)
                        owner.HidePopup();
                    return false;

                default:
                    return false;
            }
        }
    }
}
