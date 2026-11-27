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
    /// Borderless APCSOFT window. Title bar (logo, title, [settings], minimize, close image buttons), drag to move,
    /// double-click to maximize, edge resize. Maximizing does not cover the taskbar.
    /// </summary>
    public partial class ApcForm : Form
    {
        private const int BaseTitleHeight = 40;
        private const int BaseTitleHeightTwoLine = 54;
        private const int BaseTitleLinePad = 2;
        private const int BaseResizeGrip = 6;
        private const int BaseLogoHeight = 28;
        private const int BaseWinButtonW = 46;
        private const int BaseWinButtonH = 32;
        private const int BaseWinButtonRight = 12;   // 닫기 버튼과 창 오른쪽 끝 사이 여유

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
            _clock.Tick += (s, e) => { var r = ClockBounds; if (!r.IsEmpty) Invalidate(r); };
        }

        [DefaultValue("")]
        public string TitleText
        {
            get { return _titleText; }
            set { _titleText = value ?? string.Empty; Text = _titleText; Invalidate(TitleBarRect); }
        }

        private string _functionText = string.Empty;

        /// <summary>
        /// Function name on the line below the title (e.g. "작업 시작 · LOT 스캔"). Drawn in the brand color.
        /// Shown only on the main window (ShowLogo); when empty the title is one line at the default height.
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
        /// Settings (gear) button left of minimize in the title bar. Turn it on only for windows that have a settings screen. Clicking raises <see cref="SettingsClick"/>.
        /// In a screen (ApcTemplateView) do not set this directly; use UseSettingsButton.
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

        /// <summary>The title-bar settings button was clicked (only when <see cref="ShowSettings"/> is true).</summary>
        public event EventHandler SettingsClick;

        public int TitleBarHeight
        {
            get
            {
                if (!TwoLineTitle || ApcTheme.Current == null) return (int)Math.Round(BaseTitleHeight * ApcTheme.Scale);
                // 두 줄 제목: 기본 54px, 고객사 테마 글꼴이 크면 두 줄 높이 + 위아래 여백만큼 늘린다.
                int lines = TextRenderer.MeasureText("가", TitleFont()).Height + TextRenderer.MeasureText("가", FunctionFont()).Height;
                return Math.Max((int)Math.Round(BaseTitleHeightTwoLine * ApcTheme.Scale), lines + ApcTheme.Px(BaseTitleLinePad));
            }
        }

        /// <summary>Edge resize hit width (px).</summary>
        public int ResizeGrip { get { return (int)(BaseResizeGrip * ApcTheme.Scale); } }

        /// <summary>Content area without the title bar and 1px border (client coordinates) = inside Padding.</summary>
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
            int closeX = ClientSize.Width - (int)(BaseWinButtonRight * s) - w;
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
                var clock = ClockBounds;
                if (!clock.IsEmpty)
                {
                    TextRenderer.DrawText(g, ClockText, ApcTheme.GetFont(), clock, ApcTheme.C(c.Text),
                        TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.Right | TextFormatFlags.NoPadding);
                    right = clock.Left - ApcTheme.Px(16);
                }
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

        private const string ClockFormat = "yyyy-MM-dd HH:mm:ss";
        private readonly Timer _clock = new Timer { Interval = 1000 };

        internal string ClockText { get { return DateTime.Now.ToString(ClockFormat); } }

        /// <summary>Main-window clock position: left of the window buttons, vertically centered on the title line. Text width is measured from a fixed-format sample.</summary>
        internal Rectangle ClockBounds
        {
            get
            {
                if (!ShowLogo || ApcTheme.Current == null) return Rectangle.Empty;
                int w = TextRenderer.MeasureText("0000-00-00 00:00:00", ApcTheme.GetFont()).Width;
                int left = LeftmostButton().Left - ApcTheme.Px(16) - w;
                // 좁은 창: 시계가 로고·제목을 덮느니 생략한다(제목이 먼저)
                return left < ClientSize.Width / 2 ? Rectangle.Empty : new Rectangle(left, 0, w, TitleBarHeight);
            }
        }

        internal Rectangle SettingsButtonBounds { get { return ButtonRect(WinButton.Settings); } }
        internal Rectangle MinimizeButtonBounds { get { return ButtonRect(WinButton.Minimize); } }
        internal Rectangle CloseButtonBounds { get { return ButtonRect(WinButton.Close); } }

        private Rectangle LeftmostButton()
        {
            return ButtonRect(_showSettings ? WinButton.Settings : MinimizeBox ? WinButton.Minimize : WinButton.Close);
        }

        private const int BaseSeparatorHeight = 28;
        private static readonly Color SeparatorColor = Color.FromArgb(0xC4, 0xCC, 0xD5);

        /// <summary>Main window: logo │ program name (large), and below it a small brand-color line with the function name if set. Left aligned, ellipsized before the buttons.</summary>
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

        /// <summary>Function name font: theme font, bold, 1pt smaller.</summary>
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

        /// <summary>Main window title font: theme font, bold × 1.5.</summary>
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

        /// <summary>Logo on the left of the title bar + large centered title (main window). false = no logo, title on the left (dialog; SetupDialog turns it off).</summary>
        [DefaultValue(true)]
        public bool ShowLogo
        {
            get { return _showLogo; }
            set { _showLogo = value; _clock.Enabled = value && IsHandleCreated; UpdatePadding(); Invalidate(); }
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

        /// <summary>Stops the first field from getting focus with all its text selected in blue when the form opens (caret goes to the end).</summary>
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

        /// <summary>MaximizedBounds must be in that monitor's coordinates (secondary monitor support).</summary>
        private void UpdateMaximizedBounds()
        {
            var screen = IsHandleCreated ? Screen.FromHandle(Handle) : Screen.PrimaryScreen;
            var wa = screen.WorkingArea;
            MaximizedBounds = new Rectangle(wa.X - screen.Bounds.X, wa.Y - screen.Bounds.Y, wa.Width, wa.Height);
        }

        /// <summary>Content fills the inside of the 1px border. EdgeResizeFilter handles edge resize even over child controls.</summary>
        private void UpdatePadding()
        {
            Padding = new Padding(1, TitleBarHeight + 1, 1, 1);
        }

        /// <summary>HT code if a point in child-control coordinates is in the window's edge resize area, otherwise HTCLIENT.</summary>
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
            if (ShowLogo) _clock.Start();   // 메인 창만 제목 줄에 현재 시각
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_edgeFilter != null) { Application.RemoveMessageFilter(_edgeFilter); _edgeFilter = null; }
            _clock.Stop();
            base.OnHandleDestroyed(e);
        }

        private EdgeResizeFilter _edgeFilter;

        /// <summary>
        /// When a Dock=Fill child covers the edge, mouse messages go to the child. Of the mouse moves and presses this window's children receive,
        /// those inside the edge grip are intercepted to change the cursor and start a window resize (so content can fill edge to edge).
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
                _clock.Dispose();
                if (_titleFont != null) _titleFont.Dispose();
                if (_functionFont != null) _functionFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
