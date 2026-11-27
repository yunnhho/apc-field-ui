using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;
using TorqueSequence.Data;

namespace TorqueSequence.Views
{
    /// <summary>
    /// 부품 윤곽 위 체결 포인트 10개. 표준 Label 조립(그리기 코드 없음): 윤곽 Panel 안에 포인트 Label.
    /// 색 = 상태(OK=Run, NG=Alarm, 다음=Warn, 대기=Stop), 글자 = 번호 + 상태 말.
    /// </summary>
    internal sealed class BoltDiagram : Panel
    {
        private const int PerRow = 5;
        private readonly Panel _outline = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        private readonly Label[] _bolts = new Label[TorqueRules.Sequence.Length + 1];
        private readonly Label _caption = new Label { TextAlign = ContentAlignment.MiddleCenter, AutoSize = false };

        public BoltDiagram()
        {
            Dock = DockStyle.Fill;
            _caption.Font = ApcTheme.GetFont();
            _caption.Text = "실린더 헤드 커버 · 체결 순서 " + string.Join(" - ", Array.ConvertAll(TorqueRules.Sequence, p => p.ToString()));
            _outline.Controls.Add(_caption);
            for (int p = 1; p < _bolts.Length; p++)
            {
                _bolts[p] = new Label { TextAlign = ContentAlignment.MiddleCenter, AutoSize = false, Font = ApcTheme.GetFont(FontStyle.Bold) };
                _outline.Controls.Add(_bolts[p]);
            }
            Controls.Add(_outline);
            Resize += (s, e) => Place();
            _outline.Resize += (s, e) => Place();
        }

        public void SetPoint(int point, StatusKind kind, string word)
        {
            var b = _bolts[point];
            b.Text = point + "\r\n" + word;
            b.BackColor = ApcTheme.Status(kind);
            b.ForeColor = kind == StatusKind.Alarm ? Color.White : Color.Black;
        }

        private void Place()
        {
            int w = _outline.ClientSize.Width, h = _outline.ClientSize.Height;
            if (w <= 0 || h <= 0) return;
            int size = Math.Max(ApcTheme.Px(56), Math.Min(Math.Min(ApcTheme.Px(120), (int)(w * 0.76 / PerRow * 0.8)), (int)(h * 0.3)));
            for (int p = 1; p < _bolts.Length; p++)
            {
                int i = p - 1, col = i % PerRow, row = i / PerRow;
                int cx = (int)(w * (0.12 + 0.76 * col / (PerRow - 1)));
                int cy = (int)(h * (row == 0 ? 0.27 : 0.73));
                _bolts[p].SetBounds(cx - size / 2, cy - size / 2, size, size);
            }
            _caption.SetBounds(0, h / 2 - ApcTheme.Px(14), w, ApcTheme.Px(28));
        }
    }
}
