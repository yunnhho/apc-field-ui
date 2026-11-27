using System;
using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TorqueSequence.Data;

namespace TorqueSequence.Views
{
    /// <summary>체결 순서 화면. 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private enum Step { NeedScan, Tighten, RetryApproval, Scrap, Done }

        private readonly IMainViewSource _source;
        private readonly List<BoltAttempt> _attempts = new List<BoltAttempt>();
        private string _serialValue = "", _badgeValue = "";
        private bool _retryApproved;
        private string _blockText = "";

        private readonly ApcScanBox _serial = new ApcScanBox { ScanOnly = true, Width = S(240) };
        private readonly ApcScanBox _badge = new ApcScanBox { ScanOnly = true, Width = S(170) };
        private readonly ApcComboBox _mode = new ApcComboBox { Width = S(100) };
        private readonly ApcComboBox _pointIn = new ApcComboBox { Width = S(80) };
        private readonly ApcNumericBox _torqueIn = new ApcNumericBox { Width = S(110), DecimalPlaces = 1, Minimum = 0, Maximum = 99, Increment = 0.1m };
        private readonly ApcNumericBox _angleIn = new ApcNumericBox { Width = S(100), DecimalPlaces = 0, Minimum = 0, Maximum = 999, Increment = 1 };
        private readonly List<Control> _manualControls = new List<Control>();

        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly Label _reason = Caption("");
        private readonly ApcButton _action = Button("체결값 판정", ApcButtonVariant.Primary, 190);
        private readonly ApcValueCard _torque = Card("토크", "N·m");
        private readonly ApcValueCard _angle = Card("각도", "°");
        private readonly BoltBoard _board = new BoltBoard { Dock = DockStyle.Fill };
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, FillRows = true };

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("체결 순서 · 실린더 헤드 커버")
        {
            _source = source;
            _serialValue = source.GetCurrentSerial();
            _badgeValue = source.GetCurrentBadge();
            _serial.Text = _serialValue;
            _badge.Text = _badgeValue;
            _attempts.AddRange(source.GetAttempts());
            _serial.Accepts = c => c.StartsWith("HC-");
            _badge.Accepts = c => c.StartsWith("EMP-");

            _mode.Items.AddRange(new object[] { "자동", "수동" });
            _mode.SelectedIndex = 0;
            for (int p = 1; p <= TorqueRules.Sequence.Length; p++) _pointIn.Items.Add(p.ToString());
            _torqueIn.Clear();
            _angleIn.Clear();

            _grid.Columns.Add("point", "번호");
            _grid.Columns.Add("torque", "토크(N·m)");
            _grid.Columns.Add("angle", "각도(°)");
            _grid.Columns.Add("time", "시각");
            _grid.Columns.Add(new ApcLampColumn { HeaderText = "판정" });
            _grid.Columns.Add("reason", "사유");
            foreach (DataGridViewColumn c in _grid.Columns)
            {
                if (c is ApcLampColumn) continue;
                c.AutoSizeMode = c.Name == "reason" ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.AllCells;
                c.DefaultCellStyle.Alignment = c.Name == "reason" ? DataGridViewContentAlignment.MiddleLeft : DataGridViewContentAlignment.MiddleCenter;
            }
            foreach (int p in TorqueRules.Sequence) _grid.Rows.Add(p.ToString(), "", "", "", new ApcLampValue(StatusKind.Stop, "대기"), "");

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "OK"), new ApcLampValue(StatusKind.Alarm, "NG"),
                new ApcLampValue(StatusKind.Warn, "다음"), new ApcLampValue(StatusKind.Stop, "대기"));

            _reason.AutoSize = false;
            _reason.Dock = DockStyle.Fill;
            _reason.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            _action.Dock = DockStyle.Right;
            var actionRow = new Panel { Dock = DockStyle.Bottom, Height = _action.Height };
            actionRow.Controls.Add(_reason);
            actionRow.Controls.Add(_action);
            var left = new Panel();
            left.Controls.Add(_judge);
            left.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = S(12) });
            left.Controls.Add(actionRow);

            Body.Controls.Add(FillColumns(new Control[] { Titled("체결 포인트", _board, legend), Titled("포인트별 결과", _grid) }, new[] { 52f, 48f }));
            Body.Controls.Add(Columns(S(190) + S(12) + _action.Height, new Control[] { left, _torque, _angle }, new[] { 50f, 25f, 25f }));
            var scanRow = Row(Caption("부품 시리얼"), _serial, Caption("사원증"), _badge, Caption("모드"), _mode,
                Manual(Caption("포인트")), Manual(_pointIn), Manual(Caption("토크 (N·m)")), Manual(_torqueIn), Manual(Caption("각도 (°)")), Manual(_angleIn));
            Body.Controls.Add(scanRow);

            Refresh_();
            _mode.SelectedIndexChanged += (s, e) => Refresh_();
            _torqueIn.ValueChanged += (s, e) => Refresh_();
            _angleIn.ValueChanged += (s, e) => Refresh_();
            _serial.Scanned += OnSerialScanned;
            _badge.Scanned += OnBadgeScanned;
            _action.Click += (s, e) => OnAction();
        }

        private Control Manual(Control c) { _manualControls.Add(c); return c; }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form == null) return;
            _serial.Attach(form);
            _badge.Attach(form);
        }

        // ---- 상태 계산 ----

        private bool IsManual { get { return _mode.SelectedIndex == 1; } }

        private BoltAttempt Last(int point)
        {
            for (int i = _attempts.Count - 1; i >= 0; i--) if (_attempts[i].Point == point) return _attempts[i];
            return null;
        }

        private int NgCount(int point)
        {
            int n = 0;
            foreach (BoltAttempt a in _attempts) if (a.Point == point && !a.Ok) n++;
            return n;
        }

        /// <summary>순서상 아직 OK가 아닌 첫 포인트. 모두 OK면 0.</summary>
        private int NextPoint()
        {
            foreach (int p in TorqueRules.Sequence)
            {
                BoltAttempt a = Last(p);
                if (a == null || !a.Ok) return p;
            }
            return 0;
        }

        private Step CurrentStep()
        {
            if (_serialValue.Length == 0 || _badgeValue.Length == 0) return Step.NeedScan;
            int next = NextPoint();
            if (next == 0) return Step.Done;
            BoltAttempt last = Last(next);
            if (last == null || _retryApproved) return Step.Tighten;
            return NgCount(next) >= ReapproveDialog.MaxNgPerPoint ? Step.Scrap : Step.RetryApproval;
        }

        // ---- 화면 갱신 ----

        private void Refresh_()
        {
            Step step = CurrentStep();
            int next = NextPoint();
            ShowJudge(step, next);
            ShowCards();
            ShowBoardAndGrid(step, next);
            ShowAction(step, next);
        }

        private void ShowJudge(Step step, int next)
        {
            string limit = "토크 " + TorqueRules.TorqueMin.ToString("0.0") + "~" + TorqueRules.TorqueMax.ToString("0.0") + " N·m · 각도 " + TorqueRules.AngleMin + "~" + TorqueRules.AngleMax + "°";
            switch (step)
            {
                case Step.NeedScan:
                    _judge.Text = "READY"; _judge.Status = StatusKind.Stop;
                    _judge.SubText = _serialValue.Length == 0 && _badgeValue.Length == 0 ? "시리얼과 사원증을 스캔하세요" : _serialValue.Length == 0 ? "부품 시리얼을 스캔하세요" : "사원증을 스캔하세요";
                    break;
                case Step.Done:
                    _judge.Text = "완료 OK"; _judge.Status = StatusKind.Run;
                    _judge.SubText = _serialValue + " · 10개 모두 OK";
                    break;
                case Step.Tighten:
                    _judge.Text = next + "번 체결"; _judge.Status = StatusKind.Run;
                    _judge.SubText = "순서 " + (Array.IndexOf(TorqueRules.Sequence, next) + 1) + "/10 · " + limit;
                    break;
                default:
                    _judge.Text = "NG"; _judge.Status = StatusKind.Alarm;
                    _judge.SubText = next + "번 · " + TorqueRules.Check(Last(next).Torque, Last(next).Angle).Detail;
                    break;
            }
        }

        private void ShowCards()
        {
            int next = NextPoint();
            BoltAttempt a = LastTightened();
            _torque.Title = a == null ? "토크" : a.Point + "번 토크";
            _angle.Title = a == null ? "각도" : a.Point + "번 각도";
            if (a == null)
            {
                _torque.Value = ""; _torque.Footer = ""; _torque.Status = StatusKind.Stop;
                _angle.Value = ""; _angle.Footer = ""; _angle.Status = StatusKind.Stop;
                return;
            }
            decimal t = TorqueRules.RoundTorque(a.Torque);
            bool torqueOk = t >= TorqueRules.TorqueMin && t <= TorqueRules.TorqueMax;
            bool angleOk = a.Angle >= TorqueRules.AngleMin && a.Angle <= TorqueRules.AngleMax;
            _torque.Value = t.ToString("0.0");
            _torque.Status = torqueOk ? StatusKind.Run : StatusKind.Alarm;
            _torque.Footer = "체결 " + a.Time.ToString("HH:mm:ss");
            _angle.Value = a.Angle.ToString();
            _angle.Status = angleOk ? StatusKind.Run : StatusKind.Alarm;
            _angle.Footer = "체결 " + a.Time.ToString("HH:mm:ss");
        }

        private BoltAttempt LastTightened() { return _attempts.Count == 0 ? null : _attempts[_attempts.Count - 1]; }

        private void ShowBoardAndGrid(Step step, int next)
        {
            for (int i = 0; i < TorqueRules.Sequence.Length; i++)
            {
                int p = TorqueRules.Sequence[i];
                BoltAttempt a = Last(p);
                StatusKind kind = a != null ? (a.Ok ? StatusKind.Run : StatusKind.Alarm) : p == next ? StatusKind.Warn : StatusKind.Stop;
                bool ready = step != Step.NeedScan;
                if (a == null && p == next && !ready) kind = StatusKind.Stop;
                _board.SetBolt(p, kind);

                DataGridViewCellCollection cells = _grid.Rows[i].Cells;
                string word = a != null ? (a.Ok ? "OK" : "NG") : (p == next && ready ? "다음" : "대기");
                cells[4].Value = new ApcLampValue(kind, word);
                if (a == null)
                {
                    cells[1].Value = ""; cells[2].Value = ""; cells[3].Value = ""; cells[5].Value = "";
                    continue;
                }
                Verdict v = TorqueRules.Check(a.Torque, a.Angle);
                decimal t = TorqueRules.RoundTorque(a.Torque);
                cells[1].Value = t.ToString("0.0");
                cells[2].Value = a.Angle.ToString();
                cells[3].Value = a.Time.ToString("HH:mm:ss");
                cells[5].Value = v.Short;
                MarkOut(cells[1], t < TorqueRules.TorqueMin || t > TorqueRules.TorqueMax);
                MarkOut(cells[2], a.Angle < TorqueRules.AngleMin || a.Angle > TorqueRules.AngleMax);
            }
            _board.SetEmphasis(step == Step.NeedScan ? 0 : next);
        }

        private static void MarkOut(DataGridViewCell cell, bool outOfSpec)
        {
            cell.Style.ForeColor = outOfSpec ? ApcTheme.StatusText(StatusKind.Alarm) : System.Drawing.Color.Empty;
            cell.Style.Font = outOfSpec ? ApcTheme.GetFont(System.Drawing.FontStyle.Bold) : null;
        }

        private void ShowAction(Step step, int next)
        {
            foreach (Control c in _manualControls) c.Visible = IsManual;
            _action.Variant = ApcButtonVariant.Primary;
            _action.Enabled = true;
            _reason.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text);
            switch (step)
            {
                case Step.NeedScan:
                    _action.Text = "체결값 판정"; _action.Enabled = false;
                    _reason.Text = "스캔 전에는 체결 불가";
                    break;
                case Step.Done:
                    _action.Text = "새 부품 시작"; _reason.Text = "";
                    break;
                case Step.RetryApproval:
                    _action.Text = "재체결 승인"; _reason.Text = "NG · 반장 승인 전 체결 불가";
                    break;
                case Step.Scrap:
                    _action.Text = "불량 처리"; _action.Variant = ApcButtonVariant.Danger;
                    _reason.Text = "같은 포인트 NG 2회째 · 재체결 불가";
                    break;
                default:
                    _action.Text = "체결값 판정";
                    if (IsManual && (!_torqueIn.HasValue || !_angleIn.HasValue))
                    {
                        _action.Enabled = false;
                        _reason.Text = !_torqueIn.HasValue ? "토크 입력 필요" : "각도 입력 필요";
                    }
                    else _reason.Text = _blockText;
                    if (_blockText.Length > 0 && _reason.Text == _blockText) _reason.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                    break;
            }
            if (step == Step.Tighten && IsManual && _pointIn.SelectedIndex < 0 && next > 0) _pointIn.SelectedIndex = next - 1;
        }

        // ---- 동작 ----

        private void OnAction()
        {
            switch (CurrentStep())
            {
                case Step.Done: Reset(); break;
                case Step.RetryApproval:
                case Step.Scrap: OpenReapprove(); break;
                case Step.Tighten: Tighten(); break;
            }
        }

        private void Tighten()
        {
            ToolReading r = ReadValue();
            if (r == null)
            {
                _blockText = "공구에서 받은 값 없음";
                Refresh_();
                return;
            }
            int expected = NextPoint();
            if (r.Point != expected)
            {
                _blockText = "순서 오류 · " + expected + "번 차례 (" + r.Point + "번 값 수신)";
                Refresh_();
                WarnAfter(_blockText + " — 값을 버렸습니다");
                return;
            }
            _blockText = "";
            _attempts.Add(new BoltAttempt(r.Point, r.Torque, r.Angle, DateTime.Now));
            _retryApproved = false;
            if (IsManual) { _torqueIn.Clear(); _angleIn.Clear(); int n = NextPoint(); if (n > 0) _pointIn.SelectedIndex = n - 1; }
            Refresh_();
        }

        private ToolReading ReadValue()
        {
            if (!IsManual) return _source.ReadTool();
            return new ToolReading(_pointIn.SelectedIndex + 1, _torqueIn.Value, (int)_angleIn.Value);
        }

        private void OpenReapprove()
        {
            int p = NextPoint();
            BoltAttempt a = Last(p);
            using (var dlg = new ReapproveDialog(p, TorqueRules.RoundTorque(a.Torque), a.Angle, a.Time, NgCount(p)))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (dlg.Choice == ReapproveDialog.Outcome.Retry) { _retryApproved = true; _blockText = ""; Refresh_(); return; }
                if (dlg.Choice == ReapproveDialog.Outcome.Scrap)
                {
                    string serial = _serialValue;
                    Reset();
                    ApcDialog.Alert(FindForm(), serial + " 부품 불량 처리됨 · 새 부품을 스캔하세요");
                }
            }
        }

        private void Reset()
        {
            _attempts.Clear();
            _serialValue = ""; _badgeValue = "";
            _serial.Text = ""; _badge.Text = "";
            _retryApproved = false; _blockText = "";
            _torqueIn.Clear(); _angleIn.Clear();
            _source.ResetTool();
            Refresh_();
        }

        private void OnSerialScanned(object sender, ScanEventArgs e)
        {
            if (!TorqueRules.IsSerial(e.Code)) { e.Accepted = false; WarnAfter("부품 시리얼 형식이 아닙니다 (HC-yyMMdd-nnnn)"); return; }
            if (_attempts.Count > 0 && NextPoint() != 0) { e.Accepted = false; WarnAfter("체결 중인 부품이 있습니다 · 먼저 완료하거나 불량 처리하세요"); return; }
            if (_attempts.Count > 0) { _attempts.Clear(); _source.ResetTool(); }
            _serialValue = e.Code;
            Refresh_();
        }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            if (!TorqueRules.IsBadge(e.Code)) { e.Accepted = false; WarnAfter("사원증 형식이 아닙니다 (EMP-nnnnn)"); return; }
            _badgeValue = e.Code;
            Refresh_();
        }

        private void WarnAfter(string message)
        {
            BeginInvoke(new Action(() => ApcDialog.Warn(FindForm(), message)));
        }

        private static ApcValueCard Card(string title, string unit)
        {
            return new ApcValueCard { Title = title, Unit = unit, Dock = DockStyle.Fill };
        }
    }
}
