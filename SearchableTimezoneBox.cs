using System.ComponentModel;

namespace TimezoneConverter;

/// <summary>
/// Type-to-search timezone picker. The committed option is independent of the search text and
/// of the list highlight; only Enter, a click, or leaving the field with a search commits.
/// </summary>
internal sealed class SearchableTimezoneBox : DropDownField
{
    private static readonly char[] QuerySeparators = [' ', '\t', ',', '/'];

    private static readonly HashSet<string> CuratedIds =
        new(TimezoneOption.Common.Select(o => o.WindowsId), StringComparer.OrdinalIgnoreCase);

    private List<TimezoneOption> _all = [];
    private TimezoneOption[] _ordered = [];
    private TimezoneOption[] _shown = [];
    private readonly Dictionary<string, TimezoneOption> _byId = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _favorites = new(StringComparer.OrdinalIgnoreCase);
    private TimezoneOption? _selectedOption;
    private string? _pendingWindowsId;
    private bool _searching;

    public SearchableTimezoneBox()
    {
        Popup.EmptyText = "No matching timezones";
        Popup.MaxVisibleRows = 12;
    }

    public event EventHandler? SelectedOptionChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public TimezoneOption? SelectedOption => _selectedOption;

    /// <summary>The first option in list order (favourites first), for callers that need any valid zone.</summary>
    [Browsable(false)]
    public TimezoneOption? FirstOption => _ordered.Length > 0 ? _ordered[0] : null;

    /// <summary>True while the editor holds a search the user typed and has not committed.</summary>
    [Browsable(false)]
    public bool IsSearching => _searching;

    /// <summary>The rows the list currently offers, in display order.</summary>
    internal IReadOnlyList<TimezoneOption> Suggestions => _shown;

    public void SetOptions(IEnumerable<TimezoneOption> options, IEnumerable<string>? favoriteWindowsIds = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var previousId = _selectedOption?.WindowsId;
        _all = options.ToList();
        _byId.Clear();
        foreach (var option in _all)
            _byId.TryAdd(option.WindowsId, option);
        _favorites = CreateFavorites(favoriteWindowsIds ?? []);
        SortOptions();
        _selectedOption = _pendingWindowsId is not null && _byId.TryGetValue(_pendingWindowsId, out var pending)
            ? pending
            : null;
        _searching = false;
        ShowCommittedText();
        RefreshSuggestions();
        NotifySelectionChanged(previousId);
    }

    public void SetFavorites(IEnumerable<string> favoriteWindowsIds)
    {
        ArgumentNullException.ThrowIfNull(favoriteWindowsIds);
        var favorites = CreateFavorites(favoriteWindowsIds);
        if (_favorites.SetEquals(favorites))
            return;

        _favorites = favorites;
        SortOptions();
        if (IsDroppedDown)
            RefreshSuggestions();
    }

    public bool SelectWindowsId(string windowsId)
    {
        if (string.IsNullOrWhiteSpace(windowsId))
            return false;
        if (!_byId.TryGetValue(windowsId, out var option))
        {
            // Startup can request a zone before its options have been loaded.
            if (_all.Count == 0)
                _pendingWindowsId = windowsId;
            return false;
        }

        CommitOption(option);
        return true;
    }

    public bool SelectAbbreviation(string abbreviation)
    {
        var match = _ordered.FirstOrDefault(o =>
            string.Equals(o.Abbreviation, abbreviation, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            CommitOption(match);
            return true;
        }

        if (_selectedOption is null && _ordered.Length > 0)
            CommitOption(_ordered[0]);
        return false;
    }

    private static HashSet<string> CreateFavorites(IEnumerable<string> ids) =>
        new(ids.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);

    private void SortOptions() => _ordered = _all
        .OrderByDescending(o => _favorites.Contains(o.WindowsId))
        .ThenBy(o => o.Abbreviation, StringComparer.OrdinalIgnoreCase)
        .ThenBy(o => o.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    // Clicking back into a field that shows its zone starts a fresh search, even when focus never
    // left it (a click on a label closes the list but keeps focus here).
    protected override bool SelectAllOnClick => !_searching;

    protected override void OnEditorTextEdited()
    {
        _searching = true;
        RefreshSuggestions();
        if (Editor.Focused)
            ShowPopup();
    }

    protected override bool OnEditorKey(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Down or Keys.Up:
                var step = e.KeyCode == Keys.Down ? 1 : -1;
                if (IsDroppedDown)
                    Popup.MoveHighlight(step);
                else
                    OpenSuggestions();
                return true;

            case Keys.PageDown or Keys.PageUp when IsDroppedDown:
                Popup.MoveHighlight(e.KeyCode == Keys.PageDown ? Popup.PageSize : -Popup.PageSize);
                return true;

            case Keys.Enter:
                CommitFromEditor();
                return true;

            case Keys.Escape when IsDroppedDown || _searching:
                CancelSearch();
                return true;

            default:
                return false;
        }
    }

    protected override void OnDropButtonClicked()
    {
        Editor.SelectAll();
        OpenSuggestions();
    }

    protected override void OnPopupItemChosen(int index)
    {
        if (index < 0 || index >= _shown.Length)
            return;

        CommitOption(_shown[index]);
        HidePopup();
        Editor.SelectAll();
    }

    protected override void OnFieldLeave()
    {
        if (_searching)
            CommitFromEditor();
    }

    private void OpenSuggestions()
    {
        RefreshSuggestions();
        ShowPopup();
    }

    /// <summary>Enter or leaving the field: the highlighted row, else the best match, else revert.</summary>
    private void CommitFromEditor()
    {
        // While searching, the highlight stays meaningful after the list closes: a click on
        // another control closes it on mouse-down, before focus leaves and this runs.
        var highlight = Popup.HighlightIndex;
        TimezoneOption? choice = null;
        if ((IsDroppedDown || _searching) && highlight >= 0 && highlight < _shown.Length)
            choice = _shown[highlight];
        else if (_searching)
            choice = Rank(Editor.Text).FirstOrDefault();

        HidePopup();
        if (choice is not null)
            CommitOption(choice);
        else
            CancelSearch();

        if (Editor.Focused)
            Editor.SelectAll();
    }

    private void CancelSearch()
    {
        _searching = false;
        HidePopup();
        ShowCommittedText();
        if (Editor.Focused)
            Editor.SelectAll();
    }

    private void CommitOption(TimezoneOption option)
    {
        var previousId = _selectedOption?.WindowsId;
        _selectedOption = option;
        _pendingWindowsId = option.WindowsId;
        _searching = false;
        ShowCommittedText();
        if (IsDroppedDown)
            RefreshSuggestions();
        NotifySelectionChanged(previousId);
    }

    private void ShowCommittedText() =>
        SetEditorText(_selectedOption is null ? "" : FormatOption(_selectedOption));

    private void RefreshSuggestions()
    {
        var query = _searching ? Editor.Text : "";
        _shown = Rank(query);
        // A search highlights its best match; browsing highlights the committed zone, mid-list.
        var browsing = Tokenize(query).Length == 0;
        var highlight = browsing
            ? (_selectedOption is null ? -1 : Array.IndexOf(_shown, _selectedOption))
            : (_shown.Length > 0 ? 0 : -1);
        Popup.SetItems(
            _shown.Select(o => new PopupItem(o.Abbreviation, o.DisplayName, _favorites.Contains(o.WindowsId))).ToArray(),
            highlight,
            center: browsing);
    }

    private void NotifySelectionChanged(string? previousId)
    {
        if (!string.Equals(previousId, _selectedOption?.WindowsId, StringComparison.OrdinalIgnoreCase))
            SelectedOptionChanged?.Invoke(this, EventArgs.Empty);
    }

    internal static string FormatOption(TimezoneOption option) =>
        string.IsNullOrWhiteSpace(option.DisplayName)
            ? option.Abbreviation
            : $"{option.Abbreviation} - {option.DisplayName}";

    /// <summary>
    /// Every search word must appear in the zone's abbreviation, name or id. Exact names come
    /// first, then an exact abbreviation, then words that start with the search, then the rest.
    /// Ties go to favourites, then to the curated common zones (so "pacific" offers PST before
    /// "Pacific (Mexico)"), then list order.
    /// </summary>
    private TimezoneOption[] Rank(string query)
    {
        var tokens = Tokenize(query);
        if (tokens.Length == 0)
            return _ordered;

        var phrase = string.Join(' ', tokens);
        var trimmed = query.Trim();
        return _ordered
            .Select((option, order) => (option, order, score: Score(option, tokens, phrase, trimmed)))
            .Where(m => m.score >= 0)
            .OrderBy(m => m.score)
            .ThenBy(m => _favorites.Contains(m.option.WindowsId) ? 0 : 1)
            .ThenBy(m => CuratedIds.Contains(m.option.WindowsId) ? 0 : 1)
            .ThenBy(m => m.order)
            .Select(m => m.option)
            .ToArray();
    }

    private static string[] Tokenize(string query) => query
        .Split(QuerySeparators, StringSplitOptions.RemoveEmptyEntries)
        .Select(t => t.Trim('*', '★', '-', '(', ')'))
        .Where(t => t.Length > 0)
        .ToArray();

    private static int Score(TimezoneOption option, string[] tokens, string phrase, string query)
    {
        var haystack = $"{option.Abbreviation} {option.DisplayName} {option.WindowsId} {option.Keywords}";
        foreach (var token in tokens)
        {
            if (!haystack.Contains(token, StringComparison.OrdinalIgnoreCase))
                return -1;
        }

        if (string.Equals(option.WindowsId, query, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(option.DisplayName, query, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(FormatOption(option), query, StringComparison.OrdinalIgnoreCase))
            return 0;
        if (string.Equals(option.Abbreviation, phrase, StringComparison.OrdinalIgnoreCase))
            return 1;
        return HasWordStartingWith(haystack, tokens[0]) ? 2 : 3;
    }

    private static bool HasWordStartingWith(string text, string prefix)
    {
        for (var i = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
             i >= 0;
             i = text.IndexOf(prefix, i + 1, StringComparison.OrdinalIgnoreCase))
        {
            if (i == 0 || !char.IsLetterOrDigit(text[i - 1]))
                return true;
        }

        return false;
    }
}
