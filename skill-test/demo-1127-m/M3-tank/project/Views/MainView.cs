using System;
using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 배합 탱크 공정 흐름 모니터링. 맨 위 판정판(공정 전체 최악) → 왼쪽 공정 흐름도, 오른쪽 밸브 상태 표 + 오늘 알람 이력.
    /// 밸브는 표에서 고르고 [밸브 조작], 또는 흐름도의 밸브 상자를 눌러 조작 dialog를 연다.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int ColValve = 0, ColState = 2;
        private const string PickReason = "표에서 밸브를 선택하세요";

        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly FlowDiagram _diagram = new FlowDiagram { Dock = DockStyle.Fill };
        private readonly ApcGrid _valveGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _alarmGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcButton _operate = Button("밸브 조작", ApcButtonVariant.Primary);
        private readonly Label _reason = Caption(PickReason);
        private IList<TankInfo> _tanks;
        private IList<ValveInfo> _valves;
        private IList<PumpInfo> _pumps;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("배합 탱크 공정 흐름")
        {
            _source = source;
            _operate.Enabled = false;
            _operate.Click += (s, e) => OperateSelected();
            _diagram.ValveClicked += id => { SelectRow(id); OpenDialog(id); };

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "정상·열림·가동"), new ApcLampValue(StatusKind.Warn, "레벨 경고"),
                new ApcLampValue(StatusKind.Alarm, "레벨 알람"), new ApcLampValue(StatusKind.Stop, "닫힘·정지"));
            var flow = Titled("공정 흐름도", _diagram, legend);

            BuildValveGrid();
            BuildAlarmGrid();
            var valveBox = Titled("밸브 상태", _valveGrid, _reason, _operate);
            valveBox.Dock = DockStyle.Top;
            valveBox.Height = TitledHeight(ApcGrid.HeightForRows(6), true);
            var alarmBox = Titled("오늘 알람 이력", _alarmGrid);
            alarmBox.Dock = DockStyle.Fill;
            var right = new Panel { Dock = DockStyle.Fill };
            right.Controls.Add(alarmBox);
            right.Controls.Add(new Panel { Dock = DockStyle.Top, Height = S(12) });
            right.Controls.Add(valveBox);

            _valveGrid.SelectionChanged += (s, e) => UpdateOperateButton();

            Body.Controls.Add(FillColumns(new Control[] { flow, right }, new float[] { 58, 42 }));
            Body.Controls.Add(Columns(S(110), new Control[] { _judge }));
            Reload();
        }

        private void BuildValveGrid()
        {
            _valveGrid.Columns.Add("valve", "밸브");
            _valveGrid.Columns.Add("pos", "위치");
            _valveGrid.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            _valveGrid.Columns.Add("time", "마지막 조작");
            _valveGrid.Columns.Add("op", "조작자");
            foreach (DataGridViewColumn c in _valveGrid.Columns)
                if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void BuildAlarmGrid()
        {
            _alarmGrid.Columns.Add(new ApcLampColumn());
            _alarmGrid.Columns.Add("time", "시각");
            _alarmGrid.Columns.Add("target", "대상");
            _alarmGrid.Columns.Add("text", "내용");
            _alarmGrid.Columns["text"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _alarmGrid.Columns["text"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        private void Reload()
        {
            _tanks = _source.GetTanks();
            _valves = _source.GetValves();
            _pumps = _source.GetPumps();

            string selected = CurrentValveId();
            _valveGrid.Rows.Clear();
            foreach (var v in _valves)
                _valveGrid.Rows.Add(v.Id, v.Position,
                    new ApcLampValue(v.IsOpen ? StatusKind.Run : StatusKind.Stop, v.IsOpen ? "열림" : "닫힘"),
                    v.LastTime.ToString("HH:mm:ss"), v.Operator);
            _valveGrid.ClearSelection();
            _valveGrid.CurrentCell = null;
            if (selected != null) SelectRow(selected);

            _alarmGrid.Rows.Clear();
            foreach (var a in _source.GetAlarms()) _alarmGrid.Rows.Add(a.Kind, a.Time.ToString("HH:mm:ss"), a.Target, a.Text);

            _diagram.SetData(_tanks, _valves, _pumps);
            ShowVerdict();
            UpdateOperateButton();
        }

        // 판정판: 공정 전체에서 가장 나쁜 항목. 같은 등급이 여럿이면 건수 + 대상.
        private void ShowVerdict()
        {
            var worst = StatusKind.Run;
            foreach (var t in _tanks) worst = ProcessRules.Worse(worst, ProcessRules.Worse(ProcessRules.LevelKind(t.Level), ProcessRules.TempKind(t)));
            if (worst == StatusKind.Run)
            {
                _judge.Status = StatusKind.Run; _judge.Text = "정상"; _judge.SubText = "모든 탱크 레벨·온도 정상";
                return;
            }
            var names = new List<string>();
            TankInfo only = null; bool onlyIsLevel = false;
            foreach (var t in _tanks)
            {
                if (ProcessRules.LevelKind(t.Level) == worst) { names.Add(t.Id + " 레벨"); only = t; onlyIsLevel = true; }
                if (ProcessRules.TempKind(t) == worst) { names.Add(t.Id + " 온도"); only = t; onlyIsLevel = false; }
            }
            string word = worst == StatusKind.Alarm ? "알람" : "경고";
            _judge.Status = worst;
            if (names.Count > 1) { _judge.Text = word + " " + names.Count + "건"; _judge.SubText = string.Join(", ", names.ToArray()); return; }
            _judge.Text = names[0] + " " + word;
            _judge.SubText = onlyIsLevel ? LevelDetail(only, worst) : ProcessRules.Deg(only.Temp) + " (기준 " + ProcessRules.BlendTempMin.ToString("0.0") + "~" + ProcessRules.Deg(ProcessRules.BlendTempMax) + ")";
        }

        private string LevelDetail(TankInfo t, StatusKind kind)
        {
            if (kind != StatusKind.Alarm)
                return ProcessRules.Pct(t.Level) + (t.Level <= ProcessRules.LevelLow ? " (경고 " + ProcessRules.Pct(ProcessRules.LevelLow) + " 이하)" : " (경고 " + ProcessRules.Pct(ProcessRules.LevelWarn) + " 이상)");
            var locked = new List<string>();
            foreach (var v in _valves) if (!v.IsOpen && v.DownstreamTankId == t.Id) locked.Add(v.Id);
            string text = ProcessRules.Pct(t.Level) + " (기준 " + ProcessRules.Pct(ProcessRules.LevelAlarm) + " 이하)";
            return locked.Count == 0 ? text : text + " · " + string.Join("·", locked.ToArray()) + " 열기 잠금";
        }

        private string CurrentValveId()
        {
            var row = _valveGrid.CurrentRow;
            return row == null ? null : Convert.ToString(row.Cells[ColValve].Value);
        }

        private void SelectRow(string valveId)
        {
            foreach (DataGridViewRow r in _valveGrid.Rows)
                if (Convert.ToString(r.Cells[ColValve].Value) == valveId)
                {
                    _valveGrid.CurrentCell = r.Cells[ColValve];
                    r.Selected = true;
                    return;
                }
        }

        private void UpdateOperateButton()
        {
            bool has = CurrentValveId() != null;
            _operate.Enabled = has;
            _reason.Text = has ? "" : PickReason;
        }

        private void OperateSelected()
        {
            string id = CurrentValveId();
            if (id != null) OpenDialog(id);
        }

        private void OpenDialog(string valveId)
        {
            ValveInfo valve = null;
            foreach (var v in _valves) if (v.Id == valveId) valve = v;
            if (valve == null) return;
            using (var dlg = new ValveDialog(valve, _tanks, _pumps))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                try { _source.OperateValve(valveId, dlg.WantOpen, dlg.Operator); }
                catch (InvalidOperationException ex) { ApcDialog.Warn(FindForm(), ex.Message); }
            }
            Reload();
        }
    }
}
