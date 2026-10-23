using System.Runtime.InteropServices;

namespace TimezoneConverter.Services;

/// <summary>
/// Ensures only one ZoneShift process runs; second launches activate the first.
/// <para>
/// A second launch signals a named event rather than poking the first instance's window: with
/// close-to-tray the window is hidden, and restoring or foregrounding a hidden window from
/// another process shows nothing. The first instance restores itself through
/// <see cref="ActivationRequested"/>.
/// </para>
/// </summary>
public static class SingleInstance
{
    private const string MutexName = "Local\\ZoneShift_SingleInstance_Mutex_A7C3E9F1";
    private const string ActivateEventName = @"Local\ZoneShift_Activate_A7C3E9F1";
    private const string WindowTitle = "ZoneShift";
    private const int AsfwAny = -1;

    private static Mutex? _mutex;
    private static EventWaitHandle? _activateSignal;
    private static RegisteredWaitHandle? _activateWait;

    /// <summary>Raised on a thread-pool thread when another launch asks this instance to show itself.</summary>
    public static event Action? ActivationRequested;

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int processId);

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
        {
            ListenForActivation();
            return true;
        }

        try
        {
            _mutex.Dispose();
        }
        catch
        {
            // ignore
        }

        _mutex = null;
        if (!SignalExisting())
            ActivateExisting();
        return false;
    }

    private static void ListenForActivation()
    {
        try
        {
            _activateSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
            _activateWait = ThreadPool.RegisterWaitForSingleObject(
                _activateSignal,
                (_, _) => ActivationRequested?.Invoke(),
                state: null,
                Timeout.Infinite,
                executeOnlyOnce: false);
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Could not listen for activation requests: {ex.Message}");
        }
    }

    /// <returns>false when the running instance predates the activation event.</returns>
    private static bool SignalExisting()
    {
        try
        {
            if (!EventWaitHandle.TryOpenExisting(ActivateEventName, out var signal))
                return false;

            using (signal)
            {
                // This process was just launched by the user, so it may hand foreground rights on.
                AllowSetForegroundWindow(AsfwAny);
                signal.Set();
            }
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn($"Could not signal existing instance: {ex.Message}");
            return false;
        }
    }

    public static void Release()
    {
        try
        {
            _activateWait?.Unregister(null);
            _activateSignal?.Dispose();
        }
        catch
        {
            // ignore
        }
        finally
        {
            _activateWait = null;
            _activateSignal = null;
        }

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

    /// <summary>Fallback for a running instance from before the activation event existed.</summary>
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
