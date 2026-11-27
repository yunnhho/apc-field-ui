using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace PalletPacker.Views
{
    /// <summary>팔레트를 위에서 본 현재 층 도식(Label 조립): 2×4 칸, 적재=Run, 다음=Warn, 빈칸=Stop.</summary>
    internal sealed class PalletBoard : Panel
    {
        private const int Cols = 4, Rows = 2, Count = Cols * Rows;
        private readonly Panel _frame = new Panel { Dock = DockStyle.Fill };
        private readonly Label[] _cells = new Label[Count];

        public PalletBoard()
        {
            BackColor = ApcTheme.C(ApcTheme.Current.Colors.BorderStrong);   // 팔레트 테두리
            Padding = new Padding(ApcTheme.Px(4));
            _frame.BackColor = ApcTheme.C(ApcTheme.Current.Colors.Border);
            Controls.Add(_frame);
            for (int i = 0; i < Count; i++)
            {
                _cells[i] = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = ApcTheme.GetFont(FontStyle.Bold),
                    ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text) };
                _frame.Controls.Add(_cells[i]);
            }
            _frame.Resize += (s, e) => Place();
        }

        /// <summary>loaded칸까지 적재(순번 firstNo부터), 다음 칸 하나 표시. 층이 다 찼으면 다음 칸 없음.</summary>
        public void Show(int loaded, int firstNo)
        {
            for (int i = 0; i < Count; i++)
            {
                StatusKind kind = i < loaded ? StatusKind.Run : (i == loaded ? StatusKind.Warn : StatusKind.Stop);
                _cells[i].BackColor = ApcTheme.Status(kind);
                _cells[i].Text = i < loaded ? (firstNo + i).ToString() : (i == loaded ? "다음" : "");
            }
        }

        private void Place()
        {
            Rectangle[] slots = ApcLayout.Slots(_frame.ClientRectangle, Cols, Rows, 0.88, false);
            for (int i = 0; i < Count; i++) _cells[i].SetBounds(slots[i].X, slots[i].Y, slots[i].Width, slots[i].Height);
        }
    }
}
