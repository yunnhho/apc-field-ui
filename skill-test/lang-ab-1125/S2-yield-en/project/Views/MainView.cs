using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>
    /// Final Test 수율·Bin 모니터링. 맨 위: 지금 조치가 필요한 테스터(판정판) + 선택 테스터 수율 카드 → 테스터 타일 →
    /// 현재 Lot → Hard/Soft Bin·추이 → Lot 결과 이력. 타일을 누르면 아래가 그 테스터로 바뀐다.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private static readonly string[] HardBinNames = { "양품", "전류 이상", "Open/Short", "Func 불량", "속도 미달", "Retention", "온도 불량", "기타" };

        private readonly IMainViewSource _source;
        private readonly TesterSnapshot[] _testers;
        private readonly ApcStatusTile[] _tiles;
        private readonly ApcJudgePanel _panel = new ApcJudgePanel();
        private readonly ApcValueCard _firstCard = new ApcValueCard { Title = "최초 수율", Unit = "%", Dock = DockStyle.Fill };
        private readonly ApcValueCard _finalCard = new ApcValueCard { Title = "최종 수율", Unit = "%", ShowLamp = true, Dock = DockStyle.Fill };
        private readonly ApcInfoStrip _lotStrip = new ApcInfoStrip { Dock = DockStyle.Fill };
        private readonly ApcGrid _hardGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _softGrid = new ApcGrid { Dock = DockStyle.Top, ReadOnly = true };
        private readonly Label _softNote = Caption("");
        private readonly ApcTrendChart _chart = new ApcTrendChart { Dock = DockStyle.Fill, LowerLimit = YieldRule.FinalYieldCriterion, XLabels = new[] { "-11 Lot", "-6 Lot", "현재" } };
        private readonly Panel _lotSection, _trendSection;
        private readonly ApcButton _ocapButton = Button("미달 Lot 처리", ApcButtonVariant.Primary, 150);
        private readonly Label _ocapReason = Caption("");
        private int _selected;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("Final Test 수율 모니터링")
        {
            _source = source;
            _testers = source.GetTesters();
            _selected = Math.Max(0, Array.FindIndex(_testers, t => IsAlarm(t)));   // 처음엔 조치가 필요한 테스터

            _tiles = new ApcStatusTile[_testers.Length];
            var tileCells = new Control[_testers.Length];
            for (int i = 0; i < _testers.Length; i++)
            {
                int index = i;
                var tile = new ApcStatusTile { Dock = DockStyle.Fill };
                tile.Click += (s, e) => Select(index);
                _tiles[i] = tile;
                tileCells[i] = tile;
            }

            _lotStrip.SetItems("Lot ID", "고객", "제품", "테스트 프로그램", "온도(℃)", "투입 수량", "진행 수량");
            _ocapButton.Click += (s, e) => OpenOcap();

            _hardGrid.Columns.Add("bin", "Hard Bin");
            _hardGrid.Columns.Add("name", "분류");
            _hardGrid.Columns.Add("qty", "수량");
            _hardGrid.Columns.Add("rate", "비율(%)");
            FillAll(_hardGrid);

            _softGrid.Columns.Add("bin", "Soft Bin");
            _softGrid.Columns.Add("name", "불량 원인");
            _softGrid.Columns.Add("hard", "Hard Bin");
            _softGrid.Columns.Add("qty", "수량");
            _softGrid.Columns.Add("rate", "비율(%)");
            FillAll(_softGrid);
            _softGrid.Height = ApcGrid.HeightForRows(5);
            _softNote.Dock = DockStyle.Top;
            var softPanel = new Panel();
            softPanel.Controls.Add(_softNote);
            softPanel.Controls.Add(_softGrid);   // Top 도킹: 나중에 Add한 표가 위

            _lotSection = Titled("현재 Lot", _lotStrip, _ocapReason, _ocapButton);
            _trendSection = Titled("최근 Lot 최종 수율 추이 (%, 빨간 점선 = 기준)", _chart);
            var hardSection = Titled("Hard Bin 분포", _hardGrid);
            var softSection = Titled("Soft Bin 상위 5", softPanel);

            Body.Controls.Add(BuildHistory());
            Body.Controls.Add(Columns(S(40) + ApcGrid.HeightForRows(8), new Control[] { hardSection, softSection, _trendSection }, new[] { 34f, 33f, 33f }));
            Body.Controls.Add(Columns(S(88), new Control[] { _lotSection }));
            Body.Controls.Add(Columns(S(64), tileCells));
            Body.Controls.Add(Columns(S(100), new Control[] { _panel, _firstCard, _finalCard }, new[] { 50f, 25f, 25f }));

            Reload();
        }

        // 한 판정 함수: 타일·카드·판정판 모두 이것으로 상태를 정한다
        private static bool IsAlarm(TesterSnapshot t)
        {
            return t.Lot != null && YieldRule.JudgeFinal(t.Lot.FinalYield) == StatusKind.Alarm;
        }

        private static void FillAll(ApcGrid grid)
        {
            foreach (DataGridViewColumn c in grid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void Select(int index)
        {
            _selected = index;
            Reload();
        }

        // 화면 값을 데이터에서 다시 채운다(Refresh 이름은 Control 멤버와 겹쳐 쓰지 않는다)
        private void Reload()
        {
            for (int i = 0; i < _testers.Length; i++)
            {
                var t = _testers[i];
                _tiles[i].Selected = i == _selected;
                if (t.Lot == null) { _tiles[i].Status = StatusKind.Stop; _tiles[i].Text = t.Name + "  대기"; }
                else { _tiles[i].Status = YieldRule.JudgeFinal(t.Lot.FinalYield) == StatusKind.Alarm ? StatusKind.Alarm : StatusKind.Run; _tiles[i].Text = t.Name + "  " + YieldRule.Pct(t.Lot.FinalYield) + " %"; }
            }

            ReloadPanel();
            var cur = _testers[_selected];
            var lot = cur.Lot;
            _lotSection.Text = cur.Name + " 현재 Lot";
            _trendSection.Text = cur.Name + " 최근 Lot 최종 수율 추이 (%, 빨간 점선 = 기준)";
            _hardGrid.Rows.Clear();
            _softGrid.Rows.Clear();

            if (lot == null)
            {
                _firstCard.Value = ""; _firstCard.Footer = ""; _firstCard.Status = StatusKind.Stop;
                _finalCard.Value = ""; _finalCard.Footer = ""; _finalCard.Status = StatusKind.Stop;
                foreach (var n in new[] { "Lot ID", "고객", "제품", "테스트 프로그램", "온도(℃)", "투입 수량", "진행 수량" }) _lotStrip.SetValue(n, "");
                _chart.SetValues(new double[0]);
                _softNote.Text = "";
                SetOcap(false, "Lot 없음");
                return;
            }

            StatusKind verdict = YieldRule.JudgeFinal(lot.FinalYield);
            _firstCard.Value = YieldRule.Pct(lot.FirstPassYield);
            _firstCard.Status = StatusKind.Run;
            _firstCard.Footer = "양품 " + YieldRule.Qty(lot.FirstPassGood) + " / " + YieldRule.Qty(lot.TestedQty);
            _finalCard.Value = YieldRule.Pct(lot.FinalYield);
            _finalCard.Status = verdict;
            _finalCard.Footer = "양품 " + YieldRule.Qty(lot.FinalGood) + " / " + YieldRule.Qty(lot.TestedQty);

            _lotStrip.SetValue("Lot ID", lot.LotId);
            _lotStrip.SetValue("고객", lot.CustomerCode);
            _lotStrip.SetValue("제품", lot.Product);
            _lotStrip.SetValue("테스트 프로그램", lot.ProgramVersion);
            _lotStrip.SetValue("온도(℃)", YieldRule.Temp(lot.TemperatureC));
            _lotStrip.SetValue("투입 수량", YieldRule.Qty(lot.InputQty));
            _lotStrip.SetValue("진행 수량", YieldRule.Qty(lot.TestedQty));

            for (int b = 0; b < lot.HardBin.Length; b++)
                _hardGrid.Rows.Add("Bin " + (b + 1), HardBinNames[b], YieldRule.Qty(lot.HardBin[b]), YieldRule.Pct(100.0 * lot.HardBin[b] / lot.TestedQty));
            foreach (var s in lot.SoftTop)
                _softGrid.Rows.Add(s.SoftBin.ToString("00"), s.Name, "Bin " + s.HardBin, YieldRule.Qty(s.Qty), YieldRule.Pct(100.0 * s.Qty / lot.TestedQty));
            _softNote.Text = SoftBinNote(lot.SoftTop[0], cur);

            _chart.SetValues(lot.YieldTrend);
            SetOcap(verdict == StatusKind.Alarm, "기준 이상 · 처리할 Lot 없음");
        }

        // 지금 조치가 필요한 테스터(첫 기준 미달)를 판정판에. 없으면 정상.
        private void ReloadPanel()
        {
            var bad = Array.Find(_testers, t => IsAlarm(t));
            if (bad == null)
            {
                _panel.Status = StatusKind.Run; _panel.Text = "정상"; _panel.SubText = "전 테스터 최종 수율 기준 이상";
                return;
            }
            _panel.Status = StatusKind.Alarm;
            _panel.Text = bad.Name + " 수율 기준 미달";
            _panel.SubText = "최종 수율 " + YieldRule.Pct(bad.Lot.FinalYield) + " % · 기준 " + YieldRule.Pct(YieldRule.FinalYieldCriterion) + " 이상";
        }

        // 선택 테스터의 1위 Soft Bin이 다른 테스터에서도 높은지(예: T04를 지난 Lot만 Soft Bin 23이 높다)
        private string SoftBinNote(SoftBinRow top, TesterSnapshot cur)
        {
            int othersQty = 0, othersTested = 0;
            foreach (var t in _testers)
            {
                if (t == cur || t.Lot == null) continue;
                othersTested += t.Lot.TestedQty;
                foreach (var s in t.Lot.SoftTop) if (s.SoftBin == top.SoftBin) othersQty += s.Qty;
            }
            double mine = 100.0 * top.Qty / cur.Lot.TestedQty;
            double others = othersTested == 0 ? 0 : 100.0 * othersQty / othersTested;
            return "Soft Bin " + top.SoftBin.ToString("00") + " 비율 " + cur.Name + " " + YieldRule.Pct(mine) + " % · 그 외 " + YieldRule.Pct(others) + " %";
        }

        private void SetOcap(bool enabled, string reasonWhenDisabled)
        {
            _ocapButton.Enabled = enabled;
            _ocapReason.Text = enabled ? "" : reasonWhenDisabled;
        }

        private void OpenOcap()
        {
            var cur = _testers[_selected];
            if (cur.Lot == null || YieldRule.JudgeFinal(cur.Lot.FinalYield) != StatusKind.Alarm) return;
            using (var dlg = new OcapDialog(cur.Name, cur.Lot))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.SubmitLotDecision(cur.Lot.LotId, dlg.Decision, dlg.Reason);
                ApcDialog.Alert(FindForm(), cur.Lot.LotId + " 판정을 기록했습니다 · " + dlg.Decision);
            }
        }

        private Control BuildHistory()
        {
            var grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            grid.Columns.Add(new ApcLampColumn());
            grid.Columns.Add("time", "종료 시각");
            grid.Columns.Add("tester", "테스터");
            grid.Columns.Add("lot", "Lot ID");
            grid.Columns.Add("cust", "고객");
            grid.Columns.Add("product", "제품");
            grid.Columns.Add("input", "투입 수량");
            grid.Columns.Add("first", "최초 수율(%)");
            grid.Columns.Add("final", "최종 수율(%)");
            grid.Columns.Add("result", "판정");
            for (int i = 1; i < grid.Columns.Count; i++) grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (var r in _source.GetRecentLotResults())
            {
                StatusKind k = YieldRule.JudgeFinal(r.FinalYield);
                int row = grid.Rows.Add(k, r.EndTime.ToString("HH:mm:ss"), r.Tester, r.LotId, r.CustomerCode, r.Product,
                    YieldRule.Qty(r.InputQty), YieldRule.Pct(r.FirstYield), YieldRule.Pct(r.FinalYield), r.Disposition);
                if (k == StatusKind.Alarm)
                {
                    var style = grid.Rows[row].Cells["final"].Style;
                    style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                    style.Font = ApcTheme.GetFont(FontStyle.Bold);
                }
            }
            return Titled("Lot 결과 이력 (오늘)", grid);
        }
    }
}
