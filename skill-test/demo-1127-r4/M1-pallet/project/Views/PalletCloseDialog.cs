using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PalletPacker.Data;

namespace PalletPacker.Views
{
    /// <summary>
    /// 팔레트 마감 dialog: 팔레트 라벨·사원증은 스캔만 받고(ScanOnly), 총중량은 측정값 입력.
    /// 박스 수 부족·총중량 범위 밖·미입력이면 마감 버튼을 막고 이유를 버튼 왼쪽에 보인다.
    /// 박스 수는 호출한 화면이 준 값을 읽기 전용으로만 보인다(여기서 바꿀 수 없음).
    /// </summary>
    internal sealed class PalletCloseDialog : ApcForm
    {
        private static readonly Regex PalletPattern = new Regex(@"^PLT-\d{6}-\d{3}$");
        private static readonly Regex BadgePattern = new Regex(@"^EMP-\d{5}$");

        private readonly int _boxCount, _target;
        private readonly ApcScanBox _pallet = new ApcScanBox { ScanOnly = true };
        private readonly ApcScanBox _badge = new ApcScanBox { ScanOnly = true };
        private readonly ApcNumericBox _weight = new ApcNumericBox { DecimalPlaces = 1, Minimum = 0m, Maximum = 2000m, Increment = 0.5m };
        private readonly ApcButton _ok = new ApcButton { Text = "마감", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;

        public string PalletLabel { get { return _pallet.Text; } }
        public string BadgeCode { get { return _badge.Text; } }
        public decimal TotalKg { get { return _weight.Value; } }

        // 샘플: 21/40 박스 — 마감이 막힌 상태
        public PalletCloseDialog() : this(21, PalletRules.TargetBoxes) { }

        public PalletCloseDialog(int boxCount, int target)
        {
            _boxCount = boxCount; _target = target;
            SetupDialog("팔레트 마감", 640);

            var info = new ApcInfoStrip();
            info.SetItems("박스 수", "기준 총중량(kg)");
            info.SetValue("박스 수", boxCount + " / " + target);
            info.SetValue("기준 총중량(kg)", PalletRules.NominalKg.ToString("0.0"));
            AddContent(info);

            AddFieldRow("팔레트 라벨", _pallet);
            AddFieldRow("작업자 사원증", _badge);
            _weight.Clear();   // 측정값은 비운 채 시작(구독 전)
            _weight.Width = ApcTheme.Px(160);
            AddFieldRow("총중량 (kg)", _weight, FieldCaption("허용 " + PalletRules.MinKg.ToString("0.0") + " ~ " + PalletRules.MaxKg.ToString("0.0")));

            var cancel = new ApcButton { Text = "취소" };
            SetFooter(cancel, _ok);
            _note = SetFooterNote("");

            _pallet.Accepts = c => c.StartsWith("PLT-");
            _badge.Accepts = c => c.StartsWith("EMP-");
            _pallet.Attach(this);
            _badge.Attach(this);
            _pallet.Scanned += (s, e) => OnLabel(e, PalletPattern, "팔레트 라벨 형식 오류 · PLT-yyMMdd-nnn", _badge);
            _badge.Scanned += (s, e) => OnLabel(e, BadgePattern, "사원증 형식 오류 · EMP-nnnnn", _weight);
            _weight.ValueChanged += (s, e) => Refresh2();
            _ok.Click += (s, e) => Confirm();
            Refresh2();
        }

        private void OnLabel(ScanEventArgs e, Regex pattern, string error, Control next)
        {
            e.Accepted = pattern.IsMatch(e.Code);
            if (!e.Accepted) { BeginInvoke(new System.Action(() => ApcDialog.Warn(this, error))); return; }
            next.Focus();
            Refresh2();
        }

        /// <summary>마감을 막는 이유(가장 먼저 걸리는 것 하나). 없으면 null.</summary>
        private string BlockReason()
        {
            if (_boxCount < _target) return "박스 부족 · " + _boxCount + "/" + _target;
            if (_pallet.Text.Length == 0) return "팔레트 라벨 스캔 필요";
            if (_badge.Text.Length == 0) return "사원증 스캔 필요";
            if (!_weight.HasValue) return "총중량 입력 필요";
            if (!PalletRules.WeightInRange(_weight.Value))
                return "총중량 범위 밖 · " + _weight.Value.ToString("0.0") + " kg";
            return null;
        }

        private void Refresh2()
        {
            string reason = BlockReason();
            _ok.Enabled = reason == null;
            _note.Text = reason ?? "";
            _note.ForeColor = reason != null && reason.StartsWith("총중량 범위") ? ApcTheme.StatusText(StatusKind.Alarm) : _note.ForeColor;
        }

        private void Confirm()
        {
            if (BlockReason() != null) return;
            if (!ApcDialog.Confirm(this, "팔레트를 마감합니다. 마감 후에는 박스를 더 쌓을 수 없습니다.", "마감", ApcButtonVariant.Primary)) return;
            DialogResult = DialogResult.OK;
        }
    }
}
