using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PalletPacker.Data;

namespace PalletPacker.Views
{
    /// <summary>
    /// 팔레트 마감 dialog: 팔레트 라벨 + 사원증 스캔, 총중량 입력.
    /// 박스 수는 메인 화면이 넘긴 값을 읽기 전용으로만 보인다(여기서 바꿀 수 없다).
    /// 박스 수 미달·스캔 누락·총중량 범위 밖이면 [마감]이 꺼지고 이유가 버튼 왼쪽에 보인다.
    /// </summary>
    internal sealed class PalletCloseDialog : ApcForm
    {
        private const int NumberWidth = 150;
        private const decimal WeightMax = 2000.0m;

        private readonly int _boxCount, _targetBoxes;
        private readonly ApcScanBox _pallet = new ApcScanBox();
        private readonly ApcScanBox _employee = new ApcScanBox();
        private readonly ApcNumericBox _weight = new ApcNumericBox { Minimum = 0, Maximum = WeightMax, DecimalPlaces = 1, Increment = 0.1m, Width = ApcTheme.Px(NumberWidth) };
        private readonly ApcButton _confirm = new ApcButton { Text = "마감", Variant = ApcButtonVariant.Primary };
        private readonly System.Windows.Forms.Label _note;
        private string _palletCode, _employeeCode;

        // 샘플: 3층 5개째(21/40)까지 쌓은 채 연 상태 → 박스 수 미달로 막힘
        public PalletCloseDialog() : this(21, PalletRules.TargetBoxes, 284.5m) { }

        public PalletCloseDialog(int boxCount, int targetBoxes, decimal measuredKg)
        {
            _boxCount = boxCount; _targetBoxes = targetBoxes;
            SetupDialog("팔레트 마감", 820);

            var info = new ApcInfoStrip();
            info.SetItems("박스 수", "기준 총중량", "허용 범위");
            info.SetValue("박스 수", boxCount + " / " + targetBoxes);
            info.SetValue("기준 총중량", PalletRules.FormatKg(PalletRules.NominalKg) + " kg");
            info.SetValue("허용 범위", PalletRules.FormatKg(PalletRules.MinKg) + " ~ " + PalletRules.FormatKg(PalletRules.MaxKg) + " kg");
            AddContent(info);

            AddFieldRow("팔레트 라벨", _pallet);
            AddFieldRow("작업자 사원증", _employee);
            _weight.Value = measuredKg;
            AddFieldRow("총중량 (kg)", _weight);

            _pallet.Attach(this);      // 먼저 Attach한 칸이 기본 수신
            _employee.Attach(this);
            _pallet.Scanned += (s, e) => OnScanned(e, PalletRules.PalletLabel.IsMatch(e.Code), "팔레트 라벨 형식이 아닙니다 · PLT-yyMMdd-nnn", true);
            _employee.Scanned += (s, e) => OnScanned(e, PalletRules.EmployeeLabel.IsMatch(e.Code), "사원증 형식이 아닙니다 · EMP-nnnnn", false);
            _pallet.TextChanged += (s, e) => Evaluate();
            _employee.TextChanged += (s, e) => Evaluate();
            _weight.ValueChanged += (s, e) => Evaluate();

            _confirm.Click += (s, e) => Close_();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm);   // 스캔 Enter로 닫히지 않게 AcceptButton 없음
            _note = SetFooterNote("");
            Evaluate();
            Shown += (s, e) => _pallet.Focus();
        }

        public string PalletLabel { get; private set; }
        public string EmployeeId { get; private set; }
        public decimal TotalKg { get; private set; }

        private void OnScanned(ScanEventArgs e, bool valid, string message, bool isPallet)
        {
            e.Accepted = valid;
            if (!valid)
            {
                BeginInvoke(new Action(() => ApcDialog.Warn(this, message)));
                return;
            }
            if (isPallet) { _palletCode = e.Code; _employee.Focus(); }
            else { _employeeCode = e.Code; _weight.Focus(); }
            Evaluate();
        }

        /// <summary>막는 이유 하나(없으면 ""). 순서: 박스 수 → 팔레트 → 사원증 → 총중량.</summary>
        private string BlockReason()
        {
            if (_boxCount < _targetBoxes) return "박스 " + _boxCount + "/" + _targetBoxes + " · 목표 미달";
            if (_palletCode == null || _pallet.Text != _palletCode) return "팔레트 라벨 스캔 필요";
            if (_employeeCode == null || _employee.Text != _employeeCode) return "사원증 스캔 필요";
            if (_weight.Value <= 0) return "총중량 입력 필요";
            if (!PalletRules.WeightInRange(_weight.Value))
                return "총중량 " + PalletRules.FormatKg(_weight.Value) + " kg · 허용 " + PalletRules.FormatKg(PalletRules.MinKg) + " ~ " + PalletRules.FormatKg(PalletRules.MaxKg);
            return "";
        }

        private void Evaluate()
        {
            if (_note == null) return;
            string reason = BlockReason();
            _confirm.Enabled = reason.Length == 0;
            _note.Text = reason;
        }

        private void Close_()
        {
            _weight.Commit();
            if (BlockReason().Length > 0) return;
            if (!ApcDialog.Confirm(this, "팔레트 " + _palletCode + "를 마감할까요?", "마감", ApcButtonVariant.Primary)) return;
            PalletLabel = _palletCode; EmployeeId = _employeeCode; TotalKg = _weight.Value;
            DialogResult = DialogResult.OK;
        }
    }
}
