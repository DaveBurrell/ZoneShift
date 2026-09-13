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
        var effectiveWall = unspecified;

        if (inputTimeZone.IsInvalidTime(unspecified))
        {
            effectiveWall = ShiftOutOfGap(inputTimeZone, unspecified);
            warning =
                $"{unspecified:h:mm tt} does not exist in {inputTimeZone.Id} (DST spring-forward). " +
                $"Using {effectiveWall:h:mm tt} instead.";
        }

        DateTime utc;
        if (inputTimeZone.IsAmbiguousTime(effectiveWall))
        {
            // The larger offset identifies the earlier occurrence of the repeated wall time.
            var offsets = inputTimeZone.GetAmbiguousTimeOffsets(effectiveWall);
            var preferred = offsets[0] > offsets[1] ? offsets[0] : offsets[1];
            utc = new DateTime(
                Math.Clamp(effectiveWall.Ticks - preferred.Ticks, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks),
                DateTimeKind.Utc);
            warning ??=
                $"{effectiveWall:h:mm tt} is ambiguous in {inputTimeZone.Id} (DST fall-back). " +
                $"Using the {FormatOffset(preferred)} occurrence.";
        }
        else
        {
            utc = TimeZoneInfo.ConvertTimeToUtc(effectiveWall, inputTimeZone);
        }

        return CreateSnapshot(effectiveWall, utc, primaryTimeZone, targets, warning);
    }

    public static ConversionSnapshot ConvertLiveNow(
        TimeZoneInfo inputTimeZone,
        TimeZoneInfo primaryTimeZone,
        IReadOnlyList<(string Abbreviation, string WindowsId, TimeZoneInfo Zone)> targets)
    {
        var utc = DateTime.UtcNow;
        var inputWall = TimeZoneInfo.ConvertTimeFromUtc(utc, inputTimeZone);
        return CreateSnapshot(inputWall, utc, primaryTimeZone, targets);
    }

    private static DateTime ShiftOutOfGap(TimeZoneInfo zone, DateTime wall)
    {
        // Derive the actual jump: DST gaps can be half an hour or several hours.
        var before = FindOutsideGap(zone, wall, -TimeSpan.TicksPerHour);
        var after = FindOutsideGap(zone, wall, TimeSpan.TicksPerHour);
        if (zone.IsInvalidTime(after))
            return before; // No later wall time is representable at DateTime.MaxValue.

        var gap = zone.GetUtcOffset(after) - zone.GetUtcOffset(before);
        var shifted = AddClamped(wall, gap.Ticks);
        return gap > TimeSpan.Zero && !zone.IsInvalidTime(shifted) ? shifted : after;
    }

    private static DateTime FindOutsideGap(TimeZoneInfo zone, DateTime wall, long stepTicks)
    {
        while (zone.IsInvalidTime(wall))
        {
            var next = AddClamped(wall, stepTicks);
            if (next == wall)
                break;
            wall = next;
        }

        return wall;
    }

    private static DateTime AddClamped(DateTime wall, long ticks) =>
        new(Math.Clamp(wall.Ticks + ticks, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks), DateTimeKind.Unspecified);

    private static ConversionSnapshot CreateSnapshot(
        DateTime inputWall,
        DateTime utc,
        TimeZoneInfo primaryTimeZone,
        IReadOnlyList<(string Abbreviation, string WindowsId, TimeZoneInfo Zone)> targets,
        string? warning = null)
    {
        var primary = TimeZoneInfo.ConvertTimeFromUtc(utc, primaryTimeZone);
        // An ambiguous wall time loses which occurrence was selected. UTC retains that information.
        var primaryOffset = primaryTimeZone.GetUtcOffset(utc);

        var results = new ZoneConversionResult[targets.Count];
        for (var i = 0; i < targets.Count; i++)
        {
            var (abbr, windowsId, zone) = targets[i];
            var converted = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
            var offset = zone.GetUtcOffset(utc);
            var dayDelta = (converted.Date - primary.Date).Days;
            results[i] = new ZoneConversionResult(abbr, windowsId, converted, offset, dayDelta);
        }

        return new ConversionSnapshot(inputWall, utc, primary, primaryOffset, results, warning);
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
