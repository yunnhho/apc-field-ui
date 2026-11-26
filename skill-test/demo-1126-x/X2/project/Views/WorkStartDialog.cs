using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using Polish.Data;

namespace Polish.Views
{
    /// <summary>
    /// 작업 조건 전송 dialog: LOT 바코드 스캔(저속 최대 2, 고속 최대 4) + 바렐번호 수기 입력 → 조건 조회 → 확인으로 전송.
    /// 전송 불가(레시피 없음·조건 불일치·바렐번호 빠짐)는 데이터에서 정해지며 표시줄에 이유가 나온다.
    /// 레시피 없음·조건 불일치일 때만 담당자 비밀번호 후 "조건 강제 입력"으로 풀 수 있다.
    /// </summary>
    internal sealed class WorkStartDialog : ApcForm
    {
        private const int ColLot = 1, ColBarrel = 2, ColMaterial = 3, ColKind = 4, ColTime = 5;
        private readonly ApcGrid _grid = new ApcGrid { ClearInitialSelection = false, EditMode = DataGridViewEditMode.EditOnEnter,
            SelectionMode = DataGridViewSelectionMode.CellSelect, MultiSelect = false, ScrollBars = ScrollBars.None };
        private readonly Func<string, PolishCondition> _find;
        private readonly Func<string, bool> _checkPassword;
        private readonly int _slots;
        private readonly ApcButton _ok = new ApcButton { Text = "전송", Variant = ApcButtonVariant.Primary };
        private readonly ApcButton _force = new ApcButton { Text = "조건 강제 입력" };
        private readonly Label _note;
        private PolishCondition _forced;
        private string _scanned = "";   // 마지막으로 평가한 LOT 조합(같은 값으로 셀만 지나갈 때 경고를 되풀이하지 않는다)

        // 샘플: 고속 4칸 중 두 칸을 스캔했는데 두 LOT의 작업 조건이 다른 상태(전송 막힘)
        public WorkStartDialog() : this("고속연마 1호기 -1", 4, new SampleMainViewSource().FindCondition,
            new SampleMainViewSource().CheckEngineerPassword, new[] { "B052610003412", "B052610004871" }, new[] { "12", "07" }) { }

        public WorkStartDialog(string equipment, int slots, Func<string, PolishCondition> find, Func<string, bool> checkPassword,
            string[] lots = null, string[] barrels = null)
        {
            _slots = slots; _find = find; _checkPassword = checkPassword;
            SetupDialog("작업 조건 전송", 1000);

            AddFieldRow("설비", new Label { Text = equipment, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent, Font = ApcTheme.GetFont(FontStyle.Bold), ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text) });

            int rowH = ApcTheme.Px(52), headH = ApcTheme.Px(40);
            _grid.RowTemplate.Height = rowH;
            _grid.ColumnHeadersHeight = headH;
            _grid.AllowUserToResizeColumns = false;
            AddColumn("slot", "칸", 70, true);
            AddColumn("lot", "LOT No (스캔)", 250, false);
            AddColumn("barrel", "바렐번호", 120, false);
            AddColumn("material", "자재코드", 260, true).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            AddColumn("kind", "구분", 120, true);
            AddColumn("time", "목표 시간", 120, true);
            foreach (DataGridViewColumn c in _grid.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            for (int i = 0; i < slots; i++)
                _grid.Rows.Add(i + 1, At(lots, i), At(barrels, i), "", "", "");
            _grid.CellEndEdit += (s, e) => { if (e.ColumnIndex == ColLot) CheckDuplicate(e.RowIndex); Evaluate(e.ColumnIndex == ColLot); };
            _grid.Height = headH + rowH * slots + 2;
            AddContent(_grid);

            _ok.Click += (s, e) => Send();
            _force.Click += (s, e) => Force();
            _note = SetFooterNote("");
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _ok, _force);   // Enter는 스캔이 쓴다
            Evaluate(false);
            Shown += (s, e) => { _grid.CurrentCell = _grid.Rows[FirstEmpty()].Cells[ColLot]; _grid.Focus(); };
        }

        public string[] Lots { get; private set; }
        public string[] Barrels { get; private set; }
        public PolishCondition Condition { get; private set; }

        private string Cell(int row, int col) { return Convert.ToString(_grid.Rows[row].Cells[col].Value).Trim(); }

        private static string At(string[] values, int i) { return values != null && i < values.Length ? values[i] ?? "" : ""; }

        private int FirstEmpty()
        {
            for (int i = 0; i < _slots; i++) if (Cell(i, ColLot).Length == 0) return i;
            return 0;
        }

        private void CheckDuplicate(int row)
        {
            string lot = Cell(row, ColLot);
            if (lot.Length == 0) return;
            for (int i = 0; i < _slots; i++)
                if (i != row && Cell(i, ColLot) == lot)
                {
                    _grid.Rows[row].Cells[ColLot].Value = "";
                    BeginInvoke((Action)(() => ApcDialog.Warn(this, "이미 스캔한 LOT입니다. " + lot)));
                    return;
                }
        }

        // 표를 읽어 조건을 다시 조회하고, 전송 가능 여부와 이유를 정한다.
        private void Evaluate(bool warn)
        {
            string lotsNow = Cell(0, ColLot);
            for (int k = 1; k < _slots; k++) lotsNow += "|" + Cell(k, ColLot);
            warn = warn && lotsNow != _scanned;
            _scanned = lotsNow;
            var found = new List<PolishCondition>();
            string noRecipe = null;
            int filled = 0;
            bool barrelMissing = false;
            for (int i = 0; i < _slots; i++)
            {
                string lot = Cell(i, ColLot);
                var row = _grid.Rows[i];
                if (lot.Length == 0) { row.Cells[ColMaterial].Value = ""; row.Cells[ColKind].Value = ""; row.Cells[ColTime].Value = ""; continue; }
                filled++;
                if (Cell(i, ColBarrel).Length == 0) barrelMissing = true;
                var c = _find(lot);
                row.Cells[ColMaterial].Value = c == null ? "" : c.Material;
                row.Cells[ColKind].Value = c == null ? "레시피 없음" : c.Kind;
                row.Cells[ColTime].Value = c == null ? "" : PolishRules.Clock(c.TargetSeconds);
                if (c == null) { if (noRecipe == null) noRecipe = lot; } else found.Add(c);
            }

            bool mismatch = false;
            for (int i = 1; i < found.Count; i++) if (!found[0].SameAs(found[i])) mismatch = true;

            Condition = _forced ?? (found.Count > 0 && noRecipe == null && !mismatch ? found[0] : null);
            string reason = "";
            bool alarm = false, canForce = false;
            if (filled == 0) reason = "LOT를 스캔하세요";
            else if (_forced != null) reason = barrelMissing ? "바렐번호 입력 필요" : "";
            else if (noRecipe != null)
            {
                reason = "레시피 없음 · 전송 불가"; alarm = true; canForce = true;
                if (warn) BeginInvoke((Action)(() => ApcDialog.Warn(this, "레시피가 없습니다. 조건을 전송하지 않습니다. " + noRecipe)));
            }
            else if (mismatch)
            {
                reason = "작업 조건 불일치 · 전송 불가"; alarm = true; canForce = true;
                if (warn) BeginInvoke((Action)(() => ApcDialog.Warn(this, "스캔한 LOT의 작업 조건이 서로 다릅니다. 조건을 전송하지 않습니다.")));
            }
            else if (barrelMissing) reason = "바렐번호 입력 필요";

            _note.Text = reason;
            _note.ForeColor = alarm ? ApcTheme.StatusText(StatusKind.Alarm) : ApcTheme.C(ApcTheme.Current.Colors.Text);
            _ok.Enabled = Condition != null && reason.Length == 0;
            _force.Enabled = canForce;
        }

        private void Force()
        {
            using (var pw = new AdminPasswordDialog())
            {
                if (pw.ShowDialog(this) != DialogResult.OK) return;
                if (!_checkPassword(pw.Password)) { ApcDialog.Warn(this, "비밀번호가 맞지 않습니다."); return; }
            }
            var basis = _find(Cell(0, ColLot));
            using (var dlg = new ForceConditionDialog(basis == null ? "그린칩" : basis.Kind, basis == null ? "" : basis.Material,
                basis == null ? 3600 : basis.TargetSeconds))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _forced = dlg.Condition;
            }
            Evaluate(false);
        }

        private void Send()
        {
            _grid.EndEdit();
            var lots = new List<string>();
            var barrels = new List<string>();
            for (int i = 0; i < _slots; i++)
                if (Cell(i, ColLot).Length > 0) { lots.Add(Cell(i, ColLot)); barrels.Add(Cell(i, ColBarrel)); }
            Lots = lots.ToArray();
            Barrels = barrels.ToArray();
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
