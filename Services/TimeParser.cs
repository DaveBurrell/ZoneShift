using System.Globalization;

namespace TimezoneConverter.Services;

/// <summary>
/// Parses user-entered wall-clock times (12h/24h, compact forms).
/// </summary>
public static class TimeParser
{
    public static bool TryParse(string? text, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var s = text.Trim().Replace('.', ':');

        if (TryParseLoose(s, out time))
            return true;

        string[] formats =
        [
            "h:mm tt", "hh:mm tt", "h:mm:ss tt", "hh:mm:ss tt",
            "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss",
            "h tt", "hh tt",
            "H", "HH"
        ];

        try
        {
            if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces, out var exact))
            {
                time = exact.TimeOfDay;
                return true;
            }

            if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault, out var parsed))
            {
                time = parsed.TimeOfDay;
                return true;
            }
        }
        catch (FormatException)
        {
            return false;
        }

        return false;
    }

    public static string Format(TimeSpan time, bool use24Hour, bool includeSeconds)
    {
        var dt = DateTime.Today.Add(Normalize(time));
        if (use24Hour)
            return includeSeconds ? dt.ToString("HH:mm:ss") : dt.ToString("HH:mm");
        return includeSeconds ? dt.ToString("h:mm:ss tt") : dt.ToString("h:mm tt");
    }

    public static TimeSpan Normalize(TimeSpan t)
    {
        var total = (int)t.TotalSeconds % (24 * 60 * 60);
        if (total < 0)
            total += 24 * 60 * 60;
        return TimeSpan.FromSeconds(total);
    }

    private static bool TryParseLoose(string s, out TimeSpan time)
    {
        time = default;
        var compact = s.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();

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
            hour = int.Parse(core[..1], CultureInfo.InvariantCulture);
            minute = int.Parse(core[1..], CultureInfo.InvariantCulture);
            return minute is >= 0 and <= 59;
        }

        hour = int.Parse(core[..2], CultureInfo.InvariantCulture);
        minute = int.Parse(core[2..], CultureInfo.InvariantCulture);
        return minute is >= 0 and <= 59;
    }
}
