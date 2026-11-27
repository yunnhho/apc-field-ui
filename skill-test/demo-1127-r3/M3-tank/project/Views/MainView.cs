using System;
using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 배합 탱크 공정 흐름 모니터링: 판정판(공정 전체) → 밸브 조작 버튼 → 흐름도(Fill) → 밸브 상태 표 + 오늘 알람 이력.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int ValveRows = 6;
        private const string NoSelection = "밸브 표나 흐름도에서 조작할 밸브를 선택하세요";

        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly FlowBoard _board = new FlowBoard { Dock = DockStyle.Fill };
        private readonly ApcGrid _valveGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _alarmGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcButton _operate = Button("선택한 밸브 조작", ApcButtonVariant.Primary, 200);
        private readonly Label _reason;
        private ProcessSnapshot _snap;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("배합 탱크 공정 흐름")
        {
            _source = source;
            _reason = Caption(NoSelection);

            _valveGrid.Columns.Add("valve", "밸브");
            _valveGrid.Columns.Add("pos", "위치");
            _valveGrid.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            _valveGrid.Columns.Add("time", "마지막 조작");
            _valveGrid.Columns.Add("oper", "조작자");
            foreach (DataGridViewColumn c in _valveGrid.Columns)
                if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            _alarmGrid.Columns.Add(new ApcLampColumn());
            _alarmGrid.Columns.Add("time", "시각");
            _alarmGrid.Columns.Add("target", "대상");
            _alarmGrid.Columns.Add("text", "내용");
            _alarmGrid.Columns["text"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _alarmGrid.Columns["text"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            _reason.AutoSize = true;
            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "열림 · 가동 · 정상"), new ApcLampValue(StatusKind.Stop, "닫힘 · 정지"),
                new ApcLampValue(StatusKind.Warn, "레벨 85 % 이상 · 10 % 이하"), new ApcLampValue(StatusKind.Alarm, "레벨 90 % 초과"));

            var bottom = Columns(TitledHeight(ApcGrid.HeightForRows(ValveRows)),
                new Control[] { Titled("밸브 상태", _valveGrid), Titled("오늘 알람 이력", _alarmGrid) }, new[] { 50f, 50f });
            bottom.Dock = DockStyle.Bottom;

            // Fill 먼저, Bottom, 그다음 Top 줄을 아래쪽 것부터
            Body.Controls.Add(Titled("공정 흐름도", _board, legend));
            Body.Controls.Add(bottom);
            Body.Controls.Add(Row(_operate, _reason));
            Body.Controls.Add(Columns(S(110), new Control[] { _judge }));

            Reload();

            // 이벤트는 마지막에 구독(생성 중 Rows.Add·Reload가 일으키지 않게)
            _valveGrid.SelectionChanged += (s, e) => UpdateOperate();
            _board.ValveClicked += (s, e) => SelectValve(e.ValveId);
            _operate.Click += (s, e) => Operate();
            UpdateOperate();
        }

        private void Reload()
        {
            string selected = SelectedValveId();
            _snap = _source.GetSnapshot();
            _board.SetState(_snap);
            ShowJudge();
            ShowValves();
            ShowAlarms();
            if (selected != null) SelectValve(selected);
        }

        private string SelectedValveId()
        {
            return _valveGrid.SelectedRows.Count == 0 ? null : (string)_valveGrid.SelectedRows[0].Cells[0].Value;
        }

        private void SelectValve(string valveId)
        {
            for (int i = 0; i < _valveGrid.Rows.Count; i++)
            {
                if ((string)_valveGrid.Rows[i].Cells[0].Value != valveId) continue;
                _valveGrid.ClearSelection();
                _valveGrid.CurrentCell = _valveGrid.Rows[i].Cells[0];
                _valveGrid.Rows[i].Selected = true;
                return;
            }
        }

        private void ShowJudge()
        {
            var alarms = new List<string>(); var warns = new List<string>();
            foreach (TankInfo t in _snap.Tanks)
            {
                StatusKind k = ProcessRules.LevelKind(t.Level);
                if (k == StatusKind.Alarm) alarms.Add(t.Id); else if (k == StatusKind.Warn) warns.Add(t.Id);
                if (ProcessRules.TempKind(t.Id, t.Temp) == StatusKind.Alarm) alarms.Add(t.Id + "온도");
            }
            var blocked = new List<string>();
            foreach (ValveInfo v in _snap.Valves)
                if (!v.IsOpen && ProcessRules.BlockReason(v, _snap, true) != null) blocked.Add(v.Id);

            if (alarms.Count > 0)
            {
                TankInfo first = _snap.Tank(alarms[0].Substring(0, 2));
                _judge.Status = StatusKind.Alarm;
                _judge.Text = alarms.Count == 1 ? alarms[0] + (alarms[0].Length > 2 ? " 알람" : " 레벨 알람") : "알람 " + alarms.Count + "건 · " + alarms[0];
                _judge.SubText = alarms[0].Length > 2
                    ? ProcessRules.Temp(first.Temp) + " ℃ (허용 " + ProcessRules.Temp(ProcessRules.BlendTempMin) + " ~ " + ProcessRules.Temp(ProcessRules.BlendTempMax) + ")"
                    : first.Level + " % (상한 " + ProcessRules.LevelAlarm + ")" + (blocked.Count > 0 ? " · " + string.Join(" ", blocked) + " 열기 막힘" : "");
            }
            else if (warns.Count > 0)
            {
                TankInfo first = _snap.Tank(warns[0]);
                _judge.Status = StatusKind.Warn;
                _judge.Text = warns[0] + " 레벨 경고";
                _judge.SubText = first.Level + " %" + (warns.Count > 1 ? " · 외 " + (warns.Count - 1) + "건" : "");
            }
            else
            {
                _judge.Status = StatusKind.Run;
                _judge.Text = "정상 운전";
                _judge.SubText = "";
            }
        }

        private void ShowValves()
        {
            _valveGrid.Rows.Clear();
            foreach (ValveInfo v in _snap.Valves)
                _valveGrid.Rows.Add(v.Id, v.Position, new ApcLampValue(v.IsOpen ? StatusKind.Run : StatusKind.Stop, v.IsOpen ? "열림" : "닫힘"),
                    v.LastTime.ToString("HH:mm:ss"), v.OperatorId);
        }

        private void ShowAlarms()
        {
            _alarmGrid.Rows.Clear();
            foreach (AlarmEntry a in _source.GetTodayAlarms())
                _alarmGrid.Rows.Add(a.Kind, a.Time.ToString("HH:mm:ss"), a.Target, a.Text);
        }

        private void UpdateOperate()
        {
            string id = SelectedValveId();
            _operate.Enabled = id != null;
            _reason.Text = id == null ? NoSelection : "";
        }

        private void Operate()
        {
            string id = SelectedValveId();
            if (id == null) return;
            using (var dlg = new ValveDialog(_snap, id))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.OperateValve(dlg.ValveId, dlg.WantOpen, dlg.OperatorId);
            }
            Reload();
        }
    }
}
