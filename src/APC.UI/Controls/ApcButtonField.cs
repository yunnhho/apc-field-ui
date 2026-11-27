using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// Input field + a button area inside its right edge (same look as the combo arrow). This control draws the border and focus border;
    /// the inner text box has no border. The button area can be split top/bottom (numeric ▲▼). Base of ApcNumericBox and ApcDateBox.
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

        /// <summary>Inner text box (focus target, restored after a scan).</summary>
        public ApcTextBox TextBox { get { return _text; } }

        /// <summary>Number of stacked cells the button area is split into.</summary>
        internal abstract int SegmentCount { get; }
        internal abstract bool SegmentPointsUp(int index);
        internal abstract void OnSegmentClick(int index);

        internal Rectangle ButtonArea
        {
            get { int w = FieldFrame.ButtonWidth(Height); return new Rectangle(Width - w, 0, w, Height); }
        }

        /// <summary>Index of the button cell under the point; -1 outside the button area.</summary>
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
