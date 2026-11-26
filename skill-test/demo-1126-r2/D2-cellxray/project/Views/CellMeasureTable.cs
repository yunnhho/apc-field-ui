using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CellXrayJudgeStation.Data;

namespace CellXrayJudgeStation.Views
{
    /// <summary>셀 측정값 표(메인 화면 선택 셀·재판정 dialog 공용). 판정은 CellJudge 한 곳.</summary>
    internal static class CellMeasureTable
    {
        public const int RowCount = CellRecord.OverheadPoints + 2;

        public static ApcGrid Create()
        {
            var g = new ApcGrid { ReadOnly = true, Dock = DockStyle.Fill };
            g.Columns.Add(new ApcLampColumn());
            g.Columns.Add("item", "항목");
            g.Columns.Add("value", "측정값");
            g.Columns.Add("spec", "기준");
            g.Columns.Add("result", "판정");
            foreach (DataGridViewColumn c in g.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            g.Columns["item"].FillWeight = 170;
            return g;
        }

        /// <summary>cell이 null이면 비운다(값 없음 → 빈 표).</summary>
        public static void Fill(ApcGrid g, CellRecord cell)
        {
            g.Rows.Clear();
            if (cell == null) return;
            for (int i = 0; i < CellRecord.OverheadPoints; i++)
                Add(g, CellJudge.OverhangKind(cell.Overhang[i]), "오버행 " + (i + 1) + " (mm)", CellJudge.F(cell.Overhang[i]), CellJudge.F(CellJudge.OverhangMin) + " 이상");
            Add(g, CellJudge.AlignKind(cell.Align), "정렬 편차 (mm)", CellJudge.F(cell.Align), CellJudge.F(CellJudge.AlignMax) + " 이하");
            var fk = CellJudge.ForeignKind(cell.Foreign);
            g.Rows.Add(fk, "이물", cell.Foreign ? "검출" : "없음", "없음", cell.Foreign ? "재판정" : "OK");
            Mark(g.Rows[g.Rows.Count - 1], fk);
        }

        private static void Add(ApcGrid g, StatusKind kind, string item, string value, string spec)
        {
            g.Rows.Add(kind, item, value, spec, CellJudge.KindWord(kind));
            Mark(g.Rows[g.Rows.Count - 1], kind);
        }

        // 규격 밖·경계 값은 글자색 + 굵게(행 배경은 칠하지 않는다)
        private static void Mark(DataGridViewRow row, StatusKind kind)
        {
            if (kind == StatusKind.Run) return;
            var cell = row.Cells[2];
            cell.Style.ForeColor = ApcTheme.StatusText(kind);
            cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
        }
    }
}
