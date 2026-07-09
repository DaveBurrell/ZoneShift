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
        Assert.Equal(TimeSpan.Zero, snap.PrimaryUtcOffset);
    }

    [Fact]
    public void Convert_utc_to_india_is_plus_530()
    {
        var utc = TimeZoneInfo.Utc;
        var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var wall = new DateTime(2024, 1, 10, 0, 0, 0); // midnight UTC

        var snap = TimeConversionService.Convert(
            wall,
            utc,
            ist,
            [("IST", ist.Id, ist)]);

        Assert.Equal(5, snap.PrimaryLocalTime.Hour);
        Assert.Equal(30, snap.PrimaryLocalTime.Minute);
        Assert.Single(snap.Targets);
        Assert.Equal(5, snap.Targets[0].LocalWallTime.Hour);
        Assert.Equal(30, snap.Targets[0].LocalWallTime.Minute);
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
}
