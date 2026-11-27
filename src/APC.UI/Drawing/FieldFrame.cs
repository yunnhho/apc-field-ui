using System.Drawing;
using APC.UI.Theming;

namespace APC.UI.Drawing
{
    /// <summary>Common input field look: white background + border + inner top shadow (buttons raised, fields inset). The field being edited gets a 2px blue border + light blue background (visible from 1–2 m).</summary>
    internal static class FieldFrame
    {
        internal static readonly Color FocusColor = Color.FromArgb(0x3F, 0x67, 0xA6);   // 확인 버튼과 같은 파랑
        internal static readonly Color FocusFill = Color.FromArgb(0xEE, 0xF4, 0xFC);
        private static readonly Color InsetShadow = Color.FromArgb(64, 0, 0, 0);

        internal static Color Fill(bool enabled, bool readOnly, bool focused = false)
        {
            if (ApcTheme.Current == null) return SystemColors.Window;
            var c = ApcTheme.Current.Colors;
            if (!enabled) return ApcTheme.C(c.Background[0]);
            if (readOnly) return ApcTheme.C(c.GridLine);
            return focused ? FocusFill : Color.White;
        }

        internal static Color TextColor(bool enabled)
        {
            if (ApcTheme.Current == null) return enabled ? SystemColors.WindowText : SystemColors.GrayText;
            var c = ApcTheme.Current.Colors;
            return ApcTheme.C(enabled ? c.Text : c.TextMuted);
        }

        internal static void DrawBorder(Graphics g, Rectangle r, bool focused)
        {
            if (r.Width < 3 || r.Height < 3) return;
            var border = ApcTheme.Current == null ? SystemColors.ControlDark : ApcTheme.C(ApcTheme.Current.Colors.BorderStrong);
            if (focused)
            {
                using (var pen = new Pen(FocusColor))
                {
                    g.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
                    g.DrawRectangle(pen, r.X + 1, r.Y + 1, r.Width - 3, r.Height - 3);
                }
                return;
            }
            using (var pen = new Pen(border)) g.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
            using (var pen = new Pen(InsetShadow)) g.DrawLine(pen, r.X + 1, r.Y + 1, r.Right - 2, r.Y + 1);
        }

        /// <summary>Width of the button area inside the right edge (shared by combo arrow, numeric ▲▼, date ▼).</summary>
        internal static int ButtonWidth(int height)
        {
            return System.Math.Max(height - 2, (int)System.Math.Round(24 * ApcTheme.Scale));
        }

        /// <summary>In-field button area: gradient + left divider. Slightly darker when pressed.</summary>
        internal static void DrawButtonArea(Graphics g, Rectangle area, bool pressed)
        {
            if (ApcTheme.Current == null) { g.FillRectangle(SystemBrushes.Control, area); return; }
            var c = ApcTheme.Current.Colors;
            Gradients.Fill(g, area, c.GridHeader);
            if (pressed) using (var b = new SolidBrush(PressedShade)) g.FillRectangle(b, area);
            using (var pen = new Pen(ApcTheme.C(c.Border))) g.DrawLine(pen, area.Left, area.Top, area.Left, area.Bottom);
        }

        /// <summary>Triangle centered in the button area (▲ up / ▼ down).</summary>
        internal static void DrawTriangle(Graphics g, Rectangle r, Color color, bool up)
        {
            int w = ApcTheme.Px(TriangleWidth), h = ApcTheme.Px(TriangleWidth / 2);   // 고정 크기: 콤보·날짜 ▼와 숫자칸 ▲▼가 칸 높이와 상관없이 같다
            int x = r.Left + (r.Width - w) / 2, y = r.Top + (r.Height - h) / 2;
            // FillPolygon은 방향에 따라 가장자리 픽셀이 달라져 ▲가 ▼보다 작게 나온다 → 한 줄씩 채워 위·아래를 픽셀 단위로 같게
            using (var b = new SolidBrush(color))
                for (int i = 0; i < h; i++)
                {
                    int row = up ? y + h - 1 - i : y + i;
                    g.FillRectangle(b, x + i, row, w - 2 * i, 1);
                }
        }

        private const int TriangleWidth = 14;
        private static readonly Color PressedShade = Color.FromArgb(40, 0, 0, 0);
    }
}
