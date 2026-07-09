using System.ComponentModel;

namespace TimezoneConverter;

/// <summary>
/// Simple digital clock readout - solid panel, no region clipping (avoids text being cut off).
/// </summary>
internal sealed class DigitalClockPanel : Panel
{
    private readonly Label _timeLabel;
    private readonly Label? _captionLabel;

    public DigitalClockPanel(bool large = false)
    {
        DoubleBuffered = true;
        BackColor = UiTheme.ClockBack;
        Padding = large ? new Padding(8, 6, 8, 6) : new Padding(6, 2, 6, 2);

        _timeLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = large ? UiTheme.ClockLargeFont : UiTheme.ClockRowFont,
            ForeColor = UiTheme.ClockFore,
            BackColor = UiTheme.ClockBack,
            Text = "--:--",
            AutoEllipsis = true
        };

        if (large)
        {
            _captionLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                TextAlign = ContentAlignment.TopCenter,
                Font = UiTheme.CaptionFont,
                ForeColor = UiTheme.TextMuted,
                BackColor = UiTheme.ClockBack,
                Text = "",
                AutoEllipsis = true
            };
            Controls.Add(_timeLabel);
            Controls.Add(_captionLabel);
        }
        else
        {
            Controls.Add(_timeLabel);
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string TimeText
    {
        get => _timeLabel.Text;
        set => _timeLabel.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string CaptionText
    {
        get => _captionLabel?.Text ?? string.Empty;
        set
        {
            if (_captionLabel is not null)
                _captionLabel.Text = value;
        }
    }
}
