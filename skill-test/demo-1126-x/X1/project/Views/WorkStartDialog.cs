using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using Polishing.Data;

namespace Polishing.Views
{
    /// <summary>
    /// 연마 작업 시작 dialog: 설비를 고른 뒤 Lot 바코드 스캔(저속 최대 2, 고속 최대 4) + 바렐번호 수기 입력 → 작업 조건(시간) 전송.
    /// 한 건이면 그대로, 여러 건이면 작업 조건이 모두 같을 때만 전송한다(다르면 경고 + 미전송, 레시피 없으면 경고 + 미전송).
    /// 입력 표 방식은 LotInputDialog 와 같다(스캐너는 키보드처럼 들어와 Lot 칸에 바로 입력, Enter → 다음 칸).
    /// </summary>
    internal sealed class WorkStartDialog : ApcForm
    {
        private const int ColLot = 1, ColBarrel = 2, ColMaterial = 3, ColTime = 4;

        private readonly IMainViewSource _source;
        private readonly UnitInfo _unit;
        private readonly int _slots;
        private readonly Dictionary<int, LotRecipe> _recipes = new Dictionary<int, LotRecipe>();
        private readonly ApcInfoStrip _strip = new ApcInfoStrip();
        private readonly ApcGrid _grid = new ApcGrid { ClearInitialSelection = false, EditMode = DataGridViewEditMode.EditOnEnter,
            SelectionMode = DataGridViewSelectionMode.CellSelect, MultiSelect = false, ScrollBars = ScrollBars.None };

        // 샘플: 고속 5호기에 Lot 한 건을 스캔한 상태
        public WorkStartDialog() : this(new SampleMainViewSource(), new SampleMainViewSource().GetUnits()[4]) { }

        public WorkStartDialog(IMainViewSource source, UnitInfo unit)
        {
            _source = source;
            _unit = unit;
            _slots = source.GetSlotCount(unit);
            SetupDialog("연마 작업 시작", 900);

            AddFieldRow("설비", new Label { Text = unit.Name, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent,
                Font = ApcTheme.GetFont(FontStyle.Bold), ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text) });

            int rowH = ApcTheme.Px(52), headH = ApcTheme.Px(40);
            _grid.RowTemplate.Height = rowH;
            _grid.ColumnHeadersHeight = headH;
            _grid.AllowUserToResizeColumns = false;
            AddColumn("slot", "칸", 70, true).DefaultCellStyle.Font = ApcTheme.GetFont(FontStyle.Bold);
            AddColumn("lot", "Lot No (스캔)", 260, false);
            AddColumn("barrel", "바렐번호", 120, false);
            AddColumn("material", "자재코드", 300, true).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            AddColumn("time", "시각", 110, true);
            foreach (DataGridViewColumn c in _grid.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            for (int i = 0; i < _slots; i++) _grid.Rows.Add(i + 1, "", "", "", "");
            _grid.CellEndEdit += (s, e) => { if (e.ColumnIndex == ColLot) OnLotEntered(e.RowIndex); };
            _grid.Height = headH + rowH * _slots + 2;
            AddContent(_grid);

            _strip.SetItems("칩 구분", "바렐수", "바렐당 수량", "시간(분)");
            AddContent(_strip);

            var ok = new ApcButton { Text = "조건 전송", Variant = ApcButtonVariant.Primary, Width = ApcTheme.Px(130) };
            ok.Click += (s, e) => Confirm();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, ok);   // 스캔 Enter를 쓰므로 AcceptButton 없음

            if (unit.State == UnitState.Sent) Prefill();
            Shown += (s, e) => { _grid.CurrentCell = _grid.Rows[FirstEmpty()].Cells[ColLot]; _grid.Focus(); };
        }

        public IList<LotRecipe> Recipes { get; private set; }
        public IList<string> BarrelNos { get; private set; }

        // 이어서 열었을 때(이미 전송된 Lot 이 있는 설비) 앞 칸부터 채운다.
        private void Prefill()
        {
            for (int i = 0; i < _unit.Lots.Count && i < _slots; i++)
            {
                _grid.Rows[i].Cells[ColLot].Value = _unit.Lots[i];
                _grid.Rows[i].Cells[ColBarrel].Value = (i + 1).ToString(CultureInfo.InvariantCulture);
                OnLotEntered(i);
            }
        }

        private string Lot(int row) { return Convert.ToString(_grid.Rows[row].Cells[ColLot].Value).Trim(); }

        private int FirstEmpty()
        {
            for (int i = 0; i < _slots; i++) if (Lot(i).Length == 0) return i;
            return 0;
        }

        // Lot 가 들어오면 SAP 조회 결과를 보여 준다. 레시피가 없으면 경고(스캔 Enter 처리 뒤 띄운다).
        private void OnLotEntered(int row)
        {
            var cells = _grid.Rows[row].Cells;
            string lot = Lot(row);
            _recipes.Remove(row);
            cells[ColMaterial].Value = "";
            cells[ColTime].Value = "";
            if (lot.Length > 0)
            {
                var recipe = _source.LookupLot(_unit, lot);
                if (recipe == null)
                {
                    BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(this, "레시피가 없습니다 · " + lot)));
                }
                else
                {
                    _recipes[row] = recipe;
                    cells[ColMaterial].Value = recipe.MaterialCode;
                    cells[ColTime].Value = DateTime.Now.ToString("HH:mm:ss");
                }
            }
            ShowStrip();
        }

        private void ShowStrip()
        {
            LotRecipe first = null;
            for (int i = 0; i < _slots && first == null; i++) _recipes.TryGetValue(i, out first);
            _strip.SetValue("칩 구분", first == null ? "" : first.ChipType);
            _strip.SetValue("바렐수", first == null ? "" : first.Barrels.ToString(CultureInfo.InvariantCulture));
            _strip.SetValue("바렐당 수량", first == null ? "" : (first.ChipQty / first.Barrels).ToString("#,0", CultureInfo.InvariantCulture));
            _strip.SetValue("시간(분)", first == null ? "" : first.TimeMin.ToString(CultureInfo.InvariantCulture));
        }

        private void Confirm()
        {
            _grid.EndEdit();
            var recipes = new List<LotRecipe>();
            var barrels = new List<string>();
            for (int i = 0; i < _slots; i++)
            {
                string lot = Lot(i);
                if (lot.Length == 0) continue;
                string barrel = Convert.ToString(_grid.Rows[i].Cells[ColBarrel].Value).Trim();
                if (barrel.Length == 0) { Fail("바렐번호를 입력하세요 · " + lot, i, ColBarrel); return; }
                LotRecipe recipe;
                if (!_recipes.TryGetValue(i, out recipe)) { Fail("레시피가 없습니다 · " + lot, i, ColLot); return; }
                recipes.Add(recipe);
                barrels.Add(barrel);
            }
            if (recipes.Count == 0) { Fail("Lot를 스캔하세요.", 0, ColLot); return; }
            for (int i = 1; i < recipes.Count; i++)
                if (!recipes[0].SameCondition(recipes[i])) { Fail("작업 조건이 다른 Lot가 섞여 있습니다 · " + recipes[i].Lot, 0, ColLot); return; }
            Recipes = recipes;
            BarrelNos = barrels;
            DialogResult = DialogResult.OK;
        }

        private void Fail(string message, int row, int col)
        {
            ApcDialog.Warn(this, message);
            _grid.CurrentCell = _grid.Rows[row].Cells[col];
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
