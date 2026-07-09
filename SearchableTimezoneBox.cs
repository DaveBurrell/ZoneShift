using System.ComponentModel;

namespace TimezoneConverter;

/// <summary>
/// Type-to-filter timezone dropdown. Favorites appear first (starred in list text).
/// </summary>
internal sealed class SearchableTimezoneBox : ComboBox
{
    private List<TimezoneOption> _all = [];
    private HashSet<string> _favorites = new(StringComparer.OrdinalIgnoreCase);
    private bool _filtering;
    private string _filter = "";

    public SearchableTimezoneBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        DropDownStyle = ComboBoxStyle.DropDown; // allows typing to filter
        IntegralHeight = false;
        MaxDropDownItems = 16;
        FlatStyle = FlatStyle.Flat;
        ItemHeight = 22;
        AutoCompleteMode = AutoCompleteMode.None;
        // Light text on dark field — avoids default black-on-dark
        BackColor = UiTheme.InputBack;
        ForeColor = UiTheme.TextPrimary;

        TextUpdate += OnTextUpdate;
        DropDown += (_, _) => ApplyFilter(_filter, keepText: true);
        SelectionChangeCommitted += (_, _) =>
        {
            if (SelectedItem is TimezoneOption opt)
            {
                _filtering = true;
                try
                {
                    Text = opt.ToString();
                }
                finally
                {
                    _filtering = false;
                }
            }
        };
        Leave += (_, _) => CommitSelectionFromText();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitSelectionFromText();
                e.SuppressKeyPress = true;
            }
        };
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public TimezoneOption? SelectedOption
    {
        get
        {
            if (SelectedItem is TimezoneOption opt)
                return opt;
            if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
                return Items[SelectedIndex] as TimezoneOption;
            return null;
        }
    }

    public void SetOptions(IEnumerable<TimezoneOption> options, IEnumerable<string>? favoriteWindowsIds = null)
    {
        _all = options.ToList();
        _favorites = new HashSet<string>(
            favoriteWindowsIds?.Where(id => !string.IsNullOrWhiteSpace(id)) ?? [],
            StringComparer.OrdinalIgnoreCase);
        ApplyFilter("", keepText: false);
    }

    public void SetFavorites(IEnumerable<string> favoriteWindowsIds)
    {
        _favorites = new HashSet<string>(
            favoriteWindowsIds.Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.OrdinalIgnoreCase);
        var currentId = SelectedOption?.WindowsId;
        ApplyFilter(_filter, keepText: true);
        if (currentId is not null)
            SelectWindowsId(currentId);
    }

    public bool SelectWindowsId(string windowsId)
    {
        // Ensure item is in list (clear filter if needed)
        if (!Items.Cast<object>().OfType<TimezoneOption>()
                .Any(o => string.Equals(o.WindowsId, windowsId, StringComparison.OrdinalIgnoreCase)))
        {
            ApplyFilter("", keepText: false);
        }

        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i] is TimezoneOption opt &&
                string.Equals(opt.WindowsId, windowsId, StringComparison.OrdinalIgnoreCase))
            {
                SelectedIndex = i;
                _filtering = true;
                try { Text = FormatOption(opt); }
                finally { _filtering = false; }
                return true;
            }
        }

        return false;
    }

    public bool SelectAbbreviation(string abbreviation)
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i] is TimezoneOption opt &&
                string.Equals(opt.Abbreviation, abbreviation, StringComparison.OrdinalIgnoreCase))
            {
                SelectedIndex = i;
                _filtering = true;
                try { Text = FormatOption(opt); }
                finally { _filtering = false; }
                return true;
            }
        }

        // search full catalog
        var match = _all.FirstOrDefault(o =>
            string.Equals(o.Abbreviation, abbreviation, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            return SelectWindowsId(match.WindowsId);

        if (Items.Count > 0)
        {
            SelectedIndex = 0;
            return false;
        }

        return false;
    }

    private void OnTextUpdate(object? sender, EventArgs e)
    {
        if (_filtering)
            return;
        ApplyFilter(Text, keepText: true);
        DroppedDown = true;
        // keep cursor at end
        SelectionStart = Text.Length;
        SelectionLength = 0;
    }

    private void ApplyFilter(string filter, bool keepText)
    {
        _filter = filter ?? "";
        var q = _filter.Trim();

        IEnumerable<TimezoneOption> query = _all;
        if (q.Length > 0)
        {
            query = _all.Where(o =>
                o.Abbreviation.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                o.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                o.WindowsId.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query
            .OrderByDescending(o => _favorites.Contains(o.WindowsId))
            .ThenBy(o => o.Abbreviation, StringComparer.OrdinalIgnoreCase)
            .ThenBy(o => o.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var previous = keepText ? Text : null;
        var selectedId = SelectedOption?.WindowsId;

        _filtering = true;
        BeginUpdate();
        try
        {
            Items.Clear();
            foreach (var o in ordered)
                Items.Add(o);

            if (selectedId is not null)
            {
                for (var i = 0; i < Items.Count; i++)
                {
                    if (Items[i] is TimezoneOption opt &&
                        string.Equals(opt.WindowsId, selectedId, StringComparison.OrdinalIgnoreCase))
                    {
                        SelectedIndex = i;
                        break;
                    }
                }
            }

            if (keepText && previous is not null)
                Text = previous;
        }
        finally
        {
            EndUpdate();
            _filtering = false;
        }
    }

    private void CommitSelectionFromText()
    {
        if (SelectedItem is TimezoneOption)
            return;

        var q = Text.Trim();
        if (q.Length == 0)
            return;

        var match = _all.FirstOrDefault(o =>
            string.Equals(o.Abbreviation, q, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(FormatOption(o), q, StringComparison.OrdinalIgnoreCase) ||
            o.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
            SelectWindowsId(match.WindowsId);
        else if (SelectedOption is not null)
        {
            _filtering = true;
            try { Text = FormatOption(SelectedOption); }
            finally { _filtering = false; }
        }
    }

    private string FormatOption(TimezoneOption opt) =>
        _favorites.Contains(opt.WindowsId)
            ? $"* {opt.Abbreviation} - {opt.DisplayName}"
            : $"{opt.Abbreviation} - {opt.DisplayName}";

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index < 0 || e.Index >= Items.Count)
        {
            base.OnDrawItem(e);
            return;
        }

        if (Items[e.Index] is not TimezoneOption opt)
        {
            base.OnDrawItem(e);
            return;
        }

        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var back = selected ? UiTheme.SegmentActive : UiTheme.InputBack;
        var fore = selected ? UiTheme.TextOnAccent : UiTheme.TextPrimary;
        using (var b = new SolidBrush(back))
            e.Graphics.FillRectangle(b, e.Bounds);

        var text = FormatOption(opt);
        TextRenderer.DrawText(
            e.Graphics,
            text,
            Font,
            e.Bounds,
            fore,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
    }
}
