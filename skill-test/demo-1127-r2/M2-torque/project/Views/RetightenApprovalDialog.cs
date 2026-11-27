using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TorqueSequence.Data;

namespace TorqueSequence.Views
{
    /// <summary>
    /// 재체결 승인: NG 포인트 정보(읽기 전용) + 반장 사원증 스캔 + 사유 선택.
    /// 같은 포인트 NG가 NgLimitPerPoint회에 이르면 승인 입력 없이 재체결 불가 안내 + [불량 처리].
    /// 결과: 승인 → SupervisorBadge·Reason, 불량 처리 → Scrap = true (둘 다 DialogResult.OK).
    /// </summary>
    internal class RetightenApprovalDialog : ApcForm
    {
        private static readonly string[] Reasons = { "체결 공구 점검 완료", "볼트·나사산 교체", "체결면 이물 제거", "토크 설정 재확인" };

        private readonly ApcScanBox _badge = new ApcScanBox();
        private readonly ApcComboBox _reason = new ApcComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ApcButton _approve = new ApcButton { Text = "승인", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;

        public RetightenApprovalDialog()
            : this("HC-261127-0042", new PointResult(7, 8.8m, 28, new DateTime(2026, 11, 27, 10, 13, 30)), 1) { }

        public RetightenApprovalDialog(string serial, PointResult ng, int ngCount)
        {
            SetupDialog("재체결 승인", 600);
            bool blocked = ngCount >= TorqueRules.NgLimitPerPoint;

            var info1 = new ApcInfoStrip { Height = ApcTheme.Px(40) };
            info1.SetItems("시리얼", "포인트", "NG 횟수");
            info1.SetValue("시리얼", serial);
            info1.SetValue("포인트", ng.Point + "번");
            info1.SetValue("NG 횟수", ngCount + "회");
            AddContent(info1);
            var info2 = new ApcInfoStrip { Height = ApcTheme.Px(40) };
            info2.SetItems("토크 (N·m)", "각도 (°)", "NG 내용");
            info2.SetValue("토크 (N·m)", TorqueRules.Torque(ng.Torque));
            info2.SetValue("각도 (°)", ng.Angle.ToString());
            info2.SetValue("NG 내용", ng.FailReason);
            AddContent(info2);

            if (blocked)
            {
                var bar = new ApcNoticeBar { Status = StatusKind.Alarm, Text = "재체결 불가", Detail = "같은 포인트 NG " + ngCount + "회 · 부품 불량 처리로 이송하세요" };
                AddContent(bar);
                var scrap = new ApcButton { Text = "불량 처리", Variant = ApcButtonVariant.Danger };
                scrap.Click += (s, e) =>
                {
                    if (!ApcDialog.Confirm(this, "부품 " + serial + "을(를) 불량 처리할까요?")) return;
                    Scrap = true;
                    DialogResult = DialogResult.OK;
                };
                SetFooter(new ApcButton { Text = "닫기", DialogResult = DialogResult.Cancel }, scrap);
                _note = SetFooterNote("");
                return;
            }

            _badge.Attach(this);
            _badge.Accepts = c => c.StartsWith("EMP-");
            _badge.Scanned += OnBadgeScanned;
            AddFieldRow("반장 사원증", _badge);
            _reason.Items.AddRange(Reasons);
            _reason.SelectedIndexChanged += (s, e) => UpdateApprove();
            AddFieldRow("재체결 사유", _reason);

            _approve.Click += (s, e) => { DialogResult = DialogResult.OK; };
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _approve);
            _note = SetFooterNote("");
            UpdateApprove();   // 시작 상태 = 막힘(스캔·사유 없음)
        }

        public string SupervisorBadge { get; private set; }
        public string Reason { get { return _reason.SelectedItem as string; } }
        public bool Scrap { get; private set; }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            bool ok = TorqueRules.IsSupervisorBadge(e.Code);
            e.Accepted = ok;
            SupervisorBadge = ok ? e.Code : null;
            UpdateApprove();
            if (!ok)
                BeginInvoke(new Action(() => ApcDialog.Warn(this, "반장 사원증이 아닙니다 · EMP-9로 시작하는 사원증을 스캔하세요")));
        }

        private void UpdateApprove()
        {
            string why = SupervisorBadge == null ? "반장 사원증 스캔 필요" : Reason == null ? "사유 선택 필요" : "";
            _approve.Enabled = why.Length == 0;
            _note.Text = why;
        }
    }

    /// <summary>render 확인용: 같은 포인트 NG 2회째(재체결 불가) 상태.</summary>
    internal sealed class RetightenApprovalDialogSecondNg : RetightenApprovalDialog
    {
        public RetightenApprovalDialogSecondNg()
            : base("HC-261127-0042", new TorqueSequence.Data.PointResult(7, 11.4m, 52, new System.DateTime(2026, 11, 27, 10, 15, 2)), 2) { }
    }
}
