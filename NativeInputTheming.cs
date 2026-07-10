using System.Runtime.InteropServices;

namespace TimezoneConverter;

/// <summary>
/// Themes the native EDIT child that an editable <see cref="ComboBox"/> hosts.
/// <para>
/// Setting <see cref="Control.BackColor"/> only reaches the parts WinForms paints itself (the flat
/// dropdown button). The edit field is a native control that Windows paints after asking its
/// container for colors via <c>WM_CTLCOLOREDIT</c> — a combo forwards that message to its own
/// parent. Containers hosting a combo therefore call <see cref="TryHandle"/> from their WndProc.
/// </para>
/// </summary>
internal static class NativeInputTheming
{
    private const int WmCtlColorEdit = 0x0133;
    private const int WmCtlColorListBox = 0x0134;
    private const int WmCtlColorStatic = 0x0138;

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(int colorRef);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr handle);

    [DllImport("gdi32.dll")]
    private static extern int SetBkColor(IntPtr hdc, int colorRef);

    [DllImport("gdi32.dll")]
    private static extern int SetTextColor(IntPtr hdc, int colorRef);

    private static IntPtr _brush = IntPtr.Zero;
    private static Color _brushColor = Color.Empty;

    /// <summary>
    /// Answers a WM_CTLCOLOR* message with the current theme's input colors.
    /// </summary>
    /// <returns>true when the message was handled and WndProc must not call base.</returns>
    public static bool TryHandle(ref Message m)
    {
        if (m.Msg is not (WmCtlColorEdit or WmCtlColorListBox or WmCtlColorStatic))
            return false;

        var back = UiTheme.InputBack;
        SetTextColor(m.WParam, ColorTranslator.ToWin32(UiTheme.TextPrimary));
        SetBkColor(m.WParam, ColorTranslator.ToWin32(back));
        m.Result = EnsureBrush(back);
        return true;
    }

    /// <summary>
    /// One process-wide brush for the active theme. The edit field repaints on every keystroke, so
    /// allocating per message would leak a GDI handle each time. Swapping themes replaces it.
    /// </summary>
    private static IntPtr EnsureBrush(Color color)
    {
        if (_brush != IntPtr.Zero && _brushColor == color)
            return _brush;

        if (_brush != IntPtr.Zero)
            DeleteObject(_brush);

        _brush = CreateSolidBrush(ColorTranslator.ToWin32(color));
        _brushColor = color;
        return _brush;
    }
}

/// <summary>Plain panel that themes the native edit field of any combo it hosts.</summary>
internal sealed class InputHostPanel : Panel
{
    protected override void WndProc(ref Message m)
    {
        if (NativeInputTheming.TryHandle(ref m))
            return;

        base.WndProc(ref m);
    }
}
