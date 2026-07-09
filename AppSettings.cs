using System.Text.Json;

namespace TimezoneConverter;

/// <summary>
/// User preferences persisted to %AppData%\ZoneShift\settings.json
/// </summary>
public sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public bool Use24Hour { get; set; }

    /// <summary>
    /// true = enter time in another zone and convert to local;
    /// false = enter local time and convert to other zones.
    /// </summary>
    public bool ConvertToLocal { get; set; }

    /// <summary>Windows ID of the foreign timezone used when ConvertToLocal is true.</summary>
    public string? ReverseSourceWindowsId { get; set; }

    /// <summary>Windows timezone IDs for target slots (1-8). Empty uses defaults.</summary>
    public string?[]? TargetWindowsIds { get; set; }

    public bool OverlayVisible { get; set; }

    public int OverlayX { get; set; } = -1;
    public int OverlayY { get; set; } = -1;
    public double OverlayOpacity { get; set; } = 0.94;

    public static string SettingsDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ZoneShift");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    private static string LegacySettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TimezoneConverter",
            "settings.json");

    public static AppSettings Load()
    {
        try
        {
            var path = SettingsPath;
            if (!File.Exists(path) && File.Exists(LegacySettingsPath))
                File.Copy(LegacySettingsPath, path, overwrite: false);

            if (!File.Exists(path))
                return new AppSettings();

            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();

            if (settings.OverlayOpacity is < 0.4 or > 1.0)
                settings.OverlayOpacity = 0.94;

            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // best-effort
        }
    }
}
