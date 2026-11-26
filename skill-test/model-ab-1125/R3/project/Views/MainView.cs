using System;
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
    /// 테스트 구역 수율·Bin 모니터링. 위에서 아래로: 판정판·수율 카드·OCAP 버튼 → 테스터 타일 → 선택 Lot 정보
    /// → Hard Bin / Soft Bin 상위 5 / 수율 추이 → Lot 결과 이력. 데이터는 IMainViewSource에서만 받는다.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const string DefaultTesterId = "T04";
        private const int HardBinRows = 8;
        private const int SoftTopCount = 5;
        private const int FinalYieldColumn = 7;
        private const int TrendLength = 8;

        private readonly IMainViewSource _source;
        private readonly Dictionary<string, ApcStatusTile> _tiles = new Dictionary<string, ApcStatusTile>();
        private readonly ApcJudgePanel _judge = new ApcJudgePanel();
        private readonly ApcValueCard _firstCard = new ApcValueCard();
        private readonly ApcValueCard _finalCard = new ApcValueCard();
        private readonly ApcButton _ocapButton;
        private readonly Label _ocapCaption;
        private readonly ApcInfoStrip _lotInfo = new ApcInfoStrip();
        private readonly ApcGrid _hardGrid = new ApcGrid();
        private readonly ApcGrid _softGrid = new ApcGrid();
        private readonly ApcTrendChart _trend = new ApcTrendChart();
        private readonly ApcGrid _historyGrid = new ApcGrid();
        private Panel _hardBox;
        private Panel _softBox;
        private Panel _trendBox;
        private IList<TesterInfo> _testers;
        private string _selectedId = DefaultTesterId;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("Final Test 수율 모니터링")
        {
            _source = source;
            _ocapButton = Button("기준 미달 Lot 처리", ApcButtonVariant.Primary);
            _ocapButton.Dock = DockStyle.Fill;
            _ocapButton.Click += (s, e) => OpenOcapDialog();
            _ocapCaption = Caption("");

            IList<TesterInfo> testers = source.GetTesters();
            Body.Controls.Add(BuildHistory());
            Body.Controls.Add(BuildBinRow());
            Body.Controls.Add(Columns(S(40), new Control[] { _lotInfo }));
            Body.Controls.Add(BuildTileRow(testers));
            Body.Controls.Add(BuildTileHead());
            Body.Controls.Add(BuildJudgeRow());
            ReloadTesters();
        }

        private Control BuildJudgeRow()
        {
            _judge.Dock = DockStyle.Fill;
            _firstCard.Title = "최초 수율";
            _firstCard.Unit = "%";
            _firstCard.ShowLamp = true;
            _firstCard.Dock = DockStyle.Fill;
            _finalCard.Title = "최종 수율";
            _finalCard.Unit = "%";
            _finalCard.ShowLamp = true;
            _finalCard.Dock = DockStyle.Fill;

            _ocapCaption.Dock = DockStyle.Top;
            var ocapCell = new Panel { Dock = DockStyle.Fill, Padding = new Padding(S(12), S(8), S(12), S(12)) };
            ocapCell.Controls.Add(_ocapButton);
            ocapCell.Controls.Add(_ocapCaption);

            return Columns(S(116), new Control[] { _judge, _firstCard, _finalCard, ocapCell },
                new float[] { 0.40f, 0.18f, 0.18f, 0.24f });
        }

        private Control BuildTileRow(IList<TesterInfo> testers)
        {
            var cells = new Control[testers.Count];
            for (int i = 0; i < testers.Count; i++)
            {
                string id = testers[i].Id;
                var tile = new ApcStatusTile { Dock = DockStyle.Fill };
                tile.Click += (s, e) => SelectTester(id);
                _tiles[id] = tile;
                cells[i] = tile;
            }
            return Columns(S(72), cells);
        }

        private Control BuildTileHead()
        {
            var legend = new ApcStatusLegend { Dock = DockStyle.Right };
            legend.SetItems(
                new ApcLampValue(StatusKind.Run, "테스트 중"),
                new ApcLampValue(StatusKind.Warn, "수율 근접"),
                new ApcLampValue(StatusKind.Stop, "대기"),
                new ApcLampValue(StatusKind.Alarm, "알람"));

            var title = SectionTitle("테스터 상태 · 타일을 누르면 아래 Lot 정보가 바뀐다");
            title.Dock = DockStyle.Fill;

            var head = new Panel { Dock = DockStyle.Top, Height = S(30), BackColor = Color.Transparent };
            head.Controls.Add(title);
            head.Controls.Add(legend);
            return head;
        }

        private Control BuildBinRow()
        {
            _hardBox = Titled("Hard Bin 분포", BuildHardGrid());
            _softBox = Titled("Soft Bin 상위 " + SoftTopCount, BuildSoftGrid());
            _trendBox = Titled("최근 Lot 최종 수율 추이", BuildTrend());
            int height = ApcGrid.HeightForRows(HardBinRows) + S(40);
            return Columns(height, new Control[] { _hardBox, _softBox, _trendBox }, new float[] { 0.30f, 0.30f, 0.40f });
        }

        private Control BuildHistory()
        {
            _historyGrid.ReadOnly = true;
            _historyGrid.Columns.Add(new ApcLampColumn { Name = "state", HeaderText = "상태", Width = S(110) });
            foreach (string header in new[] { "시각", "테스터", "Lot ID", "고객", "제품", "최초 수율(%)", "최종 수율(%)", "주요 Soft Bin" })
            {
                _historyGrid.Columns.Add(header, header);
            }
            DataGridViewColumn softColumn = _historyGrid.Columns["주요 Soft Bin"];
            softColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            softColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            return Titled("Lot 결과 이력 · 기준 미달 Lot 표시", _historyGrid);
        }

        private ApcGrid BuildHardGrid()
        {
            _hardGrid.ReadOnly = true;
            foreach (string header in new[] { "Hard Bin", "분류", "수량", "비율(%)" })
            {
                _hardGrid.Columns.Add(header, header);
            }
            foreach (DataGridViewColumn column in _hardGrid.Columns)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            return _hardGrid;
        }

        private ApcGrid BuildSoftGrid()
        {
            _softGrid.ReadOnly = true;
            foreach (string header in new[] { "Soft Bin", "원인", "Hard Bin", "수량", "비율(%)" })
            {
                _softGrid.Columns.Add(header, header);
            }
            foreach (DataGridViewColumn column in _softGrid.Columns)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            DataGridViewColumn cause = _softGrid.Columns["원인"];
            cause.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            cause.Width = S(170);
            cause.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            return _softGrid;
        }

        private ApcTrendChart BuildTrend()
        {
            _trend.LowerLimit = YieldJudge.LimitPercent;
            _trend.Reference = YieldJudge.WarnPercent;
            return _trend;
        }

        private void ReloadTesters()
        {
            _testers = _source.GetTesters();
            foreach (TesterInfo tester in _testers)
            {
                ApcStatusTile tile;
                if (!_tiles.TryGetValue(tester.Id, out tile)) continue;
                tile.Text = tester.Id + "  " + tester.FinalYield.ToString("0.0") + " %";
                tile.Status = TileStatus(tester);
            }
            UpdateJudge();
            FillHistory();
            SelectTester(_selectedId);
        }

        private void UpdateJudge()
        {
            var holds = _testers.Where(t => YieldJudge.IsBelowLimit(t.FinalYield)).ToList();
            if (holds.Count == 0)
            {
                _judge.Status = StatusKind.Run;
                _judge.Text = "OK";
                _judge.SubText = "기준 미달 Lot 없음";
                return;
            }

            TesterInfo first = holds[0];
            LotDetail lot = _source.GetLot(first.Id);
            _judge.Status = StatusKind.Alarm;
            _judge.Text = "HOLD " + holds.Count + " Lot";
            _judge.SubText = first.Id + " · " + lot.LotId + " 최종 수율 " + first.FinalYield.ToString("0.0")
                + " % (기준 " + YieldJudge.LimitPercent.ToString("0.0") + " % 미만)";
        }

        private void FillHistory()
        {
            _historyGrid.Rows.Clear();
            foreach (LotRecord record in _source.GetLotHistory())
            {
                StatusKind verdict = YieldJudge.Judge(record.FinalYield);
                int index = _historyGrid.Rows.Add(
                    new ApcLampValue(verdict, StateWord(verdict)),
                    record.Time.ToString("HH:mm:ss"),
                    record.TesterId,
                    record.LotId,
                    record.CustomerCode,
                    record.Product,
                    record.FirstYield.ToString("0.0"),
                    record.FinalYield.ToString("0.0"),
                    record.TopSoftBin);
                if (YieldJudge.IsBelowLimit(record.FinalYield))
                {
                    DataGridViewCell cell = _historyGrid.Rows[index].Cells[FinalYieldColumn];
                    cell.Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                    cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
                }
            }
        }

        private void SelectTester(string testerId)
        {
            _selectedId = testerId;
            foreach (KeyValuePair<string, ApcStatusTile> pair in _tiles)
            {
                pair.Value.Selected = pair.Key == testerId;
            }

            TesterInfo tester = _testers.First(t => t.Id == testerId);
            LotDetail lot = _source.GetLot(testerId);
            bool stopped = tester.Process == StatusKind.Stop;

            _lotInfo.SetItems("테스터", "Lot ID", "고객", "제품", "TP 버전", "온도", "투입 / 진행");
            _lotInfo.SetValue("테스터", lot.TesterId);
            _lotInfo.SetValue("Lot ID", lot.LotId);
            _lotInfo.SetValue("고객", lot.CustomerCode);
            _lotInfo.SetValue("제품", lot.Product);
            _lotInfo.SetValue("TP 버전", lot.TpVersion);
            _lotInfo.SetValue("온도", lot.TestTemp.ToString("0.0") + " ℃");
            _lotInfo.SetValue("투입 / 진행", lot.Input.ToString("#,0") + " / " + lot.Processed.ToString("#,0"));

            ShowYieldCards(lot, stopped);
            ShowBins(lot);
            ShowTrend(testerId);

            bool hold = YieldJudge.IsBelowLimit(lot.FinalYield);
            _ocapButton.Enabled = hold;
            _ocapCaption.Text = hold ? "" : "선택 Lot은 기준 충족";
        }

        private void ShowYieldCards(LotDetail lot, bool stopped)
        {
            _firstCard.Value = lot.FirstYield.ToString("0.0");
            _firstCard.Footer = "양품 " + lot.FirstPassGood.ToString("#,0") + " / " + lot.Input.ToString("#,0");
            _firstCard.Status = stopped ? StatusKind.Stop : StatusKind.Run;

            _finalCard.Value = lot.FinalYield.ToString("0.0");
            _finalCard.Footer = "양품 " + lot.FinalGood.ToString("#,0") + " / " + lot.Input.ToString("#,0");
            _finalCard.Status = stopped ? StatusKind.Stop : YieldJudge.Judge(lot.FinalYield);
        }

        private void ShowBins(LotDetail lot)
        {
            _hardBox.Text = "Hard Bin 분포 · " + lot.TesterId;
            _hardGrid.Rows.Clear();
            foreach (BinCount bin in lot.HardBins)
            {
                int index = _hardGrid.Rows.Add(bin.Bin.ToString(), bin.Name, bin.Count.ToString("#,0"),
                    LotDetail.Percent(bin.Count, lot.Input).ToString("0.0"));
                if (bin.Bin == 1)
                {
                    _hardGrid.Rows[index].DefaultCellStyle.Font = ApcTheme.GetFont(FontStyle.Bold);
                }
            }

            _softBox.Text = "Soft Bin 상위 " + SoftTopCount + " · " + lot.TesterId;
            _softGrid.Rows.Clear();
            foreach (SoftBinCount soft in lot.SoftBins.Take(SoftTopCount))
            {
                _softGrid.Rows.Add("SB" + soft.SoftBin, soft.Cause, soft.HardBin.ToString(),
                    soft.Count.ToString("#,0"), LotDetail.Percent(soft.Count, lot.Input).ToString("0.0"));
            }
        }

        private void ShowTrend(string testerId)
        {
            IList<double> values = _source.GetYieldTrend(testerId);
            _trend.SetValues(values);
            _trend.XLabels = TrendLabels(values.Count);
            _trendBox.Text = "최근 " + values.Count + " Lot 최종 수율 · " + testerId;
        }

        private static string[] TrendLabels(int count)
        {
            var labels = new string[count];
            for (int i = 0; i < count; i++)
            {
                labels[i] = i == count - 1 ? "현재" : "-" + (count - 1 - i);
            }
            return labels;
        }

        private void OpenOcapDialog()
        {
            LotDetail lot = _source.GetLot(_selectedId);
            if (!YieldJudge.IsBelowLimit(lot.FinalYield)) return;

            using (var dialog = new OcapDialog(lot))
            {
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

                var decision = new LotDecision { LotId = lot.LotId, Decision = dialog.Decision, Reason = dialog.Reason };
                if (!_source.SaveDecision(decision))
                {
                    ApcDialog.Warn(FindForm(), "판정을 저장하지 못했다. 다시 시도한다.");
                    return;
                }
                ApcDialog.Alert(FindForm(), lot.LotId + " 판정을 기록했다.");
                ReloadTesters();
            }
        }

        private static StatusKind TileStatus(TesterInfo tester)
        {
            return tester.Process == StatusKind.Run ? YieldJudge.Judge(tester.FinalYield) : tester.Process;
        }

        private static string StateWord(StatusKind verdict)
        {
            switch (verdict)
            {
                case StatusKind.Alarm: return "Hold";
                case StatusKind.Warn: return "근접";
                default: return "정상";
            }
        }
    }
}
