using System.ComponentModel;
using TimezoneConverter.Services;

namespace TimezoneConverter;

/// <summary>
/// Editable time field: pick a preset from 30-minute steps, or type any time.
/// </summary>
internal sealed class TimeEntryCombo : ComboBox
{
    private TimeSpan _timeOfDay;
    private bool _use24Hour;
    private bool _includeSeconds;
    private bool _suppress;
    private bool _presetsInitialized;
    private bool _handlingSelection;
    private bool _presetsDirty;

    public event EventHandler? TimeChanged;

    public TimeEntryCombo()
    {
        DropDownStyle = ComboBoxStyle.DropDown; // allows typing
        IntegralHeight = false;
        MaxDropDownItems = 12;
        FlatStyle = FlatStyle.Flat;
        AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        AutoCompleteSource = AutoCompleteSource.ListItems;
        BackColor = UiTheme.InputBack;
        ForeColor = UiTheme.TextPrimary;

        // Selecting a 30-minute preset from the list
        SelectionChangeCommitted += (_, _) =>
        {
            if (_suppress)
                return;

            _handlingSelection = true;
            try
            {
                // The edit text can still describe the previous selection during
                // the native notification. Read the selected list item directly.
                var index = SelectedIndex;
                if (index >= 0 && index < Items.Count &&
                    TryParse(GetItemText(Items[index]), out var t))
                    SetTimeInternal(t, raiseEvent: true, updateText: true);
            }
            finally
            {
                _handlingSelection = false;
                if (_presetsDirty && !IsDisposed && !Disposing)
                {
                    // A TimeChanged subscriber can change the format. Wait until
                    // the native selection notification has finished to replace items.
                    if (IsHandleCreated)
                        BeginInvoke((Action)RefreshPendingPresets);
                    else
                        RefreshPendingPresets();
                }
            }
        };

        // Commit typed values when focus leaves or Enter is pressed
        Leave += (_, _) => CommitTypedText();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitTypedText();
                e.SuppressKeyPress = true;
            }
        };

        Configure(use24Hour: false, includeSeconds: false);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public TimeSpan TimeOfDay
    {
        get => _timeOfDay;
        set => SetTimeInternal(value, raiseEvent: false, updateText: true);
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

    public void Configure(bool use24Hour, bool includeSeconds)
    {
        var rebuild = !_presetsInitialized || _use24Hour != use24Hour;
        if (!rebuild && _includeSeconds == includeSeconds)
            return;

        _use24Hour = use24Hour;
        _includeSeconds = includeSeconds;
        if (rebuild)
            RebuildPresets();
        UpdateDisplayText();
    }

    public void RebuildPresets()
    {
        if (_handlingSelection)
        {
            _presetsDirty = true;
            return;
        }

        var presets = new object[48];
        for (var index = 0; index < presets.Length; index++)
            presets[index] = FormatTime(TimeSpan.FromMinutes(index * 30), includeSeconds: false);

        var wasSuppressed = _suppress;
        _suppress = true;
        BeginUpdate();
        try
        {
            var previous = Text;
            // Clear the native selection before the managed collection changes.
            SelectedIndex = -1;
            Items.Clear();
            Items.AddRange(presets);
            _presetsInitialized = true;
            _presetsDirty = false;

            // Restore typed/selected text if possible
            if (!string.IsNullOrWhiteSpace(previous))
                Text = previous;
            else
                UpdateDisplayText();
        }
        finally
        {
            EndUpdate();
            _suppress = wasSuppressed;
        }
    }

    private void RefreshPendingPresets()
    {
        if (!_presetsDirty || IsDisposed || Disposing)
            return;
        RebuildPresets();
        UpdateDisplayText();
    }

    private void CommitTypedText()
    {
        if (_suppress)
            return;

        if (TryParse(Text, out var t))
        {
            SetTimeInternal(t, raiseEvent: true, updateText: true);
        }
        else
        {
            // Revert invalid input to last known good time
            UpdateDisplayText();
        }
    }

    private void SetTimeInternal(TimeSpan time, bool raiseEvent, bool updateText)
    {
        time = Normalize(time);
        var changed = time != _timeOfDay;
        _timeOfDay = time;

        if (updateText)
            UpdateDisplayText();

        if (changed && raiseEvent)
            TimeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateDisplayText()
    {
        var display = FormatTime(_timeOfDay, _includeSeconds);
        if (Text == display)
            return;

        var wasSuppressed = _suppress;
        _suppress = true;
        try
        {
            Text = display;
        }
        finally
        {
            _suppress = wasSuppressed;
        }
    }

    private string FormatTime(TimeSpan time, bool includeSeconds) =>
        TimeParser.Format(time, _use24Hour, includeSeconds);

    /// <summary>
    /// Accepts common inputs: 7am, 7:00 AM, 19:00, 7.30pm, 0730, etc.
    /// </summary>
    public static bool TryParse(string? text, out TimeSpan time) =>
        TimeParser.TryParse(text, out time);

    private static TimeSpan Normalize(TimeSpan t) => TimeParser.Normalize(t);
}
