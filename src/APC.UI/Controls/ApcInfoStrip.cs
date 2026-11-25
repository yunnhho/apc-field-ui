using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 이름-값 정보판 한 줄: [공정 | 적층] [설비 | 12인치 적층기] [관리 항목 | 온도·Cycle time]처럼 진한 이름 칸과 흰 값 칸이 번갈아 나온다.
    /// 화면 헤더 아래 "지금 무엇을 보는가"(공정·설비·LOT·SPEC 등)를 적는 줄. 이름 칸은 글자 폭, 값 칸은 제 글자 폭 + 남은 폭을 똑같이 나눠 받는다(긴 값이 먼저 잘리지 않게).
    /// 값이 없으면 빈칸. 입력칸이 아니다(읽기 전용 표시).
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

        /// <summary>이름 칸들(왼→오). 값은 모두 빈칸으로 시작한다.</summary>
        public void SetItems(params string[] names)
        {
            _names = names ?? new string[0];
            _values = new string[_names.Length];
            Invalidate();
        }

        /// <summary>이름 칸 name 옆 값. 없는 이름이면 ArgumentException.</summary>
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

        /// <summary>값 칸 폭: 자리가 남으면 제 글자 폭 + 남는 폭 균등, 모자라면 글자 폭 비율로 줄인다.</summary>
        internal static int[] ValueWidths(int available, int[] need)
        {
            available = Math.Max(0, available);   // 이름 칸만으로 꽉 찬 좁은 줄: 값 칸 0(음수 폭으로 겹치지 않게)
            int n = need.Length, total = 0;
            foreach (int w in need) total += w;
            var widths = new int[n];
            for (int i = 0; i < n; i++)
                widths[i] = total <= available ? need[i] + (available - total) / n
                          : total == 0 ? available / n : (int)((long)available * need[i] / total);
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
