using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// Trend line. SetValues takes values in time order (left = past); XLabels sets x-axis labels (ends, middle, etc., spread evenly).
    /// Reference = set value (gray dashed), UpperLimit/LowerLimit = limits (alarm-color dashed). The y-axis range auto-fits both the values and the reference lines.
    /// </summary>
    public class ApcTrendChart : Control
    {
        private const int Ticks = 5;
        private double[] _values = new double[0];
        private string[] _xLabels = new string[0];
        private double? _reference, _upper, _lower;

        public ApcTrendChart()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(S(600), S(300));
            ApcTheme.Changed += OnThemeChanged;
        }

        public void SetValues(IEnumerable<double> values)
        {
            var list = new List<double>();
            if (values != null) foreach (var v in values) if (!double.IsNaN(v) && !double.IsInfinity(v)) list.Add(v);   // 결측(NaN)은 건너뛴다
            _values = list.ToArray();
            Invalidate();
        }

        internal int ValueCount { get { return _values.Length; } }

        [Browsable(false)]
        public string[] XLabels { get { return (string[])_xLabels.Clone(); } set { _xLabels = value == null ? new string[0] : (string[])value.Clone(); Invalidate(); } }

        /// <summary>null, NaN, infinity = no line.</summary>
        [Browsable(false)] public double? Reference { get { return _reference; } set { _reference = Finite(value); Invalidate(); } }
        [Browsable(false)] public double? UpperLimit { get { return _upper; } set { _upper = Finite(value); Invalidate(); } }
        [Browsable(false)] public double? LowerLimit { get { return _lower; } set { _lower = Finite(value); Invalidate(); } }

        private static double? Finite(double? v)
        {
            return v.HasValue && !double.IsNaN(v.Value) && !double.IsInfinity(v.Value) ? v : null;
        }

        /// <summary>Nice tick range covering min~max (steps of 1·2·5×10ⁿ).</summary>
        internal static void NiceRange(double min, double max, int ticks, out double lo, out double hi, out double step)
        {
            NiceRange(min, max, ticks, int.MaxValue, out lo, out hi, out step);
        }

        /// <summary>maxLabels: number of tick labels that fit the height. If exceeded, the step widens one level at a time (1→2→5→10).</summary>
        internal static void NiceRange(double min, double max, int ticks, int maxLabels, out double lo, out double hi, out double step)
        {
            if (max < min) { var t = min; min = max; max = t; }
            if (max - min < 1e-9) { min -= 1; max += 1; }
            double range = Nice(max - min, false);
            step = Nice(range / (ticks - 1), true);
            lo = Math.Floor(min / step) * step;
            hi = Math.Ceiling(max / step) * step;
            while (Math.Round((hi - lo) / step) + 1 > Math.Max(2, maxLabels))
            {
                step = Nice(step * 1.5, true);
                lo = Math.Floor(min / step) * step;
                hi = Math.Ceiling(max / step) * step;
            }
        }

        private static double Nice(double x, bool round)
        {
            double exp = Math.Floor(Math.Log10(x));
            double f = x / Math.Pow(10, exp);
            double nf = round ? (f < 1.5 ? 1 : f < 3 ? 2 : f < 7 ? 5 : 10)
                              : (f <= 1 ? 1 : f <= 2 ? 2 : f <= 5 ? 5 : 10);
            return nf * Math.Pow(10, exp);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);
            if (ApcTheme.Current == null) return;
            var c = ApcTheme.Current.Colors;
            var muted = ApcTheme.C(c.TextMuted);
            var font = ApcTheme.GetFont();

            var all = new List<double>(_values);
            foreach (var v in new[] { _reference, _upper, _lower }) if (v.HasValue) all.Add(v.Value);
            if (all.Count == 0)
            {
                TextRenderer.DrawText(g, "데이터 없음", font, ClientRectangle, muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            double lo, hi, step;
            double min = Min(all), max = Max(all), margin = (max - min) * 0.05;   // 기준선이 테두리에 겹치지 않게 여유
            int textH = font.Height;
            int fitLabels = (Height - textH - S(20)) / (textH * 3 / 2) + 1;   // 눈금 글자 사이 반 줄 이상 띄움
            NiceRange(min - margin, max + margin, Ticks, fitLabels, out lo, out hi, out step);
            string fmt = "F" + Math.Max(0, (int)Math.Ceiling(-Math.Log10(step)));

            int labelW = 0;
            for (double v = lo; v <= hi + step / 2; v += step)
                labelW = Math.Max(labelW, TextRenderer.MeasureText(v.ToString(fmt, CultureInfo.CurrentCulture), font).Width);
            var plot = Rectangle.FromLTRB(S(8) + labelW, S(10), Width - S(12), Height - textH - S(10));
            if (plot.Width < 10 || plot.Height < 10) return;

            Func<double, float> y = v => (float)(plot.Bottom - (v - lo) / (hi - lo) * plot.Height);

            using (var grid = new Pen(ApcTheme.C(c.GridLine)))
            using (var axis = new Pen(ApcTheme.C(c.Border)))
            {
                for (double v = lo; v <= hi + step / 2; v += step)
                {
                    float yy = y(v);
                    g.DrawLine(grid, plot.Left, yy, plot.Right, yy);
                    var s = v.ToString(fmt, CultureInfo.CurrentCulture);
                    TextRenderer.DrawText(g, s, font, new Rectangle(0, (int)yy - textH / 2, plot.Left - S(4), textH), muted,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                }
                g.DrawRectangle(axis, plot);
            }

            for (int i = 0; i < _xLabels.Length; i++)
            {
                float fx = _xLabels.Length == 1 ? plot.Left : plot.Left + plot.Width * i / (float)(_xLabels.Length - 1);
                var flags = i == 0 ? TextFormatFlags.Left : i == _xLabels.Length - 1 ? TextFormatFlags.Right : TextFormatFlags.HorizontalCenter;
                int w = S(120);
                int x = i == 0 ? (int)fx : i == _xLabels.Length - 1 ? (int)fx - w : (int)fx - w / 2;
                TextRenderer.DrawText(g, _xLabels[i], font, new Rectangle(x, plot.Bottom + S(4), w, textH), muted, flags | TextFormatFlags.SingleLine);
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            DrawLevel(g, plot, y, _reference, ApcTheme.C(c.TextMuted), 1);
            DrawLevel(g, plot, y, _upper, ApcTheme.Status(StatusKind.Alarm), 2);   // 1px 빨간 점선은 회색처럼 보인다
            DrawLevel(g, plot, y, _lower, ApcTheme.Status(StatusKind.Alarm), 2);

            if (_values.Length == 0) return;
            var pts = new PointF[_values.Length];
            for (int i = 0; i < _values.Length; i++)
            {
                float fx = _values.Length == 1 ? plot.Right : plot.Left + plot.Width * i / (float)(_values.Length - 1);
                pts[i] = new PointF(fx, y(_values[i]));
            }
            var lineColor = ApcTheme.Status(StatusKind.Info);
            using (var pen = new Pen(lineColor, Math.Max(2f, 2 * ApcTheme.Scale)) { LineJoin = LineJoin.Round })
                if (pts.Length > 1) g.DrawLines(pen, pts);
            var last = pts[pts.Length - 1];
            float r = 4 * ApcTheme.Scale;
            using (var b = new SolidBrush(lineColor)) g.FillEllipse(b, last.X - r, last.Y - r, 2 * r, 2 * r);   // 현재값 강조
        }

        private static void DrawLevel(Graphics g, Rectangle plot, Func<double, float> y, double? level, Color color, int width)
        {
            if (!level.HasValue) return;
            using (var pen = new Pen(color, Math.Max(width, width * ApcTheme.Scale)) { DashStyle = DashStyle.Dash })
                g.DrawLine(pen, plot.Left, y(level.Value), plot.Right, y(level.Value));
        }

        private static double Min(List<double> v) { double m = double.MaxValue; foreach (var x in v) m = Math.Min(m, x); return m; }
        private static double Max(List<double> v) { double m = double.MinValue; foreach (var x in v) m = Math.Max(m, x); return m; }

        private void OnThemeChanged(object sender, EventArgs e) { Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private static int S(int px) { return (int)Math.Round(px * ApcTheme.Scale); }
    }
}
