using System.Globalization;
using TimezoneConverter;
using Xunit;

namespace ZoneShift.Tests;

/// <summary>
/// ThemedDateBox replaced the native DateTimePicker, which had no dark mode. These pin the
/// behaviour the rest of the app depends on: a Value that is always a readable date, and a
/// ValueChanged that fires only on real changes.
/// </summary>
public class ThemedDateBoxTests
{
    [Fact]
    public void Value_is_normalised_to_a_date()
    {
        using var box = new ThemedDateBox { Value = new DateTime(2026, 7, 10, 14, 37, 52) };
        Assert.Equal(new DateTime(2026, 7, 10), box.Value);
        Assert.Equal(TimeSpan.Zero, box.Value.TimeOfDay);
    }

    [Fact]
    public void Setting_the_same_date_does_not_raise_ValueChanged()
    {
        using var box = new ThemedDateBox { Value = new DateTime(2026, 7, 10) };

        var raised = 0;
        box.ValueChanged += (_, _) => raised++;

        box.Value = new DateTime(2026, 7, 10);
        Assert.Equal(0, raised);

        // Same day, different clock time — still the same date.
        box.Value = new DateTime(2026, 7, 10, 23, 59, 59);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void Changing_the_date_raises_ValueChanged_once()
    {
        using var box = new ThemedDateBox { Value = new DateTime(2026, 7, 10) };

        var raised = 0;
        box.ValueChanged += (_, _) => raised++;

        box.Value = new DateTime(2026, 7, 11);

        Assert.Equal(1, raised);
        Assert.Equal(new DateTime(2026, 7, 11), box.Value);
    }

    /// <summary>
    /// SyncPickersToNow compares <c>_datePicker.Value.Date</c> against the live zone date and
    /// assigns when they differ, so a round-trip through the setter must be stable.
    /// </summary>
    [Fact]
    public void Assigning_then_reading_round_trips_the_date()
    {
        using var box = new ThemedDateBox();
        foreach (var date in new[] { new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), DateTime.Today })
        {
            box.Value = date;
            Assert.Equal(date.Date, box.Value.Date);
        }
    }

    [Fact]
    public void Value_survives_a_leap_day()
    {
        using var box = new ThemedDateBox { Value = new DateTime(2028, 2, 29) };
        Assert.Equal(new DateTime(2028, 2, 29), box.Value);
    }

    /// <summary>
    /// The control renders and re-parses with the current culture's short date pattern. If a
    /// round-trip through that pattern is lossy, typed input could not be trusted.
    /// </summary>
    [Fact]
    public void Short_date_pattern_round_trips_under_the_current_culture()
    {
        var date = new DateTime(2026, 7, 10);
        var rendered = date.ToString("d", CultureInfo.CurrentCulture);

        Assert.True(DateTime.TryParse(rendered, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed));
        Assert.Equal(date, parsed.Date);
    }
}
