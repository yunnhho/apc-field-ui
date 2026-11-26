using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using WireBondEapTerminal.Data;

namespace WireBondEapTerminal.Views
{
    /// <summary>
    /// Wire Bond Track-in/out 단말 메인 화면. 위에서부터: Track-in 판정 + 장비 상태 → Lot 스캔 · Track-in → 진행 Lot → 검증 체크리스트 · 대기 Lot · 이벤트.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int WaitingRows = 10;
        private const int QTimeWarnMinutes = 90;  // 기준값(정함): Q-time 남은 시간이 이 이하면 경고색

        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly ApcButton _trackIn = Button("Track-in", ApcButtonVariant.Primary, 200);
        private readonly Label _trackInWhy = Caption("");
        private readonly ApcButton _trackOut = Button("Track-out", ApcButtonVariant.Normal, 200);
        private readonly Label _trackOutWhy = Caption("");
        private readonly ApcGrid _checks = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _waiting = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _events = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private string _verifiedLot;
        private RunningLot _running;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("Wire Bond Track-in/out")
        {
            _source = source;
            _running = source.GetRunningLot();

            BuildGrids();
            FillWaiting(source.GetWaitingLots());
            FillEvents(source.GetEvents());

            _scan.Width = S(260);
            _scan.Scanned += (s, e) => { e.Accepted = Verify(e.Code, true); };
            _trackIn.Click += (s, e) => DoTrackIn();
            _trackOut.Click += (s, e) => DoTrackOut();

            // 왼쪽 = 작업 흐름(판정 → 스캔 → 검증 → 진행 Lot), 오른쪽 = 장비 상태·대기 Lot·이벤트.
            // 각 칸 안에서 Dock=Top은 아래 줄부터 Add(위 줄이 바깥).
            var left = Cell(12);
            var lotCards = LotCards();
            left.Controls.Add(Columns(S(100), new[] { lotCards[3], lotCards[4], lotCards[5] }, null));
            left.Controls.Add(Columns(S(100), new[] { lotCards[0], lotCards[1], lotCards[2] }, null));
            var outRow = Row(SectionTitle("진행 Lot"), _trackOut, _trackOutWhy);
            outRow.Dock = DockStyle.Top;
            left.Controls.Add(outRow);
            var checks = Section("Track-in 검증 (MES → EAP → RMS)", _checks, 0);
            checks.Dock = DockStyle.Top;
            checks.Height = ApcGrid.HeightForRows(10) + S(40 + 12);
            left.Controls.Add(checks);
            var scanRow = Row(Caption("Lot 스캔"), _scan, _trackIn, _trackInWhy);
            scanRow.Dock = DockStyle.Top;
            left.Controls.Add(scanRow);
            left.Controls.Add(Columns(S(200), new Control[] { _judge }, null));

            var right = Cell(0);
            right.Controls.Add(Section("최근 이벤트 · 알람 (S6F11 / S5F1)", _events, 0));
            var waiting = Section("대기 Lot (RTD 순위)", _waiting, 0);
            waiting.Dock = DockStyle.Top;
            waiting.Height = ApcGrid.HeightForRows(WaitingRows) + S(40 + 12);
            right.Controls.Add(waiting);
            right.Controls.Add(Columns(S(110), TopCells(), null));

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53f));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            table.Controls.Add(left, 0, 0);
            table.Controls.Add(right, 1, 0);
            Body.Controls.Add(table);

            ShowTrackOut();
            Verify(source.LastScannedLot, false);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);
        }

        // ---- 장비 상태 ------------------------------------------------------------

        private Control[] TopCells()
        {
            var eq = _source.GetEquipment();
            var hsms = Card("HSMS 연결", eq.Hsms == HsmsState.Selected ? "SELECTED" : eq.Hsms == HsmsState.NotSelected ? "NOT SELECTED" : "NOT CONNECTED",
                eq.Hsms == HsmsState.Selected ? "Linktest " + eq.LinktestAt : "메시지 교환 불가",
                eq.Hsms == HsmsState.Selected ? StatusKind.Run : eq.Hsms == HsmsState.NotSelected ? StatusKind.Warn : StatusKind.Alarm);
            var gem = Card("GEM 제어", eq.Gem == GemControl.OnlineRemote ? "REMOTE" : eq.Gem == GemControl.OnlineLocal ? "LOCAL" : "OFFLINE",
                eq.Gem == GemControl.OnlineRemote ? "Online Remote" : eq.Gem == GemControl.OnlineLocal ? "Online Local · 원격 중단" : "Offline",
                eq.Gem == GemControl.OnlineRemote ? StatusKind.Run : eq.Gem == GemControl.OnlineLocal ? StatusKind.Warn : StatusKind.Stop);
            var state = Card("장비 " + eq.Name, eq.State == EquipState.Run ? "RUN" : eq.State == EquipState.Idle ? "IDLE" : "ALARM",
                eq.StateDetail, eq.State == EquipState.Run ? StatusKind.Run : eq.State == EquipState.Idle ? StatusKind.Stop : StatusKind.Alarm);
            return new Control[] { hsms, gem, state };
        }

        private static Panel Cell(int rightGap)
        {
            return new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(rightGap), 0), Padding = new Padding(0, 0, S(rightGap), 0) };
        }

        private static ApcValueCard Card(string title, string value, string footer, StatusKind status)
        {
            return new ApcValueCard { Title = title, Value = value, Footer = footer, Status = status, ShowLamp = true,
                Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
        }

        // ---- 진행 Lot -------------------------------------------------------------

        private Control[] LotCards()
        {
            var lot = _running;
            Func<string, string, string, string, StatusKind, ApcValueCard> c = (t, v, u, f, st) =>
                new ApcValueCard { Title = t, Value = v, Unit = u, Footer = f, Status = st, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
            if (lot == null)
                return new Control[] { c("Lot ID", "", "", "진행 Lot 없음", StatusKind.Stop), c("제품", "", "", "", StatusKind.Stop),
                    c("레시피", "", "", "", StatusKind.Stop), c("투입 Strip", "", "Strip", "", StatusKind.Stop),
                    c("처리 Strip", "", "Strip", "", StatusKind.Stop), c("진행률", "", "%", "", StatusKind.Stop) };
            return new Control[]
            {
                c("Lot ID", lot.LotId, "", "", StatusKind.Run), c("제품", lot.Product, "", "", StatusKind.Run),
                c("레시피", lot.Recipe, "", "", StatusKind.Run),
                c("투입 Strip", Format.Qty(lot.Strips), "Strip", "", StatusKind.Run),
                c("처리 Strip", Format.Qty(lot.DoneStrips), "Strip", "", StatusKind.Run),
                c("진행률", lot.Percent.ToString("0.0"), "%", "", lot.ProcessEnded ? StatusKind.Info : StatusKind.Run),
            };
        }

        private void ShowTrackOut()
        {
            bool can = _running != null && _running.ProcessEnded;
            _trackOut.Enabled = can;
            _trackOutWhy.Text = _running == null ? "진행 Lot 없음" : can ? "" : "공정 종료(Process End) 수신 후 가능";
        }

        private void DoTrackOut()
        {
            if (_running == null || !_running.ProcessEnded) return;
            using (var dlg = new TrackOutDialog())
            {
                dlg.ShowDialog(FindForm());
            }
        }

        // ---- Track-in -------------------------------------------------------------

        private bool Verify(string code, bool warnUnknown)
        {
            if (string.IsNullOrEmpty(code))
            {
                SetJudge("READY", StatusKind.Stop, "Lot 캐리어를 스캔하세요", false, "스캔 후 가능");
                return true;
            }
            var result = _source.Verify(code);
            if (result == null)
            {
                if (warnUnknown) BeginInvoke(new Action(() => ApcDialog.Warn(FindForm(), "대기 목록에 없는 Lot입니다: " + code)));
                return false;
            }
            _verifiedLot = result.Passed ? result.LotId : null;
            FillChecks(result);
            if (result.Passed) SetJudge("TRACK-IN 가능", StatusKind.Run, result.LotId + " · 검증 완료", true, "");
            else SetJudge("TRACK-IN 불가", StatusKind.Alarm, result.LotId + " · " + result.BlockReason, false, result.BlockReason);
            return true;
        }

        private void SetJudge(string text, StatusKind status, string sub, bool canTrackIn, string why)
        {
            _judge.Text = text;
            _judge.Status = status;
            _judge.SubText = sub;
            _trackIn.Enabled = canTrackIn;
            _trackInWhy.Text = canTrackIn ? "" : why;
            _trackInWhy.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            if (!canTrackIn && status == StatusKind.Stop) _verifiedLot = null;
        }

        private void DoTrackIn()
        {
            if (_verifiedLot == null) return;
            if (!ApcDialog.Confirm(FindForm(), _verifiedLot + " Track-in을 시작할까요?", "Track-in", ApcButtonVariant.Primary)) return;
            _source.TrackIn(_verifiedLot);
            ApcDialog.Alert(FindForm(), "Track-in 명령을 보냈습니다 (PP-SELECT · START).");
        }

        // ---- 표 -------------------------------------------------------------------

        private static Panel Section(string title, Control grid, int rightGap)
        {
            var p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(rightGap), 0), Padding = Padding.Empty };
            p.Controls.Add(grid);
            var head = SectionTitle(title);
            head.Dock = DockStyle.Top;
            p.Controls.Add(head);
            return p;
        }

        private void BuildGrids()
        {
            _checks.Columns.Add(new ApcLampColumn { HeaderText = "상태", Width = S(60), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _checks.Columns.Add("group", "구분");
            _checks.Columns.Add("item", "항목");
            _checks.Columns.Add("result", "판정");
            _checks.Columns.Add("reason", "사유");
            _checks.Columns["group"].Width = S(70);
            _checks.Columns["result"].Width = S(80);
            _checks.Columns["item"].Width = S(250);
            foreach (var n in new[] { "group", "result", "item" }) _checks.Columns[n].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _checks.Columns["reason"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _checks.Columns["reason"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            _checks.Columns["item"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            _waiting.Columns.Add("rank", "순위");
            _waiting.Columns.Add("lot", "Lot ID");
            _waiting.Columns.Add("product", "제품");
            _waiting.Columns.Add("score", "점수");
            _waiting.Columns.Add("q", "Q-time 남음(h:mm)");
            _waiting.Columns.Add(new ApcLampColumn { HeaderText = "상태", Width = S(190), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            foreach (DataGridViewColumn c in _waiting.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            _events.Columns.Add(new ApcLampColumn { HeaderText = "구분", Width = S(60), AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _events.Columns.Add("time", "시각");
            _events.Columns.Add("msg", "메시지");
            _events.Columns.Add("text", "내용");
            _events.Columns["time"].Width = S(90);
            _events.Columns["msg"].Width = S(80);
            _events.Columns["time"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _events.Columns["msg"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            _events.Columns["text"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _events.Columns["text"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        private void FillChecks(TrackInResult r)
        {
            _checks.Rows.Clear();
            foreach (var c in r.Checks)
            {
                StatusKind k = c.State == VerifyState.Ok ? StatusKind.Run : c.State == VerifyState.Ng ? StatusKind.Alarm : StatusKind.Stop;
                string word = c.State == VerifyState.Ok ? "OK" : c.State == VerifyState.Ng ? "NG" : "미검증";
                int i = _checks.Rows.Add(k, c.Group, c.Item, word, c.Reason);
                if (c.State == VerifyState.Ng)
                    foreach (var col in new[] { "result", "reason" })
                    {
                        var cell = _checks.Rows[i].Cells[col];
                        cell.Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                        cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
                    }
            }
        }

        private void FillWaiting(IList<WaitingLot> lots)
        {
            foreach (var l in lots)
            {
                bool hold = l.HoldReason != null;
                StatusKind k = hold ? StatusKind.Alarm : StatusKind.Run;
                string text = hold ? (l.HoldReason == "Q-time 초과" ? "Hold · Q-time 초과" : "Hold · " + l.HoldReason) : "대기";
                int i = _waiting.Rows.Add(hold ? "" : l.Rank.ToString(), l.LotId, l.Product, hold ? "" : l.Score.ToString("0.0"),
                    Format.Hm(l.QRemainMinutes), new ApcLampValue(k, text));
                if (!hold && l.QRemainMinutes <= QTimeWarnMinutes)
                {
                    var cell = _waiting.Rows[i].Cells["q"];
                    cell.Style.ForeColor = ApcTheme.StatusText(StatusKind.Warn);
                    cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
                }
            }
        }

        private void FillEvents(IList<EventRow> events)
        {
            foreach (var e in events)
            {
                StatusKind k = e.Kind == EventKind.AlarmSet ? StatusKind.Warn : e.Kind == EventKind.ProcessStart ? StatusKind.Run : StatusKind.Info;
                _events.Rows.Add(k, e.At.ToString("HH:mm:ss"), e.Message, e.Text);
            }
        }
    }
}
