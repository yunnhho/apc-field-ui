using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using BumperPaintBoothMonitor.Data;

namespace BumperPaintBoothMonitor.Views
{
    /// <summary>
    /// 도장 라인 환경 모니터링. 위에서 아래로: 판정판(지금 해야 할 것) → 현재 투입 → 부스 타일 → 선택 부스 현재값·추이 → 이탈 이력.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int DefaultBooth = 3;   // 클리어(샘플에서 이탈 중인 부스)
        private static readonly string[] TrendLabels = { "-8시간", "-6시간", "-4시간", "-2시간", "현재" };

        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel();
        private readonly ApcInfoStrip _input = new ApcInfoStrip();
        private readonly Label _selectedTitle = SectionTitle("");
        private readonly ApcStatusTile[] _tiles = new ApcStatusTile[5];
        private readonly ApcValueCard[] _cards = new ApcValueCard[4];
        private readonly ApcTrendChart _tempChart = new ApcTrendChart();
        private readonly ApcTrendChart _humiChart = new ApcTrendChart();
        private readonly Panel _tempBox, _humiBox;
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false };
        private readonly ApcButton _register = Button("이탈 조치 등록", ApcButtonVariant.Primary, 150);
        private readonly Label _registerReason = Caption("");
        private int _selected = DefaultBooth;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("도장 라인 환경 모니터링")
        {
            _source = source;
            var booths = source.GetBooths();

            for (int i = 0; i < _tiles.Length; i++)
            {
                int index = i;
                _tiles[i] = new ApcStatusTile { Dock = DockStyle.Fill };
                _tiles[i].Click += (s, e) => SelectBooth(index);
            }
            for (int i = 0; i < _cards.Length; i++) _cards[i] = new ApcValueCard { Dock = DockStyle.Fill, ShowLamp = true };

            _input.SetItems("현재 투입 행거", "도료 Lot", "색상 코드");
            _input.Dock = DockStyle.Fill;

            BuildGrid();
            _grid.SelectionChanged += (s, e) => UpdateRegisterButton();
            _register.Click += (s, e) => OpenAction();
            _registerReason.Margin = new Padding(0, S(8), S(12), 0);

            _tempBox = Titled("", _tempChart);
            _humiBox = Titled("", _humiChart);
            foreach (var c in new[] { _tempChart, _humiChart }) { c.XLabels = TrendLabels; c.Dock = DockStyle.Fill; }

            // 아래 줄부터 Add(Top 도킹은 나중에 Add한 것이 위)
            Body.Controls.Add(Titled("환경 이탈 이력 · 오늘", _grid, _registerReason, _register));
            Body.Controls.Add(Columns(TitledHeight(S(120)), new Control[] { _tempBox, _humiBox }));
            Body.Controls.Add(Columns(S(104), _cards));
            _selectedTitle.Dock = DockStyle.Top;
            Body.Controls.Add(_selectedTitle);
            Body.Controls.Add(Columns(S(52), _tiles));
            Body.Controls.Add(Columns(_input.Height, new Control[] { _input }));
            Body.Controls.Add(Columns(S(84), new Control[] { _judge }));
            _judge.Dock = DockStyle.Fill;

            ReloadLine();
            ReloadSelected();
            ReloadGrid(null);
        }

        // ---- 라인 전체: 판정판, 투입 정보, 타일 ----

        private void ReloadLine()
        {
            var booths = _source.GetBooths();
            for (int i = 0; i < _tiles.Length; i++)
            {
                _tiles[i].Status = booths[i].Status;
                _tiles[i].Text = booths[i].Name + "  " + BoothSnapshot.StateWord(booths[i].Status);
            }
            var cur = _source.GetCurrentInput();
            _input.SetValue("현재 투입 행거", cur.Hanger);
            _input.SetValue("도료 Lot", cur.PaintLot);
            _input.SetValue("색상 코드", cur.ColorCode);
            ShowJudge(booths);
        }

        /// <summary>판정판: 이탈 중이면 가장 먼저 그 이유와 지속 시간, 없으면 주의, 없으면 정상.</summary>
        private void ShowJudge(System.Collections.Generic.IList<BoothSnapshot> booths)
        {
            ItemReading alarm = null, warn = null;
            BoothSnapshot alarmBooth = null, warnBooth = null;
            foreach (var b in booths)
            {
                if (b.Stopped) continue;
                foreach (var r in b.Readings)
                {
                    if (r.Range == null || !r.Value.HasValue) continue;
                    if (r.Status == StatusKind.Alarm && alarm == null) { alarm = r; alarmBooth = b; }
                    if (r.Status == StatusKind.Warn && warn == null) { warn = r; warnBooth = b; }
                }
            }
            if (alarm != null)
            {
                var span = OngoingSpan(alarmBooth.Name, alarm.Range.Name);
                bool stopAdvice = span >= ControlRange.StopAdviceAfter;
                _judge.Status = StatusKind.Alarm;
                _judge.Text = stopAdvice ? "라인 투입 정지 권고" : "환경 이탈 중";
                _judge.SubText = alarmBooth.Name + " 부스 " + alarm.Range.Name + " " + LimitText(alarm) + " · 이탈 " + Excursion.DurationText(span);
            }
            else if (warn != null)
            {
                _judge.Status = StatusKind.Warn;
                _judge.Text = "주의";
                _judge.SubText = warnBooth.Name + " 부스 " + warn.Range.Name + " " + LimitText(warn);
            }
            else
            {
                _judge.Status = StatusKind.Run;
                _judge.Text = "정상";
                _judge.SubText = "전 부스 관리 범위 안";
            }
        }

        private TimeSpan OngoingSpan(string booth, string item)
        {
            foreach (var ex in _source.GetExcursions())
                if (ex.Ongoing && ex.Booth == booth && ex.ItemName == item) return ex.Duration(_source.Now);
            return TimeSpan.Zero;
        }

        /// <summary>"상한 초과 77 %RH (상한 75)" / "상한 근접 25.0 ℃ (상한 25.0)".</summary>
        private static string LimitText(ItemReading r)
        {
            double v = r.Value.Value;
            bool high = v >= (r.Range.Low + r.Range.High) / 2;
            string side = high ? "상한" : "하한";
            string limit = " (" + side + " " + r.Range.Text(high ? r.Range.High : r.Range.Low) + ")";
            string word = r.Status == StatusKind.Alarm ? (high ? "초과" : "미달") : "근접";
            return side + " " + word + " " + r.Range.Text(v) + " " + r.Range.Unit + limit;
        }

        // ---- 선택 부스: 현재값 카드, 추이 ----

        private void SelectBooth(int index)
        {
            _selected = index;
            ReloadSelected();
        }

        private void ReloadSelected()
        {
            var booth = _source.GetBooths()[_selected];
            for (int i = 0; i < _tiles.Length; i++) _tiles[i].Selected = i == _selected;
            _selectedTitle.Text = booth.Name + " 부스 현재값 · 관리 범위";
            for (int i = 0; i < _cards.Length; i++) ShowCard(_cards[i], booth, booth.Readings[i], i);
            ShowTrend(_tempChart, _tempBox, MonitorItem.Temperature);
            ShowTrend(_humiChart, _humiBox, MonitorItem.Humidity);
        }

        private static readonly string[] ItemTitles = { "온도", "습도", "풍속", "차압" };

        private static void ShowCard(ApcValueCard card, BoothSnapshot booth, ItemReading r, int index)
        {
            card.Title = ItemTitles[index];
            if (r.Range == null || !r.Value.HasValue)
            {
                card.Value = ""; card.Unit = ""; card.Status = StatusKind.Stop; card.Footer = "관리 안 함";
                return;
            }
            card.Value = r.Range.Text(r.Value.Value);
            card.Unit = r.Range.Unit;
            var kind = r.Status;
            card.Status = booth.Stopped ? StatusKind.Stop : kind;   // 정지 부스의 값은 판정하지 않는다
            string state = booth.Stopped || kind == StatusKind.Run ? "" : " · " + BoothSnapshot.StateWord(kind);
            card.Footer = "관리 " + r.Range.RangeText + state;
        }

        private void ShowTrend(ApcTrendChart chart, Panel box, MonitorItem item)
        {
            var r = _source.GetBooths()[_selected].Readings[(int)item];
            string name = item == MonitorItem.Temperature ? "온도" : "습도";
            if (r.Range == null)
            {
                box.Text = name + " 추이 · 최근 8시간 · 관리 안 함";
                chart.UpperLimit = chart.LowerLimit = chart.Reference = null;
                chart.SetValues(new double[0]);
                return;
            }
            box.Text = name + " 추이 (" + r.Range.Unit + ") · 최근 8시간";
            chart.Reference = (r.Range.Low + r.Range.High) / 2;
            chart.UpperLimit = r.Range.High;
            chart.LowerLimit = r.Range.Low;
            chart.SetValues(_source.GetTrend(_selected, item));
        }

        // ---- 이탈 이력 ----

        private void BuildGrid()
        {
            _grid.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            foreach (var c in new[] { new[] { "booth", "부스" }, new[] { "item", "항목" }, new[] { "start", "시작" }, new[] { "end", "종료" },
                new[] { "dur", "지속 시간" }, new[] { "peak", "최대값" }, new[] { "hangers", "영향 행거" }, new[] { "action", "조치" }, new[] { "who", "확인자" } })
            {
                _grid.Columns.Add(c[0], c[1]);
                _grid.Columns[c[0]].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        private void ReloadGrid(int? keepId)
        {
            _grid.Rows.Clear();
            foreach (var ex in _source.GetExcursions())
            {
                StatusKind kind = ex.Ongoing ? StatusKind.Alarm : ex.HasAction ? StatusKind.Run : StatusKind.Warn;
                string word = ex.Ongoing ? "이탈 중" : ex.HasAction ? "조치 완료" : "조치 대기";
                int row = _grid.Rows.Add(new ApcLampValue(kind, word), ex.Booth, ex.ItemName + " (" + ex.Unit + ")",
                    ex.Start.ToString("HH:mm:ss"), ex.End.HasValue ? ex.End.Value.ToString("HH:mm:ss") : "",
                    Excursion.DurationText(ex.Duration(_source.Now)), ex.PeakValue, ex.HangerRange, ex.Action, ex.Confirmer);
                _grid.Rows[row].Tag = ex;
                // 이탈 중 투입된 행거는 굵게, 조치 전이면 빨간 글자
                var hanger = _grid.Rows[row].Cells["hangers"];
                hanger.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
                if (!ex.HasAction) hanger.Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            }
            int select = 0;
            for (int i = 0; keepId.HasValue && i < _grid.Rows.Count; i++)
                if (((Excursion)_grid.Rows[i].Tag).Id == keepId.Value) select = i;
            if (_grid.Rows.Count > 0) _grid.CurrentCell = _grid.Rows[select].Cells["booth"];
            UpdateRegisterButton();
        }

        private Excursion SelectedExcursion
        {
            get { return _grid.CurrentRow == null ? null : _grid.CurrentRow.Tag as Excursion; }
        }

        private void UpdateRegisterButton()
        {
            var ex = SelectedExcursion;
            _register.Enabled = ex != null && !ex.HasAction;
            _registerReason.Text = ex == null ? "조치할 이탈 행을 선택하세요" : ex.HasAction ? "조치 등록 완료 · 다시 등록할 수 없음" : "";
        }

        private void OpenAction()
        {
            var ex = SelectedExcursion;
            if (ex == null || ex.HasAction) return;
            using (var dlg = new ExcursionActionDialog(ex, _source.Now))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (!_source.RegisterAction(ex.Id, dlg.Action, dlg.Confirmer, dlg.Memo))
                {
                    ApcDialog.Warn(FindForm(), "이탈 중에는 영향 없음으로 등록할 수 없습니다.");
                    return;
                }
                ReloadGrid(ex.Id);
            }
        }
    }
}
