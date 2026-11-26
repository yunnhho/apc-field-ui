using System;
using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CarburizeFurnaceMonitor.Data;

namespace CarburizeFurnaceMonitor.Views
{
    /// <summary>
    /// 배치 종료 판정: 배치 정보(읽기 전용) + 공정 중 알람 + 시편 값 입력 → 판정 자동 표시.
    /// 알람 이력이 있는 배치는 기술자 사원증을 스캔해야만 합격으로 종료할 수 있다(입력칸으로 풀 수 없다).
    /// 값은 메인 화면에서 넘겨받는다. 매개변수 없는 생성자는 render용 샘플(막힌 상태로 시작).
    /// </summary>
    internal sealed class BatchJudgeDialog : ApcForm
    {
        private const int NumberWidth = 130;
        private const string BadgeWaitText = "스캔 대기";

        private readonly FurnaceInfo _furnace;
        private readonly bool _hasAlarm;
        private readonly Func<string, bool> _verifyBadge;
        private readonly ApcNumericBox _hardness = Number(1, 99.9m, 0.1m), _depth = Number(2, 9.99m, 0.01m);
        private readonly ApcScanBox _badgeBox = new ApcScanBox { Width = ApcTheme.Px(260) };
        private readonly Label _badgeState;
        private readonly ApcJudgePanel _panel = new ApcJudgePanel { Height = ApcTheme.Px(110) };
        private readonly ApcButton _finish = new ApcButton { Text = "배치 종료", Variant = ApcButtonVariant.Primary };
        private readonly Label _reason;
        private string _badge = "";

        public BatchJudgeDialog() : this(SampleFurnace(), new[] {
            new AlarmRecord("11:42", "침탄", "CP 하한 미달 1.00 % (하한 1.05) · 3분째"),
            new AlarmRecord("14:41", "강온", "온도 상한 초과 862 ℃ (상한 860) · 2분째") },
            code => code.StartsWith("TECH-", StringComparison.Ordinal), 60.4m, 0.96m) { }

        public BatchJudgeDialog(FurnaceInfo furnace, IList<AlarmRecord> alarms, Func<string, bool> verifyBadge) : this(furnace, alarms, verifyBadge, 0m, 0m) { }

        private BatchJudgeDialog(FurnaceInfo furnace, IList<AlarmRecord> alarms, Func<string, bool> verifyBadge, decimal hardness, decimal depth)
        {
            _furnace = furnace;
            _verifyBadge = verifyBadge;
            _hasAlarm = furnace.AlarmOccurred || alarms.Count > 0;
            SetupDialog("배치 종료 판정", 820);

            _hardness.Value = hardness;
            _depth.Value = depth;

            AddContent(SectionTitle("배치 정보"));
            AddContent(Strip(new[] { "로", "배치 번호", "품번" }, new[] { furnace.Id, furnace.BatchNo, furnace.PartNo }));
            AddContent(Strip(new[] { "레시피", "적재 수량 (개)", "시작", "종료" },
                new[] { furnace.Recipe, string.Format("{0} ({1}단 × {2})", furnace.Quantity, Judging.TraysPerBatch, Judging.GearsPerTray),
                    Judging.FormatTime(furnace.StartTime), furnace.EndTime.HasValue ? Judging.FormatTime(furnace.EndTime.Value) : "" }));

            AddContent(SectionTitle("공정 중 알람"));
            AddContent(AlarmView(alarms));

            AddContent(SectionTitle("시편 결과"));
            AddFieldRow("표면경도 (HRC)", _hardness, Hint(Judging.FormatHardness(Judging.HardnessMin) + " ~ " + Judging.FormatHardness(Judging.HardnessMax)));
            AddFieldRow("유효경화깊이 (mm)", _depth, Hint(Judging.FormatDepth(Judging.DepthMin) + " ~ " + Judging.FormatDepth(Judging.DepthMax)));

            _badgeState = Hint(BadgeWaitText);
            if (_hasAlarm)
            {
                AddContent(SectionTitle("기술자 확인 (알람 이력이 있어 합격 종료에 필요)"));
                AddFieldRow("기술자 사원증", _badgeBox, _badgeState);
                _badgeBox.Scanned += OnBadgeScanned;
            }

            AddContent(_panel);

            _finish.Click += (s, e) => Finish();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _finish);   // Esc = 취소, Enter 기본 버튼 없음(스캔 Enter 방지)
            _reason = SetFooterNote("");

            _hardness.ValueChanged += (s, e) => Reevaluate();
            _depth.ValueChanged += (s, e) => Reevaluate();
            Reevaluate();
        }

        /// <summary>종료 확정 시 결과. DialogResult.OK일 때만 읽는다.</summary>
        public BatchJudgement Result { get; private set; }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_hasAlarm) _badgeBox.Attach(this);   // 폼 어디에 포커스가 있어도 스캔을 받는다
        }

        private bool HasInput { get { return _hardness.Value > 0m && _depth.Value > 0m; } }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            bool ok = _verifyBadge(e.Code);
            e.Accepted = ok;
            if (ok) _badge = e.Code;
            BeginInvoke((Action)(() =>
            {
                if (!ok) ApcDialog.Warn(this, "등록되지 않은 사원증입니다. 기술자 사원증을 다시 스캔하세요.");
                Reevaluate();
            }));
        }

        /// <summary>입력·스캔이 바뀔 때마다 판정판·버튼·사유를 한 번에 맞춘다.</summary>
        private void Reevaluate()
        {
            bool badgeOk = _badge.Length > 0;
            _badgeState.Text = badgeOk ? "확인됨 · " + _badge : BadgeWaitText;

            if (!HasInput)
            {
                Show(StatusKind.Stop, "입력 대기", "시편 표면경도·유효경화깊이를 입력하세요");
                SetFinish(false, "시편 값 입력 전 · 종료 불가");
                return;
            }
            if (!Judging.SpecimenPasses(_hardness.Value, _depth.Value))
            {
                Show(StatusKind.Alarm, "불합격", FailReason());
                SetFinish(true, "");
                return;
            }
            if (_hasAlarm && !badgeOk)
            {
                Show(StatusKind.Warn, "확인 필요", "시편 기준 충족 · 기술자 사원증 스캔 필요");
                SetFinish(false, "알람 이력 있음 · 기술자 사원증 스캔 전 합격 종료 불가");
                return;
            }
            Show(StatusKind.Run, "합격", _hasAlarm ? "시편 기준 충족 · 기술자 확인 " + _badge : "시편 기준 충족");
            SetFinish(true, "");
        }

        private string FailReason()
        {
            var parts = new List<string>();
            if (_hardness.Value < Judging.HardnessMin) parts.Add("표면경도 " + Judging.FormatHardness(_hardness.Value) + " HRC (하한 " + Judging.FormatHardness(Judging.HardnessMin) + ")");
            else if (_hardness.Value > Judging.HardnessMax) parts.Add("표면경도 " + Judging.FormatHardness(_hardness.Value) + " HRC (상한 " + Judging.FormatHardness(Judging.HardnessMax) + ")");
            if (_depth.Value < Judging.DepthMin) parts.Add("경화깊이 " + Judging.FormatDepth(_depth.Value) + " mm (하한 " + Judging.FormatDepth(Judging.DepthMin) + ")");
            else if (_depth.Value > Judging.DepthMax) parts.Add("경화깊이 " + Judging.FormatDepth(_depth.Value) + " mm (상한 " + Judging.FormatDepth(Judging.DepthMax) + ")");
            return string.Join(" · ", parts.ToArray());
        }

        private void Show(StatusKind status, string text, string sub)
        {
            _panel.Status = status;
            _panel.Text = text;
            _panel.SubText = sub;
        }

        private void SetFinish(bool enabled, string reason)
        {
            _finish.Enabled = enabled;
            _reason.Text = reason;
        }

        private void Finish()
        {
            _hardness.Commit();
            _depth.Commit();
            Reevaluate();
            if (!_finish.Enabled) return;   // 막힌 상태에서는 어떤 경로로도 종료하지 않는다
            bool passed = Judging.SpecimenPasses(_hardness.Value, _depth.Value);
            string message = string.Format("{0} 배치 {1}을(를) {2}으로 종료할까요? 종료하면 되돌릴 수 없습니다.", _furnace.Id, _furnace.BatchNo, passed ? "합격" : "불합격");
            if (!ApcDialog.Confirm(this, message, "종료", passed ? ApcButtonVariant.Primary : ApcButtonVariant.Danger)) return;
            Result = new BatchJudgement(_furnace.Id, _furnace.BatchNo, _hardness.Value, _depth.Value, passed, passed && _hasAlarm ? _badge : "");
            DialogResult = DialogResult.OK;
        }

        private static Control AlarmView(IList<AlarmRecord> alarms)
        {
            if (alarms.Count == 0) return new ApcNoticeBar { Status = StatusKind.Run, Text = "공정 중 알람 없음" };
            var grid = new ApcGrid { Height = ApcGrid.HeightForRows(alarms.Count), ReadOnly = true };
            grid.Columns.Add(new ApcLampColumn());
            grid.Columns.Add("time", "시각");
            grid.Columns.Add("step", "단계");
            grid.Columns.Add("text", "내용");
            grid.Columns["text"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["text"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            foreach (var a in alarms) grid.Rows.Add(StatusKind.Alarm, a.Time, a.StepName, a.Text);
            return grid;
        }

        private static ApcInfoStrip Strip(string[] names, string[] values)
        {
            var strip = new ApcInfoStrip();
            strip.SetItems(names);
            for (int i = 0; i < names.Length; i++) strip.SetValue(names[i], values[i]);
            return strip;
        }

        private static Label Hint(string text)
        {
            var l = FieldCaption(text);
            l.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.TextMuted);
            return l;
        }

        private static ApcNumericBox Number(int decimals, decimal max, decimal increment)
        {
            return new ApcNumericBox { Minimum = 0m, Maximum = max, DecimalPlaces = decimals, Increment = increment, Width = ApcTheme.Px(NumberWidth) };
        }

        private static FurnaceInfo SampleFurnace()
        {
            var today = new DateTime(2026, 11, 26);
            return new FurnaceInfo("CF-4", 5, 20, "CF4-1126-02", "GR-4521A", 480, "GR-20", today.AddHours(9).AddMinutes(20), null, null, 66m, 0, 0, true, today.AddHours(15).AddMinutes(10));
        }
    }
}
