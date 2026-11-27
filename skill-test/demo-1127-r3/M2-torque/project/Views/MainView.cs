using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TorqueSequence.Data;

namespace TorqueSequence.Views
{
    /// <summary>
    /// 실린더 헤드 커버 체결 순서 화면: 스캔 줄 → 판정판 + 현재 토크·각도 → Primary 버튼 줄 → 도식 + 포인트별 결과.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private static readonly Regex SerialFormat = new Regex(@"^HC-\d{6}-\d{4}$");
        private static readonly Regex BadgeFormat = new Regex(@"^EMP-\d{5}$");
        private const int AutoMode = 0, ManualMode = 1;

        private readonly IMainViewSource _source;
        private readonly Dictionary<int, PointResult> _results = new Dictionary<int, PointResult>();
        private readonly int[] _ngCounts = new int[TorqueJudge.PointCount + 1];
        private bool _reworkApproved;

        private readonly ApcScanBox _serialBox = new ApcScanBox { ScanOnly = true };
        private readonly ApcScanBox _badgeBox = new ApcScanBox { ScanOnly = true };
        private readonly ApcTabStrip _mode = new ApcTabStrip();
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcValueCard _torque = new ApcValueCard { Dock = DockStyle.Fill, Unit = "N·m" };
        private readonly ApcValueCard _angle = new ApcValueCard { Dock = DockStyle.Fill, Unit = "°" };
        private readonly ApcButton _action;
        private readonly Label _reason = Caption("");
        private readonly Label _torqueCaption = Caption("토크 (N·m)");
        private readonly Label _angleCaption = Caption("각도 (°)");
        private readonly ApcNumericBox _torqueInput = new ApcNumericBox { DecimalPlaces = 1, Minimum = 0, Maximum = 99.9m, Increment = 0.1m, Width = S(110) };
        private readonly ApcNumericBox _angleInput = new ApcNumericBox { DecimalPlaces = 0, Minimum = 0, Maximum = 360, Increment = 1, Width = S(110) };
        private readonly BoltBoard _board = new BoltBoard { Dock = DockStyle.Fill };
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, FillRows = true };
        private PointResult _lastReceived;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("체결 순서")
        {
            _source = source;
            SessionSnapshot session = source.GetSession();
            _serialBox.Text = session.Serial;
            _badgeBox.Text = session.Operator;
            foreach (PointResult r in session.Results) { _results[r.Point] = r; _lastReceived = r; }
            Array.Copy(session.NgCounts, _ngCounts, Math.Min(session.NgCounts.Length, _ngCounts.Length));

            _serialBox.Width = S(260);
            _badgeBox.Width = S(220);
            _serialBox.Accepts = c => c.StartsWith("HC-");
            _badgeBox.Accepts = c => c.StartsWith("EMP-");
            _mode.SetTabs("자동 (공구 통신)", "수동 (토크렌치 입력)");
            _mode.Width = _mode.PreferredTabsWidth;
            _torqueInput.Clear();
            _angleInput.Clear();

            _action = Button("공구값 받기", ApcButtonVariant.Primary, 190);
            _grid.Columns.Add(new ApcLampColumn { HeaderText = "판정" });
            _grid.Columns.Add("point", "번호");
            _grid.Columns.Add("torque", "토크(N·m)");
            _grid.Columns.Add("angle", "각도(°)");
            _grid.Columns.Add("time", "시각");
            foreach (DataGridViewColumn c in _grid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "OK"), new ApcLampValue(StatusKind.Alarm, "NG"),
                new ApcLampValue(StatusKind.Warn, "다음"), new ApcLampValue(StatusKind.Stop, "대기"));
            string limits = "포인트별 결과 (토크 " + TorqueJudge.FormatTorque(TorqueJudge.TorqueMin) + " ~ " + TorqueJudge.FormatTorque(TorqueJudge.TorqueMax)
                + " N·m · 각도 " + TorqueJudge.AngleMin + " ~ " + TorqueJudge.AngleMax + " °)";

            // Top 도킹은 나중에 Add한 것이 위 → Fill 먼저, 아래 줄부터
            Body.Controls.Add(FillColumns(new Control[] { Titled("체결 포인트 (체결 순서 1-6-2-7-3-8-4-9-5-10)", _board, legend), Titled(limits, _grid) }, new[] { 50f, 50f }));
            Body.Controls.Add(Row(_action, _torqueCaption, _torqueInput, _angleCaption, _angleInput, _reason));
            Body.Controls.Add(Columns(S(170), new Control[] { _judge, _torque, _angle }, new[] { 52f, 28f, 20f }));
            Body.Controls.Add(Row(Caption("부품 시리얼"), _serialBox, Caption("작업자 사원증"), _badgeBox, Caption("체결 모드"), _mode));

            Reload();
            // 이벤트는 컨트롤을 모두 만든 뒤 마지막에 구독
            _serialBox.Scanned += OnSerialScanned;
            _badgeBox.Scanned += OnBadgeScanned;
            _mode.SelectedIndexChanged += (s, e) => Reload();
            _torqueInput.ValueChanged += (s, e) => Reload();
            _angleInput.ValueChanged += (s, e) => Reload();
            _action.Click += (s, e) => OnAction();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) { _serialBox.Attach(form); _badgeBox.Attach(form); }   // 생성자 시점엔 폼이 없다
        }

        // ── 상태 계산 ──────────────────────────────────────────────

        private bool Ready { get { return _serialBox.Text.Length > 0 && _badgeBox.Text.Length > 0; } }

        /// <summary>다음 체결할 포인트(OK가 아닌 첫 순서). 모두 끝났으면 0.</summary>
        private int NextPoint()
        {
            foreach (int p in TorqueJudge.Sequence)
            {
                PointResult r;
                if (!_results.TryGetValue(p, out r) || TorqueJudge.Reason(r) != null) return p;
            }
            return 0;
        }

        private bool IsNg(int point)
        {
            PointResult r;
            return _results.TryGetValue(point, out r) && TorqueJudge.Reason(r) != null;
        }

        private int DoneCount()
        {
            int n = 0;
            foreach (int p in TorqueJudge.Sequence) if (_results.ContainsKey(p) && !IsNg(p)) n++;
            return n;
        }

        private bool Scrapped(int next) { return next > 0 && IsNg(next) && _ngCounts[next] >= TorqueJudge.MaxNgPerPoint; }
        private bool NeedsApproval(int next) { return next > 0 && IsNg(next) && !_reworkApproved; }
        private bool InProgress() { int n = NextPoint(); return _results.Count > 0 && n != 0 && !Scrapped(n); }

        // ── 화면 갱신 ──────────────────────────────────────────────

        private void Reload()
        {
            int next = NextPoint();
            bool manual = _mode.SelectedIndex == ManualMode;
            bool blocked = !Ready || next == 0 || NeedsApproval(next);   // 체결값을 받을 수 없는 상태

            ShowBoard(next);
            ShowGrid(next);
            ShowJudge(next);
            ShowLastValue();

            _torqueCaption.Visible = _torqueInput.Visible = _angleCaption.Visible = _angleInput.Visible = manual;
            _torqueInput.Enabled = _angleInput.Enabled = !blocked;
            _action.Text = NeedsApproval(next) ? "재체결 승인" : manual ? "측정값 확정" : "공구값 받기";
            bool inputMissing = manual && !blocked && (!_torqueInput.HasValue || !_angleInput.HasValue);
            _action.Enabled = NeedsApproval(next) || (!blocked && !inputMissing);
            _reason.Text = ActionReason(next, inputMissing);
        }

        private string ActionReason(int next, bool inputMissing)
        {
            if (!Ready) return _serialBox.Text.Length == 0 && _badgeBox.Text.Length == 0 ? "시리얼·사원증 스캔 필요" : _serialBox.Text.Length == 0 ? "시리얼 스캔 필요" : "사원증 스캔 필요";
            if (next == 0) return "다음 부품 시리얼 스캔 필요";
            if (Scrapped(next)) return next + "번 NG 2회 · 재체결 불가";
            if (NeedsApproval(next)) return "재체결 승인 전 체결 불가";
            return inputMissing ? "토크·각도 입력 필요" : "";
        }

        private void ShowBoard(int next)
        {
            foreach (int p in TorqueJudge.Sequence)
            {
                StatusKind kind = !_results.ContainsKey(p) ? StatusKind.Stop : IsNg(p) ? StatusKind.Alarm : StatusKind.Run;
                bool isNext = p == next && Ready;
                if (isNext && kind != StatusKind.Alarm) kind = StatusKind.Warn;
                _board.SetPoint(p, kind, isNext);
            }
        }

        private void ShowGrid(int next)
        {
            _grid.Rows.Clear();
            foreach (int p in TorqueJudge.Sequence)
            {
                PointResult r;
                bool has = _results.TryGetValue(p, out r);
                bool ng = has && IsNg(p);
                ApcLampValue lamp;
                if (p == next && Ready && (!ng || _reworkApproved)) lamp = new ApcLampValue(StatusKind.Warn, ng ? "재체결" : "다음");
                else if (!has) lamp = new ApcLampValue(StatusKind.Stop, "대기");
                else lamp = new ApcLampValue(ng ? StatusKind.Alarm : StatusKind.Run, ng ? "NG" : "OK");
                int i = _grid.Rows.Add(lamp, p.ToString(), has ? TorqueJudge.FormatTorque(r.Torque) : "", has ? r.Angle.ToString() : "", has ? r.Time.ToString("HH:mm:ss") : "");
                if (has && !TorqueJudge.TorqueOk(r.Torque)) MarkOut(_grid.Rows[i].Cells[2]);
                if (has && !TorqueJudge.AngleOk(r.Angle)) MarkOut(_grid.Rows[i].Cells[3]);
            }
        }

        private static void MarkOut(DataGridViewCell cell)
        {
            cell.Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
        }

        private void ShowJudge(int next)
        {
            if (!Ready)
            {
                Judge("스캔 대기", StatusKind.Stop, _serialBox.Text.Length == 0 && _badgeBox.Text.Length == 0 ? "부품 시리얼 · 작업자 사원증 스캔" : _serialBox.Text.Length == 0 ? "부품 시리얼 스캔 필요" : "작업자 사원증 스캔 필요");
            }
            else if (next == 0) Judge("체결 완료", StatusKind.Run, "10 / 10 OK · 다음 부품 시리얼 스캔");
            else if (Scrapped(next)) Judge("부품 불량", StatusKind.Alarm, next + "번 NG 2회 · 부품 불량 처리");
            else if (NeedsApproval(next)) Judge("NG", StatusKind.Alarm, next + "번 " + TorqueJudge.Reason(_results[next]));
            else if (_reworkApproved) Judge(next + "번 재체결", StatusKind.Run, "반장 승인 완료 · 진행 " + DoneCount() + " / 10");
            else Judge(next + "번 체결", StatusKind.Run, "진행 " + DoneCount() + " / 10 · 순서 " + (Array.IndexOf(TorqueJudge.Sequence, next) + 1));
        }

        private void Judge(string text, StatusKind status, string sub)
        {
            _judge.Text = text; _judge.Status = status; _judge.SubText = sub;
        }

        /// <summary>마지막으로 받은 체결값. 아직 받은 값이 없으면 빈 칸(값 없음 = 판정 없음).</summary>
        private void ShowLastValue()
        {
            PointResult r = _lastReceived;
            _torque.Title = r == null ? "토크" : r.Point + "번 토크";
            _angle.Title = r == null ? "각도" : r.Point + "번 각도";
            if (r == null || !Ready)
            {
                _torque.Value = _angle.Value = ""; _torque.Footer = _angle.Footer = "";
                _torque.Status = _angle.Status = StatusKind.Stop;
                return;
            }
            _torque.Value = TorqueJudge.FormatTorque(r.Torque);
            _angle.Value = r.Angle.ToString();
            _torque.Status = TorqueJudge.TorqueOk(r.Torque) ? StatusKind.Run : StatusKind.Alarm;
            _angle.Status = TorqueJudge.AngleOk(r.Angle) ? StatusKind.Run : StatusKind.Alarm;
            _torque.Footer = _angle.Footer = r.Time.ToString("HH:mm:ss") + " 수신";
        }

        // ── 동작 ──────────────────────────────────────────────────

        private void OnAction()
        {
            int next = NextPoint();
            if (NeedsApproval(next) || Scrapped(next)) { OpenRework(next); return; }
            if (!Ready || next == 0) return;
            if (_mode.SelectedIndex == ManualMode)
            {
                _torqueInput.Commit(); _angleInput.Commit();
                if (!_torqueInput.HasValue || !_angleInput.HasValue) return;
                var manual = new PointResult(next, Math.Round(_torqueInput.Value, 1), (int)_angleInput.Value, DateTime.Now);
                _torqueInput.Clear(); _angleInput.Clear();
                Accept(manual);
                return;
            }
            PointResult read = _source.ReadTool();
            if (read == null) { Warn("공구에서 받은 체결값이 없습니다"); return; }
            Accept(read);
        }

        /// <summary>체결값 한 건 처리. 순서가 아닌 포인트의 값은 막는다.</summary>
        private void Accept(PointResult r)
        {
            int next = NextPoint();
            if (r.Point != next)
            {
                Warn("체결 순서 위반 · " + next + "번 차례인데 " + r.Point + "번 값이 들어왔습니다 (값은 버림)");
                return;
            }
            bool ng = TorqueJudge.Reason(r) != null;
            _results[r.Point] = r;
            _lastReceived = r;
            if (ng) _ngCounts[r.Point]++;
            _reworkApproved = false;
            _source.SaveResult(_serialBox.Text, _badgeBox.Text, r);
            Reload();
        }

        private void OpenRework(int point)
        {
            PointResult r = _results[point];
            using (var dlg = new ReworkDialog(point, r.Torque, r.Angle, r.Time, _ngCounts[point]))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.SaveRework(_serialBox.Text, point, dlg.Supervisor, dlg.ReasonText);
                _reworkApproved = true;
                Reload();
            }
        }

        private void OnSerialScanned(object sender, ScanEventArgs e)
        {
            if (!SerialFormat.IsMatch(e.Code)) { e.Accepted = false; Warn("부품 시리얼 형식이 아닙니다 (HC-yyMMdd-nnnn) · " + e.Code); return; }
            if (InProgress()) { e.Accepted = false; Warn("체결 진행 중 · 현재 부품을 마칠 때까지 시리얼을 바꿀 수 없습니다"); return; }
            _results.Clear(); Array.Clear(_ngCounts, 0, _ngCounts.Length);
            _lastReceived = null; _reworkApproved = false;
            _serialBox.Text = e.Code;
            Reload();
            if (_badgeBox.Text.Length == 0) _badgeBox.Focus();
        }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            if (!BadgeFormat.IsMatch(e.Code)) { e.Accepted = false; Warn("사원증 형식이 아닙니다 (EMP-nnnnn) · " + e.Code); return; }
            if (InProgress()) { e.Accepted = false; Warn("체결 진행 중 · 작업자를 바꿀 수 없습니다"); return; }
            _badgeBox.Text = e.Code;
            Reload();
        }

        /// <summary>경고창. 스캔 이벤트 안에서 바로 띄우지 않고 처리가 끝난 뒤 띄운다.</summary>
        private void Warn(string message)
        {
            BeginInvoke(new Action(() => ApcDialog.Warn(FindForm(), message)));
        }
    }
}
