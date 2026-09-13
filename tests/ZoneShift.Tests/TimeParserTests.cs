using System.Globalization;
using TimezoneConverter.Services;
using Xunit;

namespace ZoneShift.Tests;

public class TimeParserTests
{
    [Theory]
    [InlineData("7:00 AM", 7, 0)]
    [InlineData("7am", 7, 0)]
    [InlineData("7:30 PM", 19, 30)]
    [InlineData("19:00", 19, 0)]
    [InlineData("00:00", 0, 0)]
    [InlineData("12:00 PM", 12, 0)]
    [InlineData("12:00 AM", 0, 0)]
    [InlineData("0730", 7, 30)]
    [InlineData("1930", 19, 30)]
    [InlineData("7.45pm", 19, 45)]
    [InlineData("7:5 PM", 19, 5)]
    [InlineData(" 7 : 30 pm ", 19, 30)]
    [InlineData("12am", 0, 0)]
    [InlineData("12pm", 12, 0)]
    [InlineData("23", 23, 0)]
    [InlineData("7", 7, 0)]
    public void TryParse_valid_inputs(string input, int hour, int minute)
    {
        Assert.True(TimeParser.TryParse(input, out var time));
        Assert.Equal(hour, time.Hours);
        Assert.Equal(minute, time.Minutes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-time")]
    [InlineData("25:00")]
    [InlineData(null)]
    [InlineData("7::30pm")]
    [InlineData("7:300pm")]
    [InlineData("7:30:")]
    [InlineData(":730pm")]
    [InlineData("7:60pm")]
    [InlineData("12:30:60")]
    [InlineData("0am")]
    [InlineData("0:30am")]
    [InlineData("00:30 PM")]
    [InlineData("13pm")]
    [InlineData("24:00")]
    [InlineData("٧٣٠")]
    [InlineData("７３０pm")]
    [InlineData("2026-09-13")]
    [InlineData("9/13/2026 7:30 PM")]
    [InlineData("99999999999999999999999999")]
    public void TryParse_invalid_inputs(string? input)
    {
        Assert.False(TimeParser.TryParse(input, out _));
    }

    [Theory]
    [InlineData("7:30:45pm", 19, 30, 45)]
    [InlineData("23:59:59", 23, 59, 59)]
    [InlineData("7.30.45 AM", 7, 30, 45)]
    public void TryParse_preserves_seconds(string input, int hour, int minute, int second)
    {
        Assert.True(TimeParser.TryParse(input, out var time));
        Assert.Equal(new TimeSpan(hour, minute, second), time);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("ar-SA")]
    [InlineData("fa-IR")]
    [InlineData("he-IL")]
    [InlineData("fi-FI")]
    [InlineData("ko-KR")]
    public void Formatted_times_round_trip_in_current_culture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            foreach (var use24Hour in new[] { false, true })
            foreach (var includeSeconds in new[] { false, true })
            {
                var expected = new TimeSpan(15, 30, includeSeconds ? 27 : 0);
                var text = TimeParser.Format(expected, use24Hour, includeSeconds);

                Assert.True(TimeParser.TryParse(text, out var parsed), text);
                Assert.Equal(expected, parsed);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Format_and_parse_work_when_calendar_cannot_represent_year_one()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            var culture = new CultureInfo("he-IL");
            culture.DateTimeFormat.Calendar = new HebrewCalendar();
            CultureInfo.CurrentCulture = culture;
            var expected = new TimeSpan(15, 30, 27);

            var text = TimeParser.Format(expected, use24Hour: false, includeSeconds: true);

            Assert.True(TimeParser.TryParse(text, out var parsed), text);
            Assert.Equal(expected, parsed);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Normalize_handles_extreme_durations_without_overflow()
    {
        Assert.Equal(new TimeSpan(2, 48, 5), TimeParser.Normalize(TimeSpan.MaxValue));
        Assert.Equal(new TimeSpan(21, 11, 55), TimeParser.Normalize(TimeSpan.MinValue));
        Assert.Equal(new TimeSpan(15, 30, 0), TimeParser.Normalize(TimeSpan.FromDays(100000) + new TimeSpan(15, 30, 0)));
        Assert.Equal(new TimeSpan(23, 59, 59), TimeParser.Normalize(TimeSpan.FromSeconds(-1)));
        Assert.Equal(TimeSpan.Zero, TimeParser.Normalize(TimeSpan.FromTicks(-1)));
    }

    [Fact]
    public void Format_24h_and_12h()
    {
        var t = new TimeSpan(15, 30, 0);
        Assert.Equal("15:30", TimeParser.Format(t, use24Hour: true, includeSeconds: false));
        Assert.Equal("3:30 PM", TimeParser.Format(t, use24Hour: false, includeSeconds: false));
    }
}
