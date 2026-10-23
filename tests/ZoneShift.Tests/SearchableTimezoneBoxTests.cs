using System.Reflection;
using System.Windows.Forms;
using TimezoneConverter;
using Xunit;

namespace ZoneShift.Tests;

// Shown windows compete for activation, and a field closes its list when its form deactivates.
[CollectionDefinition("Desktop windows", DisableParallelization = true)]
public sealed class DesktopWindowCollection;

[Collection("Desktop windows")]
public class SearchableTimezoneBoxTests
{
    private static readonly TimezoneOption[] Options =
    [
        new("UTC", "Coordinated Universal Time", "UTC"),
        new("IST", "India Standard Time", "India Standard Time"),
        new("PST", "Pacific Time", "Pacific Standard Time"),
        new("Pacific (Mexico)", "(UTC-08:00) Baja California", "Pacific Standard Time (Mexico)"),
        new("GMT", "Greenwich Mean Time (London)", "GMT Standard Time"),
        new("Colombia", "(UTC-05:00) Bogota, Lima, Quito", "SA Pacific Standard Time")
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
        Assert.Equal("IST - India Standard Time", box.Text);
    });

    [Fact]
    public void Typing_after_focusing_replaces_the_zone_name_and_filters_as_you_type() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SelectWindowsId("UTC");
        var changes = 0;
        box.SelectedOptionChanged += (_, _) => changes++;

        host.FocusEditor(box);
        Assert.Equal(box.Text.Length, box.Editor.SelectionLength);

        host.Type(box, "p");
        Assert.True(box.IsDroppedDown);
        Assert.Equal("p", box.Text);
        // Curated zones lead equally good matches; "Colombia" matches through "SA Pacific".
        Assert.Equal(["PST", "Colombia", "Pacific (Mexico)"], box.Suggestions.Select(o => o.Abbreviation));

        host.Type(box, "st");
        Assert.Equal("PST", Assert.Single(box.Suggestions).Abbreviation);
        Assert.Equal(1, box.Popup.VisibleRows);
        Assert.Equal(box.Popup.RowHeight + 2, box.Popup.Height);
        Assert.Equal("UTC", box.SelectedOption?.WindowsId);
        Assert.Equal(0, changes);

        host.Key(box, Keys.Enter);
        Assert.Equal("Pacific Standard Time", box.SelectedOption?.WindowsId);
        Assert.Equal("PST - Pacific Time", box.Text);
        Assert.False(box.IsDroppedDown);
        Assert.False(box.IsSearching);
        Assert.Equal(1, changes);
    });

    [Fact]
    public void Search_words_match_in_any_order_and_rank_whole_words_first() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SelectWindowsId("UTC");
        host.FocusEditor(box);

        host.Type(box, "lon");
        // "London" starts a word; "Colombia"/"Lima" do not contain "lon" at all, so only GMT.
        Assert.Equal("GMT", Assert.Single(box.Suggestions).Abbreviation);

        host.ReplaceText(box, "lima bogota");
        Assert.Equal("Colombia", Assert.Single(box.Suggestions).Abbreviation);

        // An exact abbreviation outranks everything after an exact name.
        host.ReplaceText(box, "pst");
        Assert.Equal("PST", box.Suggestions[0].Abbreviation);
        host.ReplaceText(box, "pacific");
        Assert.Equal("PST", box.Suggestions[0].Abbreviation);

        // Editing the committed text down to a prefix still finds the zone.
        host.ReplaceText(box, "IST - India");
        Assert.Equal("IST", Assert.Single(box.Suggestions).Abbreviation);
    });

    [Fact]
    public void Arrow_keys_move_the_highlight_and_escape_discards_the_search() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SelectWindowsId("UTC");
        var changes = 0;
        box.SelectedOptionChanged += (_, _) => changes++;
        host.FocusEditor(box);

        host.Key(box, Keys.Down);
        Assert.True(box.IsDroppedDown);
        Assert.Equal(Options.Length, box.Suggestions.Count);
        Assert.Same(box.SelectedOption, box.Suggestions[box.Popup.HighlightIndex]);

        host.Type(box, "pa");
        Assert.Equal(0, box.Popup.HighlightIndex);
        host.Key(box, Keys.Down);
        Assert.Equal(1, box.Popup.HighlightIndex);

        host.Key(box, Keys.Escape);
        Assert.False(box.IsDroppedDown);
        Assert.Equal("UTC - Coordinated Universal Time", box.Text);
        Assert.Equal(0, changes);

        host.Type(box, "pa");
        host.Key(box, Keys.Down);
        var second = box.Suggestions[1];
        host.Key(box, Keys.Enter);
        Assert.Same(second, box.SelectedOption);
        Assert.Equal(1, changes);
    });

    [Fact]
    public void An_unmatched_search_shows_an_empty_list_and_enter_restores_the_zone() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SelectWindowsId("UTC");
        host.FocusEditor(box);

        for (var i = 0; i < 10; i++)
        {
            host.ReplaceText(box, "no matching timezone");
            Assert.Empty(box.Suggestions);
            Assert.True(box.IsDroppedDown);
            Assert.Equal(-1, box.Popup.HighlightIndex);

            host.Key(box, Keys.Down);
            host.Key(box, Keys.Enter);
            Assert.False(box.IsDroppedDown);
            Assert.Equal("UTC", box.SelectedOption?.WindowsId);
            Assert.Equal("UTC - Coordinated Universal Time", box.Text);
        }
    });

    [Fact]
    public void Leaving_commits_the_best_match_or_restores_the_zone() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SelectWindowsId("UTC");

        host.FocusEditor(box);
        host.ReplaceText(box, "does not exist");
        host.FocusOther();
        Assert.Equal("UTC", box.SelectedOption?.WindowsId);
        Assert.False(box.IsDroppedDown);
        Assert.StartsWith("UTC", box.Text);

        host.FocusEditor(box);
        host.ReplaceText(box, "Pacific Standard Time");
        host.FocusOther();
        Assert.Equal("Pacific Standard Time", box.SelectedOption?.WindowsId);
    });

    [Fact]
    public void Clicking_a_row_commits_it_and_keeps_focus_in_the_field() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SelectWindowsId("UTC");
        host.FocusEditor(box);
        host.Type(box, "i");

        var row = box.Suggestions.ToList().FindIndex(o => o.Abbreviation == "IST");
        Assert.True(row >= 0);
        host.ClickPopupRow(box, row);

        Assert.Equal("India Standard Time", box.SelectedOption?.WindowsId);
        Assert.False(box.IsDroppedDown);
        Assert.True(box.Editor.Focused);
    });

    [Fact]
    public void Drop_button_toggles_the_full_list_and_clicks_elsewhere_close_it() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SelectWindowsId("GMT Standard Time");

        host.ClickDropButton(box);
        Assert.True(box.IsDroppedDown);
        Assert.Equal(Options.Length, box.Suggestions.Count);
        Assert.True(box.Editor.Focused);

        host.ClickDropButton(box);
        Assert.False(box.IsDroppedDown);

        host.ClickDropButton(box);
        Assert.True(box.IsDroppedDown);
        host.ClickBackground();
        Assert.False(box.IsDroppedDown);
        Assert.Equal("GMT Standard Time", box.SelectedOption?.WindowsId);
    });

    [Fact]
    public void Favorites_lead_the_list_and_changing_them_keeps_an_active_search() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SelectWindowsId("UTC");
        var changes = 0;
        box.SelectedOptionChanged += (_, _) => changes++;
        host.FocusEditor(box);
        host.Type(box, "Ind");

        box.SetFavorites(["India Standard Time"]);
        box.SetFavorites(["india standard time"]);
        Assert.Equal("Ind", box.Text);
        Assert.True(box.IsSearching);
        Assert.Equal("IST", Assert.Single(box.Suggestions).Abbreviation);
        Assert.Equal("UTC", box.SelectedOption?.WindowsId);
        Assert.Equal(0, changes);

        host.Key(box, Keys.Escape);
        host.Key(box, Keys.Down);
        Assert.Equal("IST", box.Suggestions[0].Abbreviation);
        Assert.Equal("UTC", box.SelectedOption?.WindowsId);
    });

    [Fact]
    public void Selection_callbacks_can_change_favorites_and_dispose_the_field() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SelectWindowsId("UTC");
        box.SelectedOptionChanged += (_, _) =>
        {
            box.SetFavorites(["Pacific Standard Time"]);
            box.Dispose();
        };
        host.FocusEditor(box);
        host.Type(box, "pst");
        host.Key(box, Keys.Enter);
        Application.DoEvents();

        Assert.True(box.IsDisposed);
        Assert.False(box.Popup.Visible);
    });

    [Fact]
    public void Disabling_or_hiding_the_field_closes_its_list() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SelectWindowsId("UTC");
        host.ClickDropButton(box);
        Assert.True(box.IsDroppedDown);

        box.Enabled = false;
        Assert.False(box.IsDroppedDown);

        box.Enabled = true;
        host.ClickDropButton(box);
        Assert.True(box.IsDroppedDown);
        box.Visible = false;
        Assert.False(box.IsDroppedDown);
    });

    [Fact]
    public void Clicking_another_control_commits_the_highlighted_row_like_tab_does() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SelectWindowsId("UTC");
        host.FocusEditor(box);
        host.Type(box, "pacific");
        host.Key(box, Keys.Down);
        var highlighted = box.Suggestions[1];

        host.ClickOther();
        Assert.Same(highlighted, box.SelectedOption);
        Assert.False(box.IsDroppedDown);
    });

    [Fact]
    public void Alt_F4_still_closes_the_window_while_a_field_has_focus() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.SelectWindowsId("UTC");
        host.FocusEditor(box);

        host.PostKey(box, Keys.F4);
        Assert.True(box.IsDroppedDown);
        host.PostKey(box, Keys.F4);
        Assert.False(box.IsDroppedDown);

        host.PostKey(box, Keys.F4, alt: true);
        Assert.True(host.Form.IsDisposed);
    });

    [Fact]
    public void The_list_stays_on_screen_when_the_field_sits_at_the_bottom() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        var area = Screen.PrimaryScreen!.WorkingArea;
        host.Form.Location = new System.Drawing.Point(area.Left + 20, area.Bottom - 60);
        Application.DoEvents();
        box.SelectWindowsId("UTC");

        host.ClickDropButton(box);
        Assert.True(box.IsDroppedDown);
        var list = box.Popup.Bounds;
        Assert.True(area.Contains(list), $"{list} is outside {area}");
        Assert.True(list.Bottom <= box.RectangleToScreen(box.ClientRectangle).Top);
    });

    [Fact]
    public void Screen_readers_get_the_field_caption() => WinFormsTestHelper.Run(() =>
    {
        using var host = FieldHost.Create(out SearchableTimezoneBox box);
        box.FieldLabel = "Clock timezone";
        Assert.Equal("Clock timezone", box.Editor.AccessibleName);
        Assert.Equal(AccessibleRole.ComboBox, box.AccessibleRole);
    });
}

/// <summary>
/// Shows a field on an off-screen form and drives it with the window messages real input sends,
/// so editor notifications, focus changes and the popup behave as they do for a user.
/// </summary>
internal sealed class FieldHost : IDisposable
{
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmChar = 0x0102;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;

    private readonly Form _form;
    private readonly TextBox _other;

    private FieldHost(Form form, TextBox other)
    {
        _form = form;
        _other = other;
    }

    public Form Form => _form;

    public static FieldHost Create<T>(out T field) where T : Control, new()
    {
        var form = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new System.Drawing.Point(-2000, -2000),
            Size = new System.Drawing.Size(500, 300)
        };
        field = new T { Location = new System.Drawing.Point(10, 10), Size = new System.Drawing.Size(300, 26) };
        if (field is SearchableTimezoneBox box)
            box.SetOptions(SearchableTimezoneBoxTestsOptions.All);
        var other = new TextBox { Location = new System.Drawing.Point(10, 200) };
        form.Controls.Add(field);
        form.Controls.Add(other);
        form.Show();
        form.Activate();
        // Showing focuses the first input; start elsewhere so focusing the field is a real change.
        other.Focus();
        Application.DoEvents();
        return new FieldHost(form, other);
    }

    public void FocusEditor(DropDownField field)
    {
        field.Editor.Focus();
        Application.DoEvents();
    }

    public void FocusOther()
    {
        _other.Focus();
        Application.DoEvents();
    }

    public void Type(DropDownField field, string text)
    {
        foreach (var ch in text)
            Native.SendMessage(field.Editor.Handle, WmChar, ch, 1);
        Application.DoEvents();
    }

    public void ReplaceText(DropDownField field, string text)
    {
        field.Editor.SelectAll();
        Type(field, text);
    }

    public void Key(DropDownField field, Keys key)
    {
        var editor = field.Editor.Handle;
        Native.SendMessage(editor, WmKeyDown, (nint)key, 1);
        Native.SendMessage(editor, WmKeyUp, (nint)key, unchecked((nint)0xC0000001));
        Application.DoEvents();
    }

    /// <summary>Posts the key through the message loop, so dialog-key processing runs as for a user.</summary>
    public void PostKey(DropDownField field, Keys key, bool alt = false)
    {
        const int wmSysKeyDown = 0x0104, wmSysKeyUp = 0x0105;
        var down = alt ? wmSysKeyDown : WmKeyDown;
        var up = alt ? wmSysKeyUp : WmKeyUp;
        var context = alt ? 1 << 29 : 0;
        var editor = field.Editor.Handle;

        // Posted messages do not update the key state that WinForms and DefWindowProc read for
        // modifiers, so hold Alt in this thread's keyboard state while they are processed.
        var saved = new byte[256];
        Native.GetKeyboardState(saved);
        if (alt)
        {
            var held = (byte[])saved.Clone();
            held[(int)Keys.Menu] = 0x80;
            Native.SetKeyboardState(held);
        }

        try
        {
            Native.PostMessage(editor, down, (nint)key, 1 | context);
            Application.DoEvents();
        }
        finally
        {
            if (alt)
                Native.SetKeyboardState(saved);
        }

        if (!field.IsDisposed)
            Native.PostMessage(editor, up, (nint)key, unchecked((nint)(0xC0000001 | (uint)context)));
        Application.DoEvents();
    }

    /// <summary>Clicks the other input, moving focus there as a real click does.</summary>
    public void ClickOther() => Click(_other, new System.Drawing.Point(5, 5));

    public void ClickDropButton(DropDownField field) =>
        Click(field, new System.Drawing.Point(field.Width - 10, field.Height / 2));

    public void ClickPopupRow(DropDownField field, int index)
    {
        var popup = field.Popup;
        var y = 1 + (index - popup.TopIndex) * popup.RowHeight + popup.RowHeight / 2;
        Click(popup, new System.Drawing.Point(20, y));
    }

    /// <summary>Mouse-down on the form itself, which takes no focus — like clicking a card or label.</summary>
    public void ClickBackground()
    {
        var point = new System.Drawing.Point(400, 150);
        Native.PostMessage(_form.Handle, WmLButtonDown, 1, MakeLParam(point));
        Native.PostMessage(_form.Handle, WmLButtonUp, 0, MakeLParam(point));
        Application.DoEvents();
    }

    private static void Click(Control target, System.Drawing.Point point)
    {
        // Posted, not sent, so they pass through the message loop and its filters as real input does.
        Native.PostMessage(target.Handle, WmLButtonDown, 1, MakeLParam(point));
        Native.PostMessage(target.Handle, WmLButtonUp, 0, MakeLParam(point));
        Application.DoEvents();
    }

    private static nint MakeLParam(System.Drawing.Point p) => (p.Y << 16) | (p.X & 0xffff);

    public void Dispose()
    {
        if (!_form.IsDisposed)
            _form.Dispose();
        Application.DoEvents();
    }
}

internal static class SearchableTimezoneBoxTestsOptions
{
    public static TimezoneOption[] All => (TimezoneOption[])typeof(SearchableTimezoneBoxTests)
        .GetField("Options", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
}

internal static class Native
{
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    public static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool GetKeyboardState(byte[] state);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetKeyboardState(byte[] state);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    public static extern bool PostMessage(nint window, int message, nint wParam, nint lParam);
}
