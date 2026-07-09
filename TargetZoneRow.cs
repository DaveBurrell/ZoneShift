namespace TimezoneConverter;

/// <summary>
/// One target row with clock right-aligned so long zone names never cover the time.
/// Layout: [badge][combo........][offset][  CLOCK  ][x]
/// </summary>
internal sealed class TargetZoneRow
{
    public const int RowHeight = 46;

    public Control Root { get; }
    public ClippedComboBox Combo { get; }
    public Label Meta { get; }
    public Button RemoveButton { get; }
    public Label IndexBadge { get; }
    public ClockFace Clock { get; }

    private readonly Panel _clockPanel;
    private readonly Label _clockLabel;

    public TargetZoneRow(IReadOnlyList<TimezoneOption> options, EventHandler onChanged, EventHandler onRemove)
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

        Combo = new ClippedComboBox
        {
            Font = UiTheme.BodyFont,
            Size = new Size(180, 26)
        };
        // Use Items.Add (not DataSource) so selection works before the control handle exists
        Combo.BeginUpdate();
        try
        {
            foreach (var opt in options)
                Combo.Items.Add(opt);
        }
        finally
        {
            Combo.EndUpdate();
        }

        Combo.SelectedIndexChanged += onChanged;

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

        // Add clock before combo so combo never paints over it if bounds glitch
        row.Controls.Add(IndexBadge);
        row.Controls.Add(RemoveButton);
        row.Controls.Add(_clockPanel);
        row.Controls.Add(Meta);
        row.Controls.Add(Combo);
        row.Resize += (_, _) => LayoutRow(row.ClientSize.Width);

        Root = row;
        LayoutRow(640);
    }

    private void LayoutRow(int width)
    {
        const int pad = 8;
        const int badgeW = 22;
        const int metaW = 64;
        const int clockW = 168;
        const int removeW = 28;

        // Right-align remove + clock + meta so the time is always fully visible
        var xRemove = width - removeW - 2;
        var xClock = xRemove - pad - clockW;
        var xMeta = xClock - pad - metaW;
        var xCombo = badgeW + pad + 2;
        var comboW = Math.Max(100, xMeta - pad - xCombo);

        IndexBadge.SetBounds(2, (RowHeight - 24) / 2, badgeW, 24);
        Combo.SetBounds(xCombo, (RowHeight - 26) / 2, comboW, 26);
        Meta.SetBounds(xMeta, (RowHeight - 22) / 2, metaW, 22);
        _clockPanel.SetBounds(xClock, (RowHeight - 34) / 2, clockW, 34);
        RemoveButton.SetBounds(xRemove, (RowHeight - 28) / 2, removeW, 28);

        // Ensure clock is topmost in z-order
        _clockPanel.BringToFront();
        RemoveButton.BringToFront();
    }

    public TimezoneOption? SelectedOption
    {
        get
        {
            if (Combo.SelectedItem is TimezoneOption opt)
                return opt;
            if (Combo.SelectedIndex >= 0 && Combo.SelectedIndex < Combo.Items.Count)
                return Combo.Items[Combo.SelectedIndex] as TimezoneOption;
            return null;
        }
    }

    public void SetIndex(int oneBased) => IndexBadge.Text = oneBased.ToString();

    public bool SelectWindowsId(string windowsId)
    {
        for (var i = 0; i < Combo.Items.Count; i++)
        {
            if (Combo.Items[i] is TimezoneOption opt &&
                string.Equals(opt.WindowsId, windowsId, StringComparison.OrdinalIgnoreCase))
            {
                Combo.SelectedIndex = i;
                return true;
            }
        }

        return false;
    }

    public bool SelectAbbreviation(string abbreviation)
    {
        for (var i = 0; i < Combo.Items.Count; i++)
        {
            if (Combo.Items[i] is TimezoneOption opt &&
                string.Equals(opt.Abbreviation, abbreviation, StringComparison.OrdinalIgnoreCase))
            {
                Combo.SelectedIndex = i;
                return true;
            }
        }

        if (Combo.Items.Count > 0)
        {
            Combo.SelectedIndex = 0;
            return false;
        }

        return false;
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
