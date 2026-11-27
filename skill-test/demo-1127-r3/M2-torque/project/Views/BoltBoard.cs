using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;

namespace TorqueSequence.Views
{
    /// <summary>
    /// 부품 윤곽 도식: 테두리 Panel 안에 볼트 10개를 Label로 놓는다(위 줄 1~5, 아래 줄 6~10).
    /// 그리기 없음 — 위치·크기·색만 정한다. 다음 체결할 볼트는 더 크게 + 굵게.
    /// </summary>
    internal sealed class BoltBoard : Panel
    {
        private const int BoltPx = 80, NextBoltPx = 104;
        private const int PerRow = 5;

        private readonly Label[] _bolts = new Label[TorqueSequence.Data.TorqueJudge.PointCount + 1];
        private readonly bool[] _isNext = new bool[TorqueSequence.Data.TorqueJudge.PointCount + 1];

        public BoltBoard()
        {
            BorderStyle = BorderStyle.FixedSingle;
            BackColor = Color.White;
            for (int p = 1; p < _bolts.Length; p++)
            {
                _bolts[p] = new Label { Text = p.ToString(), TextAlign = ContentAlignment.MiddleCenter, AutoSize = false };
                Controls.Add(_bolts[p]);
            }
            Resize += (s, e) => PlaceBolts();
        }

        /// <summary>포인트 하나의 상태 색. isNext면 강조.</summary>
        public void SetPoint(int point, StatusKind kind, bool isNext)
        {
            Label b = _bolts[point];
            b.BackColor = ApcTheme.Status(kind);
            b.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text);
            b.Font = ApcTheme.GetFont(isNext ? FontStyle.Bold : FontStyle.Regular);
            _isNext[point] = isNext;
            PlaceBolts();
        }

        private void PlaceBolts()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0 || _bolts[1] == null) return;
            for (int p = 1; p < _bolts.Length; p++)
            {
                int row = (p - 1) / PerRow, col = (p - 1) % PerRow;
                int size = ApcTheme.Px(_isNext[p] ? NextBoltPx : BoltPx);
                int cx = ClientSize.Width * (2 * col + 1) / (2 * PerRow);
                int cy = ClientSize.Height * (row == 0 ? 3 : 7) / 10;
                _bolts[p].SetBounds(cx - size / 2, cy - size / 2, size, size);
                _bolts[p].BringToFront();
            }
        }
    }
}
