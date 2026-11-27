using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PalletPacker.Data;

namespace PalletPacker.Views
{
    /// <summary>
    /// 팔레트 마감: 팔레트 라벨 + 사원증 스캔, 총중량 입력. 박스 수 부족·중량 범위 밖이면 마감 버튼이 꺼지고 이유를 보인다.
    /// 박스 수는 호출한 화면의 데이터(읽기 전용)이며 이 dialog에서 바꿀 수 없다.
    /// </summary>
    internal sealed class PalletCloseDialog : ApcForm
    {
        private readonly int _boxCount, _target;
        private readonly ApcInfoStrip _status = new ApcInfoStrip();
        private readonly ApcScanBox _pallet = new ApcScanBox();
        private readonly ApcScanBox _badge = new ApcScanBox();
        private readonly ApcNumericBox _weight = new ApcNumericBox { DecimalPlaces = 1, Minimum = 0, Maximum = 2000, Increment = 0.1m };
        private readonly ApcButton _confirm = new ApcButton { Text = "마감", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;

        // 샘플: 21/40 박스(박스 부족)로 열린 상태 → 마감 버튼이 꺼진 채 이유가 보인다
        public PalletCloseDialog() : this(21, 40) { }

        public PalletCloseDialog(int boxCount, int targetBoxes)
        {
            _boxCount = boxCount;
            _target = targetBoxes;
            SetupDialog("팔레트 마감", 760);

            _status.SetItems("박스", "기준 총중량", "허용 범위");
            _status.SetValue("박스", _boxCount + "/" + _target);
            _status.SetValue("기준 총중량", PalletRules.ExpectedKg(_target).ToString("0.0") + " kg");
            _status.SetValue("허용 범위", PalletRules.MinKg(_target).ToString("0.0") + " ~ " + PalletRules.MaxKg(_target).ToString("0.0") + " kg");
            AddContent(_status);

            _pallet.Accepts = PalletRules.IsPalletLabel;
            _badge.Accepts = PalletRules.IsBadge;
            _pallet.Attach(this);
            _badge.Attach(this);
            _pallet.Scanned += (s, e) => OnScanned(_pallet, e, PalletRules.IsPalletLabel(e.Code), "팔레트 라벨이 아닙니다 · " + e.Code);
            _badge.Scanned += (s, e) => OnScanned(_badge, e, PalletRules.IsBadge(e.Code), "사원증이 아닙니다 · " + e.Code);
            _weight.ValueChanged += (s, e) => UpdateState();
            _weight.Clear();

            AddFieldRow("팔레트 라벨", _pallet);
            AddFieldRow("사원증", _badge);
            AddFieldRow("총중량 (kg)", _weight);

            _confirm.Click += (s, e) => DialogResult = DialogResult.OK;
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm);   // 스캔이 Enter를 쓰므로 AcceptButton 없음
            _note = SetFooterNote("");
            UpdateState();
        }

        public string PalletLabel { get { return _pallet.Text.Trim(); } }
        public string Badge { get { return _badge.Text.Trim(); } }
        public decimal TotalKg { get { return _weight.Value; } }

        private void OnScanned(ApcScanBox box, ScanEventArgs e, bool valid, string warning)
        {
            e.Accepted = valid;
            if (!valid) BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(this, warning)));
            else BeginInvoke((MethodInvoker)FocusNext);
            BeginInvoke((MethodInvoker)UpdateState);
        }

        private void FocusNext()
        {
            if (PalletLabel.Length == 0) _pallet.Focus();
            else if (Badge.Length == 0) _badge.Focus();
            else if (!_weight.HasValue) _weight.Focus();
        }

        private void UpdateState()
        {
            if (_note == null) return;
            string reason = BlockReason();
            _confirm.Enabled = reason == null;
            _note.Text = reason ?? "";
            _note.ForeColor = reason != null && _boxCount >= _target && _weight.HasValue
                ? ApcTheme.StatusText(StatusKind.Alarm) : ApcTheme.C(ApcTheme.Current.Colors.Text);
        }

        // 마감을 막는 이유(없으면 null). 박스 수 → 스캔 → 중량 순.
        private string BlockReason()
        {
            if (_boxCount < _target) return "박스 " + _boxCount + "/" + _target + " · 마감 불가";
            if (!PalletRules.IsPalletLabel(PalletLabel)) return "팔레트 라벨 스캔 필요";
            if (!PalletRules.IsBadge(Badge)) return "사원증 스캔 필요";
            if (!_weight.HasValue) return "총중량 입력 필요";
            if (!PalletRules.WeightInRange(_weight.Value, _target))
                return "총중량 " + _weight.Value.ToString("0.0") + " kg · 허용 범위 밖";
            return null;
        }
    }
}
