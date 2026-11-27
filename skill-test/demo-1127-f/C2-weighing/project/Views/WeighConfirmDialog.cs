using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using RubberWeighing.Data;

namespace RubberWeighing.Views
{
    /// <summary>
    /// 계량 확정 dialog. 원료·목표·실측·오차는 읽기 전용(부른 쪽이 값을 준다), 입력은 작업자 사원증 스캔뿐.
    /// 실측이 허용 범위 밖이거나 사원증이 없으면 확정 버튼이 꺼진다.
    /// </summary>
    internal sealed class WeighConfirmDialog : ApcForm
    {
        private static readonly Regex BadgePattern = new Regex(@"^EMP-\d{5}$");

        private readonly Material _material;
        private readonly decimal _measured;
        private readonly ApcScanBox _badge = new ApcScanBox { ScanOnly = true, Accepts = c => c.StartsWith("EMP-") };
        private readonly ApcButton _confirm = new ApcButton { Text = "확정", Variant = ApcButtonVariant.Primary, DialogResult = DialogResult.OK };
        private readonly Label _note;

        /// <summary>샘플: 오일 4.03 kg(상한 4.02 초과) — 막힌 상태.</summary>
        public WeighConfirmDialog() : this(new Material(3, "OIL", "오일", 4.00m), "ML-OIL-261126-01", 4.03m) { }

        public WeighConfirmDialog(Material material, string lotNo, decimal measured)
        {
            _material = material;
            _measured = measured;
            SetupDialog("계량 확정", 520);

            var measuredBox = ReadOnlyBox(material.Fmt(measured));
            bool inRange = material.Judge(measured) == WeighVerdict.Ok;
            if (!inRange)
            {
                measuredBox.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                measuredBox.Font = ApcTheme.GetFont(System.Drawing.FontStyle.Bold);
            }
            AddFieldRow("원료", ReadOnlyBox(material.No + ". " + material.Name));
            AddFieldRow("원료 LOT", ReadOnlyBox(lotNo));
            AddFieldRow("목표 (kg)", ReadOnlyBox(material.Fmt(material.Target)));
            AddFieldRow("허용 범위 (kg)", ReadOnlyBox(material.RangeText));
            AddFieldRow("실측 (kg)", measuredBox);
            AddFieldRow("오차 (%)", ReadOnlyBox(material.ErrorPercentText(measured)));
            AddFieldRow("작업자 사원증", _badge);

            _note = SetFooterNote("");
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm);
            _badge.Attach(this);
            _badge.Scanned += OnBadgeScanned;
            UpdateConfirm();
        }

        public string OperatorId { get { return _badge.Text; } }

        private static ApcTextBox ReadOnlyBox(string text)
        {
            return new ApcTextBox { ReadOnly = true, Text = text };
        }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            e.Accepted = BadgePattern.IsMatch(e.Code);
            if (!e.Accepted)
            {
                BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(this, "사원증 형식 오류 · " + e.Code)));
                return;
            }
            UpdateConfirm();
        }

        private void UpdateConfirm()
        {
            WeighVerdict v = _material.Judge(_measured);
            string reason = v == WeighVerdict.Over ? "상한 초과 · 확정 불가"
                          : v == WeighVerdict.Under ? "하한 미달 · 확정 불가"
                          : !BadgePattern.IsMatch(_badge.Text) ? "사원증 스캔 필요" : "";
            _confirm.Enabled = reason.Length == 0;
            _note.Text = reason;
            _note.ForeColor = v == WeighVerdict.Ok ? _note.ForeColor : ApcTheme.StatusText(StatusKind.Alarm);
        }
    }
}
