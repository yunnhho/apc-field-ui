using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 테마 선택 목록(드롭다운 전용, 직접 입력 없음). 닫힌 상태는 입력칸 모양 + 오른쪽 그라데이션 화살표 버튼으로 직접 그리고,
    /// 펼친 목록은 항목 높이 28px×배율 이상(마우스·펜으로 누르기 쉽게). 높이는 ItemHeight로 정해진다.
    /// </summary>
    public class ApcComboBox : ComboBox
    {
        // 네이티브 콤보는 닫힌 높이 = ItemHeight + 6
        private const int NativeChrome = 6;

        public ApcComboBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DropDownStyle = ComboBoxStyle.DropDownList;
            DrawMode = DrawMode.OwnerDrawFixed;
            ApcTheme.Changed += OnThemeChanged;
            ApplyTheme();
        }

        private int ArrowWidth { get { return FieldFrame.ButtonWidth(Height); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = ClientRectangle;
            using (var b = new SolidBrush(FieldFrame.Fill(Enabled, false, Focused || DroppedDown))) g.FillRectangle(b, rect);

            var arrow = new Rectangle(rect.Right - ArrowWidth, rect.Top, ArrowWidth, rect.Height);
            FieldFrame.DrawButtonArea(g, arrow, false);
            FieldFrame.DrawTriangle(g, arrow, FieldFrame.TextColor(Enabled), false);

            int pad = (int)Math.Round(6 * ApcTheme.Scale) + 1;
            var textRect = Rectangle.FromLTRB(rect.Left + pad, rect.Top, arrow.Left - pad, rect.Bottom);
            TextRenderer.DrawText(g, SelectedIndex >= 0 ? GetItemText(SelectedItem) : Text, Font, textRect, FieldFrame.TextColor(Enabled),
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

            FieldFrame.DrawBorder(g, rect, Focused || DroppedDown);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            var back = selected && ApcTheme.Current != null ? ApcTheme.C(ApcTheme.Current.Colors.GridSelected) : Color.White;
            using (var b = new SolidBrush(back)) e.Graphics.FillRectangle(b, e.Bounds);
            int pad = (int)Math.Round(6 * ApcTheme.Scale);
            var r = Rectangle.FromLTRB(e.Bounds.Left + pad, e.Bounds.Top, e.Bounds.Right - pad, e.Bounds.Bottom);
            TextRenderer.DrawText(e.Graphics, GetItemText(Items[e.Index]), Font, r, FieldFrame.TextColor(true),
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        protected override void OnSelectedIndexChanged(EventArgs e) { Invalidate(); base.OnSelectedIndexChanged(e); }
        protected override void OnDropDown(EventArgs e) { Invalidate(); base.OnDropDown(e); }
        protected override void OnDropDownClosed(EventArgs e) { Invalidate(); base.OnDropDownClosed(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        private void ApplyTheme()
        {
            if (ApcTheme.Current != null) Font = ApcTheme.GetFont();   // 글꼴을 먼저: 글꼴 변경이 ItemHeight를 되돌리지 않게
            ItemHeight = Math.Max(ApcTheme.MinHeight, (int)Math.Round(ApcTextBox.BaseHeight * ApcTheme.Scale)) - NativeChrome;
            Invalidate();
        }

        private void OnThemeChanged(object sender, EventArgs e) { ApplyTheme(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
