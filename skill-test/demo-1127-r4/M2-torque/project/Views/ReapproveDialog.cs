using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TorqueSequence.Data;

namespace TorqueSequence.Views
{
    /// <summary>재체결 승인: NG 포인트 정보(읽기 전용) + 반장 사원증 스캔 + 사유. 같은 포인트 NG 2회째면 재체결을 막고 불량 처리로 안내한다.</summary>
    internal sealed class ReapproveDialog : ApcForm
    {
        public enum Outcome { None, Retry, Scrap }

        public const int MaxNgPerPoint = 2;

        private readonly ApcScanBox _badge = new ApcScanBox { ScanOnly = true };
        private readonly ApcComboBox _reason = new ApcComboBox();
        private readonly ApcButton _confirm = new ApcButton { Text = "재체결 승인", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;
        private readonly bool _blocked;
        private string _approver = "";

        public Outcome Choice { get; private set; }
        public string Approver { get { return _approver; } }
        public string Reason { get { return _reason.Text; } }

        /// <summary>render용: NG 2회째(재체결 막힘) 상태.</summary>
        public ReapproveDialog() : this(7, 11.4m, 48, new DateTime(2026, 11, 27, 10, 43, 5), 2) { }

        public ReapproveDialog(int point, decimal torque, int angle, DateTime time, int ngCount)
        {
            _blocked = ngCount >= MaxNgPerPoint;
            SetupDialog("재체결 승인", 560);

            var strip = new ApcInfoStrip();
            strip.SetItems("포인트", "토크(N·m)", "각도(°)", "NG 횟수");
            strip.SetValue("포인트", point + "번");
            strip.SetValue("토크(N·m)", torque.ToString("0.0"));
            strip.SetValue("각도(°)", angle.ToString());
            strip.SetValue("NG 횟수", ngCount + "회");
            AddContent(strip);

            if (_blocked)
                AddContent(new ApcNoticeBar { Status = StatusKind.Alarm, Text = "같은 포인트 NG " + ngCount + "회째", Detail = "재체결 불가 · 부품 불량 처리" });

            _reason.Items.AddRange(new object[] { "공구 이상 조치 완료", "볼트·체결면 상태 교체", "작업자 조작 실수", "기타 (반장 확인)" });
            AddFieldRow("반장 사원증", _badge);
            AddFieldRow("재체결 사유", _reason);

            var cancel = new ApcButton { Text = "취소" };
            cancel.Click += (s, e) => { Choice = Outcome.None; DialogResult = DialogResult.Cancel; };
            _confirm.Click += (s, e) => ConfirmRetry();
            if (_blocked)
            {
                var scrap = new ApcButton { Text = "불량 처리", Variant = ApcButtonVariant.Danger };
                scrap.Click += (s, e) => ConfirmScrap();
                SetFooter(cancel, _confirm, scrap);
                _badge.Enabled = false;
                _reason.Enabled = false;
            }
            else
            {
                SetFooter(cancel, _confirm);
            }
            _note = SetFooterNote("");

            _badge.Attach(this);
            _badge.Scanned += OnBadgeScanned;
            _reason.SelectedIndexChanged += (s, e) => UpdateConfirm();
            UpdateConfirm();
        }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            bool ok = TorqueRules.IsSupervisorBadge(e.Code);
            e.Accepted = ok;
            if (ok) { _approver = e.Code; UpdateConfirm(); return; }
            BeginInvoke(new Action(() => ApcDialog.Warn(this, "반장 사원증(EMP-9으로 시작)이 아닙니다 · 다시 스캔하세요")));
        }

        private void UpdateConfirm()
        {
            string reason = _blocked ? "NG " + MaxNgPerPoint + "회째 · 재체결 불가"
                : _approver.Length == 0 ? "반장 사원증 스캔 필요"
                : _reason.SelectedIndex < 0 ? "사유 선택 필요" : "";
            _confirm.Enabled = reason.Length == 0;
            _note.Text = reason;
        }

        private void ConfirmRetry()
        {
            if (!_confirm.Enabled) return;
            if (!ApcDialog.Confirm(this, "재체결을 승인합니다. (" + _approver + ")", "승인", ApcButtonVariant.Primary)) return;
            Choice = Outcome.Retry;
            DialogResult = DialogResult.OK;
        }

        private void ConfirmScrap()
        {
            if (!ApcDialog.Confirm(this, "이 부품을 불량 처리합니다. 되돌릴 수 없습니다.")) return;
            Choice = Outcome.Scrap;
            DialogResult = DialogResult.OK;
        }
    }
}
