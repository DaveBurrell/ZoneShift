namespace TimezoneConverter.Services;

/// <summary>
/// Simple daily file logger under %LocalAppData%\ZoneShift\logs. Files older than
/// <see cref="RetentionDays"/> are deleted on the first write of each run.
/// </summary>
public static class AppLog
{
    private const int RetentionDays = 14;
    private static readonly object Gate = new();
    private static bool _pruned;

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
                if (!_pruned)
                {
                    _pruned = true;
                    PruneOldLogs();
                }
                File.AppendAllText(CurrentLogPath, line);
            }
        }
        catch
        {
            // never throw from logger
        }
    }

    private static void PruneOldLogs()
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-RetentionDays);
            foreach (var file in Directory.EnumerateFiles(LogDirectory, "zoneshift-*.log"))
            {
                if (File.GetLastWriteTime(file) < cutoff)
                    File.Delete(file);
            }
        }
        catch
        {
            // Housekeeping only.
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
