using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PalletPacker.Data;

namespace PalletPacker.Views
{
    /// <summary>
    /// 팔레트 마감: 박스 수·총중량이 기준에 못 미치면 마감 버튼이 꺼지고 이유가 버튼 옆에 나온다.
    /// 박스 수는 메인 화면이 넘겨 읽기 전용으로만 보인다(이 dialog에서 바꿀 수 없다).
    /// </summary>
    internal sealed class PalletCloseDialog : ApcForm
    {
        private static readonly Regex PalletPattern = new Regex(@"^PLT-\d{6}-\d{3}$");
        private static readonly Regex BadgePattern = new Regex(@"^EMP-\d{5}$");

        private readonly int _boxCount, _target;
        private readonly ApcScanBox _palletLabel = new ApcScanBox { ScanOnly = true };
        private readonly ApcScanBox _badge = new ApcScanBox { ScanOnly = true };
        private readonly ApcNumericBox _weight = new ApcNumericBox { DecimalPlaces = 1, Increment = 0.1m, Minimum = 0m, Maximum = 2000m };
        private readonly ApcButton _confirm = new ApcButton { Text = "마감", Variant = ApcButtonVariant.Primary };
        private readonly Label _reason;

        public string PalletLabel { get { return _palletLabel.Text; } }
        public string EmployeeBadge { get { return _badge.Text; } }
        public decimal TotalKg { get { return _weight.Value; } }

        // 샘플: 21/40 박스 → 마감이 막힌 상태
        public PalletCloseDialog() : this(21, 40) { }

        public PalletCloseDialog(int boxCount, int targetBoxes)
        {
            _boxCount = boxCount;
            _target = targetBoxes;
            SetupDialog("팔레트 마감", 640);

            var strip = new ApcInfoStrip();
            strip.SetItems("박스 수", "총중량 허용");
            strip.SetValue("박스 수", boxCount + " / " + targetBoxes);
            strip.SetValue("총중량 허용", PalletRules.Kg(PalletRules.MinKg(targetBoxes)) + " ~ " + PalletRules.Kg(PalletRules.MaxKg(targetBoxes)) + " kg");
            AddContent(strip);

            _palletLabel.Accepts = c => PalletPattern.IsMatch(c);
            _badge.Accepts = c => BadgePattern.IsMatch(c);
            _palletLabel.Attach(this);
            _badge.Attach(this);
            _palletLabel.Scanned += (s, e) => OnScan(_palletLabel, e, PalletPattern, "팔레트 라벨(PLT-yyMMdd-nnn)이 아닙니다 · ", _badge);
            _badge.Scanned += (s, e) => OnScan(_badge, e, BadgePattern, "사원증(EMP-nnnnn)이 아닙니다 · ", _weight);

            AddFieldRow("팔레트 라벨", _palletLabel);
            AddFieldRow("사원증", _badge);
            AddFieldRow("총중량 (kg)", _weight);

            _weight.Clear();   // 측정값은 비운 채 시작, 구독은 그 뒤

            var cancel = new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel };
            _confirm.Click += (s, e) => TryClose();
            SetFooter(cancel, _confirm);
            _reason = SetFooterNote();

            _weight.ValueChanged += (s, e) => Reload();
            Reload();
        }

        private void OnScan(ApcScanBox box, ScanEventArgs e, Regex pattern, string message, Control next)
        {
            if (!pattern.IsMatch(e.Code))
            {
                e.Accepted = false;
                string code = e.Code;
                BeginInvoke((Action)(() => ApcDialog.Warn(this, message + code)));
                return;
            }
            e.Accepted = true;
            next.Focus();
            BeginInvoke((Action)Reload);
        }

        /// <summary>마감을 막는 첫 사유(없으면 null). 박스 수·총중량이 막는 사유는 경고색.</summary>
        private string BlockReason(out bool isAlarm)
        {
            isAlarm = true;
            if (_boxCount < _target) return "박스 수 부족 · " + _boxCount + " / " + _target;
            if (!_weight.HasValue) return "총중량 입력 필요";
            string weight = PalletRules.WeightReason(_weight.Value, _target);
            if (weight != null) return weight;
            isAlarm = false;
            if (_palletLabel.Text.Length == 0) return "팔레트 라벨 스캔 필요";
            if (_badge.Text.Length == 0) return "사원증 스캔 필요";
            return null;
        }

        private void Reload()
        {
            bool alarm;
            string reason = BlockReason(out alarm);
            _confirm.Enabled = reason == null;
            _reason.Text = reason ?? "";
            _reason.ForeColor = alarm && reason != null && reason != "총중량 입력 필요"
                ? ApcTheme.StatusText(StatusKind.Alarm) : ApcTheme.C(ApcTheme.Current.Colors.Text);
        }

        private void TryClose()
        {
            if (_confirm.Enabled && ApcDialog.Confirm(this, "팔레트를 마감합니다. 마감 후에는 되돌릴 수 없습니다.", "마감", ApcButtonVariant.Primary))
                DialogResult = DialogResult.OK;
        }
    }
}
