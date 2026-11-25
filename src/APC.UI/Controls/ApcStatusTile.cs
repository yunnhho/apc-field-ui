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
    /// 설비 타일: 램프 + 설비명(부제 없음 — 상태는 램프 색, 사유는 화면의 배너·표가 말한다). 누르면 Click(상세 dialog 열기 등). 마우스를 올리거나 포커스면 파란 테두리,
    /// 알람이면 굵은 알람색 테두리. Tab으로 옮겨 Enter·Space로도 누른다.
    /// </summary>
    public class ApcStatusTile : Control
    {
        private StatusKind _status = StatusKind.Stop;
        private bool _hover;
        private bool _keyPressed;   // 키를 누르고 있어도(자동 반복) Click은 한 번

        public ApcStatusTile()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            Size = new Size(S(280), S(96));
            Margin = new Padding(0, 0, S(12), S(12));
            ApcTheme.Changed += OnThemeChanged;
        }

        [DefaultValue(StatusKind.Stop)] public StatusKind Status { get { return _status; } set { _status = value; Invalidate(); } }

        private bool _selected;
        /// <summary>선택 표시(바탕이 표 선택 행 색). 타일을 눌러 아래 카드·그래프가 그 설비를 보여 줄 때 그 타일 하나만 true.</summary>
        [DefaultValue(false)] public bool Selected { get { return _selected; } set { _selected = value; Invalidate(); } }

        /// <summary>설비명(굵게, 램프 오른쪽 세로 가운데).</summary>
        public override string Text { get { return base.Text; } set { base.Text = value; } }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (ApcTheme.Current == null) { g.Clear(SystemColors.Control); return; }
            var c = ApcTheme.Current.Colors;
            Cards.DrawFrame(g, Size, _status, _hover || Focused, _selected);

            int pad = S(14), lamp = Math.Min(S(32), Height - 2 * pad);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(Cards.Lamp(_status), new Rectangle(pad, (Height - lamp) / 2, lamp, lamp));

            int x = pad * 2 + lamp, w = Width - x - pad;
            var flags = TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
            TextClip.Draw(g, this, Text, ApcTheme.GetFont(FontStyle.Bold), new Rectangle(x, 0, w, Height), ApcTheme.C(c.Text), flags);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnMouseDown(MouseEventArgs e) { if (CanFocus) Focus(); base.OnMouseDown(e); }

        protected override bool IsInputKey(Keys keyData)
        {
            return keyData == Keys.Enter || keyData == Keys.Space || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) { _keyPressed = true; e.Handled = true; }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (!_keyPressed || (e.KeyCode != Keys.Enter && e.KeyCode != Keys.Space)) return;
            _keyPressed = false;
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }

        private void OnThemeChanged(object sender, EventArgs e) { Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private static int S(int px) { return (int)Math.Round(px * ApcTheme.Scale); }
    }
}
