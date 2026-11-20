using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>
    /// 칸(슬롯)별 LOT 입력 dialog 골격(참고: Failure Analysis LOT 입력 — 설비마다 칸 수가 다르다).
    /// 위: 설비 이름 줄. 가운데: 칸마다 한 행인 표 — 칸·SPEC·시간·결과는 읽기 전용(회색), LOT·바렐번호만 입력(흰색).
    /// 바코드 스캐너는 키보드처럼 들어오므로 LOT 칸을 현재 셀로 두고 바로 받는다(스캔의 Enter → 다음 칸 아래로).
    /// 확인 시 빈 LOT이 있으면 Alert로 알려 주고 닫지 않는다. 결과는 Lots / Barrels 속성(칸 순서).
    /// </summary>
    internal sealed class LotInputDialog : ApcForm
    {
        private const int ColLot = 2, ColBarrel = 3, ColTime = 4;
        private readonly ApcGrid _grid = new ApcGrid { ClearInitialSelection = false, EditMode = DataGridViewEditMode.EditOnEnter,
            SelectionMode = DataGridViewSelectionMode.CellSelect, MultiSelect = false, ScrollBars = ScrollBars.None };   // 입력 표: 행이 아니라 입력할 칸만 강조, 칸 수만큼 높이를 잡으므로 스크롤 없음
        private readonly string[] _slots;

        public LotInputDialog() : this("고속 연마기 ST-GB-02", new[] { "B1", "B2", "B3", "B4" }, new[] { "Ø32 · 0.8μm", "Ø32 · 0.8μm", "Ø40 · 0.4μm", "Ø40 · 0.4μm" }) { }

        public LotInputDialog(string equipment, string[] slots, string[] specs)
        {
            _slots = slots;
            SetupDialog("LOT 입력", 1000);

            int rowH = ApcTheme.Px(52), headH = ApcTheme.Px(40);
            AddFieldRow("설비", new Label { Text = equipment, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent,
                Font = ApcTheme.GetFont(FontStyle.Bold), ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text) });   // 어느 설비에 넣는지가 핵심 → 값 강조

            _grid.RowTemplate.Height = rowH;
            _grid.ColumnHeadersHeight = headH;
            _grid.AllowUserToResizeColumns = false;
            AddColumn("slot", "칸", 70, true).DefaultCellStyle.Font = ApcTheme.GetFont(FontStyle.Bold);
            AddColumn("spec", "SPEC", 330, true);
            AddColumn("lot", "LOT No (스캔)", 270, false);
            AddColumn("barrel", "바렐번호", 130, false);
            AddColumn("time", "시간", 100, true);
            AddColumn("result", "결과", 66, true).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (DataGridViewColumn c in _grid.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            for (int i = 0; i < slots.Length; i++) _grid.Rows.Add(slots[i], i < specs.Length ? specs[i] : "", "", "", "", "");
            _grid.CellEndEdit += (s, e) =>
            {
                if (e.ColumnIndex == ColLot && Convert.ToString(_grid.Rows[e.RowIndex].Cells[ColLot].Value).Trim().Length > 0)
                    _grid.Rows[e.RowIndex].Cells[ColTime].Value = DateTime.Now.ToString("HH:mm:ss");
            };

            _grid.Height = headH + rowH * slots.Length + 2;   // 칸 수만큼(빈 줄·스크롤 없이)
            AddContent(_grid);

            var ok = new ApcButton { Text = "확인", Variant = ApcButtonVariant.Primary };
            ok.Click += (s, e) => Confirm();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, ok);   // Enter는 스캔이 쓴다 → AcceptButton 없음
            Shown += (s, e) => { _grid.CurrentCell = _grid.Rows[0].Cells[ColLot]; _grid.Focus(); };
        }

        public string[] Lots { get; private set; }
        public string[] Barrels { get; private set; }

        private void Confirm()
        {
            _grid.EndEdit();
            var lots = new string[_slots.Length];
            var barrels = new string[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                lots[i] = Convert.ToString(_grid.Rows[i].Cells[ColLot].Value).Trim();
                barrels[i] = Convert.ToString(_grid.Rows[i].Cells[ColBarrel].Value).Trim();
                if (lots[i].Length == 0)
                {
                    ApcDialog.Warn(this, _slots[i] + " 칸의 LOT를 스캔하세요.");
                    _grid.CurrentCell = _grid.Rows[i].Cells[ColLot];
                    return;
                }
            }
            Lots = lots;
            Barrels = barrels;
            DialogResult = DialogResult.OK;
        }

        private DataGridViewColumn AddColumn(string name, string header, int width, bool readOnly)
        {
            int i = _grid.Columns.Add(name, header);
            var c = _grid.Columns[i];
            c.Width = ApcTheme.Px(width);
            c.ReadOnly = readOnly;
            return c;
        }
    }
}
