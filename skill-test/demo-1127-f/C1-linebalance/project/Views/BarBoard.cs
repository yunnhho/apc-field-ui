using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;

namespace LineBalanceBoard.Views
{
    /// <summary>
    /// 공정별 사이클 타임 막대 + 택트 기준선. 표준 Label을 Panel 안에 배치만 한다(그리기 없음).
    /// 막대 색 = 판정 상태색, 막대 값 글자 = 테마 글자색, 막대 아래 = 공정 코드.
    /// </summary>
    internal sealed class BarBoard : Panel
    {
        private const int LineThickness = 4;      // 택트 기준선 두께(px, 6 이하)
        private const double BarFill = 0.6;       // 칸 폭 대비 막대 폭
        private const double HeadRoom = 1.15;     // 최대값 위 여유

        public event Action<int> BarClicked;

        private readonly Label[] _bars;
        private readonly Label[] _names;
        private readonly Label _taktLine = new Label();
        private readonly Label _taktText = new Label();
        private readonly double[] _seconds;
        private readonly double _takt;

        public BarBoard(string[] names, double[] seconds, StatusKind[] kinds, double takt)
        {
            _seconds = seconds;
            _takt = takt;
            BackColor = ApcTheme.C(ApcTheme.Current.Colors.Panel[0]);
            var textColor = ApcTheme.C(ApcTheme.Current.Colors.Text);

            _bars = new Label[names.Length];
            _names = new Label[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                _bars[i] = new Label
                {
                    AutoSize = false, TextAlign = ContentAlignment.TopCenter, ForeColor = textColor,
                    BackColor = ApcTheme.Status(kinds[i]), Font = ApcTheme.GetFont(FontStyle.Bold),
                    Text = seconds[i].ToString("0.0"), Padding = new Padding(0, ApcTheme.Px(4), 0, 0)
                };
                _names[i] = new Label
                {
                    AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, ForeColor = textColor,
                    BackColor = Color.Transparent, Font = ApcTheme.GetFont(), Text = names[i]
                };
                int index = i;
                _bars[i].Click += (s, e) => { if (BarClicked != null) BarClicked(index); };
                _names[i].Click += (s, e) => { if (BarClicked != null) BarClicked(index); };
                Controls.Add(_bars[i]);
                Controls.Add(_names[i]);
            }
            _taktLine.AutoSize = false;
            _taktLine.Text = "";
            _taktLine.BackColor = textColor;
            _taktText.AutoSize = false;
            _taktText.TextAlign = ContentAlignment.MiddleLeft;
            _taktText.ForeColor = textColor;
            _taktText.BackColor = Color.Transparent;
            _taktText.Font = ApcTheme.GetFont(FontStyle.Bold);
            _taktText.Text = "택트 " + takt.ToString("0.0") + "초";
            Controls.Add(_taktText);
            Controls.Add(_taktLine);
            _taktLine.BringToFront();
            LayoutBars();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutBars();
        }

        private void LayoutBars()
        {
            if (_bars == null || Width <= 0 || Height <= 0) return;
            int nameH = ApcTheme.Px(32);
            int pad = ApcTheme.Px(12);
            var area = new Rectangle(pad, pad, Width - pad * 2, Height - pad * 2 - nameH);
            double max = _takt;
            foreach (double s in _seconds) max = Math.Max(max, s);
            double top = max * HeadRoom;

            var slots = ApcLayout.Slots(area, _bars.Length, 1, BarFill, false);
            var cells = ApcLayout.Slots(area, _bars.Length, 1, 1.0, false);
            for (int i = 0; i < _bars.Length; i++)
            {
                int h = (int)Math.Round(area.Height * _seconds[i] / top);
                _bars[i].SetBounds(slots[i].X, area.Bottom - h, slots[i].Width, h);
                _names[i].SetBounds(cells[i].X, area.Bottom + ApcTheme.Px(4), cells[i].Width, nameH);
            }
            int lineY = area.Bottom - (int)Math.Round(area.Height * _takt / top);
            _taktLine.SetBounds(area.X, lineY - LineThickness / 2, area.Width, LineThickness);
            _taktText.SetBounds(area.X, lineY - ApcTheme.Px(30), ApcTheme.Px(150), ApcTheme.Px(28));
            _taktLine.BringToFront();
        }
    }
}
