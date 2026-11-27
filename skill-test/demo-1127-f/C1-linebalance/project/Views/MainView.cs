using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using LineBalanceBoard.Data;

namespace LineBalanceBoard.Views
{
    /// <summary>
    /// 라인 밸런스 메인 화면: 병목 판정판 + 예상 생산량 → 배치 변경 버튼 → 공정 흐름 타일 → 막대그래프 | 공정 표.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private readonly IMainViewSource _source;
        private IList<ProcessInfo> _processes;
        private ApcStatusTile[] _tiles;
        private ApcGrid _grid;
        private ApcButton _assign;
        private Label _selectedInfo;
        private int _selected;
        private bool _building;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("라인 밸런스·작업자 배치")
        {
            _source = source;
            _selected = Bottleneck(source.GetProcesses(), source.TaktTenths);
            Build();
        }

        // 가장 긴 사이클 타임 공정의 인덱스(같으면 앞 공정)
        private static int Bottleneck(IList<ProcessInfo> list, int takt)
        {
            int max = 0;
            for (int i = 1; i < list.Count; i++) if (list[i].CycleTenths > list[max].CycleTenths) max = i;
            return max;
        }

        private void Build()
        {
            _building = true;
            foreach (Control c in Body.Controls.Cast<Control>().ToList()) { Body.Controls.Remove(c); c.Dispose(); }
            _processes = _source.GetProcesses();
            int takt = _source.TaktTenths;
            int n = _processes.Count;
            int worst = Bottleneck(_processes, takt);

            var kinds = new StatusKind[n];
            var names = new string[n];
            var seconds = new double[n];
            for (int i = 0; i < n; i++)
            {
                kinds[i] = BalanceRules.Judge(_processes[i].CycleTenths, takt);
                names[i] = _processes[i].Code;
                seconds[i] = _processes[i].CycleTenths / 10.0;
            }

            // 공정 흐름 타일
            _tiles = new ApcStatusTile[n];
            var tileCells = new Control[n];
            for (int i = 0; i < n; i++)
            {
                _tiles[i] = new ApcStatusTile { Text = TileText(_processes[i]), Status = TileKind(_processes[i], kinds[i]), Dock = DockStyle.Fill };
                tileCells[i] = _tiles[i];
            }

            // 표
            _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false, FillRows = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
            _grid.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            _grid.Columns.Add("op", "공정");
            _grid.Columns.Add("work", "작업 내용");
            _grid.Columns.Add("ct", "사이클 타임(초)");
            _grid.Columns.Add("pct", "택트 대비(%)");
            _grid.Columns.Add("worker", "작업자");
            _grid.Columns.Add("qual", "자격");
            foreach (DataGridViewColumn c in _grid.Columns)
                if (c.Index > 0) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            for (int i = 0; i < n; i++) AddRow(_processes[i], kinds[i], takt);

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "정상 95 % 미만"), new ApcLampValue(StatusKind.Warn, "경고 95~100 %"), new ApcLampValue(StatusKind.Alarm, "병목 100 % 초과"));
            var chart = new BarBoard(names, seconds, kinds, takt / 10.0) { Dock = DockStyle.Fill };
            chart.BarClicked += Select;

            _assign = Button("작업자 배치 변경", ApcButtonVariant.Primary, 200);
            _selectedInfo = Caption("");

            // 판정판 줄
            var panel = new ApcJudgePanel { Dock = DockStyle.Fill };
            var perHour = BalanceRules.PerHour(_processes[worst].CycleTenths);
            StatusKind top = kinds[worst];
            int pct = BalanceRules.Percent(_processes[worst].CycleTenths, takt);
            panel.Status = top;
            panel.Text = top == StatusKind.Alarm ? "병목 " + _processes[worst].Code
                : top == StatusKind.Warn ? "병목 위험 " + _processes[worst].Code : "병목 없음";
            panel.SubText = "사이클 타임 " + BalanceRules.Seconds(_processes[worst].CycleTenths) + "초 · 택트 대비 " + pct + " %";
            var card = new ApcValueCard { Title = "라인 예상 생산량", Value = perHour.ToString(), Unit = "대/h", Status = perHour < BalanceRules.PerHour(takt) ? StatusKind.Warn : StatusKind.Run,
                Footer = "택트 기준 " + BalanceRules.PerHour(takt) + " 대/h", Dock = DockStyle.Fill };

            // Fill 먼저, Top 줄은 아래쪽부터
            Body.Controls.Add(FillColumns(new Control[] { Titled("공정별 사이클 타임(초)", chart, legend), Titled("공정별 현황", _grid) }, new float[] { 42f, 58f }));
            Body.Controls.Add(Columns(S(90), tileCells));
            var flowTitle = SectionTitle("공정 흐름 · OP10 → OP80");
            flowTitle.Dock = DockStyle.Top;
            Body.Controls.Add(flowTitle);
            Body.Controls.Add(Row(_assign, _selectedInfo));
            Body.Controls.Add(Columns(S(150), new Control[] { panel, card }, new float[] { 72f, 28f }));

            // 이벤트는 마지막에
            for (int i = 0; i < n; i++)
            {
                int index = i;
                _tiles[i].Click += (s, e) => Select(index);
            }
            _grid.SelectionChanged += (s, e) =>
            {
                if (!_building && _grid.CurrentRow != null) Select(_grid.CurrentRow.Index);
            };
            _assign.Click += (s, e) => OpenAssign();
            _building = false;
            Select(_selected);
        }

        private string TileText(ProcessInfo p)
        {
            var w = _source.FindWorker(p.WorkerId);
            if (w == null) return p.Code + "  미배치";
            return p.Code + "  " + w.Name + " " + QualWord(BalanceRules.Qualification(w, p.Code, _source.Today));
        }

        // 타일 색 = 사이클 타임 판정과 자격 경고(만료·없음 = Warn) 중 나쁜 쪽
        private StatusKind TileKind(ProcessInfo p, StatusKind cycle)
        {
            var w = _source.FindWorker(p.WorkerId);
            if (w == null || BalanceRules.Qualification(w, p.Code, _source.Today) == QualState.Valid) return cycle;
            return cycle == StatusKind.Alarm ? cycle : StatusKind.Warn;
        }

        private static string QualWord(QualState q)
        {
            return q == QualState.Valid ? "유효" : q == QualState.Expired ? "만료" : "없음";
        }

        private void AddRow(ProcessInfo p, StatusKind kind, int takt)
        {
            var w = _source.FindWorker(p.WorkerId);
            QualState q = BalanceRules.Qualification(w, p.Code, _source.Today);
            string qual = w == null ? "" : q == QualState.Valid ? "유효 · " + w.QualExpiry[p.Code] : q == QualState.Expired ? "만료 · " + w.QualExpiry[p.Code] : "없음";
            int r = _grid.Rows.Add(new ApcLampValue(kind, BalanceRules.JudgeWord(kind)), p.Code, p.Work,
                BalanceRules.Seconds(p.CycleTenths), BalanceRules.Percent(p.CycleTenths, takt).ToString(), w == null ? "" : w.Name, qual);
            if (kind != StatusKind.Run)
            {
                var style = _grid.Rows[r].Cells["pct"].Style;
                style.ForeColor = ApcTheme.StatusText(kind);
                style.Font = ApcTheme.GetFont(FontStyle.Bold);
            }
            if (q != QualState.Valid && w != null)
            {
                var style = _grid.Rows[r].Cells["qual"].Style;
                style.ForeColor = ApcTheme.StatusText(StatusKind.Warn);
                style.Font = ApcTheme.GetFont(FontStyle.Bold);
            }
        }

        // 타일·표·버튼이 같은 공정을 가리키게 한다(타일 하나만 Selected).
        private void Select(int index)
        {
            _selected = index;
            for (int i = 0; i < _tiles.Length; i++) _tiles[i].Selected = i == index;
            if (_grid.CurrentRow == null || _grid.CurrentRow.Index != index)
            {
                bool was = _building;
                _building = true;
                _grid.CurrentCell = _grid.Rows[index].Cells["op"];
                _building = was;
            }
            var p = _processes[index];
            var w = _source.FindWorker(p.WorkerId);
            _selectedInfo.Text = "선택 공정  " + p.Code + " " + p.Work + (w == null ? "" : " · " + w.Name);
        }

        private void OpenAssign()
        {
            string code = _processes[_selected].Code;
            using (var dlg = new AssignDialog(_source, code))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK) Build();
            }
        }
    }
}
