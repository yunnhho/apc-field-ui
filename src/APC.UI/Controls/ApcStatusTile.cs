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
    /// Equipment tile: lamp + equipment name (no subtitle — the lamp color is the status; the screen's banner or table gives the reason). Click raises Click (open a detail dialog, etc.). Blue border on hover or focus,
    /// thick alarm-color border on alarm. Reachable with Tab, pressed with Enter or Space.
    /// </summary>
    public class ApcStatusTile : Control
    {
        private StatusKind _status = StatusKind.Stop;
        private bool _hover;
        private bool _keyPressed;   // 키를 누르고 있어도(자동 반복) Click은 한 번

        public ApcStatusTile()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            Size = new Size(S(280), S(96));
            Margin = new Padding(0, 0, S(12), S(12));
            ApcTheme.Changed += OnThemeChanged;
        }

        [DefaultValue(StatusKind.Stop)] public StatusKind Status { get { return _status; } set { _status = value; Invalidate(); } }

        private bool _selected;
        /// <summary>Selected mark (background = table selected-row color). When clicking a tile makes the cards and charts below show that unit, only that one tile is true.</summary>
        [DefaultValue(false)] public bool Selected { get { return _selected; } set { _selected = value; Invalidate(); } }

        /// <summary>Equipment name (bold, right of the lamp, vertically centered).</summary>
        public override string Text { get { return base.Text; } set { base.Text = value; } }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (ApcTheme.Current == null) { g.Clear(SystemColors.Control); return; }
            var c = ApcTheme.Current.Colors;
            Cards.DrawFrame(g, Size, _status, _hover || Focused, _selected);

            // 램프는 20~32px: 낮은 타일(입력칸 높이)에서도 점이 되지 않게 위아래 여백을 줄인다
            int pad = S(14), lamp = LampSize(Height);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(Cards.Lamp(_status), new Rectangle(pad, (Height - lamp) / 2, lamp, lamp));

            int x = pad * 2 + lamp, w = Width - x - pad;
            var flags = TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
            TextClip.Draw(g, this, Text, ApcTheme.GetFont(FontStyle.Bold), new Rectangle(x, 0, w, Height), ApcTheme.C(c.Text), flags);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnMouseDown(MouseEventArgs e) { if (CanFocus) Focus(); base.OnMouseDown(e); }

        protected override bool IsInputKey(Keys keyData)
        {
            return keyData == Keys.Enter || keyData == Keys.Space || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) { _keyPressed = true; e.Handled = true; }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (!_keyPressed || (e.KeyCode != Keys.Enter && e.KeyCode != Keys.Space)) return;
            _keyPressed = false;
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }

        private void OnThemeChanged(object sender, EventArgs e) { Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }

        internal const int MinLamp = 20;

        internal static int LampSize(int height)
        {
            return Math.Max(0, Math.Min(Math.Max(S(MinLamp), height - 2 * S(14)), Math.Min(S(32), height - S(6))));
        }

        private static int S(int px) { return (int)Math.Round(px * ApcTheme.Scale); }
    }
}
