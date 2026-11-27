using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// Titled group box (blocks in a settings dialog etc., instead of the standard GroupBox). Panel background + bold title with a brand-color bar on the left.
    /// Child controls are laid out inside Padding (below the title band + margin) — use Dock or a FlowLayoutPanel (Dock=Fill).
    /// </summary>
    public class ApcGroup : Panel
    {
        private const int BaseTitleH = 30, BasePad = 10, BaseBar = 3, BaseBarGap = 7;

        public ApcGroup()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            ApcTheme.Changed += OnThemeChanged;
            ApplyTheme();
        }

        /// <summary>Title band text.</summary>
        public override string Text { get { return base.Text; } set { base.Text = value; Invalidate(); } }

        internal int TitleHeight { get { return ApcTheme.Px(BaseTitleH); } }

        /// <summary>Group height that holds content of the given height (scaled px) = title band + top/bottom margin + content. On a screen holding Columns() rows, content = sum of row heights.</summary>
        public int HeightFor(int contentHeight) { return Padding.Vertical + contentHeight; }

        /// <summary>Background is the solid panel start color, so children inheriting BackColor do not clash with the gradient.</summary>
        private void ApplyTheme()
        {
            BackColor = ApcTheme.Current == null ? Color.White : ApcTheme.C(ApcTheme.Current.Colors.Panel[0]);
            ApplyPadding();
        }

        private void ApplyPadding()
        {
            int p = ApcTheme.Px(BasePad);
            Padding = new Padding(p, TitleHeight + p, p, p);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            if (ApcTheme.Current == null) return;
            var c = ApcTheme.Current.Colors;
            using (var pen = new Pen(ApcTheme.C(c.Border))) g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            using (var hi = new Pen(Color.White)) g.DrawLine(hi, 1, 1, Width - 2, 1);

            // 라벨: 왼쪽 3px 브랜드색 막대 + 굵은 먹색 글자
            var font = ApcTheme.GetFont(FontStyle.Bold);
            int pad = ApcTheme.Px(BasePad), bar = ApcTheme.Px(BaseBar);
            int barH = Math.Min(TitleHeight - 2, TextRenderer.MeasureText(g, "가", font).Height);
            using (var mark = new SolidBrush(ApcTheme.C(c.Brand))) g.FillRectangle(mark, pad, (TitleHeight - barH) / 2, bar, barH);
            int textX = pad + bar + ApcTheme.Px(BaseBarGap);
            TextClip.Draw(g, this, Text, font, new Rectangle(textX, 0, Math.Max(0, Width - textX - pad), TitleHeight),
                ApcTheme.C(c.Text), TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        private void OnThemeChanged(object sender, EventArgs e) { ApplyTheme(); Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
