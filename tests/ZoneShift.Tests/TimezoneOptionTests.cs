using System.Windows.Forms;
using TimezoneConverter;
using Xunit;

namespace ZoneShift.Tests;

[Collection("Desktop windows")]
public class TimezoneOptionTests
{
    [Theory]
    [InlineData("Vladivostok Standard Time", "Vladivostok")]
    [InlineData("Pacific Standard Time (Mexico)", "Pacific (Mexico)")]
    [InlineData("Russia Time Zone 10", "Russia Time Zone 10")]
    [InlineData("UTC-11", "UTC-11")]
    public void Generated_labels_drop_the_suffix_instead_of_cutting_words(string id, string expected) =>
        Assert.Equal(expected, TimezoneOption.ShortLabel(id));

    [Fact]
    public void Every_system_zone_gets_a_short_distinct_readable_label()
    {
        var options = TimezoneOption.BuildFullList();
        Assert.All(options, o =>
        {
            Assert.InRange(o.Abbreviation.Length, 1, TimezoneOption.MaxLabelLength);
            Assert.DoesNotContain("…", o.Abbreviation);
        });
        Assert.Equal(options.Count, options.Select(o => o.WindowsId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Theory]
    [InlineData("sydney", "AEST")]
    [InlineData("mumbai", "IST")]
    [InlineData("new york", "EST")]
    [InlineData("los angeles", "PST")]
    [InlineData("tokyo", "JST")]
    [InlineData("london", "GMT")]
    public void Cities_find_their_curated_zone_first(string query, string abbreviation) => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SetOptions(TimezoneOption.BuildFullList());
        box.SelectAbbreviation("UTC");
        host.FocusEditor(box);
        host.ReplaceText(box, query);
        Assert.NotEmpty(box.Suggestions);
        Assert.Equal(abbreviation, box.Suggestions[0].Abbreviation);
    });

    [Fact]
    public void Clicking_back_into_a_focused_zone_field_selects_it_for_a_new_search() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SelectWindowsId("UTC");
        host.FocusEditor(box);
        box.Editor.Select(3, 0);

        const int wmLButtonDown = 0x0201, wmLButtonUp = 0x0202;
        Native.SendMessage(box.Editor.Handle, wmLButtonDown, 1, (5 << 16) | 20);
        Native.SendMessage(box.Editor.Handle, wmLButtonUp, 0, (5 << 16) | 20);
        Application.DoEvents();
        Assert.Equal(box.Text.Length, box.Editor.SelectionLength);

        host.Type(box, "pst");
        host.Key(box, Keys.Enter);
        Assert.Equal("Pacific Standard Time", box.SelectedOption?.WindowsId);
    });
}
