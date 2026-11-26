using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using ShaftSpcMonitor.Data;

namespace ShaftSpcMonitor.Views
{
    /// <summary>
    /// SPC 모니터링: 판정판(전 항목 이상 여부) + 다음 측정 → 항목 선택 + 부분군 입력 → 규격/관리 한계 → X̄·R·Cpk·Ppk → X̄·R 관리도 → 이상 이력.
    /// 판정은 SpcAnalysis 하나(타일·판정판·표가 같은 규칙 결과).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int ChartPoints = 25, SoonMinutes = 10;

        private readonly IMainViewSource _source;
        private SpcAnalysis[] _analyses;
        private int _selected;

        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
        private readonly ApcValueCard _next = Card("다음 측정 예정");
        private readonly ApcStatusTile[] _tiles;
        private readonly ApcInfoStrip _info = new ApcInfoStrip();
        private readonly ApcValueCard _xbar = Card("X̄ (부분군 평균)"), _r = Card("R (범위)"), _cpk = Card("Cpk"), _ppk = Card("Ppk");
        private readonly ApcGroup _xGroup = new ApcGroup { Dock = DockStyle.Fill };
        private readonly ApcGroup _rGroup = new ApcGroup { Dock = DockStyle.Fill };
        private readonly ApcTrendChart _xChart = new ApcTrendChart { Dock = DockStyle.Fill };
        private readonly ApcTrendChart _rChart = new ApcTrendChart { Dock = DockStyle.Fill };
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("CNC 축 가공 SPC 모니터링")
        {
            _source = source;
            _tiles = _source.Characteristics.Select((c, i) => NewTile(c.Name, i)).ToArray();
            _info.SetItems("측정 항목", "규격 (LSL ~ USL)", "X̄ 관리 한계 (LCL ~ UCL)", "R 관리 한계 (UCL)", "한계 적용");

            var input = Button("부분군 측정값 입력", ApcButtonVariant.Primary, 240);
            input.Click += (s, e) => InputSubgroup();
            var tileRow = Row(_tiles.Cast<Control>().Concat(new Control[] { input }).ToArray());

            _xGroup.Controls.Add(_xChart);
            _rGroup.Controls.Add(_rChart);
            _xGroup.Margin = new Padding(0, 0, S(12), 0);
            BuildGrid();
            var gridTitle = SectionTitle("이상 규칙 위반 이력");
            gridTitle.Dock = DockStyle.Top;

            // Top 도킹은 나중에 Add한 것이 위 → 이력(Fill) 먼저, 아래 줄부터
            Body.Controls.Add(_grid);
            Body.Controls.Add(gridTitle);
            Body.Controls.Add(Columns(S(280), new Control[] { _xGroup, _rGroup }));
            Body.Controls.Add(Columns(S(110), new Control[] { _xbar, _r, _cpk, _ppk }));
            Body.Controls.Add(Columns(_info.Height, new Control[] { _info }));
            Body.Controls.Add(tileRow);
            Body.Controls.Add(Columns(S(130), new Control[] { _judge, _next }, new[] { 66f, 34f }));
            Refresh2();
        }

        private ApcStatusTile NewTile(string name, int index)
        {
            var t = new ApcStatusTile { Text = name, Size = new Size(S(240), S(ApcLayout.FieldHeight)), Margin = new Padding(0, 0, S(12), S(ApcLayout.RowGap)) };
            t.Click += (s, e) => { _selected = index; Refresh2(); };
            return t;
        }

        private static ApcValueCard Card(string title)
        {
            return new ApcValueCard { Title = title, Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        }

        private void BuildGrid()
        {
            _grid.Columns.Add(new ApcLampColumn());
            _grid.Columns.Add("time", "시각");
            _grid.Columns.Add("item", "측정 항목");
            _grid.Columns.Add("rule", "규칙");
            _grid.Columns.Add("content", "내용");
            _grid.Columns.Add("action", "조치");
            foreach (DataGridViewColumn c in _grid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns["content"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        private void InputSubgroup()
        {
            int item = _selected;
            var c = _source.Characteristics[item];
            using (var dlg = new SubgroupDialog(c, _source.GetSubgroups(item).Count + 1, _source.NextMeasurement))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.AddSubgroup(item, dlg.Values);
            }
            Refresh2();
            var a = _analyses[item];
            if (a.HasOpenViolation)
                ApcDialog.Warn(FindForm(), c.Name + " 이상 규칙 위반 · 측정 재확인 → 설비 점검 → 판정 → 기록 순서로 조치하세요.");
        }

        private void Refresh2()
        {
            _analyses = _source.Characteristics.Select((c, i) => new SpcAnalysis(c, _source.GetSubgroups(i))).ToArray();
            ShowJudge();
            ShowNext();
            for (int i = 0; i < _tiles.Length; i++)
            {
                _tiles[i].Status = _analyses[i].HasOpenViolation ? StatusKind.Alarm : StatusKind.Run;
                _tiles[i].Selected = i == _selected;
            }
            ShowSelected(_analyses[_selected]);
            ShowHistory();
        }

        private void ShowJudge()
        {
            var bad = _analyses.FirstOrDefault(a => a.HasOpenViolation);
            if (bad == null)
            {
                _judge.Text = "정상";
                _judge.Status = StatusKind.Run;
                _judge.SubText = _analyses.Length + "개 항목 모두 관리 한계 안";
                return;
            }
            var v = bad.Violations.First(x => x.Open);
            _judge.Text = "이상 감지";
            _judge.Status = StatusKind.Alarm;
            _judge.SubText = bad.Item.Name + " · " + v.Rule;
        }

        private void ShowNext()
        {
            var left = _source.NextMeasurement - _source.Now;
            int min = (int)Math.Round(Math.Abs(left.TotalMinutes));
            _next.Value = _source.NextMeasurement.ToString("HH:mm");
            _next.Status = left.TotalMinutes < 0 ? StatusKind.Alarm : left.TotalMinutes <= SoonMinutes ? StatusKind.Warn : StatusKind.Run;
            _next.ShowLamp = true;
            string span = min >= 60 ? (min / 60) + "시간 " + (min % 60) + "분" : min + "분";
            _next.Footer = (left.TotalMinutes < 0 ? span + " 지남" : span + " 남음") + " · " + SampleMainViewSource.IntervalHours + "시간마다";
        }

        private void ShowSelected(SpcAnalysis a)
        {
            var c = a.Item;
            string f = "0.000";
            _info.SetValue("측정 항목", c.Name);
            _info.SetValue("규격 (LSL ~ USL)", SpcAnalysis.F(c.Lsl) + " ~ " + SpcAnalysis.F(c.Usl) + " mm");
            _info.SetValue("X̄ 관리 한계 (LCL ~ UCL)", SpcAnalysis.F(c.XLcl) + " ~ " + SpcAnalysis.F(c.XUcl) + " mm");
            _info.SetValue("R 관리 한계 (UCL)", SpcAnalysis.F(c.RUcl) + " mm");
            _info.SetValue("한계 적용", "Phase II 고정");

            var last = a.Last;
            var prev = a.Groups.Count > 1 ? a.Groups[a.Groups.Count - 2] : null;
            bool xOpen = a.Violations.Any(v => v.Open && !v.Rule.StartsWith("R "));
            bool rOpen = last.R > c.RUcl;
            _xbar.Value = last.XBar.ToString(f, CultureInfo.InvariantCulture); _xbar.Unit = "mm";
            _xbar.Status = xOpen ? StatusKind.Alarm : StatusKind.Run;
            _xbar.Footer = prev == null ? "" : "직전 " + SpcAnalysis.F(prev.XBar);
            _r.Value = last.R.ToString(f, CultureInfo.InvariantCulture); _r.Unit = "mm";
            _r.Status = rOpen ? StatusKind.Alarm : StatusKind.Run;
            _r.Footer = prev == null ? "" : "직전 " + SpcAnalysis.F(prev.R);
            ShowCapability(_cpk, a.Cpk);
            ShowCapability(_ppk, a.Ppk);

            var win = a.Groups.Skip(Math.Max(0, a.Groups.Count - ChartPoints)).ToList();
            _xChart.Reference = c.Nominal; _xChart.UpperLimit = c.XUcl; _xChart.LowerLimit = c.XLcl;
            _xChart.SetValues(win.Select(g => g.XBar));
            _rChart.Reference = c.RBar; _rChart.UpperLimit = c.RUcl; _rChart.LowerLimit = 0;
            _rChart.SetValues(win.Select(g => g.R));
            string[] labels = { win[0].Time.ToString("MM-dd HH:mm"), win[win.Count / 2].Time.ToString("MM-dd HH:mm"), win[win.Count - 1].Time.ToString("MM-dd HH:mm") };
            _xChart.XLabels = labels; _rChart.XLabels = labels;
            _xGroup.Text = "X̄ 관리도 · 관리 한계 UCL " + SpcAnalysis.F(c.XUcl) + " / CL " + SpcAnalysis.F(c.Nominal) + " / LCL " + SpcAnalysis.F(c.XLcl);
            _rGroup.Text = "R 관리도 · 관리 한계 UCL " + SpcAnalysis.F(c.RUcl) + " / CL " + SpcAnalysis.F(c.RBar) + " / LCL 0.000";
        }

        /// <summary>Cpk·Ppk 판정 하나: 1.33 이상 양호(Run), 1.00 이상 주의(Warn), 미만 부족(Alarm). 표시는 소수 둘째 자리 기준으로 판정.</summary>
        private static void ShowCapability(ApcValueCard card, double value)
        {
            double v = Math.Round(value, 2);
            card.Value = v.ToString("0.00", CultureInfo.InvariantCulture);
            card.Unit = "";
            card.Status = v >= SpcAnalysis.CpkGood ? StatusKind.Run : v >= SpcAnalysis.CpkCaution ? StatusKind.Warn : StatusKind.Alarm;
            card.Footer = (v >= SpcAnalysis.CpkGood ? "양호" : v >= SpcAnalysis.CpkCaution ? "주의" : "부족") + " · 양호 1.33 이상";
        }

        private void ShowHistory()
        {
            _grid.Rows.Clear();
            var rows = _analyses.SelectMany(a => a.Violations).OrderByDescending(v => v.Time).ThenBy(v => v.ItemName);
            foreach (var v in rows)
                _grid.Rows.Add(v.Open ? StatusKind.Alarm : StatusKind.Info, v.Time.ToString("yyyy-MM-dd HH:mm:ss"), v.ItemName, v.Rule, v.Content, v.Action);
        }
    }
}
