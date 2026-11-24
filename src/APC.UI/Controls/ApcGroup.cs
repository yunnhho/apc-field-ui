using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 제목이 있는 묶음 상자(설정 dialog의 블록 등, 표준 GroupBox 대신). 패널 바탕 + 왼쪽 먹색 막대가 붙은 굵은 제목.
    /// 안에 넣는 컨트롤은 Padding(제목 띠 아래 + 여백) 안쪽에 배치된다 — Dock이나 FlowLayoutPanel(Dock=Fill)을 쓰면 된다.
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

        /// <summary>제목 띠 글자.</summary>
        public override string Text { get { return base.Text; } set { base.Text = value; Invalidate(); } }

        internal int TitleHeight { get { return ApcTheme.Px(BaseTitleH); } }

        /// <summary>안 내용 높이(배율 적용 px)를 담는 묶음 높이 = 제목 띠 + 위아래 여백 + 내용. 화면에서 Columns() 줄을 담으면 내용 = 줄 높이 합.</summary>
        public int HeightFor(int contentHeight) { return Padding.Vertical + contentHeight; }

        /// <summary>바탕은 패널 시작색 단색: 안에 든 컨트롤이 BackColor를 물려받아 그라데이션과 어긋나지 않게.</summary>
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

            // 라벨: 왼쪽 3px 먹색 막대 + 굵은 글자(Steel + Ink)
            var font = ApcTheme.GetFont(FontStyle.Bold);
            int pad = ApcTheme.Px(BasePad), bar = ApcTheme.Px(BaseBar);
            int barH = Math.Min(TitleHeight - 2, TextRenderer.MeasureText(g, "가", font).Height);
            using (var ink = new SolidBrush(ApcTheme.C(c.Text))) g.FillRectangle(ink, pad, (TitleHeight - barH) / 2, bar, barH);
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
