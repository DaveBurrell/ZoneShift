using System.ComponentModel;

namespace TimezoneConverter;

/// <summary>
/// Type-to-filter timezone dropdown. The committed option is independent of the native
/// list's temporary selection while searching or moving through suggestions.
/// </summary>
internal sealed class SearchableTimezoneBox : ComboBox
{
    private List<TimezoneOption> _all = [];
    private TimezoneOption[] _ordered = [];
    private readonly Dictionary<string, TimezoneOption> _byId = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _favorites = new(StringComparer.OrdinalIgnoreCase);
    private TimezoneOption? _selectedOption;
    private string? _pendingWindowsId;
    private bool _updating;
    private bool _editing;
    private bool _refreshQueued;
    private bool _itemsNeedRefresh;
    private int _nativeNotificationDepth;
    private string _filter = "";
    private int _caretStart;
    private int _caretLength;

    public SearchableTimezoneBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        DropDownStyle = ComboBoxStyle.DropDown;
        IntegralHeight = false;
        MaxDropDownItems = 16;
        FlatStyle = FlatStyle.Flat;
        ItemHeight = 22;
        AutoCompleteMode = AutoCompleteMode.None;
        FormattingEnabled = true;
        BackColor = UiTheme.InputBack;
        ForeColor = UiTheme.TextPrimary;
    }

    public event EventHandler? SelectedOptionChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public TimezoneOption? SelectedOption => _selectedOption;

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
        _editing = false;
        _filter = "";
        RequestRefresh();
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
        _itemsNeedRefresh = true;
        RequestRefresh();
        Invalidate();
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

    protected override void OnTextUpdate(EventArgs e)
    {
        if (_updating)
            return;

        _editing = true;
        _filter = Text;
        _caretStart = SelectionStart;
        _caretLength = SelectionLength;
        // Changing Items inside CBN_EDITUPDATE/CBN_DROPDOWN can leave the native
        // selection pointing past the managed collection. Wait for it to finish.
        RequestRefresh(defer: true);
        base.OnTextUpdate(e);
    }

    protected override void OnSelectionChangeCommitted(EventArgs e)
    {
        if (_updating)
            return;

        _nativeNotificationDepth++;
        try
        {
            if (SelectedItem is TimezoneOption option)
                CommitOption(option);
            base.OnSelectionChangeCommitted(e);
        }
        finally
        {
            _nativeNotificationDepth--;
        }
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        if (!_updating)
            base.OnSelectedIndexChanged(e);
    }

    protected override void OnDropDown(EventArgs e)
    {
        // A user can still click the arrow after a search returns no matches.
        // Close that empty popup after its native opening notification completes.
        if (!_updating && Items.Count == 0)
            RequestRefresh(defer: true);
        base.OnDropDown(e);
    }

    protected override void WndProc(ref Message m)
    {
        const int reflectedCommand = 0x2000 + 0x0111; // WM_REFLECT + WM_COMMAND
        const int selectionCanceled = 10; // CBN_SELENDCANCEL
        if (m.Msg != reflectedCommand)
        {
            base.WndProc(ref m);
            return;
        }

        _nativeNotificationDepth++;
        try
        {
            // Popup cancellation can be consumed by the native list before KeyDown.
            if (!_updating && ((m.WParam.ToInt64() >> 16) & 0xffff) == selectionCanceled)
                RestoreCommittedSelection();
            base.WndProc(ref m);
        }
        finally
        {
            _nativeNotificationDepth--;
        }
    }

    protected override void OnLeave(EventArgs e)
    {
        _nativeNotificationDepth++;
        try
        {
            if (_editing)
                CommitSelectionFromText(useHighlightedItem: false);
            base.OnLeave(e);
        }
        finally
        {
            _nativeNotificationDepth--;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        _nativeNotificationDepth++;
        try
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitSelectionFromText(useHighlightedItem: true);
                DroppedDown = false;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                RestoreCommittedSelection();
                DroppedDown = false;
                e.SuppressKeyPress = true;
            }
            base.OnKeyDown(e);
        }
        finally
        {
            _nativeNotificationDepth--;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RequestRefresh(defer: true);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _refreshQueued = false;
        base.OnHandleDestroyed(e);
    }

    private void RequestRefresh(bool defer = false)
    {
        if (IsDisposed || Disposing || _refreshQueued)
            return;
        if (IsHandleCreated && (defer || _nativeNotificationDepth > 0))
        {
            _refreshQueued = true;
            BeginInvoke((Action)(() =>
            {
                _refreshQueued = false;
                if (!IsDisposed && !Disposing)
                    RefreshItemsFromFilter();
            }));
            return;
        }

        RefreshItemsFromFilter();
    }

    private void RefreshItemsFromFilter()
    {
        var query = _editing ? _filter.Trim() : "";
        var matches = query.Length == 0
            ? _ordered
            : _ordered.Where(o => MatchesFilter(o, query)).ToArray();
        var itemsChanged = _itemsNeedRefresh || Items.Count != matches.Length;
        for (var i = 0; !itemsChanged && i < matches.Length; i++)
            itemsChanged = !ReferenceEquals(Items[i], matches[i]);

        _updating = true;
        BeginUpdate();
        try
        {
            if (matches.Length == 0 && DroppedDown)
                DroppedDown = false;
            if (itemsChanged)
            {
                // Clear native selection before resetting its backing collection.
                SelectedIndex = -1;
                Items.Clear();
                Items.AddRange(matches);
                _itemsNeedRefresh = false;
            }

            if (_editing)
            {
                SelectedIndex = -1;
                Text = _filter;
                SelectionStart = Math.Min(_caretStart, Text.Length);
                SelectionLength = Math.Min(_caretLength, Text.Length - SelectionStart);
            }
            else
            {
                SelectedIndex = _selectedOption is null ? -1 : Array.IndexOf(matches, _selectedOption);
                Text = _selectedOption is null ? "" : FormatOption(_selectedOption);
            }
        }
        finally
        {
            EndUpdate();
            _updating = false;
        }

        if (_editing && matches.Length > 0 && Focused && !DroppedDown)
        {
            DroppedDown = true;
            SelectionStart = Math.Min(_caretStart, Text.Length);
            SelectionLength = Math.Min(_caretLength, Text.Length - SelectionStart);
        }
    }

    private static bool MatchesFilter(TimezoneOption option, string query) =>
        option.Abbreviation.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        option.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        option.WindowsId.Contains(query, StringComparison.OrdinalIgnoreCase);

    private void CommitSelectionFromText(bool useHighlightedItem)
    {
        if (useHighlightedItem && (!_editing || !_refreshQueued) && SelectedItem is TimezoneOption selected)
        {
            CommitOption(selected);
            return;
        }

        var query = (_editing ? _filter : Text).Trim();
        var match = query.Length == 0 ? null : _ordered.FirstOrDefault(o =>
            string.Equals(o.Abbreviation, query, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(o.WindowsId, query, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(o.DisplayName, query, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(FormatOption(o), query, StringComparison.OrdinalIgnoreCase));
        match ??= query.Length == 0 ? null : _ordered.FirstOrDefault(o => MatchesFilter(o, query));
        if (match is not null)
            CommitOption(match);
        else
            RestoreCommittedSelection();
    }

    private void CommitOption(TimezoneOption option)
    {
        var previousId = _selectedOption?.WindowsId;
        _selectedOption = option;
        _pendingWindowsId = option.WindowsId;
        RestoreCommittedSelection();
        NotifySelectionChanged(previousId);
    }

    private void RestoreCommittedSelection()
    {
        _editing = false;
        _filter = "";
        RequestRefresh();
    }

    private void NotifySelectionChanged(string? previousId)
    {
        if (!string.Equals(previousId, _selectedOption?.WindowsId, StringComparison.OrdinalIgnoreCase))
            SelectedOptionChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnFormat(ListControlConvertEventArgs e)
    {
        base.OnFormat(e);
        if (e.ListItem is TimezoneOption option)
            e.Value = FormatOption(option);
    }

    private string FormatOption(TimezoneOption option) =>
        _favorites.Contains(option.WindowsId)
            ? $"* {option.Abbreviation} - {option.DisplayName}"
            : $"{option.Abbreviation} - {option.DisplayName}";

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index < 0 || e.Index >= Items.Count || Items[e.Index] is not TimezoneOption option)
        {
            base.OnDrawItem(e);
            return;
        }

        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var back = selected ? UiTheme.SegmentActive : UiTheme.InputBack;
        var fore = selected ? UiTheme.TextOnAccent : UiTheme.TextPrimary;
        using (var brush = new SolidBrush(back))
            e.Graphics.FillRectangle(brush, e.Bounds);

        TextRenderer.DrawText(e.Graphics, FormatOption(option), Font, e.Bounds, fore,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
    }
}
