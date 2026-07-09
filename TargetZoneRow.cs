namespace TimezoneConverter;

/// <summary>
/// One target row: [badge][timezone search][* fav][offset][clock][x]
/// </summary>
internal sealed class TargetZoneRow
{
    public const int RowHeight = 46;

    public Control Root { get; }
    public SearchableTimezoneBox Combo { get; }
    public Label Meta { get; }
    public Button RemoveButton { get; }
    public Button FavoriteButton { get; }
    public Label IndexBadge { get; }
    public ClockFace Clock { get; }

    private readonly Panel _clockPanel;
    private readonly Label _clockLabel;

    public TargetZoneRow(
        IReadOnlyList<TimezoneOption> options,
        IEnumerable<string> favorites,
        EventHandler onChanged,
        EventHandler onRemove,
        EventHandler onFavorite)
    {
        var row = new Panel
        {
            Height = RowHeight,
            MinimumSize = new Size(300, RowHeight),
            MaximumSize = new Size(10000, RowHeight),
            BackColor = UiTheme.CardBackground,
            Margin = new Padding(0, 0, 0, 6)
        };

        IndexBadge = new Label
        {
            Text = "1",
            Font = new Font("Segoe UI Semibold", 9f),
            ForeColor = UiTheme.Accent,
            BackColor = UiTheme.AccentSoft,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(22, 24)
        };

        Combo = new SearchableTimezoneBox
        {
            Font = UiTheme.BodyFont,
            Size = new Size(180, 26)
        };
        Combo.SetOptions(options, favorites);
        Combo.SelectedIndexChanged += onChanged;
        Combo.SelectionChangeCommitted += onChanged;

        FavoriteButton = new Button
        {
            Text = "*",
            Font = new Font("Segoe UI Semibold", 10f),
            FlatStyle = FlatStyle.Flat,
            ForeColor = UiTheme.Accent,
            BackColor = UiTheme.AccentSoft,
            Cursor = Cursors.Hand,
            Size = new Size(26, 26),
            TabStop = false
        };
        FavoriteButton.FlatAppearance.BorderSize = 0;
        FavoriteButton.Click += onFavorite;

        Meta = new Label
        {
            Text = "",
            Font = UiTheme.CaptionFont,
            ForeColor = UiTheme.TextSecondary,
            Size = new Size(62, 22),
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = UiTheme.CardBackground,
            AutoEllipsis = true
        };

        _clockPanel = new Panel
        {
            Size = new Size(168, 34),
            BackColor = UiTheme.ClockBack
        };
        _clockLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UiTheme.ClockRowFont,
            ForeColor = UiTheme.ClockFore,
            BackColor = UiTheme.ClockBack,
            Text = "--:--",
            AutoEllipsis = true
        };
        _clockPanel.Controls.Add(_clockLabel);
        Clock = new ClockFace(_clockLabel);

        RemoveButton = new Button
        {
            Text = "x",
            Font = new Font("Segoe UI Semibold", 11f),
            FlatStyle = FlatStyle.Flat,
            ForeColor = UiTheme.Danger,
            BackColor = Color.FromArgb(254, 226, 226),
            Cursor = Cursors.Hand,
            Size = new Size(28, 28),
            TabStop = false
        };
        RemoveButton.FlatAppearance.BorderSize = 0;
        RemoveButton.Click += onRemove;

        row.Controls.Add(IndexBadge);
        row.Controls.Add(RemoveButton);
        row.Controls.Add(_clockPanel);
        row.Controls.Add(Meta);
        row.Controls.Add(FavoriteButton);
        row.Controls.Add(Combo);
        row.Resize += (_, _) => LayoutRow(row.ClientSize.Width);

        Root = row;
        LayoutRow(640);
    }

    private void LayoutRow(int width)
    {
        const int pad = 6;
        const int badgeW = 22;
        const int favW = 26;
        const int metaW = 64;
        const int clockW = 168;
        const int removeW = 28;

        var xRemove = width - removeW - 2;
        var xClock = xRemove - pad - clockW;
        var xMeta = xClock - pad - metaW;
        var xFav = xMeta - pad - favW;
        var xCombo = badgeW + pad + 2;
        var comboW = Math.Max(90, xFav - pad - xCombo);

        IndexBadge.SetBounds(2, (RowHeight - 24) / 2, badgeW, 24);
        Combo.SetBounds(xCombo, (RowHeight - 26) / 2, comboW, 26);
        FavoriteButton.SetBounds(xFav, (RowHeight - 26) / 2, favW, 26);
        Meta.SetBounds(xMeta, (RowHeight - 22) / 2, metaW, 22);
        _clockPanel.SetBounds(xClock, (RowHeight - 34) / 2, clockW, 34);
        RemoveButton.SetBounds(xRemove, (RowHeight - 28) / 2, removeW, 28);

        _clockPanel.BringToFront();
        RemoveButton.BringToFront();
    }

    public TimezoneOption? SelectedOption => Combo.SelectedOption;

    public void SetIndex(int oneBased) => IndexBadge.Text = oneBased.ToString();

    public void RefreshFavoriteVisual(bool isFavorite)
    {
        FavoriteButton.Text = isFavorite ? "*" : "o";
        FavoriteButton.BackColor = isFavorite ? Color.FromArgb(254, 243, 199) : UiTheme.AccentSoft;
        FavoriteButton.ForeColor = isFavorite ? Color.FromArgb(180, 83, 9) : UiTheme.Accent;
    }

    public bool SelectWindowsId(string windowsId) => Combo.SelectWindowsId(windowsId);

    public bool SelectAbbreviation(string abbreviation) => Combo.SelectAbbreviation(abbreviation);

    public void ApplyFavorites(IEnumerable<string> favorites)
    {
        var id = SelectedOption?.WindowsId;
        Combo.SetFavorites(favorites);
        if (id is not null)
            Combo.SelectWindowsId(id);
        RefreshFavoriteVisual(id is not null &&
            favorites.Any(f => string.Equals(f, id, StringComparison.OrdinalIgnoreCase)));
    }

    internal sealed class ClockFace
    {
        private readonly Label _label;
        public ClockFace(Label label) => _label = label;
        public string TimeText
        {
            get => _label.Text;
            set => _label.Text = value;
        }
    }
}
