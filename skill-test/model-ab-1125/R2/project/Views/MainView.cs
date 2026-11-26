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
    /// 테스트 구역 수율·Bin 모니터링. 맨 위 판정판 = 지금 처리할 기준 미달 Lot, 그 아래 테스터 타일(누르면 아래가 그 테스터),
    /// 선택 테스터 Lot 정보 → 수율 카드 + Hard Bin → Soft Bin 상위 5 + 수율 추이 → Lot 결과 이력(남은 높이).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int SoftBinTopCount = 5;

        private readonly IMainViewSource _source;
        private readonly IList<TesterState> _testers;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly List<ApcStatusTile> _tiles = new List<ApcStatusTile>();
        private readonly ApcInfoStrip _info = new ApcInfoStrip();
        private readonly ApcValueCard _firstCard = new ApcValueCard { Title = "최초 수율", Unit = "%", ShowLamp = true, Dock = DockStyle.Fill };
        private readonly ApcValueCard _finalCard = new ApcValueCard { Title = "최종 수율", Unit = "%", ShowLamp = true, Dock = DockStyle.Fill };
        private readonly ApcGrid _hardGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _softGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcTrendChart _trend = new ApcTrendChart { Dock = DockStyle.Fill, LowerLimit = YieldJudge.Limit };
        private readonly ApcGrid _history = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false };
        private readonly ApcButton _ocap = Button("OCAP 처리", ApcButtonVariant.Primary, 130);
        private readonly Label _ocapReason = Caption("");
        private Panel _hardTitled, _softTitled, _trendTitled;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("Final Test 수율 모니터링")
        {
            _source = source;
            _testers = source.GetTesters();

            BuildTiles();
            _info.SetItems("테스터", "Lot ID", "고객", "제품", "테스트 프로그램", "온도 (℃)", "투입 수량", "진행 수량");
            BuildHardGrid();
            BuildSoftGrid();
            BuildHistory();

            _hardTitled = Titled("Hard Bin 분포", _hardGrid);
            _softTitled = Titled("Soft Bin 상위 5", _softGrid);
            _trendTitled = Titled("최근 Lot 최종 수율 추이 (%)", _trend);
            var historyTitled = Titled("Lot 결과 이력 (오늘)", _history, _ocapReason, _ocap);
            _ocap.Click += (s, e) => OpenOcap();

            int hardH = TitledHeight(_hardTitled, ApcGrid.HeightForRows(2));
            int softH = TitledHeight(_softTitled, ApcGrid.HeightForRows(SoftBinTopCount));

            // Fill 먼저, Top 줄은 아래쪽부터
            Body.Controls.Add(historyTitled);
            Body.Controls.Add(Columns(softH, new Control[] { _softTitled, _trendTitled }, new[] { 50f, 50f }));
            Body.Controls.Add(Columns(hardH, new Control[] { _firstCard, _finalCard, _hardTitled }, new[] { 15f, 15f, 70f }));
            Body.Controls.Add(Columns(_info.Height, new Control[] { _info }));
            Body.Controls.Add(Columns(S(60), _tiles.Cast<Control>().ToArray()));
            Body.Controls.Add(Columns(S(84), new Control[] { _judge }));

            ShowJudge();
            var first = _testers.FirstOrDefault(t => t.Process == TesterProcess.Alarm) ?? _testers[0];
            SelectTester(_testers.IndexOf(first));
        }

        private static int TitledHeight(Panel titled, int contentHeight)
        {
            int h = contentHeight + titled.Padding.Vertical;
            foreach (Control c in titled.Controls)
                if (c.Dock == DockStyle.Top) h += c.Height + c.Margin.Vertical;
            return h;
        }

        // ---- 테스터 타일 ----

        private void BuildTiles()
        {
            for (int i = 0; i < _testers.Count; i++)
            {
                var t = _testers[i];
                int index = i;
                var tile = new ApcStatusTile { Text = TileText(t), Status = TesterStatus(t), Dock = DockStyle.Fill };
                tile.Click += (s, e) => SelectTester(index);
                _tiles.Add(tile);
            }
        }

        private static string TileText(TesterState t)
        {
            return t.Lot == null ? t.Id : t.Id + "  " + t.Lot.LotId + "  " + YieldJudge.Pct(t.Lot.FinalYield) + " %";
        }

        /// <summary>타일 색 = 공정 상태(대기 Stop, 알람 Alarm)와 최종 수율 판정 중 나쁜 쪽.</summary>
        private static StatusKind TesterStatus(TesterState t)
        {
            if (t.Process == TesterProcess.Alarm) return StatusKind.Alarm;
            if (t.Process == TesterProcess.Idle || t.Lot == null) return StatusKind.Stop;
            return YieldJudge.Judge(t.Lot.FinalYield);
        }

        private void SelectTester(int index)
        {
            for (int i = 0; i < _tiles.Count; i++) _tiles[i].Selected = i == index;
            var t = _testers[index];
            ShowLotInfo(t);
            ShowYieldCards(t);
            ShowBins(t.Lot);
            ShowTrend(t);
        }

        // ---- 판정판: 지금 처리할 기준 미달 Lot 하나 ----

        private void ShowJudge()
        {
            var hold = _source.GetLotHistory().FirstOrDefault(r => r.IsHold);
            if (hold == null)
            {
                _judge.Status = StatusKind.Run;
                _judge.Text = "정상";
                _judge.SubText = "기준 미달 Lot 없음";
                return;
            }
            _judge.Status = StatusKind.Alarm;
            _judge.Text = hold.TesterId + " Lot Hold";
            _judge.SubText = hold.LotId + " 최종 수율 기준 미달 " + YieldJudge.Pct(hold.FinalYield) + " % (기준 " + YieldJudge.Pct(YieldJudge.Limit)
                + ") · 주요 " + hold.TopSoftBin;
        }

        // ---- 선택 테스터 ----

        private void ShowLotInfo(TesterState t)
        {
            var lot = t.Lot;
            _info.SetValue("테스터", t.Id + (t.Process == TesterProcess.Idle ? " · 대기" : t.Process == TesterProcess.Alarm ? " · 사용 중지" : " · 테스트 중"));
            _info.SetValue("Lot ID", lot == null ? "" : lot.LotId);
            _info.SetValue("고객", lot == null ? "" : lot.Customer);
            _info.SetValue("제품", lot == null ? "" : lot.Product);
            _info.SetValue("테스트 프로그램", lot == null ? "" : lot.Program);
            _info.SetValue("온도 (℃)", lot == null ? "" : lot.Temperature.ToString("0.0"));
            _info.SetValue("투입 수량", lot == null ? "" : lot.InputQty.ToString("#,0"));
            _info.SetValue("진행 수량", lot == null ? "" : lot.TestedQty.ToString("#,0"));
        }

        private void ShowYieldCards(TesterState t)
        {
            var lot = t.Lot;
            if (lot == null)
            {
                foreach (var c in new[] { _firstCard, _finalCard }) { c.Value = ""; c.Footer = ""; c.Status = StatusKind.Stop; }
                return;
            }
            // 최초 수율은 기준 없음: 테스트 중이면 Run, 멈춘 테스터는 Stop
            _firstCard.Value = YieldJudge.Pct(lot.FirstYield);
            _firstCard.Footer = "재테스트 전 양품 " + lot.FirstPassGood.ToString("#,0");
            _firstCard.Status = t.Process == TesterProcess.Testing ? StatusKind.Run : StatusKind.Stop;

            _finalCard.Value = YieldJudge.Pct(lot.FinalYield);
            _finalCard.Footer = "양품 " + lot.FinalGood.ToString("#,0") + " / 진행 " + lot.TestedQty.ToString("#,0");
            _finalCard.Status = YieldJudge.Judge(lot.FinalYield);
        }

        private void BuildHardGrid()
        {
            _hardGrid.Columns.Add("kind", "구분");
            for (int b = 1; b <= 8; b++) _hardGrid.Columns.Add("b" + b, "");
            foreach (DataGridViewColumn c in _hardGrid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _hardGrid.Rows.Add("수량");
            _hardGrid.Rows.Add("비율(%)");
        }

        private void BuildSoftGrid()
        {
            _softGrid.Columns.Add("sb", "Soft Bin");
            _softGrid.Columns.Add("name", "불량 원인");
            _softGrid.Columns.Add("hb", "Hard Bin");
            _softGrid.Columns.Add("qty", "수량");
            _softGrid.Columns.Add("rate", "비율(%)");
            _softGrid.Columns["name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _softGrid.Columns["name"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        private void ShowBins(LotInfo lot)
        {
            var hard = lot == null ? new List<BinCount>() : _source.GetHardBins(lot.LotId);
            for (int b = 1; b <= 8; b++)
            {
                var bin = hard.FirstOrDefault(x => x.Bin == b);
                _hardGrid.Columns["b" + b].HeaderText = "Bin " + b + " " + (bin == null ? "" : bin.Name);
                _hardGrid.Rows[0].Cells[b].Value = bin == null ? "" : bin.Count.ToString("#,0");
                _hardGrid.Rows[1].Cells[b].Value = bin == null ? "" : YieldJudge.Pct(bin.Count * 100.0 / lot.TestedQty);
            }

            _softGrid.Rows.Clear();
            if (lot == null) return;
            foreach (var sb in _source.GetSoftBinTop(lot.LotId, SoftBinTopCount))
                _softGrid.Rows.Add("SB" + sb.Bin.ToString("00"), sb.Name, sb.HardBin, sb.Count.ToString("#,0"), YieldJudge.Pct(sb.Count * 100.0 / lot.TestedQty));
        }

        private void ShowTrend(TesterState t)
        {
            string[] lotIds;
            var values = _source.GetRecentYields(t.Id, out lotIds);
            _trend.SetValues(values);
            bool running = t.Process == TesterProcess.Testing;
            _trend.XLabels = lotIds.Length == 0 ? new string[0]
                : new[] { lotIds[0], lotIds[lotIds.Length / 2], running ? "진행 중" : lotIds[lotIds.Length - 1] };
            _trendTitled.Text = "최근 Lot 최종 수율 추이 (%) · " + t.Id;
        }

        // ---- Lot 결과 이력 ----

        private void BuildHistory()
        {
            _history.Columns.Add(new ApcLampColumn { Name = "judge", HeaderText = "판정", Width = S(120) });
            _history.Columns.Add("end", "종료 시각");
            _history.Columns.Add("tester", "테스터");
            _history.Columns.Add("lot", "Lot ID");
            _history.Columns.Add("cust", "고객");
            _history.Columns.Add("prod", "제품");
            _history.Columns["prod"].Width = S(150);
            _history.Columns.Add("input", "투입");
            _history.Columns.Add("first", "최초 수율(%)");
            _history.Columns.Add("final", "최종 수율(%)");
            _history.Columns.Add("sb", "주요 Soft Bin");
            _history.Columns.Add("sbRate", "SB 비율(%)");
            _history.Columns["sb"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _history.Columns["sb"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            DataGridViewRow holdRow = null;
            foreach (var r in _source.GetLotHistory())
            {
                var kind = YieldJudge.Judge(r.FinalYield);
                int i = _history.Rows.Add(new ApcLampValue(kind, YieldJudge.Word(kind)), r.EndTime.ToString("HH:mm:ss"), r.TesterId, r.LotId,
                    r.Customer, r.Product, r.InputQty.ToString("#,0"), YieldJudge.Pct(r.FirstYield), YieldJudge.Pct(r.FinalYield),
                    r.TopSoftBin, YieldJudge.Pct(r.TopSoftBinRate));
                var row = _history.Rows[i];
                row.Tag = r;
                if (kind != StatusKind.Run)
                {
                    row.Cells["final"].Style.ForeColor = ApcTheme.StatusText(kind);
                    row.Cells["final"].Style.Font = ApcTheme.GetFont(FontStyle.Bold);
                }
                if (holdRow == null && r.IsHold) holdRow = row;
            }
            _history.SelectionChanged += (s, e) => UpdateOcapButton();
            if (holdRow != null) _history.CurrentCell = holdRow.Cells["lot"];
            UpdateOcapButton();
        }

        private LotResult SelectedLot()
        {
            return _history.CurrentRow == null ? null : _history.CurrentRow.Tag as LotResult;
        }

        private void UpdateOcapButton()
        {
            var lot = SelectedLot();
            bool ok = lot != null && lot.IsHold;
            _ocap.Enabled = ok;
            _ocapReason.Text = ok ? "" : "Hold Lot을 선택하면 처리할 수 있습니다";
        }

        private void OpenOcap()
        {
            var lot = SelectedLot();
            if (lot == null || !lot.IsHold) return;
            using (var dlg = new OcapDialog(lot, _source.GetOcapSteps(lot.LotId)))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.SaveOcapDecision(lot.LotId, dlg.Decision, dlg.Reason);
                ApcDialog.Alert(FindForm(), lot.LotId + " 판정 '" + dlg.Decision + "'을 기록했습니다.");
            }
        }
    }
}
