using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 입력칸 + 칸 안 오른쪽 버튼 영역(콤보 화살표와 같은 모양). 테두리·포커스 테두리는 이 컨트롤이 그리고,
    /// 안쪽 입력칸은 테두리 없이 들어간다. 버튼 영역은 위아래로 나눌 수 있다(숫자칸 ▲▼). ApcNumericBox·ApcDateBox의 바탕.
    /// </summary>
    public abstract class ApcButtonField : UserControl
    {
        private readonly ApcTextBox _text;
        private int _pressed = -1;

        internal ApcButtonField(ApcTextBox text)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            _text = text;
            _text.Borderless = true;
            _text.GotFocus += (s, e) => Invalidate();
            _text.LostFocus += (s, e) => Invalidate();
            _text.ReadOnlyChanged += (s, e) => Invalidate();
            Controls.Add(_text);
            Size = new Size(ApcTheme.Px(150), ApcTheme.Px(ApcTextBox.BaseHeight));
        }

        /// <summary>안쪽 입력칸(포커스 지정·스캔 복원 대상).</summary>
        public ApcTextBox TextBox { get { return _text; } }

        /// <summary>버튼 영역을 위아래로 몇 칸으로 나누는지.</summary>
        internal abstract int SegmentCount { get; }
        internal abstract bool SegmentPointsUp(int index);
        internal abstract void OnSegmentClick(int index);

        internal Rectangle ButtonArea
        {
            get { int w = FieldFrame.ButtonWidth(Height); return new Rectangle(Width - w, 0, w, Height); }
        }

        /// <summary>점이 몇 번째 버튼 칸인지. 버튼 영역 밖이면 -1.</summary>
        internal int SegmentAt(Point p)
        {
            var area = ButtonArea;
            if (!area.Contains(p)) return -1;
            return Math.Min(SegmentCount - 1, (p.Y - area.Top) * SegmentCount / area.Height);
        }

        private Rectangle Segment(int index)
        {
            var area = ButtonArea;
            int top = area.Top + area.Height * index / SegmentCount, bottom = area.Top + area.Height * (index + 1) / SegmentCount;
            return Rectangle.FromLTRB(area.Left, top, area.Right, bottom);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = ClientRectangle;
            using (var b = new SolidBrush(FieldFrame.Fill(Enabled, _text.ReadOnly, _text.Focused))) g.FillRectangle(b, rect);
            var glyph = FieldFrame.TextColor(Enabled && !_text.ReadOnly);
            for (int i = 0; i < SegmentCount; i++)
            {
                var seg = Segment(i);
                FieldFrame.DrawButtonArea(g, seg, i == _pressed);
                if (i > 0 && ApcTheme.Current != null)
                    using (var pen = new Pen(ApcTheme.C(ApcTheme.Current.Colors.Border))) g.DrawLine(pen, seg.Left, seg.Top, seg.Right, seg.Top);
                FieldFrame.DrawTriangle(g, seg, glyph, SegmentPointsUp(i));
            }
            FieldFrame.DrawBorder(g, rect, _text.Focused && !_text.ReadOnly);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int inset = 2;   // 포커스 테두리 2px 안쪽
            _text.SetBounds(inset, inset, Math.Max(ApcTheme.Px(20), ButtonArea.Left - inset), Height - 2 * inset);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || !Enabled || _text.ReadOnly) return;
            _pressed = SegmentAt(e.Location);
            if (_pressed < 0) return;
            Invalidate();
            OnSegmentClick(_pressed);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_pressed < 0) return;
            _pressed = -1;
            Invalidate();
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            base.SetBoundsCore(x, y, width, Math.Max(height, ApcTheme.MinHeight), specified);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _text.Enabled = Enabled;
            Invalidate();
        }
    }
}
