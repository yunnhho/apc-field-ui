using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 테두리 없는 APCSOFT 창. 타이틀바(로고·제목·[설정]·최소화·닫기 이미지 버튼), 드래그 이동,
    /// 더블클릭 최대화, 가장자리 리사이즈. 최대화해도 작업표시줄을 가리지 않는다.
    /// </summary>
    public partial class ApcForm : Form
    {
        private const int BaseTitleHeight = 40;
        private const int BaseTitleHeightTwoLine = 54;
        private const int BaseResizeGrip = 6;
        private const int BaseLogoHeight = 22;
        private const int BaseWinButtonW = 46;
        private const int BaseWinButtonH = 32;

        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 1, HTCAPTION = 2;
        private const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;
        private const int WS_MINIMIZEBOX = 0x20000, WS_MAXIMIZEBOX = 0x10000;

        private enum WinButton { None, Settings, Minimize, Close }

        private string _titleText = string.Empty;
        private WinButton _hover;
        private WinButton _pressed;

        public ApcForm()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FormBorderStyle = FormBorderStyle.None;
            Resizable = true;
            UpdatePadding();
            UpdateMaximizedBounds();
            ApcTheme.Changed += OnThemeChanged;
            ApplyThemeFont();
        }

        [DefaultValue("")]
        public string TitleText
        {
            get { return _titleText; }
            set { _titleText = value ?? string.Empty; Text = _titleText; Invalidate(TitleBarRect); }
        }

        private string _functionText = string.Empty;

        /// <summary>
        /// 제목 아랫줄의 기능명(예: "작업 시작 · LOT 스캔"). 브랜드 색으로 그린다.
        /// 메인 창(ShowLogo)에서만 보이고, 비어 있으면 한 줄 제목·기본 높이.
        /// </summary>
        [DefaultValue("")]
        public string FunctionText
        {
            get { return _functionText; }
            set { _functionText = value ?? string.Empty; UpdatePadding(); Invalidate(); }
        }

        private bool TwoLineTitle { get { return _showLogo && _functionText.Length > 0; } }

        private bool _resizable = true;

        [DefaultValue(true)]
        public bool Resizable
        {
            get { return _resizable; }
            set { _resizable = value; UpdatePadding(); }
        }

        private bool _showSettings;

        /// <summary>
        /// 타이틀바 최소화 왼쪽에 설정(톱니) 버튼. 설정 화면이 있는 창에서만 켠다. 누르면 <see cref="SettingsClick"/>.
        /// 화면(ApcTemplateView)에서는 직접 켜지 말고 UseSettingsButton을 쓴다.
        /// </summary>
        [DefaultValue(false)]
        public bool ShowSettings
        {
            get { return _showSettings; }
            set
            {
                // 그림이 없는 옛 고객사 테마면 그리는 순간이 아니라 켜는 순간 실패하게(파일 이름이 메시지에 나온다)
                if (value && ApcTheme.Current != null) ApcTheme.GetImage("win_settings_normal");
                _showSettings = value;
                Invalidate(TitleBarRect);
            }
        }

        /// <summary>타이틀바 설정 버튼을 눌렀다(<see cref="ShowSettings"/>가 true일 때만).</summary>
        public event EventHandler SettingsClick;

        public int TitleBarHeight { get { return (int)Math.Round((TwoLineTitle ? BaseTitleHeightTwoLine : BaseTitleHeight) * ApcTheme.Scale); } }

        /// <summary>가장자리 리사이즈 감지 폭(px).</summary>
        public int ResizeGrip { get { return (int)(BaseResizeGrip * ApcTheme.Scale); } }

        /// <summary>타이틀바·1px 테두리를 뺀 내용 영역(클라이언트 좌표) = Padding 안쪽.</summary>
        public Rectangle ContentBounds
        {
            get
            {
                var p = Padding;
                return Rectangle.FromLTRB(p.Left, p.Top, Math.Max(p.Left, ClientSize.Width - p.Right), Math.Max(p.Top, ClientSize.Height - p.Bottom));
            }
        }

        internal Rectangle MaximizedArea { get { return MaximizedBounds; } }

        private Rectangle TitleBarRect { get { return new Rectangle(0, 0, ClientSize.Width, TitleBarHeight); } }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style |= WS_MINIMIZEBOX | WS_MAXIMIZEBOX;   // 작업표시줄 최소화·더블클릭 최대화 허용
                return cp;
            }
        }

        private Rectangle ButtonRect(WinButton b)
        {
            float s = ApcTheme.Scale;
            int w = (int)(BaseWinButtonW * s), h = (int)(BaseWinButtonH * s), gap = (int)(4 * s);
            int y = (TitleBarHeight - h) / 2;
            int closeX = ClientSize.Width - gap - w;
            int minX = closeX - gap / 2 - w;
            return b == WinButton.Close ? new Rectangle(closeX, y, w, h)
                 : b == WinButton.Minimize ? new Rectangle(minX, y, w, h)
                 : b == WinButton.Settings ? new Rectangle((MinimizeBox ? minX : closeX) - gap / 2 - w, y, w, h)
                 : Rectangle.Empty;
        }

        private WinButton HitButton(Point p)
        {
            if (ButtonRect(WinButton.Close).Contains(p)) return WinButton.Close;
            if (MinimizeBox && ButtonRect(WinButton.Minimize).Contains(p)) return WinButton.Minimize;
            if (_showSettings && ButtonRect(WinButton.Settings).Contains(p)) return WinButton.Settings;
            return WinButton.None;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (ApcTheme.Current == null) { base.OnPaintBackground(e); return; }
            Gradients.Fill(e.Graphics, ClientRectangle, ApcTheme.Current.Colors.Background);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (ApcTheme.Current == null) return;
            var g = e.Graphics;
            var c = ApcTheme.Current.Colors;
            float s = ApcTheme.Scale;

            Gradients.Fill(g, TitleBarRect, c.TitleBar);
            using (var pen = new Pen(ApcTheme.C(c.Border)))
            {
                g.DrawLine(pen, 0, TitleBarHeight, ClientSize.Width, TitleBarHeight);
                g.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            }

            int x = (int)(10 * s);
            if (ShowLogo)
            {
                var logo = ApcTheme.GetImage(ApcTheme.Current.Logo);
                int lh = (int)(BaseLogoHeight * s);
                int lw = logo.Width * lh / logo.Height;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(logo, new Rectangle(x, (TitleBarHeight - lh) / 2, lw, lh));
                x += lw + (int)(12 * s);
                int sh = (int)(BaseSeparatorHeight * s);
                using (var sep = new Pen(SeparatorColor)) g.DrawLine(sep, x, (TitleBarHeight - sh) / 2, x, (TitleBarHeight + sh) / 2);
                x += (int)(12 * s);
            }

            int right = LeftmostButton().Left - (int)(8 * s);
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            if (ShowLogo)
            {
                DrawMainTitle(g, Rectangle.FromLTRB(x, 0, Math.Max(x, right), TitleBarHeight), flags);
            }
            else
            {
                TextClip.Draw(g, this, _titleText, ApcTheme.GetFont(FontStyle.Bold), Rectangle.FromLTRB(x, 0, right, TitleBarHeight), ApcTheme.C(c.Text), flags);
            }

            if (_showSettings) DrawWinButton(g, WinButton.Settings, "win_settings", true);
            if (MinimizeBox) DrawWinButton(g, WinButton.Minimize, "win_minimize", true);
            DrawWinButton(g, WinButton.Close, "win_close", true);
        }

        internal Rectangle SettingsButtonBounds { get { return ButtonRect(WinButton.Settings); } }
        internal Rectangle MinimizeButtonBounds { get { return ButtonRect(WinButton.Minimize); } }

        private Rectangle LeftmostButton()
        {
            return ButtonRect(_showSettings ? WinButton.Settings : MinimizeBox ? WinButton.Minimize : WinButton.Close);
        }

        private const int BaseSeparatorHeight = 28;
        private static readonly Color SeparatorColor = Color.FromArgb(0xC4, 0xCC, 0xD5);

        /// <summary>메인 창: 로고 │ 프로그램 이름(크게), 기능명이 있으면 그 아래 브랜드 색 작은 줄. 왼쪽 정렬, 버튼 앞에서 말줄임.</summary>
        private void DrawMainTitle(Graphics g, Rectangle area, TextFormatFlags flags)
        {
            var c = ApcTheme.Current.Colors;
            var font = TitleFont();
            if (!TwoLineTitle)
            {
                TextClip.Draw(g, this, _titleText, font, area, ApcTheme.C(c.Text), flags);
                return;
            }
            var sub = FunctionFont();
            int h1 = TextRenderer.MeasureText(g, "가", font).Height;
            int h2 = TextRenderer.MeasureText(g, "가", sub).Height;
            int top = area.Top + (area.Height - h1 - h2) / 2;
            TextClip.Draw(g, this, _titleText, font, new Rectangle(area.Left, top, area.Width, h1), ApcTheme.C(c.Text), flags);
            TextClip.Draw(g, this, _functionText, sub, new Rectangle(area.Left, top + h1, area.Width, h2), ApcTheme.C(c.Brand), flags);
        }

        private Font _functionFont;

        /// <summary>기능명 글꼴: 테마 글꼴 굵게, 1pt 작게.</summary>
        private Font FunctionFont()
        {
            var b = ApcTheme.GetFont(FontStyle.Bold);
            float size = Math.Max(1f, b.Size - 1f);
            if (_functionFont == null || _functionFont.Name != b.Name || _functionFont.Size != size)
            {
                if (_functionFont != null) _functionFont.Dispose();
                _functionFont = new Font(b.FontFamily, size, FontStyle.Bold);
            }
            return _functionFont;
        }

        private Font _titleFont;

        /// <summary>메인 창 제목 글꼴: 테마 글꼴 굵게 × 1.5.</summary>
        private Font TitleFont()
        {
            var b = ApcTheme.GetFont(FontStyle.Bold);
            float size = b.Size * 1.5f;
            if (_titleFont == null || _titleFont.Name != b.Name || _titleFont.Size != size)
            {
                if (_titleFont != null) _titleFont.Dispose();
                _titleFont = new Font(b.FontFamily, size, FontStyle.Bold);
            }
            return _titleFont;
        }

        private bool _showLogo = true;

        /// <summary>타이틀바 왼쪽 로고 + 가운데 큰 제목(메인 창). false면 로고 없이 왼쪽 제목(dialog, SetupDialog가 끈다).</summary>
        [DefaultValue(true)]
        public bool ShowLogo
        {
            get { return _showLogo; }
            set { _showLogo = value; UpdatePadding(); Invalidate(); }
        }


        private void DrawWinButton(Graphics g, WinButton b, string name, bool enabled)
        {
            string state = !enabled ? "disabled" : _pressed == b ? "pressed" : _hover == b ? "hover" : "normal";
            g.DrawImage(ApcTheme.GetImage(name + "_" + state), ButtonRect(b));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            SetHover(HitButton(e.Location));
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetHover(WinButton.None);
            _pressed = WinButton.None;
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _pressed = HitButton(e.Location); Invalidate(TitleBarRect); }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            var pressed = _pressed;
            _pressed = WinButton.None;
            Invalidate(TitleBarRect);
            if (pressed != WinButton.None && pressed == HitButton(e.Location))
            {
                if (pressed == WinButton.Close) Close();
                else if (pressed == WinButton.Minimize) WindowState = FormWindowState.Minimized;
                else { var handler = SettingsClick; if (handler != null) handler(this, EventArgs.Empty); }
            }
            base.OnMouseUp(e);
        }

        private void SetHover(WinButton b)
        {
            if (_hover == b) return;
            _hover = b;
            Invalidate(TitleBarRect);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg != WM_NCHITTEST || (int)m.Result != HTCLIENT) return;

            var p = PointToClient(new Point(unchecked((short)(long)m.LParam), unchecked((short)((long)m.LParam >> 16))));
            int hit = HitTest(p);
            if (hit != HTCLIENT) m.Result = (IntPtr)hit;
        }

        private int HitTest(Point p)
        {
            if (Resizable && WindowState == FormWindowState.Normal)
            {
                int grip = ResizeGrip;
                bool l = p.X < grip, r = p.X >= ClientSize.Width - grip;
                bool t = p.Y < grip, b = p.Y >= ClientSize.Height - grip;
                if (t && l) return HTTOPLEFT;
                if (t && r) return HTTOPRIGHT;
                if (b && l) return HTBOTTOMLEFT;
                if (b && r) return HTBOTTOMRIGHT;
                if (l) return HTLEFT;
                if (r) return HTRIGHT;
                if (t) return HTTOP;
                if (b) return HTBOTTOM;
            }
            if (p.Y < TitleBarHeight && HitButton(p) == WinButton.None) return HTCAPTION;
            return HTCLIENT;
        }

        /// <summary>폼이 뜰 때 첫 입력칸이 포커스를 받으며 글자 전체가 파랗게 선택되는 것을 막는다(커서는 끝으로).</summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // 숫자칸·날짜칸은 안쪽 입력칸이 포커스를 받는다 → 컨테이너의 ActiveControl을 따라 내려간다.
            Control active = ActiveControl;
            while (active is ContainerControl inner && inner.ActiveControl != null) active = inner.ActiveControl;
            var box = active as TextBoxBase;
            if (box != null && box.SelectionLength == box.TextLength) box.Select(box.TextLength, 0);
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            UpdateMaximizedBounds();
            base.OnLocationChanged(e);
        }

        /// <summary>MaximizedBounds는 해당 모니터 기준 좌표여야 한다(보조 모니터 대응).</summary>
        private void UpdateMaximizedBounds()
        {
            var screen = IsHandleCreated ? Screen.FromHandle(Handle) : Screen.PrimaryScreen;
            var wa = screen.WorkingArea;
            MaximizedBounds = new Rectangle(wa.X - screen.Bounds.X, wa.Y - screen.Bounds.Y, wa.Width, wa.Height);
        }

        /// <summary>내용은 1px 테두리 안을 꽉 채운다. 가장자리 리사이즈는 자식 위에서도 EdgeResizeFilter가 처리한다.</summary>
        private void UpdatePadding()
        {
            Padding = new Padding(1, TitleBarHeight + 1, 1, 1);
        }

        /// <summary>자식 컨트롤 좌표의 점이 창 가장자리 리사이즈 영역이면 HT 코드, 아니면 HTCLIENT.</summary>
        internal int EdgeHit(Control child, Point childPoint)
        {
            if (!Resizable || WindowState != FormWindowState.Normal) return HTCLIENT;
            int hit = HitTest(PointToClient(child.PointToScreen(childPoint)));
            return hit == HTCAPTION ? HTCLIENT : hit;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_edgeFilter == null) { _edgeFilter = new EdgeResizeFilter(this); Application.AddMessageFilter(_edgeFilter); }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_edgeFilter != null) { Application.RemoveMessageFilter(_edgeFilter); _edgeFilter = null; }
            base.OnHandleDestroyed(e);
        }

        private EdgeResizeFilter _edgeFilter;

        /// <summary>
        /// Dock=Fill 자식이 가장자리를 덮으면 마우스 메시지가 자식에게 간다. 이 창의 자식이 받는 마우스 이동·누름 중
        /// 가장자리 그립 안의 것을 가로채 커서를 바꾸고 창 리사이즈를 시작한다(여백 없이 꽉 채우기 위해).
        /// </summary>
        private sealed class EdgeResizeFilter : IMessageFilter
        {
            private const int WM_MOUSEMOVE = 0x200, WM_LBUTTONDOWN = 0x201, WM_NCLBUTTONDOWN = 0xA1;
            private readonly ApcForm _form;

            internal EdgeResizeFilter(ApcForm form) { _form = form; }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WM_MOUSEMOVE && m.Msg != WM_LBUTTONDOWN) return false;
                if (!_form.IsHandleCreated || m.HWnd == _form.Handle) return false;   // 창 자체는 WM_NCHITTEST로 처리
                var child = Control.FromChildHandle(m.HWnd);
                if (child == null || child.FindForm() != _form) return false;
                var p = new Point(unchecked((short)(long)m.LParam), unchecked((short)((long)m.LParam >> 16)));
                int hit = _form.EdgeHit(child, p);
                if (hit == HTCLIENT) return false;
                if (m.Msg == WM_MOUSEMOVE) { Cursor.Current = CursorFor(hit); return false; }
                ReleaseCapture();
                SendMessage(_form.Handle, WM_NCLBUTTONDOWN, (IntPtr)hit, IntPtr.Zero);
                return true;
            }

            private static Cursor CursorFor(int hit)
            {
                if (hit == HTLEFT || hit == HTRIGHT) return Cursors.SizeWE;
                if (hit == HTTOP || hit == HTBOTTOM) return Cursors.SizeNS;
                if (hit == HTTOPLEFT || hit == HTBOTTOMRIGHT) return Cursors.SizeNWSE;
                return Cursors.SizeNESW;
            }

            [DllImport("user32.dll")] private static extern bool ReleaseCapture();
            [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        }

        private void ApplyThemeFont()
        {
            if (ApcTheme.Current != null) Font = ApcTheme.GetFont();
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            ApplyThemeFont();
            UpdatePadding();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ApcTheme.Changed -= OnThemeChanged;
                if (_titleFont != null) _titleFont.Dispose();
                if (_functionFont != null) _functionFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
