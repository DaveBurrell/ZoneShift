using System.Windows.Forms;
using TimezoneConverter;
using Xunit;

namespace ZoneShift.Tests;

[Collection("Desktop windows")]
public class TimeEntryComboTests
{
    [Fact]
    public void Typed_time_commits_once_on_enter_and_invalid_input_reverts() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out TimeEntryCombo combo);
        var changes = 0;
        combo.TimeChanged += (_, _) => changes++;
        host.FocusEditor(combo);

        host.ReplaceText(combo, "7.45pm");
        host.Key(combo, Keys.Enter);
        Assert.Equal(new TimeSpan(19, 45, 0), combo.TimeOfDay);
        Assert.Equal("7:45 PM", combo.Text);
        Assert.Equal(1, changes);

        host.FocusOther();
        Assert.Equal(1, changes);

        foreach (var invalid in new[] { "٧٣٠", "7::30pm", "2026-09-13", "25:00" })
        {
            host.FocusEditor(combo);
            host.ReplaceText(combo, invalid);
            host.FocusOther();
            Assert.Equal(new TimeSpan(19, 45, 0), combo.TimeOfDay);
            Assert.Equal("7:45 PM", combo.Text);
            Assert.Equal(1, changes);
        }
    });

    [Fact]
    public void Arrow_keys_step_to_the_neighbouring_half_hour_instead_of_jumping_to_midnight() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out TimeEntryCombo combo);
        var changes = 0;
        combo.TimeChanged += (_, _) => changes++;
        combo.TimeOfDay = new TimeSpan(19, 45, 0);
        host.FocusEditor(combo);

        host.Key(combo, Keys.Down);
        Assert.Equal(new TimeSpan(19, 30, 0), combo.TimeOfDay);
        host.Key(combo, Keys.Down);
        Assert.Equal(new TimeSpan(19, 0, 0), combo.TimeOfDay);
        host.Key(combo, Keys.Up);
        Assert.Equal(new TimeSpan(19, 30, 0), combo.TimeOfDay);
        host.Key(combo, Keys.PageUp);
        Assert.Equal(new TimeSpan(20, 30, 0), combo.TimeOfDay);
        Assert.Equal("8:30 PM", combo.Text);
        Assert.Equal(4, changes);

        combo.TimeOfDay = new TimeSpan(23, 40, 0);
        host.Key(combo, Keys.Up);
        Assert.Equal(TimeSpan.Zero, combo.TimeOfDay);
        host.Key(combo, Keys.Down);
        Assert.Equal(new TimeSpan(23, 30, 0), combo.TimeOfDay);
    });

    [Fact]
    public void Arrow_keys_commit_a_typed_time_before_stepping_from_it() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out TimeEntryCombo combo);
        host.FocusEditor(combo);
        host.ReplaceText(combo, "10:10");
        host.Key(combo, Keys.Up);
        Assert.Equal(new TimeSpan(10, 30, 0), combo.TimeOfDay);
    });

    [Fact]
    public void Preset_list_opens_on_the_current_time_and_enter_picks_the_highlight() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out TimeEntryCombo combo);
        var changes = 0;
        combo.TimeChanged += (_, _) => changes++;
        combo.TimeOfDay = new TimeSpan(19, 45, 0);

        host.ClickDropButton(combo);
        Assert.True(combo.IsDroppedDown);
        Assert.Equal(48, combo.Popup.Count);
        Assert.Equal(39, combo.Popup.HighlightIndex); // 7:30 PM
        Assert.InRange(39, combo.Popup.TopIndex, combo.Popup.TopIndex + combo.Popup.VisibleRows - 1);
        Assert.Equal(0, changes);

        host.Key(combo, Keys.Down);
        host.Key(combo, Keys.Enter);
        Assert.False(combo.IsDroppedDown);
        Assert.Equal(new TimeSpan(20, 0, 0), combo.TimeOfDay);
        Assert.Equal(1, changes);

        host.ClickDropButton(combo);
        host.ClickPopupRow(combo, combo.Popup.TopIndex);
        Assert.Equal(TimeSpan.FromMinutes(combo.Popup.TopIndex * 30), combo.TimeOfDay);
        Assert.False(combo.IsDroppedDown);
    });

    [Fact]
    public void Reapplying_configuration_preserves_a_partial_edit() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out TimeEntryCombo combo);
        host.FocusEditor(combo);
        host.ReplaceText(combo, "7:");

        combo.Configure(use24Hour: false, includeSeconds: false);
        combo.Configure(use24Hour: true, includeSeconds: false);

        Assert.Equal("7:", combo.Text);
        Assert.Equal("00:00", combo.Presets[0]);
    });

    [Fact]
    public void Format_changes_redraw_the_time_and_an_open_list() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out TimeEntryCombo combo);
        combo.TimeOfDay = new TimeSpan(23, 30, 0);
        host.ClickDropButton(combo);

        combo.Configure(use24Hour: true, includeSeconds: true);
        Assert.Equal("23:30:00", combo.Text);
        Assert.True(combo.IsDroppedDown);
        Assert.Equal(47, combo.Popup.HighlightIndex);
        Assert.Equal(48, combo.Presets.Count);
        Assert.Equal("23:30", combo.Presets[47]);
    });

    [Fact]
    public void Programmatic_time_changes_do_not_raise_events_or_overwrite_typing() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out TimeEntryCombo combo);
        var changes = 0;
        combo.TimeChanged += (_, _) => changes++;

        combo.TimeOfDay = TimeSpan.MaxValue;
        combo.Configure(use24Hour: true, includeSeconds: true);
        Assert.Equal(new TimeSpan(2, 48, 5), combo.TimeOfDay);
        Assert.Equal("02:48:05", combo.Text);
        Assert.Equal(0, changes);

        // A live clock updating the time while the user types must not erase their input.
        host.FocusEditor(combo);
        host.ReplaceText(combo, "9:1");
        combo.TimeOfDay = new TimeSpan(2, 48, 6);
        Assert.Equal("9:1", combo.Text);
        host.Key(combo, Keys.Enter);
        Assert.Equal(new TimeSpan(9, 1, 0), combo.TimeOfDay);
        Assert.Equal(1, changes);
    });

    [Fact]
    public void Live_updates_keep_a_whole_field_selection_so_typing_replaces_it() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out TimeEntryCombo combo);
        combo.Configure(use24Hour: false, includeSeconds: true);
        host.FocusEditor(combo);
        Assert.Equal(combo.Text.Length, combo.Editor.SelectionLength);

        combo.TimeOfDay = new TimeSpan(10, 0, 1);
        Assert.Equal(combo.Text.Length, combo.Editor.SelectionLength);

        host.Type(combo, "3pm");
        host.Key(combo, Keys.Enter);
        Assert.Equal(new TimeSpan(15, 0, 0), combo.TimeOfDay);
    });

    [Fact]
    public void Selection_handler_can_change_format_then_dispose_owner() => WinFormsTestHelper.Run(() =>
    {
        var host = FieldHost.Create(out TimeEntryCombo combo);
        combo.TimeChanged += (_, _) =>
        {
            combo.Configure(use24Hour: true, includeSeconds: true);
            host.Form.Dispose();
        };

        host.ClickDropButton(combo);
        host.Key(combo, Keys.End);
        host.ClickPopupRow(combo, combo.Popup.TopIndex + 1);
        Application.DoEvents();

        Assert.True(host.Form.IsDisposed);
        Assert.True(combo.IsDisposed);
        host.Dispose();
    });

    [Fact]
    public void Typing_with_the_preset_list_open_commits_the_typed_time() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out TimeEntryCombo combo);
        combo.TimeOfDay = new TimeSpan(10, 0, 0);
        host.ClickDropButton(combo);
        Assert.True(combo.IsDroppedDown);

        host.Type(combo, "3:45pm");
        Assert.False(combo.IsDroppedDown);
        host.Key(combo, Keys.Enter);
        Assert.Equal(new TimeSpan(15, 45, 0), combo.TimeOfDay);
    });

    [Fact]
    public void Typing_survives_clock_updates_while_the_window_is_inactive() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out TimeEntryCombo combo);
        combo.Configure(use24Hour: false, includeSeconds: true);
        host.FocusEditor(combo);
        host.ReplaceText(combo, "11:1");

        // No real focus (as after Alt+Tab), but the typing is still pending.
        host.FocusOther();
        combo.Text = "11:1";
        combo.TimeOfDay = new TimeSpan(4, 0, 1);
        Assert.Equal("11:1", combo.Text);
    });
}
