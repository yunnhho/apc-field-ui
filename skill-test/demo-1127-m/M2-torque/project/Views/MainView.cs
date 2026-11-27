using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TorqueSequence.Data;

namespace TorqueSequence.Views
{
    /// <summary>체결 순서 메인 화면. 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private readonly IMainViewSource _source;
        private readonly Dictionary<int, PointResult> _results = new Dictionary<int, PointResult>();
        private string _serial = "", _employee = "";
        private bool _approved;   // 반장이 현재 NG 포인트의 재체결을 승인했다

        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcValueCard _torqueCard = Card("토크");
        private readonly ApcValueCard _angleCard = Card("각도");
        private readonly ApcScanBox _serialScan = new ApcScanBox { Width = S(260) };
        private readonly ApcScanBox _employeeScan = new ApcScanBox { Width = S(220) };
        private readonly ApcButton _next = Button("다음 부품", ApcButtonVariant.Normal, 130);
        private readonly Label _nextReason = Caption("");
        private readonly ApcTabStrip _mode = new ApcTabStrip();
        private readonly Label _autoNote = Caption("체결 공구 통신값을 자동으로 받습니다");
        private readonly Label _torqueLabel = Caption("토크 (N·m)");
        private readonly Label _angleLabel = Caption("각도 (°)");
        private readonly ApcNumericBox _torqueInput = new ApcNumericBox { Minimum = 0, Maximum = 30, DecimalPlaces = 1, Increment = 0.1m, Width = S(130) };
        private readonly ApcNumericBox _angleInput = new ApcNumericBox { Minimum = 0, Maximum = 360, DecimalPlaces = 0, Increment = 1, Width = S(110) };
        private readonly ApcButton _apply = Button("체결값 입력", ApcButtonVariant.Primary, 140);
        private readonly Label _applyReason = Caption("");
        private readonly Panel _part = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
        private readonly Label[] _bolts = new Label[TorqueRules.Sequence.Length + 1];   // 1..10
        private readonly Label _partCaption = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcButton _retighten = Button("재체결 승인", ApcButtonVariant.Normal, 140);
        private readonly Label _retightenReason = Caption("");

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("실린더 헤드 커버 체결")
        {
            _source = source;
            BuildGrid();
            BuildPart();

            _mode.SetTabs("자동 · 공구 통신", "수동 · 토크렌치");
            _mode.Width = _mode.PreferredTabsWidth;
            _mode.SelectedIndexChanged += (s, e) => Refresh2();
            _serialScan.Scanned += OnSerialScanned;
            _employeeScan.Scanned += OnEmployeeScanned;
            _next.Click += (s, e) => NextPart();
            _apply.Click += (s, e) => Apply(Current(), (double)_torqueInput.Value, (int)_angleInput.Value);
            _retighten.Click += (s, e) => Retighten();
            _source.ReadingReceived += (s, e) => { if (IsHandleCreated) BeginInvoke(new Action(() => Apply(e.Point, e.Torque, e.Angle))); };

            var session = _source.GetSession();
            _serial = session.Serial; _employee = session.Employee;
            _serialScan.Text = _serial; _employeeScan.Text = _employee;
            foreach (var r in session.Results) _results[r.Point] = r;
            _mode.SelectedIndex = session.Manual ? 1 : 0;

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Warn, "다음"), new ApcLampValue(StatusKind.Run, "OK"),
                new ApcLampValue(StatusKind.Alarm, "NG"), new ApcLampValue(StatusKind.Stop, "대기"));
            legend.Height = S(34);

            string limits = "기준 토크 " + TorqueRules.TorqueMin.ToString("0.0") + " ~ " + TorqueRules.TorqueMax.ToString("0.0") + " N·m · 각도 " + TorqueRules.AngleMin + " ~ " + TorqueRules.AngleMax + "°";
            var left = Titled("체결 포인트", _part, legend);
            var right = Titled("포인트별 결과 · " + limits, _grid, _retightenReason, _retighten);

            Body.Controls.Add(FillColumns(new Control[] { left, right }, new[] { 50f, 50f }));
            Body.Controls.Add(Row(Caption("체결 모드"), _mode, _autoNote, _torqueLabel, _torqueInput, _angleLabel, _angleInput, _apply, _applyReason));
            Body.Controls.Add(Row(Caption("부품 시리얼"), _serialScan, Caption("사원증"), _employeeScan, _next, _nextReason));
            Body.Controls.Add(Columns(S(170), new Control[] { _judge, _torqueCard, _angleCard }, new[] { 50f, 25f, 25f }));
            Refresh2();
            FitRows();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) { _serialScan.Attach(form); _employeeScan.Attach(form); }   // 먼저 Attach한 칸(시리얼)이 기본 수신
        }

        // ---------- 상태 ----------

        /// <summary>지금 체결할 포인트(끝났으면 0).</summary>
        private int Current()
        {
            foreach (int p in TorqueRules.Sequence)
            {
                PointResult r;
                if (!_results.TryGetValue(p, out r) || !r.IsOk) return p;
            }
            return 0;
        }

        private PointResult ResultOf(int point)
        {
            PointResult r;
            return _results.TryGetValue(point, out r) ? r : null;
        }

        private bool Scrapped(int cur)
        {
            var r = ResultOf(cur);
            return cur != 0 && r != null && !r.IsOk && r.NgCount >= TorqueRules.MaxNg;
        }

        private bool PendingNg(int cur)
        {
            var r = ResultOf(cur);
            return cur != 0 && r != null && !r.IsOk && !_approved;
        }

        /// <summary>체결값을 받을 수 없는 이유(받을 수 있으면 null). 판정판·버튼 옆 글·경고창이 같은 문장을 쓴다.</summary>
        private string BlockReason()
        {
            int cur = Current();
            if (_serial.Length == 0 || _employee.Length == 0) return "시리얼과 사원증을 모두 스캔해야 시작합니다";
            if (cur == 0) return "체결 완료 · 다음 부품을 시작하세요";
            if (Scrapped(cur)) return cur + "번 NG " + TorqueRules.MaxNg + "회 · 부품 불량 처리";
            if (PendingNg(cur)) return cur + "번 NG · 재체결 승인 필요";
            return null;
        }

        // ---------- 동작 ----------

        private bool Apply(int point, double torque, int angle)
        {
            string block = BlockReason();
            if (block != null) { WarnLater(block); return false; }
            int cur = Current();
            if (point != cur)
            {
                WarnLater("체결 순서 위반 · 지금은 " + cur + "번 차례입니다 (" + point + "번 값은 받지 않음)");
                return false;
            }
            var prev = ResultOf(point);
            int ng = prev == null ? 0 : prev.NgCount;
            bool ok = TorqueRules.Fault(TorqueRules.RoundTorque(torque), angle) == null;
            var r = new PointResult(point, torque, angle, DateTime.Now, ok ? ng : ng + 1);
            _results[point] = r;
            _approved = false;
            _source.SaveResult(_serial, _employee, r);
            Refresh2();
            return true;
        }

        private void Retighten()
        {
            int cur = Current();
            var r = ResultOf(cur);
            if (r == null || r.IsOk) return;
            using (var dlg = new RetightenDialog(cur, r.Torque, r.Angle, r.NgCount))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.SaveApproval(_serial, cur, dlg.Foreman, dlg.Reason);
                _approved = true;
                Refresh2();
            }
        }

        private void NextPart()
        {
            if (!ApcDialog.Confirm(FindForm(), "다음 부품을 시작합니다. 현재 부품 결과는 저장되어 있습니다.", "시작", ApcButtonVariant.Primary)) return;
            _results.Clear();
            _serial = ""; _employee = ""; _approved = false;
            _serialScan.Text = ""; _employeeScan.Text = "";
            Refresh2();
            _serialScan.Focus();
        }

        private void OnSerialScanned(object sender, ScanEventArgs e)
        {
            if (!AcceptScan(e, TorqueRules.IsSerial(e.Code), "부품 시리얼 형식이 아닙니다 (HC-yyMMdd-nnnn)")) return;
            _serial = e.Code;
            Refresh2();
            _employeeScan.Focus();
        }

        private void OnEmployeeScanned(object sender, ScanEventArgs e)
        {
            if (!AcceptScan(e, TorqueRules.IsEmployee(e.Code), "사원증 형식이 아닙니다 (EMP-nnnnn)")) return;
            _employee = e.Code;
            Refresh2();
        }

        // 체결이 시작된 부품은 스캔을 바꿀 수 없다.
        private bool AcceptScan(ScanEventArgs e, bool valid, string badFormat)
        {
            string why = _results.Count > 0 ? "체결 중에는 시리얼·사원증을 바꿀 수 없습니다" : !valid ? badFormat : null;
            if (why == null) return true;
            e.Accepted = false;
            WarnLater(why);
            return false;
        }

        private void WarnLater(string message)
        {
            BeginInvoke(new Action(() => ApcDialog.Warn(FindForm(), message)));
        }

        // ---------- 화면 갱신 ----------

        private void Refresh2()
        {
            int cur = Current();
            string block = BlockReason();
            bool manual = _mode.SelectedIndex == 1;

            ShowJudge(cur);
            ShowCards(cur);
            ShowGrid(cur);
            ShowBolts(cur);

            _autoNote.Visible = !manual;
            foreach (Control c in new Control[] { _torqueLabel, _torqueInput, _angleLabel, _angleInput, _apply, _applyReason }) c.Visible = manual;
            _apply.Enabled = block == null;
            _applyReason.Text = block ?? "";

            bool finished = _serial.Length > 0 && _employee.Length > 0 && (cur == 0 || Scrapped(cur));
            _next.Enabled = finished;
            _nextReason.Text = finished ? "" : "체결 완료 또는 불량 처리 후 사용";

            var r = ResultOf(cur);
            bool canRetighten = PendingNg(cur) || Scrapped(cur);
            _retighten.Enabled = canRetighten;
            _retightenReason.Text = canRetighten ? "" : "NG 포인트 없음";
        }

        private void ShowJudge(int cur)
        {
            var r = ResultOf(cur);
            if (_serial.Length == 0 || _employee.Length == 0)
            {
                Judge("READY", StatusKind.Stop, _serial.Length == 0 ? "부품 시리얼을 스캔하세요" : "사원증을 스캔하세요");
            }
            else if (cur == 0) Judge("OK", StatusKind.Run, _serial + " · 10개 체결 완료");
            else if (Scrapped(cur)) Judge("불량 처리", StatusKind.Alarm, cur + "번 NG " + TorqueRules.MaxNg + "회 · 재체결 불가");
            else if (PendingNg(cur)) Judge("NG · " + cur + "번", StatusKind.Alarm, FaultText(r) + " · 재체결 승인 필요");
            else if (r != null) Judge("재체결 · " + cur + "번", StatusKind.Warn, "승인 완료 · 다시 체결하세요");
            else Judge("다음 " + cur + "번", StatusKind.Run, "완료 " + CountOk() + "/" + TorqueRules.Sequence.Length + " · " + (_mode.SelectedIndex == 1 ? "측정값을 입력하세요" : "공구 체결 대기"));
        }

        private void Judge(string text, StatusKind status, string sub)
        {
            _judge.Text = text; _judge.Status = status; _judge.SubText = sub;
        }

        private static string FaultText(PointResult r)
        {
            string fault = r.Fault;   // 토크 사유가 있으면 토크, 아니면 각도
            return fault.StartsWith("토크", StringComparison.Ordinal)
                ? "토크 " + r.Torque.ToString("0.0") + " N·m " + fault.Substring(3)
                : "각도 " + r.Angle + "° " + fault.Substring(3);
        }

        private int CountOk()
        {
            int n = 0;
            foreach (var r in _results.Values) if (r.IsOk) n++;
            return n;
        }

        private void ShowCards(int cur)
        {
            // 끝났으면 마지막 포인트, 아니면 지금 포인트. 재체결 승인 뒤에는 이전 값을 비운다.
            int shown = cur == 0 ? TorqueRules.Sequence[TorqueRules.Sequence.Length - 1] : cur;
            var r = _approved && cur != 0 ? null : ResultOf(shown);
            _torqueCard.Title = shown + "번 토크";
            _angleCard.Title = shown + "번 각도";
            _torqueCard.Value = r == null ? "" : r.Torque.ToString("0.0");
            _angleCard.Value = r == null ? "" : r.Angle.ToString();
            _torqueCard.Footer = r == null ? "" : "측정 " + r.Time.ToString("HH:mm:ss");
            _angleCard.Footer = r == null ? "" : "측정 " + r.Time.ToString("HH:mm:ss");
            _torqueCard.Status = r == null ? StatusKind.Stop : TorqueRules.TorqueFault(r.Torque) == null ? StatusKind.Run : StatusKind.Alarm;
            _angleCard.Status = r == null ? StatusKind.Stop : TorqueRules.AngleFault(r.Angle) == null ? StatusKind.Run : StatusKind.Alarm;
        }

        private void ShowGrid(int cur)
        {
            _grid.Rows.Clear();
            int order = 0;
            foreach (int p in TorqueRules.Sequence)
            {
                order++;
                var r = ResultOf(p);
                StatusKind kind; string verdict;
                RowState(p, r, cur, out kind, out verdict);
                _grid.Rows.Add(kind, order, p, r == null ? "" : r.Torque.ToString("0.0"), r == null ? "" : r.Angle.ToString(),
                    verdict, r == null ? "" : r.Time.ToString("HH:mm:ss"));
                var row = _grid.Rows[_grid.Rows.Count - 1];
                if (r != null && TorqueRules.TorqueFault(r.Torque) != null) Emphasize(row.Cells[3], StatusKind.Alarm);
                if (r != null && TorqueRules.AngleFault(r.Angle) != null) Emphasize(row.Cells[4], StatusKind.Alarm);
            }
        }

        private static void Emphasize(DataGridViewCell cell, StatusKind kind)
        {
            cell.Style.ForeColor = ApcTheme.StatusText(kind);
            cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
        }

        /// <summary>표 행과 도식 볼트가 같은 상태를 말하게 한다.</summary>
        private void RowState(int point, PointResult r, int cur, out StatusKind kind, out string verdict)
        {
            if (r != null && r.IsOk) { kind = StatusKind.Run; verdict = "OK"; }
            else if (r != null && point == cur && _approved) { kind = StatusKind.Warn; verdict = "재체결 차례"; }
            else if (r != null) { kind = StatusKind.Alarm; verdict = "NG · " + r.Fault; }
            else if (point == cur && BlockReason() == null) { kind = StatusKind.Warn; verdict = "체결 차례"; }
            else { kind = StatusKind.Stop; verdict = "대기"; }
        }

        // ---------- 도식 ----------

        private void BuildGrid()
        {
            _grid.Columns.Add(new ApcLampColumn());
            _grid.Columns.Add("order", "순서");
            _grid.Columns.Add("point", "포인트");
            _grid.Columns.Add("torque", "토크(N·m)");
            _grid.Columns.Add("angle", "각도(°)");
            _grid.Columns.Add("verdict", "판정");
            _grid.Columns.Add("time", "시각");
            foreach (DataGridViewColumn c in _grid.Columns)
            {
                if (c is ApcLampColumn) continue;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            _grid.AllowUserToResizeRows = false;
            _grid.Resize += (s, e) => FitRows();
            _grid.Columns["verdict"].FillWeight = 160;
            _grid.Columns["verdict"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        // 10행 고정 표: 남는 높이를 행 높이로 나눠 표 아래가 비지 않게 한다.
        private void FitRows()
        {
            int rows = TorqueRules.Sequence.Length;
            int h = (_grid.ClientSize.Height - _grid.ColumnHeadersHeight) / rows;
            if (h > _grid.RowTemplate.Height)
            {
                _grid.RowTemplate.Height = h;
                foreach (DataGridViewRow r in _grid.Rows) r.Height = h;
            }
        }

        private void BuildPart()
        {
            _part.Padding = new Padding(S(12));
            _partCaption.Font = ApcTheme.GetFont(FontStyle.Bold);
            _partCaption.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.TextMuted);
            _partCaption.Text = "실린더 헤드 커버\r\n체결 순서  1 → 6 → 2 → 7 → 3 → 8 → 4 → 9 → 5 → 10";
            _part.Controls.Add(_partCaption);
            for (int p = 1; p < _bolts.Length; p++)
            {
                _bolts[p] = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = ApcTheme.GetFont(FontStyle.Bold) };
                _part.Controls.Add(_bolts[p]);
            }
            _part.Resize += (s, e) => LayoutPart();
            LayoutPart();
        }

        // 위 한 줄 1~5, 아래 한 줄 6~10. 순서 1-6-2-7…이 위아래를 번갈아 지그재그가 된다.
        private void LayoutPart()
        {
            int pad = S(12), w = _part.ClientSize.Width, h = _part.ClientSize.Height;
            if (w <= 0 || h <= 0) return;
            int colW = (w - 2 * pad) / 5;
            int d = Math.Min(S(150), Math.Min(colW - S(12), (h - 2 * pad) / 3));
            for (int p = 1; p <= 10; p++)
            {
                int col = (p - 1) % 5, rowIdx = (p - 1) / 5;
                int x = pad + col * colW + (colW - d) / 2;
                int y = rowIdx == 0 ? pad : h - pad - d;
                _bolts[p].SetBounds(x, y, d, d);
            }
            _partCaption.SetBounds(pad, pad + d, w - 2 * pad, h - 2 * pad - 2 * d);
        }

        private void ShowBolts(int cur)
        {
            for (int p = 1; p <= 10; p++)
            {
                StatusKind kind; string verdict;
                RowState(p, ResultOf(p), cur, out kind, out verdict);
                string word = kind == StatusKind.Run ? "OK" : kind == StatusKind.Alarm ? "NG" : kind == StatusKind.Warn ? "다음" : "";
                bool dark = kind == StatusKind.Run || kind == StatusKind.Alarm;
                _bolts[p].Text = p + (word.Length > 0 ? "\r\n" + word : "");
                _bolts[p].BackColor = ApcTheme.Status(kind);
                _bolts[p].ForeColor = ApcTheme.C(dark ? ApcTheme.Current.Colors.TextInverse : ApcTheme.Current.Colors.Text);
            }
        }

        private static ApcValueCard Card(string title)
        {
            return new ApcValueCard { Title = title, Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0), Unit = title == "토크" ? "N·m" : "°" };
        }
    }
}
