using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 상태색 범례: 램프 + 뜻을 왼→오로("● 관리선 내  ● 관리선 이탈  ● SPEC 초과"). 표·그래프 칸의 램프 색이 무엇인지 화면에서 읽히게.
    /// 항목은 SetItems로 그 화면의 판정 문구대로 준다. Width는 항목에 맞춰 정해진다(구획 제목 줄 오른쪽에 Dock = Right).
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

        /// <summary>범례 항목(상태 + 뜻). 부르면 Width가 항목에 맞게 바뀐다.</summary>
        public void SetItems(params ApcLampValue[] items)
        {
            _items = items ?? new ApcLampValue[0];
            Width = PreferredWidth;
            Invalidate();
        }

        /// <summary>항목을 모두 그리는 폭(배율 적용 px).</summary>
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
