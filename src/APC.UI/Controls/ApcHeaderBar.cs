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
    /// 화면 상단 띠: 오른쪽에 상태 램프 + 상태 문구, 그 오른쪽 끝에 현재 시각(1초마다 갱신).
    /// 프로그램 이름은 창 제목 줄에 있으므로 띠에는 그리지 않는다(Title은 창 제목과 같은 이름을 보관만 한다).
    /// </summary>
    public class ApcHeaderBar : Panel
    {
        private const int BaseHeight = 40;
        private const int BasePadding = 12;
        private const int BaseLamp = 18;
        private const int BaseClockGap = 20;
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

        [DefaultValue("")]
        public string StatusText { get { return _statusText; } set { _statusText = value ?? string.Empty; Invalidate(); } }

        [DefaultValue(StatusKind.Stop)]
        public StatusKind Status { get { return _status; } set { _status = value; Invalidate(); } }

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

        /// <summary>램프 + 상태 문구 영역(시각 왼쪽).</summary>
        internal Rectangle StatusBounds
        {
            get
            {
                int textW = TextRenderer.MeasureText(_statusText, ApcTheme.GetFont()).Width;
                int lamp = ApcTheme.Px(BaseLamp);
                int right = ClockBounds.Left - ApcTheme.Px(BaseClockGap);
                int left = right - textW - ApcTheme.Px(6) - lamp;
                return Rectangle.FromLTRB(left, 0, right, Height);
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
            int sepX = clock.Left - ApcTheme.Px(BaseClockGap) / 2;
            using (var sep = new Pen(ApcTheme.C(c.Border))) g.DrawLine(sep, sepX, Height / 4, sepX, Height * 3 / 4);

            var status = StatusBounds;
            int lamp = ApcTheme.Px(BaseLamp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(ApcTheme.GetImage("lamp_" + _status.ToString().ToLowerInvariant()), new Rectangle(status.Left, (Height - lamp) / 2, lamp, lamp));
            var textRect = Rectangle.FromLTRB(status.Left + lamp + ApcTheme.Px(6), 0, status.Right, Height);
            TextRenderer.DrawText(g, _statusText, font, textRect, ApcTheme.C(c.Text), flags);
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
