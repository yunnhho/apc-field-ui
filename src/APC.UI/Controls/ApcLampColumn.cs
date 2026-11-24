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
    /// 상태 램프 컬럼. 셀 값 StatusKind = 램프만(가운데), ApcLampValue = 램프 + 문구("전송됨", "대기") 한 칸(램프는 왼쪽 고정, 문구는 바로 옆 — 행마다 램프 위치가 같게), 그 외(null 등) = 빈 셀.
    /// </summary>
    public class ApcLampColumn : DataGridViewColumn
    {
        public ApcLampColumn() : base(new ApcLampCell())
        {
            Width = (int)Math.Round(56 * ApcTheme.Scale);
            SortMode = DataGridViewColumnSortMode.Automatic;
            HeaderText = "상태";
            DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }
    }

    public class ApcLampCell : DataGridViewTextBoxCell
    {
        private const int BaseLamp = 18;
        private const int TextLeftPad = 8;

        public override Type ValueType { get { return typeof(object); } }

        internal string ImageNameFor(object value)
        {
            if (value is ApcLampValue) value = ((ApcLampValue)value).Status;
            return value is StatusKind ? "lamp_" + value.ToString().ToLowerInvariant() : null;
        }

        protected override object GetFormattedValue(object value, int rowIndex, ref DataGridViewCellStyle cellStyle,
            TypeConverter valueTypeConverter, TypeConverter formattedValueTypeConverter, DataGridViewDataErrorContexts context)
        {
            return string.Empty;   // 글자는 그리지 않는다
        }

        protected override void Paint(Graphics g, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
            DataGridViewElementStates cellState, object value, object formattedValue, string errorText,
            DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
        {
            base.Paint(g, clipBounds, cellBounds, rowIndex, cellState, value, string.Empty, errorText, cellStyle, advancedBorderStyle, paintParts);

            var name = ImageNameFor(value);
            if (name == null || ApcTheme.Current == null || (paintParts & DataGridViewPaintParts.ContentForeground) == 0) return;

            int side = Math.Min((int)(BaseLamp * ApcTheme.Scale), cellBounds.Height - 4);
            string text = value is ApcLampValue ? ((ApcLampValue)value).Text : null;
            int gap = ApcTheme.Px(6);
            int textW = string.IsNullOrEmpty(text) ? 0 : TextRenderer.MeasureText(g, text, cellStyle.Font).Width;
            int total = side + (textW > 0 ? gap + textW : 0);
            int x = textW > 0 ? cellBounds.X + ApcTheme.Px(TextLeftPad)   // 문구가 있으면 램프 왼쪽 고정
                : cellBounds.X + Math.Max(ApcTheme.Px(4), (cellBounds.Width - total) / 2);   // 램프만이면 가운데
            var r = new Rectangle(x, cellBounds.Y + (cellBounds.Height - side) / 2, side, side);
            if (textW > 0)
                TextClip.Draw(g, DataGridView, text, cellStyle.Font, Rectangle.FromLTRB(r.Right + gap, cellBounds.Top, cellBounds.Right - 2, cellBounds.Bottom - 1),
                    cellStyle.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            var old = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(ApcTheme.GetImage(name), r);
            g.InterpolationMode = old;
        }
    }

    /// <summary>램프 + 문구 셀 값(ApcLampColumn). 정렬은 상태 → 문구 순.</summary>
    public sealed class ApcLampValue : IComparable
    {
        public ApcLampValue(StatusKind status, string text) { Status = status; Text = text ?? string.Empty; }

        public StatusKind Status { get; private set; }
        public string Text { get; private set; }

        public int CompareTo(object obj)
        {
            var o = obj as ApcLampValue;
            if (o == null) return 1;
            int c = Status.CompareTo(o.Status);
            return c != 0 ? c : string.CompareOrdinal(Text, o.Text);
        }

        public override string ToString() { return Text; }
    }
}
