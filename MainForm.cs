namespace TimezoneConverter;

public sealed class MainForm : Form
{
    private readonly TimeZoneInfo _localTimezone = TimeZoneInfo.Local;
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly System.Windows.Forms.Timer _liveTimer = new();

    private readonly Label _localTimezoneLabel = new();
    private readonly Label _sourceSectionTitle = new();
    private readonly Label _inputZoneCaption = new();
    private readonly Label _dateFieldCaption = new();
    private readonly Label _timeFieldCaption = new();
    private readonly Panel _reverseZoneRow = new();
    private Panel _sourceWrap = null!;
    private readonly ClippedComboBox _reverseSourceTimezone = new();
    private readonly DateTimePicker _datePicker = new();
    private readonly TimeEntryCombo _timeEntry = new();
    private readonly CheckBox _liveModeCheck = new();
    private readonly Button _useNowButton = new();
    private readonly SegmentedToggle _formatToggle = new("12-hour", "24-hour");
    private readonly SegmentedToggle _directionToggle = new("From my zone", "To my zone");
    private readonly CheckBox _overlayCheck = new();
    private readonly DigitalClockPanel _primaryClock = new(large: true);
    private readonly Label _statusLabel = new();
    private readonly Button _addTimezoneButton = new();
    private readonly List<TargetZoneRow> _targetRows = [];
    private FlowLayoutPanel _targetListHost = null!;
    private Label _targetCountLabel = null!;

    private List<TimezoneOption> _timezoneOptions = [];
    private OverlayForm? _overlay;
    private ToolStripMenuItem _trayOverlayItem = null!;

    private NotifyIcon _trayIcon = null!;
    private ContextMenuStrip _trayMenu = null!;
    private bool _exitRequested;
    private bool _shownTrayTip;

    private bool _suppressEvents;
    private bool _ready;
    private bool _liveMode = true;

    private const int MinTargetZones = 1;
    private const int MaxTargetZones = 8;
    private static readonly string[] DefaultTargetAbbreviations = ["IST", "CST", "EST", "PST", "GMT"];

    private bool Use24Hour => _formatToggle.RightSelected;
    private bool ConvertToLocal => _directionToggle.RightSelected;

    public MainForm()
    {
        Text = "ZoneShift";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        // Absolute layout - no autoscaling (prevents collapsed/overlapping sections)
        AutoScaleMode = AutoScaleMode.None;
        Font = UiTheme.BodyFont;
        BackColor = UiTheme.AppBackground;
        DoubleBuffered = true;
        ShowInTaskbar = true;
        ClientSize = new Size(700, 780);
        MinimumSize = new Size(680, 720);

        ApplyAppIcon();
        BuildUi();

        // Size after controls are added so layout isn't crushed
        ClientSize = new Size(700, 780);

        SetupTrayIcon();
        ShowDetectedLocalTimezone();
        LoadTimezones();
        ApplySettingsAndDefaults();
        ApplyDirectionUi();
        SetupLiveTimer();
        _ready = true;
        RefreshDisplays();

        if (_settings.OverlayVisible)
            ShowOverlay();

        Shown += (_, _) =>
        {
            ClientSize = new Size(700, 780);
            // Re-apply saved zones after handles exist (ComboBox selection is reliable now)
            RestoreSavedTargetSelections();
            RebuildTargetListUi();
            RefreshDisplays();
            PersistSettings();
        };

        FormClosing += OnFormClosing;
        Resize += OnFormResize;
        Application.ApplicationExit += (_, _) => PersistSettings();
    }

    private void ApplyAppIcon()
    {
        try
        {
            var icon = LoadAppIcon();
            if (icon is not null)
                Icon = icon;
        }
        catch
        {
            // default icon
        }
    }

    private static Icon? LoadAppIcon()
    {
        try
        {
            var exe = Application.ExecutablePath;
            if (!string.IsNullOrWhiteSpace(exe) && File.Exists(exe))
            {
                var extracted = Icon.ExtractAssociatedIcon(exe);
                if (extracted is not null)
                    return extracted;
            }
        }
        catch
        {
            // continue
        }

        foreach (var path in new[]
                 {
                     Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"),
                     Path.Combine(AppContext.BaseDirectory, "app.ico"),
                 })
        {
            if (File.Exists(path))
                return new Icon(path);
        }

        return null;
    }

    private void SetupTrayIcon()
    {
        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Open ZoneShift", null, (_, _) => RestoreFromTray());
        _trayOverlayItem = new ToolStripMenuItem("Desktop overlay")
        {
            CheckOnClick = true,
            Checked = _settings.OverlayVisible
        };
        _trayOverlayItem.CheckedChanged += (_, _) =>
        {
            if (_trayOverlayItem.Checked)
                ShowOverlay();
            else
                HideOverlay();
        };
        _trayMenu.Items.Add(_trayOverlayItem);
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add("Exit", null, (_, _) => ExitApplication());

        _trayIcon = new NotifyIcon
        {
            Text = "ZoneShift",
            Visible = true,
            ContextMenuStrip = _trayMenu,
            Icon = Icon ?? SystemIcons.Application
        };

        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        _trayIcon.BalloonTipClicked += (_, _) => RestoreFromTray();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        // Always save preferences first (including close-to-tray)
        SaveOverlayPlacement();
        PersistSettings();

        if (!_exitRequested && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            MinimizeToTray(showTip: true);
            return;
        }

        _liveTimer.Stop();

        if (_overlay is not null && !_overlay.IsDisposed)
        {
            _overlay.CloseRequested -= OnOverlayCloseRequested;
            _overlay.OpenMainRequested -= OnOverlayOpenMain;
            _overlay.Dispose();
            _overlay = null;
        }

        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        _trayMenu?.Dispose();
    }

    private void OnFormResize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
            MinimizeToTray(showTip: false);
    }

    private void MinimizeToTray(bool showTip)
    {
        Hide();
        ShowInTaskbar = false;
        WindowState = FormWindowState.Normal;
        _trayIcon.Visible = true;

        if (showTip && !_shownTrayTip)
        {
            _shownTrayTip = true;
            _trayIcon.BalloonTipTitle = "ZoneShift";
            _trayIcon.BalloonTipText =
                "Still running in the system tray. Double-click the icon to open again, or right-click Exit to quit.";
            _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            _trayIcon.ShowBalloonTip(3500);
        }
    }

    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    private void ExitApplication()
    {
        _exitRequested = true;
        _liveTimer.Stop();
        PersistSettings();
        _trayIcon.Visible = false;
        Application.Exit();
    }

    private void BuildUi()
    {
        SuspendLayout();
        BackColor = UiTheme.AppBackground;

        // Status bar (bottom)
        _statusLabel.Height = 24;
        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.ForeColor = UiTheme.TextMuted;
        _statusLabel.Font = UiTheme.CaptionFont;
        _statusLabel.BackColor = UiTheme.AppBackground;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Padding = new Padding(16, 0, 16, 0);
        _statusLabel.AutoEllipsis = true;
        Controls.Add(_statusLabel);

        // Header (top)
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = UiTheme.Accent
        };
        var title = new Label
        {
            Text = "ZoneShift",
            Font = UiTheme.TitleFont,
            ForeColor = Color.White,
            BackColor = UiTheme.Accent,
            AutoSize = false,
            Location = new Point(20, 8),
            Size = new Size(400, 28)
        };
        var subtitle = new Label
        {
            Text = "Live clocks - convert from your zone or into your zone",
            Font = UiTheme.BodyFont,
            ForeColor = Color.FromArgb(199, 210, 254),
            BackColor = UiTheme.Accent,
            AutoSize = false,
            Location = new Point(22, 36),
            Size = new Size(500, 20)
        };
        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        Controls.Add(header);

        // Options card
        var optionsWrap = new Panel
        {
            Dock = DockStyle.Top,
            Height = 118,
            BackColor = UiTheme.AppBackground,
            Padding = new Padding(16, 10, 16, 0)
        };
        var optionsCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.CardBackground,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(12)
        };

        var dirCap = MakeCaption("CONVERSION DIRECTION");
        dirCap.Location = new Point(12, 8);
        dirCap.Size = new Size(200, 16);
        _directionToggle.Location = new Point(12, 26);
        _directionToggle.Size = new Size(280, 32);
        _directionToggle.SelectionChanged += OnDirectionChanged;

        var fmtCap = MakeCaption("CLOCK FORMAT");
        fmtCap.Location = new Point(320, 8);
        fmtCap.Size = new Size(160, 16);
        _formatToggle.Location = new Point(320, 26);
        _formatToggle.Size = new Size(200, 32);
        _formatToggle.SelectionChanged += OnFormatChanged;

        _overlayCheck.Text = "Desktop overlay (always-on-top mini view)";
        _overlayCheck.Font = new Font("Segoe UI Semibold", 9f);
        _overlayCheck.ForeColor = UiTheme.TextPrimary;
        _overlayCheck.BackColor = UiTheme.CardBackground;
        _overlayCheck.AutoSize = true;
        _overlayCheck.Location = new Point(12, 68);
        _overlayCheck.CheckedChanged += OnOverlayCheckChanged;

        optionsCard.Controls.Add(dirCap);
        optionsCard.Controls.Add(_directionToggle);
        optionsCard.Controls.Add(fmtCap);
        optionsCard.Controls.Add(_formatToggle);
        optionsCard.Controls.Add(_overlayCheck);
        optionsWrap.Controls.Add(optionsCard);
        Controls.Add(optionsWrap);

        // Source card
        _sourceWrap = new Panel
        {
            Dock = DockStyle.Top,
            Height = 200,
            BackColor = UiTheme.AppBackground,
            Padding = new Padding(16, 10, 16, 0)
        };
        var sourceCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.CardBackground,
            BorderStyle = BorderStyle.FixedSingle
        };

        // Right: primary clock (docked so it never overlaps left fields)
        var clockHost = new Panel
        {
            Dock = DockStyle.Right,
            Width = 270,
            BackColor = UiTheme.CardBackground,
            Padding = new Padding(8, 16, 12, 16)
        };
        _primaryClock.Dock = DockStyle.Fill;
        clockHost.Controls.Add(_primaryClock);

        // Left: form fields
        var leftHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.CardBackground,
            Padding = new Padding(12, 8, 8, 8)
        };

        _sourceSectionTitle.Text = "Your time";
        _sourceSectionTitle.Font = UiTheme.SectionFont;
        _sourceSectionTitle.ForeColor = UiTheme.TextPrimary;
        _sourceSectionTitle.BackColor = UiTheme.CardBackground;
        _sourceSectionTitle.AutoSize = false;
        _sourceSectionTitle.Location = new Point(0, 0);
        _sourceSectionTitle.Size = new Size(360, 22);

        var localCap = MakeCaption("YOUR PC TIMEZONE");
        localCap.Location = new Point(0, 26);
        localCap.Size = new Size(360, 14);

        _localTimezoneLabel.Font = new Font("Segoe UI Semibold", 9f);
        _localTimezoneLabel.ForeColor = UiTheme.TextPrimary;
        _localTimezoneLabel.BackColor = UiTheme.CardBackground;
        _localTimezoneLabel.AutoSize = false;
        _localTimezoneLabel.AutoEllipsis = true;
        _localTimezoneLabel.Location = new Point(0, 42);
        _localTimezoneLabel.Size = new Size(380, 20);

        _reverseZoneRow.Location = new Point(0, 66);
        _reverseZoneRow.Size = new Size(380, 48);
        _reverseZoneRow.BackColor = UiTheme.CardBackground;
        _inputZoneCaption.Text = "ENTER TIME IN THIS TIMEZONE";
        _inputZoneCaption.Font = UiTheme.CaptionFont;
        _inputZoneCaption.ForeColor = UiTheme.TextMuted;
        _inputZoneCaption.BackColor = UiTheme.CardBackground;
        _inputZoneCaption.AutoSize = false;
        _inputZoneCaption.Location = new Point(0, 0);
        _inputZoneCaption.Size = new Size(370, 14);
        _reverseSourceTimezone.Location = new Point(0, 16);
        _reverseSourceTimezone.Size = new Size(370, 26);
        _reverseSourceTimezone.Font = UiTheme.BodyFont;
        _reverseSourceTimezone.SelectedIndexChanged += OnReverseSourceChanged;
        _reverseZoneRow.Controls.Add(_inputZoneCaption);
        _reverseZoneRow.Controls.Add(_reverseSourceTimezone);

        _liveModeCheck.Text = "Use current time (live)";
        _liveModeCheck.Font = new Font("Segoe UI Semibold", 9f);
        _liveModeCheck.ForeColor = UiTheme.TextPrimary;
        _liveModeCheck.BackColor = UiTheme.CardBackground;
        _liveModeCheck.AutoSize = true;
        _liveModeCheck.Location = new Point(0, 118);
        _liveModeCheck.Checked = true;
        _liveModeCheck.CheckedChanged += OnLiveModeChanged;

        _useNowButton.Text = "Reset to now";
        _useNowButton.FlatStyle = FlatStyle.Flat;
        _useNowButton.Font = UiTheme.CaptionFont;
        _useNowButton.ForeColor = UiTheme.Accent;
        _useNowButton.BackColor = UiTheme.AccentSoft;
        _useNowButton.FlatAppearance.BorderSize = 0;
        _useNowButton.Cursor = Cursors.Hand;
        _useNowButton.Size = new Size(96, 24);
        _useNowButton.Location = new Point(190, 116);
        _useNowButton.Click += (_, _) => EnterLiveMode();
        _useNowButton.Visible = false;

        _dateFieldCaption.Text = "DATE";
        _dateFieldCaption.Font = UiTheme.CaptionFont;
        _dateFieldCaption.ForeColor = UiTheme.TextMuted;
        _dateFieldCaption.BackColor = UiTheme.CardBackground;
        _dateFieldCaption.AutoSize = false;
        _dateFieldCaption.Location = new Point(0, 146);
        _dateFieldCaption.Size = new Size(120, 14);
        _datePicker.Format = DateTimePickerFormat.Short;
        _datePicker.Location = new Point(0, 162);
        _datePicker.Size = new Size(150, 24);
        _datePicker.ValueChanged += OnInputChanged;

        _timeFieldCaption.Text = "TIME (pick or type)";
        _timeFieldCaption.Font = UiTheme.CaptionFont;
        _timeFieldCaption.ForeColor = UiTheme.TextMuted;
        _timeFieldCaption.BackColor = UiTheme.CardBackground;
        _timeFieldCaption.AutoSize = false;
        _timeFieldCaption.Location = new Point(166, 146);
        _timeFieldCaption.Size = new Size(160, 14);
        _timeEntry.Location = new Point(166, 162);
        _timeEntry.Size = new Size(150, 24);
        _timeEntry.Font = UiTheme.BodyFont;
        _timeEntry.Configure(use24Hour: false, includeSeconds: false);
        _timeEntry.TimeChanged += OnInputChanged;

        leftHost.Controls.Add(_sourceSectionTitle);
        leftHost.Controls.Add(localCap);
        leftHost.Controls.Add(_localTimezoneLabel);
        leftHost.Controls.Add(_reverseZoneRow);
        leftHost.Controls.Add(_liveModeCheck);
        leftHost.Controls.Add(_useNowButton);
        leftHost.Controls.Add(_dateFieldCaption);
        leftHost.Controls.Add(_datePicker);
        leftHost.Controls.Add(_timeFieldCaption);
        leftHost.Controls.Add(_timeEntry);

        sourceCard.Controls.Add(leftHost);
        sourceCard.Controls.Add(clockHost);
        _sourceWrap.Controls.Add(sourceCard);
        Controls.Add(_sourceWrap);

        // Targets card (fills remaining space)
        var targetsWrap = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.AppBackground,
            Padding = new Padding(16, 10, 16, 8)
        };
        var targetsCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.CardBackground,
            BorderStyle = BorderStyle.FixedSingle
        };

        var targetsHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = UiTheme.CardBackground,
            Padding = new Padding(12, 6, 12, 0)
        };
        var section = new Label
        {
            Text = "Other timezones",
            Font = UiTheme.SectionFont,
            ForeColor = UiTheme.TextPrimary,
            BackColor = UiTheme.CardBackground,
            AutoSize = false,
            Dock = DockStyle.Left,
            Width = 180,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _targetCountLabel = new Label
        {
            Text = "",
            Font = UiTheme.CaptionFont,
            ForeColor = UiTheme.TextMuted,
            BackColor = UiTheme.CardBackground,
            AutoSize = false,
            Dock = DockStyle.Right,
            Width = 80,
            TextAlign = ContentAlignment.MiddleRight
        };
        targetsHeader.Controls.Add(_targetCountLabel);
        targetsHeader.Controls.Add(section);

        var footer = new Panel
        {
            Height = 42,
            Dock = DockStyle.Bottom,
            BackColor = UiTheme.CardBackground,
            Padding = new Padding(12, 6, 12, 6)
        };
        _addTimezoneButton.Text = "+ Add timezone";
        _addTimezoneButton.FlatStyle = FlatStyle.Flat;
        _addTimezoneButton.Font = new Font("Segoe UI Semibold", 9f);
        _addTimezoneButton.ForeColor = UiTheme.Accent;
        _addTimezoneButton.BackColor = UiTheme.AccentSoft;
        _addTimezoneButton.FlatAppearance.BorderSize = 0;
        _addTimezoneButton.Cursor = Cursors.Hand;
        _addTimezoneButton.Dock = DockStyle.Left;
        _addTimezoneButton.Width = 140;
        _addTimezoneButton.Click += (_, _) => AddTargetRow(selectAbbreviation: null, persist: true);
        footer.Controls.Add(_addTimezoneButton);

        _targetListHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = UiTheme.CardBackground,
            Padding = new Padding(8, 4, 8, 4)
        };
        _targetListHost.Resize += (_, _) =>
        {
            var w = Math.Max(360, _targetListHost.ClientSize.Width - 28);
            foreach (Control c in _targetListHost.Controls)
                c.Width = w;
        };

        // Fill first, then bottom, then top (docking order)
        targetsCard.Controls.Add(_targetListHost);
        targetsCard.Controls.Add(footer);
        targetsCard.Controls.Add(targetsHeader);

        targetsWrap.Controls.Add(targetsCard);
        Controls.Add(targetsWrap);

        // Dock order: Fill first, then Tops, Bottom already added.
        // Bring header/options/source to correct z by setting child index.
        // With Dock: add Fill last among fills; Tops added after Fill end up above Fill.
        // Correct approach: Controls order - Bottom, Fill, then Tops from bottom to top of screen.
        // We already added: status(Bottom), header(Top), options(Top), source(Top), targets(Fill).
        // Re-order so Fill is under Tops:
        Controls.SetChildIndex(targetsWrap, 0);
        Controls.SetChildIndex(_sourceWrap, 1);
        Controls.SetChildIndex(optionsWrap, 2);
        Controls.SetChildIndex(header, 3);
        Controls.SetChildIndex(_statusLabel, 4);

        ResumeLayout(true);
    }

    private void RebuildTargetListUi()
    {
        if (_targetListHost is null)
            return;

        _targetListHost.SuspendLayout();
        _targetListHost.Controls.Clear();

        var rowWidth = Math.Max(360, _targetListHost.ClientSize.Width - 28);
        if (rowWidth < 360)
            rowWidth = 600;

        for (var i = 0; i < _targetRows.Count; i++)
        {
            _targetRows[i].SetIndex(i + 1);
            _targetRows[i].RemoveButton.Enabled = _targetRows.Count > MinTargetZones;
            _targetRows[i].Root.Width = rowWidth;
            _targetRows[i].Root.Height = TargetZoneRow.RowHeight;
            _targetRows[i].Root.Margin = new Padding(0, 0, 0, 4);
            _targetListHost.Controls.Add(_targetRows[i].Root);
        }

        _addTimezoneButton.Enabled = _targetRows.Count < MaxTargetZones;
        if (_targetCountLabel is not null)
            _targetCountLabel.Text = $"{_targetRows.Count} of {MaxTargetZones}";

        _targetListHost.ResumeLayout(true);
    }

    private void AddTargetRow(string? selectWindowsId, string? selectAbbreviation, bool persist)
    {
        if (_targetRows.Count >= MaxTargetZones)
            return;

        TargetZoneRow? row = null;
        row = new TargetZoneRow(
            _timezoneOptions,
            OnTargetChanged,
            (_, _) =>
            {
                if (row is not null)
                    RemoveTargetRow(row);
            });

        _suppressEvents = true;
        try
        {
            if (!string.IsNullOrWhiteSpace(selectWindowsId))
                row.SelectWindowsId(selectWindowsId);
            else if (!string.IsNullOrWhiteSpace(selectAbbreviation))
                row.SelectAbbreviation(selectAbbreviation);
            else
            {
                var used = _targetRows
                    .Select(r => r.SelectedOption?.WindowsId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Cast<string>()
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var pick = DefaultTargetAbbreviations
                    .Select(abbr => _timezoneOptions.FirstOrDefault(o =>
                        string.Equals(o.Abbreviation, abbr, StringComparison.OrdinalIgnoreCase)))
                    .FirstOrDefault(o => o is not null && !used.Contains(o.WindowsId));

                if (pick is not null)
                    row.SelectWindowsId(pick.WindowsId);
                else if (row.Combo.Items.Count > 0)
                    row.Combo.SelectedIndex = 0;
            }
        }
        finally
        {
            _suppressEvents = false;
        }

        _targetRows.Add(row);
        RebuildTargetListUi();

        if (persist && _ready)
        {
            RefreshDisplays();
            PersistSettings();
        }
    }

    private void AddTargetRow(string? selectAbbreviation, bool persist) =>
        AddTargetRow(selectWindowsId: null, selectAbbreviation: selectAbbreviation, persist: persist);

    private void RemoveTargetRow(TargetZoneRow row)
    {
        if (_targetRows.Count <= MinTargetZones)
            return;

        _targetRows.Remove(row);
        row.Root.Dispose();
        RebuildTargetListUi();

        if (_ready)
        {
            RefreshDisplays();
            PersistSettings();
        }
    }

    private static Label MakeCaption(string text) =>
        new()
        {
            Text = text,
            Font = UiTheme.CaptionFont,
            ForeColor = UiTheme.TextMuted,
            BackColor = UiTheme.CardBackground,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };

    private void SetupLiveTimer()
    {
        _liveTimer.Interval = 1000;
        _liveTimer.Tick += (_, _) =>
        {
            if (!_liveMode || !_ready)
                return;
            SyncPickersToNow();
            RefreshDisplays();
        };
        _liveTimer.Start();
    }

    private void ApplyDirectionUi()
    {
        var reverse = ConvertToLocal;
        _sourceSectionTitle.Text = reverse ? "Enter time in another timezone" : "Your time";
        _reverseZoneRow.Visible = reverse;

        // Fixed Y bands inside leftHost (0,0 origin)
        if (reverse)
        {
            if (_sourceWrap is not null)
                _sourceWrap.Height = 220;
            _liveModeCheck.Location = new Point(0, 118);
            _useNowButton.Location = new Point(190, 116);
            _dateFieldCaption.Location = new Point(0, 146);
            _timeFieldCaption.Location = new Point(166, 146);
            _datePicker.Location = new Point(0, 162);
            _timeEntry.Location = new Point(166, 162);
        }
        else
        {
            if (_sourceWrap is not null)
                _sourceWrap.Height = 190;
            _liveModeCheck.Location = new Point(0, 72);
            _useNowButton.Location = new Point(190, 70);
            _dateFieldCaption.Location = new Point(0, 104);
            _timeFieldCaption.Location = new Point(166, 104);
            _datePicker.Location = new Point(0, 120);
            _timeEntry.Location = new Point(166, 120);
        }
    }

    private void OnDirectionChanged(object? sender, EventArgs e)
    {
        if (!_ready || _suppressEvents)
            return;
        ApplyDirectionUi();
        RefreshDisplays();
        PersistSettings();
    }

    private void OnReverseSourceChanged(object? sender, EventArgs e)
    {
        if (!_ready || _suppressEvents)
            return;
        RefreshDisplays();
        PersistSettings();
    }

    private void OnLiveModeChanged(object? sender, EventArgs e)
    {
        if (!_ready || _suppressEvents)
            return;

        if (_liveModeCheck.Checked)
            EnterLiveMode();
        else
            ExitLiveMode(fromUserToggle: true);
    }

    private void EnterLiveMode()
    {
        _liveMode = true;
        _suppressEvents = true;
        try
        {
            _liveModeCheck.Checked = true;
            SyncPickersToNow();
            ApplyTimePickerFormat();
        }
        finally
        {
            _suppressEvents = false;
        }

        _useNowButton.Visible = false;
        _datePicker.Enabled = false;
        _timeEntry.Enabled = false;
        if (!_liveTimer.Enabled)
            _liveTimer.Start();

        RefreshDisplays();
    }

    private void ExitLiveMode(bool fromUserToggle)
    {
        _liveMode = false;
        _useNowButton.Visible = true;
        _datePicker.Enabled = true;
        _timeEntry.Enabled = true;
        ApplyTimePickerFormat();

        if (!fromUserToggle || _liveModeCheck.Checked)
        {
            _suppressEvents = true;
            try
            {
                _liveModeCheck.Checked = false;
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        RefreshDisplays();
    }

    private void OnFormatChanged(object? sender, EventArgs e)
    {
        if (!_ready || _suppressEvents)
            return;
        ApplyTimePickerFormat();
        RefreshDisplays();
        PersistSettings();
    }

    private void OnInputChanged(object? sender, EventArgs e)
    {
        if (!_ready || _suppressEvents)
            return;

        if (_liveMode)
        {
            ExitLiveMode(fromUserToggle: false);
            return;
        }

        RefreshDisplays();
    }

    private void OnTargetChanged(object? sender, EventArgs e)
    {
        if (!_ready || _suppressEvents)
            return;
        RefreshDisplays();
        PersistSettings();
    }

    private void ApplyTimePickerFormat()
    {
        // Live mode shows seconds; custom entry uses HH:mm / h:mm tt with 30-min presets
        _timeEntry.Configure(Use24Hour, includeSeconds: _liveMode);
    }

    private void ShowDetectedLocalTimezone()
    {
        var offset = _localTimezone.GetUtcOffset(DateTime.Now);
        _localTimezoneLabel.Text = $"{_localTimezone.DisplayName}  -  {FormatOffset(offset)}";
    }

    private void LoadTimezones()
    {
        _timezoneOptions = TimezoneOption.BuildFullList().ToList();

        _suppressEvents = true;
        try
        {
            _reverseSourceTimezone.BeginUpdate();
            try
            {
                _reverseSourceTimezone.Items.Clear();
                foreach (var opt in _timezoneOptions)
                    _reverseSourceTimezone.Items.Add(opt);
            }
            finally
            {
                _reverseSourceTimezone.EndUpdate();
            }
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void ApplySettingsAndDefaults()
    {
        _suppressEvents = true;
        try
        {
            _formatToggle.RightSelected = _settings.Use24Hour;
            _directionToggle.RightSelected = _settings.ConvertToLocal;
            _overlayCheck.Checked = _settings.OverlayVisible;

            _liveMode = true;
            _liveModeCheck.Checked = true;
            _datePicker.Enabled = false;
            _timeEntry.Enabled = false;
            _useNowButton.Visible = false;
            SyncPickersToNow();
            ApplyTimePickerFormat();

            if (string.IsNullOrWhiteSpace(_settings.ReverseSourceWindowsId) ||
                !SelectByWindowsId(_reverseSourceTimezone, _settings.ReverseSourceWindowsId!))
            {
                SelectByAbbreviation(_reverseSourceTimezone, "IST");
            }

            RebuildTargetRowsFromSettings();
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    /// <summary>
    /// Recreate target rows from saved Windows timezone IDs (or defaults).
    /// </summary>
    private void RebuildTargetRowsFromSettings()
    {
        foreach (var existing in _targetRows.ToList())
            existing.Root.Dispose();
        _targetRows.Clear();

        var saved = (_settings.TargetWindowsIds ?? Array.Empty<string?>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxTargetZones)
            .ToList();

        if (saved.Count == 0)
        {
            foreach (var abbr in DefaultTargetAbbreviations.Take(MaxTargetZones))
                AddTargetRow(selectWindowsId: null, selectAbbreviation: abbr, persist: false);
        }
        else
        {
            foreach (var id in saved)
                AddTargetRow(selectWindowsId: id, selectAbbreviation: null, persist: false);
        }

        if (_targetRows.Count == 0)
            AddTargetRow(selectWindowsId: null, selectAbbreviation: "UTC", persist: false);
    }

    /// <summary>
    /// After the form is shown, re-select saved zones so ComboBox handles are ready.
    /// </summary>
    private void RestoreSavedTargetSelections()
    {
        var saved = (_settings.TargetWindowsIds ?? Array.Empty<string?>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToList();

        if (saved.Count == 0)
            return;

        // If row count does not match, rebuild from settings
        if (_targetRows.Count != saved.Count)
        {
            _suppressEvents = true;
            try
            {
                RebuildTargetRowsFromSettings();
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        _suppressEvents = true;
        try
        {
            for (var i = 0; i < Math.Min(_targetRows.Count, saved.Count); i++)
                _targetRows[i].SelectWindowsId(saved[i]);

            if (!string.IsNullOrWhiteSpace(_settings.ReverseSourceWindowsId))
                SelectByWindowsId(_reverseSourceTimezone, _settings.ReverseSourceWindowsId!);
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void SyncPickersToNow()
    {
        var nowInInputZone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetInputTimezone());

        _suppressEvents = true;
        try
        {
            if (_datePicker.Value.Date != nowInInputZone.Date)
                _datePicker.Value = nowInInputZone.Date;

            var current = _timeEntry.TimeOfDay;
            if (current.Hours != nowInInputZone.Hour ||
                current.Minutes != nowInInputZone.Minute ||
                current.Seconds != nowInInputZone.Second)
            {
                _timeEntry.TimeOfDay = nowInInputZone.TimeOfDay;
            }
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void PersistSettings()
    {
        try
        {
            _settings.Use24Hour = Use24Hour;
            _settings.ConvertToLocal = ConvertToLocal;
            _settings.OverlayVisible = _overlayCheck.Checked || (_overlay is { Visible: true });

            if (_reverseSourceTimezone.SelectedItem is TimezoneOption reverseOpt)
                _settings.ReverseSourceWindowsId = reverseOpt.WindowsId;
            else if (_reverseSourceTimezone.SelectedIndex >= 0 &&
                     _reverseSourceTimezone.SelectedIndex < _reverseSourceTimezone.Items.Count &&
                     _reverseSourceTimezone.Items[_reverseSourceTimezone.SelectedIndex] is TimezoneOption rev)
            {
                _settings.ReverseSourceWindowsId = rev.WindowsId;
            }

            var ids = new List<string>();
            foreach (var row in _targetRows)
            {
                var id = row.SelectedOption?.WindowsId;
                if (!string.IsNullOrWhiteSpace(id))
                    ids.Add(id);
            }

            // Never wipe saved zones with an empty list due to a transient UI state
            if (ids.Count > 0)
                _settings.TargetWindowsIds = ids.ToArray();

            SaveOverlayPlacement();
            _settings.Save();
        }
        catch
        {
            // Preferences are best-effort
        }
    }

    private void OnOverlayCheckChanged(object? sender, EventArgs e)
    {
        if (!_ready || _suppressEvents)
            return;

        if (_overlayCheck.Checked)
            ShowOverlay();
        else
            HideOverlay();

        PersistSettings();
    }

    private void ShowOverlay()
    {
        if (_overlay is null || _overlay.IsDisposed)
        {
            _overlay = new OverlayForm();
            _overlay.CloseRequested += OnOverlayCloseRequested;
            _overlay.OpenMainRequested += OnOverlayOpenMain;
            _overlay.SetOpacity(_settings.OverlayOpacity);

            if (_settings.OverlayX >= 0 && _settings.OverlayY >= 0 &&
                IsOnScreen(_settings.OverlayX, _settings.OverlayY))
            {
                _overlay.Location = new Point(_settings.OverlayX, _settings.OverlayY);
            }
            else
            {
                // Default: top-right of primary screen
                var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1200, 800);
                _overlay.Location = new Point(wa.Right - _overlay.Width - 24, wa.Top + 24);
            }
        }

        _suppressEvents = true;
        try
        {
            _overlayCheck.Checked = true;
            if (_trayOverlayItem is not null)
                _trayOverlayItem.Checked = true;
        }
        finally
        {
            _suppressEvents = false;
        }

        if (!_overlay.Visible)
            _overlay.Show(this);

        _settings.OverlayVisible = true;
        RefreshDisplays();
    }

    private void HideOverlay()
    {
        SaveOverlayPlacement();

        if (_overlay is { IsDisposed: false })
            _overlay.Hide();

        _suppressEvents = true;
        try
        {
            _overlayCheck.Checked = false;
            if (_trayOverlayItem is not null)
                _trayOverlayItem.Checked = false;
        }
        finally
        {
            _suppressEvents = false;
        }

        _settings.OverlayVisible = false;
        _settings.Save();
    }

    private void OnOverlayCloseRequested(object? sender, EventArgs e) => HideOverlay();

    private void OnOverlayOpenMain(object? sender, EventArgs e) => RestoreFromTray();

    private void SaveOverlayPlacement()
    {
        if (_overlay is null || _overlay.IsDisposed)
            return;

        _settings.OverlayX = _overlay.Location.X;
        _settings.OverlayY = _overlay.Location.Y;
        _settings.OverlayOpacity = _overlay.Opacity;
    }

    private static bool IsOnScreen(int x, int y)
    {
        var pt = new Point(x + 20, y + 20);
        return Screen.AllScreens.Any(s => s.WorkingArea.Contains(pt));
    }

    private static bool SelectByWindowsId(ComboBox combo, string windowsId)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is TimezoneOption opt &&
                string.Equals(opt.WindowsId, windowsId, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = i;
                return true;
            }
        }

        return false;
    }

    private static void SelectByAbbreviation(ComboBox combo, string abbreviation)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is TimezoneOption opt &&
                string.Equals(opt.Abbreviation, abbreviation, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = i;
                return;
            }
        }

        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
    }

    private TimeZoneInfo GetInputTimezone()
    {
        if (ConvertToLocal && _reverseSourceTimezone.SelectedItem is TimezoneOption reverseOpt)
            return reverseOpt.GetTimeZoneInfo();
        return _localTimezone;
    }

    private string GetInputTimezoneLabel()
    {
        if (ConvertToLocal && _reverseSourceTimezone.SelectedItem is TimezoneOption reverseOpt)
            return reverseOpt.Abbreviation;
        return "Local";
    }

    private void RefreshDisplays()
    {
        if (_suppressEvents)
            return;

        try
        {
            var inputTz = GetInputTimezone();
            DateTime inputWall;
            DateTime utc;

            if (_liveMode)
            {
                var nowLocal = DateTime.Now;
                utc = nowLocal.ToUniversalTime();
                inputWall = TimeZoneInfo.ConvertTimeFromUtc(utc, inputTz);
            }
            else
            {
                inputWall = _datePicker.Value.Date + _timeEntry.TimeOfDay;
                var unspecified = DateTime.SpecifyKind(inputWall, DateTimeKind.Unspecified);
                utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, inputTz);
            }

            var primaryTime = TimeZoneInfo.ConvertTimeFromUtc(utc, _localTimezone);
            var localOffset = _localTimezone.GetUtcOffset(primaryTime);
            var inputLabel = GetInputTimezoneLabel();

            string primaryCaption;
            if (ConvertToLocal)
            {
                primaryCaption = _liveMode
                    ? $"Your local (live)  -  {primaryTime:ddd d MMM}  -  {FormatOffset(localOffset)}"
                    : $"{FormatDigitalTime(inputWall)} {inputLabel} -> local  -  {primaryTime:ddd d MMM}  -  {FormatOffset(localOffset)}";
            }
            else
            {
                primaryCaption = _liveMode
                    ? $"Live now  -  {primaryTime:ddd d MMM}  -  {FormatOffset(localOffset)}"
                    : $"Custom local  -  {primaryTime:ddd d MMM}  -  {FormatOffset(localOffset)}";
            }

            _primaryClock.TimeText = FormatDigitalTime(primaryTime);
            _primaryClock.CaptionText = primaryCaption;

            var overlayZones = new List<(string label, string time, string meta)>();

            foreach (var row in _targetRows)
            {
                if (row.SelectedOption is not TimezoneOption targetOpt)
                {
                    row.Clock.TimeText = "--:--";
                    row.Meta.Text = string.Empty;
                    continue;
                }

                var targetTz = targetOpt.GetTimeZoneInfo();
                var converted = TimeZoneInfo.ConvertTimeFromUtc(utc, targetTz);
                var offset = targetTz.GetUtcOffset(converted);
                var dayNote = DayDeltaNote(primaryTime.Date, converted.Date);
                var meta = string.IsNullOrEmpty(dayNote)
                    ? FormatOffset(offset)
                    : $"{FormatOffset(offset)}{dayNote}";

                row.Clock.TimeText = FormatDigitalTime(converted);
                row.Meta.Text = meta;

                overlayZones.Add((targetOpt.Abbreviation, FormatDigitalTime(converted), meta));
            }

            if (_overlay is { Visible: true, IsDisposed: false })
            {
                var overlayCaption = _liveMode ? "Your time - live" : "Your time - custom";
                _overlay.UpdateDisplay(FormatDigitalTime(primaryTime), overlayCaption, overlayZones);
            }

            var mode = Use24Hour ? "24-hour" : "12-hour";
            _statusLabel.Text = ConvertToLocal
                ? (_liveMode
                    ? $"To my zone - live - {GetInputTimezoneLabel()} -> local - {_targetRows.Count} zone(s) - {mode}"
                    : $"To my zone - entered in {GetInputTimezoneLabel()} - {_targetRows.Count} zone(s) - {mode}")
                : (_liveMode
                    ? $"From my zone - live - {_localTimezone.Id} - {_targetRows.Count} zone(s) - {mode}"
                    : $"From my zone - custom - {_localTimezone.Id} - {_targetRows.Count} zone(s) - {mode}");
            _statusLabel.ForeColor = UiTheme.TextMuted;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Could not convert: {ex.Message}";
            _statusLabel.ForeColor = UiTheme.Danger;
        }
    }

    private string FormatDigitalTime(DateTime time)
    {
        if (_liveMode)
            return Use24Hour ? time.ToString("HH:mm:ss") : time.ToString("hh:mm:ss tt");
        return Use24Hour ? time.ToString("HH:mm") : time.ToString("hh:mm tt");
    }

    private static string FormatOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var abs = offset.Duration();
        return abs.Minutes == 0
            ? $"UTC{sign}{abs.Hours}"
            : $"UTC{sign}{abs.Hours}:{abs.Minutes:D2}";
    }

    private static string DayDeltaNote(DateTime sourceDate, DateTime targetDate)
    {
        var days = (targetDate - sourceDate).Days;
        return days switch
        {
            0 => string.Empty,
            1 => " +1d",
            -1 => " -1d",
            > 1 => $" +{days}d",
            _ => $" {days}d"
        };
    }
}

