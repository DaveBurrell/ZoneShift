using TimezoneConverter.Services;

namespace TimezoneConverter;

static class Program
{
    [STAThread]
    static void Main()
    {
        if (!SingleInstance.TryAcquire())
            return;

        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Absolute layout is designed at 96 DPI. GDI-scaled unaware keeps
            // control positions stable while looking clearer than pure DpiUnaware.
            // PerMonitorV2 + AutoScaleMode.Dpi crushed the absolute layout.
            Application.SetHighDpiMode(HighDpiMode.DpiUnawareGdiScaled);

            AppLog.Info($"ZoneShift starting (arch={UpdateChecker.CurrentArchitectureLabel}).");
            Application.ApplicationExit += (_, _) =>
            {
                AppLog.Info("ZoneShift exiting.");
                SingleInstance.Release();
            };

            Application.ThreadException += (_, e) =>
            {
                AppLog.Error("UI thread exception", e.Exception);
                MessageBox.Show(
                    $"Unexpected error:\n{e.Exception.Message}\n\nDetails were written to the log folder.",
                    "ZoneShift",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    AppLog.Error("Unhandled exception", ex);
                else
                    AppLog.Error($"Unhandled non-Exception: {e.ExceptionObject}");
            };

            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            AppLog.Error("Fatal startup error", ex);
            MessageBox.Show(ex.Message, "ZoneShift failed to start", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SingleInstance.Release();
        }
    }
}
