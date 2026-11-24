using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 입력 카드: 제목 띠(왼쪽 Title 굵게, 오른쪽 SubTitle 흐리게 — 규격·단위) + 이름표가 위에 붙은 입력칸을 위→아래로 + 맨 아래 상태 줄(램프 + Footer).
    /// 칸·슬롯마다 같은 항목을 넣는 dialog(LOT 스캔)나 짝 입력(하한·상한)을 카드 여러 장으로 나란히 둘 때 쓴다 — dialog에서는 AddContentRow로.
    /// 높이는 AddField로 넣은 칸 수로 정해진다(상태 줄 자리는 늘 있음 → 실행 중 Footer가 바뀌어도 높이 그대로).
    /// 카드 안 입력칸에 커서가 있으면 카드 테두리가 입력 중 칸과 같은 파란 2px → 지금 어느 칸(슬롯)을 넣는지 멀리서도 보인다.
    /// </summary>
    public class ApcFieldCard : Panel
    {
        private const int BaseTitleH = 36, BasePad = 12, BaseCaptionH = 22, BaseFieldGap = 10, BaseFooterH = 30, BaseLamp = 14;

        private readonly List<KeyValuePair<Label, Control>> _fields = new List<KeyValuePair<Label, Control>>();
        private string _subTitle = "", _footer = "";
        private StatusKind? _footerStatus;

        public ApcFieldCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            Width = ApcTheme.Px(240);
            Height = HeightFor(0);
            ApcTheme.Changed += OnThemeChanged;
        }

        /// <summary>제목 띠 왼쪽(칸 이름 B1, 항목 이름 하중).</summary>
        public override string Text { get { return base.Text; } set { base.Text = value; Invalidate(); } }

        /// <summary>제목 띠 오른쪽 흐린 글자(규격 Ø32 · 0.8μm, 단위 kN).</summary>
        [DefaultValue("")] public string SubTitle { get { return _subTitle; } set { _subTitle = value ?? ""; Invalidate(); } }

        /// <summary>맨 아래 상태 줄 글자(스캔 시각, 허용 범위 안내).</summary>
        [DefaultValue("")] public string Footer { get { return _footer; } set { _footer = value ?? ""; Invalidate(); } }

        /// <summary>상태 줄 왼쪽 램프. null이면 램프 없이 흐린 글자만(안내 문구용).</summary>
        [DefaultValue(null)] public StatusKind? FooterStatus { get { return _footerStatus; } set { _footerStatus = value; Invalidate(); } }

        /// <summary>이름표 + 입력칸 한 칸을 아래에 붙인다(입력칸은 카드 폭을 채움). 카드 높이가 늘어난다.</summary>
        public void AddField(string caption, Control field)
        {
            if (field == null) throw new ArgumentNullException("field");
            var label = new Label
            {
                Text = caption ?? "", AutoSize = false, TextAlign = ContentAlignment.BottomLeft, BackColor = Color.Transparent,
                Font = ApcTheme.GetFont(), ForeColor = ApcTheme.C(ApcTheme.Current.Colors.TextMuted),
            };
            field.Enter += OnFocusChanged;
            field.Leave += OnFocusChanged;
            Controls.Add(label);
            Controls.Add(field);
            _fields.Add(new KeyValuePair<Label, Control>(label, field));
            Height = HeightFor(_fields.Count);
            PerformLayout();
        }

        /// <summary>입력칸 n개를 담는 카드 높이(배율 적용 px).</summary>
        public static int HeightFor(int fieldCount)
        {
            int fieldH = ApcTheme.Px(ApcTextBox.BaseHeight);
            return ApcTheme.Px(BaseTitleH) + ApcTheme.Px(BasePad) + fieldCount * (ApcTheme.Px(BaseCaptionH) + fieldH)
                   + Math.Max(0, fieldCount - 1) * ApcTheme.Px(BaseFieldGap) + ApcTheme.Px(BasePad) + ApcTheme.Px(BaseFooterH);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            int pad = ApcTheme.Px(BasePad), capH = ApcTheme.Px(BaseCaptionH), fieldH = ApcTheme.Px(ApcTextBox.BaseHeight);
            int y = ApcTheme.Px(BaseTitleH) + pad, w = Width - 2 * pad;
            foreach (var f in _fields)
            {
                f.Key.SetBounds(pad, y, w, capH);
                f.Value.SetBounds(pad, y + capH, w, fieldH);
                y += capH + fieldH + ApcTheme.Px(BaseFieldGap);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            if (ApcTheme.Current == null) return;
            var c = ApcTheme.Current.Colors;
            int pad = ApcTheme.Px(BasePad), titleH = ApcTheme.Px(BaseTitleH), footerH = ApcTheme.Px(BaseFooterH);
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            var border = ApcTheme.C(c.Border);

            // 제목 띠: 그리드 헤더 그라데이션(묶음·값 카드와 같은 입체)
            Gradients.Fill(g, new Rectangle(1, 1, Width - 2, titleH - 1), c.GridHeader);
            using (var pen = new Pen(border)) g.DrawLine(pen, 0, titleH, Width, titleH);
            var font = ApcTheme.GetFont();
            int subW = _subTitle.Length == 0 ? 0 : TextRenderer.MeasureText(g, _subTitle, font).Width;
            var subRect = new Rectangle(Width - pad - subW, 0, subW, titleH);
            TextRenderer.DrawText(g, _subTitle, font, subRect, ApcTheme.C(c.TextMuted), flags);
            TextClip.Draw(g, this, Text, ApcTheme.GetFont(FontStyle.Bold), Rectangle.FromLTRB(pad, 0, subRect.Left - ApcTheme.Px(8), titleH), ApcTheme.C(c.Text), flags);

            // 상태 줄: 연한 바탕 + 위 구분선, 왼쪽 램프
            var foot = new Rectangle(1, Height - footerH, Width - 2, footerH - 1);
            using (var b = new SolidBrush(ApcTheme.C(c.GridLine))) g.FillRectangle(b, foot);
            using (var pen = new Pen(ApcTheme.C(c.GridHeader[1]))) g.DrawLine(pen, 0, foot.Top, Width, foot.Top);
            int tx = pad;
            if (_footerStatus.HasValue)
            {
                int lamp = ApcTheme.Px(BaseLamp);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(Cards.Lamp(_footerStatus.Value), new Rectangle(pad, foot.Top + (footerH - lamp) / 2, lamp, lamp));
                tx += lamp + ApcTheme.Px(6);
            }
            var footColor = _footerStatus.HasValue ? Cards.EmphasisColor(_footerStatus.Value) : ApcTheme.C(c.TextMuted);
            TextClip.Draw(g, this, _footer, font, Rectangle.FromLTRB(tx, foot.Top, Width - pad, foot.Bottom), footColor, flags);

            // 테두리: 안 입력칸에 커서가 있으면 입력 중 칸과 같은 파란 2px
            if (ContainsFocus)
                using (var pen = new Pen(FieldFrame.FocusColor))
                {
                    g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                    g.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
                }
            else
                using (var pen = new Pen(border)) g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        private void OnFocusChanged(object sender, EventArgs e) { Invalidate(); }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            foreach (var f in _fields) f.Key.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.TextMuted);
            Height = HeightFor(_fields.Count);
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
