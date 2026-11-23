using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CleanroomMonitor.Data;

namespace CleanroomMonitor.Views
{
    /// <summary>메인 화면: 구역 센서 표 + 선택 구역 온습도 추이. 데이터는 IMainViewSource에서만 받는다.</summary>
    internal sealed class MainView : ApcTemplateView
    {
        public const string ScreenTitle = "클린룸 환경 모니터링";
        private const int ColStatus = 0, ColTemp = 2, ColHumi = 3, ColParticle = 4, ColDiff = 5;
        private static readonly string[] TrendLabels = { "11:00", "11:30", "12:00" };

        private readonly IMainViewSource _source;
        private readonly ApcGrid _grid = new ApcGrid { ReadOnly = true, ClearInitialSelection = false, MultiSelect = false };
        private readonly ApcGroup _tempGroup = new ApcGroup { Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        private readonly ApcGroup _humiGroup = new ApcGroup { Dock = DockStyle.Fill, Margin = new Padding(0) };
        private readonly ApcTrendChart _tempChart = new ApcTrendChart { Dock = DockStyle.Fill };
        private readonly ApcTrendChart _humiChart = new ApcTrendChart { Dock = DockStyle.Fill };
        private IList<ZoneReading> _readings;
        private IList<ZoneLimits> _limits;
        private ZoneJudgement[] _judged;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base(ScreenTitle)
        {
            _source = source;
            _tempChart.XLabels = TrendLabels;
            _humiChart.XLabels = TrendLabels;
            _tempGroup.Controls.Add(_tempChart);
            _humiGroup.Controls.Add(_humiChart);

            var charts = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            charts.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            charts.Controls.Add(_tempGroup, 0, 0);
            charts.Controls.Add(_humiGroup, 1, 0);

            BuildColumns();
            var title = SectionTitle("구역별 센서 현황");
            title.Dock = DockStyle.Top;
            var trendTitle = SectionTitle("선택 구역 추이 (최근 1시간)");
            trendTitle.Dock = DockStyle.Top;

            Body.Controls.Add(charts);   // Fill 먼저
            Body.Controls.Add(trendTitle);
            Body.Controls.Add(Columns(ApcGrid.HeightForRows(8), new Control[] { _grid }));
            Body.Controls.Add(title);

            UseSettingsButton((s, e) => OpenSettings());
            _grid.SelectionChanged += (s, e) => ShowTrend();
            Reload(-1);
        }

        private void BuildColumns()
        {
            _grid.Columns.Add(new ApcLampColumn { HeaderText = "상태", Width = S(150), SortMode = DataGridViewColumnSortMode.NotSortable });
            AddText("구역", 100);
            AddText("온도(℃)", 130);
            AddText("습도(%RH)", 140);
            AddText("파티클 0.5µm(개/ft³)", 240);
            AddText("차압(Pa)", 130);
            var content = AddText("내용", 0);
            content.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            content.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        private DataGridViewColumn AddText(string header, int width)
        {
            var c = new DataGridViewTextBoxColumn { HeaderText = header, SortMode = DataGridViewColumnSortMode.NotSortable };
            if (width > 0) c.Width = S(width);
            _grid.Columns.Add(c);
            return c;
        }

        /// <summary>표·헤더·알람바를 다시 채운다. selected = 선택할 행(-1이면 첫 알람 구역, 없으면 첫 행).</summary>
        private void Reload(int selected)
        {
            _readings = _source.GetReadings();
            _limits = _source.GetLimits();
            _judged = new ZoneJudgement[_readings.Count];
            _grid.Rows.Clear();
            for (int i = 0; i < _readings.Count; i++)
            {
                _judged[i] = ZoneJudge.Evaluate(_readings[i], _limits[i]);
                AddRow(_readings[i], _judged[i]);
            }
            ShowSummary();
            if (selected < 0) selected = FirstAlarmIndex();
            _grid.CurrentCell = _grid.Rows[selected].Cells[1];
            ShowTrend();
        }

        private void AddRow(ZoneReading r, ZoneJudgement j)
        {
            int row = _grid.Rows.Add(new ApcLampValue(j.Status, j.StatusText), r.Zone,
                r.Online ? r.Temp.ToString("0.0") : "",
                r.Online ? r.Humi.ToString("0.0") : "",
                r.Online ? r.Particle.ToString("#,0") : "",
                r.Online ? r.DiffPa.ToString("0.0") : "",
                j.Reason);
            MarkIfOut(row, ColTemp, j.Temp);
            MarkIfOut(row, ColHumi, j.Humi);
            MarkIfOut(row, ColParticle, j.Particle);
            MarkIfOut(row, ColDiff, j.Diff);
        }

        private void MarkIfOut(int row, int col, StatusKind kind)
        {
            if (kind != StatusKind.Warn && kind != StatusKind.Alarm) return;
            var style = _grid.Rows[row].Cells[col].Style;
            style.ForeColor = ApcTheme.Status(kind);
            style.Font = ApcTheme.GetFont(FontStyle.Bold);
        }

        private int FirstAlarmIndex()
        {
            for (int i = 0; i < _judged.Length; i++)
                if (_judged[i].Status == StatusKind.Alarm) return i;
            return 0;
        }

        /// <summary>헤더 요약: 전체 · 정상 → 알람 → 경고 → 통신 끊김. 알람바는 알람 구역만.</summary>
        private void ShowSummary()
        {
            int normal = 0, alarm = 0, warn = 0, lost = 0;
            foreach (var j in _judged)
            {
                if (j.Status == StatusKind.Alarm) alarm++;
                else if (j.Status == StatusKind.Warn) warn++;
                else if (j.Status == StatusKind.Stop) lost++;
                else normal++;
            }
            var parts = new List<string> { "전체", "정상 " + normal };
            if (alarm > 0) parts.Add("알람 " + alarm);
            if (warn > 0) parts.Add("경고 " + warn);
            if (lost > 0) parts.Add("통신 끊김 " + lost);
            Header.StatusText = string.Join(" · ", parts.ToArray());
            Header.Status = alarm > 0 ? StatusKind.Alarm : warn > 0 ? StatusKind.Warn : lost > 0 ? StatusKind.Stop : StatusKind.Run;
            for (int i = 0; i < _judged.Length; i++)
                if (_judged[i].Status == StatusKind.Alarm) Alarm.Push(_readings[i].Zone + ": " + _judged[i].Reason);
        }

        private void ShowTrend()
        {
            var row = _grid.CurrentRow;
            if (row == null) return;
            int i = row.Index;
            var r = _readings[i];
            var l = _limits[i];
            string lost = r.Online ? "" : " · 통신 끊김";
            _tempGroup.Text = r.Zone + " 온도 추이 (℃)" + lost;
            _humiGroup.Text = r.Zone + " 습도 추이 (%RH)" + lost;
            _tempChart.UpperLimit = l.TempHigh;
            _tempChart.LowerLimit = l.TempLow;
            _humiChart.UpperLimit = l.HumiHigh;
            _humiChart.LowerLimit = l.HumiLow;
            _tempChart.SetValues(r.TempTrend);
            _humiChart.SetValues(r.HumiTrend);
        }

        private void OpenSettings()
        {
            using (var dlg = new SettingsDialog(_source.GetLimits(), _source.GetSaveIntervalSec()))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.SaveSettings(dlg.Limits, dlg.SaveIntervalSec);
                int keep = _grid.CurrentRow == null ? -1 : _grid.CurrentRow.Index;
                Alarm.AcknowledgeCurrent();
                Reload(keep);
            }
        }
    }
}
