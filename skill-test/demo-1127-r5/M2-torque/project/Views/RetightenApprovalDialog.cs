using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TorqueSequence.Data;

namespace TorqueSequence.Views
{
    /// <summary>
    /// 재체결 승인 dialog. NG 포인트 정보는 읽기 전용, 반장 사원증 스캔(ScanOnly) + 사유 선택 후 승인.
    /// 같은 포인트 NG가 MaxNgPerPoint회 이상이면 재체결을 막고 부품 불량 처리만 안내한다.
    /// 결과: 승인 → SupervisorBadge·Reason, 불량 처리 → ScrapRequested. 둘 다 DialogResult.OK.
    /// </summary>
    internal class RetightenApprovalDialog : ApcForm
    {
        private static readonly string[] Reasons = { "공구 토크 미달", "볼트·체결면 이물", "공구 교체 후 재작업", "기타 (반장 판단)" };
        private static readonly Regex SupervisorBadgePattern = new Regex(@"^EMP-9\d{4}$");

        private readonly ApcScanBox _badge = new ApcScanBox { ScanOnly = true };
        private readonly ApcComboBox _reason = new ApcComboBox();
        private readonly ApcButton _confirm = new ApcButton { Variant = ApcButtonVariant.Primary };
        private readonly System.Windows.Forms.Label _note;
        private readonly bool _blocked;

        public RetightenApprovalDialog() : this("HC-261127-0042", 7, 8.7m, 28, 1) { }

        public RetightenApprovalDialog(string serial, int point, decimal torque, int angle, int ngCount)
        {
            _blocked = ngCount >= TightenRules.MaxNgPerPoint;
            SetupDialog("재체결 승인", 600);

            var info = new ApcInfoStrip();
            info.SetItems("포인트", "토크(N·m)", "각도(°)", "NG 횟수");
            info.SetValue("포인트", point + "번");
            info.SetValue("토크(N·m)", TightenRules.Torque(torque));
            info.SetValue("각도(°)", angle.ToString());
            info.SetValue("NG 횟수", ngCount + "회");
            var detail = new ApcInfoStrip();
            detail.SetItems("부품 시리얼", "NG 사유");
            detail.SetValue("부품 시리얼", serial);
            detail.SetValue("NG 사유", TightenRules.NgReason(new TightenRecord(point, torque, angle, System.DateTime.Now)));

            AddContent(SectionTitle("NG 포인트"));
            AddContent(info);
            AddContent(detail);

            _note = SetFooterNote("");
            var cancel = new ApcButton { Text = _blocked ? "닫기" : "취소", DialogResult = DialogResult.Cancel };
            if (_blocked) BuildBlocked(point, cancel); else BuildApproval(cancel);
        }

        public string SupervisorBadge { get { return _badge.Text; } }
        public string Reason { get { return _reason.Text; } }
        public bool ScrapRequested { get; private set; }

        private void BuildBlocked(int point, ApcButton cancel)
        {
            AddContent(new ApcNoticeBar
            {
                Status = StatusKind.Alarm,
                Text = "재체결 불가",
                Detail = "같은 " + point + "번 NG 2회 · 부품 불량 처리",
                Height = ApcTheme.Px(ApcLayout.FieldHeight)
            });
            _confirm.Text = "부품 불량 처리";
            _confirm.Variant = ApcButtonVariant.Danger;
            _confirm.Click += (s, e) =>
            {
                if (!ApcDialog.Confirm(this, point + "번 NG 2회로 이 부품을 불량 처리할까요?")) return;
                ScrapRequested = true;
                DialogResult = DialogResult.OK;
            };
            SetFooter(cancel, _confirm);
        }

        private void BuildApproval(ApcButton cancel)
        {
            AddContent(SectionTitle("승인"));
            _reason.Items.AddRange(Reasons);
            AddFieldRow("반장 사원증", _badge);
            AddFieldRow("재체결 사유", _reason);

            _confirm.Text = "승인";
            _confirm.Click += (s, e) => { DialogResult = DialogResult.OK; };
            SetFooter(cancel, _confirm);
            UpdateConfirm();

            // 이벤트는 구성이 끝난 뒤 마지막에 연결한다.
            _badge.Attach(this);
            _badge.Accepts = c => c.StartsWith("EMP-");
            _badge.Scanned += OnBadgeScanned;
            _reason.SelectedIndexChanged += (s, e) => UpdateConfirm();
        }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            e.Accepted = SupervisorBadgePattern.IsMatch(e.Code);
            if (!e.Accepted)
                BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(this, "반장 사원증(EMP-9로 시작)을 스캔하세요.")));
            UpdateConfirm();
        }

        private void UpdateConfirm()
        {
            bool badgeOk = SupervisorBadgePattern.IsMatch(_badge.Text);
            bool reasonOk = _reason.SelectedIndex >= 0;
            _confirm.Enabled = badgeOk && reasonOk;
            _note.Text = !badgeOk ? "반장 사원증 스캔 필요" : !reasonOk ? "사유 선택 필요" : "";
        }
    }

    /// <summary>render용: NG 2회째로 재체결이 막힌 상태(부품 불량 처리 안내).</summary>
    internal sealed class RetightenBlockedDialog : RetightenApprovalDialog
    {
        public RetightenBlockedDialog() : base("HC-261127-0042", 7, 8.9m, 33, TightenRules.MaxNgPerPoint) { }
    }
}
