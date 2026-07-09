using TimezoneConverter.Services;

namespace TimezoneConverter;

public sealed class MainForm : Form
{
    private readonly TimeZoneInfo _localTimezone = TimeZoneInfo.Local;
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly System.Windows.Forms.Timer _liveTimer = new();

    private readonly Label _localTimezoneLabel = new();
    private readonly Label _sourceSectionTitle = new();
    private readonly Label _localZoneCaption = new();
    private readonly Label _inputZoneCaption = new();
    private readonly Label _dateFieldCaption = new();
    private readonly Label _timeFieldCaption = new();
    private readonly Panel _reverseZoneRow = new();
    private Panel _sourceWrap = null!;
    private readonly SearchableTimezoneBox _reverseSourceTimezone = new();
    private readonly DateTimePicker _datePicker = new();
    private readonly TimeEntryCombo _timeEntry = new();
    private readonly CheckBox _liveModeCheck = new();
    private readonly Button _useNowButton = new();
    private readonly Button _copyButton = new();
    private readonly SegmentedToggle _formatToggle = new("12-hour", "24-hour");
    private readonly SegmentedToggle _directionToggle = new("From my zone", "To my zone");
    private readonly CheckBox _overlayCheck = new();
    private readonly CheckBox _closeToTrayCheck = new();
    private readonly DigitalClockPanel _primaryClock = new(large: true);
    private readonly Label _statusLabel = new();
    private readonly Button _addTimezoneButton = new();
    private readonly List<TargetZoneRow> _targetRows = [];
    private FlowLayoutPanel _targetListHost = null!;
    private Label _targetCountLabel = null!;
    private string _lastCopyText = "";
    private string _lastCopyOneLine = "";
    private ConversionSnapshot? _lastSnapshot;

    private List<TimezoneOption> _timezoneOptions = [];
    private OverlayForm? _overlay;
    private ToolStripMenuItem _trayOverlayItem = null!;
    private ToolStripMenuItem _menuCheckUpdates = null!;
    private LinkLabel _footerUpdatesLink = null!;
    private LinkLabel _footerTipsLink = null!;
    private LinkLabel _footerAboutLink = null!;
    private LiveBadgeControl _liveBadge = null!;
    private MenuStrip _mainMenu = null!;
    private Label _brandTitle = null!;
    private Label _brandSubtitle = null!;
    private Label _wallSectionLabel = null!;
    private Panel _footerBar = null!;
    private FlowLayoutPanel _footerLinks = null!;
    private Panel _toolbarHost = null!;
    private Panel _clockHost = null!;
    private Panel _leftHost = null!;
    private Panel _wallHeader = null!;
    private Panel _wallFooter = null!;
    private Panel _toolbarWrap = null!;
    private Panel _wallWrap = null!;
    private StudioPanel _sourceCard = null!;
    private StudioPanel _wallCard = null!;
    private readonly ToolTip _tips = new() { ShowAlways = false, AutoPopDelay = 6000 };
    private readonly List<ToolStripMenuItem> _themeMenuItems = [];
    private bool _updateBusy;
    private bool _layoutToolbarReady;

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
        // Apply saved theme before any control paints
        UiTheme.SetTheme(_settings.Theme, raiseEvent: false);

        Text = "ZoneShift";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        // Absolute layout at 96 DPI logical pixels (pairs with DpiUnawareGdiScaled)
        AutoScaleMode = AutoScaleMode.None;
        Font = UiTheme.BodyFont;
        BackColor = UiTheme.AppBackground;
        DoubleBuffered = true;
        ShowInTaskbar = true;
        ClientSize = new Size(LayoutMetrics.ClientWidth, LayoutMetrics.ClientHeight);
        MinimumSize = new Size(LayoutMetrics.MinWidth, LayoutMetrics.MinHeight);

        ApplyAppIcon();
        BuildUi();
        ClientSize = new Size(LayoutMetrics.ClientWidth, LayoutMetrics.ClientHeight);
        SyncThemeMenuChecks();

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
            // Enforce readable size if saved bounds are missing/corrupt
            if (Width < LayoutMetrics.MinWidth - 40 || Height < LayoutMetrics.MinHeight - 40)
                ClientSize = new Size(LayoutMetrics.ClientWidth, LayoutMetrics.ClientHeight);
            else
                RestoreWindowBounds();

            RestoreSavedTargetSelections();
            RebuildTargetListUi();
            RefreshFavoriteVisuals();
            RefreshDisplays();
            PersistSettings();

            if (!_settings.HasSeenOnboarding)
                BeginInvoke(() => ShowOnboarding(force: false));
        };

        FormClosing += OnFormClosing;
        Resize += OnFormResize;
        LocationChanged += (_, _) =>
        {
            if (_ready && WindowState == FormWindowState.Normal)
                SaveWindowBounds();
        };
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
            if (_suppressEvents)
                return;
            if (_trayOverlayItem.Checked)
                ShowOverlay();
            else
                HideOverlay();
        };
        _trayMenu.Items.Add(_trayOverlayItem);
        _trayMenu.Items.Add("About ZoneShift...", null, (_, _) => ShowAbout());
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

        if (!_exitRequested && e.CloseReason == CloseReason.UserClosing && _settings.CloseToTray)
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

        // --- Footer: status + support links ---
        _footerBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = LayoutMetrics.FooterH,
            BackColor = UiTheme.FooterBack,
            Padding = new Padding(LayoutMetrics.OuterX, 0, LayoutMetrics.OuterX, 0)
        };

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = UiTheme.TextSecondary;
        _statusLabel.Font = UiTheme.CaptionFont;
        _statusLabel.BackColor = UiTheme.FooterBack;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.AutoEllipsis = true;
        _statusLabel.Padding = new Padding(0, 0, 8, 0);

        _footerLinks = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 250,
            BackColor = UiTheme.FooterBack,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, (LayoutMetrics.FooterH - 18) / 2, 0, 0),
            AutoSize = false
        };

        _footerTipsLink = MakeFooterLink("Tips");
        _footerTipsLink.LinkClicked += (_, _) => ShowOnboarding(force: true);
        _footerAboutLink = MakeFooterLink("About");
        _footerAboutLink.LinkClicked += (_, _) => ShowAbout();
        _footerUpdatesLink = MakeFooterLink("Check updates");
        _footerUpdatesLink.LinkClicked += async (_, _) => await CheckForUpdatesAsync();
        _footerLinks.Controls.Add(_footerTipsLink);
        _footerLinks.Controls.Add(MakeFooterSep());
        _footerLinks.Controls.Add(_footerAboutLink);
        _footerLinks.Controls.Add(MakeFooterSep());
        _footerLinks.Controls.Add(_footerUpdatesLink);

        _footerBar.Controls.Add(_statusLabel);
        _footerBar.Controls.Add(_footerLinks);
        Controls.Add(_footerBar);

        // --- Menu ---
        _mainMenu = new MenuStrip
        {
            Dock = DockStyle.Top,
            BackColor = UiTheme.HeaderBack,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.BodyFont,
            Renderer = new ThemeMenuRenderer(),
            Padding = new Padding(4, 2, 0, 2)
        };

        var viewMenu = new ToolStripMenuItem("&View") { ForeColor = UiTheme.TextPrimary };
        var themeMenu = new ToolStripMenuItem("&Theme");
        foreach (var palette in ThemePalette.All)
        {
            var id = palette.Id;
            var item = new ToolStripMenuItem(palette.DisplayName)
            {
                Tag = id,
                Checked = id == UiTheme.CurrentId,
                CheckOnClick = false
            };
            item.Click += (_, _) => SelectTheme(id);
            _themeMenuItems.Add(item);
            themeMenu.DropDownItems.Add(item);
        }
        // Short descriptions under the submenu (tooltips via text suffix on first open)
        themeMenu.DropDownItems[0].ToolTipText = "Newsroom amber LEDs on dark studio wall";
        themeMenu.DropDownItems[1].ToolTipText = "Original light UI with indigo accents";
        themeMenu.DropDownItems[2].ToolTipText = "Cyberpunk cyan clocks and magenta neon";
        viewMenu.DropDownItems.Add(themeMenu);
        _mainMenu.Items.Add(viewMenu);

        var editMenu = new ToolStripMenuItem("&Edit") { ForeColor = UiTheme.TextPrimary };
        editMenu.DropDownItems.Add("Copy multi-line", null, (_, _) => CopyResults(oneLine: false));
        editMenu.DropDownItems.Add("Copy one line (chat)", null, (_, _) => CopyResults(oneLine: true));
        _mainMenu.Items.Add(editMenu);

        var helpMenu = new ToolStripMenuItem("&Help") { ForeColor = UiTheme.TextPrimary };
        helpMenu.DropDownItems.Add("Tips...", null, (_, _) => ShowOnboarding(force: true));
        _menuCheckUpdates = new ToolStripMenuItem("Check for &updates...", null,
            async (_, _) => await CheckForUpdatesAsync());
        helpMenu.DropDownItems.Add(_menuCheckUpdates);
        helpMenu.DropDownItems.Add(new ToolStripSeparator());
        helpMenu.DropDownItems.Add("&About ZoneShift...", null, (_, _) => ShowAbout());
        _mainMenu.Items.Add(helpMenu);

        MainMenuStrip = _mainMenu;
        Controls.Add(_mainMenu);

        // --- Brand strip ---
        var header = new StudioHeader
        {
            Dock = DockStyle.Top,
            Height = LayoutMetrics.HeaderH
        };
        _brandTitle = new Label
        {
            Text = "ZONESHIFT",
            Font = new Font("Segoe UI Semibold", 15f),
            ForeColor = UiTheme.ClockFore,
            BackColor = UiTheme.HeaderBack,
            AutoSize = false,
            Location = new Point(LayoutMetrics.HeaderPadX, 10),
            Size = new Size(240, 24)
        };
        _brandSubtitle = new Label
        {
            Text = UiTheme.Tagline,
            Font = new Font("Segoe UI", 8f),
            ForeColor = UiTheme.TextSecondary,
            BackColor = UiTheme.HeaderBack,
            AutoSize = false,
            Location = new Point(LayoutMetrics.HeaderPadX + 2, 34),
            Size = new Size(440, 14)
        };
        _liveBadge = new LiveBadgeControl
        {
            Location = new Point(680, 16),
            Size = new Size(82, 24),
            IsLive = true
        };
        header.Controls.Add(_brandTitle);
        header.Controls.Add(_brandSubtitle);
        header.Controls.Add(_liveBadge);
        header.Resize += (_, _) =>
            _liveBadge.Left = Math.Max(420, header.ClientSize.Width - _liveBadge.Width - LayoutMetrics.HeaderPadX);
        Controls.Add(header);

        // --- Control toolbar ---
        _toolbarWrap = new Panel
        {
            Dock = DockStyle.Top,
            Height = LayoutMetrics.ToolbarOuterH,
            BackColor = UiTheme.AppBackground,
            Padding = new Padding(LayoutMetrics.OuterX, LayoutMetrics.OuterY, LayoutMetrics.OuterX, 0)
        };
        _toolbarHost = new StudioPanel
        {
            Dock = DockStyle.Fill
        };

        _directionToggle.SelectionChanged += OnDirectionChanged;
        _formatToggle.SelectionChanged += OnFormatChanged;

        var cardFace = UiTheme.CardFace;

        _overlayCheck.Text = "Overlay";
        _overlayCheck.Font = new Font("Segoe UI Semibold", 8.5f);
        _overlayCheck.ForeColor = UiTheme.TextPrimary;
        _overlayCheck.BackColor = cardFace;
        _overlayCheck.FlatStyle = FlatStyle.Flat;
        _overlayCheck.AutoSize = true;
        _overlayCheck.CheckedChanged += OnOverlayCheckChanged;

        _closeToTrayCheck.Text = "Close to tray";
        _closeToTrayCheck.Font = new Font("Segoe UI Semibold", 8.5f);
        _closeToTrayCheck.ForeColor = UiTheme.TextPrimary;
        _closeToTrayCheck.BackColor = cardFace;
        _closeToTrayCheck.FlatStyle = FlatStyle.Flat;
        _closeToTrayCheck.AutoSize = true;
        _closeToTrayCheck.CheckedChanged += (_, _) =>
        {
            if (_suppressEvents) return;
            _settings.CloseToTray = _closeToTrayCheck.Checked;
            PersistSettings();
        };

        _copyButton.Text = "Copy";
        _copyButton.Size = new Size(LayoutMetrics.CopyW, LayoutMetrics.ToolbarControlH);
        UiTheme.StylePrimaryButton(_copyButton);
        var copyMenu = new ContextMenuStrip();
        copyMenu.Items.Add("Copy multi-line", null, (_, _) => CopyResults(oneLine: false));
        copyMenu.Items.Add("Copy one line (chat)", null, (_, _) => CopyResults(oneLine: true));
        _copyButton.Click += (_, _) => CopyResults(oneLine: false);
        _copyButton.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
                copyMenu.Show(_copyButton, e.Location);
        };
        _copyButton.ContextMenuStrip = copyMenu;

        _tips.SetToolTip(_directionToggle, "Convert from your PC zone, or enter a time in another zone");
        _tips.SetToolTip(_formatToggle, "12-hour or 24-hour clock display");
        _tips.SetToolTip(_overlayCheck, "Always-on-top mini clock for meetings");
        _tips.SetToolTip(_closeToTrayCheck, "Close button hides to the system tray instead of quitting");
        _tips.SetToolTip(_copyButton, "Copy results (right-click for one-line chat format)");

        _toolbarHost.Controls.Add(_directionToggle);
        _toolbarHost.Controls.Add(_formatToggle);
        _toolbarHost.Controls.Add(_overlayCheck);
        _toolbarHost.Controls.Add(_closeToTrayCheck);
        _toolbarHost.Controls.Add(_copyButton);
        _layoutToolbarReady = true;
        _toolbarHost.Resize += (_, _) => LayoutToolbar();
        LayoutToolbar();
        _toolbarWrap.Controls.Add(_toolbarHost);
        Controls.Add(_toolbarWrap);

        // --- Master clock + input ---
        _sourceWrap = new Panel
        {
            Dock = DockStyle.Top,
            Height = LayoutMetrics.SourceHNormal,
            BackColor = UiTheme.AppBackground,
            Padding = new Padding(LayoutMetrics.OuterX, LayoutMetrics.OuterY, LayoutMetrics.OuterX, 0)
        };
        _sourceCard = new StudioPanel
        {
            Dock = DockStyle.Fill,
            // Docked hosts (left + clock) sit inside the rounded card
            Padding = new Padding(LayoutMetrics.CardPad)
        };
        var sourceCard = _sourceCard;

        _clockHost = new Panel
        {
            Dock = DockStyle.Right,
            Width = LayoutMetrics.ClockHostW,
            BackColor = cardFace,
            Padding = new Padding(LayoutMetrics.ClockHostPad)
        };
        _primaryClock.Dock = DockStyle.Fill;
        _primaryClock.ZoneText = "LOCAL";
        _primaryClock.BlinkColons = true;
        _clockHost.Controls.Add(_primaryClock);

        // Absolute layout — coordinates are relative to leftHost (already inset by sourceCard.Padding)
        _leftHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = cardFace,
            Padding = new Padding(0, 0, 10, 0) // gap before master LED
        };

        _sourceSectionTitle.Text = "Master clock";
        _sourceSectionTitle.Font = UiTheme.SectionFont;
        _sourceSectionTitle.ForeColor = UiTheme.Accent;
        _sourceSectionTitle.BackColor = cardFace;
        _sourceSectionTitle.AutoSize = false;

        StyleCaption(_localZoneCaption, "YOUR PC TIMEZONE");
        _localTimezoneLabel.Font = new Font("Segoe UI Semibold", 9f);
        _localTimezoneLabel.ForeColor = UiTheme.TextPrimary;
        _localTimezoneLabel.BackColor = cardFace;
        _localTimezoneLabel.AutoSize = false;
        _localTimezoneLabel.AutoEllipsis = true;

        _reverseZoneRow.BackColor = cardFace;
        StyleCaption(_inputZoneCaption, "ENTER TIME IN THIS TIMEZONE");
        _reverseSourceTimezone.Font = UiTheme.BodyFont;
        _reverseSourceTimezone.SelectedIndexChanged += OnReverseSourceChanged;
        _reverseZoneRow.Controls.Add(_inputZoneCaption);
        _reverseZoneRow.Controls.Add(_reverseSourceTimezone);

        _liveModeCheck.Text = "Use current time (live)";
        _liveModeCheck.Font = new Font("Segoe UI Semibold", 8.5f);
        _liveModeCheck.ForeColor = UiTheme.TextPrimary;
        _liveModeCheck.BackColor = cardFace;
        _liveModeCheck.FlatStyle = FlatStyle.Flat;
        _liveModeCheck.AutoSize = true;
        _liveModeCheck.Checked = true;
        _liveModeCheck.CheckedChanged += OnLiveModeChanged;

        _useNowButton.Text = "Reset to now";
        _useNowButton.Size = new Size(104, 26);
        UiTheme.StylePrimaryButton(_useNowButton);
        _useNowButton.Font = UiTheme.CaptionFont;
        _useNowButton.Click += (_, _) => EnterLiveMode();
        _useNowButton.Visible = false;

        StyleCaption(_dateFieldCaption, "DATE");
        _datePicker.Format = DateTimePickerFormat.Short;
        _datePicker.Size = new Size(148, 26);
        _datePicker.ValueChanged += OnInputChanged;

        StyleCaption(_timeFieldCaption, "TIME");
        _timeEntry.Size = new Size(148, 26);
        _timeEntry.Font = UiTheme.BodyFont;
        _timeEntry.Configure(use24Hour: false, includeSeconds: false);
        _timeEntry.TimeChanged += OnInputChanged;

        _leftHost.Controls.Add(_sourceSectionTitle);
        _leftHost.Controls.Add(_localZoneCaption);
        _leftHost.Controls.Add(_localTimezoneLabel);
        _leftHost.Controls.Add(_reverseZoneRow);
        _leftHost.Controls.Add(_liveModeCheck);
        _leftHost.Controls.Add(_useNowButton);
        _leftHost.Controls.Add(_dateFieldCaption);
        _leftHost.Controls.Add(_datePicker);
        _leftHost.Controls.Add(_timeFieldCaption);
        _leftHost.Controls.Add(_timeEntry);

        LayoutMasterFields(reverse: false);
        _leftHost.Resize += (_, _) => LayoutMasterFields(ConvertToLocal);

        sourceCard.Controls.Add(_leftHost);
        sourceCard.Controls.Add(_clockHost);
        _sourceWrap.Controls.Add(sourceCard);
        Controls.Add(_sourceWrap);

        // --- World clock wall ---
        _wallWrap = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.AppBackground,
            Padding = new Padding(
                LayoutMetrics.OuterX,
                LayoutMetrics.OuterY,
                LayoutMetrics.OuterX,
                LayoutMetrics.OuterBottom)
        };
        _wallCard = new StudioPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(LayoutMetrics.WallCardPad)
        };
        var wallCard = _wallCard;

        _wallHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = LayoutMetrics.WallHeaderH,
            BackColor = cardFace,
            Padding = new Padding(LayoutMetrics.CardPad, 10, LayoutMetrics.CardPad, 6)
        };
        _wallSectionLabel = new Label
        {
            Text = "WORLD CLOCK WALL",
            Font = UiTheme.SectionFont,
            ForeColor = UiTheme.ClockFore,
            BackColor = cardFace,
            AutoSize = false,
            Dock = DockStyle.Left,
            Width = 200,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _targetCountLabel = new Label
        {
            Text = "",
            Font = UiTheme.CaptionFont,
            ForeColor = UiTheme.TextSecondary,
            BackColor = cardFace,
            AutoSize = false,
            Dock = DockStyle.Right,
            Width = 100,
            TextAlign = ContentAlignment.MiddleRight
        };
        _wallHeader.Controls.Add(_targetCountLabel);
        _wallHeader.Controls.Add(_wallSectionLabel);

        _wallFooter = new Panel
        {
            Height = LayoutMetrics.WallFooterH,
            Dock = DockStyle.Bottom,
            BackColor = cardFace,
            Padding = new Padding(LayoutMetrics.CardPad, 8, LayoutMetrics.CardPad, 10)
        };
        var wallFooter = _wallFooter;
        _addTimezoneButton.Text = "+ Add clock";
        _addTimezoneButton.Dock = DockStyle.Left;
        _addTimezoneButton.Width = 128;
        UiTheme.StylePrimaryButton(_addTimezoneButton);
        _addTimezoneButton.Click += (_, _) => AddTargetRow(selectAbbreviation: null, persist: true);
        wallFooter.Controls.Add(_addTimezoneButton);

        _targetListHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = UiTheme.WallBack,
            Padding = new Padding(LayoutMetrics.WallListPad)
        };
        _targetListHost.Paint += (_, e) =>
        {
            UiPaint.PaintWallBackground(e.Graphics, _targetListHost.ClientRectangle);
        };

        wallCard.Controls.Add(_targetListHost);
        wallCard.Controls.Add(wallFooter);
        wallCard.Controls.Add(_wallHeader);
        _wallWrap.Controls.Add(wallCard);
        Controls.Add(_wallWrap);

        // Dock z-order: Fill under Tops; Bottom already docked
        Controls.SetChildIndex(_wallWrap, 0);
        Controls.SetChildIndex(_sourceWrap, 1);
        Controls.SetChildIndex(_toolbarWrap, 2);
        Controls.SetChildIndex(header, 3);
        Controls.SetChildIndex(_mainMenu, 4);
        Controls.SetChildIndex(_footerBar, 5);

        ResumeLayout(true);
    }

    /// <summary>
    /// Responsive toolbar: toggles left, options center-left, Copy pinned right.
    /// Never lets checkboxes collide with Copy.
    /// </summary>
    private void LayoutToolbar()
    {
        if (!_layoutToolbarReady || _toolbarHost is null || _toolbarHost.ClientSize.Width < 40)
            return;

        var padX = LayoutMetrics.ToolbarInnerPadX;
        var padY = LayoutMetrics.ToolbarInnerPadY;
        var h = LayoutMetrics.ToolbarControlH;
        var gap = LayoutMetrics.ToolbarGap;
        var w = _toolbarHost.ClientSize.Width;

        _directionToggle.SetBounds(padX, padY, LayoutMetrics.DirectionW, h);
        _formatToggle.SetBounds(padX + LayoutMetrics.DirectionW + gap, padY, LayoutMetrics.FormatW, h);

        _copyButton.Size = new Size(LayoutMetrics.CopyW, h);
        _copyButton.Location = new Point(Math.Max(padX, w - padX - LayoutMetrics.CopyW), padY);

        // Options sit between format toggle and Copy, with a safe gap
        var optsLeft = _formatToggle.Right + gap + 8;
        var optsRight = _copyButton.Left - gap;
        var optsSpace = optsRight - optsLeft;

        _overlayCheck.Visible = optsSpace >= 70;
        _closeToTrayCheck.Visible = optsSpace >= 200;

        if (_overlayCheck.Visible)
        {
            _overlayCheck.Location = new Point(optsLeft, padY + (h - _overlayCheck.PreferredSize.Height) / 2);
            if (_closeToTrayCheck.Visible)
            {
                var trayX = _overlayCheck.Right + 14;
                if (trayX + 100 > optsRight)
                    _closeToTrayCheck.Visible = false;
                else
                    _closeToTrayCheck.Location = new Point(
                        trayX,
                        padY + (h - _closeToTrayCheck.PreferredSize.Height) / 2);
            }
        }
    }

    private void SelectTheme(AppThemeId id)
    {
        if (UiTheme.CurrentId == id)
            return;

        UiTheme.SetTheme(id, raiseEvent: true);
        _settings.Theme = id.ToString();
        ApplyChromeTheme();
        PersistSettings();
        RefreshDisplays();
        _statusLabel.Text = $"Theme: {UiTheme.DisplayName}";
        _statusLabel.ForeColor = UiTheme.Success;
    }

    private void SyncThemeMenuChecks()
    {
        foreach (var item in _themeMenuItems)
        {
            item.Checked = item.Tag is AppThemeId id && id == UiTheme.CurrentId;
            item.ForeColor = UiTheme.TextPrimary;
        }
    }

    /// <summary>Repaint non-self-updating chrome after a theme switch.</summary>
    private void ApplyChromeTheme()
    {
        BackColor = UiTheme.AppBackground;
        var card = UiTheme.CardFace;

        if (_toolbarWrap is not null) _toolbarWrap.BackColor = UiTheme.AppBackground;
        if (_sourceWrap is not null) _sourceWrap.BackColor = UiTheme.AppBackground;
        if (_wallWrap is not null) _wallWrap.BackColor = UiTheme.AppBackground;

        _mainMenu.BackColor = UiTheme.HeaderBack;
        _mainMenu.ForeColor = UiTheme.TextPrimary;
        foreach (ToolStripItem item in _mainMenu.Items)
            item.ForeColor = UiTheme.TextPrimary;
        SyncThemeMenuChecks();

        _brandTitle.ForeColor = UiTheme.ClockFore;
        _brandTitle.BackColor = UiTheme.HeaderBack;
        _brandSubtitle.Text = UiTheme.Tagline;
        _brandSubtitle.ForeColor = UiTheme.TextSecondary;
        _brandSubtitle.BackColor = UiTheme.HeaderBack;

        _footerBar.BackColor = UiTheme.FooterBack;
        _footerLinks.BackColor = UiTheme.FooterBack;
        _statusLabel.BackColor = UiTheme.FooterBack;
        _statusLabel.ForeColor = UiTheme.TextSecondary;
        StyleFooterLink(_footerTipsLink);
        StyleFooterLink(_footerAboutLink);
        StyleFooterLink(_footerUpdatesLink);

        _overlayCheck.ForeColor = UiTheme.TextPrimary;
        _overlayCheck.BackColor = card;
        _closeToTrayCheck.ForeColor = UiTheme.TextPrimary;
        _closeToTrayCheck.BackColor = card;
        UiTheme.StylePrimaryButton(_copyButton);
        UiTheme.StylePrimaryButton(_useNowButton);
        _useNowButton.Font = UiTheme.CaptionFont;
        UiTheme.StylePrimaryButton(_addTimezoneButton);

        if (_clockHost is not null) _clockHost.BackColor = card;
        if (_leftHost is not null) _leftHost.BackColor = card;
        if (_wallHeader is not null) _wallHeader.BackColor = card;
        if (_wallFooter is not null) _wallFooter.BackColor = card;
        if (_targetListHost is not null) _targetListHost.BackColor = UiTheme.WallBack;

        _sourceSectionTitle.ForeColor = UiTheme.Accent;
        _sourceSectionTitle.BackColor = card;
        StyleCaption(_localZoneCaption, _localZoneCaption.Text);
        _localTimezoneLabel.ForeColor = UiTheme.TextPrimary;
        _localTimezoneLabel.BackColor = card;
        _reverseZoneRow.BackColor = card;
        StyleCaption(_inputZoneCaption, _inputZoneCaption.Text);
        _liveModeCheck.ForeColor = UiTheme.TextPrimary;
        _liveModeCheck.BackColor = card;
        StyleCaption(_dateFieldCaption, _dateFieldCaption.Text);
        StyleCaption(_timeFieldCaption, _timeFieldCaption.Text);

        _wallSectionLabel.ForeColor = UiTheme.ClockFore;
        _wallSectionLabel.BackColor = card;
        _targetCountLabel.ForeColor = UiTheme.TextSecondary;
        _targetCountLabel.BackColor = card;

        UiTheme.StyleInput(_reverseSourceTimezone);
        UiTheme.StyleInput(_timeEntry);

        foreach (var row in _targetRows)
            row.ApplyTheme();
        RefreshFavoriteVisuals();
        LayoutToolbar();
        LayoutMasterFields(ConvertToLocal);

        // Refresh footer separators
        foreach (Control c in _footerLinks.Controls)
        {
            if (c is Label { Text: "|" } sep)
            {
                sep.ForeColor = UiTheme.TextMuted;
                sep.BackColor = UiTheme.FooterBack;
            }
        }

        Invalidate(true);
    }

    private static LinkLabel MakeFooterLink(string text)
    {
        var link = new LinkLabel
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(6, 0, 6, 0),
            BackColor = UiTheme.FooterBack,
            Font = new Font("Segoe UI Semibold", 8.25f)
        };
        StyleFooterLink(link);
        return link;
    }

    private static Label MakeFooterSep() =>
        new()
        {
            Text = "|",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 0),
            ForeColor = UiTheme.TextMuted,
            BackColor = UiTheme.FooterBack,
            Font = UiTheme.CaptionFont
        };

    private static void StyleCaption(Label label, string text)
    {
        label.Text = text;
        label.Font = UiTheme.CaptionFont;
        label.ForeColor = UiTheme.TextSecondary;
        label.BackColor = UiTheme.CardFace;
        label.AutoSize = false;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private static void StyleFooterLink(LinkLabel link)
    {
        link.LinkColor = UiTheme.Accent;
        link.ActiveLinkColor = UiTheme.AccentHover;
        link.VisitedLinkColor = UiTheme.Accent;
        link.DisabledLinkColor = UiTheme.TextMuted;
        link.BackColor = UiTheme.FooterBack;
    }

    /// <summary>MenuStrip renderer that tracks the active theme palette.</summary>
    private sealed class ThemeMenuRenderer : ToolStripProfessionalRenderer
    {
        public ThemeMenuRenderer() : base(new ThemeColorTable()) { }

        private sealed class ThemeColorTable : ProfessionalColorTable
        {
            public override Color MenuStripGradientBegin => UiTheme.HeaderBack;
            public override Color MenuStripGradientEnd => UiTheme.HeaderBack;
            public override Color MenuItemSelected => UiTheme.MenuHover;
            public override Color MenuItemSelectedGradientBegin => UiTheme.MenuHover;
            public override Color MenuItemSelectedGradientEnd => UiTheme.MenuHover;
            public override Color MenuItemBorder => UiTheme.AccentSoftBorder;
            public override Color MenuBorder => UiTheme.CardBorder;
            public override Color ToolStripDropDownBackground => UiTheme.CardBackground;
            public override Color ImageMarginGradientBegin => UiTheme.CardBackground;
            public override Color ImageMarginGradientMiddle => UiTheme.CardBackground;
            public override Color ImageMarginGradientEnd => UiTheme.CardBackground;
            public override Color MenuItemPressedGradientBegin => UiTheme.SegmentActive;
            public override Color MenuItemPressedGradientEnd => UiTheme.SegmentActive;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.Selected && e.Item.Pressed)
                e.TextColor = UiTheme.TextOnAccent;
            else if (e.Item.Selected)
                e.TextColor = UiTheme.Accent;
            else if (e.Item is ToolStripMenuItem { Checked: true })
                e.TextColor = UiTheme.Accent;
            else
                e.TextColor = UiTheme.TextPrimary;
            base.OnRenderItemText(e);
        }
    }

    private void RebuildTargetListUi()
    {
        if (_targetListHost is null)
            return;

        _targetListHost.SuspendLayout();
        _targetListHost.Controls.Clear();

        for (var i = 0; i < _targetRows.Count; i++)
        {
            _targetRows[i].SetIndex(i + 1);
            _targetRows[i].RemoveButton.Enabled = _targetRows.Count > MinTargetZones;
            _targetRows[i].Root.Width = TargetZoneRow.TileWidth;
            _targetRows[i].Root.Height = TargetZoneRow.TileHeight;
            _targetRows[i].Root.Margin = new Padding(LayoutMetrics.TileMargin);
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
        var favorites = (_settings.FavoriteWindowsIds ?? Array.Empty<string?>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToArray();
        row = new TargetZoneRow(
            _timezoneOptions,
            favorites,
            OnTargetChanged,
            (_, _) =>
            {
                if (row is not null)
                    RemoveTargetRow(row);
            },
            (_, _) =>
            {
                if (row?.SelectedOption is TimezoneOption opt)
                    ToggleFavorite(opt.WindowsId);
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

            if (row.SelectedOption is TimezoneOption selected)
                row.RefreshFavoriteVisual(_settings.IsFavorite(selected.WindowsId));
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
            ForeColor = UiTheme.TextSecondary,
            BackColor = UiTheme.CardFace,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };

    private void SetUpdateUiBusy(bool busy)
    {
        _updateBusy = busy;
        if (_menuCheckUpdates is not null)
            _menuCheckUpdates.Enabled = !busy;
        if (_footerUpdatesLink is not null)
            _footerUpdatesLink.Enabled = !busy;
    }

    private void UpdateLiveBadge()
    {
        if (_liveBadge is not null)
            _liveBadge.IsLive = _liveMode;
        _primaryClock.BlinkColons = _liveMode;
    }

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
        _sourceSectionTitle.Text = reverse ? "Enter time in another timezone" : "Master clock";
        LayoutMasterFields(reverse);
    }

    /// <summary>
    /// Positions master-section fields with consistent vertical rhythm.
    /// Absolute coords start at LayoutMetrics.ContentX/Y (not flush to the card edge —
    /// the host is already inset by StudioPanel.Padding).
    /// </summary>
    private void LayoutMasterFields(bool reverse)
    {
        if (_leftHost is null || _sourceWrap is null)
            return;

        const int x = LayoutMetrics.ContentX;
        var y = LayoutMetrics.ContentY;
        var avail = _leftHost.ClientSize.Width - x - LayoutMetrics.ContentRightGutter;
        var fieldW = Math.Clamp(avail > 40 ? avail : 320, 280, 440);
        var colW = Math.Min(156, (fieldW - LayoutMetrics.FieldGap) / 2);
        var col2 = x + colW + LayoutMetrics.FieldGap;

        _sourceSectionTitle.SetBounds(x, y, fieldW, 20);
        y += 24;

        _localZoneCaption.SetBounds(x, y, fieldW, LayoutMetrics.LineCaption);
        y += LayoutMetrics.LineCaption + 3;
        _localTimezoneLabel.SetBounds(x, y, fieldW, LayoutMetrics.LineBody);
        y += LayoutMetrics.LineBody + LayoutMetrics.BlockGap;

        _reverseZoneRow.Visible = reverse;
        if (reverse)
        {
            _reverseZoneRow.SetBounds(x, y, fieldW, 52);
            _inputZoneCaption.SetBounds(0, 0, fieldW, LayoutMetrics.LineCaption);
            _reverseSourceTimezone.SetBounds(0, 18, fieldW, LayoutMetrics.LineField);
            y += 52 + LayoutMetrics.BlockGap;
        }

        _liveModeCheck.Location = new Point(x, y + 4);
        // Keep Reset aligned to second column when space allows
        var resetX = Math.Max(x + 210, col2);
        if (resetX + _useNowButton.Width > x + fieldW)
            resetX = x + fieldW - _useNowButton.Width;
        _useNowButton.Location = new Point(Math.Max(x, resetX), y);
        y += 34;

        _dateFieldCaption.SetBounds(x, y, colW, LayoutMetrics.LineCaption);
        _timeFieldCaption.SetBounds(col2, y, colW, LayoutMetrics.LineCaption);
        y += LayoutMetrics.LineCaption + 5;
        _datePicker.SetBounds(x, y, colW, LayoutMetrics.LineField);
        _timeEntry.SetBounds(col2, y, colW, LayoutMetrics.LineField);
        y += LayoutMetrics.LineField + 10;

        var contentH = y + LayoutMetrics.ContentY;
        var wrapH = contentH + LayoutMetrics.CardPad * 2 + LayoutMetrics.OuterY + 6;
        _sourceWrap.Height = Math.Max(
            reverse ? LayoutMetrics.SourceHReverse : LayoutMetrics.SourceHNormal,
            wrapH);
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
        UpdateLiveBadge();

        RefreshDisplays();
    }

    private void ExitLiveMode(bool fromUserToggle)
    {
        _liveMode = false;
        _useNowButton.Visible = true;
        _datePicker.Enabled = true;
        _timeEntry.Enabled = true;
        ApplyTimePickerFormat();
        UpdateLiveBadge();

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
        _localTimezoneLabel.Text = $"{_localTimezone.DisplayName}  -  {TimeConversionService.FormatOffset(offset)}";
    }

    private void LoadTimezones()
    {
        _timezoneOptions = TimezoneOption.BuildFullList().ToList();
        _suppressEvents = true;
        try
        {
            _reverseSourceTimezone.SetOptions(
                _timezoneOptions,
                (_settings.FavoriteWindowsIds ?? Array.Empty<string?>()).Where(id => id is not null).Select(id => id!));
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void ToggleFavorite(string windowsId)
    {
        _settings.ToggleFavorite(windowsId);
        RefreshFavoriteVisuals();
        PersistSettings();
    }

    private void RefreshFavoriteVisuals()
    {
        var favs = (_settings.FavoriteWindowsIds ?? Array.Empty<string?>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToArray();
        foreach (var row in _targetRows)
            row.ApplyFavorites(favs);
        _reverseSourceTimezone.SetFavorites(favs);
    }

    private void CopyResults(bool oneLine)
    {
        if (string.IsNullOrWhiteSpace(_lastCopyText) && string.IsNullOrWhiteSpace(_lastCopyOneLine))
            RefreshDisplays();

        var text = oneLine ? _lastCopyOneLine : _lastCopyText;
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(this, "Nothing to copy yet.", "ZoneShift", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Clipboard.SetText(text);
            _statusLabel.Text = oneLine ? "One-line results copied." : "Multi-line results copied.";
            _statusLabel.ForeColor = UiTheme.Success;
            AppLog.Info($"Copied conversion results ({(oneLine ? "one-line" : "multi-line")}).");
        }
        catch (Exception ex)
        {
            AppLog.Error("Clipboard copy failed", ex);
            MessageBox.Show(this, "Could not access the clipboard.", "ZoneShift", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowOnboarding(bool force)
    {
        if (!force && _settings.HasSeenOnboarding)
            return;

        using var dlg = new OnboardingForm();
        dlg.ShowDialog(this);
        _settings.HasSeenOnboarding = true;
        PersistSettings();
    }

    private async Task CheckForUpdatesAsync()
    {
        if (_updateBusy)
            return;

        SetUpdateUiBusy(true);
        _statusLabel.Text = "Checking for updates...";
        _statusLabel.ForeColor = UiTheme.TextSecondary;
        try
        {
            var version = typeof(MainForm).Assembly.GetName().Version?.ToString(3) ?? "1.5.0";
            var result = await UpdateChecker.CheckAsync(version);
            if (!result.UpdateAvailable)
            {
                MessageBox.Show(this, result.Message, "ZoneShift", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _statusLabel.Text = result.Message;
                _statusLabel.ForeColor = UiTheme.TextSecondary;
                return;
            }

            if (string.IsNullOrWhiteSpace(result.SetupDownloadUrl))
            {
                var openOnly = MessageBox.Show(
                    this,
                    $"{result.Message}\n\nNo installer asset found for {UpdateChecker.CurrentArchitectureLabel}.\nOpen the release page?",
                    "ZoneShift update",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if (openOnly == DialogResult.Yes && !string.IsNullOrWhiteSpace(result.ReleaseUrl))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = result.ReleaseUrl,
                        UseShellExecute = true
                    });
                }
                return;
            }

            var answer = MessageBox.Show(
                this,
                $"{result.Message}\n\nDownload and install {result.SetupFileName} now?\n" +
                "ZoneShift will close, update silently, then reopen.",
                "ZoneShift update",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
            {
                if (!string.IsNullOrWhiteSpace(result.ReleaseUrl))
                {
                    var open = MessageBox.Show(this, "Open the release page instead?", "ZoneShift",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (open == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = result.ReleaseUrl,
                            UseShellExecute = true
                        });
                    }
                }
                return;
            }

            var progress = new Progress<string>(msg =>
            {
                _statusLabel.Text = msg;
                _statusLabel.ForeColor = UiTheme.TextSecondary;
            });

            await UpdateChecker.InstallUpdateAsync(result.SetupDownloadUrl!, result.SetupFileName, progress);
            // Exit so the installer can replace files; silent [Run] relaunches ZoneShift.
            _exitRequested = true;
            PersistSettings();
            try { SingleInstance.Release(); } catch { /* ignore */ }
            Application.Exit();
        }
        catch (Exception ex)
        {
            AppLog.Error("Update check/install UI failed", ex);
            _statusLabel.Text = "Update failed - see logs.";
            _statusLabel.ForeColor = UiTheme.Danger;
            MessageBox.Show(this, $"Update failed:\n{ex.Message}", "ZoneShift", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            SetUpdateUiBusy(false);
        }
    }

    private void RestoreWindowBounds()
    {
        // Ignore tiny bounds saved while PerMonitorV2 crushed the layout (~480x560)
        if (_settings.WindowWidth >= LayoutMetrics.MinWidth &&
            _settings.WindowHeight >= LayoutMetrics.MinHeight &&
            _settings.WindowX >= 0 && _settings.WindowY >= 0 &&
            IsOnScreen(_settings.WindowX, _settings.WindowY))
        {
            StartPosition = FormStartPosition.Manual;
            Bounds = new Rectangle(_settings.WindowX, _settings.WindowY, _settings.WindowWidth, _settings.WindowHeight);
        }
        else
        {
            ClientSize = new Size(LayoutMetrics.ClientWidth, LayoutMetrics.ClientHeight);
        }
    }

    private void SaveWindowBounds()
    {
        if (WindowState != FormWindowState.Normal)
            return;
        _settings.WindowX = Left;
        _settings.WindowY = Top;
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
    }

    private void ApplySettingsAndDefaults()
    {
        _suppressEvents = true;
        try
        {
            _formatToggle.RightSelected = _settings.Use24Hour;
            _directionToggle.RightSelected = _settings.ConvertToLocal;
            _overlayCheck.Checked = _settings.OverlayVisible;
            _closeToTrayCheck.Checked = _settings.CloseToTray;

            _liveMode = _settings.LiveMode;
            _liveModeCheck.Checked = _liveMode;
            _datePicker.Enabled = !_liveMode;
            _timeEntry.Enabled = !_liveMode;
            _useNowButton.Visible = !_liveMode;
            if (_liveMode)
                SyncPickersToNow();
            ApplyTimePickerFormat();
            UpdateLiveBadge();

            if (string.IsNullOrWhiteSpace(_settings.ReverseSourceWindowsId) ||
                !_reverseSourceTimezone.SelectWindowsId(_settings.ReverseSourceWindowsId!))
            {
                _reverseSourceTimezone.SelectAbbreviation("IST");
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
                _reverseSourceTimezone.SelectWindowsId(_settings.ReverseSourceWindowsId!);
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
            _settings.LiveMode = _liveMode;
            _settings.CloseToTray = _closeToTrayCheck.Checked;
            _settings.OverlayVisible = _overlayCheck.Checked || (_overlay is { Visible: true });

            if (_reverseSourceTimezone.SelectedOption is TimezoneOption reverseOpt)
                _settings.ReverseSourceWindowsId = reverseOpt.WindowsId;

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

            SaveWindowBounds();
            SaveOverlayPlacement();
            _settings.Save();
        }
        catch (Exception ex)
        {
            AppLog.Error("PersistSettings failed", ex);
            if (_statusLabel is not null)
            {
                _statusLabel.Text = "Could not save preferences - see logs (About / diagnostics).";
                _statusLabel.ForeColor = UiTheme.Danger;
            }
        }
    }

    private void ShowAbout()
    {
        using var dlg = new AboutForm();
        dlg.ShowDialog(this);
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
            _overlay.SettingsChanged += (_, _) =>
            {
                if (_overlay is null) return;
                _settings.OverlayOpacity = _overlay.OverlayOpacity;
                _settings.OverlayLocked = _overlay.IsLocked;
                _settings.OverlayCompact = _overlay.IsCompact;
                PersistSettings();
            };
            _overlay.OverlayOpacity = _settings.OverlayOpacity;
            _overlay.IsLocked = _settings.OverlayLocked;
            _overlay.IsCompact = _settings.OverlayCompact;

            if (_settings.OverlayX >= 0 && _settings.OverlayY >= 0 &&
                IsOnScreen(_settings.OverlayX, _settings.OverlayY))
            {
                _overlay.Location = new Point(_settings.OverlayX, _settings.OverlayY);
            }
            else
            {
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
        _settings.OverlayOpacity = _overlay.OverlayOpacity;
        _settings.OverlayLocked = _overlay.IsLocked;
        _settings.OverlayCompact = _overlay.IsCompact;
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
        if (ConvertToLocal && _reverseSourceTimezone.SelectedOption is TimezoneOption reverseOpt)
            return reverseOpt.GetTimeZoneInfo();
        return _localTimezone;
    }

    private string GetInputTimezoneLabel()
    {
        if (ConvertToLocal && _reverseSourceTimezone.SelectedOption is TimezoneOption reverseOpt)
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
            var targets = new List<(string Abbreviation, string WindowsId, TimeZoneInfo Zone)>();
            var rowMap = new List<TargetZoneRow?>();

            foreach (var row in _targetRows)
            {
                if (row.SelectedOption is not TimezoneOption targetOpt)
                {
                    row.Clock.TimeText = "--:--";
                    row.Clock.ZoneText = "---";
                    row.Clock.CaptionText = "";
                    row.Meta.Text = string.Empty;
                    rowMap.Add(null);
                    continue;
                }

                targets.Add((targetOpt.Abbreviation, targetOpt.WindowsId, targetOpt.GetTimeZoneInfo()));
                rowMap.Add(row);
            }

            ConversionSnapshot snapshot;
            if (_liveMode)
            {
                snapshot = TimeConversionService.ConvertLiveNow(inputTz, _localTimezone, targets);
            }
            else
            {
                var inputWall = _datePicker.Value.Date + _timeEntry.TimeOfDay;
                snapshot = TimeConversionService.Convert(inputWall, inputTz, _localTimezone, targets);
            }

            var primaryTime = snapshot.PrimaryLocalTime;
            var localOffset = snapshot.PrimaryUtcOffset;

            _primaryClock.ZoneText = "LOCAL";
            _primaryClock.TimeText = FormatDigitalTime(primaryTime);
            _primaryClock.CaptionText = _liveMode
                ? $"{primaryTime:ddd d MMM}  {TimeConversionService.FormatOffset(localOffset)}  LIVE"
                : $"{primaryTime:ddd d MMM}  {TimeConversionService.FormatOffset(localOffset)}";

            var overlayZones = new List<(string label, string time, string meta)>();
            var resultIndex = 0;
            for (var i = 0; i < rowMap.Count; i++)
            {
                var row = rowMap[i];
                if (row is null)
                    continue;

                var r = snapshot.Targets[resultIndex++];
                var dayNote = TimeConversionService.FormatDayDelta(r.DayDeltaFromPrimary);
                var meta = string.IsNullOrEmpty(dayNote)
                    ? TimeConversionService.FormatOffset(r.UtcOffset)
                    : $"{TimeConversionService.FormatOffset(r.UtcOffset)}{dayNote}";

                row.Clock.TimeText = FormatDigitalTime(r.LocalWallTime);
                row.Clock.ZoneText = r.Abbreviation;
                row.Clock.CaptionText = meta;
                row.Meta.Text = meta;
                overlayZones.Add((r.Abbreviation, FormatDigitalTime(r.LocalWallTime), meta));
            }

            if (_overlay is { Visible: true, IsDisposed: false })
            {
                var overlayCaption = _liveMode ? "Your time - live" : "Your time - custom";
                _overlay.UpdateDisplay(FormatDigitalTime(primaryTime), overlayCaption, overlayZones);
            }

            _lastSnapshot = snapshot;
            _lastCopyText = TimeConversionService.FormatCopyMultiline(snapshot, Use24Hour, _liveMode);
            _lastCopyOneLine = TimeConversionService.FormatCopyOneLine(snapshot, Use24Hour, _liveMode);

            var mode = Use24Hour ? "24-hour" : "12-hour";
            if (!string.IsNullOrWhiteSpace(snapshot.Warning))
            {
                _statusLabel.Text = snapshot.Warning;
                _statusLabel.ForeColor = UiTheme.Warning;
            }
            else
            {
                _statusLabel.Text = ConvertToLocal
                    ? (_liveMode
                        ? $"To my zone - live - {GetInputTimezoneLabel()} -> local - {_targetRows.Count} zone(s) - {mode}"
                        : $"To my zone - entered in {GetInputTimezoneLabel()} - {_targetRows.Count} zone(s) - {mode}")
                    : (_liveMode
                        ? $"From my zone - live - {_localTimezone.Id} - {_targetRows.Count} zone(s) - {mode}"
                        : $"From my zone - custom - {_localTimezone.Id} - {_targetRows.Count} zone(s) - {mode}");
                _statusLabel.ForeColor = UiTheme.TextSecondary;
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("RefreshDisplays failed", ex);
            _statusLabel.Text = $"Could not convert: {ex.Message}";
            _statusLabel.ForeColor = UiTheme.Danger;
        }
    }

    private string FormatDigitalTime(DateTime time) =>
        TimeConversionService.FormatDigital(time, Use24Hour, includeSeconds: _liveMode);
}


