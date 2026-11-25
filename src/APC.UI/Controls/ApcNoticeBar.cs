using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 지속 상태 알림 띠(한 줄): 인터락 우회 중·시험 모드·수동 운전처럼 판정은 아니지만 계속 보여야 하는 상태.
    /// 연한 상태색 바탕 + 왼쪽 상태색 막대 + 램프 + 굵은 글자(Text) + 보통 글자(Detail). 높이는 입력칸 정도로 낮다 —
    /// 화면의 큰 면(ApcJudgePanel)은 하나만 두고, 이런 상태는 이 띠로 둬서 판정판과 크기를 다투지 않게 한다.
    /// </summary>
    public class ApcNoticeBar : Control
    {
        private const int BaseHeight = 44, BaseStripe = 6, BaseLamp = 22, BasePad = 14;
        private const float TintAmount = 0.18f;   // 바탕 = 패널색 82% + 상태색 18%

        private StatusKind _status = StatusKind.Warn;
        private string _detail = "";

        public ApcNoticeBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Top;
            Height = S(BaseHeight);
            ApcTheme.Changed += OnThemeChanged;
        }

        [DefaultValue(StatusKind.Warn)] public StatusKind Status { get { return _status; } set { _status = value; Invalidate(); } }

        /// <summary>굵은 글자 뒤 보통 글자(예: "15:52까지 · 생산분은 추가 검사").</summary>
        [DefaultValue("")] public string Detail { get { return _detail; } set { _detail = value ?? ""; Invalidate(); } }

        /// <summary>굵은 글자(예: "인터락 E05 우회 중").</summary>
        public override string Text { get { return base.Text; } set { base.Text = value; } }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

        internal static Color Tint(Color panel, Color status)
        {
            return Color.FromArgb(
                (int)(panel.R + (status.R - panel.R) * TintAmount),
                (int)(panel.G + (status.G - panel.G) * TintAmount),
                (int)(panel.B + (status.B - panel.B) * TintAmount));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (ApcTheme.Current == null) { g.Clear(SystemColors.Control); return; }
            var c = ApcTheme.Current.Colors;
            var status = ApcTheme.Status(_status);
            using (var bg = new SolidBrush(Tint(ApcTheme.C(c.Panel[0]), status))) g.FillRectangle(bg, ClientRectangle);
            using (var stripe = new SolidBrush(status)) g.FillRectangle(stripe, 0, 0, S(BaseStripe), Height);
            using (var pen = new Pen(ApcTheme.C(c.Border))) g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            int lamp = Math.Max(0, Math.Min(S(BaseLamp), Height - S(8)));
            int x = S(BaseStripe) + S(BasePad);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(Cards.Lamp(_status), new Rectangle(x, (Height - lamp) / 2, lamp, lamp));
            x += lamp + S(10);

            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
            var bold = ApcTheme.GetFont(FontStyle.Bold);
            int right = Width - S(BasePad);
            TextClip.Draw(g, this, Text, bold, Rectangle.FromLTRB(x, 0, right, Height), ApcTheme.C(c.Text), flags);
            if (_detail.Length == 0) return;
            x += TextRenderer.MeasureText(g, Text, bold, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width + S(16);
            if (x < right) TextClip.Draw(g, this, _detail, ApcTheme.GetFont(), Rectangle.FromLTRB(x, 0, right, Height), ApcTheme.C(c.Text), flags);
        }

        // 배율이 바뀌면 같은 비율로(화면 코드가 정한 높이는 유지)
        private float _scale = ApcTheme.Scale;

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (ApcTheme.Scale != _scale) { Height = (int)Math.Round(Height * ApcTheme.Scale / _scale); _scale = ApcTheme.Scale; }
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private static int S(int px) { return (int)Math.Round(px * ApcTheme.Scale); }
    }
}
