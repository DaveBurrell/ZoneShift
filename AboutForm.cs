using System.Diagnostics;
using System.Reflection;
using TimezoneConverter.Services;

namespace TimezoneConverter;

internal sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "About ZoneShift";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(440, 320);
        Font = UiTheme.BodyFont;
        BackColor = UiTheme.AppBackground;
        ShowInTaskbar = false;

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "1.2.0";

        var title = new Label
        {
            Text = "ZoneShift",
            Font = UiTheme.TitleFont,
            ForeColor = UiTheme.Accent,
            Location = new Point(20, 16),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        var body = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = UiTheme.CardBackground,
            Location = new Point(20, 56),
            Size = new Size(400, 180),
            Text = AppLog.BuildDiagnosticsSummary(AppSettings.SettingsPath, version),
            ScrollBars = ScrollBars.Vertical
        };

        var openSettings = new Button
        {
            Text = "Open settings folder",
            Location = new Point(20, 250),
            Size = new Size(150, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.AccentSoft,
            ForeColor = UiTheme.Accent
        };
        openSettings.FlatAppearance.BorderSize = 0;
        openSettings.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = AppSettings.SettingsDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AppLog.Error("Open settings folder failed", ex);
                MessageBox.Show(this, ex.Message, "ZoneShift", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var openLogs = new Button
        {
            Text = "Open logs folder",
            Location = new Point(180, 250),
            Size = new Size(130, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.AccentSoft,
            ForeColor = UiTheme.Accent
        };
        openLogs.FlatAppearance.BorderSize = 0;
        openLogs.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = AppLog.LogDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AppLog.Error("Open logs folder failed", ex);
                MessageBox.Show(this, ex.Message, "ZoneShift", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var close = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            Location = new Point(330, 250),
            Size = new Size(90, 30)
        };
        AcceptButton = close;

        Controls.Add(title);
        Controls.Add(body);
        Controls.Add(openSettings);
        Controls.Add(openLogs);
        Controls.Add(close);
    }
}
