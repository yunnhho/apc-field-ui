using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TorqueSequence.Data;

namespace TorqueSequence.Views
{
    /// <summary>
    /// 실린더 헤드 커버 체결 순서 화면. 시리얼·사원증 스캔 → 판정판 → (수동 입력 + Primary 한 개) → 체결 도식 + 포인트별 결과.
    /// 막는 동작: 스캔 미완료, 순서 아닌 포인트 값, NG 승인 전 재체결, NG 2회 재체결.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private static readonly Regex SerialPattern = new Regex(@"^HC-\d{6}-\d{4}$");
        private static readonly Regex BadgePattern = new Regex(@"^EMP-\d{5}$");
        private const int AutoMode = 0, ManualMode = 1;
        private const string ActionConfirm = "체결값 확정", ActionApprove = "재체결 승인";

        private readonly IMainViewSource _source;
        private readonly List<TightenRecord> _records = new List<TightenRecord>();
        private readonly HashSet<string> _approved = new HashSet<string>();   // "포인트:NG횟수"
        private bool _scrapped, _serialOk, _badgeOk;
        private string _outOfOrder;   // 순서 오류 안내(다음 정상 처리 때까지)

        private readonly ApcScanBox _serial = new ApcScanBox { ScanOnly = true, Width = S(240) };
        private readonly ApcScanBox _badge = new ApcScanBox { ScanOnly = true, Width = S(200) };
        private readonly ApcTabStrip _mode = new ApcTabStrip();
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        private readonly ApcValueCard _torque = Card("체결 토크", "N·m");
        private readonly ApcValueCard _angle = Card("체결 각도", "°");
        private readonly Label _torqueCap = Caption("토크 (N·m)"), _angleCap = Caption("각도 (°)");
        private readonly ApcNumericBox _torqueIn = new ApcNumericBox { Minimum = 0, Maximum = 50, DecimalPlaces = 1, Increment = 0.1m, Width = S(130) };
        private readonly ApcNumericBox _angleIn = new ApcNumericBox { Minimum = 0, Maximum = 360, DecimalPlaces = 0, Increment = 1, Width = S(130) };
        private readonly ApcButton _action = Button(ActionConfirm, ApcButtonVariant.Primary, 150);
        private readonly Label _reason = Caption("");
        private readonly BoltBoard _board = new BoltBoard();
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, FillRows = true };

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("실린더 헤드 커버 체결")
        {
            _source = source;
            _serial.Accepts = c => c.StartsWith("HC-");
            _badge.Accepts = c => c.StartsWith("EMP-");
            _mode.SetTabs("자동 (공구 통신)", "수동 (토크렌치)");
            _mode.Width = _mode.PreferredTabsWidth;
            _torqueIn.Clear();
            _angleIn.Clear();
            BuildGrid();

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "OK"), new ApcLampValue(StatusKind.Alarm, "NG"),
                new ApcLampValue(StatusKind.Warn, "다음"), new ApcLampValue(StatusKind.Stop, "대기"));

            Body.Controls.Add(FillColumns(new Control[] { Titled("실린더 헤드 커버 · 체결 포인트", _board, legend), Titled("포인트별 결과", _grid) },
                new[] { 58f, 42f }));
            Body.Controls.Add(Row(_torqueCap, _torqueIn, _angleCap, _angleIn, _action, _reason));
            Body.Controls.Add(Columns(S(190), new Control[] { _judge, _torque, _angle }, new[] { 44f, 32f, 24f }));
            Body.Controls.Add(Row(Caption("부품 시리얼"), _serial, Caption("작업자 사원증"), _badge, Caption("체결 모드"), _mode));

            LoadSession();
            Reload();

            // 이벤트는 마지막에 연결한다(Clear·Rows.Add가 일으키는 이벤트 방지).
            _serial.Scanned += OnSerialScanned;
            _badge.Scanned += OnBadgeScanned;
            _mode.SelectedIndexChanged += (s, e) => Reload();
            _torqueIn.ValueChanged += (s, e) => Reload();
            _angleIn.ValueChanged += (s, e) => Reload();
            _action.Click += (s, e) => OnAction();
            _source.ToolReceived += OnToolReceived;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) { _serial.Attach(form); _badge.Attach(form); }
        }

        // ---- 상태 계산 ----
        private bool InProgress { get { return _serialOk && _badgeOk && !_scrapped && NextPoint() != 0; } }
        private bool Complete { get { return _serialOk && !_scrapped && NextPoint() == 0; } }

        private TightenRecord LastOf(int point) { return _records.LastOrDefault(r => r.Point == point); }
        private int NgCount(int point) { return _records.Count(r => r.Point == point && !TightenRules.IsOk(r)); }
        private static string ApprovalKey(int point, int ng) { return point + ":" + ng; }

        /// <summary>다음 체결할 포인트(순서 고정). 모두 OK면 0.</summary>
        private int NextPoint()
        {
            foreach (int p in TightenRules.Sequence)
            {
                var last = LastOf(p);
                if (last == null || !TightenRules.IsOk(last)) return p;
            }
            return 0;
        }

        /// <summary>다음 포인트가 NG이고 아직 재체결 승인을 받지 못했으면 true.</summary>
        private bool PendingNg
        {
            get
            {
                int next = NextPoint();
                if (next == 0 || _scrapped) return false;
                var last = LastOf(next);
                return last != null && !_approved.Contains(ApprovalKey(next, NgCount(next)));
            }
        }

        private bool NgBlocked { get { int n = NextPoint(); return n != 0 && PendingNg && NgCount(n) >= TightenRules.MaxNgPerPoint; } }

        private void LoadSession()
        {
            _serial.Text = _source.CurrentSerial;
            _badge.Text = _source.CurrentOperator;
            _serialOk = SerialPattern.IsMatch(_serial.Text);
            _badgeOk = BadgePattern.IsMatch(_badge.Text);
            if (_serialOk) _records.AddRange(_source.LoadRecords(_serial.Text));
            // 이어 받은 기록 중 NG 뒤에 재체결이 있었던 포인트는 승인된 것으로 본다.
            foreach (int p in TightenRules.Sequence)
            {
                int ng = 0;
                foreach (var r in _records.Where(x => x.Point == p))
                {
                    if (!TightenRules.IsOk(r)) ng++;
                    else if (ng > 0) _approved.Add(ApprovalKey(p, ng));
                }
            }
        }

        // ---- 스캔 ----
        private void OnSerialScanned(object sender, ScanEventArgs e)
        {
            if (!SerialPattern.IsMatch(e.Code)) { Reject(e, "부품 시리얼 형식이 아닙니다 (HC-yyMMdd-nnnn)."); return; }
            if (_serialOk && e.Code != _serial.Text && !_scrapped && NextPoint() != 0)
            { Reject(e, "체결 중인 부품이 있습니다. 완료 또는 불량 처리 후 스캔하세요."); return; }
            if (e.Code != _serial.Text)
            {
                _records.Clear(); _approved.Clear(); _scrapped = false; _outOfOrder = null;
                _records.AddRange(_source.LoadRecords(e.Code));
            }
            _serialOk = true;
            Reload();
        }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            if (!BadgePattern.IsMatch(e.Code)) { Reject(e, "사원증 형식이 아닙니다 (EMP-nnnnn)."); return; }
            _badgeOk = true;
            Reload();
        }

        private void Reject(ScanEventArgs e, string message)
        {
            e.Accepted = false;
            BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(FindForm(), message)));
        }

        // ---- 체결값 입력 ----
        private void OnToolReceived(TightenRecord r)
        {
            if (_mode.SelectedIndex != AutoMode) return;   // 수동 모드에서는 공구 값을 받지 않는다
            BeginInvoke((MethodInvoker)(() => Accept(r)));
        }

        private void OnAction()
        {
            if (PendingNg) { OpenApproval(); return; }
            if (!InProgress || _mode.SelectedIndex != ManualMode || !_torqueIn.HasValue || !_angleIn.HasValue) return;
            Accept(new TightenRecord(NextPoint(), _torqueIn.Value, (int)_angleIn.Value, DateTime.Now));
            _torqueIn.Clear();
            _angleIn.Clear();
        }

        /// <summary>체결값 한 건 처리. 시작 전·승인 전·순서 오류는 막고 이유를 보인다.</summary>
        private void Accept(TightenRecord r)
        {
            string block = !(_serialOk && _badgeOk) ? "시리얼·사원증 스캔 전 값은 받지 않습니다."
                : _scrapped ? "불량 처리된 부품입니다."
                : NextPoint() == 0 ? "체결이 끝난 부품입니다."
                : PendingNg ? "재체결 승인 전 값은 받지 않습니다."
                : r.Point != NextPoint() ? r.Point + "번 값 수신 · 지금은 " + NextPoint() + "번 차례" : null;
            if (block != null)
            {
                if (_serialOk && _badgeOk && !_scrapped && NextPoint() != 0 && !PendingNg) _outOfOrder = block;
                Reload();
                ApcDialog.Warn(FindForm(), block);
                return;
            }
            _outOfOrder = null;
            _records.Add(r);
            _source.SaveRecord(_serial.Text, _badge.Text, r);
            Reload();
        }

        private void OpenApproval()
        {
            int p = NextPoint();
            var last = LastOf(p);
            using (var dlg = new RetightenApprovalDialog(_serial.Text, p, last.Torque, last.Angle, NgCount(p)))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (dlg.ScrapRequested) { _scrapped = true; _source.SaveScrap(_serial.Text, p); }
                else { _approved.Add(ApprovalKey(p, NgCount(p))); _source.SaveApproval(_serial.Text, p, dlg.SupervisorBadge, dlg.Reason); }
                Reload();
            }
        }

        // ---- 화면 갱신 ----
        private void Reload()
        {
            ShowJudge();
            ShowCards();
            ShowBoardAndGrid();
            ShowAction();
        }

        private void ShowJudge()
        {
            int next = NextPoint();
            var last = next == 0 ? null : LastOf(next);
            string text, sub; StatusKind kind;
            if (!(_serialOk && _badgeOk))
            {
                text = "READY"; kind = StatusKind.Stop;
                sub = !_serialOk && !_badgeOk ? "부품 시리얼 · 사원증 스캔 필요" : !_serialOk ? "부품 시리얼 스캔 필요" : "사원증 스캔 필요";
            }
            else if (_scrapped)
            {
                text = "불량"; kind = StatusKind.Alarm; sub = "부품 불량 처리됨 · 새 시리얼 스캔";
            }
            else if (next == 0)
            {
                text = "완료"; kind = StatusKind.Run; sub = "10/10 OK · 새 시리얼 스캔";
            }
            else if (NgBlocked)
            {
                text = "NG"; kind = StatusKind.Alarm; sub = next + "번 NG " + NgCount(next) + "회 · 부품 불량 처리";
            }
            else if (PendingNg)
            {
                text = "NG"; kind = StatusKind.Alarm;
                sub = next + "번 " + TightenRules.NgItems(last) + " 이탈 · 재체결 승인 필요";
            }
            else if (_outOfOrder != null)
            {
                text = "순서 오류"; kind = StatusKind.Warn; sub = _outOfOrder;
            }
            else
            {
                text = "다음 " + next + "번"; kind = StatusKind.Run;
                sub = DoneCount() + "/10 완료 · " + (_mode.SelectedIndex == AutoMode ? "공구 체결 대기" : "토크·각도 입력");
            }
            _judge.Text = text; _judge.SubText = sub; _judge.Status = kind;
        }

        private int DoneCount() { return TightenRules.Sequence.Count(p => LastOf(p) != null && TightenRules.IsOk(LastOf(p))); }

        private void ShowCards()
        {
            var r = _records.Count == 0 ? null : _records[_records.Count - 1];
            if (r == null)
            {
                _torque.Value = ""; _torque.Footer = ""; _torque.Status = StatusKind.Stop;
                _angle.Value = ""; _angle.Footer = ""; _angle.Status = StatusKind.Stop;
                return;
            }
            string source = r.Point + "번 · " + r.Time.ToString("HH:mm:ss");
            _torque.Value = TightenRules.Torque(r.Torque); _torque.Footer = source;
            _torque.Status = TightenRules.TorqueOk(r.Torque) ? StatusKind.Run : StatusKind.Alarm;
            _angle.Value = r.Angle.ToString(CultureInfo.InvariantCulture); _angle.Footer = source;
            _angle.Status = TightenRules.AngleOk(r.Angle) ? StatusKind.Run : StatusKind.Alarm;
        }

        private void ShowBoardAndGrid()
        {
            int next = InProgress ? NextPoint() : 0;
            for (int i = 0; i < TightenRules.Sequence.Length; i++)
            {
                int p = TightenRules.Sequence[i];
                var last = LastOf(p);
                StatusKind kind; string word;
                if (last != null && TightenRules.IsOk(last)) { kind = StatusKind.Run; word = "OK"; }
                else if (last != null) { kind = StatusKind.Alarm; word = "NG"; }
                else if (p == next) { kind = StatusKind.Warn; word = "다음"; }
                else { kind = StatusKind.Stop; word = "대기"; }
                _board.SetBolt(p, kind, word);

                var row = _grid.Rows[i];
                bool torqueNg = last != null && !TightenRules.TorqueOk(last.Torque);
                bool angleNg = last != null && !TightenRules.AngleOk(last.Angle);
                row.Cells[0].Value = p;
                SetCell(row.Cells[1], last == null ? "" : TightenRules.Torque(last.Torque), torqueNg);
                SetCell(row.Cells[2], last == null ? "" : last.Angle.ToString(CultureInfo.InvariantCulture), angleNg);
                int tries = _records.Count(x => x.Point == p);
                row.Cells[3].Value = tries == 0 ? "" : tries.ToString();
                // 이미 NG였다가 재체결을 기다리는 포인트도 NG 표시(다음 강조는 도식에서)
                row.Cells[4].Value = new ApcLampValue(kind, word);
            }
        }

        private static void SetCell(DataGridViewCell cell, string text, bool outOfSpec)
        {
            cell.Value = text;
            cell.Style.ForeColor = outOfSpec ? ApcTheme.StatusText(StatusKind.Alarm) : System.Drawing.Color.Empty;
            cell.Style.Font = outOfSpec ? ApcTheme.GetFont(System.Drawing.FontStyle.Bold) : null;
        }

        private void ShowAction()
        {
            bool manual = _mode.SelectedIndex == ManualMode;
            foreach (Control c in new Control[] { _torqueCap, _torqueIn, _angleCap, _angleIn }) c.Visible = manual;
            bool pending = PendingNg;
            bool entry = InProgress && !pending;
            _torqueIn.Enabled = _angleIn.Enabled = entry;

            string reason;
            bool enabled;
            if (pending)
            {
                _action.Text = ActionApprove; enabled = true; reason = "";
            }
            else
            {
                _action.Text = ActionConfirm;
                if (!(_serialOk && _badgeOk)) { enabled = false; reason = "시리얼·사원증 스캔 필요"; }
                else if (_scrapped) { enabled = false; reason = "불량 처리됨 · 새 시리얼 스캔"; }
                else if (!entry) { enabled = false; reason = "체결 완료 · 새 시리얼 스캔"; }
                else if (!manual) { enabled = false; reason = "공구 통신값 대기"; }
                else if (!_torqueIn.HasValue || !_angleIn.HasValue) { enabled = false; reason = "토크·각도 입력 필요"; }
                else { enabled = true; reason = ""; }
            }
            _action.Enabled = enabled;
            _reason.Text = reason;
        }

        // ---- 구성 ----
        private void BuildGrid()
        {
            _grid.Columns.Add("no", "번호");
            _grid.Columns.Add("torque", "토크(N·m)");
            _grid.Columns.Add("angle", "각도(°)");
            _grid.Columns.Add("tries", "시도(회)");
            _grid.Columns.Add(new ApcLampColumn { HeaderText = "판정" });
            foreach (DataGridViewColumn c in _grid.Columns)
                if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (int p in TightenRules.Sequence) _grid.Rows.Add(p, "", "", "", new ApcLampValue(StatusKind.Stop, "대기"));
        }

        private static ApcValueCard Card(string title, string unit)
        {
            return new ApcValueCard { Title = title, Unit = unit, Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        }
    }
}
