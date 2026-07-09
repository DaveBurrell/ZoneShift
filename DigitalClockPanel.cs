using System.ComponentModel;

namespace TimezoneConverter;

/// <summary>
/// Hero / module LED clock (wraps custom-painted <see cref="LedClockDisplay"/>).
/// </summary>
internal sealed class DigitalClockPanel : Panel
{
    private readonly LedClockDisplay _led;

    public DigitalClockPanel(bool large = false)
    {
        DoubleBuffered = true;
        BackColor = UiTheme.CardFace;
        BorderStyle = BorderStyle.None;
        Padding = Padding.Empty;

        _led = new LedClockDisplay(large)
        {
            Dock = DockStyle.Fill,
            ZoneText = large ? "LOCAL" : "",
            BlinkColons = large,
            GutterColor = UiTheme.CardFace
        };
        Controls.Add(_led);

        UiTheme.ThemeChanged += OnThemeChanged;
        Disposed += (_, _) => UiTheme.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged()
    {
        if (IsDisposed) return;
        BackColor = UiTheme.CardFace;
        _led.GutterColor = UiTheme.CardFace;
        Invalidate(true);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string TimeText
    {
        get => _led.TimeText;
        set => _led.TimeText = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string CaptionText
    {
        get => _led.CaptionText;
        set => _led.CaptionText = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string ZoneText
    {
        get => _led.ZoneText;
        set => _led.ZoneText = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool BlinkColons
    {
        get => _led.BlinkColons;
        set => _led.BlinkColons = value;
    }
}
