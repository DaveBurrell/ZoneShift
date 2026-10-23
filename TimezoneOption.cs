namespace TimezoneConverter;

/// <summary>
/// Friendly timezone entry with a short label (e.g. IST) and Windows TimeZoneInfo id.
/// </summary>
public sealed class TimezoneOption
{
    public string Abbreviation { get; }
    public string DisplayName { get; }
    public string WindowsId { get; }

    /// <summary>Extra words search should match, such as the cities a zone covers. Not displayed.</summary>
    public string Keywords { get; }

    public TimezoneOption(string abbreviation, string displayName, string windowsId, string keywords = "")
    {
        Abbreviation = abbreviation;
        DisplayName = displayName;
        WindowsId = windowsId;
        Keywords = keywords;
    }

    /// <summary>Major cities missing from the Windows names of the curated US zones.</summary>
    private static readonly Dictionary<string, string> CityAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Eastern Standard Time"] = "New York, Toronto, Boston, Washington, Atlanta, Miami",
        ["Central Standard Time"] = "Chicago, Dallas, Houston, Winnipeg",
        ["Mountain Standard Time"] = "Denver, Calgary, Edmonton",
        ["Pacific Standard Time"] = "Los Angeles, San Francisco, Seattle, Vancouver",
        ["Alaskan Standard Time"] = "Anchorage",
        ["Hawaiian Standard Time"] = "Honolulu",
    };

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
            if (!TryResolve(option.WindowsId, out var tz))
                continue;
            // The curated name replaces Windows' "(UTC+10:00) Canberra, Melbourne, Sydney", so keep
            // that text searchable: otherwise "Sydney" or "Mumbai" found nothing.
            var keywords = CityAliases.TryGetValue(option.WindowsId, out var cities)
                ? $"{tz.DisplayName} {cities}"
                : tz.DisplayName;
            list.Add(new TimezoneOption(option.Abbreviation, option.DisplayName, option.WindowsId, keywords));
            knownIds.Add(option.WindowsId);
        }

        foreach (var tz in TimeZoneInfo.GetSystemTimeZones().OrderBy(t => t.DisplayName))
        {
            if (knownIds.Contains(tz.Id))
                continue;

            list.Add(new TimezoneOption(ShortLabel(tz.Id), tz.DisplayName, tz.Id));
            knownIds.Add(tz.Id);
        }

        return list;
    }

    internal const int MaxLabelLength = 24;

    /// <summary>
    /// Short label for a zone without a curated abbreviation. Windows ids are stable English
    /// names ("Vladivostok Standard Time"); dropping the suffix keeps them readable on a clock
    /// tile, where cutting the id at a fixed width produced labels like "Vladivostok Stan".
    /// </summary>
    internal static string ShortLabel(string windowsId)
    {
        // Also inside ids such as "Pacific Standard Time (Mexico)".
        var label = windowsId.Replace(" Standard Time", "", StringComparison.OrdinalIgnoreCase).Trim();
        return label.Length <= MaxLabelLength ? label : label[..(MaxLabelLength - 1)].TrimEnd() + "…";
    }

    private static bool TryResolve(string windowsId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TimeZoneInfo? tz)
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
