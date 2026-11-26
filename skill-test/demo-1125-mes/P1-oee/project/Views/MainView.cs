using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PressLineOee.Data;

namespace PressLineOee.Views
{
    /// <summary>
    /// 프레스 라인 가동 현황. 위에서 아래로: 지금 조치할 것(판정판) → 설비 타일 → 선택 설비 OEE 카드 → 비가동 이력(사유 입력).
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly List<ApcStatusTile> _tiles = new List<ApcStatusTile>();
        private readonly Label _cardTitle = SectionTitle("");
        private readonly Label _cardNote = Caption("");
        private readonly ApcValueCard _avail = Card("가용률"), _perf = Card("성능효율"), _quality = Card("양품률"),
            _oee = Card("OEE"), _produced = Card("금일 생산"), _good = Card("금일 양품");
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false };
        private readonly ApcButton _enter = Button("사유 입력", ApcButtonVariant.Primary, 130);
        private readonly Label _enterWhy = Caption("");

        private IList<MachineInfo> _machines;
        private IList<StopEvent> _events;
        private string _selected;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("프레스 라인 설비 가동 현황")
        {
            _source = source;
            _machines = _source.GetMachines();
            _events = _source.GetStopEvents();

            BuildGrid();
            _enter.Click += (s, e) => EnterReason();
            _grid.SelectionChanged += (s, e) => UpdateEnterButton();

            var tiles = new Control[_machines.Count];
            for (int i = 0; i < tiles.Length; i++)
            {
                var id = _machines[i].Id;
                var tile = new ApcStatusTile { Text = id, Dock = DockStyle.Fill };
                tile.Click += (s, e) => SelectMachine(id);
                _tiles.Add(tile);
                tiles[i] = tile;
            }

            var legend = new ApcStatusLegend { Dock = DockStyle.Right };
            legend.SetItems(new ApcLampValue(StatusKind.Run, "가동"), new ApcLampValue(StatusKind.Alarm, "비가동"),
                new ApcLampValue(StatusKind.Stop, "계획정지"));
            legend.Width = legend.PreferredWidth;
            var tileTitle = new Panel { Dock = DockStyle.Top, Height = S(40) };
            tileTitle.Controls.Add(legend);
            var tileLabel = SectionTitle("프레스 상태");
            tileLabel.Dock = DockStyle.Fill;
            tileTitle.Controls.Add(tileLabel);
            tileTitle.Controls.SetChildIndex(tileLabel, 0);

            var cardTitleRow = Row(_cardTitle, _cardNote);
            var cardRow = Columns(S(120), new Control[] { _avail, _perf, _quality, _oee, _produced, _good });

            var gridTitleRow = Row(SectionTitle("비가동 이력"), _enter, _enterWhy);

            // Fill(이력 표) 먼저, Top 줄은 아래쪽부터
            Body.Controls.Add(_grid);
            Body.Controls.Add(gridTitleRow);
            Body.Controls.Add(cardRow);
            Body.Controls.Add(cardTitleRow);
            Body.Controls.Add(Columns(S(80), tiles));
            Body.Controls.Add(tileTitle);
            Body.Controls.Add(Columns(S(110), new Control[] { _judge }));

            _selected = StoppedMachineOrFirst();
            Reload();
            SelectPendingRow();
        }

        private static ApcValueCard Card(string title)
        {
            return new ApcValueCard { Title = title, Dock = DockStyle.Fill };
        }

        private void BuildGrid()
        {
            _grid.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            _grid.Columns.Add("machine", "설비");
            _grid.Columns.Add("start", "시작");
            _grid.Columns.Add("end", "종료");
            _grid.Columns.Add("min", "시간(min)");
            _grid.Columns.Add("cat", "대분류");
            _grid.Columns.Add("detail", "세부 사유");
            _grid.Columns.Add("user", "입력자");
            _grid.Columns[0].Width = S(150);
            _grid.Columns["detail"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns["detail"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        private string StoppedMachineOrFirst()
        {
            foreach (var m in _machines)
                if (Oee.MachineStatus(m.Id, _events) == StatusKind.Alarm) return m.Id;
            return _machines[0].Id;
        }

        private void Reload()
        {
            _events = _source.GetStopEvents();
            RefreshJudge();
            RefreshTiles();
            RefreshCards();
            RefillGrid();
            UpdateEnterButton();
        }

        private void RefreshTiles()
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                _tiles[i].Status = Oee.MachineStatus(_machines[i].Id, _events);
                _tiles[i].Selected = _machines[i].Id == _selected;
            }
        }

        // 지금 가장 급한 것 하나: 정지 중인 설비와 사유 미입력 건수
        private void RefreshJudge()
        {
            var now = _source.Now;
            var stopped = new List<StopEvent>();
            int pending = 0;
            foreach (var e in _events)
            {
                if (e.Kind == StopKind.Unplanned && e.End == null) stopped.Add(e);
                if (Oee.NeedsReason(e, now)) pending++;
            }
            if (stopped.Count > 0)
            {
                _judge.Status = StatusKind.Alarm;
                _judge.Text = stopped.Count == 1
                    ? stopped[0].Machine + " 비가동 " + Oee.DurationMin(stopped[0], now) + " min"
                    : stopped.Count + "대 비가동";
                _judge.SubText = pending > 0 ? "사유 입력 필요 · 미입력 " + pending + "건" : "사유 입력 완료";
            }
            else if (pending > 0)
            {
                _judge.Status = StatusKind.Warn;
                _judge.Text = "사유 미입력 " + pending + "건";
                _judge.SubText = "아래 표에서 행을 골라 입력하세요";
            }
            else
            {
                _judge.Status = StatusKind.Run;
                _judge.Text = "전 설비 가동";
                _judge.SubText = "사유 미입력 정지 없음";
            }
        }

        private void SelectMachine(string id)
        {
            _selected = id;
            RefreshTiles();
            RefreshCards();
        }

        private void RefreshCards()
        {
            var now = _source.Now;
            MachineInfo m = _machines[0];
            foreach (var x in _machines) if (x.Id == _selected) m = x;
            var r = Oee.Compute(m, _events, now);
            bool running = Oee.MachineStatus(m.Id, _events) == StatusKind.Run;
            var plain = running ? StatusKind.Run : StatusKind.Stop;   // 판정 기준이 없는 값: 갱신 중 Run, 멈춤 Stop

            _cardTitle.Text = m.Id + " 가동 지표";
            var start = Oee.ProductionDayStart(now);
            _cardNote.Text = "생산일자 " + start.ToString("yyyy-MM-dd") + " · " + Oee.ShiftName(now) + " · " + start.ToString("HH:mm") + "부터";

            Set(_avail, Pct(r.Availability), "%", plain, "가동 " + r.RunMin + " / 부하 " + r.LoadMin + " min");
            // 성능효율 100% 초과 = 표준 CT 오류 의심
            bool ctSuspect = r.Performance > 1.0;
            Set(_perf, Pct(r.Performance), "%", ctSuspect ? StatusKind.Warn : plain,
                ctSuspect ? "표준 CT 확인 필요" : "이론 CT " + r.TheoreticalCtMin.ToString("0.0") + " min");
            Set(_quality, Pct(r.Quality), "%", plain, "양품 " + r.Good.ToString("#,0") + " / 생산 " + r.Produced.ToString("#,0"));
            Set(_oee, Pct(r.Value), "%", plain, "");
            Set(_produced, r.Produced.ToString("#,0"), "개", plain, "");
            Set(_good, r.Good.ToString("#,0"), "개", plain, "불량 " + (r.Produced - r.Good).ToString("#,0"));
        }

        private static string Pct(double v) { return (v * 100).ToString("0.0"); }

        private static void Set(ApcValueCard c, string value, string unit, StatusKind status, string footer)
        {
            c.Value = value; c.Unit = unit; c.Status = status; c.Footer = footer;
        }

        private void RefillGrid()
        {
            var now = _source.Now;
            string keep = CurrentEventId();
            _grid.Rows.Clear();
            var pendingBack = BlendWithWhite(ApcTheme.Status(StatusKind.Alarm), 0.12f);
            foreach (var e in _events)
            {
                bool pending = Oee.NeedsReason(e, now);
                int i = _grid.Rows.Add(RowStatus(e, pending), e.Machine, e.Start.ToString("HH:mm:ss"),
                    e.End == null ? "" : e.End.Value.ToString("HH:mm:ss"), Oee.DurationMin(e, now).ToString("0"),
                    pending ? "사유 입력 필요" : e.Category, e.Detail, e.User);
                var row = _grid.Rows[i];
                row.Tag = e;
                if (pending)
                {
                    row.DefaultCellStyle.BackColor = pendingBack;
                    var cat = row.Cells["cat"].Style;
                    cat.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                    cat.Font = ApcTheme.GetFont(FontStyle.Bold);
                }
                if (e.Id == keep) { _grid.CurrentCell = row.Cells[1]; }
            }
        }

        private static ApcLampValue RowStatus(StopEvent e, bool pending)
        {
            if (pending) return new ApcLampValue(StatusKind.Alarm, "사유 필요");
            if (e.Kind == StopKind.Planned) return new ApcLampValue(StatusKind.Stop, "계획정지");
            if (e.Category == Oee.AutoCategory) return new ApcLampValue(StatusKind.Info, "자동 기록");
            return new ApcLampValue(StatusKind.Run, "입력됨");
        }

        private static Color BlendWithWhite(Color c, float amount)
        {
            return Color.FromArgb(255, (int)(255 - (255 - c.R) * amount), (int)(255 - (255 - c.G) * amount),
                (int)(255 - (255 - c.B) * amount));
        }

        private string CurrentEventId()
        {
            var e = CurrentEvent();
            return e == null ? null : e.Id;
        }

        private StopEvent CurrentEvent()
        {
            return _grid.CurrentRow == null ? null : _grid.CurrentRow.Tag as StopEvent;
        }

        // 처음 열면 가장 급한(정지 중인) 사유 미입력 행이 골라져 있어 한 번 누르면 입력
        private void SelectPendingRow()
        {
            var now = _source.Now;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                var e = (StopEvent)row.Tag;
                if (Oee.NeedsReason(e, now) && e.End == null) { _grid.CurrentCell = row.Cells[1]; row.Selected = true; UpdateEnterButton(); return; }
            }
        }

        // 막는 것은 버튼이 막고, 이유는 옆에 글로
        private void UpdateEnterButton()
        {
            var e = CurrentEvent();
            if (e == null)
            {
                _enter.Enabled = false;
                _enterWhy.Text = "표에서 사유를 입력할 정지 행을 고르세요.";
                return;
            }
            string why = Oee.WhyNoReason(e, _source.Now);
            _enter.Enabled = why == null;
            _enterWhy.Text = why ?? e.Machine + " " + e.Start.ToString("HH:mm:ss") + " 정지";
        }

        private void EnterReason()
        {
            var e = CurrentEvent();
            if (e == null) return;
            string why = Oee.WhyNoReason(e, _source.Now);
            if (why != null) { ApcDialog.Warn(FindForm(), why); return; }

            using (var dlg = new StopReasonDialog(e.Machine, e.Start.ToString("yyyy-MM-dd HH:mm:ss"),
                e.End == null ? "" : e.End.Value.ToString("yyyy-MM-dd HH:mm:ss")))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.SaveReason(e.Id, dlg.Category, dlg.Detail, dlg.Note);
            }
            Reload();
            SelectPendingRow();
        }
    }
}
