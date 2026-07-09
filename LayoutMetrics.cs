namespace TimezoneConverter;

/// <summary>
/// Shared spacing for the main window. Absolute-positioned controls ignore
/// <see cref="Control.Padding"/>, so insets are applied in coordinates explicitly.
/// </summary>
internal static class LayoutMetrics
{
    // Window
    public const int ClientWidth = 820;
    public const int ClientHeight = 880;
    public const int MinWidth = 780;
    public const int MinHeight = 800;

    // Outer gutters (app background between chrome and cards)
    public const int OuterX = 18;
    public const int OuterY = 12;
    public const int OuterBottom = 14;

    // Inside painted cards (docked hosts sit this far from rounded edge)
    public const int CardPad = 16;

    // Content inset inside master-clock left column
    public const int ContentX = 6;
    public const int ContentY = 4;
    public const int ContentRightGutter = 14;

    // Vertical rhythm in master section
    public const int LineCaption = 15;
    public const int LineBody = 22;
    public const int LineField = 28;
    public const int BlockGap = 12;
    public const int FieldGap = 16; // horizontal gap between date/time

    // Toolbar
    public const int ToolbarOuterH = 64;
    public const int ToolbarInnerPadX = 18;
    public const int ToolbarInnerPadY = 14;
    public const int ToolbarControlH = 34;
    public const int ToolbarGap = 10;
    public const int DirectionW = 238;
    public const int FormatW = 158;
    public const int CopyW = 76;

    // Source / master desk
    public const int SourceHNormal = 222;
    public const int SourceHReverse = 268;
    public const int ClockHostW = 308;
    public const int ClockHostPad = 14;

    // Wall
    public const int WallHeaderH = 44;
    public const int WallFooterH = 50;
    public const int WallListPad = 16;
    public const int WallCardPad = 4;

    // Footer
    public const int FooterH = 36;

    // Header brand
    public const int HeaderH = 58;
    public const int HeaderPadX = 22;

    // Clock wall tiles
    public const int TileW = 216;
    public const int TileH = 180;
    public const int TileMargin = 8;
    public const int TileInner = 14;
}
