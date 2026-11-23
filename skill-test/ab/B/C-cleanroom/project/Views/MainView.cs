using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CleanroomMonitor.Data;

namespace CleanroomMonitor.Views
{
    /// <summary>
    /// 클린룸 환경 모니터링: 구역 센서 표(기준 이탈 셀 강조) + 선택 구역 온습도 추이. 설정은 타이틀바 톱니.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private static readonly string[] TrendLabels = { "10:20", "10:50", "11:20" };

        private readonly IMainViewSource _source;
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Top, ReadOnly = true, ClearInitialSelection = false };
        private readonly ApcGroup _tempGroup = new ApcGroup { Dock = DockStyle.Fill };
        private readonly ApcGroup _humiGroup = new ApcGroup { Dock = DockStyle.Fill };
        private readonly ApcTrendChart _tempChart = new ApcTrendChart { Dock = DockStyle.Fill, XLabels = TrendLabels };
        private readonly ApcTrendChart _humiChart = new ApcTrendChart { Dock = DockStyle.Fill, XLabels = TrendLabels };
        private readonly HashSet<string> _pushedAlarms = new HashSet<string>();
        private ZoneReading[] _zones;
        private MonitorSettings _settings;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("클린룸 환경 모니터링")
        {
            _source = source;
            UseSettingsButton((s, e) => OpenSettings());

            BuildGrid();
            _tempGroup.Controls.Add(_tempChart);
            _humiGroup.Controls.Add(_humiChart);
            _humiGroup.Margin = Padding.Empty;
            _tempGroup.Margin = new Padding(0, 0, S(ApcLayout.RowGap), 0);
            var charts = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            charts.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            charts.Controls.Add(_tempGroup, 0, 0);
            charts.Controls.Add(_humiGroup, 1, 0);

            // Fill 먼저, Top 줄은 아래쪽 것부터
            Body.Controls.Add(charts);
            Body.Controls.Add(new Panel { Dock = DockStyle.Top, Height = S(ApcLayout.RowGap), BackColor = Color.Transparent });
            Body.Controls.Add(_grid);
            Body.Controls.Add(Row(SectionTitle("구역 센서 현황")));

            Reload();
            _grid.SelectionChanged += (s, e) => ShowTrend();
            _grid.CurrentCell = _grid.Rows[0].Cells[0];
            _grid.Rows[0].Selected = true;
            ShowTrend();
        }

        private void BuildGrid()
        {
            _grid.Columns.Add("zone", "구역");
            _grid.Columns.Add(new ApcLampColumn { Name = "status", HeaderText = "상태" });
            _grid.Columns.Add("temp", "온도(℃)");
            _grid.Columns.Add("humi", "습도(%RH)");
            _grid.Columns.Add("part", "파티클 0.5µm(개/ft³)");
            _grid.Columns.Add("pres", "차압(Pa)");
            _grid.Columns["zone"].Width = S(110);
            _grid.Columns["status"].Width = S(440);
            foreach (var n in new[] { "temp", "humi", "part", "pres" })
                _grid.Columns[n].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Height = ApcGrid.HeightForRows(8);
        }

        /// <summary>데이터를 다시 읽어 표·헤더·알람바를 채운다(설정 저장 뒤에도).</summary>
        private void Reload()
        {
            _zones = _source.GetZones();
            _settings = _source.GetSettings();
            int selected = _grid.CurrentRow == null ? 0 : _grid.CurrentRow.Index;
            _grid.Rows.Clear();
            int run = 0, alarm = 0, warn = 0, down = 0;
            for (int i = 0; i < _zones.Length; i++)
            {
                var z = _zones[i];
                var j = ZoneJudge.Judge(z, _settings.Limits[i]);
                _grid.Rows.Add(z.Name, new ApcLampValue(j.Kind, j.StatusText),
                    CellText(z.Temperature, ZoneJudge.T), CellText(z.Humidity, ZoneJudge.T), CellText(z.Particles, ZoneJudge.P), CellText(z.Pressure, ZoneJudge.T));
                var cells = _grid.Rows[i].Cells;
                Mark(cells[2], j.Temp.Kind); Mark(cells[3], j.Humi.Kind); Mark(cells[4], j.Particle.Kind); Mark(cells[5], j.Pressure.Kind);
                if (j.Kind == StatusKind.Run) run++;
                else if (j.Kind == StatusKind.Alarm) alarm++;
                else if (j.Kind == StatusKind.Warn) warn++;
                else down++;
                if (j.AlarmMessage != null && _pushedAlarms.Add(j.AlarmMessage)) Alarm.Push(j.AlarmMessage);
            }
            if (selected < _grid.Rows.Count) { _grid.CurrentCell = _grid.Rows[selected].Cells[0]; _grid.Rows[selected].Selected = true; }

            var parts = new List<string> { "전체", "정상 " + run };
            if (alarm > 0) parts.Add("알람 " + alarm);
            if (warn > 0) parts.Add("경고 " + warn);
            if (down > 0) parts.Add("통신 끊김 " + down);
            Header.Status = alarm > 0 ? StatusKind.Alarm : warn > 0 ? StatusKind.Warn : down > 0 ? StatusKind.Stop : StatusKind.Run;
            Header.StatusText = string.Join(" · ", parts.ToArray());
        }

        private static string CellText(double? v, System.Func<double, string> fmt) { return v.HasValue ? fmt(v.Value) : ""; }

        private static void Mark(DataGridViewCell cell, StatusKind kind)
        {
            if (kind == StatusKind.Run) return;
            cell.Style.ForeColor = ApcTheme.Status(kind);
            cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
        }

        private void ShowTrend()
        {
            if (_grid.CurrentRow == null) return;
            int i = _grid.CurrentRow.Index;
            var z = _zones[i];
            var lim = _settings.Limits[i];
            string down = z.Connected ? "" : " · 통신 끊김";
            _tempGroup.Text = z.Name + " 온도 (℃)" + down;
            _humiGroup.Text = z.Name + " 습도 (%RH)" + down;
            _tempChart.UpperLimit = lim.TempHigh; _tempChart.LowerLimit = lim.TempLow;
            _humiChart.UpperLimit = lim.HumiHigh; _humiChart.LowerLimit = lim.HumiLow;
            _tempChart.SetValues(z.TemperatureTrend);
            _humiChart.SetValues(z.HumidityTrend);
        }

        /// <summary>설정 진입: 관리자 확인 → 설정 dialog(샘플은 비밀번호 검사 생략).</summary>
        private void OpenSettings()
        {
            var owner = FindForm();
            using (var pw = new AdminPasswordDialog())
                if (pw.ShowDialog(owner) != DialogResult.OK) return;
            using (var dlg = new SettingsDialog(_zones, _settings))
            {
                if (dlg.ShowDialog(owner) != DialogResult.OK) return;
                _source.SaveSettings(dlg.Result);
            }
            Reload();
            ShowTrend();
        }
    }
}
