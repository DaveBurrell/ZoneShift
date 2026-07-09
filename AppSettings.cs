using System.Text.Json;
using TimezoneConverter.Services;

namespace TimezoneConverter;

/// <summary>
/// User preferences persisted to %AppData%\ZoneShift\settings.json
/// </summary>
public sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    public bool Use24Hour { get; set; }
    public bool ConvertToLocal { get; set; }
    public string? ReverseSourceWindowsId { get; set; }
    public string?[]? TargetWindowsIds { get; set; }
    public string?[]? FavoriteWindowsIds { get; set; }

    public bool OverlayVisible { get; set; }
    public int OverlayX { get; set; } = -1;
    public int OverlayY { get; set; } = -1;
    public double OverlayOpacity { get; set; } = 0.94;
    public bool OverlayLocked { get; set; }
    public bool OverlayCompact { get; set; }

    public bool CloseToTray { get; set; } = true;
    public bool LiveMode { get; set; } = true;
    public bool HasSeenOnboarding { get; set; }

    /// <summary>Studio | Classic | NeonPulse (see <see cref="AppThemeId"/>).</summary>
    public string Theme { get; set; } = nameof(AppThemeId.Studio);

    public int WindowX { get; set; } = -1;
    public int WindowY { get; set; } = -1;
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }

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
    private static string BackupPath => Path.Combine(SettingsDirectory, "settings.bak.json");

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
            {
                File.Copy(LegacySettingsPath, path, overwrite: false);
                AppLog.Info($"Migrated settings from legacy path: {LegacySettingsPath}");
            }

            if (!File.Exists(path))
            {
                AppLog.Info("No settings file; using defaults.");
                return new AppSettings();
            }

            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();

            if (settings.OverlayOpacity is < 0.4 or > 1.0)
                settings.OverlayOpacity = 0.94;

            AppLog.Info($"Loaded settings from {path} ({settings.TargetWindowsIds?.Length ?? 0} zones).");
            return settings;
        }
        catch (Exception ex)
        {
            AppLog.Error("Failed to load settings; trying backup.", ex);
            try
            {
                if (File.Exists(BackupPath))
                {
                    var json = File.ReadAllText(BackupPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (settings is not null)
                    {
                        AppLog.Info("Restored settings from backup.");
                        return settings;
                    }
                }
            }
            catch (Exception backupEx)
            {
                AppLog.Error("Backup restore failed.", backupEx);
            }

            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var path = SettingsPath;
            Directory.CreateDirectory(SettingsDirectory);

            var json = JsonSerializer.Serialize(this, JsonOptions);
            var temp = path + ".tmp";
            File.WriteAllText(temp, json);

            if (File.Exists(path))
            {
                try { File.Copy(path, BackupPath, overwrite: true); }
                catch (Exception ex) { AppLog.Warn($"Could not write settings backup: {ex.Message}"); }
            }

            File.Copy(temp, path, overwrite: true);
            File.Delete(temp);
            AppLog.Info($"Saved settings ({TargetWindowsIds?.Length ?? 0} zones) to {path}");
        }
        catch (Exception ex)
        {
            AppLog.Error("Failed to save settings (atomic path).", ex);
            try
            {
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
                AppLog.Info("Saved settings via fallback direct write.");
            }
            catch (Exception fallbackEx)
            {
                AppLog.Error("Fallback settings save failed.", fallbackEx);
                throw;
            }
        }
    }

    public bool IsFavorite(string windowsId) =>
        FavoriteWindowsIds?.Any(id => string.Equals(id, windowsId, StringComparison.OrdinalIgnoreCase)) == true;

    public void ToggleFavorite(string windowsId)
    {
        var list = FavoriteWindowsIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList() ?? [];
        var existing = list.FirstOrDefault(id => string.Equals(id, windowsId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            list.Remove(existing);
        else
            list.Add(windowsId);
        FavoriteWindowsIds = list.ToArray();
    }
}
