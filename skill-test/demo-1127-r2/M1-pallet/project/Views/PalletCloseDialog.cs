using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PalletPacker.Data;

namespace PalletPacker.Views
{
    /// <summary>팔레트 마감: 팔레트 라벨·사원증 스캔 + 총중량 입력. 박스 수 미달·중량 범위 밖이면 마감 불가.</summary>
    internal sealed class PalletCloseDialog : ApcForm
    {
        private static readonly Regex PalletFormat = new Regex(@"^PLT-\d{6}-\d{3}$");
        private static readonly Regex BadgeFormat = new Regex(@"^EMP-\d{5}$");

        private readonly int _boxCount, _targetBoxes;
        private readonly ApcScanBox _pallet = new ApcScanBox();
        private readonly ApcScanBox _badge = new ApcScanBox();
        private readonly ApcNumericBox _weight = new ApcNumericBox { DecimalPlaces = 1, Minimum = 0, Maximum = 2000, Increment = 0.5m };
        private readonly ApcButton _confirm = new ApcButton { Text = "마감", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;
        private readonly System.Drawing.Color _noteColor;
        private string _scannedPallet = "", _scannedBadge = "";

        public string PalletLabel { get { return _pallet.Text; } }
        public string Badge { get { return _badge.Text; } }
        public decimal TotalKg { get { return _weight.Value; } }

        public PalletCloseDialog() : this(21, PalletRules.TargetBoxes) { }

        public PalletCloseDialog(int boxCount, int targetBoxes)
        {
            _boxCount = boxCount; _targetBoxes = targetBoxes;
            SetupDialog("팔레트 마감", 620);

            var info = new ApcInfoStrip();
            info.SetItems("박스 수", "허용 총중량");
            info.SetValue("박스 수", boxCount + " / " + targetBoxes);
            info.SetValue("허용 총중량", PalletRules.WeightMinKg.ToString("0.0", CultureInfo.InvariantCulture)
                + " ~ " + PalletRules.WeightMaxKg.ToString("0.0", CultureInfo.InvariantCulture) + " kg");
            AddContent(info);

            _pallet.Accepts = c => c.StartsWith("PLT-", StringComparison.Ordinal);
            _badge.Accepts = c => c.StartsWith("EMP-", StringComparison.Ordinal);
            _pallet.Attach(this);
            _badge.Attach(this);
            _pallet.Scanned += (s, e) => { e.Accepted = Take(e.Code, PalletFormat, ref _scannedPallet, _pallet); };
            _badge.Scanned += (s, e) => { e.Accepted = Take(e.Code, BadgeFormat, ref _scannedBadge, _badge); };
            // 손으로 고친 글자는 스캔 값이 아니다 → 다시 스캔할 때까지 미확인
            _pallet.TextChanged += (s, e) => ShowState();
            _badge.TextChanged += (s, e) => ShowState();

            _weight.Clear();
            _weight.ValueChanged += (s, e) => ShowState();
            _weight.Leave += (s, e) => ShowState();

            AddFieldRow("팔레트 라벨", _pallet);
            AddFieldRow("작업자 사원증", _badge);
            AddFieldRow("총중량 (kg)", _weight);

            _confirm.Click += (s, e) => Confirm();
            var cancel = new ApcButton { Text = "취소" };
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
            SetFooter(cancel, _confirm);
            _note = SetFooterNote("");
            _noteColor = _note.ForeColor;
            ShowState();
        }

        private bool Take(string code, Regex format, ref string slot, ApcScanBox box)
        {
            if (!format.IsMatch(code)) { slot = ""; return false; }
            slot = code;
            BeginInvoke((Action)ShowState);
            return true;
        }

        private string BlockReason()
        {
            if (_boxCount < _targetBoxes) return "박스 " + _boxCount + "/" + _targetBoxes + " · 목표 미달";
            if (!_weight.HasValue) return "총중량 입력 필요";
            if (_weight.Value > PalletRules.WeightMaxKg)
                return "상한 초과 " + _weight.Value.ToString("0.0", CultureInfo.InvariantCulture) + " kg (상한 "
                    + PalletRules.WeightMaxKg.ToString("0.0", CultureInfo.InvariantCulture) + ")";
            if (_weight.Value < PalletRules.WeightMinKg)
                return "하한 미달 " + _weight.Value.ToString("0.0", CultureInfo.InvariantCulture) + " kg (하한 "
                    + PalletRules.WeightMinKg.ToString("0.0", CultureInfo.InvariantCulture) + ")";
            if (_scannedPallet.Length == 0 || _pallet.Text != _scannedPallet) return "팔레트 라벨 스캔 필요";
            if (_scannedBadge.Length == 0 || _badge.Text != _scannedBadge) return "사원증 스캔 필요";
            return "";
        }

        private void ShowState()
        {
            string reason = BlockReason();
            _confirm.Enabled = reason.Length == 0;
            _note.Text = reason;
            bool alarm = reason.StartsWith("상한") || reason.StartsWith("하한") || reason.StartsWith("박스");
            _note.ForeColor = alarm ? ApcTheme.StatusText(StatusKind.Alarm) : _noteColor;
        }

        private void Confirm()
        {
            if (BlockReason().Length != 0) return;   // 버튼이 꺼져 있어도 이중으로 막는다
            if (!ApcDialog.Confirm(this, "팔레트를 마감합니다. 마감 후에는 되돌릴 수 없습니다.", "마감", ApcButtonVariant.Primary)) return;
            DialogResult = DialogResult.OK;
        }
    }
}
