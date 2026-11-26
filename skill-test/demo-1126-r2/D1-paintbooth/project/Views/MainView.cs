using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using BumperPaintBoothMonitor.Data;

namespace BumperPaintBoothMonitor.Views
{
    /// <summary>
    /// 도장 라인 부스 환경 모니터링: 판정판(지금 해야 할 것) + 현재 투입 → 부스 타일 → 선택 부스 현재값 카드
    /// → 8시간 추이 → 환경 이탈 이력(+ 조치 등록 dialog).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private static readonly string[] StatusWords = { "정상", "주의", "이탈", "정지" };

        private readonly IMainViewSource _source;
        private readonly ApcStatusTile[] _tiles = new ApcStatusTile[BoothSpec.BoothNames.Length];
        private readonly ApcValueCard[] _cards = new ApcValueCard[4];
        private readonly ApcJudgePanel _judge = new ApcJudgePanel();
        private readonly ApcInfoStrip _feed = new ApcInfoStrip();
        private readonly ApcTrendChart _tempChart = new ApcTrendChart(), _humiChart = new ApcTrendChart();
        private readonly Panel _tempTitled, _humiTitled;
        private readonly ApcGrid _grid = new ApcGrid { ReadOnly = true, ClearInitialSelection = false };
        private readonly ApcButton _register = Button("조치 등록", ApcButtonVariant.Primary, 130);
        private readonly Label _registerReason = Caption("");
        private BoothReading[] _readings;
        private int _selected;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("도장 라인 부스 환경 모니터링")
        {
            _source = source;
            _readings = source.GetReadings();

            for (int i = 0; i < _tiles.Length; i++)
            {
                int booth = i;
                _tiles[i] = new ApcStatusTile { Dock = DockStyle.Fill };
                _tiles[i].Click += (s, e) => SelectBooth(booth);
            }
            for (int i = 0; i < _cards.Length; i++)
                _cards[i] = new ApcValueCard { Dock = DockStyle.Fill, ShowLamp = true };

            _feed.SetItems("투입 행거", "도료 Lot", "색상 코드");
            _tempChart.Dock = DockStyle.Fill;
            _humiChart.Dock = DockStyle.Fill;
            _tempTitled = Titled("", _tempChart);
            _humiTitled = Titled("", _humiChart);
            BuildGrid();

            _register.Click += (s, e) => OpenActionDialog();
            _grid.SelectionChanged += (s, e) => UpdateRegisterGate();

            var history = Titled("환경 이탈 이력", _grid, _registerReason, _register);
            Body.Controls.Add(history);
            Body.Controls.Add(Columns(TitledHeight(S(150)), new Control[] { _tempTitled, _humiTitled }));
            Body.Controls.Add(Columns(S(120), _cards));
            Body.Controls.Add(Columns(S(90), _tiles));
            Body.Controls.Add(Columns(S(110), new Control[] { _judge, Titled("현재 투입 중", _feed) }, new float[] { 62, 38 }));

            _selected = FirstAbnormalBooth();
            Reload();
        }

        // ---- 판정 ----

        private static int Rank(StatusKind kind)
        {
            return kind == StatusKind.Alarm ? 3 : kind == StatusKind.Warn ? 2 : kind == StatusKind.Run ? 1 : 0;
        }

        /// <summary>항목 하나의 판정(정지한 부스는 판정하지 않는다).</summary>
        private static StatusKind ItemStatus(BoothReading r, MeasureItem item)
        {
            double? v = r.Values[(int)item];
            if (!r.Running || !v.HasValue) return StatusKind.Stop;
            return BoothSpec.Judge(BoothSpec.Get(r.Booth, item), v.Value);
        }

        private static StatusKind BoothStatus(BoothReading r)
        {
            if (!r.Running) return StatusKind.Stop;
            StatusKind worst = StatusKind.Run;
            foreach (MeasureItem item in Enum.GetValues(typeof(MeasureItem)))
            {
                var k = ItemStatus(r, item);
                if (k != StatusKind.Stop && Rank(k) > Rank(worst)) worst = k;
            }
            return worst;
        }

        private static string StatusWord(StatusKind kind)
        {
            return kind == StatusKind.Run ? StatusWords[0] : kind == StatusKind.Warn ? StatusWords[1]
                : kind == StatusKind.Alarm ? StatusWords[2] : StatusWords[3];
        }

        private int FirstAbnormalBooth()
        {
            int best = 0, bestRank = -1;
            for (int i = 0; i < _readings.Length; i++)
            {
                int rank = Rank(BoothStatus(_readings[i]));
                if (rank > bestRank) { best = i; bestRank = rank; }
            }
            return best;
        }

        /// <summary>"베이스 습도 76 %RH 상한 초과" 꼴의 현재 상태 글.</summary>
        private string CauseText(int booth, MeasureItem item)
        {
            var spec = BoothSpec.Get(booth, item);
            double v = _readings[booth].Values[(int)item].Value;
            string verdict = BoothSpec.JudgeText(spec, v);
            return BoothSpec.BoothNames[booth] + " " + spec.Name + " " + spec.Format(v) + " " + spec.Unit
                + (verdict.Length > 0 ? " " + verdict : "");
        }

        // ---- 갱신 ----

        private void Reload()
        {
            _readings = _source.GetReadings();
            for (int i = 0; i < _tiles.Length; i++)
            {
                var kind = BoothStatus(_readings[i]);
                _tiles[i].Status = kind;
                _tiles[i].Text = BoothSpec.BoothNames[i] + "  " + StatusWord(kind);
                _tiles[i].Selected = i == _selected;
            }
            ReloadFeed();
            ReloadSelectedBooth();
            ReloadHistory();
            ReloadJudge();
        }

        private void ReloadFeed()
        {
            var feed = _source.GetFeed();
            _feed.SetValue("투입 행거", feed.Hanger);
            _feed.SetValue("도료 Lot", feed.PaintLot);
            _feed.SetValue("색상 코드", feed.ColorCode);
        }

        private void SelectBooth(int booth)
        {
            _selected = booth;
            for (int i = 0; i < _tiles.Length; i++) _tiles[i].Selected = i == booth;
            ReloadSelectedBooth();
        }

        private void ReloadSelectedBooth()
        {
            var r = _readings[_selected];
            string booth = BoothSpec.BoothNames[_selected];
            for (int i = 0; i < _cards.Length; i++)
            {
                var item = (MeasureItem)i;
                var spec = BoothSpec.Get(_selected, item);
                double? v = r.Values[i];
                var kind = ItemStatus(r, item);
                string verdict = (v.HasValue && r.Running) ? BoothSpec.JudgeText(spec, v.Value) : "";
                var card = _cards[i];
                card.Title = booth + " · " + spec.Name;
                card.Unit = spec.Managed ? spec.Unit : "";
                card.Value = v.HasValue ? spec.Format(v.Value) : "";
                card.Status = kind;
                card.Footer = !spec.Managed ? "관리 안 함"
                    : "관리 " + spec.RangeText + (verdict.Length > 0 ? " · " + verdict : "");
            }
            ShowTrend(_tempTitled, _tempChart, MeasureItem.Temperature);
            ShowTrend(_humiTitled, _humiChart, MeasureItem.Humidity);
        }

        private void ShowTrend(Panel titled, ApcTrendChart chart, MeasureItem item)
        {
            var spec = BoothSpec.Get(_selected, item);
            string title = BoothSpec.BoothNames[_selected] + " " + spec.Name + " (" + spec.Unit + ") · 최근 8시간";
            var values = _source.GetTrend(_selected, item);
            chart.SetValues(values);
            chart.Reference = spec.Managed ? spec.Center : (double?)null;
            chart.UpperLimit = spec.Managed ? spec.Upper : (double?)null;
            chart.LowerLimit = spec.Managed ? spec.Lower : (double?)null;
            DateTime now = _source.Now;
            chart.XLabels = new[] { Hm(now.AddHours(-8)), Hm(now.AddHours(-6)), Hm(now.AddHours(-4)), Hm(now.AddHours(-2)), Hm(now) };
            titled.Text = spec.Managed ? title : title + " · 관리 안 함";
        }

        private static string Hm(DateTime t) { return t.ToString("HH:mm"); }

        // ---- 판정판 ----

        private void ReloadJudge()
        {
            var events = _source.GetDeviations();
            DateTime now = _source.Now;
            DeviationEvent ongoing = null;
            int pending = 0;
            foreach (var e in events)
            {
                if (e.Ongoing && (ongoing == null || e.Start < ongoing.Start)) ongoing = e;
                else if (!e.Ongoing && e.Action == ActionKind.None) pending++;
            }

            if (ongoing != null)
            {
                var span = ongoing.DurationAt(now);
                _judge.Status = StatusKind.Alarm;
                _judge.Text = span >= BoothSpec.StopRecommend ? "투입 정지 권고" : "환경 이탈";
                _judge.SubText = CauseText(ongoing.Booth, ongoing.Item) + " · " + BoothSpec.DurationText(span) + " 경과";
                return;
            }
            if (pending > 0)
            {
                _judge.Status = StatusKind.Warn;
                _judge.Text = "조치 대기";
                _judge.SubText = "이탈 종료 · 조치 미등록 " + pending + "건";
                return;
            }
            for (int i = 0; i < _readings.Length; i++)
            {
                foreach (MeasureItem item in Enum.GetValues(typeof(MeasureItem)))
                {
                    if (ItemStatus(_readings[i], item) != StatusKind.Warn) continue;
                    _judge.Status = StatusKind.Warn;
                    _judge.Text = "주의";
                    _judge.SubText = CauseText(i, item);
                    return;
                }
            }
            _judge.Status = StatusKind.Run;
            _judge.Text = "정상";
            _judge.SubText = "가동 중인 부스 모두 관리 범위 안";
        }

        // ---- 이탈 이력 ----

        private void BuildGrid()
        {
            _grid.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            foreach (var c in new[] { "booth:부스", "item:항목", "start:시작", "end:종료", "span:지속 시간", "peak:최대값",
                "hanger:영향 행거", "action:조치", "who:확인자" })
            {
                var p = c.Split(':');
                _grid.Columns.Add(p[0], p[1]);
                _grid.Columns[p[0]].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        private void ReloadHistory()
        {
            int keepId = CurrentEventId();
            _grid.Rows.Clear();
            DateTime now = _source.Now;
            foreach (var e in _source.GetDeviations())
            {
                var spec = BoothSpec.Get(e.Booth, e.Item);
                var range = _source.GetAffectedHangers(e.Start, e.End);
                bool needsAction = e.Ongoing || e.Action == ActionKind.None;
                var lamp = e.Ongoing ? new ApcLampValue(StatusKind.Alarm, "이탈 중")
                    : e.Action == ActionKind.None ? new ApcLampValue(StatusKind.Warn, "조치 대기")
                    : new ApcLampValue(StatusKind.Run, "조치 완료");
                int row = _grid.Rows.Add(lamp, BoothSpec.BoothNames[e.Booth], spec.Name + " (" + spec.Unit + ")",
                    e.Start.ToString("HH:mm:ss"), e.Ongoing ? "" : e.End.Value.ToString("HH:mm:ss"),
                    BoothSpec.DurationText(e.DurationAt(now)), spec.Format(e.Peak),
                    range.Count == 0 ? "없음" : range.Text, ActionText.Of(e.Action), e.Confirmer);
                _grid.Rows[row].Tag = e.Id;
                if (range.Count > 0)   // 이탈 중 투입된 행거: 굵게, 조치가 필요한 건은 알람색
                {
                    var style = _grid.Rows[row].Cells["hanger"].Style;
                    style.Font = ApcTheme.GetFont(System.Drawing.FontStyle.Bold);
                    if (needsAction) style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                }
                if (e.Id == keepId || (keepId < 0 && row == 0)) _grid.CurrentCell = _grid.Rows[row].Cells[1];
            }
            UpdateRegisterGate();
        }

        private int CurrentEventId()
        {
            return _grid.CurrentRow == null ? -1 : (int)_grid.CurrentRow.Tag;
        }

        private DeviationEvent CurrentEvent()
        {
            int id = CurrentEventId();
            foreach (var e in _source.GetDeviations()) if (e.Id == id) return e;
            return null;
        }

        private void UpdateRegisterGate()
        {
            var e = CurrentEvent();
            _register.Enabled = e != null && e.Action == ActionKind.None;
            _registerReason.Text = e == null ? "이력 표에서 조치할 행을 선택하세요"
                : e.Action != ActionKind.None ? "이미 조치가 등록된 건입니다" : "";
        }

        private void OpenActionDialog()
        {
            var e = CurrentEvent();
            if (e == null || e.Action != ActionKind.None) return;
            using (var dlg = new ActionDialog(e, _source, ActionKind.None))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (!_source.RegisterAction(dlg.EventId, dlg.Action, dlg.Remark))
                {
                    ApcDialog.Warn(FindForm(), "이탈이 끝나지 않아 영향 없음으로 등록할 수 없습니다.");
                    return;
                }
                Reload();
            }
        }
    }
}
