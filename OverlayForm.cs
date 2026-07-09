using System.ComponentModel;

namespace TimezoneConverter;

/// <summary>
/// Sticky-note style always-on-top overlay showing local time and converted zones.
/// Supports opacity, position lock, and compact mode.
/// </summary>
internal sealed class OverlayForm : Form
{
    private readonly Panel _titleBar = new();
    private readonly Label _titleLabel = new();
    private readonly Button _closeButton = new();
    private readonly Button _lockButton = new();
    private readonly Button _compactButton = new();
    private readonly TrackBar _opacityBar = new();
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
            _localPanel.Height = _compact ? 36 : 58;
            _localTimeLabel.Font = new Font("Consolas", _compact ? 12f : 18f, FontStyle.Bold);
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
        MinimumSize = new Size(220, 100);
        Size = new Size(280, 240);
        BackColor = Color.FromArgb(15, 23, 42);
        Opacity = 0.94;
        Font = UiTheme.BodyFont;
        DoubleBuffered = true;
        Padding = new Padding(0);

        BuildUi();
    }

    private void BuildUi()
    {
        _titleBar.Dock = DockStyle.Top;
        _titleBar.Height = 32;
        _titleBar.BackColor = Color.FromArgb(30, 41, 59);
        _titleBar.Padding = new Padding(8, 0, 2, 0);
        _titleBar.Cursor = Cursors.SizeAll;
        _titleBar.MouseDown += TitleBar_MouseDown;
        _titleBar.MouseMove += TitleBar_MouseMove;
        _titleBar.MouseUp += (_, _) => _dragging = false;
        _titleBar.DoubleClick += (_, _) => OpenMainRequested?.Invoke(this, EventArgs.Empty);

        _titleLabel.Text = "ZoneShift";
        _titleLabel.Font = new Font("Segoe UI Semibold", 9f);
        _titleLabel.ForeColor = Color.FromArgb(226, 232, 240);
        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _titleLabel.BackColor = Color.Transparent;
        _titleLabel.Cursor = Cursors.SizeAll;
        _titleLabel.MouseDown += TitleBar_MouseDown;
        _titleLabel.MouseMove += TitleBar_MouseMove;
        _titleLabel.MouseUp += (_, _) => _dragging = false;
        _titleLabel.DoubleClick += (_, _) => OpenMainRequested?.Invoke(this, EventArgs.Empty);

        _closeButton.Text = "x";
        _closeButton.Dock = DockStyle.Right;
        _closeButton.Width = 26;
        StyleTitleButton(_closeButton);
        _closeButton.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);

        _compactButton.Text = "C";
        _compactButton.Dock = DockStyle.Right;
        _compactButton.Width = 26;
        StyleTitleButton(_compactButton);
        _compactButton.Click += (_, _) => IsCompact = !IsCompact;
        // Tooltips via title: C=compact, E=expand, L=locked, U=unlocked

        _lockButton.Text = "U";
        _lockButton.Dock = DockStyle.Right;
        _lockButton.Width = 26;
        StyleTitleButton(_lockButton);
        _lockButton.Click += (_, _) =>
        {
            IsLocked = !IsLocked;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        };

        _titleBar.Controls.Add(_titleLabel);
        _titleBar.Controls.Add(_lockButton);
        _titleBar.Controls.Add(_compactButton);
        _titleBar.Controls.Add(_closeButton);

        // Opacity strip
        var opacityPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 22,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(8, 0, 8, 0)
        };
        var opacityLabel = new Label
        {
            Text = "Opacity",
            Dock = DockStyle.Left,
            Width = 48,
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 7.5f),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _opacityBar.Dock = DockStyle.Fill;
        _opacityBar.Minimum = 40;
        _opacityBar.Maximum = 100;
        _opacityBar.TickFrequency = 10;
        _opacityBar.Value = 94;
        _opacityBar.BackColor = Color.FromArgb(15, 23, 42);
        _opacityBar.ValueChanged += (_, _) =>
        {
            Opacity = _opacityBar.Value / 100.0;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        };
        opacityPanel.Controls.Add(_opacityBar);
        opacityPanel.Controls.Add(opacityLabel);

        _localPanel.Dock = DockStyle.Top;
        _localPanel.Height = 58;
        _localPanel.BackColor = Color.FromArgb(15, 23, 42);
        _localPanel.Padding = new Padding(12, 6, 12, 4);

        _localLabel.Text = "YOUR TIME";
        _localLabel.Font = new Font("Segoe UI", 7.5f);
        _localLabel.ForeColor = Color.FromArgb(148, 163, 184);
        _localLabel.Dock = DockStyle.Top;
        _localLabel.Height = 14;
        _localLabel.BackColor = Color.Transparent;

        _localTimeLabel.Text = "--:--";
        _localTimeLabel.Font = new Font("Consolas", 18f, FontStyle.Bold);
        _localTimeLabel.ForeColor = Color.FromArgb(52, 211, 153);
        _localTimeLabel.Dock = DockStyle.Fill;
        _localTimeLabel.TextAlign = ContentAlignment.MiddleLeft;
        _localTimeLabel.BackColor = Color.Transparent;

        _localPanel.Controls.Add(_localTimeLabel);
        _localPanel.Controls.Add(_localLabel);

        _rowsHost.Dock = DockStyle.Fill;
        _rowsHost.FlowDirection = FlowDirection.TopDown;
        _rowsHost.WrapContents = false;
        _rowsHost.AutoScroll = true;
        _rowsHost.BackColor = Color.FromArgb(15, 23, 42);
        _rowsHost.Padding = new Padding(10, 4, 10, 10);

        Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(51, 65, 85));
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
        Controls.Add(opacityPanel);
        Controls.Add(_titleBar);
    }

    private static void StyleTitleButton(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.ForeColor = Color.FromArgb(148, 163, 184);
        b.BackColor = Color.FromArgb(30, 41, 59);
        b.Cursor = Cursors.Hand;
        b.Font = new Font("Segoe UI Semibold", 9f);
        b.TabStop = false;
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
            _rows[i].Root.Width = Math.Max(180, _rowsHost.ClientSize.Width - 24);
        }

        var rowH = _compact ? 28 : 40;
        var contentHeight = 32 + 22 + _localPanel.Height + 14 + zones.Count * (rowH + 4);
        Height = Math.Clamp(contentHeight, 100, 560);
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
                Height = compact ? 28 : 40,
                Margin = new Padding(0, 0, 0, 4),
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(8, 2, 8, 2)
            };

            _name = new Label
            {
                Dock = DockStyle.Left,
                Width = 52,
                Font = new Font("Segoe UI Semibold", 8f),
                ForeColor = Color.FromArgb(165, 180, 252),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };

            _meta = new Label
            {
                Dock = DockStyle.Right,
                Width = 54,
                Font = new Font("Segoe UI", 7f),
                ForeColor = Color.FromArgb(148, 163, 184),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };

            _time = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", compact ? 10f : 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 211, 153),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
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
            Root.Height = compact ? 28 : 40;
            _time.Font = new Font("Consolas", compact ? 10f : 12f, FontStyle.Bold);
        }
    }
}
