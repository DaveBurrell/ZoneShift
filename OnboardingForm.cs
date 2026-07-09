namespace TimezoneConverter;

/// <summary>
/// Short first-run tips shown once.
/// </summary>
internal sealed class OnboardingForm : Form
{
    public OnboardingForm()
    {
        Text = "Welcome to ZoneShift";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 300);
        Font = UiTheme.BodyFont;
        BackColor = UiTheme.AppBackground;
        ShowInTaskbar = false;

        var title = new Label
        {
            Text = "Quick tips",
            Font = UiTheme.TitleFont,
            ForeColor = UiTheme.Accent,
            Location = new Point(20, 16),
            AutoSize = true
        };

        var body = new Label
        {
            Location = new Point(20, 56),
            Size = new Size(420, 180),
            BackColor = UiTheme.CardBackground,
            ForeColor = UiTheme.TextPrimary,
            Padding = new Padding(12),
            Text =
                "1. Live mode\r\n" +
                "   Leave \"Use current time (live)\" on to watch clocks tick.\r\n" +
                "   Uncheck it to convert a specific date/time.\r\n\r\n" +
                "2. Add only the zones you need\r\n" +
                "   Use + Add timezone / x to keep 1-8 zones.\r\n" +
                "   Type in a dropdown to search; * marks favorites.\r\n\r\n" +
                "3. Desktop overlay\r\n" +
                "   Enable the always-on-top mini view for meetings.\r\n" +
                "   Drag the title bar; L locks, C is compact.\r\n\r\n" +
                "Tip: Close can minimize to the tray (see options)."
        };

        var ok = new Button
        {
            Text = "Got it",
            DialogResult = DialogResult.OK,
            Location = new Point(340, 250),
            Size = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.Accent,
            ForeColor = Color.White
        };
        ok.FlatAppearance.BorderSize = 0;
        AcceptButton = ok;

        Controls.Add(title);
        Controls.Add(body);
        Controls.Add(ok);
    }
}
