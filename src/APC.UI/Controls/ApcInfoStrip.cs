using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// One-line name-value strip: dark name cells and white value cells alternate, e.g. [공정 | 적층] [설비 | 12인치 적층기] [관리 항목 | 온도·Cycle time].
    /// The line below the screen header that says "what you are looking at" (process, equipment, LOT, SPEC, etc.). Name cells take their text width; value cells take their text width + an equal share of the rest (so long values are not clipped first).
    /// No value = empty. Not an input field (read-only display).
    /// </summary>
    public class ApcInfoStrip : Control
    {
        private const int BaseNamePad = 16;
        private const int BaseValuePad = 12;

        private string[] _names = new string[0];
        private string[] _values = new string[0];

        public ApcInfoStrip()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Top;
            Height = (int)Math.Round(ApcTheme.MinHeight * 1.15);   // 표 머리줄과 같은 높이
            ApcTheme.Changed += OnThemeChanged;
        }

        /// <summary>Name cells (left to right). All values start empty.</summary>
        public void SetItems(params string[] names)
        {
            _names = names ?? new string[0];
            _values = new string[_names.Length];
            Invalidate();
        }

        /// <summary>Value next to name cell name. ArgumentException for an unknown name.</summary>
        public void SetValue(string name, string value)
        {
            int i = Array.IndexOf(_names, name);
            if (i < 0) throw new ArgumentException("정보판에 없는 이름입니다: " + name, "name");
            _values[i] = value ?? string.Empty;
            Invalidate();
        }

        public string GetValue(string name)
        {
            int i = Array.IndexOf(_names, name);
            return i < 0 ? null : _values[i] ?? string.Empty;
        }

        private static int Px(int px) { return (int)Math.Round(px * ApcTheme.Scale); }

        /// <summary>
        /// Width at which no name or value is clipped (scaled px). A dialog's AddContent(strip) widens the dialog to this width.
        /// On a screen inside Row(), set Width to at least this value.
        /// </summary>
        public int PreferredWidth
        {
            get
            {
                if (ApcTheme.Current == null) return 0;
                var nameFont = ApcTheme.GetFont();
                var valueFont = ApcTheme.GetFont(FontStyle.Bold);
                int w = 0;
                for (int i = 0; i < _names.Length; i++)
                    w += TextRenderer.MeasureText(_names[i], nameFont).Width + Px(BaseNamePad) * 2
                       + TextRenderer.MeasureText(_values[i] ?? "", valueFont).Width + Px(BaseValuePad) * 2 + Px(2);   // +2: 표시 측정과의 반올림 차
                return w;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);
            if (ApcTheme.Current == null || _names.Length == 0) return;

            var c = ApcTheme.Current.Colors;
            var nameFont = ApcTheme.GetFont();
            var valueFont = ApcTheme.GetFont(FontStyle.Bold);   // 값 강조
            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
                        | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

            var nameWidths = new int[_names.Length];
            int namesTotal = 0;
            for (int i = 0; i < _names.Length; i++)
            {
                nameWidths[i] = TextRenderer.MeasureText(g, _names[i], nameFont).Width + Px(BaseNamePad) * 2;
                namesTotal += nameWidths[i];
            }
            var need = new int[_names.Length];
            for (int i = 0; i < _names.Length; i++)
                need[i] = TextRenderer.MeasureText(g, _values[i] ?? "", valueFont).Width + Px(BaseValuePad) * 2;
            var valueWidths = ValueWidths(Width - namesTotal, need);

            int x = 0;
            using (var pen = new Pen(ApcTheme.C(c.Border)))
            {
                for (int i = 0; i < _names.Length; i++)
                {
                    var name = new Rectangle(x, 0, nameWidths[i], Height);
                    Gradients.Fill(g, name, c.Header);
                    TextRenderer.DrawText(g, _names[i], nameFont, name, ApcTheme.C(c.TextInverse), flags);
                    x = name.Right;
                    // 마지막 값 칸은 나눗셈 나머지까지 끝까지
                    var value = new Rectangle(x, 0, i == _names.Length - 1 ? Width - x : valueWidths[i], Height);
                    var inner = Rectangle.Inflate(value, -Px(BaseValuePad), 0);
                    TextClip.Draw(g, this, _values[i], valueFont, inner, ApcTheme.C(c.Text), flags);
                    x = value.Right;
                    if (i < _names.Length - 1) g.DrawLine(pen, x - 1, 0, x - 1, Height - 1);
                }
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        /// <summary>
        /// Value cell widths: with spare room, text width + an equal share of the rest. When short, the longest values shrink first — a common cap is lowered for all cells
        /// so values shorter than the cap (V2, 닫힘) stay fully visible and only long values get an ellipsis (shrinking all proportionally clips every value even when only a few px are missing).
        /// </summary>
        internal static int[] ValueWidths(int available, int[] need)
        {
            available = Math.Max(0, available);   // 이름 칸만으로 꽉 찬 좁은 줄: 값 칸 0(음수 폭으로 겹치지 않게)
            int n = need.Length, total = 0;
            foreach (int w in need) total += w;
            var widths = new int[n];
            if (total <= available)
            {
                for (int i = 0; i < n; i++) widths[i] = need[i] + (available - total) / n;
                return widths;
            }
            // 상한 cap: sum(min(need, cap)) <= available인 가장 큰 값. 짧은 값부터 다 주고 남는 폭을 긴 값들이 나눈다.
            var sorted = (int[])need.Clone();
            Array.Sort(sorted);
            int left = available, rest = n, cap = 0;
            foreach (int w in sorted)
            {
                if (w * rest <= left) { left -= w; rest--; cap = w; continue; }
                cap = left / rest;
                break;
            }
            for (int i = 0; i < n; i++) widths[i] = Math.Min(need[i], cap);
            return widths;
        }

        private void OnThemeChanged(object sender, EventArgs e) { Height = (int)Math.Round(ApcTheme.MinHeight * 1.15); Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
