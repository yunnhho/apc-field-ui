using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;
using PalletPacker.Data;

namespace PalletPacker.Views
{
    /// <summary>
    /// 팔레트 위에서 본 현재 층 도식(2행 x 4열 = 8칸). 표준 Label만 배치한다(직접 그리지 않음).
    /// 칸 색: 적재 = Run, 다음 놓을 칸 = Warn, 빈칸 = Stop.
    /// </summary>
    internal sealed class PalletBoard : Panel
    {
        private const int Columns = 4, Rows = 2;
        private readonly Label[] _cells = new Label[PalletRules.CellsPerLayer];

        public PalletBoard()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = ApcTheme.GetFont(),
                    ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text) };
                Controls.Add(_cells[i]);
            }
        }

        /// <summary>현재 층 번호(1~5). 다 쌓으면 마지막 층.</summary>
        public static int CurrentLayer(int boxCount)
        {
            return boxCount >= PalletRules.TargetBoxes ? PalletRules.Layers : boxCount / PalletRules.CellsPerLayer + 1;
        }

        public void Show(IList<BoxRecord> boxes)
        {
            int count = boxes.Count;
            int layer = CurrentLayer(count);
            int filled = count - (layer - 1) * PalletRules.CellsPerLayer;
            for (int i = 0; i < _cells.Length; i++)
            {
                int seq = (layer - 1) * PalletRules.CellsPerLayer + i + 1;
                StatusKind kind = i < filled ? StatusKind.Run : (i == filled ? StatusKind.Warn : StatusKind.Stop);
                _cells[i].BackColor = ApcTheme.Status(kind);
                _cells[i].Text = i < filled ? seq.ToString() : (i == filled ? seq + "  다음" : "");
            }
            LayoutCells();
        }

        protected override void OnResize(System.EventArgs eventargs)
        {
            base.OnResize(eventargs);
            LayoutCells();
        }

        private void LayoutCells()
        {
            int gap = ApcTheme.Px(12);
            int w = (ClientSize.Width - gap * (Columns - 1)) / Columns;
            int h = (ClientSize.Height - gap * (Rows - 1)) / Rows;
            for (int i = 0; i < _cells.Length; i++)
                _cells[i].SetBounds((i % Columns) * (w + gap), (i / Columns) * (h + gap), w, h);
        }
    }
}
