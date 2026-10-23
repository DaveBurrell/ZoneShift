using System.ComponentModel;
using TimezoneConverter.Services;

namespace TimezoneConverter;

/// <summary>
/// Editable time field: type any time, pick a 30-minute preset from the list, or step with the
/// arrow keys / wheel. Typed text commits on Enter or when focus leaves; unreadable text reverts.
/// </summary>
internal sealed class TimeEntryCombo : DropDownField
{
    private const int PresetMinutes = 30;
    private const int PresetCount = 24 * 60 / PresetMinutes;

    private TimeSpan _timeOfDay;
    private bool _use24Hour;
    private bool _includeSeconds;
    private bool _configured;

    public event EventHandler? TimeChanged;

    public TimeEntryCombo()
    {
        Popup.MaxVisibleRows = 12;
        Configure(use24Hour: false, includeSeconds: false);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public TimeSpan TimeOfDay
    {
        get => _timeOfDay;
        set => SetTimeInternal(value, raiseEvent: false);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool Use24Hour
    {
        get => _use24Hour;
        set => Configure(value, _includeSeconds);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool IncludeSeconds
    {
        get => _includeSeconds;
        set => Configure(_use24Hour, value);
    }

    /// <summary>True while the editor holds text that differs from the committed time.</summary>
    [Browsable(false)]
    public bool IsEditing => Editor.Text != FormatTime(_timeOfDay, _includeSeconds);

    public void Configure(bool use24Hour, bool includeSeconds)
    {
        if (_configured && _use24Hour == use24Hour && _includeSeconds == includeSeconds)
            return;

        // A half-typed time survives a format switch; it is re-read on commit.
        var keepTypedText = _configured && IsEditing;
        _configured = true;
        _use24Hour = use24Hour;
        _includeSeconds = includeSeconds;
        if (!keepTypedText)
            UpdateDisplayText();
        if (IsDroppedDown)
            ShowPresets();
    }

    /// <summary>The 30-minute presets in the current format.</summary>
    internal IReadOnlyList<string> Presets =>
        Enumerable.Range(0, PresetCount)
            .Select(i => FormatTime(TimeSpan.FromMinutes(i * PresetMinutes), includeSeconds: false))
            .ToArray();

    protected override bool OnEditorKey(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Enter:
                if (IsDroppedDown && Popup.HighlightIndex >= 0)
                    OnPopupItemChosen(Popup.HighlightIndex);
                else
                    CommitTypedText();
                return true;

            case Keys.Escape when IsDroppedDown:
                HidePopup();
                return true;

            case Keys.Escape when IsEditing:
                UpdateDisplayText();
                Editor.SelectAll();
                return true;

            case Keys.Up or Keys.Down:
                var direction = e.KeyCode == Keys.Up ? 1 : -1;
                if (IsDroppedDown)
                    Popup.MoveHighlight(-direction);
                else
                    Step(direction);
                return true;

            case Keys.PageUp or Keys.PageDown:
                var page = e.KeyCode == Keys.PageUp ? 1 : -1;
                if (IsDroppedDown)
                    Popup.MoveHighlight(-page * Popup.PageSize);
                else
                    StepHours(page);
                return true;

            default:
                return false;
        }
    }

    protected override void OnEditorWheel(int direction) => Step(direction);

    // Typing means "this time", not a preset: close the list so Enter commits the typed text.
    protected override void OnEditorTextEdited() => HidePopup();

    protected override void OnDropButtonClicked()
    {
        CommitTypedText();
        Editor.SelectAll();
        ShowPresets();
    }

    protected override void OnPopupItemChosen(int index)
    {
        if (index < 0 || index >= PresetCount)
            return;

        HidePopup();
        SetTimeInternal(TimeSpan.FromMinutes(index * PresetMinutes), raiseEvent: true, forceText: true);
        if (Editor.Focused)
            Editor.SelectAll();
    }

    protected override void OnFieldLeave() => CommitTypedText();

    private void ShowPresets()
    {
        var nearest = (int)(_timeOfDay.TotalMinutes / PresetMinutes);
        Popup.SetItems(Presets.Select(p => new PopupItem(p)).ToArray(), nearest, center: true);
        ShowPopup();
    }

    /// <summary>Moves to the next (or previous) half hour: 7:45 steps to 8:00 or 7:30.</summary>
    private void Step(int direction)
    {
        CommitTypedText();
        var slot = _timeOfDay.TotalMinutes / PresetMinutes;
        var next = direction > 0 ? Math.Floor(slot) + 1 : Math.Ceiling(slot) - 1;
        SetTimeInternal(TimeSpan.FromMinutes(next * PresetMinutes), raiseEvent: true, forceText: true);
        Editor.SelectAll();
    }

    private void StepHours(int hours)
    {
        CommitTypedText();
        var minutes = Math.Floor(_timeOfDay.TotalMinutes);
        SetTimeInternal(TimeSpan.FromMinutes(minutes + hours * 60), raiseEvent: true, forceText: true);
        Editor.SelectAll();
    }

    private void CommitTypedText()
    {
        if (!IsEditing)
            return;

        if (TryParse(Editor.Text, out var t))
            SetTimeInternal(t, raiseEvent: true, forceText: true);
        else
            UpdateDisplayText(); // Revert unreadable input to the last good time.
    }

    private void SetTimeInternal(TimeSpan time, bool raiseEvent, bool forceText = false)
    {
        time = TimeParser.Normalize(time);
        var changed = time != _timeOfDay;
        // Programmatic updates (the live clock) never overwrite a time the user is still typing,
        // including while the window is inactive and the editor has lost real focus.
        var keepTypedText = !forceText && IsEditing;
        _timeOfDay = time;

        if (!keepTypedText)
            UpdateDisplayText();

        if (changed && raiseEvent)
            TimeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateDisplayText() => SetEditorText(FormatTime(_timeOfDay, _includeSeconds));

    private string FormatTime(TimeSpan time, bool includeSeconds) =>
        TimeParser.Format(time, _use24Hour, includeSeconds);

    /// <summary>
    /// Accepts common inputs: 7am, 7:00 AM, 19:00, 7.30pm, 0730, etc.
    /// </summary>
    public static bool TryParse(string? text, out TimeSpan time) =>
        TimeParser.TryParse(text, out time);
}
