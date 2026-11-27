using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// Confirmation check (preheat temperature checked, safety check done, etc.). Same height as an input field, box on the left + sentence on the right. Clicking the box or the text toggles it.
    /// When checked, the box fills with the confirm-button blue and shows a white check mark. Width follows the text (as the field of AddFieldRow it takes the field width).
    /// For actions that require the check, change the confirm button's Enabled and its reason (SetFooterNote) together in CheckedChanged.
    /// </summary>
    public class ApcCheckBox : CheckBox
    {
        private const int BaseBox = 24;
        private const int BaseGap = 10;

        public ApcCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            // ButtonBase는 Opaque를 켜 배경을 안 칠한다 → 네모·글자 밖에 이전 화면이 남는다(실행 화면만, render는 정상)
            SetStyle(ControlStyles.Opaque, false);
            BackColor = Color.Transparent;
            AutoSize = false;
            ApcTheme.Changed += OnThemeChanged;
            ApplyTheme();
        }

        private static int Px(int px) { return (int)Math.Round(px * ApcTheme.Scale); }

        /// <summary>Box + gap + text width (scaled px).</summary>
        public int PreferredWidth
        {
            get { return Px(BaseBox) + Px(BaseGap) + TextRenderer.MeasureText(Text ?? "", Font).Width + Px(4); }
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            base.SetBoundsCore(x, y, width, Math.Max(height, ApcTheme.MinHeight), specified);
        }

        protected override void OnTextChanged(EventArgs e) { Width = PreferredWidth; Invalidate(); base.OnTextChanged(e); }
        protected override void OnCheckedChanged(EventArgs e) { Invalidate(); base.OnCheckedChanged(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        internal Rectangle BoxBounds
        {
            get { int s = Px(BaseBox); return new Rectangle(0, (Height - s) / 2, s, s); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var box = BoxBounds;
            bool focused = Focused && ShowFocusCues;
            if (Checked && Enabled)
                using (var b = new SolidBrush(FieldFrame.FocusColor)) g.FillRectangle(b, box);
            else
                using (var b = new SolidBrush(FieldFrame.Fill(Enabled, false))) g.FillRectangle(b, box);
            if (!(Checked && Enabled) || focused) FieldFrame.DrawBorder(g, box, focused);
            if (Checked) DrawMark(g, box, Enabled ? Color.White : FieldFrame.TextColor(false));

            var text = new Rectangle(box.Right + Px(BaseGap), 0, Math.Max(0, Width - box.Right - Px(BaseGap)), Height);
            TextRenderer.DrawText(g, Text, Font, text, FieldFrame.TextColor(Enabled),
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        private static void DrawMark(Graphics g, Rectangle box, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = box.Width;
            var pts = new[]
            {
                new PointF(box.X + s * 0.22f, box.Y + s * 0.52f),
                new PointF(box.X + s * 0.42f, box.Y + s * 0.72f),
                new PointF(box.X + s * 0.78f, box.Y + s * 0.30f),
            };
            using (var pen = new Pen(color, Math.Max(2f, s * 0.12f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                g.DrawLines(pen, pts);
            g.SmoothingMode = SmoothingMode.Default;
        }

        private void ApplyTheme()
        {
            if (ApcTheme.Current != null) Font = ApcTheme.GetFont();
            Height = Math.Max(Height, (int)Math.Round(ApcTextBox.BaseHeight * ApcTheme.Scale));
            Width = PreferredWidth;
        }

        private void OnThemeChanged(object sender, EventArgs e) { ApplyTheme(); Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
