using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PressPmChecklist.Data;

namespace PressPmChecklist.Views
{
    /// <summary>
    /// 프레스 PM 점검 진행 화면: 스캔으로 시작 → 판정판(지금 할 단계) + 진행 카드 → 단계 타일 → 점검 방법·기준 → 입력 → 이전 결과.
    /// 단계는 순서대로 하나씩 끝내야 넘어간다. 안전 단계(1)가 OK 되기 전에는 다른 단계가 열리지 않는다.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private enum Phase { Ready, Running, AllDone }

        private const string ScanBlockedReason = "설비·사원증 스캔 전 · 점검 불가";
        private const string MeasureBlockedReason = "측정값 입력 전 · 확정 불가";

        private readonly IMainViewSource _source;
        private readonly IList<PmStep> _steps;
        private readonly List<StepResult> _results = new List<StepResult>();
        private Phase _phase = Phase.Ready;
        private string _equipment, _employee;
        private DateTime _startedAt;
        private TimeSpan _elapsed;
        private bool _safetyNg, _valueEntered;

        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        private readonly ApcValueCard _cardOk = Card("완료 (OK)", "단계");
        private readonly ApcValueCard _cardNg = Card("NG", "단계");
        private readonly ApcValueCard _cardRemain = Card("남음", "단계");
        private readonly ApcValueCard _cardTime = Card("진행 시간", "");
        private readonly ApcScanBox _scanEq = new ApcScanBox { Width = S(240) };
        private readonly ApcScanBox _scanEmp = new ApcScanBox { Width = S(240) };
        private readonly ApcButton _abort = Button("점검 중단", ApcButtonVariant.Normal, 130);
        private readonly List<ApcStatusTile> _tiles = new List<ApcStatusTile>();
        private readonly ApcInfoStrip _strip = new ApcInfoStrip();
        private readonly ApcButton _okBtn = Button("OK", ApcButtonVariant.Primary);
        private readonly ApcButton _ngBtn = Button("NG", ApcButtonVariant.Normal);
        private readonly Label _reasonA = Caption("");
        private readonly ApcNumericBox _value = new ApcNumericBox { Width = S(170), Maximum = 9999 };
        private readonly Label _valueCaption = Caption("측정값");
        private readonly Label _valueVerdict = Caption("");
        private readonly ApcButton _confirmBtn = Button("측정값 확정", ApcButtonVariant.Primary);
        private readonly Label _reasonB = Caption("");
        private readonly ApcButton _doneBtn = Button("점검 완료 · 결과 확인", ApcButtonVariant.Primary);
        private readonly Label _reasonC = Caption("");
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private TableLayoutPanel _okNgRow, _measRow, _doneRow;
        private readonly Timer _timer = new Timer { Interval = 1000 };

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("프레스 PM 점검")
        {
            _source = source;
            _steps = source.GetSteps();
            BuildGrid();

            _scanEq.Scanned += OnScanned;
            _scanEmp.Scanned += OnScanned;
            _abort.Click += (s, e) => Abort();
            _okBtn.Click += (s, e) => RecordVisual(true);
            _ngBtn.Click += (s, e) => RecordVisual(false);
            _value.ValueChanged += (s, e) => { _valueEntered = true; ShowValueVerdict(); };
            _confirmBtn.Click += (s, e) => RecordMeasure();
            _doneBtn.Click += (s, e) => OpenComplete();
            _timer.Tick += (s, e) => ShowTime();
            Disposed += (s, e) => _timer.Dispose();

            for (int i = 0; i < _steps.Count; i++) _tiles.Add(new ApcStatusTile { Dock = DockStyle.Fill });
            _strip.SetItems("점검 방법", "기준");
            foreach (var l in new[] { _reasonA, _reasonB, _reasonC }) { l.Dock = DockStyle.Fill; l.TextAlign = ContentAlignment.MiddleLeft; }
            foreach (var b in new[] { _okBtn, _ngBtn, _confirmBtn, _doneBtn }) { b.Dock = DockStyle.Fill; }

            _okNgRow = Columns(S(110), new Control[] { _okBtn, _ngBtn, _reasonA }, new[] { 30f, 30f, 40f });
            var valueHost = new Panel { Dock = DockStyle.Fill };
            var valueRow = Row(_valueCaption, _value, _valueVerdict);
            valueRow.Padding = new Padding(0, S(24), 0, 0);   // 높이 110 칸 안에서 세로 가운데 쪽으로
            valueHost.Controls.Add(valueRow);
            _measRow = Columns(S(110), new Control[] { valueHost, _confirmBtn, _reasonB }, new[] { 40f, 30f, 30f });
            _doneRow = Columns(S(110), new Control[] { _doneBtn, _reasonC }, new[] { 40f, 60f });

            // Fill 먼저, Top 줄은 화면 아래쪽부터
            Body.Controls.Add(Titled("이전 단계 결과", _grid));
            Body.Controls.Add(_doneRow);
            Body.Controls.Add(_measRow);
            Body.Controls.Add(_okNgRow);
            Body.Controls.Add(Columns(_strip.Height, new Control[] { _strip }));
            Body.Controls.Add(Columns(S(96), _tiles.Cast<Control>().ToArray()));
            Body.Controls.Add(Row(Caption("설비 바코드"), _scanEq, Caption("점검자 사원증"), _scanEmp, _abort));
            Body.Controls.Add(Columns(S(150), new Control[] { _judge, _cardOk, _cardNg, _cardRemain, _cardTime }, new[] { 60f, 10f, 10f, 10f, 10f }));

            PmSession session = source.GetCurrentSession();
            if (session != null) Restore(session);
            UpdateView();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) { _scanEq.Attach(form); _scanEmp.Attach(form); }
        }

        // ---- 시작·중단 ----

        private void Restore(PmSession s)
        {
            _equipment = s.Equipment; _employee = s.Employee; _startedAt = s.StartedAt;
            _scanEq.Text = _equipment; _scanEmp.Text = _employee;
            foreach (var r in s.Results) { _results.Add(r); AddGridRow(r); }
            _phase = _results.Count >= _steps.Count ? Phase.AllDone : Phase.Running;
            if (_phase == Phase.AllDone) _elapsed = _results.Last().At - _startedAt;
        }

        private void OnScanned(object sender, ScanEventArgs e)
        {
            if (_phase != Phase.Ready)
            {
                e.Accepted = false;
                // 진행 중에는 칸의 값을 지킨다(거부된 코드가 칸에 남으므로 되돌린다)
                BeginInvoke(new Action(() => { _scanEq.Text = _equipment; _scanEmp.Text = _employee; }));
                WarnAfterScan("점검 진행 중 · 설비·점검자는 점검 중단 후 바꿀 수 있습니다");
                return;
            }
            bool isEquipment = e.Code.StartsWith("EQ-", StringComparison.Ordinal);
            bool isEmployee = e.Code.StartsWith("EMP-", StringComparison.Ordinal);
            if ((isEquipment && !_source.IsKnownEquipment(e.Code)) || (isEmployee && !_source.IsKnownEmployee(e.Code)) || (!isEquipment && !isEmployee))
            {
                e.Accepted = false;
                if (sender == _scanEq) _equipment = null; else _employee = null;
                WarnAfterScan("등록되지 않은 바코드 · " + e.Code + " (설비 EQ-PRS-nn, 사원증 EMP-nnnnn)");
                return;
            }
            // 칸이 바뀌어도 종류대로 제자리에 넣는다
            if (isEquipment) { _equipment = e.Code; BeginInvoke(new Action(() => { _scanEq.Text = _equipment; if (sender != _scanEq) _scanEmp.Text = _employee ?? ""; AfterScan(); })); }
            else { _employee = e.Code; BeginInvoke(new Action(() => { _scanEmp.Text = _employee; if (sender != _scanEmp) _scanEq.Text = _equipment ?? ""; AfterScan(); })); }
        }

        private void AfterScan()
        {
            if (_equipment != null && _employee != null) { _startedAt = DateTime.Now; _phase = Phase.Running; }
            else (_equipment == null ? _scanEq : _scanEmp).Focus();
            UpdateView();
        }

        private void WarnAfterScan(string message)
        {
            BeginInvoke(new Action(() => ApcDialog.Warn(FindForm(), message)));
        }

        private void Abort()
        {
            if (_phase == Phase.Ready) return;
            if (!ApcDialog.Confirm(this, "점검을 중단합니다. 지금까지의 결과는 새 점검에 이어지지 않습니다.", "중단", ApcButtonVariant.Danger)) return;
            ResetToReady();
        }

        private void ResetToReady()
        {
            _phase = Phase.Ready; _results.Clear(); _grid.Rows.Clear();
            _equipment = _employee = null; _scanEq.Text = ""; _scanEmp.Text = "";
            _safetyNg = false; _elapsed = TimeSpan.Zero;
            UpdateView();
            _scanEq.Focus();
        }

        // ---- 단계 기록 ----

        private PmStep Current { get { return _results.Count < _steps.Count ? _steps[_results.Count] : null; } }

        private void RecordVisual(bool ok)
        {
            PmStep step = Current;
            if (_phase != Phase.Running || step == null || step.IsMeasure) return;
            if (!ok && step.Kind == StepKind.Safety)
            {
                // 안전 단계는 NG로 넘어갈 수 없다 — 잠금이 확인될 때까지 이 단계에 머문다.
                _safetyNg = true;
                UpdateView();
                ApcDialog.Warn(FindForm(), "안전 단계 NG · 전원 차단·잠금을 확인한 뒤 OK를 누르세요. 완료 전에는 다음 단계로 갈 수 없습니다");
                return;
            }
            Record(new StepResult(step, ok, null, DateTime.Now));
        }

        private void RecordMeasure()
        {
            PmStep step = Current;
            if (_phase != Phase.Running || step == null || !step.IsMeasure || !_valueEntered) return;
            decimal v = Math.Round(_value.Value, step.Decimals);
            Record(new StepResult(step, step.IsWithin(v), v, DateTime.Now));
        }

        private void Record(StepResult r)
        {
            _safetyNg = false;
            _results.Add(r);
            _source.SaveStepResult(_equipment, _employee, r);
            AddGridRow(r);
            if (_results.Count >= _steps.Count) { _phase = Phase.AllDone; _elapsed = DateTime.Now - _startedAt; }
            UpdateView();
        }

        private void OpenComplete()
        {
            using (var dlg = new PmCompleteDialog(_source, _equipment, _employee, DateTime.Now, _elapsed, _results))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.CompleteSession(_equipment, _employee, _elapsed);
                ApcDialog.Alert(FindForm(), "점검 결과를 저장했습니다" + (dlg.RequestNo != null ? " · 정비 요청 " + dlg.RequestNo : ""));
                ResetToReady();
            }
        }

        // ---- 화면 갱신 ----

        private void UpdateView()
        {
            int ng = _results.Count(r => !r.Ok);
            int total = _steps.Count;
            bool ready = _phase == Phase.Ready;
            _timer.Enabled = _phase == Phase.Running;
            _abort.Enabled = !ready;

            _cardOk.Value = ready ? "" : (_results.Count - ng).ToString();
            _cardNg.Value = ready ? "" : ng.ToString();
            _cardRemain.Value = ready ? "" : (total - _results.Count).ToString();
            foreach (var c in new[] { _cardOk, _cardNg, _cardRemain }) c.Status = ready ? StatusKind.Stop : StatusKind.Run;
            _cardNg.ValueStatus = ng > 0 ? StatusKind.Alarm : (StatusKind?)null;
            _cardTime.Footer = ready ? "" : "시작 " + _startedAt.ToString("HH:mm");
            _cardTime.Status = ready ? StatusKind.Stop : StatusKind.Run;
            ShowTime();

            UpdateTiles();
            UpdateJudge(ng);
            UpdateInput();
        }

        private void ShowTime()
        {
            if (_phase == Phase.Ready) { _cardTime.Value = ""; return; }
            TimeSpan t = _phase == Phase.AllDone ? _elapsed : DateTime.Now - _startedAt;
            _cardTime.Value = string.Format("{0:00}:{1:00}", (int)t.TotalMinutes, t.Seconds);
        }

        private void UpdateTiles()
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                var tile = _tiles[i];
                tile.Text = _steps[i].No + "  " + ShortName(_steps[i]);
                tile.Selected = _phase == Phase.Running && i == _results.Count;
                if (i < _results.Count) tile.Status = _results[i].Ok ? StatusKind.Run : StatusKind.Alarm;
                else if (_phase == Phase.Running && i == _results.Count) tile.Status = _safetyNg ? StatusKind.Alarm : StatusKind.Warn;
                else tile.Status = StatusKind.Stop;
            }
        }

        private void UpdateJudge(int ng)
        {
            PmStep step = Current;
            if (_phase == Phase.Ready)
            {
                _judge.Status = StatusKind.Stop; _judge.Text = "READY"; _judge.SubText = "설비 바코드와 사원증을 스캔하세요";
            }
            else if (_phase == Phase.AllDone)
            {
                _judge.Status = ng > 0 ? StatusKind.Alarm : StatusKind.Run;
                _judge.Text = ng > 0 ? "NG " + ng + "건" : "전 항목 OK";
                _judge.SubText = "점검 완료 처리 전 · 결과 확인 필요";
            }
            else if (step.Kind == StepKind.Safety)
            {
                _judge.Status = _safetyNg ? StatusKind.Alarm : StatusKind.Warn;
                _judge.Text = step.Name;
                _judge.SubText = _safetyNg ? "안전 단계 NG · 잠금 확인 후 OK 필요" : "안전 단계 · 완료 전 다른 단계 진행 불가";
            }
            else
            {
                _judge.Status = StatusKind.Info;
                _judge.Text = step.Name;
                _judge.SubText = "단계 " + step.No + " / " + _steps.Count + (ng > 0 ? " · NG " + ng + "건" : "");
            }
        }

        private void UpdateInput()
        {
            PmStep step = Current;
            bool ready = _phase == Phase.Ready, running = _phase == Phase.Running;
            bool measure = running && step.IsMeasure;
            _okNgRow.Visible = !measure && _phase != Phase.AllDone;
            _measRow.Visible = measure;
            _doneRow.Visible = _phase == Phase.AllDone;

            _okBtn.Enabled = _ngBtn.Enabled = running && !measure;
            _reasonA.Text = ready ? ScanBlockedReason : "";
            _strip.SetValue("점검 방법", running ? step.Method : "");
            _strip.SetValue("기준", running ? step.Standard : "");

            if (measure)
            {
                _valueEntered = false;
                _value.DecimalPlaces = step.Decimals;
                _value.Increment = (decimal)Math.Pow(10, -step.Decimals);
                _value.Value = 0;
                _valueEntered = false;
                _valueCaption.Text = "측정값 (" + step.Unit + ")";
                ShowValueVerdict();
            }
            _doneBtn.Enabled = _phase == Phase.AllDone;
            _reasonC.Text = "";
        }

        private void ShowValueVerdict()
        {
            PmStep step = Current;
            _confirmBtn.Enabled = _valueEntered;
            _reasonB.Text = _valueEntered ? "" : MeasureBlockedReason;
            if (step == null || !_valueEntered) { _valueVerdict.Text = ""; return; }
            bool ok = step.IsWithin(Math.Round(_value.Value, step.Decimals));
            _valueVerdict.Text = ok ? "판정 OK" : "판정 NG";
            _valueVerdict.ForeColor = ApcTheme.StatusText(ok ? StatusKind.Run : StatusKind.Alarm);
        }

        // ---- 결과 표 ----

        private void BuildGrid()
        {
            _grid.Columns.Add("no", "단계");
            _grid.Columns.Add("item", "점검 항목");
            _grid.Columns.Add("value", "측정값");
            _grid.Columns.Add("std", "기준");
            _grid.Columns.Add(new ApcLampColumn { HeaderText = "판정" });
            _grid.Columns.Add("time", "시각");
            _grid.Columns["item"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns["item"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            _grid.Columns["std"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }

        private void AddGridRow(StepResult r)
        {
            _grid.Rows.Insert(0, r.Step.No.ToString(), r.Step.ItemLabel, r.ValueText, r.Step.Standard,
                new ApcLampValue(r.Ok ? StatusKind.Run : StatusKind.Alarm, r.Ok ? "OK" : "NG"), r.At.ToString("HH:mm"));
            if (!r.Ok && r.Value.HasValue)
            {
                var style = _grid.Rows[0].Cells["value"].Style;
                style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                style.Font = ApcTheme.GetFont(FontStyle.Bold);
            }
        }

        private static string ShortName(PmStep s)
        {
            switch (s.No)
            {
                case 1: return "전원·LOTO";
                case 2: return "작동유";
                case 3: return "유압 압력";
                case 4: return "클러치·브레이크";
                case 5: return "평행도";
                case 6: return "윤활유";
                case 7: return "광커튼";
                default: return "비상정지";
            }
        }

        private static ApcValueCard Card(string title, string unit)
        {
            return new ApcValueCard { Title = title, Unit = unit, Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        }
    }
}
