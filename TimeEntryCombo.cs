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

    public event EventHandler? TimeChanged;

    public TimeEntryCombo()
    {
        DropDownStyle = ComboBoxStyle.DropDown; // allows typing
        IntegralHeight = false;
        MaxDropDownItems = 12;
        FlatStyle = FlatStyle.Flat;
        AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        AutoCompleteSource = AutoCompleteSource.ListItems;

        // Selecting a 30-minute preset from the list
        SelectionChangeCommitted += (_, _) =>
        {
            if (_suppress)
                return;
            if (TryParse(Text, out var t))
                SetTimeInternal(t, raiseEvent: true, updateText: true);
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
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public TimeSpan TimeOfDay
    {
        get => _timeOfDay;
        set => SetTimeInternal(Normalize(value), raiseEvent: false, updateText: true);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool Use24Hour
    {
        get => _use24Hour;
        set
        {
            if (_use24Hour == value)
                return;
            _use24Hour = value;
            RebuildPresets();
            UpdateDisplayText();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool IncludeSeconds
    {
        get => _includeSeconds;
        set
        {
            if (_includeSeconds == value)
                return;
            _includeSeconds = value;
            UpdateDisplayText();
        }
    }

    public void Configure(bool use24Hour, bool includeSeconds)
    {
        _use24Hour = use24Hour;
        _includeSeconds = includeSeconds;
        RebuildPresets();
        UpdateDisplayText();
    }

    public void RebuildPresets()
    {
        _suppress = true;
        try
        {
            var previous = Text;
            Items.Clear();
            for (var minutes = 0; minutes < 24 * 60; minutes += 30)
            {
                var t = TimeSpan.FromMinutes(minutes);
                Items.Add(FormatTime(t, includeSeconds: false));
            }

            // Restore typed/selected text if possible
            if (!string.IsNullOrWhiteSpace(previous))
                Text = previous;
            else
                UpdateDisplayText();
        }
        finally
        {
            _suppress = false;
        }
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
        _suppress = true;
        try
        {
            Text = FormatTime(_timeOfDay, _includeSeconds);
        }
        finally
        {
            _suppress = false;
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

