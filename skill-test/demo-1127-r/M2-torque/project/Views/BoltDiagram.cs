using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;

namespace TorqueSequence.Views
{
    /// <summary>
    /// 실린더 헤드 커버 윤곽 위 체결 포인트 10개. 표준 Label을 Panel 안에 배치만 한다(직접 그리기 없음).
    /// 위 줄 1~5, 아래 줄 6~10 (왼→오). 다음 포인트는 크게 + 굵게.
    /// </summary>
    internal sealed class BoltDiagram : Panel
    {
        private readonly Panel _outline = new Panel { BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
        private readonly Label[] _bolts = new Label[11];
        private int _next;

        public BoltDiagram()
        {
            BackColor = Color.Transparent;
            for (int p = 1; p <= 10; p++)
            {
                _bolts[p] = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, BackColor = ApcTheme.Status(StatusKind.Stop),
                    ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text), Font = ApcTheme.GetFont(), Text = p.ToString() };
                _outline.Controls.Add(_bolts[p]);
            }
            Controls.Add(_outline);
        }

        /// <summary>포인트 하나의 색·글자. word가 있으면 번호 아래 줄에 결과를 적는다(색만으로 뜻을 전하지 않는다).</summary>
        public void SetBolt(int point, StatusKind kind, string word, bool isNext)
        {
            Label l = _bolts[point];
            l.BackColor = ApcTheme.Status(kind);
            l.Text = point + (string.IsNullOrEmpty(word) ? "" : Environment.NewLine + word);
            l.Font = ApcTheme.GetFont(isNext ? FontStyle.Bold : FontStyle.Regular);
            if (isNext) _next = point; else if (_next == point) _next = 0;
            LayoutBolts();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutBolts();
        }

        private void LayoutBolts()
        {
            int w = Width, h = Height;
            if (w <= 0 || h <= 0) return;
            _outline.SetBounds(0, 0, w, h);
            int cw = _outline.ClientSize.Width, ch = _outline.ClientSize.Height;
            int d = Math.Min(cw / 6, ch / 4);
            for (int p = 1; p <= 10; p++)
            {
                int col = (p - 1) % 5, row = (p - 1) / 5;
                int size = p == _next ? d + d / 4 : d;
                int cx = cw * (2 * col + 1) / 10, cy = ch * (row == 0 ? 25 : 75) / 100;
                _bolts[p].SetBounds(cx - size / 2, cy - size / 2, size, size);
            }
        }
    }
}
