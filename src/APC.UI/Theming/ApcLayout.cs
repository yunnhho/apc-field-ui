using APC.UI.Controls;

namespace APC.UI.Theming
{
    /// <summary>
    /// Shared screen and dialog sizes (unscaled px; use with ApcTheme.Px). Used by ApcTemplateView and the ApcForm dialog helpers,
    /// and checked with the same values by LayoutLint in APC.UI.Tools (change the LayoutLint constants along with these).
    /// </summary>
    public static class ApcLayout
    {
        /// <summary>Outer margin of dialogs and screens.</summary>
        public const int Pad = 16;
        /// <summary>Between input rows; between main-screen rows.</summary>
        public const int RowGap = 12;
        /// <summary>Between groups (ApcGroup) and blocks; between content and the bottom button row.</summary>
        public const int GroupGap = 16;
        /// <summary>Between the caption column and the field.</summary>
        public const int LabelGap = 12;
        /// <summary>Between elements in one row (buttons, fields).</summary>
        public const int ItemGap = 8;
        public const int FooterButtonWidth = 110;
        public const int FooterButtonHeight = 40;
        /// <summary>Input field and caption height.</summary>
        public const int FieldHeight = ApcTextBox.BaseHeight;

        /// <summary>
        /// Item bounds for a Label-assembled diagram: splits <paramref name="area"/> into an even cols x rows grid and returns,
        /// row by row, one item per cell centered in it and sized to <paramref name="fill"/> (0~1) of the cell, so items grow
        /// with the area and are evenly spaced — no fixed px sizes or hand-picked positions. <paramref name="square"/>: the item
        /// side is <paramref name="fill"/> of the smaller cell side (bolts, cells); otherwise each side scales separately (tanks).
        /// Call it from the panel's <c>OnResize</c> with its <c>ClientRectangle</c> (minus padding) and <c>SetBounds</c> each Label.
        /// </summary>
        public static System.Drawing.Rectangle[] Slots(System.Drawing.Rectangle area, int cols, int rows, double fill, bool square)
        {
            if (cols <= 0 || rows <= 0) return new System.Drawing.Rectangle[0];
            fill = System.Math.Max(0, System.Math.Min(1, fill));
            var result = new System.Drawing.Rectangle[cols * rows];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    int x0 = area.Left + area.Width * c / cols, x1 = area.Left + area.Width * (c + 1) / cols;
                    int y0 = area.Top + area.Height * r / rows, y1 = area.Top + area.Height * (r + 1) / rows;
                    int w = (int)System.Math.Round((x1 - x0) * fill), h = (int)System.Math.Round((y1 - y0) * fill);
                    if (square) w = h = System.Math.Min(w, h);
                    result[r * cols + c] = new System.Drawing.Rectangle(x0 + (x1 - x0 - w) / 2, y0 + (y1 - y0 - h) / 2, w, h);
                }
            return result;
        }
    }
}
