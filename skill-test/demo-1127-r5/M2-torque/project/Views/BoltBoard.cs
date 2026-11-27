using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;

namespace TorqueSequence.Views
{
    /// <summary>
    /// 부품 윤곽(테두리 Panel) 위 체결 포인트 10개. 표준 Label만 배치한다(그리기 없음).
    /// 1~5는 윗줄, 6~10은 아랫줄. 위치·크기는 ApcLayout.Slots.
    /// </summary>
    internal sealed class BoltBoard : Panel
    {
        public const int PointCount = 10, Columns = 5, Rows = 2;
        private const double BoltFill = 0.6;
        private readonly Label[] _bolts = new Label[PointCount];

        public BoltBoard()
        {
            BorderStyle = BorderStyle.FixedSingle;
            BackColor = ApcTheme.C(ApcTheme.Current.Colors.Panel[0]);
            Dock = DockStyle.Fill;
            for (int i = 0; i < PointCount; i++)
            {
                _bolts[i] = new Label
                {
                    TextAlign = ContentAlignment.MiddleCenter,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = ApcTheme.GetFont(FontStyle.Bold),
                    ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text)
                };
                Controls.Add(_bolts[i]);
            }
        }

        /// <summary>point(1~10)의 상태 색과 글자("6" + 줄바꿈 + "다음").</summary>
        public void SetBolt(int point, StatusKind kind, string word)
        {
            var l = _bolts[point - 1];
            l.BackColor = ApcTheme.Status(kind);
            l.Text = point + "\n" + word;
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            if (_bolts[0] == null) return;
            var slots = ApcLayout.Slots(ClientRectangle, Columns, Rows, BoltFill, true);
            for (int i = 0; i < PointCount; i++) _bolts[i].SetBounds(slots[i].X, slots[i].Y, slots[i].Width, slots[i].Height);
        }
    }
}
