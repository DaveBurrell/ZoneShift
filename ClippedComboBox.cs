namespace TimezoneConverter;

/// <summary>
/// Drop-down list that draws selected text clipped with an ellipsis so long
/// timezone names never spill over neighbouring controls.
/// </summary>
internal sealed class ClippedComboBox : ComboBox
{
    public ClippedComboBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        DropDownStyle = ComboBoxStyle.DropDownList;
        IntegralHeight = false;
        MaxDropDownItems = 14;
        FlatStyle = FlatStyle.Flat;
        ItemHeight = 22;
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        e.DrawBackground();

        if (e.Index < 0 || e.Index >= Items.Count)
        {
            base.OnDrawItem(e);
            return;
        }

        var text = GetItemText(Items[e.Index]);
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var fore = selected ? SystemColors.HighlightText : ForeColor;

        TextRenderer.DrawText(
            e.Graphics,
            text,
            Font,
            e.Bounds,
            fore,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);

        e.DrawFocusRectangle();
    }
}
