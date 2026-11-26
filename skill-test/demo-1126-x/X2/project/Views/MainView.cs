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
    /// 연마 공정 바코드 작업 시작 화면. 설비군 탭 + 설비 표(LOT·자재코드·시간·RPM) + 전체 판정판.
    /// 설비를 누르면 LOT 스캔 dialog가 열리고, 확인하면 작업 조건(시간)을 전송한다. 가동 중에는 전송할 수 없다.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int MaxRowHeight = 48;
                private readonly IMainViewSource _source;
        private readonly List<List<EquipmentRow>> _groups = new List<List<EquipmentRow>>();
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcTabStrip _tabs = new ApcTabStrip();
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        private readonly ApcValueCard _running = Card("가동 중"), _waiting = Card("대기"), _offline = Card("통신 끊김");

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("연마 공정 바코드 시스템")
        {
            _source = source;
            for (int g = 0; g < source.GroupNames.Length; g++) _groups.Add(new List<EquipmentRow>(source.GetEquipment(g)));

            _tabs.SetTabs(source.GroupNames);
            _tabs.Width = _tabs.PreferredTabsWidth;
            BuildColumns();
            _grid.CellClick += OnCellClick;
            _grid.Resize += (s, e) => FitRows();
            _tabs.SelectedIndexChanged += (s, e) => ShowGroup();

            Body.Controls.Add(_grid);
            Body.Controls.Add(Row(_tabs));
            Body.Controls.Add(Columns(S(130), new Control[] { _judge, _running, _waiting, _offline }, new[] { 55f, 15f, 15f, 15f }));
            ShowGroup();
            ShowSummary();
        }

        private static ApcValueCard Card(string title)
        {
            return new ApcValueCard { Title = title, Unit = "대", Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        }

        private void BuildColumns()
        {
            _grid.Columns.Add("name", "설비");
            _grid.Columns.Add("kind", "구분");
            _grid.Columns.Add("material", "자재코드");
            _grid.Columns.Add("lot", "LOT No");
            _grid.Columns.Add("barrel", "바렐번호");
            _grid.Columns.Add("target", "목표 시간");
            _grid.Columns.Add("current", "경과 시간");
            _grid.Columns.Add("rpm", "회전속도(rpm)");
            _grid.Columns.Add(new ApcIconColumn { Name = "send", HeaderText = "전송", IconName = "icon_play" });
            _grid.Columns.Add(new ApcLampColumn { Name = "status", HeaderText = "상태" });

            int[] fixedW = { 0, 90, 0, 0, 100, 110, 110, 140, 72, 0 };
            float[] weights = { 26, 0, 34, 28, 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < fixedW.Length - 1; i++)
            {
                var col = _grid.Columns[i];
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (fixedW[i] > 0) { col.Width = S(fixedW[i]); continue; }
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.FillWeight = weights[i];
            }
            foreach (var name in new[] { "target", "current", "rpm" })
                _grid.Columns[name].DefaultCellStyle.Font = ApcTheme.GetFont(FontStyle.Bold);
        }

        private void ShowGroup()
        {
            var rows = _groups[_tabs.SelectedIndex];
            _grid.Rows.Clear();
            foreach (var r in rows) _grid.Rows.Add(ToCells(r));
            for (int i = 0; i < rows.Count; i++) StyleRpm(_grid.Rows[i], rows[i]);
            FitRows();
        }

        private static object[] ToCells(EquipmentRow r)
        {
            string lot = r.Lots.Length == 0 ? "" : r.Lots[0] + (r.Lots.Length > 1 ? " 외 " + (r.Lots.Length - 1) : "");
            return new object[]
            {
                r.Name,
                r.Condition == null ? "" : r.Condition.Kind,
                r.Condition == null ? "" : r.Condition.Material,
                lot,
                string.Join(", ", r.Barrels),
                r.Condition == null ? "" : PolishRules.Clock(r.Condition.TargetSeconds),
                r.Phase == EquipPhase.Running ? PolishRules.Clock(r.ElapsedSeconds) : "",
                r.Rpm.HasValue ? r.Rpm.Value.ToString() : "",
                r.Phase != EquipPhase.Running && r.Phase != EquipPhase.Offline,
                new ApcLampValue(PolishRules.StatusOf(r), PolishRules.StatusText(r))
            };
        }

        private static void StyleRpm(DataGridViewRow row, EquipmentRow r)
        {
            if (!PolishRules.IsRpmOut(r)) return;
            var style = row.Cells["rpm"].Style;
            style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            style.Font = ApcTheme.GetFont(FontStyle.Bold);
        }

        /// <summary>전체 설비(탭과 무관)의 가장 나쁜 상태를 판정판에, 상태별 대수를 카드에.</summary>
        private void ShowSummary()
        {
            EquipmentRow worst = null;
            int alarms = 0, running = 0, waiting = 0, offline = 0;
            foreach (var group in _groups)
                foreach (var r in group)
                {
                    if (PolishRules.IsRpmOut(r)) { alarms++; if (worst == null) worst = r; }
                    if (r.Phase == EquipPhase.Running) running++;
                    else if (r.Phase == EquipPhase.Offline) offline++;
                    else waiting++;
                }
            if (worst != null)
            {
                _judge.Status = StatusKind.Alarm;
                _judge.Text = alarms == 1 ? "RPM 이탈" : "RPM 이탈 " + alarms + "건";
                _judge.SubText = worst.Name + " · " + worst.Rpm + " rpm (" + PolishRules.RpmLimitText() + ")";
            }
            else
            {
                _judge.Status = StatusKind.Run;
                _judge.Text = "정상";
                _judge.SubText = "RPM 이탈 없음";
            }
            _running.Value = running.ToString();
            _waiting.Value = waiting.ToString();
            _offline.Value = offline.ToString();
            _running.Status = StatusKind.Run; _waiting.Status = StatusKind.Stop; _offline.Status = StatusKind.Stop;
        }

        private void FitRows()
        {
            int rows = _grid.Rows.Count;
            if (rows == 0) return;
            int fit = (_grid.ClientSize.Height - _grid.ColumnHeadersHeight - 2) / rows;
            int h = Math.Max(ApcTheme.MinHeight, Math.Min(S(MaxRowHeight), fit));
            _grid.RowTemplate.Height = h;
            foreach (DataGridViewRow row in _grid.Rows) row.Height = h;
        }

        private void OnCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var list = _groups[_tabs.SelectedIndex];
            var r = list[e.RowIndex];
            if (r.Phase == EquipPhase.Running) { ApcDialog.Warn(FindForm(), r.Name + " 가동 중에는 작업 조건을 전송할 수 없습니다."); return; }
            if (r.Phase == EquipPhase.Offline) { ApcDialog.Warn(FindForm(), r.Name + " 통신이 끊겨 전송할 수 없습니다."); return; }
            using (var dlg = new WorkStartDialog(r.Name, r.SlotCount, _source.FindCondition, _source.CheckEngineerPassword))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (!_source.SendCondition(r.Name, dlg.Lots, dlg.Barrels, dlg.Condition))
                {
                    ApcDialog.Warn(FindForm(), "전송에 실패했습니다. 다시 시도하세요.");
                    return;
                }
                list[e.RowIndex] = r.WithSent(dlg.Lots, dlg.Barrels, dlg.Condition);
            }
            ShowGroup();
            ShowSummary();
        }
    }
}
