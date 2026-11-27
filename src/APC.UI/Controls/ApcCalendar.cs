using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>Calendar dropped down by ApcDateBox. Top ◀ year-month ▶, weekday row, 6 weeks × 7 day cells. Clicking a cell raises DateSelected.</summary>
    internal sealed class ApcCalendar : Control
    {
        private static readonly string[] DayNames = { "일", "월", "화", "수", "목", "금", "토" };

        private DateTime _month;
        private DateTime _value;

        public event EventHandler DateSelected;

        public ApcCalendar(DateTime value)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            _value = value.Date;
            _month = new DateTime(value.Year, value.Month, 1);
            Size = new Size(S(Cell) * 7 + 1, S(HeaderH) + S(DayRowH) + S(Cell) * 6 + 1);
        }

        private const int Cell = 40, HeaderH = 40, DayRowH = 28;

        public DateTime Value { get { return _value; } }
        public DateTime Month { get { return _month; } }

        /// <summary>First cell of the calendar (Sunday of the week containing the 1st of the month).</summary>
        internal static DateTime FirstCell(DateTime month)
        {
            var first = new DateTime(month.Year, month.Month, 1);
            return first.AddDays(-(int)first.DayOfWeek);
        }

        internal void ShiftMonth(int months)
        {
            var m = _month.AddMonths(months);
            if (m < new DateTime(ApcDateBox.MinDate.Year, ApcDateBox.MinDate.Month, 1) || m > ApcDateBox.MaxDate) return;
            _month = m;
            Invalidate();
        }

        /// <summary>Date at point p; null if not on a date cell.</summary>
        internal DateTime? DateAt(Point p)
        {
            int top = S(HeaderH) + S(DayRowH), cell = S(Cell);
            if (p.Y < top || p.X < 0) return null;
            int col = p.X / cell, row = (p.Y - top) / cell;
            if (col > 6 || row > 5) return null;
            return FirstCell(_month).AddDays(row * 7 + col);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);
            if (ApcTheme.Current == null) return;
            var c = ApcTheme.Current.Colors;
            int cell = S(Cell), headerH = S(HeaderH), dayH = S(DayRowH);
            var text = ApcTheme.C(c.Text);
            var muted = ApcTheme.C(c.TextMuted);
            var center = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;

            var header = new Rectangle(0, 0, Width, headerH);
            Gradients.Fill(g, header, c.GridHeader);
            TextRenderer.DrawText(g, "◀", ApcTheme.GetFont(), new Rectangle(0, 0, cell, headerH), text, center);
            TextRenderer.DrawText(g, "▶", ApcTheme.GetFont(), new Rectangle(Width - cell, 0, cell, headerH), text, center);
            TextRenderer.DrawText(g, _month.ToString("yyyy년 M월"), ApcTheme.GetFont(FontStyle.Bold), header, text, center);

            for (int i = 0; i < 7; i++)
                TextRenderer.DrawText(g, DayNames[i], ApcTheme.GetFont(), new Rectangle(i * cell, headerH, cell, dayH), muted, center);

            var first = FirstCell(_month);
            var today = DateTime.Today;
            using (var sel = new SolidBrush(ApcTheme.C(c.GridSelected)))
            using (var focus = new Pen(FieldFrame.FocusColor))
            using (var line = new Pen(ApcTheme.C(c.GridLine)))
            {
                for (int i = 0; i < 42; i++)
                {
                    var d = first.AddDays(i);
                    var r = new Rectangle((i % 7) * cell, headerH + dayH + (i / 7) * cell, cell, cell);
                    g.DrawRectangle(line, r);
                    if (d == _value)
                    {
                        g.FillRectangle(sel, r.X + 1, r.Y + 1, r.Width - 1, r.Height - 1);
                        g.DrawRectangle(focus, r.X + 1, r.Y + 1, r.Width - 3, r.Height - 3);
                    }
                    var font = d == today ? ApcTheme.GetFont(FontStyle.Bold | FontStyle.Underline) : ApcTheme.GetFont();
                    TextRenderer.DrawText(g, d.Day.ToString(), font, r, d.Month == _month.Month ? text : muted, center);
                }
            }
            using (var border = new Pen(ApcTheme.C(c.Border))) g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int cell = S(Cell);
            if (e.Y < S(HeaderH))
            {
                if (e.X < cell) ShiftMonth(-1);
                else if (e.X >= Width - cell) ShiftMonth(1);
                return;
            }
            var d = DateAt(e.Location);
            if (d == null) return;
            _value = d.Value;
            Invalidate();
            var h = DateSelected;
            if (h != null) h(this, EventArgs.Empty);
        }

        private static int S(int px) { return (int)Math.Round(px * ApcTheme.Scale); }
    }
}
