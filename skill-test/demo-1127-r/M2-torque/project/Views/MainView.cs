using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TorqueSequence.Data;

namespace TorqueSequence.Views
{
    /// <summary>
    /// 체결 순서 메인 화면. 위: 판정판(지금 해야 할 일 / 막힌 이유) + 현재 토크, 스캔·모드 줄, (수동일 때) 측정값 입력 줄,
    /// 아래: 부품 윤곽 도식 + 포인트별 결과 표. 막는 규칙은 TighteningSession 한 곳에 있다.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int ModeAuto = 0, ModeManual = 1;

        private readonly IMainViewSource _source;
        private TighteningSession _session;
        private int _lastPoint;   // 현재 토크 카드가 보여 주는 포인트(0 = 없음)

        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcValueCard _torque = new ApcValueCard { Title = "현재 토크", Unit = "N·m", Dock = DockStyle.Fill };
        private readonly ApcScanBox _serial = new ApcScanBox { Width = S(300) };
        private readonly ApcScanBox _badge = new ApcScanBox { Width = S(240) };
        private readonly ApcTabStrip _mode = new ApcTabStrip();
        private readonly Label _manualCaption = Caption("");
        private readonly ApcNumericBox _manualTorque = new ApcNumericBox { Minimum = 0m, Maximum = 99.9m, DecimalPlaces = 1, Increment = 0.1m, Width = S(130) };
        private readonly ApcNumericBox _manualAngle = new ApcNumericBox { Minimum = 0m, Maximum = 360m, DecimalPlaces = 0, Increment = 1m, Width = S(120) };
        private readonly ApcButton _manualEnter = Button("체결값 입력", ApcButtonVariant.Primary, 150);
        private readonly Label _manualReason = Caption("");
        private readonly FlowLayoutPanel _manualRow;
        private readonly BoltDiagram _diagram = new BoltDiagram { Dock = DockStyle.Fill };
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, FillRows = true };
        private readonly Label _retightenReason = Caption("");
        private readonly ApcButton _retighten = Button("재체결 승인", ApcButtonVariant.Normal, 140);

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("체결 순서 · 실린더 헤드 커버")
        {
            _source = source;
            _session = source.GetSession();
            _lastPoint = LastTightened();

            _mode.SetTabs("자동 · 체결 공구", "수동 · 토크렌치");
            _mode.Width = _mode.PreferredTabsWidth;
            _mode.SelectedIndexChanged += (s, e) => { _manualRow.Visible = _mode.SelectedIndex == ModeManual; Reload(); };

            _serial.Accepts = TorqueRules.IsPartSerial;
            _badge.Accepts = TorqueRules.IsBadge;
            _serial.Scanned += OnSerialScanned;
            _badge.Scanned += OnBadgeScanned;
            _serial.Text = _session.Serial ?? "";
            _badge.Text = _session.Badge ?? "";

            _manualTorque.Clear();
            _manualAngle.Clear();
            _manualTorque.ValueChanged += (s, e) => Reload();
            _manualAngle.ValueChanged += (s, e) => Reload();
            _manualEnter.Click += (s, e) => OnManualEnter();
            _manualRow = Row(_manualCaption, Caption("토크 (N·m)"), _manualTorque, Caption("각도 (°)"), _manualAngle, _manualEnter, _manualReason);
            _manualRow.Visible = false;

            _grid.Columns.Add("no", "번호");
            _grid.Columns.Add("torque", "토크(N·m)");
            _grid.Columns.Add("angle", "각도(°)");
            _grid.Columns.Add(new ApcLampColumn { HeaderText = "판정" });
            _grid.Columns.Add("time", "시각");
            foreach (DataGridViewColumn c in _grid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            _retighten.Click += (s, e) => OnRetighten();

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "OK"), new ApcLampValue(StatusKind.Alarm, "NG"),
                new ApcLampValue(StatusKind.Warn, "다음"), new ApcLampValue(StatusKind.Stop, "대기"));
            var diagramCell = new Panel { BackColor = Color.Transparent };
            diagramCell.Controls.Add(_diagram);
            var rule = Caption("체결 순서 " + string.Join("-", Array.ConvertAll(TorqueRules.Sequence, n => n.ToString()))
                + "  ·  기준 토크 " + TorqueRules.TorqueMin.ToString("0.0") + " ~ " + TorqueRules.TorqueMax.ToString("0.0")
                + " N·m  ·  각도 " + TorqueRules.AngleMin + " ~ " + TorqueRules.AngleMax + "°");
            rule.Dock = DockStyle.Bottom;
            diagramCell.Controls.Add(rule);

            Body.Controls.Add(FillColumns(new Control[] {
                Titled("체결 포인트 · 실린더 헤드 커버", diagramCell, legend),
                Titled("포인트별 결과", _grid, _retightenReason, _retighten) }, new[] { 58f, 42f }));
            Body.Controls.Add(_manualRow);
            Body.Controls.Add(Row(Caption("부품 시리얼"), _serial, Caption("사원증"), _badge, _mode));
            Body.Controls.Add(Columns(S(150), new Control[] { _judge, _torque }, new[] { 72f, 28f }));
            Reload();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form == null) return;
            _serial.Attach(form);
            _badge.Attach(form);
            _source.ToolValueReceived -= OnToolValue;
            _source.ToolValueReceived += OnToolValue;
        }

        // ---- 입력 ----

        private void OnSerialScanned(object sender, ScanEventArgs e)
        {
            if (!TorqueRules.IsPartSerial(e.Code)) { Reject(e, "부품 시리얼 형식이 아닙니다 · " + e.Code); return; }
            if (!_session.CanChangeSerial) { Reject(e, "체결 중인 부품이 있습니다 · 체결 완료 또는 불량 처리 후 스캔하세요"); return; }
            _session = new TighteningSession { Serial = e.Code, Badge = _session.Badge };
            _lastPoint = 0;
            if (_badge.Text.Length == 0) _badge.Focus();
            Reload();
        }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            if (!TorqueRules.IsBadge(e.Code)) { Reject(e, "사원증 형식이 아닙니다 · " + e.Code); return; }
            _session.Badge = e.Code;
            if (_serial.Text.Length == 0) _serial.Focus();
            Reload();
        }

        private void Reject(ScanEventArgs e, string message)
        {
            e.Accepted = false;
            WarnAfterScan(message);
        }

        private void OnToolValue(object sender, ToolValueEventArgs e)
        {
            if (_mode.SelectedIndex != ModeAuto) return;   // 수동 모드에서는 공구값을 받지 않는다
            Submit(e.Point, e.Torque, e.Angle);
        }

        private void OnManualEnter()
        {
            _manualTorque.Commit();
            _manualAngle.Commit();
            if (!_manualTorque.HasValue || !_manualAngle.HasValue) return;
            if (Submit(_session.NextPoint, _manualTorque.Value, (int)_manualAngle.Value))
            {
                _manualTorque.Clear();
                _manualAngle.Clear();
                Reload();
            }
        }

        /// <summary>체결값을 세션에 넣는다. 막히면 이유를 경고창으로 알리고 false.</summary>
        private bool Submit(int point, decimal torque, int angle)
        {
            string reason;
            SubmitResult r = _session.Submit(point, torque, angle, DateTime.Now);
            switch (r)
            {
                case SubmitResult.Ok:
                case SubmitResult.Ng:
                    _lastPoint = point;
                    Reload();
                    return true;
                case SubmitResult.OutOfOrder:
                    WarnAfterScan("체결 순서가 아닙니다 · " + point + "번 체결값이 왔지만 다음은 " + _session.NextPoint + "번입니다");
                    return false;
                default:
                    _session.CanTighten(out reason);
                    WarnAfterScan("체결값을 받지 않았습니다 · " + reason);
                    return false;
            }
        }

        private void OnRetighten()
        {
            int p = _session.PendingNg;
            if (p == 0) return;
            PointRecord rec = _session.Point(p);
            using (var dlg = new RetightenDialog(p, rec.Torque.Value, rec.Angle.Value, rec.Time.Value, rec.NgCount, _source.GetRetightenReasons()))
            {
                DialogResult result = dlg.ShowDialog(FindForm());
                if (result == DialogResult.OK)
                {
                    // 실제로는 dlg.ForemanBadge·dlg.Reason을 데이터 인터페이스로 기록한다.
                    if (_session.Approve()) _lastPoint = 0;
                }
                else if (result == DialogResult.Abort
                    && ApcDialog.Confirm(FindForm(), "이 부품을 불량 처리합니다. 되돌릴 수 없습니다."))
                    _session.Scrap();
            }
            Reload();
        }

        // 스캔·공구 이벤트 처리 중에는 경고창을 이벤트가 끝난 뒤에 띄운다
        private void WarnAfterScan(string message)
        {
            BeginInvoke((Action)(() => ApcDialog.Warn(FindForm(), message)));
        }

        // ---- 표시 ----

        private int LastTightened()
        {
            int last = 0;
            DateTime best = DateTime.MinValue;
            for (int p = 1; p <= 10; p++)
            {
                PointRecord r = _session.Point(p);
                if (r.Time.HasValue && r.Time.Value >= best) { best = r.Time.Value; last = p; }
            }
            return last;
        }

        private void Reload()
        {
            ShowJudge();
            ShowTorque();
            ShowGridAndDiagram();
            ShowManualState();
            _retighten.Enabled = _session.PendingNg != 0;
            _retightenReason.Text = _retighten.Enabled ? "" : "NG 포인트 없음";
        }

        private void ShowJudge()
        {
            string reason;
            int next = _session.NextPoint, ng = _session.PendingNg;
            bool manual = _mode.SelectedIndex == ModeManual;
            if (_session.Scrapped)
                Judge("불량 처리", StatusKind.Alarm, _session.Serial + " · 체결 중지");
            else if (!_session.Ready)
                Judge("READY", StatusKind.Stop, _session.Serial == null ? "부품 시리얼 스캔 필요" : "작업자 사원증 스캔 필요");
            else if (_session.Complete)
                Judge("완료", StatusKind.Run, "10개 모두 OK · " + _session.Serial);
            else if (_session.NeedsScrap)
                Judge("체결 중지", StatusKind.Alarm, ng + "번 NG 2회째 · 부품 불량 처리 필요");
            else if (ng != 0)
                Judge("NG", StatusKind.Alarm, ng + "번 NG · " + TorqueRules.NgReason(_session.Point(ng).Torque.Value, _session.Point(ng).Angle.Value));
            else
            {
                _session.CanTighten(out reason);
                bool again = _session.Point(next).NgCount > 0;
                Judge(next + (again ? "번 재체결" : "번 체결"), StatusKind.Run, manual ? "토크렌치 측정값을 입력하세요" : "체결 공구로 체결하세요");
            }
        }

        private void Judge(string text, StatusKind status, string sub)
        {
            _judge.Text = text;
            _judge.Status = status;
            _judge.SubText = sub;
        }

        private void ShowTorque()
        {
            if (_lastPoint == 0) { _torque.Value = ""; _torque.Footer = ""; _torque.Status = StatusKind.Stop; return; }
            PointRecord r = _session.Point(_lastPoint);
            if (!r.Ok.HasValue) { _torque.Value = ""; _torque.Footer = ""; _torque.Status = StatusKind.Stop; return; }
            _torque.Value = r.Torque.Value.ToString("0.0");
            _torque.Footer = _lastPoint + "번 · 각도 " + r.Angle.Value + "°";
            _torque.Status = r.Ok == true ? StatusKind.Run : StatusKind.Alarm;
        }

        private void ShowGridAndDiagram()
        {
            int next = _session.NextPoint;
            _grid.Rows.Clear();
            foreach (int p in TorqueRules.Sequence)
            {
                PointRecord r = _session.Point(p);
                StatusKind kind; string word;
                if (r.Ok == true) { kind = StatusKind.Run; word = "OK"; }
                else if (r.Ok == false && p != next) { kind = StatusKind.Alarm; word = "NG"; }
                else if (r.Ok == false && _session.Approved) { kind = StatusKind.Warn; word = "재체결"; }
                else if (r.Ok == false) { kind = StatusKind.Alarm; word = "NG"; }
                else if (p == next) { kind = StatusKind.Warn; word = "다음"; }
                else { kind = StatusKind.Stop; word = "대기"; }

                bool tBad = r.Torque.HasValue && (r.Torque.Value > TorqueRules.TorqueMax || r.Torque.Value < TorqueRules.TorqueMin);
                bool aBad = r.Angle.HasValue && (r.Angle.Value > TorqueRules.AngleMax || r.Angle.Value < TorqueRules.AngleMin);
                int row = _grid.Rows.Add(p.ToString(), r.Torque.HasValue ? r.Torque.Value.ToString("0.0") : "",
                    r.Angle.HasValue ? r.Angle.Value.ToString() : "", new ApcLampValue(kind, word),
                    r.Time.HasValue ? r.Time.Value.ToString("HH:mm:ss") : "");
                if (tBad) Emphasize(_grid.Rows[row].Cells[1]);
                if (aBad) Emphasize(_grid.Rows[row].Cells[2]);

                _diagram.SetBolt(p, kind, r.Ok.HasValue || p == next ? word : "", p == next);
            }
            _grid.ClearSelection();
        }

        private static void Emphasize(DataGridViewCell cell)
        {
            cell.Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
        }

        private void ShowManualState()
        {
            string reason;
            int next = _session.NextPoint;
            _manualCaption.Text = next == 0 ? "" : next + "번 측정값";
            bool can = _session.CanTighten(out reason);
            bool filled = _manualTorque.HasValue && _manualAngle.HasValue;
            _manualTorque.Enabled = _manualAngle.Enabled = can;
            _manualEnter.Enabled = can && filled;
            _manualReason.Text = !can ? reason : filled ? "" : "토크·각도 입력 필요";
        }
    }
}
