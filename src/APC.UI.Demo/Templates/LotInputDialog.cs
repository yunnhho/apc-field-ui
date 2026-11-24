using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>
    /// 칸(슬롯)별 LOT 입력 dialog 골격(참고: Failure Analysis LOT 입력 — 설비마다 칸 수가 다르다).
    /// 위: 설비 이름 줄(어느 설비에 넣는지 → 값 굵게).
    /// 가운데: 칸마다 한 행인 입력 표 — 칸·SPEC·시간은 읽기 전용(회색), LOT·바렐번호만 입력(흰색). 넣은 칸은 시간이 찍힌다.
    /// 입력 중인 행은 읽기 전용 칸까지 연한 파랑(ApcGrid가 그린다)이라 지금 어느 칸을 스캔하는지 보인다.
    /// 바코드 스캐너는 키보드처럼 들어오므로 LOT 칸을 현재 셀로 두고 바로 받는다(스캔의 Enter → 다음 칸 아래로).
    /// 확인 시 빈 LOT이 있으면 Warn 후 그 칸으로, 닫지 않음. 결과는 Lots / Barrels 속성(칸 순서).
    /// </summary>
    internal sealed class LotInputDialog : ApcForm
    {
        private const int ColLot = 2, ColBarrel = 3, ColTime = 4;
        private readonly ApcGrid _grid = new ApcGrid { ClearInitialSelection = false, EditMode = DataGridViewEditMode.EditOnEnter,
            SelectionMode = DataGridViewSelectionMode.CellSelect, MultiSelect = false, ScrollBars = ScrollBars.None };   // 입력 표: 칸 수만큼 높이를 잡으므로 스크롤 없음
        private readonly string[] _slots;

        // 샘플: 두 칸을 넣다가 다시 연 상태
        public LotInputDialog() : this("고속 연마기 ST-GB-02", new[] { "B1", "B2", "B3", "B4" }, new[] { "Ø32 · 0.8μm", "Ø32 · 0.8μm", "Ø40 · 0.4μm", "Ø40 · 0.4μm" },
            new[] { "LOT-24112-0381", "LOT-24112-0394" }, new[] { "12", "07" }, new[] { "08:12:33", "08:13:05" }) { }

        /// <param name="lots">이미 넣은 LOT(앞 칸부터, 없으면 null) — 다시 열었을 때 이어서 스캔. barrels·times도 같은 순서.</param>
        public LotInputDialog(string equipment, string[] slots, string[] specs, string[] lots = null, string[] barrels = null, string[] times = null)
        {
            _slots = slots;
            SetupDialog("LOT 입력", 900);

            AddFieldRow("설비", new Label { Text = equipment, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent,
                Font = ApcTheme.GetFont(FontStyle.Bold), ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text) });

            int rowH = ApcTheme.Px(52), headH = ApcTheme.Px(40);
            _grid.RowTemplate.Height = rowH;
            _grid.ColumnHeadersHeight = headH;
            _grid.AllowUserToResizeColumns = false;
            AddColumn("slot", "칸", 70, true).DefaultCellStyle.Font = ApcTheme.GetFont(FontStyle.Bold);
            AddColumn("spec", "SPEC", 220, true);
            AddColumn("lot", "LOT No (스캔)", 280, false).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            AddColumn("barrel", "바렐번호", 130, false);
            AddColumn("time", "시간", 110, true);
            foreach (DataGridViewColumn c in _grid.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            for (int i = 0; i < slots.Length; i++)
                _grid.Rows.Add(slots[i], At(specs, i), At(lots, i), At(barrels, i), At(times, i));
            _grid.CellEndEdit += (s, e) => { if (e.ColumnIndex == ColLot) Refresh(e.RowIndex); };

            _grid.Height = headH + rowH * slots.Length + 2;   // 칸 수만큼(빈 줄·스크롤 없이)
            AddContent(_grid);

            var ok = new ApcButton { Text = "확인", Variant = ApcButtonVariant.Primary };
            ok.Click += (s, e) => Confirm();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, ok);   // Enter는 스캔이 쓴다 → AcceptButton 없음
            Shown += (s, e) => { _grid.CurrentCell = _grid.Rows[FirstEmpty()].Cells[ColLot]; _grid.Focus(); };
        }

        public string[] Lots { get; private set; }
        public string[] Barrels { get; private set; }

        private string Lot(int row) { return Convert.ToString(_grid.Rows[row].Cells[ColLot].Value).Trim(); }

        private static string At(string[] values, int i) { return values != null && i < values.Length ? values[i] ?? "" : ""; }

        // 이어서 넣을 칸: 첫 빈 LOT(다 찼으면 첫 칸)
        private int FirstEmpty()
        {
            for (int i = 0; i < _slots.Length; i++) if (Lot(i).Length == 0) return i;
            return 0;
        }

        // LOT가 처음 들어온 때 시각을 한 번만 적는다(같은 값으로 다시 Enter해도 유지), 지우면 시각도 지운다
        private void Refresh(int row)
        {
            var time = _grid.Rows[row].Cells[ColTime];
            if (Lot(row).Length == 0) time.Value = "";
            else if (Convert.ToString(time.Value).Length == 0) time.Value = DateTime.Now.ToString("HH:mm:ss");
        }

        private void Confirm()
        {
            _grid.EndEdit();
            var lots = Enumerable.Range(0, _slots.Length).Select(Lot).ToArray();
            int empty = Array.FindIndex(lots, l => l.Length == 0);
            if (empty >= 0)
            {
                ApcDialog.Warn(this, _slots[empty] + " 칸의 LOT를 스캔하세요.");
                _grid.CurrentCell = _grid.Rows[empty].Cells[ColLot];
                return;
            }
            Lots = lots;
            Barrels = Enumerable.Range(0, _slots.Length).Select(i => Convert.ToString(_grid.Rows[i].Cells[ColBarrel].Value).Trim()).ToArray();
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
