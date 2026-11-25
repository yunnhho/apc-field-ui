using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 전환 버튼 줄(설비군·라인 전환 등). 탭 하나하나가 보통 버튼(btn_normal)이고, 선택된 것은 눌린 버튼 + 굵은 글자 + 아래 남색 막대.
    /// 실행 버튼(남색 Primary)과 겹치지 않게 선택도 Primary 색을 쓰지 않는다. 본문 맨 위 줄에 둔다(창 제목 줄·헤더 띠가 아니다).
    /// 버튼 폭은 글자에 맞춰 자동(잘리지 않음), 버튼 사이 4px. 내용 전환은 <see cref="SelectedIndexChanged"/>에서 화면이 한다.
    /// 클릭 또는 포커스 상태에서 ←/→로 바꾼다. 높이는 테마 입력칸 높이.
    /// </summary>
    public class ApcTabStrip : Control
    {
        private const int BaseTabPad = 20, BaseMinTab = 120, BaseAccent = 3, BaseHeight = 40, BaseGap = 4;
        private readonly List<string> _tabs = new List<string>();
        private int _selected = -1;
        private int _hover = -1;

        public ApcTabStrip()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = ApcTheme.Px(BaseHeight);
            ApcTheme.Changed += OnThemeChanged;
        }

        /// <summary>선택이 바뀌면(클릭·←/→·SelectedIndex 대입) 발생.</summary>
        public event EventHandler SelectedIndexChanged;

        /// <summary>탭 글자 목록을 한 번에 정한다. 탭이 있으면 첫 탭이 선택된다(이벤트 발생).</summary>
        public void SetTabs(params string[] tabs)
        {
            if (tabs == null) throw new ArgumentNullException("tabs");
            _tabs.Clear();
            _tabs.AddRange(tabs);
            _selected = -1;
            Invalidate();
            if (_tabs.Count > 0) SelectedIndex = 0;
        }

        public int TabCount { get { return _tabs.Count; } }

        public string GetTabText(int index) { return _tabs[index]; }

        /// <summary>선택된 탭 번호(없으면 -1). 범위 밖이면 ArgumentOutOfRangeException.</summary>
        public int SelectedIndex
        {
            get { return _selected; }
            set
            {
                if (value < -1 || value >= _tabs.Count) throw new ArgumentOutOfRangeException("value");
                if (value == _selected) return;
                _selected = value;
                Invalidate();
                var h = SelectedIndexChanged;
                if (h != null) h(this, EventArgs.Empty);
            }
        }

        /// <summary>모든 탭을 담는 데 필요한 폭(배치용). 탭 폭 합.</summary>
        public int PreferredTabsWidth
        {
            get { int w = 0; for (int i = 0; i < _tabs.Count; i++) w += TabWidth(i) + (i > 0 ? ApcTheme.Px(BaseGap) : 0); return w; }
        }

        internal Rectangle TabBounds(int index)
        {
            int x = 0;
            for (int i = 0; i < index; i++) x += TabWidth(i) + ApcTheme.Px(BaseGap);
            return new Rectangle(x, 0, TabWidth(index), Height);
        }

        private int TabWidth(int index)
        {
            int text = TextRenderer.MeasureText(_tabs[index], BoldFont()).Width;
            return Math.Max(ApcTheme.Px(BaseMinTab), text + 2 * ApcTheme.Px(BaseTabPad));
        }

        private int HitTest(Point p)
        {
            for (int i = 0; i < _tabs.Count; i++) if (TabBounds(i).Contains(p)) return i;
            return -1;
        }

        private static Font BoldFont() { return ApcTheme.Current == null ? SystemFonts.DefaultFont : ApcTheme.GetFont(FontStyle.Bold); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (ApcTheme.Current == null) { g.Clear(SystemColors.Control); return; }
            for (int i = 0; i < _tabs.Count; i++) DrawTab(g, i, ApcTheme.Current.Colors);
        }

        private void DrawTab(Graphics g, int i, ThemeColors c)
        {
            var r = TabBounds(i);
            bool selected = i == _selected;
            string state = !Enabled ? "disabled" : selected ? "pressed" : i == _hover ? "hover" : "normal";
            var img = ApcTheme.GetImage("btn_normal_" + state);
            var sl = ApcTheme.Current.ButtonSlice;
            NineSlice.Draw(g, img, r, new Padding(sl[0], sl[1], sl[2], sl[3]), ApcTheme.ImageScale(img));
            if (selected && Enabled)
            {
                int a = ApcTheme.Px(BaseAccent), inset = ApcTheme.Px(6);
                using (var accent = new SolidBrush(ApcTheme.C(c.Header[1]))) g.FillRectangle(accent, r.X + inset, r.Bottom - inset - a + ApcTheme.Px(2), r.Width - 2 * inset, a);
            }

            var font = selected ? BoldFont() : ApcTheme.GetFont();
            var color = !Enabled ? ApcTheme.C(c.TextMuted) : selected ? ApcTheme.C(c.Text) : ApcTheme.C(c.TextMuted);
            var tr = r;
            if (selected) tr.Offset(0, 1);
            TextRenderer.DrawText(g, _tabs[i], font, tr, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            if (selected && Focused && ShowFocusCues)
            {
                var f = Rectangle.Inflate(r, -3, -3);
                using (var pen = new Pen(FieldFrame.FocusColor)) g.DrawRectangle(pen, f.X, f.Y, f.Width - 1, f.Height - 1);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            int i = HitTest(e.Location);
            if (i >= 0) SelectedIndex = i;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int i = HitTest(e.Location);
            if (i != _hover) { _hover = i; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = -1; Invalidate(); }

        protected override bool IsInputKey(Keys keyData)
        {
            return keyData == Keys.Left || keyData == Keys.Right || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_tabs.Count == 0) return;
            if (e.KeyCode == Keys.Left && _selected > 0) SelectedIndex = _selected - 1;
            else if (e.KeyCode == Keys.Right && _selected < _tabs.Count - 1) SelectedIndex = _selected + 1;
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

        private void OnThemeChanged(object sender, EventArgs e) { Height = ApcTheme.Px(BaseHeight); Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
