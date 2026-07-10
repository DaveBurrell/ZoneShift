using System.Runtime.InteropServices;
using TimezoneConverter.Services;

namespace TimezoneConverter;

/// <summary>
/// Bridges a <see cref="ThemePalette"/> to the parts of the UI that Windows paints for us:
/// the date picker, checkbox glyphs, combo borders, scroll bars, and the title bar.
/// <para>
/// These are native common controls. They ignore <see cref="Control.BackColor"/> and read their
/// appearance from the process-wide color mode instead, so a palette alone cannot reach them.
/// </para>
/// </summary>
internal static class SystemTheming
{
    // DWMWA_USE_IMMERSIVE_DARK_MODE. Windows 10 builds before 20H1 used attribute 19.
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeLegacy = 19;

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpFrameChanged = 0x0020;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    /// <summary>
    /// Light themes map to <see cref="SystemColorMode.Classic"/> rather than
    /// <see cref="SystemColorMode.System"/>, so a light ZoneShift theme stays light even when
    /// Windows itself is set to dark.
    /// </summary>
    public static SystemColorMode ModeFor(bool isDark) =>
        isDark ? SystemColorMode.Dark : SystemColorMode.Classic;

    /// <summary>
    /// Sets the process color mode. Flipping <c>SystemColors</c> takes effect on the next repaint;
    /// controls that bake their theme at handle-creation need <see cref="RefreshNativeTheme"/>.
    /// </summary>
    public static void ApplyColorMode(bool isDark)
    {
        try
        {
            Application.SetColorMode(ModeFor(isDark));
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Could not set color mode: {ex.Message}");
        }
    }

    /// <summary>Paints the non-client title bar to match the theme.</summary>
    public static void ApplyTitleBar(IntPtr handle, bool isDark)
    {
        if (handle == IntPtr.Zero)
            return;

        try
        {
            var value = isDark ? 1 : 0;
            if (DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref value, sizeof(int)) != 0)
                DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeLegacy, ref value, sizeof(int));

            // The frame does not repaint on its own until something invalidates it.
            SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0,
                SwpNoMove | SwpNoSize | SwpNoZOrder | SwpFrameChanged);
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Could not set title bar mode: {ex.Message}");
        }
    }
}
