using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 배합 탱크 공정 흐름 모니터링. 위에서부터: 전체 판정판 → 밸브 조작 버튼 → 공정 흐름도 → 밸브 상태 표 + 오늘 알람 이력.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int ValveCount = 6;

        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly FlowDiagram _diagram = new FlowDiagram { Dock = DockStyle.Fill };
        private readonly ApcGrid _valveGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false };
        private readonly ApcGrid _alarmGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcButton _operate = Button("밸브 조작", ApcButtonVariant.Primary, 160);
        private readonly Label _selected = Caption("");
        private string _selectedId = "V3";

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("배합 탱크 공정 흐름")
        {
            _source = source;
            BuildGrids();
            _operate.Click += (s, e) => OpenOperation();
            _valveGrid.SelectionChanged += (s, e) => OnValveSelected();

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "정상·열림·가동"), new ApcLampValue(StatusKind.Warn, "경고"),
                new ApcLampValue(StatusKind.Alarm, "알람"), new ApcLampValue(StatusKind.Stop, "닫힘·정지"));

            Body.Controls.Add(Titled("공정 흐름도", _diagram, legend));
            var tables = Columns(TitledHeight(ApcGrid.HeightForRows(ValveCount)),
                new Control[] { Titled("밸브 상태", _valveGrid), Titled("오늘 알람 이력", _alarmGrid) }, new float[] { 50, 50 });
            tables.Dock = DockStyle.Bottom;
            Body.Controls.Add(tables);
            Body.Controls.Add(Row(_operate, _selected));
            Body.Controls.Add(Columns(S(110), new Control[] { _judge }));

            Reload();
        }

        private void BuildGrids()
        {
            _valveGrid.Columns.Add("id", "밸브");
            _valveGrid.Columns.Add("route", "위치");
            _valveGrid.Columns.Add(new ApcLampColumn { Name = "state", HeaderText = "상태" });
            _valveGrid.Columns.Add("time", "마지막 조작 시각");
            _valveGrid.Columns.Add("op", "조작자");
            foreach (DataGridViewColumn c in _valveGrid.Columns)
                if (c.Name != "state") c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            _alarmGrid.Columns.Add(new ApcLampColumn { Name = "kind", HeaderText = "상태" });
            _alarmGrid.Columns.Add("time", "시각");
            _alarmGrid.Columns.Add("target", "대상");
            _alarmGrid.Columns.Add("text", "내용");
            _alarmGrid.Columns["time"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            _alarmGrid.Columns["target"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            _alarmGrid.Columns["text"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _alarmGrid.Columns["text"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        /// <summary>데이터 소스에서 다시 읽어 판정판·흐름도·표를 갱신한다.</summary>
        private void Reload()
        {
            var tanks = _source.GetTanks();
            var pumps = _source.GetPumps();
            var valves = _source.GetValves();

            ApplyJudge(tanks, valves);
            _diagram.Apply(tanks, pumps, valves);

            _valveGrid.Rows.Clear();
            foreach (var v in valves)
                _valveGrid.Rows.Add(v.Id, v.Route, new ApcLampValue(v.IsOpen ? StatusKind.Run : StatusKind.Stop, v.IsOpen ? "열림" : "닫힘"),
                    v.LastTime.ToString("HH:mm:ss"), v.Operator);
            SelectValve(_selectedId);

            _alarmGrid.Rows.Clear();
            foreach (var a in _source.GetTodayAlarms())
                _alarmGrid.Rows.Add(a.Kind, a.Time.ToString("HH:mm:ss"), a.Target, a.Text);
        }

        private void SelectValve(string id)
        {
            foreach (DataGridViewRow r in _valveGrid.Rows)
                if ((string)r.Cells["id"].Value == id)
                {
                    _valveGrid.CurrentCell = r.Cells["id"];
                    r.Selected = true;
                    break;
                }
            OnValveSelected();
        }

        private void OnValveSelected()
        {
            var row = _valveGrid.CurrentRow;
            _operate.Enabled = row != null;
            if (row == null) { _selected.Text = "밸브를 표에서 선택하세요"; return; }
            _selectedId = (string)row.Cells["id"].Value;
            _selected.Text = "선택 " + _selectedId + " · " + row.Cells["route"].Value;
        }

        // 판정판: 전체 공정에서 가장 나쁜 것(알람 > 경고). 레벨·온도 판정은 ProcessRules 한 곳.
        private void ApplyJudge(IList<TankInfo> tanks, IList<ValveInfo> valves)
        {
            var alarms = new List<string>();
            string firstSub = "";
            foreach (var t in tanks)
            {
                if (ProcessRules.LevelStatus(t.Level) == StatusKind.Alarm)
                {
                    var blocked = valves.Where(v => v.DownstreamTank == t.Id).Select(v => v.Id).ToArray();
                    if (alarms.Count == 0)
                        firstSub = ProcessRules.LevelAlarm + " % 초과 · " + string.Join("·", blocked) + " 열기 차단";
                    alarms.Add(t.Id + " 레벨 알람 " + t.Level + " %");
                }
                if (ProcessRules.TempStatus(t) == StatusKind.Alarm)
                {
                    if (alarms.Count == 0)
                        firstSub = "범위 " + ProcessRules.TempMin.ToString("0.0") + " ~ " + ProcessRules.TempMax.ToString("0.0") + " ℃";
                    alarms.Add(t.Id + " 온도 이탈 " + t.Temp.ToString("0.0") + " ℃");
                }
            }
            if (alarms.Count > 0)
            {
                _judge.Status = StatusKind.Alarm;
                _judge.Text = alarms.Count == 1 ? alarms[0] : "알람 " + alarms.Count + "건";
                _judge.SubText = alarms.Count == 1 ? firstSub : string.Join(" · ", alarms.Take(2).Select(a => a.Substring(0, 2)).ToArray());
                return;
            }
            var warns = tanks.Where(t => ProcessRules.LevelStatus(t.Level) == StatusKind.Warn).ToList();
            if (warns.Count > 0)
            {
                _judge.Status = StatusKind.Warn;
                _judge.Text = warns.Count == 1 ? warns[0].Id + " 레벨 경고 " + warns[0].Level + " %" : "경고 " + warns.Count + "건";
                _judge.SubText = string.Join(" · ", warns.Take(2).Select(t => t.Id).ToArray());
                return;
            }
            _judge.Status = StatusKind.Run;
            _judge.Text = "정상 운전";
            _judge.SubText = "레벨·온도 이상 없음";
        }

        private void OpenOperation()
        {
            if (_valveGrid.CurrentRow == null) return;
            var valve = _source.GetValves().First(v => v.Id == _selectedId);
            var downstream = _source.GetTanks().FirstOrDefault(t => t.Id == valve.DownstreamTank);
            var pump = _source.GetPumps().FirstOrDefault(p => p.Id == valve.PumpId);
            using (var dlg = new ValveOperationDialog(valve, downstream, pump))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.OperateValve(valve.Id, dlg.TargetOpen, dlg.Operator, DateTime.Now);
            }
            Reload();
        }
    }
}
