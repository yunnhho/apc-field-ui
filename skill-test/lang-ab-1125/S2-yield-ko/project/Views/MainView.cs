using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>
    /// 메인 화면: 판정판 + 수율 카드 → 테스터 타일 6 → 선택 Lot 정보·스캔·처리 버튼 → Hard/Soft Bin·추이 → Lot 결과 이력.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int HardBinCount = 8, SoftBinTopCount = 5;
        private const string LotLabel = "Lot", NoneText = "";
        private static readonly string[] HardBinNames =
            { "양품", "Open/Short", "누설 전류", "기능 불량", "속도·주파수", "아날로그 한계", "파라메트릭", "기타" };

        private readonly IMainViewSource _source;
        private readonly IList<TesterInfo> _testers;
        private readonly ApcStatusTile[] _tiles;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcValueCard _first = Card("최초 수율", false);
        private readonly ApcValueCard _final = Card("최종 수율", true);
        private readonly ApcInfoStrip _strip = new ApcInfoStrip { Dock = DockStyle.Fill };
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly Label _reason = Caption("");
        private readonly ApcButton _ocap = Button("미달 Lot 처리", ApcButtonVariant.Primary, 150);
        private readonly ApcGrid _hardGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _softGrid = new ApcGrid { ReadOnly = true };
        private readonly ApcTrendChart _trend = new ApcTrendChart { Dock = DockStyle.Fill, LowerLimit = YieldRule.Limit };
        private readonly Panel _trendPanel;
        private int _selected;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("Final Test 수율 모니터링")
        {
            _source = source;
            _testers = source.GetTesters();

            _tiles = new ApcStatusTile[_testers.Count];
            var tileCells = new Control[_testers.Count];
            for (int i = 0; i < _testers.Count; i++)
            {
                int index = i;
                _tiles[i] = new ApcStatusTile { Dock = DockStyle.Fill };
                _tiles[i].Click += (s, e) => Select(index);
                tileCells[i] = _tiles[i];
            }

            _strip.SetItems("테스터", LotLabel + " ID", "고객", "제품", "테스트 프로그램", "온도 (℃)", "투입 수량 (EA)", "진행 수량 (EA)");
            _scan.Dock = DockStyle.Top;
            _scan.Scanned += (s, e) => e.Accepted = SelectByLot(e.Code);
            _reason.Dock = DockStyle.Top;
            _ocap.Dock = DockStyle.Top;
            _ocap.Click += (s, e) => OpenOcap();
            // 스캔 + 처리 버튼 칸(위→아래: 이름표, 스캔칸, 버튼, 못 누를 때 이유)
            var actions = new Panel { Dock = DockStyle.Fill };
            actions.Controls.Add(_reason);
            actions.Controls.Add(new Panel { Dock = DockStyle.Top, Height = S(6) });
            actions.Controls.Add(_ocap);
            actions.Controls.Add(new Panel { Dock = DockStyle.Top, Height = S(6) });
            actions.Controls.Add(_scan);
            var scanCaption = Caption("Lot 스캔");
            scanCaption.Dock = DockStyle.Top;
            actions.Controls.Add(scanCaption);

            BuildGrids();
            var historyGrid = BuildHistory(source.GetHistory());

            _trendPanel = Titled("최근 Lot 수율 추이 (%)", _trend);
            var softPanel = new Panel { Dock = DockStyle.Fill };
            _softGrid.Dock = DockStyle.Top;
            _softGrid.Height = ApcGrid.HeightForRows(SoftBinTopCount);
            softPanel.Controls.Add(_softGrid);
            var binRow = Columns(ApcGrid.HeightForRows(HardBinCount) + S(44),
                new Control[] { Titled("Hard Bin 분포", _hardGrid), Titled("Soft Bin 상위 5", softPanel), _trendPanel },
                new[] { 34f, 38f, 28f });

            // Top 도킹은 나중에 Add한 것이 위 → 이력(Fill) 먼저, 그다음 아래 줄부터
            Body.Controls.Add(Titled("Lot 결과 이력 (오늘)", historyGrid));
            Body.Controls.Add(binRow);
            Body.Controls.Add(Columns(S(40), new Control[] { _strip }));
            Body.Controls.Add(Columns(S(64), tileCells));
            Body.Controls.Add(Columns(S(150), new Control[] { _judge, _first, _final, actions }, new[] { 42f, 16f, 16f, 26f }));

            // 처음 보여 줄 테스터: 조치가 필요한(기준 미달) 테스터, 없으면 첫 테스터
            int start = 0;
            for (int i = 0; i < _testers.Count; i++) if (IsBelow(_testers[i])) { start = i; break; }
            Select(start);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);   // 생성자 시점엔 폼이 없다
        }

        private static ApcValueCard Card(string title, bool lamp)
        {
            return new ApcValueCard { Title = title, Unit = "%", ShowLamp = lamp, Dock = DockStyle.Fill };
        }

        private static bool IsBelow(TesterInfo t) { return t.Lot != null && YieldRule.IsBelow(t.Lot.FinalYield); }

        private void BuildGrids()
        {
            _hardGrid.Columns.Add("bin", "Hard Bin");
            _hardGrid.Columns.Add("name", "구분");
            _hardGrid.Columns.Add("qty", "수량");
            _hardGrid.Columns.Add("pct", "비율 (%)");
            FillAll(_hardGrid);

            _softGrid.Columns.Add("bin", "Soft Bin");
            _softGrid.Columns.Add("name", "불량 원인");
            _softGrid.Columns.Add("hard", "Hard Bin");
            _softGrid.Columns.Add("qty", "수량");
            _softGrid.Columns.Add("pct", "비율 (%)");
            FillAll(_softGrid);
        }

        private static void FillAll(ApcGrid grid)
        {
            foreach (DataGridViewColumn c in grid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private ApcGrid BuildHistory(IList<HistoryRow> rows)
        {
            var g = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            g.Columns.Add(new ApcLampColumn());
            g.Columns.Add("time", "종료 시각");
            g.Columns.Add("lot", "Lot ID");
            g.Columns.Add("cust", "고객");
            g.Columns.Add("product", "제품");
            g.Columns.Add("tester", "테스터");
            g.Columns.Add("input", "투입 수량 (EA)");
            g.Columns.Add("first", "최초 수율 (%)");
            g.Columns.Add("final", "최종 수율 (%)");
            g.Columns.Add("result", "판정");
            g.Columns["result"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            g.Columns["result"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            foreach (var r in rows)
            {
                bool below = YieldRule.IsBelow(r.FinalYield);
                string result = !below ? "OK" : (r.Disposition.Length > 0 ? "기준 미달 · " + r.Disposition : "기준 미달");
                int i = g.Rows.Add(below ? StatusKind.Alarm : StatusKind.Run, r.Time, r.LotId, r.Customer, r.Product, r.Tester,
                    YieldRule.Qty(r.InputQty), YieldRule.Pct(r.FirstYield), YieldRule.Pct(r.FinalYield), result);
                if (below)
                {
                    var cell = g.Rows[i].Cells["final"];
                    cell.Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                    cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
                }
            }
            return g;
        }

        private bool SelectByLot(string code)
        {
            string lot = (code ?? "").Trim();
            for (int i = 0; i < _testers.Count; i++)
            {
                if (_testers[i].Lot != null && string.Equals(_testers[i].Lot.LotId, lot, StringComparison.OrdinalIgnoreCase))
                {
                    Select(i);
                    return true;
                }
            }
            // 스캔 이벤트 처리가 끝난 뒤 경고창
            BeginInvoke((Action)(() => ApcDialog.Warn(FindForm(), "진행 중인 Lot이 아닙니다 · " + lot)));
            return false;
        }

        private void Select(int index)
        {
            _selected = index;
            for (int i = 0; i < _tiles.Length; i++)
            {
                var t = _testers[i];
                _tiles[i].Selected = i == index;
                _tiles[i].Status = IsBelow(t) ? StatusKind.Alarm : TileStatus(t.State);
                _tiles[i].Text = t.Lot == null ? t.Id : t.Id + "  " + YieldRule.Pct(t.Lot.FinalYield) + " %";
            }
            Reload();
        }

        private static StatusKind TileStatus(TesterState s)
        {
            switch (s)
            {
                case TesterState.Testing: return StatusKind.Run;
                case TesterState.Alarm: return StatusKind.Alarm;
                default: return StatusKind.Stop;
            }
        }

        private void Reload()
        {
            var t = _testers[_selected];
            var lot = t.Lot;
            ShowJudge(t);

            _first.Value = lot == null ? "" : YieldRule.Pct(lot.FirstYield);
            _first.Footer = lot == null ? "" : "재테스트 전";
            _first.Status = lot == null || t.State == TesterState.Alarm ? StatusKind.Stop : StatusKind.Run;
            _final.Value = lot == null ? "" : YieldRule.Pct(lot.FinalYield);
            _final.Footer = lot == null ? "" : "양품 " + YieldRule.Qty(lot.HardBins[0]) + " / " + YieldRule.Qty(lot.DoneQty);
            _final.Status = lot == null ? StatusKind.Stop : (IsBelow(t) ? StatusKind.Alarm : StatusKind.Run);

            _strip.SetValue("테스터", t.Id);
            _strip.SetValue(LotLabel + " ID", lot == null ? NoneText : lot.LotId);
            _strip.SetValue("고객", lot == null ? NoneText : lot.Customer);
            _strip.SetValue("제품", lot == null ? NoneText : lot.Product);
            _strip.SetValue("테스트 프로그램", lot == null ? NoneText : lot.ProgramVersion);
            _strip.SetValue("온도 (℃)", lot == null ? NoneText : lot.Temperature.ToString("0.0"));
            _strip.SetValue("투입 수량 (EA)", lot == null ? NoneText : YieldRule.Qty(lot.InputQty));
            _strip.SetValue("진행 수량 (EA)", lot == null ? NoneText : YieldRule.Qty(lot.DoneQty));

            bool below = IsBelow(t);
            _ocap.Enabled = below;
            _reason.Text = below ? "" : (lot == null ? "대기 중 · 처리할 Lot 없음" : "처리할 미달 Lot 없음");

            ShowBins(lot);
            _trend.SetValues(t.Trend);
            _trend.XLabels = new[] { "과거", "최근" };
            _trendPanel.Text = "최근 Lot 수율 추이 (%) · " + t.Id;
        }

        private void ShowJudge(TesterInfo t)
        {
            if (t.Lot == null)
            {
                _judge.Status = StatusKind.Stop; _judge.Text = "대기"; _judge.SubText = t.Id + " 투입 Lot 없음";
                return;
            }
            string basis = t.Id + " 최종 수율 " + YieldRule.Pct(t.Lot.FinalYield) + " % · 기준 " + YieldRule.Pct(YieldRule.Limit) + " 이상";
            if (IsBelow(t)) { _judge.Status = StatusKind.Alarm; _judge.Text = "기준 미달 · Lot Hold"; }
            else { _judge.Status = StatusKind.Run; _judge.Text = "수율 정상"; }
            _judge.SubText = basis;
        }

        private void ShowBins(LotInfo lot)
        {
            _hardGrid.Rows.Clear();
            for (int i = 0; i < HardBinCount; i++)
            {
                if (lot == null) _hardGrid.Rows.Add((i + 1).ToString(), HardBinNames[i], "", "");
                else _hardGrid.Rows.Add((i + 1).ToString(), HardBinNames[i], YieldRule.Qty(lot.HardBins[i]),
                    YieldRule.Pct((double)lot.HardBins[i] * 100.0 / lot.DoneQty));
            }
            _softGrid.Rows.Clear();
            for (int i = 0; i < SoftBinTopCount; i++)
            {
                if (lot == null) { _softGrid.Rows.Add("", "", "", "", ""); continue; }
                var s = lot.SoftTop[i];
                _softGrid.Rows.Add(s.Soft.ToString(), s.Name, s.Hard.ToString(), YieldRule.Qty(s.Qty),
                    YieldRule.Pct((double)s.Qty * 100.0 / lot.DoneQty));
            }
        }

        private void OpenOcap()
        {
            var t = _testers[_selected];
            if (!IsBelow(t)) return;
            using (var dlg = new OcapDialog(t))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.SaveOcapDecision(t.Lot.LotId, dlg.Decision, dlg.Reason);
                ApcDialog.Alert(FindForm(), t.Lot.LotId + " 판정 기록 · " + dlg.Decision);
            }
        }
    }
}
