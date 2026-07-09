using System.ComponentModel;

namespace TimezoneConverter;

/// <summary>
/// Sticky always-on-top overlay showing local time and converted zones.
/// Colors follow the active <see cref="UiTheme"/>.
/// </summary>
internal sealed class OverlayForm : Form
{
    private readonly Panel _titleBar = new();
    private readonly Label _titleLabel = new();
    private readonly Button _closeButton = new();
    private readonly Button _lockButton = new();
    private readonly Button _compactButton = new();
    private readonly TrackBar _opacityBar = new();
    private readonly Panel _opacityPanel = new();
    private readonly Label _opacityLabel = new();
    private readonly Panel _localPanel = new();
    private readonly Label _localLabel = new();
    private readonly Label _localTimeLabel = new();
    private readonly FlowLayoutPanel _rowsHost = new();
    private readonly List<OverlayRow> _rows = [];

    private bool _dragging;
    private Point _dragOffset;
    private bool _locked;
    private bool _compact;

    public event EventHandler? CloseRequested;
    public event EventHandler? OpenMainRequested;
    public event EventHandler? SettingsChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool IsLocked
    {
        get => _locked;
        set
        {
            _locked = value;
            _lockButton.Text = _locked ? "L" : "U";
            _titleBar.Cursor = _locked ? Cursors.Default : Cursors.SizeAll;
            _titleLabel.Cursor = _titleBar.Cursor;
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool IsCompact
    {
        get => _compact;
        set
        {
            _compact = value;
            _compactButton.Text = _compact ? "E" : "C";
            _localPanel.Height = _compact ? 40 : 62;
            _localTimeLabel.Font = new Font("Consolas", _compact ? 13f : 20f, FontStyle.Bold);
            foreach (var row in _rows)
                row.SetCompact(_compact);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public double OverlayOpacity
    {
        get => Opacity;
        set
        {
            Opacity = Math.Clamp(value, 0.4, 1.0);
            _opacityBar.Value = (int)Math.Round(Opacity * 100);
        }
    }

    public OverlayForm()
    {
        Text = "ZoneShift Overlay";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        MinimumSize = new Size(240, 120);
        Size = new Size(300, 260);
        BackColor = UiTheme.AppBackground;
        Opacity = 0.94;
        Font = UiTheme.BodyFont;
        DoubleBuffered = true;
        Padding = new Padding(0);

        BuildUi();
        ApplyThemeChrome();
        UiTheme.ThemeChanged += OnThemeChanged;
        Disposed += (_, _) => UiTheme.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged()
    {
        if (IsDisposed) return;
        ApplyThemeChrome();
        foreach (var row in _rows)
            row.ApplyTheme();
        Invalidate(true);
    }

    private void ApplyThemeChrome()
    {
        BackColor = UiTheme.AppBackground;
        _titleBar.BackColor = UiTheme.HeaderBack;
        _titleLabel.ForeColor = UiTheme.TextPrimary;
        _titleLabel.BackColor = UiTheme.HeaderBack;
        StyleTitleButton(_closeButton);
        StyleTitleButton(_lockButton);
        StyleTitleButton(_compactButton);

        _opacityPanel.BackColor = UiTheme.AppBackground;
        _opacityLabel.ForeColor = UiTheme.TextSecondary;
        _opacityLabel.BackColor = UiTheme.AppBackground;
        _opacityBar.BackColor = UiTheme.AppBackground;

        _localPanel.BackColor = UiTheme.CardFace;
        _localLabel.ForeColor = UiTheme.TextSecondary;
        _localLabel.BackColor = UiTheme.CardFace;
        _localTimeLabel.ForeColor = UiTheme.ClockFore;
        _localTimeLabel.BackColor = UiTheme.CardFace;

        _rowsHost.BackColor = UiTheme.AppBackground;
    }

    private void BuildUi()
    {
        _titleBar.Dock = DockStyle.Top;
        _titleBar.Height = 34;
        _titleBar.Padding = new Padding(12, 0, 4, 0);
        _titleBar.Cursor = Cursors.SizeAll;
        _titleBar.MouseDown += TitleBar_MouseDown;
        _titleBar.MouseMove += TitleBar_MouseMove;
        _titleBar.MouseUp += (_, _) => _dragging = false;
        _titleBar.DoubleClick += (_, _) => OpenMainRequested?.Invoke(this, EventArgs.Empty);

        _titleLabel.Text = "ZoneShift";
        _titleLabel.Font = new Font("Segoe UI Semibold", 9f);
        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _titleLabel.Cursor = Cursors.SizeAll;
        _titleLabel.MouseDown += TitleBar_MouseDown;
        _titleLabel.MouseMove += TitleBar_MouseMove;
        _titleLabel.MouseUp += (_, _) => _dragging = false;
        _titleLabel.DoubleClick += (_, _) => OpenMainRequested?.Invoke(this, EventArgs.Empty);

        _closeButton.Text = "x";
        _closeButton.Dock = DockStyle.Right;
        _closeButton.Width = 28;
        _closeButton.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);

        _compactButton.Text = "C";
        _compactButton.Dock = DockStyle.Right;
        _compactButton.Width = 28;
        _compactButton.Click += (_, _) => IsCompact = !IsCompact;

        _lockButton.Text = "U";
        _lockButton.Dock = DockStyle.Right;
        _lockButton.Width = 28;
        _lockButton.Click += (_, _) =>
        {
            IsLocked = !IsLocked;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        };

        _titleBar.Controls.Add(_titleLabel);
        _titleBar.Controls.Add(_lockButton);
        _titleBar.Controls.Add(_compactButton);
        _titleBar.Controls.Add(_closeButton);

        _opacityPanel.Dock = DockStyle.Top;
        _opacityPanel.Height = 26;
        _opacityPanel.Padding = new Padding(12, 2, 12, 2);

        _opacityLabel.Text = "Opacity";
        _opacityLabel.Dock = DockStyle.Left;
        _opacityLabel.Width = 52;
        _opacityLabel.Font = new Font("Segoe UI", 7.5f);
        _opacityLabel.TextAlign = ContentAlignment.MiddleLeft;

        _opacityBar.Dock = DockStyle.Fill;
        _opacityBar.Minimum = 40;
        _opacityBar.Maximum = 100;
        _opacityBar.TickFrequency = 10;
        _opacityBar.Value = 94;
        _opacityBar.ValueChanged += (_, _) =>
        {
            Opacity = _opacityBar.Value / 100.0;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        };
        _opacityPanel.Controls.Add(_opacityBar);
        _opacityPanel.Controls.Add(_opacityLabel);

        _localPanel.Dock = DockStyle.Top;
        _localPanel.Height = 62;
        _localPanel.Padding = new Padding(14, 8, 14, 8);

        _localLabel.Text = "YOUR TIME";
        _localLabel.Font = new Font("Segoe UI Semibold", 7.5f);
        _localLabel.Dock = DockStyle.Top;
        _localLabel.Height = 16;

        _localTimeLabel.Text = "--:--";
        _localTimeLabel.Font = new Font("Consolas", 20f, FontStyle.Bold);
        _localTimeLabel.Dock = DockStyle.Fill;
        _localTimeLabel.TextAlign = ContentAlignment.MiddleLeft;

        _localPanel.Controls.Add(_localTimeLabel);
        _localPanel.Controls.Add(_localLabel);

        _rowsHost.Dock = DockStyle.Fill;
        _rowsHost.FlowDirection = FlowDirection.TopDown;
        _rowsHost.WrapContents = false;
        _rowsHost.AutoScroll = true;
        _rowsHost.Padding = new Padding(12, 8, 12, 12);

        Paint += (_, e) =>
        {
            using var pen = new Pen(UiTheme.CardBorder);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open ZoneShift", null, (_, _) => OpenMainRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("Hide overlay", null, (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty));
        ContextMenuStrip = menu;
        _titleBar.ContextMenuStrip = menu;
        _rowsHost.ContextMenuStrip = menu;

        Controls.Add(_rowsHost);
        Controls.Add(_localPanel);
        Controls.Add(_opacityPanel);
        Controls.Add(_titleBar);
    }

    private static void StyleTitleButton(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.ForeColor = UiTheme.TextSecondary;
        b.BackColor = UiTheme.HeaderBack;
        b.FlatAppearance.MouseOverBackColor = UiTheme.SegmentIdle;
        b.Cursor = Cursors.Hand;
        b.Font = new Font("Segoe UI Semibold", 9f);
        b.TabStop = false;
        b.UseVisualStyleBackColor = false;
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_locked || e.Button != MouseButtons.Left)
            return;
        _dragging = true;
        _dragOffset = e.Location;
    }

    private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging || _locked)
            return;
        var screen = PointToScreen(e.Location);
        Location = new Point(screen.X - _dragOffset.X, screen.Y - _dragOffset.Y);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.OnFormClosing(e);
    }

    public void UpdateDisplay(string localTimeText, string localCaption, IReadOnlyList<(string label, string time, string meta)> zones)
    {
        if (IsDisposed)
            return;

        _localTimeLabel.Text = localTimeText;
        _localLabel.Text = string.IsNullOrWhiteSpace(localCaption)
            ? "YOUR TIME"
            : localCaption.ToUpperInvariant();

        while (_rows.Count < zones.Count)
        {
            var row = new OverlayRow(_compact);
            _rows.Add(row);
            _rowsHost.Controls.Add(row.Root);
        }

        while (_rows.Count > zones.Count)
        {
            var last = _rows[^1];
            _rows.RemoveAt(_rows.Count - 1);
            _rowsHost.Controls.Remove(last.Root);
            last.Root.Dispose();
        }

        for (var i = 0; i < zones.Count; i++)
        {
            var (label, time, meta) = zones[i];
            _rows[i].Set(label, time, meta);
            _rows[i].Root.Width = Math.Max(200, _rowsHost.ClientSize.Width - 28);
        }

        var rowH = _compact ? 32 : 42;
        var contentHeight = 34 + 26 + _localPanel.Height + 16 + zones.Count * (rowH + 6);
        Height = Math.Clamp(contentHeight, 120, 580);
    }

    private sealed class OverlayRow
    {
        public Panel Root { get; }
        private readonly Label _name;
        private readonly Label _time;
        private readonly Label _meta;
        private bool _compact;

        public OverlayRow(bool compact)
        {
            _compact = compact;
            Root = new Panel
            {
                Height = compact ? 32 : 42,
                Margin = new Padding(0, 0, 0, 6),
                BackColor = UiTheme.CardFace,
                Padding = new Padding(10, 4, 10, 4)
            };

            _name = new Label
            {
                Dock = DockStyle.Left,
                Width = 56,
                Font = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = UiTheme.Accent,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = UiTheme.CardFace,
                AutoEllipsis = true
            };

            _meta = new Label
            {
                Dock = DockStyle.Right,
                Width = 60,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = UiTheme.TextSecondary,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = UiTheme.CardFace,
                AutoEllipsis = true
            };

            _time = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", compact ? 11f : 13f, FontStyle.Bold),
                ForeColor = UiTheme.ClockFore,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = UiTheme.CardFace,
                AutoEllipsis = true
            };

            Root.Controls.Add(_time);
            Root.Controls.Add(_meta);
            Root.Controls.Add(_name);
        }

        public void Set(string label, string time, string meta)
        {
            _name.Text = label;
            _time.Text = time;
            _meta.Text = meta;
        }

        public void SetCompact(bool compact)
        {
            _compact = compact;
            Root.Height = compact ? 32 : 42;
            _time.Font = new Font("Consolas", compact ? 11f : 13f, FontStyle.Bold);
        }

        public void ApplyTheme()
        {
            Root.BackColor = UiTheme.CardFace;
            _name.ForeColor = UiTheme.Accent;
            _name.BackColor = UiTheme.CardFace;
            _meta.ForeColor = UiTheme.TextSecondary;
            _meta.BackColor = UiTheme.CardFace;
            _time.ForeColor = UiTheme.ClockFore;
            _time.BackColor = UiTheme.CardFace;
        }
    }
}
