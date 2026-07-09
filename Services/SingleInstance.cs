using System.Runtime.InteropServices;

namespace TimezoneConverter.Services;

/// <summary>
/// Ensures only one ZoneShift process runs; second launches activate the first.
/// </summary>
public static class SingleInstance
{
    private const string MutexName = "Local\\ZoneShift_SingleInstance_Mutex_A7C3E9F1";
    private const string WindowTitle = "ZoneShift";

    private static Mutex? _mutex;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    private const int SwRestore = 9;

    /// <returns>true if this process should continue; false if another instance owns the app.</returns>
    public static bool TryAcquire()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (createdNew)
            return true;

        try
        {
            _mutex.Dispose();
        }
        catch
        {
            // ignore
        }

        _mutex = null;
        ActivateExisting();
        return false;
    }

    public static void Release()
    {
        try
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
        catch
        {
            // ignore
        }
        finally
        {
            _mutex = null;
        }
    }

    private static void ActivateExisting()
    {
        try
        {
            var hWnd = FindWindow(null, WindowTitle);
            if (hWnd == IntPtr.Zero)
                return;

            if (IsIconic(hWnd))
                ShowWindow(hWnd, SwRestore);

            SetForegroundWindow(hWnd);
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Could not activate existing instance: {ex.Message}");
        }
    }
}
