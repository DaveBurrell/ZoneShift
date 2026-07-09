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
    public void TryParse_invalid_inputs(string input)
    {
        Assert.False(TimeParser.TryParse(input, out _));
    }

    [Fact]
    public void Format_24h_and_12h()
    {
        var t = new TimeSpan(15, 30, 0);
        Assert.Equal("15:30", TimeParser.Format(t, use24Hour: true, includeSeconds: false));
        Assert.Equal("3:30 PM", TimeParser.Format(t, use24Hour: false, includeSeconds: false));
    }
}
