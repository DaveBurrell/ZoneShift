namespace TimezoneConverter.Services;

public sealed record ZoneConversionResult(
    string Abbreviation,
    string WindowsId,
    DateTime LocalWallTime,
    TimeSpan UtcOffset,
    int DayDeltaFromPrimary);

public sealed record ConversionSnapshot(
    DateTime InputWallTime,
    DateTime Utc,
    DateTime PrimaryLocalTime,
    TimeSpan PrimaryUtcOffset,
    IReadOnlyList<ZoneConversionResult> Targets);

/// <summary>
/// Pure timezone conversion logic (no UI).
/// </summary>
public static class TimeConversionService
{
    /// <summary>
    /// Interprets a wall-clock time in <paramref name="inputTimeZone"/> and converts to UTC and targets.
    /// </summary>
    public static ConversionSnapshot Convert(
        DateTime inputWallUnspecified,
        TimeZoneInfo inputTimeZone,
        TimeZoneInfo primaryTimeZone,
        IReadOnlyList<(string Abbreviation, string WindowsId, TimeZoneInfo Zone)> targets)
    {
        var unspecified = DateTime.SpecifyKind(inputWallUnspecified, DateTimeKind.Unspecified);
        DateTime utc;
        try
        {
            utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, inputTimeZone);
        }
        catch (ArgumentException)
        {
            // Invalid local time (e.g. DST spring-forward gap) — nudge forward 1 hour and retry once
            utc = TimeZoneInfo.ConvertTimeToUtc(unspecified.AddHours(1), inputTimeZone);
            inputWallUnspecified = unspecified.AddHours(1);
        }

        var primary = TimeZoneInfo.ConvertTimeFromUtc(utc, primaryTimeZone);
        var primaryOffset = primaryTimeZone.GetUtcOffset(primary);

        var results = new List<ZoneConversionResult>(targets.Count);
        foreach (var (abbr, windowsId, zone) in targets)
        {
            var converted = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
            var offset = zone.GetUtcOffset(converted);
            var dayDelta = (converted.Date - primary.Date).Days;
            results.Add(new ZoneConversionResult(abbr, windowsId, converted, offset, dayDelta));
        }

        return new ConversionSnapshot(
            inputWallUnspecified,
            utc,
            primary,
            primaryOffset,
            results);
    }

    /// <summary>
    /// Live "now" converted into input zone wall time and all target zones.
    /// </summary>
    public static ConversionSnapshot ConvertLiveNow(
        TimeZoneInfo inputTimeZone,
        TimeZoneInfo primaryTimeZone,
        IReadOnlyList<(string Abbreviation, string WindowsId, TimeZoneInfo Zone)> targets)
    {
        var utc = DateTime.UtcNow;
        var inputWall = TimeZoneInfo.ConvertTimeFromUtc(utc, inputTimeZone);
        var primary = TimeZoneInfo.ConvertTimeFromUtc(utc, primaryTimeZone);
        var primaryOffset = primaryTimeZone.GetUtcOffset(primary);

        var results = new List<ZoneConversionResult>(targets.Count);
        foreach (var (abbr, windowsId, zone) in targets)
        {
            var converted = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
            var offset = zone.GetUtcOffset(converted);
            var dayDelta = (converted.Date - primary.Date).Days;
            results.Add(new ZoneConversionResult(abbr, windowsId, converted, offset, dayDelta));
        }

        return new ConversionSnapshot(inputWall, utc, primary, primaryOffset, results);
    }

    public static string FormatOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var abs = offset.Duration();
        return abs.Minutes == 0
            ? $"UTC{sign}{abs.Hours}"
            : $"UTC{sign}{abs.Hours}:{abs.Minutes:D2}";
    }

    public static string FormatDayDelta(int days) =>
        days switch
        {
            0 => string.Empty,
            1 => " +1d",
            -1 => " -1d",
            > 1 => $" +{days}d",
            _ => $" {days}d"
        };

    public static string FormatDigital(DateTime time, bool use24Hour, bool includeSeconds)
    {
        if (includeSeconds)
            return use24Hour ? time.ToString("HH:mm:ss") : time.ToString("hh:mm:ss tt");
        return use24Hour ? time.ToString("HH:mm") : time.ToString("hh:mm tt");
    }
}
