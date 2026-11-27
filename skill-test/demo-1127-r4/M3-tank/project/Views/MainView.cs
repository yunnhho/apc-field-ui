using System;
using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>메인 화면. 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int JudgeHeight = 100;
        private const int ValveRows = 6;

        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly FlowBoard _board = new FlowBoard { Dock = DockStyle.Fill };
        private readonly ApcGrid _valveGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false };
        private readonly ApcGrid _alarmGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcButton _operate = Button("밸브 조작", ApcButtonVariant.Primary, 160);
        private readonly Label _selected = Caption("");

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("배합 탱크 공정 흐름 모니터링")
        {
            _source = source;

            _valveGrid.Columns.Add("id", "밸브");
            _valveGrid.Columns.Add("pos", "위치");
            _valveGrid.Columns.Add(new ApcLampColumn { HeaderText = "상태", Name = "state" });
            _valveGrid.Columns.Add("time", "마지막 조작");
            _valveGrid.Columns.Add("op", "조작자");
            foreach (DataGridViewColumn c in _valveGrid.Columns)
                if (c.Name != "state") c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            _alarmGrid.Columns.Add(new ApcLampColumn { HeaderText = "상태", Name = "kind" });
            _alarmGrid.Columns.Add("time", "시각");
            _alarmGrid.Columns.Add("text", "내용");
            _alarmGrid.Columns["text"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _alarmGrid.Columns["text"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "정상·열림·가동"), new ApcLampValue(StatusKind.Warn, "경고"),
                new ApcLampValue(StatusKind.Alarm, "알람"), new ApcLampValue(StatusKind.Stop, "닫힘·정지"));

            Reload();

            // Fill 먼저, 아래 고정 줄(Bottom), 그다음 위쪽 줄을 아래부터
            Body.Controls.Add(Titled("공정 흐름도", _board, legend));
            var bottom = Columns(TitledHeight(ApcGrid.HeightForRows(ValveRows)),
                new Control[] { Titled("밸브 상태", _valveGrid), Titled("오늘 알람 이력", _alarmGrid) }, new[] { 50f, 50f });
            bottom.Dock = DockStyle.Bottom;
            Body.Controls.Add(bottom);
            Body.Controls.Add(Row(_operate, _selected));
            Body.Controls.Add(Columns(S(JudgeHeight), new Control[] { _judge }));

            _valveGrid.SelectionChanged += (s, e) => UpdateSelected();
            _operate.Click += (s, e) => OperateSelected();
            _board.ValveClicked += id => { SelectValve(id); OperateSelected(); };
            UpdateSelected();
        }

        private void Reload()
        {
            var tanks = _source.GetTanks();
            var valves = _source.GetValves();
            var pumps = _source.GetPumps();
            string keep = SelectedValveId() ?? "V5";

            ShowJudge(tanks, valves);
            _board.SetData(tanks, valves, pumps);

            _valveGrid.Rows.Clear();
            foreach (var v in valves)
                _valveGrid.Rows.Add(v.Id, v.Position,
                    new ApcLampValue(v.IsOpen ? StatusKind.Run : StatusKind.Stop, v.IsOpen ? "열림" : "닫힘"),
                    v.LastOperated.ToString("HH:mm:ss"), v.Operator);

            _alarmGrid.Rows.Clear();
            foreach (var a in _source.GetTodayAlarms())
                _alarmGrid.Rows.Add(a.Kind, a.Time.ToString("HH:mm:ss"), a.Text);

            SelectValve(keep);
        }

        /// <summary>판정판 = 전체 공정의 가장 나쁜 항목. 선택한 밸브와 무관하다.</summary>
        private void ShowJudge(TankInfo[] tanks, ValveInfo[] valves)
        {
            var alarms = new List<string>();
            var warns = new List<string>();
            var detail = new List<string>();
            foreach (var t in tanks)
            {
                var lv = ProcessRules.LevelStatus(t.Level);
                if (lv == StatusKind.Alarm)
                {
                    alarms.Add(t.Id + " 레벨");
                    detail.Add("레벨 " + ProcessRules.Pct(t.Level) + " · " + BlockedValves(t.Id, valves) + " 열기 차단");
                }
                else if (lv == StatusKind.Warn) warns.Add(t.Id + " 레벨");
                var tk = ProcessRules.TempStatus(t.Id, t.Temperature);
                if (tk == StatusKind.Alarm) alarms.Add(t.Id + " 온도");
            }

            if (alarms.Count > 0)
            {
                _judge.Status = StatusKind.Alarm;
                _judge.Text = alarms.Count == 1 ? alarms[0] + " 알람" : "알람 " + alarms.Count + "건";
                _judge.SubText = alarms.Count == 1 && detail.Count == 1 ? detail[0] : string.Join(", ", alarms.ToArray());
            }
            else if (warns.Count > 0)
            {
                _judge.Status = StatusKind.Warn;
                _judge.Text = warns.Count == 1 ? warns[0] + " 경고" : "경고 " + warns.Count + "건";
                _judge.SubText = string.Join(", ", warns.ToArray());
            }
            else
            {
                _judge.Status = StatusKind.Run;
                _judge.Text = "정상";
                _judge.SubText = "전체 탱크 정상 운전";
            }
        }

        private static string BlockedValves(string tankId, ValveInfo[] valves)
        {
            var ids = new List<string>();
            foreach (var v in valves)
                if (!v.IsOpen && ProcessRules.DownstreamTank(v.Id) == tankId) ids.Add(v.Id);
            return ids.Count == 0 ? "열린 밸브 외" : string.Join(",", ids.ToArray());
        }

        private string SelectedValveId()
        {
            var row = _valveGrid.CurrentRow;
            return row == null ? null : (string)row.Cells["id"].Value;
        }

        private void SelectValve(string id)
        {
            foreach (DataGridViewRow row in _valveGrid.Rows)
                if ((string)row.Cells["id"].Value == id)
                {
                    _valveGrid.CurrentCell = row.Cells["id"];
                    row.Selected = true;
                    return;
                }
        }

        private void UpdateSelected()
        {
            var row = _valveGrid.CurrentRow;
            _operate.Enabled = row != null;
            _selected.Text = row == null ? "밸브 상태 표에서 밸브를 고르세요"
                : "선택 밸브 " + row.Cells["id"].Value + " · " + ((ApcLampValue)row.Cells["state"].Value).Text;
        }

        private void OperateSelected()
        {
            string id = SelectedValveId();
            if (id == null) return;
            ValveInfo valve = null;
            foreach (var v in _source.GetValves()) if (v.Id == id) valve = v;
            if (valve == null) return;

            bool open = !valve.IsOpen;
            string block = ProcessRules.BlockReason(id, open, _source.GetTanks(), _source.GetPumps());
            using (var dlg = new ValveDialog(valve, block))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                // 대화 중 상태가 바뀌었을 수 있으니 한 번 더 막는다.
                block = ProcessRules.BlockReason(id, open, _source.GetTanks(), _source.GetPumps());
                if (block != null) { ApcDialog.Warn(FindForm(), block); return; }
                _source.OperateValve(id, open, dlg.OperatorBadge);
            }
            Reload();
        }
    }
}
