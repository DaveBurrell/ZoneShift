using System.ComponentModel;
using System.Globalization;

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

    private string FormatTime(TimeSpan time, bool includeSeconds)
    {
        // Anchor to a dummy date so DateTime formatting works
        var dt = DateTime.Today.Add(time);
        if (_use24Hour)
            return includeSeconds ? dt.ToString("HH:mm:ss") : dt.ToString("HH:mm");
        return includeSeconds ? dt.ToString("h:mm:ss tt") : dt.ToString("h:mm tt");
    }

    /// <summary>
    /// Accepts common inputs: 7am, 7:00 AM, 19:00, 7.30pm, 0730, etc.
    /// </summary>
    public static bool TryParse(string? text, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var s = text.Trim();

        // Normalize dots used as separators (7.30)
        s = s.Replace('.', ':');

        // Compact forms: 7am / 730pm / 0730 / 1930
        if (TryParseLoose(s, out time))
            return true;

        string[] formats =
        [
            "h:mm tt", "hh:mm tt", "h:mm:ss tt", "hh:mm:ss tt",
            "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss",
            "h tt", "hh tt", "htt", "hhtt",
            "H", "HH"
        ];

        if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out var exact))
        {
            time = exact.TimeOfDay;
            return true;
        }

        // Culture-aware fallback (respects local AM/PM patterns)
        if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault, out var parsed))
        {
            time = parsed.TimeOfDay;
            return true;
        }

        return false;
    }

    private static bool TryParseLoose(string s, out TimeSpan time)
    {
        time = default;
        var compact = s.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();

        // 7AM / 12PM / 7:30PM already handled by DateTime; try 730AM / 0730PM
        var am = compact.EndsWith("AM", StringComparison.Ordinal);
        var pm = compact.EndsWith("PM", StringComparison.Ordinal);
        if (am || pm)
        {
            var core = compact[..^2];
            if (TryParseHourMinuteDigits(core, out var h12, out var m))
            {
                if (h12 is < 1 or > 12)
                    return false;
                var h = h12 % 12;
                if (pm)
                    h += 12;
                time = new TimeSpan(h, m, 0);
                return true;
            }
        }

        // 24h digits only: 730, 0730, 1930, 19:30 already covered
        if (compact.All(char.IsDigit) && TryParseHourMinuteDigits(compact, out var h24, out var m24))
        {
            if (h24 is >= 0 and <= 23 && m24 is >= 0 and <= 59)
            {
                time = new TimeSpan(h24, m24, 0);
                return true;
            }
        }

        return false;
    }

    private static bool TryParseHourMinuteDigits(string core, out int hour, out int minute)
    {
        hour = 0;
        minute = 0;
        core = core.Replace(":", "", StringComparison.Ordinal);
        if (core.Length is < 1 or > 4 || !core.All(char.IsDigit))
            return false;

        if (core.Length <= 2)
        {
            hour = int.Parse(core, CultureInfo.InvariantCulture);
            minute = 0;
            return true;
        }

        if (core.Length == 3)
        {
            // 730 -> 7:30
            hour = int.Parse(core[..1], CultureInfo.InvariantCulture);
            minute = int.Parse(core[1..], CultureInfo.InvariantCulture);
            return minute is >= 0 and <= 59;
        }

        // 4 digits: 0730 / 1930
        hour = int.Parse(core[..2], CultureInfo.InvariantCulture);
        minute = int.Parse(core[2..], CultureInfo.InvariantCulture);
        return minute is >= 0 and <= 59;
    }

    private static TimeSpan Normalize(TimeSpan t)
    {
        // Keep within a single day
        var total = (int)t.TotalSeconds % (24 * 60 * 60);
        if (total < 0)
            total += 24 * 60 * 60;
        return TimeSpan.FromSeconds(total);
    }
}
