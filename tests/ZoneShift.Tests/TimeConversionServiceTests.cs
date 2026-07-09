using TimezoneConverter.Services;
using Xunit;

namespace ZoneShift.Tests;

public class TimeConversionServiceTests
{
    [Fact]
    public void Convert_same_zone_preserves_wall_time()
    {
        var tz = TimeZoneInfo.Utc;
        var wall = new DateTime(2024, 6, 15, 14, 30, 0);
        var snap = TimeConversionService.Convert(
            wall,
            tz,
            tz,
            Array.Empty<(string, string, TimeZoneInfo)>());

        Assert.Equal(14, snap.PrimaryLocalTime.Hour);
        Assert.Equal(30, snap.PrimaryLocalTime.Minute);
        Assert.Null(snap.Warning);
    }

    [Fact]
    public void Convert_utc_to_india_is_plus_530()
    {
        var utc = TimeZoneInfo.Utc;
        var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var wall = new DateTime(2024, 1, 10, 0, 0, 0);

        var snap = TimeConversionService.Convert(
            wall,
            utc,
            ist,
            [("IST", ist.Id, ist)]);

        Assert.Equal(5, snap.PrimaryLocalTime.Hour);
        Assert.Equal(30, snap.PrimaryLocalTime.Minute);
        Assert.Single(snap.Targets);
    }

    [Fact]
    public void FormatOffset_handles_half_hours()
    {
        Assert.Equal("UTC+5:30", TimeConversionService.FormatOffset(TimeSpan.FromHours(5.5)));
        Assert.Equal("UTC-5", TimeConversionService.FormatOffset(TimeSpan.FromHours(-5)));
    }

    [Fact]
    public void FormatDayDelta_labels()
    {
        Assert.Equal(string.Empty, TimeConversionService.FormatDayDelta(0));
        Assert.Equal(" +1d", TimeConversionService.FormatDayDelta(1));
        Assert.Equal(" -1d", TimeConversionService.FormatDayDelta(-1));
    }

    [Fact]
    public void ClassifyLocalTime_utc_is_always_valid()
    {
        var wall = new DateTime(2024, 3, 10, 2, 30, 0);
        Assert.Equal(
            TimeConversionService.LocalTimeKind.Valid,
            TimeConversionService.ClassifyLocalTime(TimeZoneInfo.Utc, wall));
    }

    [Fact]
    public void ClassifyLocalTime_us_eastern_spring_forward_gap_is_invalid()
    {
        // US Eastern DST spring forward 2024: 2:00 AM -> 3:00 AM on March 10
        TimeZoneInfo eastern;
        try
        {
            eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            // Non-Windows CI would use IANA; this project is Windows-only
            return;
        }

        var gap = new DateTime(2024, 3, 10, 2, 30, 0);
        var kind = TimeConversionService.ClassifyLocalTime(eastern, gap);
        Assert.Equal(TimeConversionService.LocalTimeKind.Invalid, kind);

        var snap = TimeConversionService.Convert(
            gap,
            eastern,
            TimeZoneInfo.Utc,
            Array.Empty<(string, string, TimeZoneInfo)>());

        Assert.NotNull(snap.Warning);
        Assert.Contains("does not exist", snap.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClassifyLocalTime_us_eastern_fall_back_overlap_is_ambiguous()
    {
        // US Eastern DST fall back 2024: 2:00 AM -> 1:00 AM on November 3
        TimeZoneInfo eastern;
        try
        {
            eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return;
        }

        var overlap = new DateTime(2024, 11, 3, 1, 30, 0);
        var kind = TimeConversionService.ClassifyLocalTime(eastern, overlap);
        Assert.Equal(TimeConversionService.LocalTimeKind.Ambiguous, kind);

        var snap = TimeConversionService.Convert(
            overlap,
            eastern,
            TimeZoneInfo.Utc,
            Array.Empty<(string, string, TimeZoneInfo)>());

        Assert.NotNull(snap.Warning);
        Assert.Contains("ambiguous", snap.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatCopy_multiline_and_one_line()
    {
        var utc = TimeZoneInfo.Utc;
        var snap = TimeConversionService.Convert(
            new DateTime(2024, 6, 1, 12, 0, 0),
            utc,
            utc,
            [("UTC2", "UTC", utc)]);

        var multi = TimeConversionService.FormatCopyMultiline(snap, use24Hour: true, live: false);
        var one = TimeConversionService.FormatCopyOneLine(snap, use24Hour: true, live: false);

        Assert.Contains("ZoneShift", multi);
        Assert.Contains("UTC2", multi);
        Assert.Contains('|', one);
        Assert.Contains("UTC2", one);
    }
}
