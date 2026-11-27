using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TorqueSequence.Data;

namespace TorqueSequence.Views
{
    /// <summary>메인 화면. 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private enum Phase { Idle, Ready, Running, Retighten, Blocked, Complete, Scrapped }

        private const string AutoMode = "자동 (체결 공구 통신값)", ManualMode = "수동 (토크렌치 측정값 입력)";
        private const int Total = 10;

        private readonly IMainViewSource _source;
        private readonly List<PointResult> _attempts = new List<PointResult>();
        private string _serialCode = "", _badgeCode = "";
        private bool _started, _approved, _scrapped, _ready;

        private readonly ApcScanBox _serial = new ApcScanBox { Accepts = c => c.StartsWith("HC-") };
        private readonly ApcScanBox _badge = new ApcScanBox { Accepts = c => c.StartsWith("EMP-") };
        private readonly ApcComboBox _mode = new ApcComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        private readonly ApcValueCard _nextCard = new ApcValueCard { Title = "체결할 포인트", Unit = "번", Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        private readonly ApcValueCard _torqueCard = new ApcValueCard { Title = "토크", Unit = "N·m", Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        private readonly ApcButton _primary = Button("작업 시작", ApcButtonVariant.Primary, 200);
        private readonly Label _reason = Caption("");
        private readonly Label _torqueCaption = Caption("토크 (N·m)"), _angleCaption = Caption("각도 (°)");
        private readonly ApcNumericBox _torqueInput = new ApcNumericBox { DecimalPlaces = 1, Minimum = 0m, Maximum = 50m, Increment = 0.1m, Width = S(130) };
        private readonly ApcNumericBox _angleInput = new ApcNumericBox { DecimalPlaces = 0, Minimum = 0m, Maximum = 360m, Increment = 1m, Width = S(120) };
        private readonly BoltDiagram _diagram = new BoltDiagram();
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, FillRows = true };

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("실린더 헤드 커버 체결 순서")
        {
            _source = source;
            _serial.Width = S(280);
            _badge.Width = S(220);
            _mode.Width = S(300);
            _mode.Items.AddRange(new object[] { AutoMode, ManualMode });
            _mode.SelectedIndex = 0;
            _mode.SelectedIndexChanged += (s, e) => { _torqueInput.Clear(); _angleInput.Clear(); Render(); };
            _serial.Scanned += (s, e) => e.Accepted = OnSerial(e.Code);
            _badge.Scanned += (s, e) => e.Accepted = OnBadge(e.Code);
            _primary.Click += (s, e) => OnPrimary();
            _torqueInput.ValueChanged += (s, e) => Render();
            _angleInput.ValueChanged += (s, e) => Render();
            _torqueInput.Clear();
            _angleInput.Clear();

            _grid.Columns.Add("no", "번호");
            _grid.Columns.Add("torque", "토크(N·m)");
            _grid.Columns.Add("angle", "각도(°)");
            var lamp = new ApcLampColumn { HeaderText = "판정" };
            _grid.Columns.Add(lamp);
            foreach (DataGridViewColumn c in _grid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "OK"), new ApcLampValue(StatusKind.Alarm, "NG"),
                new ApcLampValue(StatusKind.Warn, "다음"), new ApcLampValue(StatusKind.Stop, "대기"));

            _serialCode = source.LoadSerial();
            _badgeCode = source.LoadBadge();
            _serial.Text = _serialCode;
            _badge.Text = _badgeCode;
            _attempts.AddRange(source.LoadAttempts());
            _started = _attempts.Count > 0;

            // Fill 먼저, Top 줄은 아래쪽부터
            Body.Controls.Add(FillColumns(new Control[]
            {
                Titled("체결 포인트", _diagram, legend),
                Titled("포인트별 결과 · 토크 " + TorqueRules.Torque(TorqueRules.TorqueMin) + " ~ " + TorqueRules.Torque(TorqueRules.TorqueMax)
                    + " N·m · 각도 " + TorqueRules.AngleMin + " ~ " + TorqueRules.AngleMax + "°", _grid)
            }, new[] { 55f, 45f }));
            Body.Controls.Add(Row(_primary, _torqueCaption, _torqueInput, _angleCaption, _angleInput, _reason));
            Body.Controls.Add(Columns(S(170), new Control[] { _judge, _nextCard, _torqueCard }, new[] { 50f, 20f, 30f }));
            Body.Controls.Add(Row(Caption("부품 시리얼"), _serial, Caption("작업자 사원증"), _badge, Caption("체결 모드"), _mode));
            _ready = true;
            Render();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) { _serial.Attach(form); _badge.Attach(form); }
        }

        // ---- 상태 계산 ----------------------------------------------------------

        private bool Working { get { Phase p = GetPhase(); return p == Phase.Running || p == Phase.Retighten || p == Phase.Blocked; } }

        private PointResult Latest(int point)
        {
            for (int i = _attempts.Count - 1; i >= 0; i--) if (_attempts[i].Point == point) return _attempts[i];
            return null;
        }

        /// <summary>지금 체결할 순서 위치(0~9). 전부 OK면 Total.</summary>
        private int CurrentIndex()
        {
            for (int i = 0; i < Total; i++)
            {
                var r = Latest(TorqueRules.Sequence[i]);
                if (r == null || !r.Ok) return i;
            }
            return Total;
        }

        private int NgCount(int point)
        {
            int n = 0;
            foreach (var a in _attempts) if (a.Point == point && !a.Ok) n++;
            return n;
        }

        private Phase GetPhase()
        {
            if (_scrapped) return Phase.Scrapped;
            if (!_started)
                return TorqueRules.IsSerial(_serialCode) && TorqueRules.IsBadge(_badgeCode) ? Phase.Ready : Phase.Idle;
            int idx = CurrentIndex();
            if (idx == Total) return Phase.Complete;
            if (Latest(TorqueRules.Sequence[idx]) == null) return Phase.Running;
            return _approved ? Phase.Retighten : Phase.Blocked;
        }

        // ---- 스캔 ---------------------------------------------------------------

        private bool OnSerial(string code)
        {
            if (!TorqueRules.IsSerial(code)) return Reject("시리얼 형식이 맞지 않습니다 · HC-yyMMdd-nnnn");
            if (code == _serialCode) return true;
            if (Working) return Reject("체결 중에는 시리얼을 바꿀 수 없습니다 · 작업 완료 후 스캔하세요");
            _attempts.Clear();   // 새 부품 → 작업 초기화(사원증은 유지)
            _started = false; _approved = false; _scrapped = false;
            _serialCode = code;
            Render();
            return true;
        }

        private bool OnBadge(string code)
        {
            if (!TorqueRules.IsBadge(code)) return Reject("사원증 형식이 맞지 않습니다 · EMP-nnnnn");
            if (code == _badgeCode) return true;
            if (Working) return Reject("체결 중에는 작업자를 바꿀 수 없습니다 · 작업 완료 후 스캔하세요");
            _badgeCode = code;
            Render();
            return true;
        }

        /// <summary>스캔 이벤트 안에서는 경고창을 바로 열지 않는다. 처리 후 띄운다.</summary>
        private bool Reject(string message)
        {
            Warn(message);
            return false;
        }

        private void Warn(string message)
        {
            BeginInvoke(new Action(() => ApcDialog.Warn(FindForm(), message)));
        }

        // ---- 동작 ---------------------------------------------------------------

        private void OnPrimary()
        {
            switch (GetPhase())
            {
                case Phase.Ready:
                    _started = true;
                    Render();
                    break;
                case Phase.Blocked:
                    OpenApproval();
                    break;
                case Phase.Running:
                case Phase.Retighten:
                    ReceiveValue();
                    break;
            }
        }

        private void ReceiveValue()
        {
            int expected = TorqueRules.Sequence[CurrentIndex()];
            PointResult r;
            if (_mode.SelectedItem as string == ManualMode)
            {
                _torqueInput.Commit();
                _angleInput.Commit();
                if (!_torqueInput.HasValue || !_angleInput.HasValue) return;
                r = new PointResult(expected, _torqueInput.Value, (int)_angleInput.Value, DateTime.Now);
            }
            else
            {
                r = _source.ReadToolValue(expected);   // 실제로는 공구 통신 이벤트가 같은 Accept를 부른다
            }
            Accept(r);
        }

        /// <summary>체결값 한 건을 받는다. 순서가 아니거나 막힌 상태면 기록하지 않고 막는다.</summary>
        private void Accept(PointResult r)
        {
            Phase p = GetPhase();
            if (p != Phase.Running && p != Phase.Retighten)
            {
                Warn("지금은 체결값을 받을 수 없습니다 · " + (p == Phase.Blocked ? "재체결 승인 후 체결하세요" : "작업 상태를 확인하세요"));
                return;
            }
            int expected = TorqueRules.Sequence[CurrentIndex()];
            if (r.Point != expected)
            {
                Warn("체결 순서가 아닙니다 · " + r.Point + "번 체결값 수신, 지금은 " + expected + "번 차례입니다");
                return;
            }
            _attempts.Add(r);
            _approved = false;
            _source.SaveAttempt(_serialCode, _badgeCode, r);
            _torqueInput.Clear();
            _angleInput.Clear();
            Render();
        }

        private void OpenApproval()
        {
            int point = TorqueRules.Sequence[CurrentIndex()];
            using (var dlg = new RetightenApprovalDialog(_serialCode, Latest(point), NgCount(point)))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (dlg.Scrap)
                {
                    _source.SaveScrap(_serialCode, point);
                    _scrapped = true;
                }
                else
                {
                    _source.SaveRetightenApproval(_serialCode, point, dlg.SupervisorBadge, dlg.Reason);
                    _approved = true;
                }
            }
            Render();
        }

        // ---- 화면 갱신 ----------------------------------------------------------

        private void Render()
        {
            if (_ready) Draw();
        }

        private void Draw()
        {
            Phase phase = GetPhase();
            int idx = CurrentIndex();
            int point = idx < Total ? TorqueRules.Sequence[idx] : 0;
            PointResult last = _attempts.Count > 0 ? _attempts[_attempts.Count - 1] : null;
            bool manual = _mode.SelectedItem as string == ManualMode;
            bool measuring = phase == Phase.Running || phase == Phase.Retighten;

            ShowJudge(phase, point, idx, last);
            ShowCards(phase, point, idx, last);
            ShowPrimary(phase, manual, measuring);
            ShowGrid(phase, point);
        }

        private void ShowJudge(Phase phase, int point, int idx, PointResult last)
        {
            switch (phase)
            {
                case Phase.Idle: Judge(StatusKind.Stop, "READY", "시리얼 · 사원증 스캔 대기"); break;
                case Phase.Ready: Judge(StatusKind.Stop, "READY", "작업 시작을 누르세요"); break;
                case Phase.Running:
                    Judge(StatusKind.Run, point + "번 체결", "체결 순서 " + (idx + 1) + " / " + Total + (last == null ? "" : " · 직전 " + last.Point + "번 OK"));
                    break;
                case Phase.Retighten:
                    Judge(StatusKind.Run, point + "번 재체결", "반장 승인 완료 · 다시 체결하세요");
                    break;
                case Phase.Blocked:
                    if (NgCount(point) >= TorqueRules.NgLimitPerPoint)
                        Judge(StatusKind.Alarm, "NG · " + point + "번", "재체결 불가 · 부품 불량 처리");
                    else
                        Judge(StatusKind.Alarm, "NG · " + point + "번", TorqueRules.Describe(Latest(point)) + " · 재체결 승인 필요");
                    break;
                case Phase.Complete: Judge(StatusKind.Run, "OK", "10 / 10 체결 완료 · " + _serialCode); break;
                case Phase.Scrapped: Judge(StatusKind.Alarm, "부품 불량", "NG " + NgCount(LastNgPoint()) + "회 · 불량 처리 완료"); break;
            }
        }

        private int LastNgPoint()
        {
            for (int i = _attempts.Count - 1; i >= 0; i--) if (!_attempts[i].Ok) return _attempts[i].Point;
            return 0;
        }

        private void Judge(StatusKind kind, string text, string sub)
        {
            _judge.Status = kind;
            _judge.Text = text;
            _judge.SubText = sub;
        }

        private void ShowCards(Phase phase, int point, int idx, PointResult last)
        {
            bool live = phase == Phase.Running || phase == Phase.Retighten || phase == Phase.Blocked;
            _nextCard.Value = live ? point.ToString() : "";
            _nextCard.Footer = live ? "순서 " + (idx + 1) + " / " + Total : "";
            _nextCard.Status = live ? StatusKind.Run : StatusKind.Stop;

            // 현재 포인트(NG 재체결 대기면 그 포인트)의 직전 값. 없으면 값 비움.
            PointResult shown = live && point != 0 ? (Latest(point) ?? last) : last;
            _torqueCard.Title = shown == null ? "토크" : shown.Point + "번 토크";
            if (shown == null)
            {
                _torqueCard.Value = ""; _torqueCard.Footer = ""; _torqueCard.Status = StatusKind.Stop;
                return;
            }
            _torqueCard.Value = TorqueRules.Torque(shown.Torque);
            _torqueCard.Footer = "각도 " + shown.Angle + "° · " + shown.At.ToString("HH:mm:ss");
            _torqueCard.Status = shown.Ok ? StatusKind.Run : StatusKind.Alarm;
        }

        private void ShowPrimary(Phase phase, bool manual, bool measuring)
        {
            _torqueCaption.Visible = _torqueInput.Visible = _angleCaption.Visible = _angleInput.Visible = manual;
            _torqueInput.Enabled = _angleInput.Enabled = measuring;
            _mode.Enabled = !Working || measuring;

            string text = "작업 시작", why = "";
            bool enabled = false;
            switch (phase)
            {
                case Phase.Idle:
                    why = !TorqueRules.IsSerial(_serialCode) && !TorqueRules.IsBadge(_badgeCode) ? "시리얼 · 사원증 스캔 필요"
                        : !TorqueRules.IsSerial(_serialCode) ? "시리얼 스캔 필요" : "사원증 스캔 필요";
                    break;
                case Phase.Ready: enabled = true; break;
                case Phase.Running:
                case Phase.Retighten:
                    text = manual ? "체결값 입력" : "공구값 수신";
                    enabled = !manual || (_torqueInput.HasValue && _angleInput.HasValue);
                    why = enabled ? "" : "토크 · 각도 입력 필요";
                    break;
                case Phase.Blocked: text = "재체결 승인"; enabled = true; break;
                case Phase.Complete: why = "완료 · 다음 부품 시리얼 스캔"; break;
                case Phase.Scrapped: why = "불량 처리됨 · 다음 부품 시리얼 스캔"; break;
            }
            _primary.Text = text;
            _primary.Enabled = enabled;
            _reason.Text = why;
        }

        private void ShowGrid(Phase phase, int point)
        {
            _grid.Rows.Clear();
            foreach (int p in TorqueRules.Sequence)
            {
                PointResult r = Latest(p);
                StatusKind kind; string word, torque = "", angle = "";
                bool current = p == point && phase != Phase.Idle && phase != Phase.Ready;
                if (r != null)
                {
                    torque = TorqueRules.Torque(r.Torque);
                    angle = r.Angle.ToString();
                }
                if (r != null && r.Ok) { kind = StatusKind.Run; word = "OK"; }
                else if (r != null && phase == Phase.Retighten && current) { kind = StatusKind.Warn; word = "재체결"; }
                else if (r != null) { kind = StatusKind.Alarm; word = "NG · " + r.FailReason; }
                else if (current) { kind = StatusKind.Warn; word = "다음"; }
                else { kind = StatusKind.Stop; word = "대기"; }
                int row = _grid.Rows.Add(p.ToString(), torque, angle, new ApcLampValue(kind, word));
                if (r != null && !r.Ok)
                {
                    if (r.Torque < TorqueRules.TorqueMin || r.Torque > TorqueRules.TorqueMax) MarkNg(row, 1);
                    if (r.Angle < TorqueRules.AngleMin || r.Angle > TorqueRules.AngleMax) MarkNg(row, 2);
                }
                _diagram.SetPoint(p, kind, word == "재체결" ? "재체결" : (r != null && !r.Ok ? "NG" : word));
            }
        }

        private void MarkNg(int row, int column)
        {
            var style = _grid.Rows[row].Cells[column].Style;
            style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            style.Font = ApcTheme.GetFont(FontStyle.Bold);
        }
    }
}
