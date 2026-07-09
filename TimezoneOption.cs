namespace TimezoneConverter;

/// <summary>
/// Friendly timezone entry with a short label (e.g. IST) and Windows TimeZoneInfo id.
/// </summary>
public sealed class TimezoneOption
{
    public string Abbreviation { get; }
    public string DisplayName { get; }
    public string WindowsId { get; }

    public TimezoneOption(string abbreviation, string displayName, string windowsId)
    {
        Abbreviation = abbreviation;
        DisplayName = displayName;
        WindowsId = windowsId;
    }

    public TimeZoneInfo GetTimeZoneInfo() => TimeZoneInfo.FindSystemTimeZoneById(WindowsId);

    // Keep closed-combo text compact; full name still available in the list.
    public override string ToString() =>
        string.IsNullOrWhiteSpace(DisplayName)
            ? Abbreviation
            : $"{Abbreviation}  -  {DisplayName}";

    /// <summary>
    /// Common timezones people pick when coordinating across regions.
    /// Windows timezone IDs are used so this works out of the box on Windows.
    /// </summary>
    public static IReadOnlyList<TimezoneOption> Common { get; } =
    [
        new("UTC", "Coordinated Universal Time", "UTC"),
        new("GMT", "Greenwich Mean Time (London)", "GMT Standard Time"),
        new("CET", "Central European Time", "Central Europe Standard Time"),
        new("EET", "Eastern European Time", "E. Europe Standard Time"),
        new("IST", "India Standard Time", "India Standard Time"),
        new("PKT", "Pakistan Standard Time", "Pakistan Standard Time"),
        new("CST", "Central Time (US & Canada)", "Central Standard Time"),
        new("EST", "Eastern Time (US & Canada)", "Eastern Standard Time"),
        new("MST", "Mountain Time (US & Canada)", "Mountain Standard Time"),
        new("PST", "Pacific Time (US & Canada)", "Pacific Standard Time"),
        new("AST", "Atlantic Time (Canada)", "Atlantic Standard Time"),
        new("AKST", "Alaska Time", "Alaskan Standard Time"),
        new("HST", "Hawaii Time", "Hawaiian Standard Time"),
        new("CST-CN", "China Standard Time", "China Standard Time"),
        new("JST", "Japan Standard Time", "Tokyo Standard Time"),
        new("KST", "Korea Standard Time", "Korea Standard Time"),
        new("SGT", "Singapore Time", "Singapore Standard Time"),
        new("AEST", "Australian Eastern Time", "AUS Eastern Standard Time"),
        new("AWST", "Australian Western Time", "W. Australia Standard Time"),
        new("NZST", "New Zealand Time", "New Zealand Standard Time"),
        new("SAST", "South Africa Standard Time", "South Africa Standard Time"),
        new("MSK", "Moscow Standard Time", "Russian Standard Time"),
        new("GST", "Gulf Standard Time (Dubai)", "Arabian Standard Time"),
        new("BRT", "Brasilia Time", "E. South America Standard Time"),
        new("ART", "Argentina Time", "Argentina Standard Time"),
    ];

    /// <summary>
    /// Curated common zones first, then remaining Windows system zones.
    /// Invalid IDs (rare on some machines) are skipped.
    /// </summary>
    public static List<TimezoneOption> BuildFullList()
    {
        var list = new List<TimezoneOption>();
        var knownIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var option in Common)
        {
            if (!TryResolve(option.WindowsId, out _))
                continue;
            list.Add(option);
            knownIds.Add(option.WindowsId);
        }

        foreach (var tz in TimeZoneInfo.GetSystemTimeZones().OrderBy(t => t.DisplayName))
        {
            if (knownIds.Contains(tz.Id))
                continue;

            var abbr = tz.StandardName;
            if (string.IsNullOrWhiteSpace(abbr) || abbr.Length > 18)
                abbr = tz.Id.Length <= 16 ? tz.Id : tz.Id[..16];

            list.Add(new TimezoneOption(abbr, tz.DisplayName, tz.Id));
            knownIds.Add(tz.Id);
        }

        return list;
    }

    private static bool TryResolve(string windowsId, out TimeZoneInfo? tz)
    {
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(windowsId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            tz = null;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            tz = null;
            return false;
        }
    }
}
