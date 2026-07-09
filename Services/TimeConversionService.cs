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
    IReadOnlyList<ZoneConversionResult> Targets,
    string? Warning = null);

/// <summary>
/// Pure timezone conversion logic (no UI).
/// </summary>
public static class TimeConversionService
{
    public enum LocalTimeKind
    {
        Valid,
        Invalid,   // DST spring-forward gap
        Ambiguous  // DST fall-back overlap
    }

    public static LocalTimeKind ClassifyLocalTime(TimeZoneInfo zone, DateTime wallUnspecified)
    {
        var t = DateTime.SpecifyKind(wallUnspecified, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(t))
            return LocalTimeKind.Invalid;
        if (zone.IsAmbiguousTime(t))
            return LocalTimeKind.Ambiguous;
        return LocalTimeKind.Valid;
    }

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
        string? warning = null;
        var kind = ClassifyLocalTime(inputTimeZone, unspecified);

        DateTime utc;
        var effectiveWall = unspecified;

        if (kind == LocalTimeKind.Invalid)
        {
            // Spring-forward gap: time does not exist — advance to next valid instant
            effectiveWall = unspecified.AddHours(1);
            // keep advancing until valid (rare multi-hour gaps)
            var guard = 0;
            while (inputTimeZone.IsInvalidTime(DateTime.SpecifyKind(effectiveWall, DateTimeKind.Unspecified)) && guard++ < 4)
                effectiveWall = effectiveWall.AddMinutes(30);

            warning =
                $"{unspecified:h:mm tt} does not exist in {inputTimeZone.Id} (DST spring-forward). " +
                $"Using {effectiveWall:h:mm tt} instead.";
            utc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(effectiveWall, DateTimeKind.Unspecified),
                inputTimeZone);
        }
        else if (kind == LocalTimeKind.Ambiguous)
        {
            // Fall-back: prefer the earlier (DST) offset for consistency
            var offsets = inputTimeZone.GetAmbiguousTimeOffsets(unspecified);
            var preferred = offsets.OrderByDescending(o => o).First(); // larger offset = usually DST
            utc = DateTime.SpecifyKind(unspecified - preferred, DateTimeKind.Utc);
            warning =
                $"{unspecified:h:mm tt} is ambiguous in {inputTimeZone.Id} (DST fall-back). " +
                $"Using the {FormatOffset(preferred)} occurrence.";
            effectiveWall = unspecified;
        }
        else
        {
            try
            {
                utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, inputTimeZone);
            }
            catch (ArgumentException)
            {
                effectiveWall = unspecified.AddHours(1);
                utc = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(effectiveWall, DateTimeKind.Unspecified),
                    inputTimeZone);
                warning =
                    $"Could not convert {unspecified:h:mm tt} in {inputTimeZone.Id}. Using {effectiveWall:h:mm tt}.";
            }
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
            effectiveWall,
            utc,
            primary,
            primaryOffset,
            results,
            warning);
    }

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

    /// <summary>Multi-line clipboard block.</summary>
    public static string FormatCopyMultiline(ConversionSnapshot snap, bool use24Hour, bool live)
    {
        var lines = new List<string>
        {
            $"ZoneShift ({snap.PrimaryLocalTime:yyyy-MM-dd}{(live ? ", live" : "")})",
            $"Local: {FormatDigital(snap.PrimaryLocalTime, use24Hour, live)} ({FormatOffset(snap.PrimaryUtcOffset)})"
        };
        foreach (var t in snap.Targets)
        {
            var day = FormatDayDelta(t.DayDeltaFromPrimary);
            lines.Add($"{t.Abbreviation}: {FormatDigital(t.LocalWallTime, use24Hour, live)} ({FormatOffset(t.UtcOffset)}{day})");
        }

        if (!string.IsNullOrWhiteSpace(snap.Warning))
            lines.Add($"Note: {snap.Warning}");

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Single-line clipboard for chat apps.</summary>
    public static string FormatCopyOneLine(ConversionSnapshot snap, bool use24Hour, bool live)
    {
        var parts = new List<string>
        {
            $"Local {FormatDigital(snap.PrimaryLocalTime, use24Hour, false)}"
        };
        foreach (var t in snap.Targets)
            parts.Add($"{t.Abbreviation} {FormatDigital(t.LocalWallTime, use24Hour, false)}");

        var line = string.Join(" | ", parts);
        if (!string.IsNullOrWhiteSpace(snap.Warning))
            line += " (DST note)";
        return line;
    }
}
