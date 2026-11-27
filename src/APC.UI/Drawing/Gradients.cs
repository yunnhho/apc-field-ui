using System.Drawing;
using System.Drawing.Drawing2D;
using APC.UI.Theming;

namespace APC.UI.Drawing
{
    public static class Gradients
    {
        /// <summary>Vertical gradient. pair = [top, bottom] (#RRGGBB)</summary>
        public static void Fill(Graphics g, Rectangle r, string[] pair)
        {
            Fill(g, r, ApcTheme.C(pair[0]), ApcTheme.C(pair[1]));
        }

        public static void Fill(Graphics g, Rectangle r, Color top, Color bottom)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            // 브러시 영역을 1px 늘려야 마지막 줄에 시작색이 반복되는 GDI+ 버그를 피한다.
            var brushRect = new Rectangle(r.X, r.Y - 1, r.Width, r.Height + 2);
            using (var brush = new LinearGradientBrush(brushRect, top, bottom, LinearGradientMode.Vertical))
                g.FillRectangle(brush, r);
        }
    }
}
