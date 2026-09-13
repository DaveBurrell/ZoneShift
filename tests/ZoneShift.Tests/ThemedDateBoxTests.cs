using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Programmatic_dates_outside_calendar_bounds_can_open_safely(bool upperBound)
    {
        WinFormsTestHelper.Run(() =>
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            using var box = new ThemedDateBox
            {
                Value = upperBound ? DateTime.MaxValue : DateTime.MinValue
            };
            var expected = upperBound ? DateTimePicker.MaximumDateTime.Date : DateTimePicker.MinimumDateTime.Date;

            Invoke(box, "ShowCalendar");

            Assert.Equal(expected, box.Value);
            var calendar = GetField<MonthCalendar>(box, "_calendar");
            Assert.True(calendar.IsHandleCreated);
            Assert.Equal(expected, calendar.SelectionStart);
            Assert.Equal(expected, calendar.SelectionEnd);
            Assert.True(GetField<ToolStripDropDown>(box, "_calendarDropDown").Visible);
        });
    }

    [Theory]
    [InlineData("01/01/0001", 1753, 1, 1)]
    [InlineData("12/31/9999", 9998, 12, 31)]
    [InlineData("07/15/2026", 2026, 7, 15)]
    public void Opening_calendar_commits_and_bounds_typed_dates(string input, int year, int month, int day)
    {
        WinFormsTestHelper.Run(() =>
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            using var box = new ThemedDateBox { Value = new DateTime(2026, 7, 10) };
            var text = Assert.Single(box.Controls.OfType<TextBox>());
            text.Text = input;

            Invoke(box, "ShowCalendar");

            var expected = new DateTime(year, month, day);
            Assert.Equal(expected, box.Value);
            Assert.Equal(expected, GetField<MonthCalendar>(box, "_calendar").SelectionStart);
            Assert.Equal(expected.ToString("d", CultureInfo.InvariantCulture), text.Text);
        });
    }

    [Fact]
    public void Alt_down_opens_calendar_without_decrementing_the_date()
    {
        WinFormsTestHelper.Run(() =>
        {
            using var box = new ThemedDateBox { Value = new DateTime(2026, 7, 10) };
            var key = new KeyEventArgs(Keys.Alt | Keys.Down);

            Invoke(box, "OnTextKeyDown", null, key);

            Assert.Equal(new DateTime(2026, 7, 10), box.Value);
            Assert.True(key.SuppressKeyPress);
            Assert.True(GetField<ToolStripDropDown>(box, "_calendarDropDown").Visible);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Arrow_keys_reach_and_stay_at_the_calendar_bounds(bool upperBound)
    {
        WinFormsTestHelper.Run(() =>
        {
            var boundary = upperBound ? DateTimePicker.MaximumDateTime.Date : DateTimePicker.MinimumDateTime.Date;
            using var box = new ThemedDateBox { Value = boundary.AddDays(upperBound ? -1 : 1) };
            var key = upperBound ? Keys.Up : Keys.Down;

            Invoke(box, "OnTextKeyDown", null, new KeyEventArgs(key));
            Assert.Equal(boundary, box.Value);

            Invoke(box, "OnTextKeyDown", null, new KeyEventArgs(key));
            Assert.Equal(boundary, box.Value);
        });
    }

    [Fact]
    public void Page_down_reaches_and_stays_at_the_minimum_date()
    {
        WinFormsTestHelper.Run(() =>
        {
            var minimum = DateTimePicker.MinimumDateTime.Date;
            using var box = new ThemedDateBox { Value = minimum.AddMonths(1) };

            Invoke(box, "OnTextKeyDown", null, new KeyEventArgs(Keys.PageDown));
            Assert.Equal(minimum, box.Value);

            Invoke(box, "OnTextKeyDown", null, new KeyEventArgs(Keys.PageDown));
            Assert.Equal(minimum, box.Value);
        });
    }

    [Fact]
    public void Page_up_advances_into_the_last_month_and_clamps_at_the_maximum_date()
    {
        WinFormsTestHelper.Run(() =>
        {
            var maximum = DateTimePicker.MaximumDateTime.Date;
            var initial = maximum.AddMonths(-1);
            using var box = new ThemedDateBox { Value = initial };

            Invoke(box, "OnTextKeyDown", null, new KeyEventArgs(Keys.PageUp));
            Assert.Equal(initial.AddMonths(1), box.Value);

            Invoke(box, "OnTextKeyDown", null, new KeyEventArgs(Keys.PageUp));
            Assert.Equal(maximum, box.Value);

            Invoke(box, "OnTextKeyDown", null, new KeyEventArgs(Keys.PageUp));
            Assert.Equal(maximum, box.Value);
        });
    }

    [Fact]
    public void Calendar_selection_keeps_native_controls_alive_for_reuse_and_owner_disposes_them()
    {
        WinFormsTestHelper.Run(() =>
        {
            using var box = new ThemedDateBox { Value = new DateTime(2026, 7, 10) };
            Invoke(box, "ShowCalendar");
            var calendar = GetField<MonthCalendar>(box, "_calendar");
            var dropDown = GetField<ToolStripDropDown>(box, "_calendarDropDown");
            var calendarHandle = calendar.Handle;
            var selected = new DateTime(2026, 7, 15);
            var changes = 0;
            box.ValueChanged += (_, _) =>
            {
                changes++;
                Assert.False(calendar.IsDisposed);
                Assert.False(dropDown.IsDisposed);
            };

            calendar.SetDate(selected);
            typeof(MonthCalendar).GetMethod("OnDateSelected", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(calendar, [new DateRangeEventArgs(selected, selected)]);

            Assert.Equal(selected, box.Value);
            Assert.Equal(1, changes);
            Assert.False(dropDown.Visible);
            Assert.False(calendar.IsDisposed);
            Assert.False(dropDown.IsDisposed);

            Invoke(box, "ShowCalendar");
            Assert.Same(calendar, GetField<MonthCalendar>(box, "_calendar"));
            Assert.Same(dropDown, GetField<ToolStripDropDown>(box, "_calendarDropDown"));
            Assert.Equal(calendarHandle, calendar.Handle);
            Assert.Equal(selected, calendar.SelectionStart);
            Assert.True(dropDown.Visible);

            box.Dispose();
            Assert.True(calendar.IsDisposed);
            Assert.True(dropDown.IsDisposed);
        });
    }

    [Fact]
    public void Disabling_the_date_box_closes_the_calendar_and_prevents_reopening()
    {
        WinFormsTestHelper.Run(() =>
        {
            using var box = new ThemedDateBox();
            Invoke(box, "ShowCalendar");
            var dropDown = GetField<ToolStripDropDown>(box, "_calendarDropDown");

            box.Enabled = false;
            Invoke(box, "ShowCalendar");

            Assert.False(dropDown.Visible);
            Assert.False(dropDown.IsDisposed);
        });
    }

    [Theory]
    [InlineData("ar-SA")]
    [InlineData("he-IL")]
    public void Date_bounds_respect_the_active_formatting_calendar(string cultureName)
    {
        WinFormsTestHelper.Run(() =>
        {
            var culture = new CultureInfo(cultureName);
            if (cultureName == "he-IL")
                culture.DateTimeFormat.Calendar = new HebrewCalendar();
            CultureInfo.CurrentCulture = culture;
            var formattingCalendar = culture.DateTimeFormat.Calendar;
            using var box = new ThemedDateBox();

            foreach (var input in new[] { DateTime.MinValue, DateTime.MaxValue })
            {
                box.Value = input;
                Invoke(box, "ShowCalendar");

                Assert.InRange(box.Value, formattingCalendar.MinSupportedDateTime.Date, formattingCalendar.MaxSupportedDateTime.Date);
                Assert.Equal(box.Value, GetField<MonthCalendar>(box, "_calendar").SelectionStart);
                var text = Assert.Single(box.Controls.OfType<TextBox>()).Text;
                Assert.True(DateTime.TryParse(text, culture, DateTimeStyles.None, out var parsed));
                Assert.Equal(box.Value, parsed.Date);
                GetField<ToolStripDropDown>(box, "_calendarDropDown").Close();
            }
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Committing_a_date_cannot_open_a_popup_after_a_handler_disables_or_disposes_the_owner(bool dispose)
    {
        WinFormsTestHelper.Run(() =>
        {
            using var box = new ThemedDateBox { Value = new DateTime(2026, 7, 10) };
            Assert.Single(box.Controls.OfType<TextBox>()).Text = new DateTime(2026, 7, 15).ToString("d", CultureInfo.CurrentCulture);
            box.ValueChanged += (_, _) =>
            {
                if (dispose)
                    box.Dispose();
                else
                    box.Enabled = false;
            };

            Invoke(box, "ShowCalendar");

            Assert.Null(typeof(ThemedDateBox).GetField("_calendarDropDown", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(box));
        });
    }

    private static T GetField<T>(ThemedDateBox box, string name) where T : class =>
        Assert.IsType<T>(typeof(ThemedDateBox).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(box));

    private static void Invoke(ThemedDateBox box, string method, params object?[] args) =>
        typeof(ThemedDateBox).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(box, args);
}
