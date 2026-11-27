using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// Date input: text box (yyyy-MM-dd) + ▼ button area inside its right edge (same look as the combo). Clicking opens a calendar; clicking a date closes it.
    /// Typed input accepts yyyy-MM-dd / yyyyMMdd and commits on Enter or on leaving the field; invalid input reverts to the previous value. ↑↓ keys move one day.
    /// </summary>
    public class ApcDateBox : ApcButtonField
    {
        public const string Format = "yyyy-MM-dd";
        /// <summary>Accepted range, so calendar and day stepping never throw at the ends of DateTime.</summary>
        public static readonly DateTime MinDate = new DateTime(1900, 1, 1), MaxDate = new DateTime(2999, 12, 31);
        private static readonly string[] InputFormats = { "yyyy-MM-dd", "yyyyMMdd", "yyyy-M-d", "yyyy/MM/dd", "yyyy/M/d", "yyyy.MM.dd" };

        private readonly ApcTextBox _text;
        private DateTime _value = DateTime.Today;
        private ToolStripDropDown _popup;
        private DateTime _popupClosedAt;

        public event EventHandler ValueChanged;

        public ApcDateBox() : base(new ApcTextBox { TextAlign = HorizontalAlignment.Center })
        {
            _text = TextBox;
            _text.Leave += (s, e) => Commit();
            _text.KeyDown += OnTextKeyDown;
            Size = new Size(ApcTheme.Px(160), ApcTheme.Px(ApcTextBox.BaseHeight));
            ShowValue();
        }

        public DateTime Value
        {
            get { return _value; }
            set
            {
                var v = value.Date < MinDate ? MinDate : value.Date > MaxDate ? MaxDate : value.Date;
                bool changed = v != _value;
                _value = v;
                ShowValue();
                if (changed) OnValueChanged(EventArgs.Empty);
            }
        }

        internal static bool TryParse(string text, out DateTime value)
        {
            return DateTime.TryParseExact((text ?? "").Trim(), InputFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
                && value >= MinDate && value <= MaxDate;
        }

        /// <summary>Commits the typed text. If it is not a date, shows the previous value again.</summary>
        public void Commit()
        {
            DateTime d;
            if (TryParse(_text.Text, out d)) Value = d;
            ShowValue();
        }

        private void ShowValue() { _text.Text = _value.ToString(Format, CultureInfo.InvariantCulture); }

        private void OnTextKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F4 || (e.Alt && e.KeyCode == Keys.Down)) { ShowCalendar(); e.Handled = true; }
            else if (e.KeyCode == Keys.Up) { Commit(); if (_value < MaxDate) Value = _value.AddDays(1); e.Handled = true; }
            else if (e.KeyCode == Keys.Down) { Commit(); if (_value > MinDate) Value = _value.AddDays(-1); e.Handled = true; }
            else if (e.KeyCode == Keys.Enter) Commit();
        }

        private void ShowCalendar()
        {
            // 열린 달력은 ▼ 영역의 마우스다운에서 먼저 닫힌다. 바로 이어진 Click은 다시 열지 않는다(토글).
            if ((DateTime.Now - _popupClosedAt).TotalMilliseconds < 250) return;
            Commit();
            if (_popup != null) _popup.Dispose();
            var cal = new ApcCalendar(_value);
            var host = new ToolStripControlHost(cal) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false, Size = cal.Size };
            _popup = new ToolStripDropDown { Padding = Padding.Empty, DropShadowEnabled = true };
            _popup.Items.Add(host);
            _popup.Closed += (s, e) => _popupClosedAt = DateTime.Now;
            cal.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) _popup.Close(); };
            cal.DateSelected += (s, e) => { Value = cal.Value; _popup.Close(); _text.Focus(); };
            _popup.Show(this, new Point(0, Height));
            cal.Focus();   // Esc로 닫을 수 있게
        }

        protected virtual void OnValueChanged(EventArgs e)
        {
            var h = ValueChanged;
            if (h != null) h(this, e);
        }

        internal override int SegmentCount { get { return 1; } }
        internal override bool SegmentPointsUp(int index) { return false; }
        internal override void OnSegmentClick(int index) { ShowCalendar(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _popup != null) _popup.Dispose();
            base.Dispose(disposing);
        }
    }
}
