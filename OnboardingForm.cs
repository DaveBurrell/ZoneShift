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
        ClientSize = new Size(480, 340);
        Font = UiTheme.BodyFont;
        BackColor = UiTheme.AppBackground;
        ShowInTaskbar = false;

        var title = new Label
        {
            Text = "Quick tips",
            Font = UiTheme.TitleFont,
            ForeColor = UiTheme.Accent,
            BackColor = UiTheme.AppBackground,
            Location = new Point(24, 18),
            AutoSize = true
        };

        var body = new Label
        {
            Location = new Point(24, 58),
            Size = new Size(432, 210),
            BackColor = UiTheme.CardBackground,
            ForeColor = UiTheme.TextPrimary,
            Padding = new Padding(16, 14, 16, 14),
            Text =
                "1. Live mode\r\n" +
                "   Leave \"Use current time (live)\" on to watch clocks tick.\r\n" +
                "   Uncheck it to convert a specific date/time.\r\n\r\n" +
                "2. Add only the zones you need\r\n" +
                "   Use + Add clock / x to keep 1-8 zones.\r\n" +
                "   Type in a dropdown to search; * marks favorites.\r\n\r\n" +
                "3. Desktop overlay\r\n" +
                "   Enable the always-on-top mini view for meetings.\r\n" +
                "   Drag the title bar; L locks, C is compact.\r\n\r\n" +
                "Tip: Close can minimize to the tray (toolbar checkbox).\r\n" +
                "Tip: View > Theme switches Studio, Classic, or Neon Pulse."
        };

        var ok = new Button
        {
            Text = "Got it",
            DialogResult = DialogResult.OK,
            Location = new Point(356, 284),
            Size = new Size(100, 34)
        };
        UiTheme.StylePrimaryButton(ok);
        AcceptButton = ok;

        Controls.Add(title);
        Controls.Add(body);
        Controls.Add(ok);
    }
}
