using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>화면 상단 띠: 왼쪽 제목(설비명 등), 오른쪽 상태 램프 + 상태 문구.</summary>
    public class ApcHeaderBar : Panel
    {
        private const int BaseHeight = 40;
        private const int BasePadding = 12;
        private const int BaseLamp = 18;

        private string _title = string.Empty;
        private string _statusText = string.Empty;
        private StatusKind _status = StatusKind.Stop;

        public ApcHeaderBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Top;
            Height = (int)Math.Round(BaseHeight * ApcTheme.Scale);
            ApcTheme.Changed += OnThemeChanged;
        }

        [DefaultValue("")]
        public string Title { get { return _title; } set { _title = value ?? string.Empty; Invalidate(); } }

        [DefaultValue("")]
        public string StatusText { get { return _statusText; } set { _statusText = value ?? string.Empty; Invalidate(); } }

        [DefaultValue(StatusKind.Stop)]
        public StatusKind Status { get { return _status; } set { _status = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (ApcTheme.Current == null) { g.Clear(Color.SlateGray); return; }

            var c = ApcTheme.Current.Colors;
            Gradients.Fill(g, ClientRectangle, c.TitleBar);
            using (var pen = new Pen(ApcTheme.C(c.Border))) g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            float s = ApcTheme.Scale;
            int pad = (int)(BasePadding * s);
            var fore = ApcTheme.C(c.Text);
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;

            // 오른쪽부터: 상태 문구 → 램프
            var statusFont = ApcTheme.GetFont();
            int textW = TextRenderer.MeasureText(g, _statusText, statusFont).Width;
            var textRect = new Rectangle(Width - pad - textW, 0, textW, Height);
            TextRenderer.DrawText(g, _statusText, statusFont, textRect, fore, flags);

            int lamp = (int)(BaseLamp * s);
            var lampRect = new Rectangle(textRect.Left - (int)(6 * s) - lamp, (Height - lamp) / 2, lamp, lamp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(ApcTheme.GetImage("lamp_" + _status.ToString().ToLowerInvariant()), lampRect);

            var titleRect = Rectangle.FromLTRB(pad, 0, lampRect.Left - pad, Height);
            TextClip.Draw(g, this, _title, ApcTheme.GetFont(FontStyle.Bold), titleRect, fore, flags);
        }

        private void OnThemeChanged(object sender, EventArgs e) { Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
