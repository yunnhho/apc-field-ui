using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 화면 상단 띠: 오른쪽 끝에 현재 시각(1초마다 갱신)만 그린다. 왼쪽은 화면이 탭 등을 넣는 자리(ApcTemplateView.HeaderTools).
    /// 프로그램 이름은 창 제목 줄에, 상태는 화면 본문(타일·램프·알람 띠)에 있으므로 띠에는 그리지 않는다.
    /// Title·Status·StatusText는 기존 화면 호환을 위해 값만 보관한다.
    /// </summary>
    public class ApcHeaderBar : Panel
    {
        private const int BaseHeight = 40;
        private const int BasePadding = 12;
        private const string ClockFormat = "yyyy-MM-dd HH:mm:ss";

        private string _title = string.Empty;
        private string _statusText = string.Empty;
        private StatusKind _status = StatusKind.Stop;
        private readonly Timer _clock = new Timer { Interval = 1000 };

        public ApcHeaderBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Top;
            Height = (int)Math.Round(BaseHeight * ApcTheme.Scale);
            ApcTheme.Changed += OnThemeChanged;
            _clock.Tick += (s, e) => Invalidate(ClockBounds);
        }

        /// <summary>화면 이름(창 제목과 같은 값). 띠에는 그리지 않는다.</summary>
        [DefaultValue("")]
        public string Title { get { return _title; } set { _title = value ?? string.Empty; } }

        /// <summary>상태 문구(보관만, 그리지 않음).</summary>
        [DefaultValue("")]
        public string StatusText { get { return _statusText; } set { _statusText = value ?? string.Empty; } }

        /// <summary>상태(보관만, 그리지 않음).</summary>
        [DefaultValue(StatusKind.Stop)]
        public StatusKind Status { get { return _status; } set { _status = value; } }

        internal string ClockText { get { return DateTime.Now.ToString(ClockFormat); } }

        /// <summary>시각 영역(오른쪽 끝). 글자 폭은 고정 형식이라 고정 견본으로 잰다.</summary>
        internal Rectangle ClockBounds
        {
            get
            {
                int w = TextRenderer.MeasureText("0000-00-00 00:00:00", ApcTheme.GetFont()).Width;
                return new Rectangle(Width - ApcTheme.Px(BasePadding) - w, 0, w, Height);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _clock.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (ApcTheme.Current == null) { g.Clear(Color.SlateGray); return; }

            var c = ApcTheme.Current.Colors;
            Gradients.Fill(g, ClientRectangle, c.TitleBar);
            using (var pen = new Pen(ApcTheme.C(c.Border))) g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            var font = ApcTheme.GetFont();
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

            var clock = ClockBounds;
            TextRenderer.DrawText(g, ClockText, font, clock, ApcTheme.C(c.Text), flags | TextFormatFlags.Right);
        }

        private void OnThemeChanged(object sender, EventArgs e) { Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ApcTheme.Changed -= OnThemeChanged;
                _clock.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
