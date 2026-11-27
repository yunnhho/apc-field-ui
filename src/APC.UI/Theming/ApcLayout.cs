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
    }
}
