namespace TimezoneConverter;

/// <summary>
/// Sticky-note style always-on-top overlay showing local time and converted zones.
/// </summary>
internal sealed class OverlayForm : Form
{
    private readonly Panel _titleBar = new();
    private readonly Label _titleLabel = new();
    private readonly Button _closeButton = new();
    private readonly Label _localLabel = new();
    private readonly Label _localTimeLabel = new();
    private readonly FlowLayoutPanel _rowsHost = new();
    private readonly List<OverlayRow> _rows = [];

    private bool _dragging;
    private Point _dragOffset;

    public event EventHandler? CloseRequested;
    public event EventHandler? OpenMainRequested;

    public OverlayForm()
    {
        Text = "ZoneShift Overlay";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        MinimumSize = new Size(220, 120);
        Size = new Size(260, 220);
        BackColor = Color.FromArgb(15, 23, 42);
        Opacity = 0.94;
        Font = UiTheme.BodyFont;
        DoubleBuffered = true;
        Padding = new Padding(0);

        BuildUi();
    }

    private void BuildUi()
    {
        // Title bar (drag handle)
        _titleBar.Dock = DockStyle.Top;
        _titleBar.Height = 32;
        _titleBar.BackColor = Color.FromArgb(30, 41, 59);
        _titleBar.Padding = new Padding(10, 0, 4, 0);
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

        _closeButton.Text = "×";
        _closeButton.Dock = DockStyle.Right;
        _closeButton.Width = 28;
        _closeButton.FlatStyle = FlatStyle.Flat;
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.ForeColor = Color.FromArgb(148, 163, 184);
        _closeButton.BackColor = Color.FromArgb(30, 41, 59);
        _closeButton.Cursor = Cursors.Hand;
        _closeButton.Font = new Font("Segoe UI Semibold", 12f);
        _closeButton.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);

        _titleBar.Controls.Add(_titleLabel);
        _titleBar.Controls.Add(_closeButton);

        // Local time block
        var localPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(12, 8, 12, 4)
        };

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

        localPanel.Controls.Add(_localTimeLabel);
        localPanel.Controls.Add(_localLabel);

        // Converted rows
        _rowsHost.Dock = DockStyle.Fill;
        _rowsHost.FlowDirection = FlowDirection.TopDown;
        _rowsHost.WrapContents = false;
        _rowsHost.AutoScroll = true;
        _rowsHost.BackColor = Color.FromArgb(15, 23, 42);
        _rowsHost.Padding = new Padding(10, 4, 10, 10);

        // Subtle border
        Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(51, 65, 85));
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };

        Controls.Add(_rowsHost);
        Controls.Add(localPanel);
        Controls.Add(_titleBar);

        // Context menu
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open ZoneShift", null, (_, _) => OpenMainRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("Hide overlay", null, (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty));
        ContextMenuStrip = menu;
        _titleBar.ContextMenuStrip = menu;
        _rowsHost.ContextMenuStrip = menu;
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;
        _dragging = true;
        _dragOffset = e.Location;
    }

    private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging)
            return;
        var screen = PointToScreen(e.Location);
        Location = new Point(screen.X - _dragOffset.X, screen.Y - _dragOffset.Y);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Hide instead of dispose when user hits Alt+F4 on overlay
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.OnFormClosing(e);
    }

    public void SetOpacity(double opacity)
    {
        Opacity = Math.Clamp(opacity, 0.4, 1.0);
    }

    public void UpdateDisplay(string localTimeText, string localCaption, IReadOnlyList<(string label, string time, string meta)> zones)
    {
        if (IsDisposed)
            return;

        _localTimeLabel.Text = localTimeText;
        _localLabel.Text = string.IsNullOrWhiteSpace(localCaption) ? "YOUR TIME" : localCaption.ToUpperInvariant();

        // Resize row pool
        while (_rows.Count < zones.Count)
        {
            var row = new OverlayRow();
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

        // Auto-size height to content (capped)
        var contentHeight = 32 + 58 + 14 + zones.Count * 44;
        Height = Math.Clamp(contentHeight, 120, 520);
    }

    private sealed class OverlayRow
    {
        public Panel Root { get; }
        private readonly Label _name;
        private readonly Label _time;
        private readonly Label _meta;

        public OverlayRow()
        {
            Root = new Panel
            {
                Height = 40,
                Margin = new Padding(0, 0, 0, 4),
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(8, 4, 8, 4)
            };

            _name = new Label
            {
                Dock = DockStyle.Left,
                Width = 56,
                Font = new Font("Segoe UI Semibold", 8.5f),
                ForeColor = Color.FromArgb(165, 180, 252),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };

            _meta = new Label
            {
                Dock = DockStyle.Right,
                Width = 58,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };

            _time = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 12f, FontStyle.Bold),
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
    }
}
