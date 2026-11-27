using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// Numeric input: ▲(up)/▼(down) button area inside the right edge (same look as the combo arrow). Buttons, ↑↓ keys and mouse wheel step by Increment; typed input commits on Enter or on leaving the field.
    /// Out-of-range values are clamped to Minimum/Maximum; non-numbers revert to the previous value. Decimal places: DecimalPlaces.
    /// For fields that can be "not entered yet" such as measurements, empty them with Clear() and check HasValue (▲ on an empty field goes to Minimum plus one step).
    /// </summary>
    public class ApcNumericBox : ApcButtonField
    {
        private readonly ApcTextBox _text;
        private decimal _value;
        private bool _hasValue = true;
        private decimal _minimum;
        private decimal _maximum = 100;
        private decimal _increment = 1;
        private int _decimalPlaces;

        public event EventHandler ValueChanged;

        public ApcNumericBox() : base(new ApcTextBox { TextAlign = HorizontalAlignment.Right })
        {
            _text = TextBox;
            _text.KeyDown += OnTextKeyDown;
            _text.KeyPress += OnTextKeyPress;
            _text.Leave += (s, e) => Commit();
            _text.MouseWheel += (s, e) => Step(Math.Sign(e.Delta));
            ShowValue();
        }

        /// <summary>Entered value. Meaningless while HasValue is false (empty after Clear) — check HasValue before judging.</summary>
        [DefaultValue(typeof(decimal), "0")]
        public decimal Value
        {
            get { return _value; }
            set
            {
                var v = Clamp(Math.Round(value, _decimalPlaces, MidpointRounding.AwayFromZero), _minimum, _maximum);
                bool changed = v != _value || !_hasValue;
                _value = v;
                _hasValue = true;
                ShowValue();
                if (changed) OnValueChanged(EventArgs.Empty);
            }
        }

        /// <summary>Whether a value has been entered. false after Clear() (field is empty); true after setting Value or committing input. Default true.</summary>
        [Browsable(false)]
        public bool HasValue { get { return _hasValue; } }

        /// <summary>
        /// Empties the field (not entered yet). Initial state of a measurement field, and when moving to the next part or point.
        /// Raises ValueChanged if the field had a value — in a constructor call it before subscribing to ValueChanged.
        /// </summary>
        public void Clear()
        {
            if (!_hasValue) return;
            _hasValue = false;
            ShowValue();
            OnValueChanged(EventArgs.Empty);
        }

        [DefaultValue(typeof(decimal), "0")]
        public decimal Minimum
        {
            get { return _minimum; }
            set { _minimum = value; if (_maximum < value) _maximum = value; Reclamp(); }
        }

        [DefaultValue(typeof(decimal), "100")]
        public decimal Maximum
        {
            get { return _maximum; }
            set { _maximum = value; if (_minimum > value) _minimum = value; Reclamp(); }
        }

        [DefaultValue(typeof(decimal), "1")]
        public decimal Increment
        {
            get { return _increment; }
            set { _increment = value; }
        }

        [DefaultValue(0)]
        public int DecimalPlaces
        {
            get { return _decimalPlaces; }
            set { _decimalPlaces = Math.Max(0, Math.Min(10, value)); Reclamp(); }
        }

        /// <summary>Parses typed text into a value; false if not a number. The result is clamped to range and rounded to the decimal places.</summary>
        internal static bool TryParse(string text, decimal min, decimal max, int decimals, out decimal value)
        {
            value = 0;
            decimal parsed;
            if (!decimal.TryParse((text ?? "").Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out parsed)) return false;
            value = Clamp(Math.Round(parsed, decimals, MidpointRounding.AwayFromZero), min, max);
            return true;
        }

        /// <summary>Re-fits the value when range or decimals change. An empty field stays empty.</summary>
        private void Reclamp()
        {
            if (_hasValue) { Value = _value; return; }
            _value = Clamp(Math.Round(_value, _decimalPlaces, MidpointRounding.AwayFromZero), _minimum, _maximum);
        }

        private static decimal Clamp(decimal v, decimal min, decimal max)
        {
            return v < min ? min : v > max ? max : v;
        }

        /// <summary>Commits the typed text. If it is not a number, shows the previous value again.</summary>
        public void Commit()
        {
            decimal v;
            if (TryParse(_text.Text, _minimum, _maximum, _decimalPlaces, out v)) Value = v;
            ShowValue();
        }

        private void Step(int direction)
        {
            if (direction == 0 || !Enabled) return;
            Commit();
            Value = _hasValue ? _value + direction * _increment : _minimum + (direction > 0 ? _increment : 0);
            _text.SelectAll();
        }

        private void ShowValue()
        {
            _text.Text = _hasValue ? _value.ToString("F" + _decimalPlaces, CultureInfo.CurrentCulture) : "";
        }

        private void OnTextKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up) { Step(1); e.Handled = true; }
            else if (e.KeyCode == Keys.Down) { Step(-1); e.Handled = true; }
            else if (e.KeyCode == Keys.Enter) Commit();
        }

        private void OnTextKeyPress(object sender, KeyPressEventArgs e)
        {
            var nf = CultureInfo.CurrentCulture.NumberFormat;
            char c = e.KeyChar;
            bool ok = char.IsControl(c) || char.IsDigit(c)
                || (_decimalPlaces > 0 && nf.NumberDecimalSeparator.IndexOf(c) >= 0)
                || (_minimum < 0 && nf.NegativeSign.IndexOf(c) >= 0);
            e.Handled = !ok;
        }

        protected virtual void OnValueChanged(EventArgs e)
        {
            var h = ValueChanged;
            if (h != null) h(this, e);
        }

        internal override int SegmentCount { get { return 2; } }
        internal override bool SegmentPointsUp(int index) { return index == 0; }
        internal override void OnSegmentClick(int index) { Step(index == 0 ? 1 : -1); }
    }
}
