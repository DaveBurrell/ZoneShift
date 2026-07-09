namespace TimezoneConverter;

/// <summary>
/// Shared spacing for the main window. Absolute-positioned controls ignore
/// <see cref="Control.Padding"/>, so insets are applied in coordinates explicitly.
/// </summary>
internal static class LayoutMetrics
{
    // Window
    public const int ClientWidth = 800;
    public const int ClientHeight = 860;
    public const int MinWidth = 760;
    public const int MinHeight = 780;

    // Outer gutters (app background between chrome and cards)
    public const int OuterX = 16;
    public const int OuterY = 12;
    public const int OuterBottom = 12;

    // Inside painted cards (docked hosts sit this far from rounded edge)
    public const int CardPad = 14;

    // Content inset inside master-clock left column (absolute layout)
    public const int ContentX = 4;
    public const int ContentY = 2;
    public const int ContentRightGutter = 12;

    // Vertical rhythm in master section
    public const int LineCaption = 14;
    public const int LineBody = 20;
    public const int LineField = 26;
    public const int BlockGap = 10;

    // Toolbar
    public const int ToolbarOuterH = 68;
    public const int ToolbarInnerPadX = 16;
    public const int ToolbarInnerPadY = 14;
    public const int ToolbarControlH = 32;
    public const int ToolbarGap = 12;

    // Source / master desk
    public const int SourceHNormal = 210;
    public const int SourceHReverse = 248;
    public const int ClockHostW = 300;
    public const int ClockHostPad = 16;

    // Wall
    public const int WallHeaderH = 40;
    public const int WallFooterH = 48;
    public const int WallListPad = 14;

    // Footer
    public const int FooterH = 34;

    // Header brand
    public const int HeaderH = 56;
    public const int HeaderPadX = 20;

    // Clock wall tiles
    public const int TileW = 212;
    public const int TileH = 176;
    public const int TileMargin = 10;
    public const int TileInner = 12;
}
