namespace TimezoneConverter;

/// <summary>
/// Simple white content card with a light border - no custom region clipping.
/// </summary>
internal sealed class RoundedCard : Panel
{
    public RoundedCard()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.CardBackground;
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(12);
        Margin = new Padding(0, 0, 0, 10);
    }
}
