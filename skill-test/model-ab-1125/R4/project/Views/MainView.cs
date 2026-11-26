using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>
    /// Final Test 수율·Bin 모니터링 메인 화면. 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// 위에서부터: 구역 판정판 + 선택 테스터 수율 카드 → 테스터 타일 → 선택 Lot 정보 → Bin·추이 → Lot 결과 이력.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int HardBinCount = 8, SoftBinTopCount = 5, MidRows = 8, TrendLotCount = 12;
        private static readonly string[] HardBinNames =
        {
            "양품", "Open / Short", "DC 파라메트릭", "기능 불량", "속도 불량", "고온 불량", "누설 전류", "외관 · 기타",
        };

        private readonly IMainViewSource _source;
        private readonly IList<TesterInfo> _testers;
        private readonly ApcStatusTile[] _tiles;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcValueCard _first = new ApcValueCard { Dock = DockStyle.Fill, Unit = "%" };
        private readonly ApcValueCard _final = new ApcValueCard { Dock = DockStyle.Fill, Unit = "%", ShowLamp = true };
        private readonly ApcInfoStrip _lotInfo = new ApcInfoStrip { Dock = DockStyle.Fill };
        private readonly ApcGrid _hardGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _softGrid = new ApcGrid { Dock = DockStyle.Top, ReadOnly = true };
        private readonly ApcNoticeBar _skewBar = new ApcNoticeBar { Dock = DockStyle.Fill };
        private readonly ApcTrendChart _chart = new ApcTrendChart { Dock = DockStyle.Fill, LowerLimit = YieldRules.FinalYieldMin };
        private TesterInfo _selected;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("Final Test 수율 모니터링")
        {
            _source = source;
            _testers = source.GetTesters();

            _tiles = new ApcStatusTile[_testers.Count];
            var tileCells = new Control[_testers.Count];
            for (int i = 0; i < _testers.Count; i++)
            {
                var tester = _testers[i];
                var tile = new ApcStatusTile { Text = TileText(tester), Status = tester.State, Dock = DockStyle.Fill };
                tile.Click += (s, e) => Select(tester);
                _tiles[i] = tile;
                tileCells[i] = tile;
            }

            BuildHardGrid();
            BuildSoftGrid();
            _lotInfo.SetItems("테스터", "Lot ID", "고객", "제품", "프로그램 버전", "온도(℃)", "투입(Unit)", "진행(Unit)");
            var ocap = Button("미달 Lot 처리", ApcButtonVariant.Primary, 160);
            ocap.Dock = DockStyle.Fill;
            ocap.Click += (s, e) => OpenOcap();

            Body.Controls.Add(Titled("Lot 결과 이력", BuildHistoryGrid()));
            Body.Controls.Add(Columns(TitledHeight(ApcGrid.HeightForRows(MidRows)), new Control[]
            {
                Titled("Hard Bin 분포", _hardGrid),
                Titled("Soft Bin 상위 5", BuildSoftCell()),
                Titled("최근 Lot 수율 추이 (빨간 점선 = 기준)", _chart),
            }, new[] { 31f, 36f, 33f }));
            Body.Controls.Add(Columns(S(48), new Control[] { _lotInfo, ocap }, new[] { 86f, 14f }));
            Body.Controls.Add(Columns(S(72), tileCells));
            Body.Controls.Add(Columns(S(140), new Control[] { _judge, _first, _final }, new[] { 50f, 25f, 25f }));

            ShowZoneJudge();
            Select(FirstSelection());
        }

        // 테스터 타일 글: 이름 + Lot + 최종 수율. 대기는 이름 + 상태 말.
        private static string TileText(TesterInfo t)
        {
            return t.Lot == null ? t.Id + "  대기" : t.Id + "  " + t.Lot.LotId + "  " + YieldRules.Pct(t.Lot.FinalYield) + " %";
        }

        private TesterInfo FirstSelection()
        {
            foreach (var t in _testers) if (t.State == StatusKind.Alarm) return t;
            return _testers[0];
        }

        // 구역 전체에서 가장 급한 것 하나. 선택한 테스터와 상관없이 항상 같은 자리에 보인다.
        private void ShowZoneJudge()
        {
            foreach (var t in _testers)
            {
                if (t.State != StatusKind.Alarm || t.Lot == null) continue;
                _judge.Status = StatusKind.Alarm;
                _judge.Text = t.Id + " 수율 미달 · Lot Hold";
                _judge.SubText = "최종 " + YieldRules.Pct(t.Lot.FinalYield) + " % < " + YieldRules.Pct(YieldRules.FinalYieldMin) + " · " + TopSoftBinCause(t);
                return;
            }
            _judge.Status = StatusKind.Run;
            _judge.Text = "정상";
            _judge.SubText = "기준 미달 Lot 없음";
        }

        private static string TopSoftBinCause(TesterInfo t)
        {
            foreach (var sb in t.SoftBins) if (sb.IsSkewed(t.Lot.Tested)) return "Soft Bin " + sb.SoftNo + " 높음 (" + t.Id + "만)";
            return "원인 Soft Bin 확인 필요";
        }

        private void Select(TesterInfo t)
        {
            _selected = t;
            for (int i = 0; i < _tiles.Length; i++) _tiles[i].Selected = _testers[i] == t;
            ShowYieldCards(t);
            ShowLotInfo(t);
            ShowHardBins(t);
            ShowSoftBins(t);
            ShowTrend(t);
        }

        private void ShowYieldCards(TesterInfo t)
        {
            var lot = t.Lot;
            _first.Title = t.Id + " 최초 수율";
            _final.Title = t.Id + " 최종 수율";
            _first.Value = lot == null ? "" : YieldRules.Pct(lot.FirstYield);
            _final.Value = lot == null ? "" : YieldRules.Pct(lot.FinalYield);
            _first.Footer = lot == null ? "" : "재테스트 전";
            _final.Footer = lot == null ? "" : "재테스트 후";
            _first.Status = lot == null ? StatusKind.Stop : StatusKind.Run;
            _final.Status = lot == null ? StatusKind.Stop : (lot.BelowLimit ? StatusKind.Alarm : StatusKind.Run);
        }

        private void ShowLotInfo(TesterInfo t)
        {
            var lot = t.Lot;
            _lotInfo.SetValue("테스터", t.Id);
            _lotInfo.SetValue("Lot ID", lot == null ? "" : lot.LotId);
            _lotInfo.SetValue("고객", lot == null ? "" : lot.Customer);
            _lotInfo.SetValue("제품", lot == null ? "" : lot.Product);
            _lotInfo.SetValue("프로그램 버전", lot == null ? "" : lot.ProgramVersion);
            _lotInfo.SetValue("온도(℃)", lot == null ? "" : YieldRules.Temp(lot.TempC));
            _lotInfo.SetValue("투입(Unit)", lot == null ? "" : YieldRules.Qty(lot.Input));
            _lotInfo.SetValue("진행(Unit)", lot == null ? "" : YieldRules.Qty(lot.Tested));
        }

        private void ShowHardBins(TesterInfo t)
        {
            _hardGrid.Rows.Clear();
            bool has = t.Lot != null;
            for (int i = 0; i < HardBinCount; i++)
            {
                _hardGrid.Rows.Add("Bin " + (i + 1), HardBinNames[i],
                    has ? YieldRules.Qty(t.HardBins[i]) : "", has ? YieldRules.Pct(t.HardBins[i] * 100.0 / t.Lot.Tested) : "");
            }
        }

        private void ShowSoftBins(TesterInfo t)
        {
            _softGrid.Rows.Clear();
            bool has = t.Lot != null;
            SoftBinRow skew = null;
            for (int i = 0; i < SoftBinTopCount; i++)
            {
                if (!has) { _softGrid.Rows.Add(i + 1, "", "", "", "", ""); continue; }
                var sb = t.SoftBins[i];
                int row = _softGrid.Rows.Add(i + 1, "SB" + sb.SoftNo, sb.HardNo, YieldRules.Qty(sb.Count),
                    YieldRules.Pct(sb.Rate(t.Lot.Tested)), YieldRules.Pct(sb.OtherAvgRate));
                if (sb.IsSkewed(t.Lot.Tested))
                {
                    if (skew == null) skew = sb;
                    var style = _softGrid.Rows[row].Cells["rate"].Style;
                    style.ForeColor = ApcTheme.StatusText(StatusKind.Warn);
                    style.Font = ApcTheme.GetFont(System.Drawing.FontStyle.Bold);
                }
            }
            ShowSkewBar(t, skew);
        }

        // 선택 테스터의 편중 Soft Bin(다른 테스터 평균의 3배 이상). 없으면 1위를 알려 준다.
        private void ShowSkewBar(TesterInfo t, SoftBinRow skew)
        {
            if (t.Lot == null) { _skewBar.Status = StatusKind.Stop; _skewBar.Text = ""; _skewBar.Detail = ""; return; }
            var sb = skew ?? t.SoftBins[0];
            _skewBar.Status = skew != null ? StatusKind.Warn : StatusKind.Info;
            _skewBar.Text = "Soft Bin " + sb.SoftNo + (skew != null ? " 편중" : " 1위");
            _skewBar.Detail = YieldRules.Pct(sb.Rate(t.Lot.Tested)) + " % · 타 테스터 평균 " + YieldRules.Pct(sb.OtherAvgRate) + " %";
        }

        private void ShowTrend(TesterInfo t)
        {
            _chart.SetValues(t.YieldTrend);
            _chart.XLabels = t.YieldTrend.Length == 0 ? new string[0]
                : new[] { (TrendLotCount - 1) + " Lot 전", "5 Lot 전", "현재" };
        }

        private void BuildHardGrid()
        {
            _hardGrid.Columns.Add("bin", "Hard Bin");
            _hardGrid.Columns.Add("name", "구분");
            _hardGrid.Columns.Add("qty", "수량(Unit)");
            _hardGrid.Columns.Add("rate", "비율(%)");
            foreach (DataGridViewColumn c in _hardGrid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void BuildSoftGrid()
        {
            _softGrid.Height = ApcGrid.HeightForRows(SoftBinTopCount);
            _softGrid.Columns.Add("rank", "순위");
            _softGrid.Columns.Add("soft", "Soft Bin");
            _softGrid.Columns.Add("hard", "Hard Bin");
            _softGrid.Columns.Add("qty", "수량(Unit)");
            _softGrid.Columns.Add("rate", "비율(%)");
            _softGrid.Columns.Add("other", "타 테스터(%)");
            foreach (DataGridViewColumn c in _softGrid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        // 표(고정 5행) 아래 남는 높이는 편중 안내 띠가 채운다.
        private Control BuildSoftCell()
        {
            var cardHolder = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, S(12), 0, 0) };
            cardHolder.Controls.Add(_skewBar);
            var cell = new Panel { Dock = DockStyle.Fill };
            cell.Controls.Add(cardHolder);
            cell.Controls.Add(_softGrid);
            return cell;
        }

        private ApcGrid BuildHistoryGrid()
        {
            var grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            grid.Columns.Add(new ApcLampColumn());
            grid.Columns.Add("time", "완료 시각");
            grid.Columns.Add("tester", "테스터");
            grid.Columns.Add("lot", "Lot ID");
            grid.Columns.Add("cust", "고객");
            grid.Columns.Add("prod", "제품");
            grid.Columns.Add("input", "투입(Unit)");
            grid.Columns.Add("first", "최초 수율(%)");
            grid.Columns.Add("final", "최종 수율(%)");
            grid.Columns.Add("disp", "판정");
            grid.Columns["disp"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["disp"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            foreach (var lot in _source.GetLotHistory())
            {
                bool below = lot.BelowLimit;
                int row = grid.Rows.Add(below ? StatusKind.Alarm : StatusKind.Run, lot.CompletedAt.Value.ToString("HH:mm:ss"), lot.Tester,
                    lot.LotId, lot.Customer, lot.Product, YieldRules.Qty(lot.Input), YieldRules.Pct(lot.FirstYield), YieldRules.Pct(lot.FinalYield),
                    below ? "기준 미달 · " + lot.Disposition : lot.Disposition);
                if (!below) continue;
                var style = grid.Rows[row].Cells["final"].Style;
                style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                style.Font = ApcTheme.GetFont(System.Drawing.FontStyle.Bold);
            }
            return grid;
        }

        private void OpenOcap()
        {
            var t = _selected;
            if (t == null || t.Lot == null || t.State != StatusKind.Alarm)
            {
                ApcDialog.Warn(FindForm(), "처리할 기준 미달 Lot가 없습니다. 수율 미달 알람 테스터를 선택하세요.");
                return;
            }
            using (var dlg = new OcapDialog(_source.GetOcapCase(t)))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.SaveDisposition(t.Lot.LotId, dlg.Decision, dlg.Reason);
                ApcDialog.Alert(FindForm(), t.Lot.LotId + " 판정 '" + dlg.Decision + "' 판정을 기록했습니다.");
            }
        }
    }
}
