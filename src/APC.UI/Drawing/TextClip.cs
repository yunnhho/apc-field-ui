using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace APC.UI.Drawing
{
    /// <summary>
    /// Text drawn with an ellipsis (…). When the render check (APC.UI.Tools LayoutLint) turns on Log via reflection, clipped text is recorded.
    /// Normally Log is null and it only draws.
    /// </summary>
    internal static class TextClip
    {
        internal static List<Tuple<Control, string>> Log;

        internal static void Draw(Graphics g, Control owner, string text, Font font, Rectangle rect, Color color, TextFormatFlags flags)
        {
            TextRenderer.DrawText(g, text, font, rect, color, flags);
            if (Log == null || string.IsNullOrEmpty(text) || rect.Width <= 0) return;
            int need = TextRenderer.MeasureText(g, text, font, Size.Empty, flags & ~TextFormatFlags.EndEllipsis).Width;
            if (need > rect.Width) Log.Add(Tuple.Create(owner, "'" + text + "' 말줄임(…)으로 잘림(필요 " + need + "px > 폭 " + rect.Width + "px)"));
        }
    }
}
