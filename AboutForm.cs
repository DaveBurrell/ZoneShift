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
        ClientSize = new Size(460, 340);
        Font = UiTheme.BodyFont;
        BackColor = UiTheme.AppBackground;
        ShowInTaskbar = false;
        Padding = new Padding(20);

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "1.5.0";

        var title = new Label
        {
            Text = "ZoneShift",
            Font = UiTheme.TitleFont,
            ForeColor = UiTheme.Accent,
            Location = new Point(20, 18),
            AutoSize = true,
            BackColor = UiTheme.AppBackground
        };

        var body = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = UiTheme.InputBack,
            ForeColor = UiTheme.TextPrimary,
            Location = new Point(20, 58),
            Size = new Size(420, 200),
            Text = AppLog.BuildDiagnosticsSummary(AppSettings.SettingsPath, version),
            ScrollBars = ScrollBars.Vertical,
            Padding = new Padding(6)
        };

        var openSettings = new Button
        {
            Text = "Open settings folder",
            Location = new Point(20, 276),
            Size = new Size(156, 32)
        };
        UiTheme.StyleSecondaryButton(openSettings);
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
            Location = new Point(188, 276),
            Size = new Size(140, 32)
        };
        UiTheme.StyleSecondaryButton(openLogs);
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
            Location = new Point(344, 276),
            Size = new Size(96, 32)
        };
        UiTheme.StylePrimaryButton(close);
        AcceptButton = close;

        Controls.Add(title);
        Controls.Add(body);
        Controls.Add(openSettings);
        Controls.Add(openLogs);
        Controls.Add(close);
    }
}
