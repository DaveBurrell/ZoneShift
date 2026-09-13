using System.Reflection;
using System.Windows.Forms;
using TimezoneConverter;
using Xunit;

namespace ZoneShift.Tests;

public class TimeEntryComboTests
{
    [Fact]
    public void Typed_time_commits_once_on_enter_and_invalid_input_reverts() => WinFormsTestHelper.Run(() =>
    {
        using var combo = CreateCombo();
        var changes = 0;
        combo.TimeChanged += (_, _) => changes++;

        combo.Text = "7.45pm";
        RaiseKeyDown(combo, Keys.Enter);
        Assert.Equal(new TimeSpan(19, 45, 0), combo.TimeOfDay);
        Assert.Equal(1, changes);

        RaiseLeave(combo);
        Assert.Equal(1, changes);

        foreach (var invalid in new[] { "٧٣٠", "7::30pm", "2026-09-13", "25:00" })
        {
            combo.Text = invalid;
            RaiseLeave(combo);
            Assert.Equal(new TimeSpan(19, 45, 0), combo.TimeOfDay);
            Assert.Equal("7:45 PM", combo.Text);
            Assert.Equal(1, changes);
        }
    });

    [Fact]
    public void Reapplying_configuration_preserves_a_partial_edit() => WinFormsTestHelper.Run(() =>
    {
        using var combo = CreateCombo();
        combo.Text = "7:";

        combo.Configure(use24Hour: false, includeSeconds: false);

        Assert.Equal("7:", combo.Text);
        Assert.Equal(48, combo.Items.Count);
    });

    [Fact]
    public void Selecting_presets_and_changing_format_keeps_native_selection_valid() => WinFormsTestHelper.Run(() =>
    {
        using var combo = CreateCombo();
        var changes = 0;
        combo.TimeChanged += (_, _) => changes++;

        for (var index = 0; index < 48; index++)
        {
            combo.SelectedIndex = index;
            RaiseSelectionCommitted(combo);
            Assert.Equal(TimeSpan.FromMinutes(index * 30), combo.TimeOfDay);

            combo.Configure(use24Hour: index % 2 == 0, includeSeconds: index % 3 == 0);
            Assert.Equal(48, combo.Items.Count);
            Assert.True(TimeEntryCombo.TryParse(combo.Text, out var displayed));
            Assert.Equal(combo.TimeOfDay, displayed);
        }

        Assert.Equal(47, changes);
    });

    [Fact]
    public void Format_change_from_a_selection_handler_defers_replacing_native_items() => WinFormsTestHelper.Run(() =>
    {
        using var combo = CreateCombo();
        var firstPreset = combo.Items[0];
        combo.TimeChanged += (_, _) =>
        {
            combo.Configure(use24Hour: true, includeSeconds: true);
            Assert.Same(firstPreset, combo.Items[0]);
        };

        combo.SelectedIndex = 47;
        RaiseSelectionCommitted(combo);
        Application.DoEvents();

        Assert.Equal(new TimeSpan(23, 30, 0), combo.TimeOfDay);
        Assert.Equal("23:30:00", combo.Text);
        Assert.Equal("00:00", combo.Items[0]);
        Assert.Equal(48, combo.Items.Count);
    });

    [Fact]
    public void Programmatic_time_changes_do_not_raise_user_change_events() => WinFormsTestHelper.Run(() =>
    {
        using var combo = CreateCombo();
        var changes = 0;
        combo.TimeChanged += (_, _) => changes++;

        combo.TimeOfDay = TimeSpan.MaxValue;
        combo.Configure(use24Hour: true, includeSeconds: true);

        Assert.Equal(new TimeSpan(2, 48, 5), combo.TimeOfDay);
        Assert.Equal("02:48:05", combo.Text);
        Assert.Equal(0, changes);
    });

    [Fact]
    public void Selection_handler_can_change_format_then_dispose_owner() => WinFormsTestHelper.Run(() =>
    {
        using var owner = new Form();
        using var combo = CreateCombo();
        owner.Controls.Add(combo);
        _ = owner.Handle;
        combo.TimeChanged += (_, _) =>
        {
            combo.Configure(use24Hour: true, includeSeconds: true);
            owner.Dispose();
        };

        combo.SelectedIndex = 47;
        RaiseSelectionCommitted(combo);
        Application.DoEvents();

        Assert.True(owner.IsDisposed);
        Assert.True(combo.IsDisposed);
    });

    private static TimeEntryCombo CreateCombo()
    {
        var combo = new TimeEntryCombo();
        _ = combo.Handle;
        return combo;
    }

    private static void RaiseSelectionCommitted(TimeEntryCombo combo) =>
        typeof(ComboBox).GetMethod("OnSelectionChangeCommitted", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(combo, [EventArgs.Empty]);

    private static void RaiseLeave(TimeEntryCombo combo) =>
        typeof(Control).GetMethod("OnLeave", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(combo, [EventArgs.Empty]);

    private static void RaiseKeyDown(TimeEntryCombo combo, Keys key) =>
        typeof(Control).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(combo, [new KeyEventArgs(key)]);
}
