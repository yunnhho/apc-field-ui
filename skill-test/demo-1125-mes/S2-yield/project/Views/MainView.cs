using System;
using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>
    /// 메인 화면: 선택 테스터 판정판(맨 위) → 테스터 타일 → 현재 Lot·수율 카드 → Hard/Soft Bin·추이 → Lot 결과 이력.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int TrendLots = 12;

        private readonly IMainViewSource _source;
        private readonly IList<TesterInfo> _testers;
        private readonly IList<LotResult> _history;
        private readonly ApcStatusTile[] _tiles;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcButton _process = Button("Lot 처리", ApcButtonVariant.Primary, 200);
        private readonly Label _processNote = Caption("");
        private readonly ApcGroup _lotGroup = new ApcGroup { Dock = DockStyle.Fill };
        private readonly ApcInfoStrip _lotStrip = new ApcInfoStrip { Dock = DockStyle.Top };
        private readonly ApcInfoStrip _testStrip = new ApcInfoStrip { Dock = DockStyle.Top };
        private readonly ApcValueCard _firstCard = new ApcValueCard { Title = "최초 수율", Unit = "%", Dock = DockStyle.Fill };
        private readonly ApcValueCard _finalCard = new ApcValueCard { Title = "최종 수율", Unit = "%", ShowLamp = true, Dock = DockStyle.Fill };
        private readonly ApcGrid _hardGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _softGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcTrendChart _trend = new ApcTrendChart { Dock = DockStyle.Fill };
        private int _selected;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("Final Test 수율·Bin 모니터링")
        {
            _source = source;
            _testers = source.GetTesters();
            _history = source.GetHistory();

            _tiles = new ApcStatusTile[_testers.Count];
            var tiles = new Control[_testers.Count];
            for (int i = 0; i < _testers.Count; i++)
            {
                int index = i;
                _tiles[i] = new ApcStatusTile { Dock = DockStyle.Fill };
                _tiles[i].Click += (s, e) => ShowTester(index);
                tiles[i] = _tiles[i];
            }

            // 판정판 줄: 큰 판정 + Lot 처리 버튼(기준 미달 때만) 와 이유 한 줄
            var action = new Panel { Dock = DockStyle.Fill };
            _processNote.Dock = DockStyle.Top;
            _process.Dock = DockStyle.Top;
            action.Controls.Add(_processNote);
            action.Controls.Add(_process);
            action.Padding = new Padding(S(8), S(12), 0, 0);
            _process.Click += (s, e) => OpenProcess();

            // 현재 Lot 묶음: 두 줄 정보판 + 수율 카드 둘
            _lotStrip.SetItems("Lot ID", "고객", "제품");
            _testStrip.SetItems("테스트 프로그램", "온도 (℃)", "진행 / 투입");
            _lotStrip.Height = S(40);
            _testStrip.Height = S(40);
            _lotGroup.Controls.Add(_testStrip);
            _lotGroup.Controls.Add(_lotStrip);
            int lotRowHeight = _lotGroup.HeightFor(S(40) * 2 + S(8));

            _hardGrid.Columns.Add("bin", "Hard Bin");
            _hardGrid.Columns.Add("name", "구분");
            _hardGrid.Columns.Add("count", "수량");
            _hardGrid.Columns.Add("pct", "비율(%)");
            _softGrid.Columns.Add("bin", "Soft Bin");
            _softGrid.Columns.Add("name", "원인");
            _softGrid.Columns.Add("hard", "Hard Bin");
            _softGrid.Columns.Add("count", "수량");
            _softGrid.Columns.Add("pct", "비율(%)");
            _softGrid.Columns["name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _softGrid.Columns["name"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            _hardGrid.Columns["name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _hardGrid.Columns["name"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            var history = BuildHistoryGrid();
            var historyArea = new Panel { Dock = DockStyle.Fill };
            historyArea.Controls.Add(history);
            historyArea.Controls.Add(Titled("Lot 결과 이력 (최종 수율 " + YieldRules.SpecText + ")", null));

            int binRowHeight = ApcGrid.HeightForRows(8) + S(50);
            Body.Controls.Add(historyArea);
            Body.Controls.Add(Columns(binRowHeight, new Control[]
            {
                Titled("Hard Bin 분포", _hardGrid),
                Titled("Soft Bin 상위 5", _softGrid),
                Titled("최근 Lot 최종 수율 추이 (%)", _trend),
            }, new[] { 34f, 36f, 30f }));
            Body.Controls.Add(Columns(lotRowHeight, new Control[] { _lotGroup, _firstCard, _finalCard }, new[] { 60f, 20f, 20f }));
            Body.Controls.Add(Columns(S(64), tiles));
            Body.Controls.Add(Columns(S(104), new Control[] { _judge, action }, new[] { 80f, 20f }));

            int first = 0;
            for (int i = 0; i < _testers.Count; i++) if (_testers[i].Status == StatusKind.Alarm) { first = i; break; }
            ShowTester(first);
        }

        // 구획 제목(막대 + 아래 구분선) + 내용. content가 null이면 제목만.
        private static Control Titled(string title, Control content)
        {
            var title_ = SectionTitle(title);
            title_.Dock = DockStyle.Top;
            if (content == null) return title_;
            var p = new Panel { Dock = DockStyle.Fill };
            p.Controls.Add(content);
            p.Controls.Add(title_);
            return p;
        }

        private ApcGrid BuildHistoryGrid()
        {
            var g = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            g.Columns.Add(new ApcLampColumn());
            g.Columns.Add("time", "완료 시각");
            g.Columns["time"].Width = S(190);
            g.Columns.Add("lot", "Lot ID");
            g.Columns.Add("cust", "고객");
            g.Columns.Add("prod", "제품");
            g.Columns.Add("tester", "테스터");
            g.Columns.Add("input", "투입 수량");
            g.Columns.Add("first", "최초 수율(%)");
            g.Columns.Add("final", "최종 수율(%)");
            g.Columns.Add("result", "판정");
            g.Columns["result"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            g.Columns["result"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            foreach (var r in _history)
            {
                var kind = YieldRules.Judge(r.FinalYield);
                string text = kind == StatusKind.Alarm ? "기준 미달 · " + r.Disposition
                    : kind == StatusKind.Warn ? "기준 경계" : "양호";
                int row = g.Rows.Add(kind, r.Time, r.LotId, r.Customer, r.Product, r.Tester,
                    YieldRules.Qty(r.Input), YieldRules.Pct(r.FirstYield), YieldRules.Pct(r.FinalYield), text);
                if (kind == StatusKind.Alarm)
                {
                    var c = g.Rows[row].Cells["final"];
                    c.Style.ForeColor = ApcTheme.StatusText(kind);
                    c.Style.Font = ApcTheme.GetFont(System.Drawing.FontStyle.Bold);
                }
            }
            return g;
        }

        private void ShowTester(int index)
        {
            _selected = index;
            var t = _testers[index];
            for (int i = 0; i < _tiles.Length; i++)
            {
                var x = _testers[i];
                _tiles[i].Status = x.Status;
                _tiles[i].Selected = i == index;
                _tiles[i].Text = x.HasLot
                    ? x.Name + "  " + x.LotId + "  " + YieldRules.Pct(x.FinalYield) + " %"
                    : x.Name + "  " + x.State;
            }

            _lotGroup.Text = t.Name + " · 현재 Lot";
            _lotStrip.SetValue("Lot ID", t.LotId);
            _lotStrip.SetValue("고객", t.Customer);
            _lotStrip.SetValue("제품", t.Product);
            _testStrip.SetValue("테스트 프로그램", t.ProgramVersion);
            _testStrip.SetValue("온도 (℃)", t.HasLot ? t.Temperature.ToString("0.0") : "");
            _testStrip.SetValue("진행 / 투입", t.HasLot ? YieldRules.Qty(t.Progress) + " / " + YieldRules.Qty(t.Input) : "");

            ShowYield(t);
            ShowBins(t);
            ShowTrend(t);
        }

        private void ShowYield(TesterInfo t)
        {
            var kind = t.HasLot ? YieldRules.Judge(t.FinalYield) : StatusKind.Stop;
            _firstCard.Value = t.HasLot ? YieldRules.Pct(t.FirstYield) : "";
            _firstCard.Footer = t.HasLot ? "재테스트 전" : "";
            _firstCard.Status = t.HasLot ? StatusKind.Run : StatusKind.Stop;
            _finalCard.Value = t.HasLot ? YieldRules.Pct(t.FinalYield) : "";
            _finalCard.Footer = t.HasLot ? YieldRules.SpecText : "";
            _finalCard.Status = kind;

            _judge.Status = kind;
            if (!t.HasLot) { _judge.Text = "대기"; _judge.SubText = "시험 중인 Lot 없음"; }
            else if (kind == StatusKind.Alarm) { _judge.Text = "기준 미달 · Lot Hold"; _judge.SubText = t.LotId + " 최종 " + YieldRules.Pct(t.FinalYield) + " % · " + YieldRules.SpecText; }
            else if (kind == StatusKind.Warn) { _judge.Text = "기준 경계"; _judge.SubText = t.LotId + " 최종 " + YieldRules.Pct(t.FinalYield) + " % · " + YieldRules.SpecText; }
            else { _judge.Text = "수율 양호"; _judge.SubText = t.LotId + " 최종 " + YieldRules.Pct(t.FinalYield) + " % · " + YieldRules.SpecText; }

            bool need = t.HasLot && kind == StatusKind.Alarm;
            _process.Enabled = need;
            _processNote.Text = need ? "OCAP 판정이 필요합니다" : (t.HasLot ? "처리 대상 아님 · 기준 이상" : "처리 대상 아님 · Lot 없음");
        }

        private void ShowBins(TesterInfo t)
        {
            _hardGrid.Rows.Clear();
            foreach (var r in _source.GetHardBins(t))
                _hardGrid.Rows.Add(r.Bin.ToString(), r.Name, YieldRules.Qty(r.Count), YieldRules.Pct(r.Percent));
            _softGrid.Rows.Clear();
            foreach (var r in _source.GetSoftTop(t, 5))
                _softGrid.Rows.Add(r.Bin.ToString(), r.Name, r.HardBin.ToString(), YieldRules.Qty(r.Count), YieldRules.Pct(r.Percent));
        }

        // 최근 끝난 Lot의 최종 수율(과거→현재) + 선택 테스터의 지금 Lot를 마지막 점으로
        private void ShowTrend(TesterInfo t)
        {
            var values = new List<double>();
            var lots = new List<string>();
            for (int i = Math.Min(_history.Count, TrendLots - 1) - 1; i >= 0; i--)
            {
                values.Add(_history[i].FinalYield);
                lots.Add(_history[i].LotId);
            }
            if (t.HasLot) { values.Add(t.FinalYield); lots.Add(t.LotId); }
            _trend.LowerLimit = YieldRules.SpecFinal;
            _trend.SetValues(values);
            _trend.XLabels = new[] { lots[0], lots[lots.Count / 2], lots[lots.Count - 1] };
        }

        private void OpenProcess()
        {
            var t = _testers[_selected];
            if (!t.HasLot || YieldRules.Judge(t.FinalYield) != StatusKind.Alarm) return;
            using (var dlg = new OcapDialog(t, _source))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                ApcDialog.Alert(FindForm(), t.LotId + " 판정 '" + dlg.Disposition + "'을 기록했습니다.");
            }
        }
    }
}
