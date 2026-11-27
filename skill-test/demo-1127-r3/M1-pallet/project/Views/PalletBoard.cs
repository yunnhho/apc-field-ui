using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;
using PalletPacker.Data;

namespace PalletPacker.Views
{
    /// <summary>팔레트를 위에서 본 현재 층 도식(2행 x 4열). 표준 Label만 배치한다(그리기 없음).</summary>
    internal sealed class PalletBoard : Panel
    {
        private const int Rows = 2, Cols = 4;
        private readonly Label[] _cells = new Label[PalletRules.BoxesPerFloor];

        public PalletBoard()
        {
            BackColor = Color.Transparent;
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = new Label
                {
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = ApcTheme.GetFont(FontStyle.Bold),
                    ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text)
                };
                Controls.Add(_cells[i]);
            }
            Resize += (s, e) => Place();
        }

        /// <summary>현재 층 칸 채우기. firstNumber = 이 층 첫 칸의 팔레트 내 순번, placedInFloor = 이 층에 쌓은 수.</summary>
        public void Show(int firstNumber, int placedInFloor)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                bool placed = i < placedInFloor;
                bool next = i == placedInFloor;
                _cells[i].Text = placed || next ? (firstNumber + i).ToString() : "";
                _cells[i].BackColor = ApcTheme.Status(placed ? StatusKind.Run : next ? StatusKind.Warn : StatusKind.Stop);
            }
        }

        private void Place()
        {
            int gap = ApcTheme.Px(12);
            int w = (ClientSize.Width - gap * (Cols - 1)) / Cols;
            int h = (ClientSize.Height - gap * (Rows - 1)) / Rows;
            if (w <= 0 || h <= 0) return;
            for (int i = 0; i < _cells.Length; i++)
                _cells[i].SetBounds((i % Cols) * (w + gap), (i / Cols) * (h + gap), w, h);
        }
    }
}
