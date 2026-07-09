namespace TimezoneConverter.Services;

/// <summary>
/// Simple rolling file logger under %LocalAppData%\ZoneShift\logs.
/// </summary>
public static class AppLog
{
    private static readonly object Gate = new();

    public static string LogDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ZoneShift",
                "logs");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string CurrentLogPath =>
        Path.Combine(LogDirectory, $"zoneshift-{DateTime.Now:yyyyMMdd}.log");

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? ex = null)
    {
        if (ex is null)
            Write("ERROR", message);
        else
            Write("ERROR", $"{message} | {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
    }

    private static void Write(string level, string message)
    {
        try
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
            lock (Gate)
            {
                File.AppendAllText(CurrentLogPath, line);
            }
        }
        catch
        {
            // never throw from logger
        }
    }

    public static string BuildDiagnosticsSummary(string settingsPath, string version)
    {
        return string.Join(Environment.NewLine,
        [
            $"ZoneShift {version}",
            $".NET {Environment.Version}",
            $"OS {Environment.OSVersion}",
            $"64-bit process: {Environment.Is64BitProcess}",
            $"Settings: {settingsPath}",
            $"Logs: {LogDirectory}",
            $"Local TZ: {TimeZoneInfo.Local.Id}",
            $"Time (UTC): {DateTime.UtcNow:O}",
        ]);
    }
}
