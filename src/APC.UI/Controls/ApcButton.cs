using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    public enum ApcButtonVariant
    {
        Normal,
        Primary,
        Danger
    }

    /// <summary>9-slice image button. Images: btn_{variant}_{normal|hover|pressed|disabled}</summary>
    public class ApcButton : Button
    {

        private ApcButtonVariant _variant;
        private bool _hover;
        private bool _pressed;

        public ApcButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            ApcTheme.Changed += OnThemeChanged;
            ApplyThemeFont();
        }

        [DefaultValue(ApcButtonVariant.Normal)]
        public ApcButtonVariant Variant
        {
            get { return _variant; }
            set { _variant = value; Invalidate(); }
        }

        internal string StateName
        {
            get
            {
                if (!Enabled) return "disabled";
                if (_pressed) return "pressed";
                return _hover ? "hover" : "normal";
            }
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            base.SetBoundsCore(x, y, width, Math.Max(height, ApcTheme.MinHeight), specified);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = ClientRectangle;
            if (ApcTheme.Current == null)   // 디자이너 등 테마 로드 전
            {
                g.Clear(SystemColors.Control);
                ControlPaint.DrawBorder(g, rect, SystemColors.ControlDark, ButtonBorderStyle.Solid);
                TextRenderer.DrawText(g, Text, Font, rect, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            var img = ApcTheme.GetImage("btn_" + _variant.ToString().ToLowerInvariant() + "_" + StateName);
            var s = ApcTheme.Current.ButtonSlice;
            NineSlice.Draw(g, img, rect, new Padding(s[0], s[1], s[2], s[3]), ApcTheme.ImageScale(img));

            var textRect = rect;
            if (_pressed) textRect.Offset(0, 1);
            TextRenderer.DrawText(g, Text, Font, textRect, TextColor(),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (Focused && ShowFocusCues)
            {
                var f = Rectangle.Inflate(rect, -3, -3);
                using (var pen = new Pen(FieldFrame.FocusColor)) g.DrawRectangle(pen, f.X, f.Y, f.Width - 1, f.Height - 1);
            }
        }

        private Color TextColor()
        {
            var c = ApcTheme.Current.Colors;
            if (!Enabled) return ApcTheme.C(c.TextMuted);
            return _variant == ApcButtonVariant.Normal ? ApcTheme.C(c.Text) : ApcTheme.C(c.TextInverse);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false; Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            ApplyThemeFont();
            Height = Math.Max(Height, ApcTheme.MinHeight);
            Invalidate();
        }

        private void ApplyThemeFont()
        {
            if (ApcTheme.Current != null) Font = ApcTheme.GetFont();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
