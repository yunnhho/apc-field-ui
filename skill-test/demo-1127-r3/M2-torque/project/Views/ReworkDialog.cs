using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TorqueSequence.Data;

namespace TorqueSequence.Views
{
    /// <summary>
    /// 재체결 승인: NG 포인트 정보(읽기 전용) + 반장 사원증 스캔 + 사유 선택.
    /// 같은 포인트 NG가 2회째면 승인 버튼을 막고 부품 불량 처리로 안내한다. NG 횟수는 부른 쪽이 준다(여기서 바꿀 수 없다).
    /// </summary>
    internal class ReworkDialog : ApcForm
    {
        private static readonly Regex Badge = new Regex(@"^EMP-\d{5}$");
        private static readonly string[] Reasons = { "공구 오작동·재교정", "볼트·나사산 상태 불량", "작업자 체결 자세 불량", "기타(반장 판단)" };

        private readonly ApcScanBox _supervisor = new ApcScanBox { ScanOnly = true };
        private readonly ApcComboBox _reason = new ApcComboBox();
        private readonly ApcButton _approve = new ApcButton { Text = "재체결 승인", Variant = ApcButtonVariant.Primary, DialogResult = DialogResult.OK, Enabled = false };
        private readonly Label _note;
        private readonly bool _blocked;

        public ReworkDialog() : this(7, 8.6m, 52, new DateTime(2026, 11, 27, 14, 32, 46), 1) { }

        public ReworkDialog(int point, decimal torque, int angle, DateTime time, int ngCount)
        {
            _blocked = ngCount >= TorqueJudge.MaxNgPerPoint;
            SetupDialog("재체결 승인", 780);

            var info = new ApcInfoStrip();
            info.SetItems("포인트", "토크(N·m)", "각도(°)", "NG 횟수");
            info.SetValue("포인트", point + "번");
            info.SetValue("토크(N·m)", TorqueJudge.FormatTorque(torque));
            info.SetValue("각도(°)", angle.ToString());
            info.SetValue("NG 횟수", ngCount + "회");
            AddContent(SectionTitle("NG 포인트 (체결 " + time.ToString("HH:mm:ss") + ")"));
            AddContent(info);

            if (_blocked)
            {
                var bar = new ApcNoticeBar { Status = StatusKind.Alarm, Text = "재체결 불가", Detail = "같은 포인트 NG 2회 · 부품 불량 처리" };
                AddContent(bar);
            }

            _supervisor.Accepts = c => c.StartsWith("EMP-");
            _supervisor.Scanned += OnSupervisorScanned;
            _reason.Items.AddRange(Reasons);
            _reason.SelectedIndexChanged += (s, e) => UpdateApprove();
            AddFieldRow("반장 사원증", _supervisor);
            AddFieldRow("재체결 사유", _reason);

            _note = SetFooterNote("");
            SetFooter(new ApcButton { Text = _blocked ? "닫기" : "취소", DialogResult = DialogResult.Cancel }, _approve);
            _supervisor.Attach(this);
            UpdateApprove();
        }

        public string Supervisor { get { return _supervisor.Text; } }
        public string ReasonText { get { return _reason.SelectedItem == null ? "" : _reason.SelectedItem.ToString(); } }

        private void OnSupervisorScanned(object sender, ScanEventArgs e)
        {
            bool ok = Badge.IsMatch(e.Code) && e.Code.StartsWith("EMP-9");
            e.Accepted = ok;
            if (ok) { _reason.Focus(); }
            else BeginInvoke(new Action(() => ApcDialog.Warn(this, "반장 사원증이 아닙니다 (EMP-9로 시작) · " + e.Code)));
            UpdateApprove();
        }

        private void UpdateApprove()
        {
            bool hasSupervisor = _supervisor.Text.StartsWith("EMP-9") && Badge.IsMatch(_supervisor.Text);
            bool hasReason = _reason.SelectedIndex >= 0;
            _approve.Enabled = !_blocked && hasSupervisor && hasReason;
            _note.Text = _blocked ? "재체결 불가 · 부품 불량 처리"
                       : !hasSupervisor ? "반장 사원증 스캔 필요"
                       : !hasReason ? "사유 선택 필요" : "";
        }
    }

    /// <summary>render용: 같은 포인트 NG 2회째(재체결 불가) 상태의 재체결 승인.</summary>
    internal sealed class ReworkBlockedDialog : ReworkDialog
    {
        public ReworkBlockedDialog() : base(7, 8.8m, 44, new DateTime(2026, 11, 27, 14, 34, 12), 2) { }
    }
}
