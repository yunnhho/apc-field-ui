using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 테마 입력칸. 오목한 테두리·포커스 테두리는 비클라이언트 영역에 직접 그리고, 글자는 높이 가운데에 온다.
    /// 높이를 자유롭게 줄 수 있다(기본 34px×배율, 최소 28px×배율). TextBox를 상속하므로 Text·ReadOnly·MaxLength 등은 그대로.
    /// Multiline이면 스크롤바 자리가 필요해 Windows 기본 한 줄 테두리로 돌아간다.
    /// </summary>
    public class ApcTextBox : TextBox
    {
        public const int BaseHeight = 34;
        private const int BasePadX = 6;

        private const int WM_NCCALCSIZE = 0x0083;
        private const int WM_NCHITTEST = 0x0084;
        private const int HTCLIENT = 1;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_PRINT = 0x0317;
        private const int PRF_NONCLIENT = 0x0002;
        private const uint RDW_INVALIDATE = 0x0001, RDW_FRAME = 0x0400;
        private const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010, SWP_FRAMECHANGED = 0x0020;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")] private static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("user32.dll")] private static extern bool RedrawWindow(IntPtr hWnd, IntPtr rect, IntPtr rgn, uint flags);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        public ApcTextBox()
        {
            BorderStyle = BorderStyle.None;
            AutoSize = false;
            ApcTheme.Changed += OnThemeChanged;
            ApplyTheme();
            Height = (int)Math.Round(BaseHeight * ApcTheme.Scale);
        }

        /// <summary>테두리 없이 바탕만(숫자칸·날짜칸처럼 바깥 컨트롤이 테두리를 그릴 때).</summary>
        internal bool Borderless { get; set; }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            base.SetBoundsCore(x, y, width, Multiline || Borderless ? height : Math.Max(height, ApcTheme.MinHeight), specified);
        }

        /// <summary>창 크기 h일 때 테두리+여백 두께. 한 줄이면 글자 줄이 세로 가운데에 오도록 위아래를 나눈다.</summary>
        internal Padding FrameInsets(int windowHeight)
        {
            int padX = (int)Math.Round(BasePadX * ApcTheme.Scale) + 1;
            int line = Font.Height;
            int top = Math.Max(2, (windowHeight - line) / 2);
            int bottom = Math.Max(2, windowHeight - line - top);
            return new Padding(padX, top, padX, bottom);
        }

        /// <summary>바탕색을 상태(사용 가능·읽기 전용)에 맞춘다. 스캔 플래시 등으로 바꾼 색을 되돌릴 때도 쓴다.</summary>
        protected void ResetFieldColor()
        {
            BackColor = FieldFrame.Fill(Enabled, LooksReadOnly, Focused);
        }

        /// <summary>읽기 전용 모양(회색 바탕)으로 그릴지. 스캔 전용 칸은 ReadOnly여도 입력칸 모양이다.</summary>
        internal virtual bool LooksReadOnly { get { return ReadOnly; } }

        protected override void WndProc(ref Message m)
        {
            if (Multiline) { base.WndProc(ref m); return; }
            switch (m.Msg)
            {
                case WM_NCHITTEST:   // 여백(비클라이언트)을 눌러도 입력칸이 눌린 것으로 (터치·장갑 입력)
                    m.Result = (IntPtr)HTCLIENT;
                    return;
                case WM_NCCALCSIZE:
                    // wParam이 0이든 아니든 lParam 맨 앞은 창 사각형 → 클라이언트 사각형으로 줄여 돌려준다.
                    var r = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));
                    var ins = FrameInsets(r.Bottom - r.Top);
                    r.Left += ins.Left; r.Top += ins.Top; r.Right -= ins.Right; r.Bottom -= ins.Bottom;
                    if (r.Right < r.Left) r.Right = r.Left;
                    if (r.Bottom < r.Top) r.Bottom = r.Top;
                    Marshal.StructureToPtr(r, m.LParam, false);
                    m.Result = IntPtr.Zero;
                    return;
                case WM_NCPAINT:
                    var hdc = GetWindowDC(Handle);
                    try { using (var g = Graphics.FromHdc(hdc)) PaintFrame(g); }
                    finally { ReleaseDC(Handle, hdc); }
                    m.Result = IntPtr.Zero;
                    return;
                case WM_PRINT:   // DrawToBitmap(render 도구) 경로: 기본 처리 후 테두리를 덧그린다
                    base.WndProc(ref m);
                    if ((m.LParam.ToInt64() & PRF_NONCLIENT) != 0)
                        using (var g = Graphics.FromHdc(m.WParam)) PaintFrame(g);
                    return;
            }
            base.WndProc(ref m);
        }

        private void PaintFrame(Graphics g)
        {
            var win = new Rectangle(0, 0, Width, Height);
            var ins = FrameInsets(Height);
            var client = Rectangle.FromLTRB(ins.Left, ins.Top, Width - ins.Right, Height - ins.Bottom);
            using (var region = new Region(win))
            using (var brush = new SolidBrush(BackColor))
            {
                region.Exclude(client);
                g.FillRegion(brush, region);
            }
            if (!Borderless) FieldFrame.DrawBorder(g, win, Focused && !LooksReadOnly);
        }

        private void RedrawFrame()
        {
            if (IsHandleCreated) RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE);
        }

        private void RecalcFrame()
        {
            if (IsHandleCreated)
                SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }

        // 바탕색이 바뀌면 OnBackColorChanged가 테두리도 다시 그린다
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); ResetFieldColor(); RedrawFrame(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); ResetFieldColor(); RedrawFrame(); }
        protected override void OnBackColorChanged(EventArgs e) { base.OnBackColorChanged(e); RedrawFrame(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); RecalcFrame(); }

        protected override void OnMultilineChanged(EventArgs e)
        {
            BorderStyle = Multiline ? BorderStyle.FixedSingle : BorderStyle.None;
            base.OnMultilineChanged(e);
            RecalcFrame();
        }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); ResetFieldColor(); }
        protected override void OnReadOnlyChanged(EventArgs e) { base.OnReadOnlyChanged(e); ResetFieldColor(); }

        private void ApplyTheme()
        {
            ResetFieldColor();
            if (ApcTheme.Current == null) return;
            Font = ApcTheme.GetFont();
            ForeColor = FieldFrame.TextColor(true);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            ApplyTheme();
            Height = Math.Max(Height, ApcTheme.MinHeight);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
