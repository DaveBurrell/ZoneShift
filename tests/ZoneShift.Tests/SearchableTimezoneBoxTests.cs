using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TimezoneConverter;
using Xunit;

namespace ZoneShift.Tests;

public class SearchableTimezoneBoxTests
{
    private static readonly TimezoneOption[] Options =
    [
        new("UTC", "Coordinated Universal Time", "UTC"),
        new("IST", "India Standard Time", "India Standard Time"),
        new("PST", "Pacific Time", "Pacific Standard Time")
    ];

    [Fact]
    public void Pending_selection_survives_loading_and_handle_creation() => WinFormsTestHelper.Run(() =>
    {
        using var box = new SearchableTimezoneBox();
        Assert.False(box.SelectWindowsId("India Standard Time"));
        box.SetOptions(Options);
        Assert.Equal("India Standard Time", box.SelectedOption?.WindowsId);

        _ = box.Handle;
        Application.DoEvents();
        Assert.Equal("India Standard Time", box.SelectedOption?.WindowsId);
        Assert.Same(box.SelectedOption, box.SelectedItem);
    });

    [Fact]
    public void Searching_preserves_query_caret_and_committed_zone() => WinFormsTestHelper.Run(() =>
    {
        using var box = CreateBox();
        box.SelectWindowsId("India Standard Time");
        var changes = 0;
        box.SelectedOptionChanged += (_, _) => changes++;

        TypeQuery(box, "Ind", caret: 1);
        // Native edit notifications finish before the list is replaced.
        Assert.Equal(3, box.Items.Count);
        Application.DoEvents();

        Assert.Equal("Ind", box.Text);
        Assert.Equal(1, box.SelectionStart);
        Assert.Single(box.Items.Cast<TimezoneOption>());
        Assert.Equal("India Standard Time", box.SelectedOption?.WindowsId);
        Assert.Equal(0, changes);
    });

    [Fact]
    public void Unmatched_query_and_keyboard_navigation_keep_native_selection_valid() => WinFormsTestHelper.Run(() =>
    {
        using var box = CreateBox();
        box.SelectWindowsId("UTC");
        for (var i = 0; i < 25; i++)
        {
            TypeQuery(box, "no matching timezone");
            Application.DoEvents();
            Assert.Empty(box.Items);
            Assert.Equal(-1, box.SelectedIndex);
            Assert.Equal("no matching timezone", box.Text);
            Assert.False(box.DroppedDown);

            SendMessage(box.Handle, 0x0100, (nint)Keys.Down, 0); // WM_KEYDOWN
            InvokeKey(box, Keys.Escape);
            Application.DoEvents();
            Assert.Equal(3, box.Items.Count);
            Assert.Same(box.SelectedOption, box.SelectedItem);
            Assert.Equal("UTC", box.SelectedOption?.WindowsId);
        }
    });

    [Fact]
    public void Rapid_edits_are_coalesced_and_enter_commits_the_latest_query_once() => WinFormsTestHelper.Run(() =>
    {
        using var box = CreateBox();
        box.SelectWindowsId("UTC");
        var changes = 0;
        box.SelectedOptionChanged += (_, _) => changes++;

        TypeQuery(box, "P");
        TypeQuery(box, "PS");
        TypeQuery(box, "PST");
        InvokeKey(box, Keys.Enter);
        Application.DoEvents();

        Assert.Equal("Pacific Standard Time", box.SelectedOption?.WindowsId);
        Assert.Equal(3, box.Items.Count);
        Assert.Same(box.SelectedOption, box.SelectedItem);
        Assert.Equal(1, changes);
    });

    [Fact]
    public void Hover_does_not_commit_and_selection_callbacks_can_refresh_favorites() => WinFormsTestHelper.Run(() =>
    {
        using var box = CreateBox();
        box.SelectWindowsId("UTC");
        var changes = 0;
        box.SelectedOptionChanged += (_, _) =>
        {
            changes++;
            box.SetFavorites(["Pacific Standard Time"]);
            Assert.Equal("Pacific Standard Time", box.SelectedOption?.WindowsId);
        };

        var pacificIndex = box.Items.IndexOf(Options[2]);
        SendMessage(box.Handle, 0x014E, pacificIndex, 0); // CB_SETCURSEL
        Notify(box, 1); // CBN_SELCHANGE: native list highlight
        Assert.Equal("UTC", box.SelectedOption?.WindowsId);
        Assert.Equal(0, changes);

        Notify(box, 9); // CBN_SELENDOK: user commits
        Assert.Equal(1, changes);
        Application.DoEvents();
        Assert.Equal(1, changes);
        Assert.Same(Options[2], box.Items[0]);
        Assert.Same(box.SelectedOption, box.SelectedItem);
        Assert.StartsWith("* PST", box.Text);
    });

    [Fact]
    public void Favorites_preserve_active_search_and_do_not_raise_selection_changes() => WinFormsTestHelper.Run(() =>
    {
        using var box = CreateBox();
        box.SelectWindowsId("UTC");
        var changes = 0;
        box.SelectedOptionChanged += (_, _) => changes++;
        TypeQuery(box, "Ind");
        Application.DoEvents();

        box.SetFavorites(["India Standard Time"]);
        box.SetFavorites(["india standard time"]);
        Assert.Equal("Ind", box.Text);
        Assert.Single(box.Items.Cast<TimezoneOption>());
        Assert.Equal("UTC", box.SelectedOption?.WindowsId);
        Assert.Equal(0, changes);

        InvokeKey(box, Keys.Escape);
        Application.DoEvents();
        Assert.Equal("UTC", box.SelectedOption?.WindowsId);
        Assert.Same(Options[1], box.Items[0]);
    });

    [Fact]
    public void Leaving_invalid_input_restores_selection_and_exact_id_can_be_typed() => WinFormsTestHelper.Run(() =>
    {
        using var box = CreateBox();
        box.SelectWindowsId("UTC");
        TypeQuery(box, "does not exist");
        InvokeEvent(box, "OnLeave");
        Application.DoEvents();
        Assert.Same(box.SelectedOption, box.SelectedItem);
        Assert.StartsWith("UTC", box.Text);

        TypeQuery(box, "Pacific Standard Time");
        InvokeEvent(box, "OnLeave");
        Application.DoEvents();
        Assert.Equal("Pacific Standard Time", box.SelectedOption?.WindowsId);
    });

    [Fact]
    public void Opening_the_dropdown_does_not_reset_the_native_items_or_selection() => WinFormsTestHelper.Run(() =>
    {
        using var box = CreateBox();
        box.SelectWindowsId("UTC");
        var indexChanges = 0;
        box.SelectedIndexChanged += (_, _) => indexChanges++;

        for (var i = 0; i < 20; i++)
        {
            Notify(box, 7); // CBN_DROPDOWN
            Assert.Same(box.SelectedOption, box.SelectedItem);
            Assert.Equal(3, box.Items.Count);
        }
        Assert.Equal(0, indexChanges);
    });

    [Fact]
    public void Native_popup_cancellation_discards_highlight_and_pending_search() => WinFormsTestHelper.Run(() =>
    {
        using var box = CreateBox();
        box.SelectWindowsId("UTC");
        var changes = 0;
        box.SelectedOptionChanged += (_, _) => changes++;
        TypeQuery(box, "PST");
        Application.DoEvents();
        SendMessage(box.Handle, 0x014E, 0, 0); // CB_SETCURSEL
        Notify(box, 1); // CBN_SELCHANGE
        Notify(box, 10); // CBN_SELENDCANCEL
        InvokeEvent(box, "OnLeave");
        Application.DoEvents();

        Assert.Equal("UTC", box.SelectedOption?.WindowsId);
        Assert.Same(box.SelectedOption, box.SelectedItem);
        Assert.Equal(3, box.Items.Count);
        Assert.Equal(0, changes);
    });

    [Fact]
    public void Raw_native_selection_callbacks_cannot_rebuild_items_during_notification() => WinFormsTestHelper.Run(() =>
    {
        using var box = CreateBox();
        box.SelectWindowsId("UTC");
        var originalFirst = box.Items[0];
        box.SelectedIndexChanged += (_, _) =>
        {
            box.SetFavorites(["Pacific Standard Time"]);
            Assert.Same(originalFirst, box.Items[0]);
        };

        SendMessage(box.Handle, 0x014E, box.Items.IndexOf(Options[2]), 0); // CB_SETCURSEL
        Notify(box, 1); // CBN_SELCHANGE
        Application.DoEvents();
        Assert.Same(Options[2], box.Items[0]);
        Assert.Equal("UTC", box.SelectedOption?.WindowsId);
    });

    [Fact]
    public void Typing_in_the_native_edit_child_keeps_text_and_item_indices_consistent() => WinFormsTestHelper.Run(() =>
    {
        using var host = new Form { ShowInTaskbar = false };
        using var box = new SearchableTimezoneBox();
        host.Controls.Add(box);
        box.SetOptions(Options);
        box.SelectWindowsId("UTC");
        _ = host.Handle;
        _ = box.Handle;
        Application.DoEvents();
        var info = new ComboBoxInfo { Size = Marshal.SizeOf<ComboBoxInfo>() };
        Assert.True(GetComboBoxInfo(box.Handle, ref info));
        Assert.NotEqual(0, info.Edit);

        for (var i = 0; i < 10; i++)
        {
            foreach (var query in new[] { "Ind", "no matching timezone", "PST", "UTC" })
            {
                SendMessage(info.Edit, 0x00B1, 0, -1); // EM_SETSEL: replace the current native text
                foreach (var character in query)
                {
                    SendMessage(info.Edit, 0x0102, character, 0); // WM_CHAR: real edit notifications
                    Application.DoEvents();
                    Assert.InRange(box.SelectedIndex, -1, box.Items.Count - 1);
                }
                Assert.Equal(query, box.Text);
                Assert.Equal(query == "no matching timezone" ? 0 : 1, box.Items.Count);
                Assert.Equal("UTC", box.SelectedOption?.WindowsId);
                InvokeKey(box, Keys.Escape);
                Application.DoEvents();
                Assert.Same(box.SelectedOption, box.SelectedItem);
            }
        }
    });

    private static SearchableTimezoneBox CreateBox()
    {
        var box = new SearchableTimezoneBox();
        box.SetOptions(Options);
        _ = box.Handle;
        Application.DoEvents();
        return box;
    }

    private static void TypeQuery(SearchableTimezoneBox box, string query, int? caret = null)
    {
        box.SelectedIndex = -1;
        box.Text = query;
        box.SelectionStart = caret ?? query.Length;
        box.SelectionLength = 0;
        Notify(box, 6); // CBN_EDITUPDATE
    }

    private static void Notify(SearchableTimezoneBox box, int notification) =>
        SendMessage(box.Handle, 0x2111, notification << 16, box.Handle); // WM_REFLECT + WM_COMMAND

    private static void InvokeKey(SearchableTimezoneBox box, Keys key) =>
        typeof(SearchableTimezoneBox).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(box, [new KeyEventArgs(key)]);

    private static void InvokeEvent(SearchableTimezoneBox box, string name) =>
        typeof(SearchableTimezoneBox).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(box, [EventArgs.Empty]);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ComboBoxInfo
    {
        public int Size;
        public NativeRect ItemRect, ButtonRect;
        public int ButtonState;
        public nint Combo, Edit, List;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetComboBoxInfo(nint combo, ref ComboBoxInfo info);
}
