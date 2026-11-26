using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using DoorTrimJisShipping.Data;

namespace DoorTrimJisShipping.Views
{
    /// <summary>
    /// JIS 서열 출하 검증 메인 화면. 위: 납입 마감 남은 시간 · 판정판(지금 할 일/막힌 이유) · 결번 알람.
    /// 가운데: 스캔 줄(다음 칸 정보) + 대체·출하 확정 버튼. 아래: 서열 수신 목록 + 대차 12칸.
    /// 출하 확정은 12칸이 모두 OK일 때만 눌린다.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        // 정한 기준값: 납입 마감까지 이 시간 이하면 경고/알람 (OEM 라인 정지 비용은 분 단위로 청구)
        private const double DeadlineWarnMinutes = 15, DeadlineAlarmMinutes = 5;

        private readonly IMainViewSource _source;
        private readonly Stopwatch _elapsed = Stopwatch.StartNew();
        private readonly Timer _timer = new Timer { Interval = 1000 };
        private readonly ApcValueCard _deadline = new ApcValueCard { Title = "납입 마감까지", Unit = "분:초" };
        private readonly ApcJudgePanel _judge = new ApcJudgePanel();
        private readonly ApcValueCard _missing = new ApcValueCard { Title = "서열 결번" };
        private readonly ApcScanBox _scan = new ApcScanBox { Width = S(360) };
        private readonly ApcInfoStrip _next = new ApcInfoStrip { Dock = DockStyle.None, Width = S(980) };
        private readonly ApcButton _substitute = Button("서열 부품 대체", ApcButtonVariant.Normal, 170);
        private readonly ApcButton _confirm = Button("대차 출하 확정", ApcButtonVariant.Primary, 170);
        private readonly Label _reason = Caption("");
        private readonly ApcGrid _seqGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _historyGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _cartGrid =new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false };

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("도어 트림 JIS 서열 출하 검증")
        {
            _source = source;
            _next.SetItems("다음 칸", "서열 번호", "사양 코드", "지정 품번");
            _reason.Margin = new Padding(0, S(8), 0, 0);
            _scan.Scanned += OnScanned;
            _substitute.Click += (s, e) => Substitute();
            _confirm.Click += (s, e) => ConfirmShipment();
            _timer.Tick += (s, e) => ShowDeadline();

            BuildSequenceGrid();
            BuildCartGrid();

            var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
            split.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));
            split.Controls.Add(Section("OEM 서열 수신 목록", _seqGrid, S(ApcLayout.RowGap)), 0, 0);
            split.Controls.Add(CartColumn(), 1, 0);

            // Top 도킹은 나중에 Add한 것이 위 → Fill 먼저, 아래 줄부터
            Body.Controls.Add(split);
            Body.Controls.Add(Row(_substitute, _confirm, _reason));
            Body.Controls.Add(Row(Caption("라벨 스캔"), _scan, _next));
            Body.Controls.Add(Columns(S(150), new Control[] { _deadline, _judge, _missing }, new[] { 28f, 46f, 26f }));

            Reload();
            _timer.Start();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);   // 생성자 시점엔 폼이 없다
            _scan.Focus();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }

        private static Panel Section(string title, Control grid, int rightGap)
        {
            var p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, rightGap, 0), BackColor = Color.Transparent };
            var t = SectionTitle(title);
            t.Dock = DockStyle.Top;
            p.Controls.Add(grid);
            p.Controls.Add(t);
            return p;
        }

        // 오른쪽: 대차 12칸(고정 높이) + 그 아래 오늘 대체 이력(남은 높이)
        private Panel CartColumn()
        {
            _historyGrid.Columns.Add("time", "시각");
            _historyGrid.Columns.Add("slot", "칸");
            _historyGrid.Columns.Add("seq", "서열 번호");
            _historyGrid.Columns.Add("old", "기존 시리얼");
            _historyGrid.Columns.Add("new", "대체 시리얼");
            _historyGrid.Columns.Add("reason", "사유");
            foreach (DataGridViewColumn c in _historyGrid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            _cartGrid.Dock = DockStyle.Top;
            _cartGrid.Height = ApcGrid.HeightForRows(12);
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var cartTitle = SectionTitle("납입 대차 12칸");
            cartTitle.Dock = DockStyle.Top;
            var historyTitle = SectionTitle("서열 부품 대체 이력");
            historyTitle.Dock = DockStyle.Top;
            var gap = new Panel { Dock = DockStyle.Top, Height = S(ApcLayout.GroupGap), BackColor = Color.Transparent };
            // Top 도킹: 아래부터 Add
            p.Controls.Add(_historyGrid);
            p.Controls.Add(historyTitle);
            p.Controls.Add(gap);
            p.Controls.Add(_cartGrid);
            p.Controls.Add(cartTitle);
            return p;
        }

        private void BuildSequenceGrid()
        {
            _seqGrid.Columns.Add("seq", "서열 번호");
            _seqGrid.Columns.Add("vin", "차대번호");
            _seqGrid.Columns.Add("spec", "사양 코드");
            _seqGrid.Columns.Add("part", "품번");
            _seqGrid.Columns.Add("eta", "투입 예정");
            _seqGrid.Columns.Add(new ApcLampColumn { HeaderText = "상태", Name = "state" });
            foreach (DataGridViewColumn c in _seqGrid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _seqGrid.Columns["state"].Width = S(110);
            _seqGrid.Columns["vin"].MinimumWidth = S(200);
            foreach (var r in _source.GetSequences())
            {
                bool missing = r.State == SeqState.Missing;
                _seqGrid.Rows.Add(r.Seq, missing ? "" : r.Vin, missing ? "" : r.Spec.Code, missing ? "" : r.Spec.Part,
                    missing ? "" : r.Eta.ToString("HH:mm:ss"), StateValue(r.State));
                if (missing)
                {
                    var cell = _seqGrid.Rows[_seqGrid.Rows.Count - 1].Cells[0];
                    cell.Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                    cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
                }
            }
        }

        private static ApcLampValue StateValue(SeqState s)
        {
            switch (s)
            {
                case SeqState.Changed: return new ApcLampValue(StatusKind.Warn, "변경");
                case SeqState.Cancelled: return new ApcLampValue(StatusKind.Stop, "취소");
                case SeqState.Missing: return new ApcLampValue(StatusKind.Alarm, "결번");
                default: return new ApcLampValue(StatusKind.Run, "신규");
            }
        }

        private void BuildCartGrid()
        {
            _cartGrid.Columns.Add("slot", "칸");
            _cartGrid.Columns.Add("seq", "서열 번호");
            _cartGrid.Columns.Add("spec", "사양 코드");
            _cartGrid.Columns.Add("part", "지정 품번");
            _cartGrid.Columns.Add("scanned", "스캔 품번");
            _cartGrid.Columns.Add(new ApcLampColumn { HeaderText = "판정", Name = "verdict" });
            foreach (DataGridViewColumn c in _cartGrid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _cartGrid.Columns["verdict"].Width = S(190);
        }

        private void Reload()
        {
            var slots = _source.GetSlots();
            var next = slots.FirstOrDefault(s => s.Verdict == SlotVerdict.Waiting);
            var ng = slots.FirstOrDefault(s => s.Verdict == SlotVerdict.Ng);
            int okCount = slots.Count(s => s.Verdict == SlotVerdict.Ok);

            _cartGrid.Rows.Clear();
            foreach (var s in slots)
            {
                bool isNext = s == next;
                string verdict = s.Verdict == SlotVerdict.Ok ? "OK" : s.Verdict == SlotVerdict.Ng ? "NG · 품번 불일치" : isNext ? "다음 스캔" : "대기";
                var kind = s.Verdict == SlotVerdict.Ok ? StatusKind.Run : s.Verdict == SlotVerdict.Ng ? StatusKind.Alarm : isNext ? StatusKind.Info : StatusKind.Stop;
                _cartGrid.Rows.Add(s.Slot.ToString("00"), s.Seq, s.Spec.Code, s.Spec.Part, s.ScannedPart ?? "", new ApcLampValue(kind, verdict));
                var row = _cartGrid.Rows[_cartGrid.Rows.Count - 1];
                row.Cells[0].Style.Font = ApcTheme.GetFont(FontStyle.Bold);
                if (s.Verdict != SlotVerdict.Ng) continue;
                row.Cells[4].Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                row.Cells[4].Style.Font = ApcTheme.GetFont(FontStyle.Bold);
            }
            if (next != null) _cartGrid.CurrentCell = _cartGrid.Rows[next.Slot - 1].Cells[0];   // 다음에 스캔할 칸 강조

            _historyGrid.Rows.Clear();
            foreach (var h in _source.GetSubstituteHistory())
                _historyGrid.Rows.Add(h.At.ToString("HH:mm:ss"), h.Slot.ToString("00"), h.Seq, h.OldSerial, h.NewSerial, h.Reason);

            ShowNext(next);
            ShowJudge(next, ng, okCount, slots.Count);
            ShowMissing();
            ShowGate(slots, okCount);
            ShowDeadline();
        }

        private void ShowNext(SlotRow next)
        {
            _next.SetValue("다음 칸", next == null ? "" : next.Slot.ToString("00"));
            _next.SetValue("서열 번호", next == null ? "" : next.Seq.ToString());
            _next.SetValue("사양 코드", next == null ? "" : next.Spec.Code);
            _next.SetValue("지정 품번", next == null ? "" : next.Spec.Part);
        }

        // 판정판 = 이 화면의 가장 중요한 한 가지: 막힌 이유 > 출하 가능 > 다음 할 일
        private void ShowJudge(SlotRow next, SlotRow ng, int okCount, int total)
        {
            if (ng != null)
            {
                _judge.Status = StatusKind.Alarm;
                _judge.Text = "NG · 칸 " + ng.Slot.ToString("00");
                _judge.SubText = ng.NgReason;
            }
            else if (next == null && okCount == total)
            {
                _judge.Status = StatusKind.Run;
                _judge.Text = "OK";
                _judge.SubText = total + "칸 모두 OK · 출하 확정을 누르세요";
            }
            else
            {
                _judge.Status = StatusKind.Stop;
                _judge.Text = "READY";
                _judge.SubText = next == null ? "" : "칸 " + next.Slot.ToString("00") + " 부품 라벨을 스캔하세요";
            }
        }

        private void ShowMissing()
        {
            var gap = _source.GetSequences().FirstOrDefault(r => r.State == SeqState.Missing);
            _missing.Status = gap == null ? StatusKind.Run : StatusKind.Alarm;
            _missing.Value = gap == null ? "" : gap.Seq.ToString();
            _missing.Footer = gap == null ? "결번 없음" : "수신 누락 · OEM 확인";
        }

        // 출하 확정 게이트: 모든 칸 OK일 때만. 못 누르면 바로 옆에 이유.
        private void ShowGate(System.Collections.Generic.IList<SlotRow> slots, int okCount)
        {
            _confirm.Enabled = okCount == slots.Count;
            if (_confirm.Enabled) { _reason.Text = "출하 가능 · " + okCount + "칸 모두 OK"; return; }
            var ngSlots = string.Join(", ", slots.Where(s => s.Verdict == SlotVerdict.Ng).Select(s => s.Slot.ToString("00")).ToArray());
            int waiting = slots.Count(s => s.Verdict == SlotVerdict.Waiting);
            var parts = new System.Collections.Generic.List<string>();
            if (ngSlots.Length > 0) parts.Add("NG 칸 " + ngSlots + " 대체 필요");
            if (waiting > 0) parts.Add("미스캔 " + waiting + "칸");
            _reason.Text = "출하 확정 불가 · " + string.Join(" · ", parts.ToArray());
        }

        private void ShowDeadline()
        {
            var left = _source.Deadline() - (_source.Now() + _elapsed.Elapsed);
            if (left < TimeSpan.Zero) left = TimeSpan.Zero;
            _deadline.Value = ((int)left.TotalMinutes).ToString("00") + ":" + left.Seconds.ToString("00");
            _deadline.Status = left.TotalMinutes <= DeadlineAlarmMinutes ? StatusKind.Alarm : left.TotalMinutes <= DeadlineWarnMinutes ? StatusKind.Warn : StatusKind.Run;
            int ok = _source.GetSlots().Count(s => s.Verdict == SlotVerdict.Ok);
            _deadline.Footer = "마감 " + _source.Deadline().ToString("HH:mm:ss") + " · 대차 " + ok + "/" + _source.GetSlots().Count + " OK";
        }

        private void OnScanned(object sender, ScanEventArgs e)
        {
            int ngBefore = _source.GetSlots().Count(s => s.Verdict == SlotVerdict.Ng);
            var result = _source.Scan(e.Code);
            if (!result.Accepted)
            {
                e.Accepted = false;
                WarnAfterScan(result.Message);
                return;
            }
            Reload();
            e.Accepted = _source.GetSlots().Count(s => s.Verdict == SlotVerdict.Ng) == ngBefore;   // 새 NG면 빨강 + 경고음
        }

        private void WarnAfterScan(string message)
        {
            BeginInvoke((Action)(() => ApcDialog.Warn(FindForm(), message)));
        }

        private void Substitute()
        {
            var target = _source.GetSlots().FirstOrDefault(s => s.Verdict == SlotVerdict.Ng);
            if (target == null)
            {
                ApcDialog.Warn(FindForm(), "대체할 NG 칸이 없습니다.");
                return;
            }
            using (var dlg = new SubstituteDialog(target, _source.GetSubstituteHistory(), _source.ValidateSubstitute))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.Substitute(dlg.Slot, dlg.ReplacementLabel, dlg.Reason);
                Reload();
            }
        }

        private void ConfirmShipment()
        {
            if (!ApcDialog.Confirm(FindForm(), "이 대차를 출하 확정합니다. 확정 후에는 되돌릴 수 없습니다.", "출하 확정", ApcButtonVariant.Primary)) return;
            _source.ConfirmShipment();
            Reload();
        }
    }
}
