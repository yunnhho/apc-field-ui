using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>
    /// Final Test 테스트 구역 수율·Bin 모니터링.
    /// 맨 위: 판정판(기준 미달 Lot) + 테스터 타일 6 → 선택 테스터 현재 Lot 정보 → 수율 카드 + Hard Bin 분포 → Soft Bin 상위 5 + 최근 Lot 추이 → Lot 결과 이력.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int HardBinCount = 8;
        private const int SoftBinTopRows = 5;
        private const string InitialTester = "T04";

        private readonly IMainViewSource _source;
        private readonly List<ApcStatusTile> _tiles = new List<ApcStatusTile>();
        private readonly ApcInfoStrip _info = new ApcInfoStrip();
        private readonly ApcValueCard _firstCard = Card("최초 수율");
        private readonly ApcValueCard _finalCard = Card("최종 수율");
        private readonly ApcGrid _hardBins = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _softBins = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcTrendChart _trend = new ApcTrendChart { Dock = DockStyle.Fill };
        private readonly ApcGrid _history = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false };
        private readonly ApcButton _ocapButton = Button("OCAP 처리", ApcButtonVariant.Primary, 130);
        private readonly Label _ocapReason = Caption("");
        private Panel _hardBinCell, _softBinCell, _trendCell;
        private IList<LotResult> _lots;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("Final Test 수율 모니터링")
        {
            _source = source;
            _info.SetItems("테스터", "Lot ID", "고객", "제품", "테스트 프로그램", "온도 (℃)", "투입 / 진행 (EA)");
            BuildHardBinGrid();
            BuildSoftBinGrid();
            BuildHistoryGrid();
            _trend.LowerLimit = YieldRule.FinalYieldLimit;
            _trend.XLabels = new[] { "10 Lot 전", "5 Lot 전", "최근 Lot" };
            _ocapButton.Click += (s, e) => OpenOcap();

            _hardBinCell = Titled("Hard Bin 분포", _hardBins);
            _softBinCell = Titled("Soft Bin 상위 5", _softBins);
            _trendCell = Titled("최근 Lot 최종 수율 추이", _trend);
            var historyCell = Titled("Lot 결과 이력 (최종 수율 하한 " + YieldRule.Pct(YieldRule.FinalYieldLimit) + " %)", _history, _ocapReason, _ocapButton);

            // Fill 먼저, Top 줄은 아래쪽부터
            Body.Controls.Add(historyCell);
            Body.Controls.Add(Columns(TitledHeight(ApcGrid.HeightForRows(SoftBinTopRows)), new Control[] { _softBinCell, _trendCell }, new float[] { 55, 45 }));
            Body.Controls.Add(Columns(TitledHeight(ApcGrid.HeightForRows(2)), new Control[] { _firstCard, _finalCard, _hardBinCell }, new float[] { 16, 16, 68 }));
            Body.Controls.Add(Columns(_info.Height, new Control[] { _info }));
            Body.Controls.Add(Columns(S(110), BuildTopRow(), new float[] { 28, 12, 12, 12, 12, 12, 12 }));

            LoadHistory();
            ShowTester(InitialTester);
        }

        private static ApcValueCard Card(string title)
        {
            return new ApcValueCard { Title = title, Unit = "%", ShowLamp = true, Dock = DockStyle.Fill };
        }

        // ---------- 맨 위 줄: 판정판 + 테스터 타일 ----------

        private Control[] BuildTopRow()
        {
            var cells = new List<Control> { BuildJudgePanel() };
            foreach (var t in _source.GetTesters())
            {
                string name = t.Name;
                var tile = new ApcStatusTile
                {
                    Text = t.Yield.HasValue ? name + "  " + YieldRule.Pct(t.Yield.Value) + " %" : name,
                    Status = TileStatus(t.State),
                    Tag = name,
                    Dock = DockStyle.Fill,
                };
                tile.Click += (s, e) => ShowTester(name);
                _tiles.Add(tile);
                cells.Add(tile);
            }
            return cells.ToArray();
        }

        private static StatusKind TileStatus(TesterState state)
        {
            switch (state)
            {
                case TesterState.Testing: return StatusKind.Run;
                case TesterState.Alarm: return StatusKind.Alarm;
                default: return StatusKind.Stop;
            }
        }

        private ApcJudgePanel BuildJudgePanel()
        {
            var panel = new ApcJudgePanel { Dock = DockStyle.Fill };
            var held = _source.GetLotHistory().Where(l => l.BelowSpec).ToList();
            if (held.Count == 0)
            {
                panel.Status = StatusKind.Run;
                panel.Text = "수율 정상";
                panel.SubText = "기준 미달 Lot 없음";
                return panel;
            }
            var lot = held[0];
            var top = _source.GetSoftBinTop(lot.LotId).FirstOrDefault();
            panel.Status = StatusKind.Alarm;
            panel.Text = held.Count == 1 ? "Lot Hold" : "Lot Hold " + held.Count + "건";
            panel.SubText = lot.Tester + " " + lot.LotId + " 최종 " + YieldRule.Pct(lot.FinalYield) + " %"
                + (top == null ? "" : " · SB" + top.SoftBin + " " + top.Name);
            return panel;
        }

        // ---------- 선택 테스터 ----------

        private void ShowTester(string tester)
        {
            foreach (var tile in _tiles) tile.Selected = (string)tile.Tag == tester;
            var lot = _source.GetCurrentLot(tester);

            _info.SetValue("테스터", tester);
            _info.SetValue("Lot ID", lot.LotId);
            _info.SetValue("고객", lot.Customer);
            _info.SetValue("제품", lot.Product);
            _info.SetValue("테스트 프로그램", lot.Program);
            _info.SetValue("온도 (℃)", lot.HasLot ? lot.TempC.ToString(YieldRule.TempFormat) : "");
            _info.SetValue("투입 / 진행 (EA)", lot.HasLot ? YieldRule.Count(lot.Input) + " / " + YieldRule.Count(lot.Tested) : "");

            ShowYieldCards(tester, lot);
            ShowHardBins(lot);
            ShowSoftBins(lot);
            _trend.SetValues(lot.RecentFinalYields);
            _trendCell.Text = "최근 Lot 최종 수율 추이 · " + tester;
        }

        private void ShowYieldCards(string tester, LotDetail lot)
        {
            _firstCard.Title = "최초 수율 · " + tester;
            _finalCard.Title = "최종 수율 · " + tester;
            int good = lot.HardBins[0];

            if (lot.FirstYield.HasValue)
            {
                _firstCard.Value = YieldRule.Pct(lot.FirstYield.Value);
                _firstCard.Status = StatusKind.Run;   // 최초 수율은 기준 없음
                _firstCard.Footer = lot.FinalYield.HasValue ? "재테스트 전 기준" : "양품 " + YieldRule.Count(good) + " / 진행 " + YieldRule.Count(lot.Tested);
            }
            else
            {
                _firstCard.Value = "";
                _firstCard.Status = StatusKind.Stop;
                _firstCard.Footer = "대기";
            }

            if (lot.FinalYield.HasValue)
            {
                _finalCard.Value = YieldRule.Pct(lot.FinalYield.Value);
                _finalCard.Status = YieldRule.Judge(lot.FinalYield.Value);
                _finalCard.FooterStatus = StatusKind.Run;   // 양품/투입 수량은 판정 대상이 아님
                _finalCard.Footer = "양품 " + YieldRule.Count(good) + " / 투입 " + YieldRule.Count(lot.Input);
            }
            else
            {
                _finalCard.Value = "";
                _finalCard.Status = StatusKind.Stop;
                _finalCard.Footer = lot.HasLot ? "재테스트 후 확정" : "대기";
            }
        }

        private void BuildHardBinGrid()
        {
            _hardBins.Columns.Add("kind", "구분");
            _hardBins.Columns.Add("b1", "Bin1 양품");
            for (int i = 2; i <= HardBinCount; i++) _hardBins.Columns.Add("b" + i, "Bin" + i);
            foreach (DataGridViewColumn c in _hardBins.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _hardBins.Rows.Add("수량 (EA)");
            _hardBins.Rows.Add("비율 (%)");
        }

        private void ShowHardBins(LotDetail lot)
        {
            int total = lot.Tested;
            for (int i = 0; i < HardBinCount; i++)
            {
                var countCell = _hardBins.Rows[0].Cells[i + 1];
                var rateCell = _hardBins.Rows[1].Cells[i + 1];
                countCell.Value = lot.HasLot ? YieldRule.Count(lot.HardBins[i]) : "";
                rateCell.Value = lot.HasLot ? YieldRule.Pct(YieldRule.Rate(lot.HardBins[i], total)) : "";
            }
            // Bin1 비율 = 최종 수율. 기준 미달이면 빨간 굵은 글자
            var bin1Rate = _hardBins.Rows[1].Cells[1];
            bool below = lot.FinalYield.HasValue && YieldRule.IsBelowSpec(lot.FinalYield.Value);
            bin1Rate.Style.ForeColor = below ? ApcTheme.StatusText(StatusKind.Alarm) : Color.Empty;
            bin1Rate.Style.Font = below ? ApcTheme.GetFont(FontStyle.Bold) : null;
            _hardBinCell.Text = lot.HasLot ? "Hard Bin 분포 · " + lot.LotId : "Hard Bin 분포";
        }

        private void BuildSoftBinGrid()
        {
            _softBins.Columns.Add("rank", "순위");
            _softBins.Columns.Add("sb", "Soft Bin");
            _softBins.Columns.Add("name", "불량 원인");
            _softBins.Columns.Add("hb", "Hard Bin");
            _softBins.Columns.Add("count", "수량(EA)");
            _softBins.Columns.Add("rate", "비율(%)");
            foreach (DataGridViewColumn c in _softBins.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void ShowSoftBins(LotDetail lot)
        {
            _softBins.Rows.Clear();
            for (int i = 0; i < lot.SoftBinTop.Count && i < SoftBinTopRows; i++)
            {
                var s = lot.SoftBinTop[i];
                _softBins.Rows.Add(i + 1, "SB" + s.SoftBin, s.Name, "Bin" + s.HardBin, YieldRule.Count(s.Count), YieldRule.Pct(YieldRule.Rate(s.Count, lot.Tested)));
            }
        }

        // ---------- Lot 결과 이력 ----------

        private void BuildHistoryGrid()
        {
            _history.Columns.Add(new ApcLampColumn { Name = "state", HeaderText = "상태" });
            _history.Columns.Add("end", "종료 시각");
            _history.Columns.Add("tester", "테스터");
            _history.Columns.Add("lot", "Lot ID");
            _history.Columns.Add("customer", "고객");
            _history.Columns.Add("product", "제품");
            _history.Columns.Add("input", "투입(EA)");
            _history.Columns.Add("first", "최초 수율(%)");
            _history.Columns.Add("final", "최종 수율(%)");
            _history.Columns.Add("sb", "주요 Soft Bin");
            _history.Columns.Add("sbRate", "Soft Bin 비율(%)");
            foreach (DataGridViewColumn c in _history.Columns)
                if (c.Name != "state") c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _history.SelectionChanged += (s, e) => UpdateOcapButton();
        }

        private void LoadHistory()
        {
            _lots = _source.GetLotHistory();
            _history.Rows.Clear();
            int holdRow = -1;
            foreach (var l in _lots)
            {
                var status = YieldRule.Judge(l.FinalYield);
                int r = _history.Rows.Add(new ApcLampValue(status, l.BelowSpec ? "기준 미달" : "합격"), l.EndTime.ToString("HH:mm:ss"), l.Tester, l.LotId,
                    l.Customer, l.Product, YieldRule.Count(l.Input), YieldRule.Pct(l.FirstYield), YieldRule.Pct(l.FinalYield),
                    "SB" + l.TopSoftBin, YieldRule.Pct(l.TopSoftBinRate));
                _history.Rows[r].Tag = l;
                if (l.BelowSpec)
                {
                    var cell = _history.Rows[r].Cells["final"];
                    cell.Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                    cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
                    if (holdRow < 0) holdRow = r;
                }
            }
            if (holdRow >= 0) _history.CurrentCell = _history.Rows[holdRow].Cells["end"];
            UpdateOcapButton();
        }

        private LotResult SelectedLot()
        {
            var row = _history.CurrentRow;
            return row == null ? null : row.Tag as LotResult;
        }

        private void UpdateOcapButton()
        {
            var lot = SelectedLot();
            string reason = lot == null ? "이력에서 Lot 선택" : !lot.BelowSpec ? "합격 Lot · OCAP 대상 아님" : "";
            _ocapButton.Enabled = reason.Length == 0;
            _ocapReason.Text = reason;
        }

        private void OpenOcap()
        {
            var lot = SelectedLot();
            if (lot == null || !lot.BelowSpec) return;
            using (var dlg = new OcapDialog(lot, _source.GetSoftBinTop(lot.LotId), _source.GetOcapSteps(lot.LotId), OcapDialog.HoldKeep))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                    _source.SaveOcapDecision(lot.LotId, dlg.Decision, dlg.Reason);
            }
        }
    }
}
