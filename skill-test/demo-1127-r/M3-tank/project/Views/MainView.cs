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
        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel();
        private readonly FlowDiagram _diagram = new FlowDiagram { Dock = DockStyle.Fill };
        private readonly ApcGrid _valveGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, FillRows = true };
        private readonly ApcGrid _alarmGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcButton _operate = Button("밸브 조작", ApcButtonVariant.Primary);
        private readonly Label _operateReason = Caption("");

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("배합 탱크 공정 흐름 모니터링")
        {
            _source = source;
            BuildValveGrid();
            BuildAlarmGrid();

            var legend = new ApcStatusLegend();
            legend.SetItems(
                new ApcLampValue(StatusKind.Info, "정상 레벨"),
                new ApcLampValue(StatusKind.Warn, "경고 (85 % 이상 · 10 % 이하)"),
                new ApcLampValue(StatusKind.Alarm, "알람 (90 % 초과)"),
                new ApcLampValue(StatusKind.Run, "열림 · 가동"),
                new ApcLampValue(StatusKind.Stop, "닫힘 · 정지"));

            _operate.Click += delegate { OperateSelected(); };
            _diagram.ValveClicked += SelectValve;
            _valveGrid.SelectionChanged += delegate { UpdateSelection(); };

            Body.Controls.Add(Titled("오늘 알람 이력", _alarmGrid));
            Body.Controls.Add(Columns(S(400), new Control[]
            {
                Titled("공정 흐름도", _diagram, legend),
                Titled("밸브 상태", _valveGrid, _operateReason, _operate)
            }, new[] { 62f, 38f }));
            Body.Controls.Add(Columns(S(100), new Control[] { _judge }));

            Reload(-1);
        }

        private void BuildValveGrid()
        {
            _valveGrid.Columns.Add("id", "밸브");
            _valveGrid.Columns.Add("pos", "위치");
            _valveGrid.Columns.Add(new ApcLampColumn { Name = "state", HeaderText = "상태" });
            _valveGrid.Columns.Add("time", "마지막 조작 시각");
            _valveGrid.Columns.Add("op", "조작자");
            foreach (string name in new[] { "id", "pos", "time", "op" })
                _valveGrid.Columns[name].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void BuildAlarmGrid()
        {
            _alarmGrid.Columns.Add(new ApcLampColumn { Name = "kind", HeaderText = "상태" });
            _alarmGrid.Columns.Add("time", "시각");
            _alarmGrid.Columns.Add("src", "대상");
            _alarmGrid.Columns.Add("text", "내용");
            _alarmGrid.Columns["text"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _alarmGrid.Columns["text"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        /// <summary>데이터를 다시 읽어 판정판·흐름도·표를 갱신한다. selectIndex = 다시 선택할 밸브 행(-1 = 선택 없음).</summary>
        private void Reload(int selectIndex)
        {
            IList<TankInfo> tanks = _source.GetTanks();
            IList<PumpInfo> pumps = _source.GetPumps();
            IList<ValveInfo> valves = _source.GetValves();

            StatusKind kind; string text, sub;
            ProcessRules.Judge(tanks, valves, pumps, out kind, out text, out sub);
            _judge.Status = kind; _judge.Text = text; _judge.SubText = sub;

            _diagram.Update(tanks, valves, pumps);

            _valveGrid.Rows.Clear();
            foreach (ValveInfo v in valves)
            {
                _valveGrid.Rows.Add(v.Id, v.Position,
                    new ApcLampValue(v.Open ? StatusKind.Run : StatusKind.Stop, v.Open ? "열림" : "닫힘"),
                    v.LastTime.HasValue ? v.LastTime.Value.ToString("HH:mm:ss") : "", v.Operator);
            }
            _alarmGrid.Rows.Clear();
            foreach (AlarmEntry a in _source.GetAlarms())
                _alarmGrid.Rows.Add(a.Kind, a.Time.ToString("HH:mm:ss"), a.Source, a.Text);

            if (selectIndex >= 0 && selectIndex < _valveGrid.Rows.Count)
                _valveGrid.CurrentCell = _valveGrid.Rows[selectIndex].Cells[0];
            UpdateSelection();
        }

        private void SelectValve(string valveId)
        {
            foreach (DataGridViewRow row in _valveGrid.Rows)
                if ((string)row.Cells[0].Value == valveId) { _valveGrid.CurrentCell = row.Cells[0]; return; }
        }

        private void UpdateSelection()
        {
            DataGridViewRow row = _valveGrid.CurrentRow;
            _operate.Enabled = row != null;
            _operateReason.Text = row == null ? "밸브를 선택하세요" : "";
            _diagram.SetSelected(row == null ? "" : (string)row.Cells[0].Value);
        }

        private void OperateSelected()
        {
            DataGridViewRow row = _valveGrid.CurrentRow;
            if (row == null) return;
            int index = row.Index;
            ValveInfo valve = _source.GetValves()[index];
            using (var dlg = new ValveOperationDialog(valve, _source.GetTanks(), _source.GetPumps()))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.OperateValve(valve.Id, dlg.OpenRequested, dlg.OperatorId, DateTime.Now);
            }
            Reload(index);
        }
    }
}
