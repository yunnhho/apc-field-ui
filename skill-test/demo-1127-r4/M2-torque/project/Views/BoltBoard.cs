using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;

namespace TorqueSequence.Views
{
    /// <summary>부품 윤곽 위 체결 포인트 10개. 표준 Label만 배치한다(직접 그리기 없음). 볼트는 윤곽 Panel의 자식.</summary>
    internal sealed class BoltBoard : Panel
    {
        private const int Count = 10, PerRow = 5;
        private readonly Panel _outline = new Panel { BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
        private readonly Label _title = new Label { Text = "실린더 헤드 커버", TextAlign = ContentAlignment.MiddleCenter };
        private readonly Label[] _bolts = new Label[Count];
        private int _emphasis;

        public BoltBoard()
        {
            _title.Font = ApcTheme.GetFont();
            _title.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.TextMuted);
            _outline.Controls.Add(_title);
            for (int i = 0; i < Count; i++)
            {
                _bolts[i] = new Label
                {
                    Text = (i + 1).ToString(), TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle,
                    Font = ApcTheme.GetFont(FontStyle.Bold), ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text)
                };
                _outline.Controls.Add(_bolts[i]);
            }
            Controls.Add(_outline);
            Resize += (s, e) => PlaceBolts();
        }

        public void SetBolt(int point, StatusKind kind)
        {
            _bolts[point - 1].BackColor = ApcTheme.Status(kind);
        }

        /// <summary>다음 체결할 포인트(크게 보인다). 0 = 없음.</summary>
        public void SetEmphasis(int point)
        {
            _emphasis = point;
            PlaceBolts();
        }

        private void PlaceBolts()
        {
            int w = ClientSize.Width, h = ClientSize.Height;
            if (w <= 0 || h <= 0) return;
            int b = Math.Min(ApcTheme.Px(64), Math.Min(w / 12, h / 5));
            int pad = b / 2;
            _outline.SetBounds(pad, pad, w - 2 * pad, h - 2 * pad);
            int iw = _outline.ClientSize.Width, ih = _outline.ClientSize.Height;
            for (int p = 1; p <= Count; p++)
            {
                int col = (p - 1) % PerRow;
                bool top = p <= PerRow;
                int size = p == _emphasis ? b * 13 / 10 : b;
                int cx = (2 * col + 1) * iw / (2 * PerRow);
                int cy = top ? b : ih - b;
                _bolts[p - 1].SetBounds(cx - size / 2, cy - size / 2, size, size);
            }
            int th = ApcTheme.Px(32);
            _title.SetBounds(iw / 4, (ih - th) / 2, iw / 2, th);
        }
    }
}
