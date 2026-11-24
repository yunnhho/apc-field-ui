using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 행 동작 아이콘 열(전송 ▶, 초기화 ↻ 등). 셀 값 true = 파란 아이콘(누를 수 있음), false = 회색 아이콘(지금은 못 함), 그 밖 = 빈 칸.
    /// 이미지는 테마의 "{IconName}_normal" / "{IconName}_disabled". 누름은 표의 CellClick에서 IsEnabled(셀)로 확인해 처리한다.
    /// </summary>
    public class ApcIconColumn : DataGridViewColumn
    {
        public ApcIconColumn() : base(new ApcIconCell())
        {
            Width = (int)Math.Round(72 * ApcTheme.Scale);
            SortMode = DataGridViewColumnSortMode.NotSortable;
            ReadOnly = true;
            IconName = "icon_play";
        }

        /// <summary>테마 이미지 이름 앞부분(예: "icon_play", "icon_reset").</summary>
        [DefaultValue("icon_play")]
        public string IconName { get; set; }

        /// <summary>셀이 누를 수 있는 상태(값 true)인지.</summary>
        public static bool IsEnabled(DataGridViewCell cell) { return cell != null && cell.Value is bool && (bool)cell.Value; }

        public override object Clone()
        {
            var c = (ApcIconColumn)base.Clone();
            c.IconName = IconName;
            return c;
        }
    }

    public class ApcIconCell : DataGridViewTextBoxCell
    {
        private const int BaseIcon = 28;   // 34px 행에서 위아래 3px 여백

        public override Type ValueType { get { return typeof(object); } }

        internal string ImageNameFor(object value)
        {
            var col = OwningColumn as ApcIconColumn;
            if (col == null || !(value is bool)) return null;
            return col.IconName + ((bool)value ? "_normal" : "_disabled");
        }

        protected override object GetFormattedValue(object value, int rowIndex, ref DataGridViewCellStyle cellStyle,
            TypeConverter valueTypeConverter, TypeConverter formattedValueTypeConverter, DataGridViewDataErrorContexts context)
        {
            return string.Empty;
        }

        protected override void Paint(Graphics g, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
            DataGridViewElementStates cellState, object value, object formattedValue, string errorText,
            DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
        {
            base.Paint(g, clipBounds, cellBounds, rowIndex, cellState, value, string.Empty, errorText, cellStyle, advancedBorderStyle, paintParts);

            var name = ImageNameFor(value);
            if (name == null || ApcTheme.Current == null || (paintParts & DataGridViewPaintParts.ContentForeground) == 0) return;

            int side = Math.Min((int)(BaseIcon * ApcTheme.Scale), cellBounds.Height - 4);
            var r = new Rectangle(cellBounds.X + (cellBounds.Width - side) / 2, cellBounds.Y + (cellBounds.Height - side) / 2, side, side);
            var old = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(ApcTheme.GetImage(name), r);
            g.InterpolationMode = old;
        }
    }
}
