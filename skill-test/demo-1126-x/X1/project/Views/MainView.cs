using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using Polishing.Data;

namespace Polishing.Views
{
    /// <summary>
    /// 연마 공정 바코드 시스템 메인 화면: 판정판(전 설비 중 가장 나쁜 것) + 설비군 탭 + 설비 현황 표.
    /// 설비를 고르고 [작업 시작]을 누르면 Lot 스캔 dialog 가 열린다. 데이터는 IMainViewSource 에서만 받는다(기본: 샘플).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int JudgeRowHeight = 110, MaxUnitRows = 8;

        private readonly IMainViewSource _source;
        private readonly List<UnitInfo> _units;
        private readonly List<UnitInfo> _shown = new List<UnitInfo>();
        private readonly ApcTabStrip _tabs = new ApcTabStrip();
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcValueCard _running = Card("가동 중", "대");
        private readonly ApcValueCard _done = Card("작업 완료", "대");
        private readonly ApcGrid _grid = new ApcGrid { ReadOnly = true, ClearInitialSelection = false, ScrollBars = ScrollBars.None };
        private readonly Label _reason = Caption("");
        private readonly ApcButton _start = Button("작업 시작", ApcButtonVariant.Primary, 130);

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("연마 공정 바코드 시스템")
        {
            _source = source;
            _units = new List<UnitInfo>(source.GetUnits());
            _tabs.SetTabs(new List<string>(source.GetGroups()).ToArray());
            _tabs.Width = _tabs.PreferredTabsWidth;
            _tabs.SelectedIndexChanged += (s, e) => Reload(0);
            _judge.Margin = new Padding(0, 0, S(12), 0);

            BuildColumns();
            _grid.SelectionChanged += (s, e) => ShowStartState();
            _start.Click += (s, e) => StartWork();

            Body.Controls.Add(Titled("SAP 전송 이력", BuildHistory()));
            Body.Controls.Add(Columns(TitledHeight(ApcGrid.HeightForRows(MaxUnitRows), true), new Control[] { Titled("설비 현황", _grid, _reason, _start) }));
            Body.Controls.Add(Columns(_tabs.Height, new Control[] { _tabs }));
            Body.Controls.Add(Columns(S(JudgeRowHeight), new Control[] { _judge, _running, _done }, new[] { 60f, 20f, 20f }));
            Reload(0);
        }

        private void BuildColumns()
        {
            _grid.Columns.Add(new ApcLampColumn { Name = "status", HeaderText = "상태" });
            AddColumn("name", "설비", 150);
            AddColumn("lot", "Lot No.", 200);
            AddColumn("material", "자재코드", 250);
            AddColumn("chip", "칩 구분", 90);
            AddColumn("barrels", "바렐수", 80);
            AddColumn("svmin", "시간 SV(분)", 120);
            AddColumn("pvmin", "시간 PV(분)", 120);
            AddColumn("rpmsv", "RPM SV(rpm)", 120);
            AddColumn("rpmpv", "RPM PV(rpm)", 120);
            AddColumn("note", "내용", 0);
            var note = _grid.Columns["note"];
            note.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            note.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            note.MinimumWidth = S(300);
        }

        private void AddColumn(string name, string header, int width)
        {
            int i = _grid.Columns.Add(name, header);
            var c = _grid.Columns[i];
            c.SortMode = DataGridViewColumnSortMode.NotSortable;
            c.Width = S(width);
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        /// <summary>선택 탭 설비군의 표와 위 판정판·카드를 다시 채운다. keepRow = 다시 선택할 행 번호.</summary>
        private void Reload(int keepRow)
        {
            _shown.Clear();
            _grid.Rows.Clear();
            foreach (var u in _units)
            {
                if (u.Group != _tabs.SelectedIndex) continue;
                _shown.Add(u);
                var v = UnitJudge.Evaluate(u);
                int r = _grid.Rows.Add(new ApcLampValue(v.Status, v.StateWord), u.Name, LotText(u), u.MaterialCode, u.ChipType,
                    Count(u.Barrels), Count(u.SvMin), u.PvMin.HasValue ? u.PvMin.Value.ToString("0.0", CultureInfo.InvariantCulture) : "",
                    Count(u.State == UnitState.Waiting || u.State == UnitState.Offline ? 0 : u.RpmSv),
                    u.RpmPv.HasValue ? u.RpmPv.Value.ToString(CultureInfo.InvariantCulture) : "", v.Reason);
                if (v.Status == StatusKind.Alarm) MarkAlarmCell(_grid.Rows[r].Cells["note"]);
            }
            if (_grid.Rows.Count > 0)
            {
                _grid.CurrentCell = _grid.Rows[keepRow].Cells[1];
                _grid.Rows[keepRow].Selected = true;
            }
            ShowSummary();
            ShowStartState();
        }

        private ApcGrid BuildHistory()
        {
            var grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            grid.Columns.Add("time", "시각");
            grid.Columns.Add("unit", "설비");
            grid.Columns.Add("lot", "Lot No.");
            grid.Columns.Add("kind", "구분");
            grid.Columns.Add(new ApcLampColumn { Name = "result", HeaderText = "결과" });
            foreach (DataGridViewColumn c in grid.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            foreach (var r in _source.GetSendHistory())
                grid.Rows.Add(r.Time, r.Unit, r.Lot, r.Kind, new ApcLampValue(r.Status, r.Result));
            return grid;
        }

        private static void MarkAlarmCell(DataGridViewCell cell)
        {
            cell.Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            cell.Style.Font = ApcTheme.GetFont(System.Drawing.FontStyle.Bold);
        }

        private static string Count(int n) { return n == 0 ? "" : n.ToString(CultureInfo.InvariantCulture); }

        private static string LotText(UnitInfo u)
        {
            if (u.Lots.Count == 0) return "";
            return u.Lots.Count == 1 ? u.Lots[0] : u.Lots[0] + " 외 " + (u.Lots.Count - 1);
        }

        /// <summary>판정판은 전 설비 중 가장 나쁜 것을 말한다(탭을 바꿔도 그대로).</summary>
        private void ShowSummary()
        {
            var alarms = new List<UnitInfo>();
            int running = 0, done = 0;
            foreach (var u in _units)
            {
                if (UnitJudge.Evaluate(u).Status == StatusKind.Alarm) alarms.Add(u);
                if (u.State == UnitState.Running) running++;
                if (u.State == UnitState.Done) done++;
            }
            if (alarms.Count == 0)
            {
                _judge.Status = StatusKind.Run; _judge.Text = "이상 없음"; _judge.SubText = "전 설비 정상";
            }
            else if (alarms.Count == 1)
            {
                _judge.Status = StatusKind.Alarm; _judge.Text = alarms[0].Name + " 알람";
                _judge.SubText = UnitJudge.Evaluate(alarms[0]).Reason;
            }
            else
            {
                _judge.Status = StatusKind.Alarm; _judge.Text = "알람 " + alarms.Count + "건";
                _judge.SubText = alarms[0].Name + " · " + alarms[1].Name + (alarms.Count > 2 ? " 외 " + (alarms.Count - 2) + "건" : "");
            }
            _running.Value = running.ToString(CultureInfo.InvariantCulture);
            _done.Value = done.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>작업 중(가동)·통신 끊김 설비에는 조건을 보낼 수 없다. 이유는 버튼 왼쪽에.</summary>
        private void ShowStartState()
        {
            var u = Selected();
            bool blocked = u != null && (u.State == UnitState.Running || u.State == UnitState.Offline);
            _start.Enabled = u != null && !blocked;
            _reason.Text = u == null ? "설비를 선택하세요" : blocked ? (u.State == UnitState.Running ? "가동 중 · 전송 불가" : "통신 끊김 · 전송 불가") : "";
        }

        private UnitInfo Selected()
        {
            var row = _grid.CurrentRow;
            return row == null || row.Index >= _shown.Count ? null : _shown[row.Index];
        }

        private void StartWork()
        {
            var u = Selected();
            if (u == null || u.State == UnitState.Running || u.State == UnitState.Offline) return;
            int row = _grid.CurrentRow.Index;
            using (var dlg = new WorkStartDialog(_source, u))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (!_source.SendCondition(u, dlg.Recipes, dlg.BarrelNos))
                {
                    ApcDialog.Warn(FindForm(), "조건 전송에 실패했습니다 · " + u.Name);
                    return;
                }
                var first = dlg.Recipes[0];
                var lots = new List<string>();
                foreach (var r in dlg.Recipes) lots.Add(r.Lot);
                _units[_units.IndexOf(u)] = new UnitInfo(u.Name, u.Group, UnitState.Sent, lots, first.MaterialCode, first.ChipType,
                    first.Barrels, first.TimeMin, null, u.RpmSv, null);
                Reload(row);
            }
        }

        private static ApcValueCard Card(string title, string unit)
        {
            return new ApcValueCard { Title = title, Unit = unit, ShowLamp = false, Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        }
    }
}
