using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>Status lamp. Images: lamp_{run|warn|alarm|stop|info}. Blink works only for Alarm.</summary>
    public class ApcLamp : Control
    {
        public const int BlinkIntervalMs = 500;
        private const int BaseSize = 24;

        private readonly Timer _blinkTimer = new Timer { Interval = BlinkIntervalMs };
        private StatusKind _status = StatusKind.Stop;
        private bool _blink;
        private bool _blinkOff;

        public ApcLamp()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            int s = (int)Math.Round(BaseSize * ApcTheme.Scale);
            Size = new Size(s, s);
            _blinkTimer.Tick += (sender, e) => { _blinkOff = !_blinkOff; Invalidate(); };
            ApcTheme.Changed += OnThemeChanged;
        }

        [DefaultValue(StatusKind.Stop)]
        public StatusKind Status
        {
            get { return _status; }
            set { _status = value; UpdateBlink(); Invalidate(); }
        }

        [DefaultValue(false)]
        public bool Blink
        {
            get { return _blink; }
            set { _blink = value; UpdateBlink(); }
        }

        internal string ImageName { get { return "lamp_" + _status.ToString().ToLowerInvariant(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_blinkOff) return;
            int side = Math.Min(Width, Height);
            var r = new Rectangle((Width - side) / 2, (Height - side) / 2, side, side);
            var g = e.Graphics;
            if (ApcTheme.Current == null)
            {
                g.FillEllipse(Brushes.Gray, r);
                return;
            }
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(ApcTheme.GetImage(ImageName), r);
        }

        private void UpdateBlink()
        {
            bool on = _blink && _status == StatusKind.Alarm;
            _blinkTimer.Enabled = on;
            if (!on) _blinkOff = false;
        }

        private void OnThemeChanged(object sender, EventArgs e) { Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ApcTheme.Changed -= OnThemeChanged;
                _blinkTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
