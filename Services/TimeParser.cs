using System.Globalization;

namespace TimezoneConverter.Services;

/// <summary>
/// Parses user-entered wall-clock times (12h/24h, compact forms).
/// </summary>
public static class TimeParser
{
    private static readonly string[] TimeFormats =
    [
        "h:mm tt", "hh:mm tt", "h:mm:ss tt", "hh:mm:ss tt",
        "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss",
        "h tt", "hh tt", "%H", "HH"
    ];

    public static bool TryParse(string? text, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var s = text.AsSpan().Trim();

        if (TryParseLoose(s, out time, out var hasExplicitDesignator))
            return true;

        // DateTime accepts "0am" even with an h-format. Preserve the explicit
        // 1–12 hour validation for AM/PM input instead of retrying it permissively.
        if (hasExplicitDesignator)
            return false;

        // Accept localized separators and AM/PM labels, but never interpret dates
        // or other non-time input as midnight.
        if (DateTime.TryParseExact(s, TimeFormats, CultureInfo.CurrentCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault, out var parsed))
        {
            time = parsed.TimeOfDay;
            return true;
        }

        return false;
    }

    public static string Format(TimeSpan time, bool use24Hour, bool includeSeconds)
    {
        var dt = DateTime.MinValue.Add(Normalize(time));
        if (use24Hour)
            return includeSeconds ? dt.ToString("HH:mm:ss") : dt.ToString("HH:mm");
        return includeSeconds ? dt.ToString("h:mm:ss tt") : dt.ToString("h:mm tt");
    }

    public static TimeSpan Normalize(TimeSpan t)
    {
        var total = t.Ticks / TimeSpan.TicksPerSecond % (24 * 60 * 60);
        if (total < 0)
            total += 24 * 60 * 60;
        return TimeSpan.FromSeconds(total);
    }

    private static bool TryParseLoose(ReadOnlySpan<char> s, out TimeSpan time, out bool hasExplicitDesignator)
    {
        time = default;
        var am = s.EndsWith("AM", StringComparison.OrdinalIgnoreCase);
        var pm = s.EndsWith("PM", StringComparison.OrdinalIgnoreCase);
        hasExplicitDesignator = am || pm;
        if (hasExplicitDesignator)
            s = s[..^2].TrimEnd();

        if (!TryParseParts(s, out var hour, out var minute, out var second))
            return false;

        if (am || pm)
        {
            if (hour is < 1 or > 12)
                return false;
            hour = hour % 12 + (pm ? 12 : 0);
        }
        else if (hour > 23)
            return false;

        time = new TimeSpan(hour, minute, second);
        return true;
    }

    private static bool TryParseParts(ReadOnlySpan<char> core, out int hour, out int minute, out int second)
    {
        hour = 0;
        minute = 0;
        second = 0;

        var separator = core.IndexOfAny(':', '.');
        if (separator >= 0)
        {
            if (!TryParseDigits(core[..separator].Trim(), out hour))
                return false;

            var separatorChar = core[separator];
            var remainder = core[(separator + 1)..];
            var secondsSeparator = remainder.IndexOfAny(':', '.');
            if (secondsSeparator >= 0)
            {
                if (remainder[secondsSeparator] != separatorChar ||
                    !TryParseDigits(remainder[(secondsSeparator + 1)..].Trim(), out second) || second > 59)
                    return false;
                remainder = remainder[..secondsSeparator];
            }

            return TryParseDigits(remainder.Trim(), out minute) && minute <= 59;
        }

        if (core.Length <= 2)
            return TryParseDigits(core, out hour);

        return core.Length is 3 or 4 &&
            TryParseDigits(core[..^2], out hour) &&
            TryParseDigits(core[^2..], out minute) && minute <= 59;
    }

    private static bool TryParseDigits(ReadOnlySpan<char> text, out int value)
    {
        value = 0;
        if (text.Length is < 1 or > 2)
            return false;

        foreach (var digit in text)
        {
            // char.IsDigit also accepts characters int.Parse cannot parse.
            if (digit is < '0' or > '9')
                return false;
            value = value * 10 + digit - '0';
        }

        return true;
    }
}
