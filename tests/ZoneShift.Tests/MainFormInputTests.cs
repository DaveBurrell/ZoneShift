using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using TimezoneConverter;
using TimezoneConverter.Services;
using Xunit;

namespace ZoneShift.Tests;

// These tests temporarily redirect the process-wide settings path.
[CollectionDefinition("Main form inputs", DisableParallelization = true)]
public sealed class MainFormInputCollection;

[Collection("Main form inputs")]
public sealed class MainFormInputTests : IDisposable
{
    private readonly string _scratchDir = Path.Combine(Path.GetTempPath(), "ZoneShift.Tests", Guid.NewGuid().ToString("N"));
    private readonly string? _previousOverride = Environment.GetEnvironmentVariable(AppSettings.DirectoryOverrideVariable);

    public MainFormInputTests() =>
        Environment.SetEnvironmentVariable(AppSettings.DirectoryOverrideVariable, _scratchDir);

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(AppSettings.DirectoryOverrideVariable, _previousOverride);
        if (Directory.Exists(_scratchDir))
            Directory.Delete(_scratchDir, recursive: true);
    }

    [Fact]
    public void Leaving_live_mode_freezes_the_displayed_source_time_and_persists_mode() => WinFormsTestHelper.Run(() =>
    {
        using var form = new MainForm(Settings(live: true));
        var snapshot = Field<ConversionSnapshot>(form, "_lastSnapshot");
        var date = Field<ThemedDateBox>(form, "_datePicker");
        var time = Field<TimeEntryCombo>(form, "_timeEntry");
        Assert.Equal(snapshot.InputWallTime.Date, date.Value);
        Assert.Equal(TimeParser.Normalize(snapshot.InputWallTime.TimeOfDay), time.TimeOfDay);

        Field<ThemedCheckBox>(form, "_liveModeCheck").Checked = false;
        var frozen = Field<ConversionSnapshot>(form, "_lastSnapshot");
        Assert.Equal(date.Value + time.TimeOfDay, frozen.InputWallTime);
        Assert.True(time.Enabled);
        Assert.True(date.Enabled);
        Assert.False(AppSettings.Load().LiveMode);

        Field<ThemedCheckBox>(form, "_liveModeCheck").Checked = true;
        // Live mode keeps the fields editable; committing a value switches to a custom time.
        Assert.True(time.Enabled);
        Assert.True(date.Enabled);
        Assert.True(AppSettings.Load().LiveMode);
    });

    [Fact]
    public void Committing_a_time_during_live_mode_switches_to_that_custom_time() => WinFormsTestHelper.Run(() =>
    {
        using var form = new MainForm(Settings(live: true));
        var date = Field<ThemedDateBox>(form, "_datePicker");
        var time = Field<TimeEntryCombo>(form, "_timeEntry");
        _ = time.Handle;
        time.Text = "3:15 PM";
        typeof(Control).GetMethod("OnLeave", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(time, [EventArgs.Empty]);

        Assert.False(Field<bool>(form, "_liveMode"));
        Assert.False(Field<ThemedCheckBox>(form, "_liveModeCheck").Checked);
        Assert.Equal(new TimeSpan(15, 15, 0), time.TimeOfDay);
        Assert.Equal("3:15 PM", time.Text);
        Assert.Equal(date.Value + new TimeSpan(15, 15, 0), Field<ConversionSnapshot>(form, "_lastSnapshot").InputWallTime);
        Assert.False(AppSettings.Load().LiveMode);
    });

    [Fact]
    public void Leaving_live_mode_drops_the_live_clock_seconds() => WinFormsTestHelper.Run(() =>
    {
        using var form = new MainForm(Settings(live: true));
        var time = Field<TimeEntryCombo>(form, "_timeEntry");
        Field<ThemedCheckBox>(form, "_liveModeCheck").Checked = false;
        Assert.Equal(0, time.TimeOfDay.Seconds);
        Assert.Equal(0, Field<ConversionSnapshot>(form, "_lastSnapshot").InputWallTime.Second);
    });

    [Fact]
    public void Time_and_timezone_commits_update_the_conversion_and_survive_format_and_favorite_changes() => WinFormsTestHelper.Run(() =>
    {
        using var form = new MainForm(Settings(live: false));
        var date = Field<ThemedDateBox>(form, "_datePicker");
        var time = Field<TimeEntryCombo>(form, "_timeEntry");
        date.Value = new DateTime(2026, 9, 13);
        _ = time.Handle;
        time.Text = "7:30 PM";
        typeof(Control).GetMethod("OnLeave", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(time, [EventArgs.Empty]);

        var expected = new DateTime(2026, 9, 13, 19, 30, 0, DateTimeKind.Utc);
        Assert.Equal(expected, Field<ConversionSnapshot>(form, "_lastSnapshot").Utc);

        var format = Field<SegmentedToggle>(form, "_formatToggle");
        var button = format.Controls.OfType<Button>().Single(b => b.Text == "24-hour");
        typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(button, [EventArgs.Empty]);
        Assert.Equal("19:30", time.Text);
        Assert.Equal(expected, Field<ConversionSnapshot>(form, "_lastSnapshot").Utc);

        var rows = Field<List<TargetZoneRow>>(form, "_targetRows");
        Assert.Equal("*", rows[0].FavoriteButton.Text);
        Assert.True(rows[0].SelectWindowsId("Tokyo Standard Time"));
        Assert.Equal("o", rows[0].FavoriteButton.Text);
        var target = Assert.Single(Field<ConversionSnapshot>(form, "_lastSnapshot").Targets);
        Assert.Equal(new DateTime(2026, 9, 14, 4, 30, 0), target.LocalWallTime);

        rows[0].ApplyFavorites(["Tokyo Standard Time", "UTC"]);
        Assert.Equal("Tokyo Standard Time", rows[0].SelectedOption!.WindowsId);
        Assert.Equal(expected, Field<ConversionSnapshot>(form, "_lastSnapshot").Utc);
        Assert.Equal("Tokyo Standard Time", Assert.Single(AppSettings.Load().TargetWindowsIds!));

        Field<SearchableTimezoneBox>(form, "_reverseSourceTimezone").SelectWindowsId("India Standard Time");
        Assert.Equal(expected.AddHours(-5.5), Field<ConversionSnapshot>(form, "_lastSnapshot").Utc);

        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
    });

    [Fact]
    public void An_unavailable_saved_timezone_gets_a_usable_fallback() => WinFormsTestHelper.Run(() =>
    {
        var settings = Settings(live: false);
        settings.TargetWindowsIds = ["Unavailable zone"];
        using var form = new MainForm(settings);
        var target = Assert.Single(Field<ConversionSnapshot>(form, "_lastSnapshot").Targets);
        Assert.Equal("UTC", target.WindowsId);
    });

    private static AppSettings Settings(bool live) => new()
    {
        LiveMode = live,
        ConvertToLocal = true,
        ReverseSourceWindowsId = "UTC",
        TargetWindowsIds = ["India Standard Time"],
        FavoriteWindowsIds = ["India Standard Time"],
        HasSeenOnboarding = true,
        CloseToTray = false
    };

    private static T Field<T>(MainForm form, string name) =>
        (T)typeof(MainForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;

    [Fact]
    public void Saved_overlay_lock_and_compact_survive_a_restart() => WinFormsTestHelper.Run(() =>
    {
        var settings = Settings(live: false);
        settings.OverlayVisible = true;
        settings.OverlayOpacity = 0.7;
        settings.OverlayLocked = true;
        settings.OverlayCompact = true;

        using var form = new MainForm(settings);
        var overlay = Field<OverlayForm>(form, "_overlay");
        Assert.True(overlay.IsLocked);
        Assert.True(overlay.IsCompact);
        Assert.Equal(0.7, overlay.OverlayOpacity, 2);
        Assert.True(settings.OverlayLocked);
        Assert.True(settings.OverlayCompact);
    });

    [Fact]
    public void Clicking_live_off_after_typing_a_time_keeps_that_time() => WinFormsTestHelper.Run(() =>
    {
        using var form = new MainForm(Settings(live: true));
        form.Show();
        form.Location = new Point(-3000, -3000);
        Application.DoEvents();
        var time = Field<TimeEntryCombo>(form, "_timeEntry");
        var live = Field<ThemedCheckBox>(form, "_liveModeCheck");

        time.Editor.Focus();
        Application.DoEvents();
        time.Editor.SelectAll();
        foreach (var ch in "3pm")
            PostMessage(time.Editor.Handle, 0x0102, ch, 1); // WM_CHAR
        Application.DoEvents();

        // A real click: focus moves to the box on mouse-down (committing 3pm), it toggles on mouse-up.
        PostMessage(live.Handle, 0x0201, 1, (8 << 16) | 8);
        PostMessage(live.Handle, 0x0202, 0, (8 << 16) | 8);
        Application.DoEvents();

        Assert.False(Field<bool>(form, "_liveMode"));
        Assert.False(live.Checked);
        Assert.Equal(new TimeSpan(15, 0, 0), time.TimeOfDay);

        // The next click is a genuine request to go live again.
        PostMessage(live.Handle, 0x0201, 1, (8 << 16) | 8);
        PostMessage(live.Handle, 0x0202, 0, (8 << 16) | 8);
        Application.DoEvents();
        Assert.True(Field<bool>(form, "_liveMode"));
        Assert.True(live.Checked);
    });

    [Fact]
    public void The_live_clock_never_overwrites_a_time_being_typed() => WinFormsTestHelper.Run(() =>
    {
        using var form = new MainForm(Settings(live: true));
        var time = Field<TimeEntryCombo>(form, "_timeEntry");
        time.Text = "9:1";
        typeof(MainForm).GetMethod("RefreshDisplays", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(form, null);
        Assert.Equal("9:1", time.Text);
        Assert.True(Field<bool>(form, "_liveMode"));
    });

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PostMessage(nint window, int message, nint wParam, nint lParam);
}
