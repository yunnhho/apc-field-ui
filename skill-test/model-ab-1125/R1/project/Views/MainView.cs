using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>메인 화면. 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int SelectedAtStart = 3;   // 샘플 시작 시 선택 테스터(T04)

        private readonly IMainViewSource _source;
        private readonly IList<TesterInfo> _testers;
        private readonly List<ApcStatusTile> _tiles = new List<ApcStatusTile>();
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcValueCard _firstCard = new ApcValueCard { Title = "최초 수율", Unit = "%", Dock = DockStyle.Fill };
        private readonly ApcValueCard _finalCard = new ApcValueCard { Title = "최종 수율", Unit = "%", ShowLamp = true, Dock = DockStyle.Fill };
        private readonly ApcInfoStrip _info = new ApcInfoStrip { Dock = DockStyle.Fill };
        private readonly ApcButton _handle = Button("기준 미달 Lot 처리", ApcButtonVariant.Primary, 200);
        private readonly Label _handleReason = Caption("");
        private readonly ApcGrid _hardGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _softGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcTrendChart _trend = new ApcTrendChart { Dock = DockStyle.Fill, LowerLimit = (double)YieldJudge.MinFinalYield, XLabels = new[] { "12 Lot 전", "6 Lot 전", "현재" } };
        private Panel _lotSection;
        private int _selected;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("Final Test 수율·Bin 모니터링")
        {
            _source = source;
            _testers = source.GetTesters();

            var tiles = new Control[_testers.Count];
            for (int i = 0; i < _testers.Count; i++)
            {
                int index = i;
                var tile = new ApcStatusTile { Dock = DockStyle.Fill };
                tile.Click += (s, e) => Select(index);
                _tiles.Add(tile);
                tiles[i] = tile;
            }

            _info.SetItems("테스터", "Lot ID", "고객", "제품", "프로그램", "온도 (℃)", "투입 / 진행 (EA)");
            _handle.Click += (s, e) => HandleLowYieldLot();
            foreach (var g in new[] { _hardGrid, _softGrid }) { g.ClearInitialSelection = true; }
            BuildHardGrid(); BuildSoftGrid();

            _lotSection = Titled("현재 Lot", _info, _handleReason, _handle);
            // Fill(이력) 먼저, 그다음 아래 줄부터 위 줄 순서
            Body.Controls.Add(Titled("Lot 결과 이력 (최종 수율 95.0 % 미만 = 기준 미달)", BuildHistory()));
            Body.Controls.Add(Columns(S(40) + ApcGrid.HeightForRows(8), new Control[]
            {
                Titled("Hard Bin 분포", _hardGrid), Titled("Soft Bin 상위 5", _softGrid), Titled("최근 Lot 최종 수율 추이 (빨간 점선 = 기준 95.0 %)", _trend),
            }, new[] { 34f, 34f, 32f }));
            Body.Controls.Add(Columns(S(40) + S(48), new Control[] { _lotSection }));
            Body.Controls.Add(Columns(S(104), new Control[] { _judge, _firstCard, _finalCard }, new[] { 50f, 25f, 25f }));
            Body.Controls.Add(Columns(S(64), tiles));

            Select(SelectedAtStart);
        }

        private void BuildHardGrid()
        {
            _hardGrid.Columns.Add("bin", "Hard Bin");
            _hardGrid.Columns.Add("name", "구분");
            _hardGrid.Columns.Add("qty", "수량");
            _hardGrid.Columns.Add("rate", "비율 (%)");
            foreach (DataGridViewColumn c in _hardGrid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void BuildSoftGrid()
        {
            _softGrid.Columns.Add("sb", "Soft Bin");
            _softGrid.Columns.Add("hb", "Hard Bin");
            _softGrid.Columns.Add("cause", "원인");
            _softGrid.Columns.Add("qty", "수량");
            _softGrid.Columns.Add("rate", "비율 (%)");
            foreach (DataGridViewColumn c in _softGrid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private ApcGrid BuildHistory()
        {
            var grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            grid.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            grid.Columns.Add("time", "종료 시각");
            grid.Columns.Add("lot", "Lot ID");
            grid.Columns.Add("tester", "테스터");
            grid.Columns.Add("customer", "고객");
            grid.Columns.Add("product", "제품");
            grid.Columns.Add("input", "투입 수량 (EA)");
            grid.Columns.Add("first", "최초 수율 (%)");
            grid.Columns.Add("final", "최종 수율 (%)");
            grid.Columns.Add("result", "판정");
            foreach (DataGridViewColumn c in grid.Columns) if (c.Index > 0) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (var r in _source.GetRecentResults())
            {
                bool below = YieldJudge.IsBelow(r.FinalYield);
                string result = below ? "기준 미달 · " + (r.Disposition.Length > 0 ? r.Disposition : "Hold") : "합격";
                int row = grid.Rows.Add(YieldJudge.Kind(r.FinalYield), r.EndTime.ToString("yyyy-MM-dd HH:mm:ss"), r.LotId, r.TesterId, r.Customer, r.Product,
                    r.Input.ToString("#,0"), YieldJudge.Text(r.FirstYield), YieldJudge.Text(r.FinalYield), result);
                if (below)
                {
                    foreach (var name in new[] { "final", "result" })
                    {
                        var style = grid.Rows[row].Cells[name].Style;
                        style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                        style.Font = ApcTheme.GetFont(FontStyle.Bold);
                    }
                }
            }
            return grid;
        }

        private void Select(int index)
        {
            _selected = index;
            _lotSection.Text = _testers[index].Id + " 현재 Lot";
            for (int i = 0; i < _testers.Count; i++)
            {
                var t = _testers[i];
                var tile = _tiles[i];
                tile.Selected = i == index;
                tile.Status = t.TileStatus;
                tile.Text = t.Lot == null ? t.Id : t.Id + "  " + YieldJudge.Text(t.Lot.FinalYield) + " %";
            }
            Reload(_testers[index]);
        }

        private void Reload(TesterInfo t)
        {
            var lot = t.Lot;
            _hardGrid.Rows.Clear(); _softGrid.Rows.Clear();
            _trend.SetValues(lot == null ? new double[0] : lot.RecentFinalYields);

            if (lot == null)
            {
                _judge.Status = StatusKind.Stop; _judge.Text = "대기"; _judge.SubText = t.Id + " · 투입된 Lot 없음";
                foreach (var c in new[] { _firstCard, _finalCard }) { c.Value = ""; c.Footer = ""; c.Status = StatusKind.Stop; }
                foreach (var n in new[] { "Lot ID", "고객", "제품", "프로그램", "온도 (℃)", "투입 / 진행 (EA)" }) _info.SetValue(n, "");
                _info.SetValue("테스터", t.Id);
                _handle.Enabled = false; _handleReason.Text = "";
                return;
            }

            decimal fin = lot.FinalYield;
            bool below = YieldJudge.IsBelow(fin);
            _judge.Status = below ? StatusKind.Alarm : StatusKind.Run;
            _judge.Text = below ? "기준 미달 · Lot Hold" : "합격 범위";
            _judge.SubText = t.Id + " · 최종 수율 " + YieldJudge.Text(fin) + " % (기준 " + YieldJudge.MinFinalYield.ToString("0.0") + " 이상)";

            var run = t.Process == StatusKind.Stop ? StatusKind.Stop : StatusKind.Run;
            _firstCard.Value = YieldJudge.Text(lot.FirstYield); _firstCard.Status = run;
            _firstCard.Footer = "양품 " + lot.GoodFirst.ToString("#,0") + " EA";
            _finalCard.Value = YieldJudge.Text(fin); _finalCard.Status = YieldJudge.Kind(fin);
            _finalCard.Footer = "양품 " + lot.GoodFinal.ToString("#,0") + " EA";

            _info.SetValue("테스터", t.Id);
            _info.SetValue("Lot ID", lot.LotId);
            _info.SetValue("고객", lot.Customer);
            _info.SetValue("제품", lot.Product);
            _info.SetValue("프로그램", lot.ProgramVersion);
            _info.SetValue("온도 (℃)", lot.TempC.ToString("0.0"));
            _info.SetValue("투입 / 진행 (EA)", lot.Input.ToString("#,0") + " / " + lot.Tested.ToString("#,0"));

            _handle.Enabled = below;
            _handleReason.Text = below ? "" : "기준 미달 Lot 없음";

            foreach (var b in lot.HardBins)
                _hardGrid.Rows.Add(b.Bin.ToString(), b.Name, b.Count.ToString("#,0"), Percent(b.Count, lot.Tested));
            foreach (var s in lot.SoftBinTop)
                _softGrid.Rows.Add(s.SoftBin.ToString(), s.HardBin.ToString(), s.Cause, s.Count.ToString("#,0"), Percent(s.Count, lot.Tested));
        }

        private static string Percent(int count, int tested)
        {
            return tested == 0 ? "" : (count * 100m / tested).ToString("0.0");
        }

        private void HandleLowYieldLot()
        {
            var t = _testers[_selected];
            if (t.Lot == null || !YieldJudge.IsBelow(t.Lot.FinalYield)) return;
            using (var dlg = new LowYieldLotDialog(t.Id, t.Lot, t.Process == StatusKind.Alarm))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.RecordDisposition(t.Lot.LotId, dlg.Disposition, dlg.Reason);
                ApcDialog.Alert(FindForm(), "Lot " + t.Lot.LotId + " 판정을 기록했습니다.");
            }
        }
    }
}
