using System.Windows.Forms;
using APC.UI.Controls;
using CellXrayJudgeStation.Data;

namespace CellXrayJudgeStation.Views
{
    /// <summary>선택 셀 측정값 표(메인 화면과 재판정 dialog가 같이 쓴다). 항목마다 같은 판정 함수.</summary>
    internal static class MeasureTable
    {
        public const int RowCount = CellSpec.OverhangPoints + 2;

        public static ApcGrid Create()
        {
            var g = new ApcGrid { ReadOnly = true };
            g.Columns.Add(new ApcLampColumn());
            g.Columns.Add("item", "항목");
            g.Columns.Add("value", "측정값");
            g.Columns.Add("spec", "규격");
            g.Columns.Add("verdict", "판정");
            foreach (DataGridViewColumn c in g.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            return g;
        }

        public static void Fill(ApcGrid g, CellRecord cell)
        {
            g.Rows.Clear();
            if (cell == null) return;
            for (int i = 0; i < CellSpec.OverhangPoints; i++)
                Add(g, "오버행 " + (i + 1) + "(mm)", cell.Overhang[i].ToString("0.00"), "0.50 이상", CellSpec.Overhang(cell.Overhang[i]));
            Add(g, "정렬 편차(mm)", cell.Align.ToString("0.00"), "0.30 이하", CellSpec.Align(cell.Align));
            Add(g, "이물", cell.Foreign ? "검출" : "없음", "없음", CellSpec.Foreign(cell.Foreign));
        }

        private static void Add(ApcGrid g, string item, string value, string spec, CellVerdict v)
        {
            int i = g.Rows.Add(CellSpec.Kind(v), item, value, spec, CellSpec.Word(v));
            if (v == CellVerdict.Ng)
            {
                var style = g.Rows[i].Cells[2].Style;
                style.ForeColor = APC.UI.Theming.ApcTheme.StatusText(CellSpec.Kind(v));
                style.Font = APC.UI.Theming.ApcTheme.GetFont(System.Drawing.FontStyle.Bold);
            }
        }
    }
}
