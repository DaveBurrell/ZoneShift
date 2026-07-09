namespace TimezoneConverter;

/// <summary>
/// Newsroom wall tile: painted chassis + LED module + zone picker.
/// Inner layout uses consistent insets so chrome never kisses the rounded edge.
/// </summary>
internal sealed class TargetZoneRow
{
    public const int TileWidth = LayoutMetrics.TileW;
    public const int TileHeight = LayoutMetrics.TileH;

    public Control Root { get; }
    public SearchableTimezoneBox Combo { get; }
    public Label Meta { get; }
    public Button RemoveButton { get; }
    public Button FavoriteButton { get; }
    public Label IndexBadge { get; }
    public ClockFace Clock { get; }

    private readonly LedClockDisplay _led;

    public TargetZoneRow(
        IReadOnlyList<TimezoneOption> options,
        IEnumerable<string> favorites,
        EventHandler onChanged,
        EventHandler onRemove,
        EventHandler onFavorite)
    {
        const int pad = LayoutMetrics.TileInner;
        const int chromeH = 26;
        const int comboH = 28;
        const int gap = 8;

        var tile = new ClockTilePanel
        {
            Width = TileWidth,
            Height = TileHeight,
            MinimumSize = new Size(TileWidth, TileHeight),
            MaximumSize = new Size(TileWidth, TileHeight),
            Margin = new Padding(LayoutMetrics.TileMargin)
        };

        FavoriteButton = new Button
        {
            Text = "*",
            Font = new Font("Segoe UI Semibold", 9f),
            Size = new Size(26, 24),
            Location = new Point(pad, pad),
            TabStop = false
        };
        UiTheme.StyleSecondaryButton(FavoriteButton);
        FavoriteButton.Click += onFavorite;

        IndexBadge = new Label
        {
            Text = "1",
            Font = new Font("Segoe UI Semibold", 8f),
            ForeColor = UiTheme.TextSecondary,
            BackColor = UiTheme.TileBack,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(24, 20),
            Location = new Point(pad + 30, pad + 2)
        };

        RemoveButton = new Button
        {
            Text = "x",
            Font = new Font("Segoe UI Semibold", 9f),
            Size = new Size(26, 24),
            Location = new Point(TileWidth - pad - 26, pad),
            TabStop = false
        };
        UiTheme.StyleDangerButton(RemoveButton);
        RemoveButton.Click += onRemove;

        var ledTop = pad + chromeH + 4;
        var ledH = TileHeight - pad - ledTop - comboH - gap - 2;
        var innerW = TileWidth - pad * 2;

        _led = new LedClockDisplay(large: false)
        {
            Location = new Point(pad, ledTop),
            Size = new Size(innerW, Math.Max(64, ledH)),
            ZoneText = "---",
            TimeText = "--:--",
            CaptionText = "",
            BlinkColons = false,
            GutterColor = UiTheme.TileBack
        };
        Clock = new ClockFace(_led);

        Meta = new Label
        {
            Text = "",
            Visible = false,
            Size = new Size(1, 1),
            Location = new Point(0, 0)
        };

        Combo = new SearchableTimezoneBox
        {
            Font = UiTheme.BodyFont,
            Location = new Point(pad, TileHeight - pad - comboH),
            Size = new Size(innerW, comboH)
        };
        Combo.SetOptions(options, favorites);
        Combo.SelectedIndexChanged += onChanged;
        Combo.SelectionChangeCommitted += onChanged;
        Combo.SelectedIndexChanged += (_, _) => SyncZoneLabel();
        Combo.SelectionChangeCommitted += (_, _) => SyncZoneLabel();

        tile.Controls.Add(FavoriteButton);
        tile.Controls.Add(IndexBadge);
        tile.Controls.Add(RemoveButton);
        tile.Controls.Add(_led);
        tile.Controls.Add(Meta);
        tile.Controls.Add(Combo);

        Root = tile;
        SyncZoneLabel();

        UiTheme.ThemeChanged += ApplyTheme;
        tile.Disposed += (_, _) => UiTheme.ThemeChanged -= ApplyTheme;
    }

    public void ApplyTheme()
    {
        if (Root.IsDisposed) return;
        IndexBadge.ForeColor = UiTheme.TextSecondary;
        IndexBadge.BackColor = UiTheme.TileBack;
        _led.GutterColor = UiTheme.TileBack;
        UiTheme.StyleInput(Combo);
        UiTheme.StyleDangerButton(RemoveButton);
        RefreshFavoriteVisual(string.Equals(FavoriteButton.Text, "*", StringComparison.Ordinal));
        Root.Invalidate(true);
    }

    private void SyncZoneLabel()
    {
        var opt = Combo.SelectedOption;
        _led.ZoneText = opt?.Abbreviation ?? "---";
    }

    public TimezoneOption? SelectedOption => Combo.SelectedOption;

    public void SetIndex(int oneBased) => IndexBadge.Text = oneBased.ToString();

    public void RefreshFavoriteVisual(bool isFavorite)
    {
        FavoriteButton.Text = isFavorite ? "*" : "o";
        if (isFavorite)
        {
            FavoriteButton.BackColor = UiTheme.Accent;
            FavoriteButton.ForeColor = UiTheme.TextOnAccent;
            FavoriteButton.FlatAppearance.BorderColor = UiTheme.Accent;
            FavoriteButton.FlatAppearance.MouseOverBackColor = UiTheme.AccentHover;
        }
        else
        {
            UiTheme.StyleSecondaryButton(FavoriteButton);
            FavoriteButton.ForeColor = UiTheme.TextSecondary;
        }
    }

    public bool SelectWindowsId(string windowsId)
    {
        var ok = Combo.SelectWindowsId(windowsId);
        SyncZoneLabel();
        return ok;
    }

    public bool SelectAbbreviation(string abbreviation)
    {
        var ok = Combo.SelectAbbreviation(abbreviation);
        SyncZoneLabel();
        return ok;
    }

    public void ApplyFavorites(IEnumerable<string> favorites)
    {
        var id = SelectedOption?.WindowsId;
        Combo.SetFavorites(favorites);
        if (id is not null)
            Combo.SelectWindowsId(id);
        SyncZoneLabel();
        RefreshFavoriteVisual(id is not null &&
            favorites.Any(f => string.Equals(f, id, StringComparison.OrdinalIgnoreCase)));
    }

    internal sealed class ClockFace
    {
        private readonly LedClockDisplay _led;

        public ClockFace(LedClockDisplay led) => _led = led;

        public string TimeText
        {
            get => _led.TimeText;
            set => _led.TimeText = value;
        }

        public string ZoneText
        {
            get => _led.ZoneText;
            set => _led.ZoneText = value;
        }

        public string CaptionText
        {
            get => _led.CaptionText;
            set => _led.CaptionText = value;
        }
    }
}
