using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// Status color legend: lamp + meaning, left to right ("● 관리선 내  ● 관리선 이탈  ● SPEC 초과"), so the screen tells what the lamp colors in tables and charts mean.
    /// Give the items with SetItems in that screen's verdict wording. Width follows the items. To place it at the right of a section title row, pass it as tools of Titled("...", content, legend) (no Dock).
    /// </summary>
    public class ApcStatusLegend : Control
    {
        private const int BaseLamp = 18;
        private const int BaseLampGap = 6;
        private const int BaseItemGap = 20;

        private ApcLampValue[] _items = new ApcLampValue[0];

        public ApcStatusLegend()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = ApcTheme.MinHeight;
            ApcTheme.Changed += OnThemeChanged;
        }

        /// <summary>Legend items (status + meaning). Calling it resizes Width to the items.</summary>
        public void SetItems(params ApcLampValue[] items)
        {
            _items = items ?? new ApcLampValue[0];
            Width = PreferredWidth;
            Invalidate();
        }

        /// <summary>Width that draws all items (scaled px).</summary>
        public int PreferredWidth
        {
            get
            {
                int w = 0;
                foreach (var item in _items)
                    w += Px(BaseLamp) + Px(BaseLampGap) + TextRenderer.MeasureText(item.Text, ApcTheme.GetFont()).Width;
                return _items.Length == 0 ? 0 : w + Px(BaseItemGap) * (_items.Length - 1);
            }
        }

        private static int Px(int px) { return (int)Math.Round(px * ApcTheme.Scale); }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (ApcTheme.Current == null || _items.Length == 0) return;
            var g = e.Graphics;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            var font = ApcTheme.GetFont();
            var fore = ApcTheme.C(ApcTheme.Current.Colors.Text);
            int lamp = Px(BaseLamp), x = 0;
            foreach (var item in _items)
            {
                g.DrawImage(ApcTheme.GetImage("lamp_" + item.Status.ToString().ToLowerInvariant()), new Rectangle(x, (Height - lamp) / 2, lamp, lamp));
                x += lamp + Px(BaseLampGap);
                int w = TextRenderer.MeasureText(g, item.Text, font).Width;
                TextRenderer.DrawText(g, item.Text, font, new Rectangle(x, 0, w, Height), fore,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                x += w + Px(BaseItemGap);
            }
        }

        private void OnThemeChanged(object sender, EventArgs e) { Width = PreferredWidth; Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
