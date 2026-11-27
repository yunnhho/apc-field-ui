using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>메인 화면: 판정판(전 공정 최악 1건) → 밸브 조작 버튼 → 공정 흐름도 | 밸브 상태 표 + 오늘 알람 이력.</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const string Title = "배합 탱크 공정 흐름 모니터링";
        private const float DiagramPercent = 62f, TablesPercent = 38f;

        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly FlowBoard _board = new FlowBoard { Dock = DockStyle.Fill };
        private readonly ApcGrid _valveGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false };
        private readonly ApcGrid _alarmGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcButton _operate = Button("밸브 조작", ApcButtonVariant.Primary, 160);
        private readonly Label _reason = Caption("");
        private IList<TankInfo> _tanks;
        private IList<ValveInfo> _valves;
        private IList<PumpInfo> _pumps;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base(Title)
        {
            _source = source;
            BuildValveGrid();
            BuildAlarmGrid();

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "열림·가동"), new ApcLampValue(StatusKind.Stop, "닫힘·정지"),
                new ApcLampValue(StatusKind.Info, "레벨 정상"), new ApcLampValue(StatusKind.Warn, "레벨 경고"), new ApcLampValue(StatusKind.Alarm, "레벨 알람"));

            var valveCell = Titled("밸브 상태", _valveGrid);
            valveCell.Dock = DockStyle.Top;
            valveCell.Height = TitledHeight(ApcGrid.HeightForRows(6));
            var spacer = new Panel { Dock = DockStyle.Top, Height = S(12) };
            var right = new Panel { Dock = DockStyle.Fill };
            right.Controls.Add(Titled("오늘 알람 이력", _alarmGrid));
            right.Controls.Add(spacer);
            right.Controls.Add(valveCell);

            Body.Controls.Add(FillColumns(new Control[] { Titled("공정 흐름도", _board, legend), right }, new[] { DiagramPercent, TablesPercent }));
            Body.Controls.Add(Row(_operate, _reason));
            Body.Controls.Add(Columns(S(110), new Control[] { _judge }));

            Reload();
            SelectValve("V3");
            UpdateOperateButton();

            _valveGrid.SelectionChanged += (s, e) => UpdateOperateButton();
            _board.ValveClicked += SelectValve;
            _operate.Click += (s, e) => OpenOperate();
        }

        private void BuildValveGrid()
        {
            _valveGrid.Columns.Add("id", "밸브");
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
            string selected = SelectedValveId();
            _tanks = _source.GetTanks();
            _valves = _source.GetValves();
            _pumps = _source.GetPumps();

            _board.Bind(_tanks, _valves, _pumps);
            var j = FlowRules.Judge(_tanks, _valves);
            _judge.Status = j.Kind; _judge.Text = j.Text; _judge.SubText = j.Sub;

            _valveGrid.Rows.Clear();
            foreach (var v in _valves)
                _valveGrid.Rows.Add(v.Id, v.Position, new ApcLampValue(v.IsOpen ? StatusKind.Run : StatusKind.Stop, v.IsOpen ? "열림" : "닫힘"),
                    v.LastTime.ToString("HH:mm:ss"), v.OperatorId);

            _alarmGrid.Rows.Clear();
            foreach (var a in _source.GetAlarms())
                _alarmGrid.Rows.Add(a.Kind, a.Time.ToString("HH:mm:ss"), a.Target, a.Text);

            if (selected != null) SelectValve(selected);
        }

        private string SelectedValveId()
        {
            return _valveGrid.CurrentRow == null ? null : (string)_valveGrid.CurrentRow.Cells[0].Value;
        }

        private void SelectValve(string id)
        {
            foreach (DataGridViewRow row in _valveGrid.Rows)
                if ((string)row.Cells[0].Value == id)
                {
                    _valveGrid.CurrentCell = row.Cells[0];
                    row.Selected = true;
                    return;
                }
        }

        private void UpdateOperateButton()
        {
            string id = SelectedValveId();
            _operate.Enabled = id != null;
            _reason.Text = id == null ? "표나 흐름도에서 밸브를 선택하세요" : "선택 밸브 " + id;
        }

        private void OpenOperate()
        {
            string id = SelectedValveId();
            if (id == null) return;
            ValveInfo valve = null;
            foreach (var v in _valves) if (v.Id == id) valve = v;
            if (valve == null) return;
            using (var dlg = new ValveDialog(valve, _tanks, _pumps))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.OperateValve(valve.Id, dlg.TargetOpen, dlg.OperatorId);
            }
            Reload();
        }
    }
}
