using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PalletPacker.Data;

namespace PalletPacker.Views
{
    /// <summary>팔레트 마감: 팔레트 라벨·사원증 스캔 + 총중량 입력. 박스 수 부족·중량 범위 밖이면 확정을 막는다.</summary>
    internal sealed class PalletCloseDialog : ApcForm
    {
        private static readonly Regex PalletLabel = new Regex(@"^PLT-\d{6}-\d{3}$");
        private static readonly Regex Badge = new Regex(@"^EMP-\d{5}$");

        private readonly int _boxes, _target;
        private readonly ApcScanBox _pallet = new ApcScanBox { ScanOnly = true, Accepts = c => c.StartsWith("PLT-") };
        private readonly ApcScanBox _badge = new ApcScanBox { ScanOnly = true, Accepts = c => c.StartsWith("EMP-") };
        private readonly ApcNumericBox _weight = new ApcNumericBox { DecimalPlaces = 1, Minimum = 0, Maximum = 2000, Increment = 0.5m, Width = ApcTheme.Px(160) };
        private readonly ApcButton _ok = new ApcButton { Text = "마감", Variant = ApcButtonVariant.Primary, Enabled = false };
        private readonly Label _note;

        // 샘플: 3층 5번째까지 쌓은 상태(21/40) → 막힌 상태로 시작
        public PalletCloseDialog() : this(21, 40) { }

        public PalletCloseDialog(int boxes, int targetBoxes)
        {
            _boxes = boxes;
            _target = targetBoxes;
            SetupDialog("팔레트 마감", 620);

            var info = new ApcInfoStrip();
            info.SetItems("박스 수", "목표", "총중량 기준");
            info.SetValue("박스 수", boxes.ToString());
            info.SetValue("목표", targetBoxes.ToString());
            info.SetValue("총중량 기준", PalletRules.MinKg(targetBoxes).ToString("0.0", CultureInfo.InvariantCulture) + " ~ " + PalletRules.Kg(PalletRules.MaxKg(targetBoxes)));
            AddContent(info);

            _weight.Clear();   // 측정값은 빈칸으로 시작 — ValueChanged는 그 뒤에 구독
            AddFieldRow("팔레트 라벨", _pallet);
            AddFieldRow("사원증", _badge);
            AddFieldRow("총중량 (kg)", _weight);

            _pallet.Attach(this);
            _badge.Attach(this);
            _pallet.Scanned += (s, e) => OnScan(e, PalletLabel, "팔레트 라벨 형식 오류 · " + e.Code, _badge);
            _badge.Scanned += (s, e) => OnScan(e, Badge, "사원증 형식 오류 · " + e.Code, _weight);
            _weight.ValueChanged += (s, e) => Refresh2();
            _ok.Click += (s, e) => Close2();

            _note = SetFooterNote("");
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _ok);   // Enter는 스캔이 쓴다
            Refresh2();
        }

        public string PalletLabelCode { get { return _pallet.Text; } }
        public string BadgeCode { get { return _badge.Text; } }
        public decimal TotalKg { get { return _weight.Value; } }

        private void OnScan(ScanEventArgs e, Regex format, string reason, Control next)
        {
            e.Accepted = format.IsMatch(e.Code);
            if (e.Accepted) { Refresh2(); next.Focus(); }
            else BeginInvoke(new Action(() => ApcDialog.Warn(this, reason)));
        }

        // 마감을 막는 이유를 한 곳(하단 안내 글)에 보인다. 우선순위: 박스 수 → 라벨 → 사원증 → 중량 입력 → 중량 범위
        private void Refresh2()
        {
            decimal? w = _weight.HasValue ? _weight.Value : (decimal?)null;
            string reason = PalletRules.CloseBlockReason(_boxes, _target, null);
            if (reason == null && !PalletLabel.IsMatch(_pallet.Text)) reason = "팔레트 라벨 스캔 필요";
            if (reason == null && !Badge.IsMatch(_badge.Text)) reason = "사원증 스캔 필요";
            if (reason == null && w == null) reason = "총중량 입력 필요";
            if (reason == null) reason = PalletRules.CloseBlockReason(_boxes, _target, w);
            _ok.Enabled = reason == null;
            _note.Text = reason ?? "";
            _note.ForeColor = reason != null && (reason.StartsWith("박스") || reason.StartsWith("총중량 ") && w != null)
                ? ApcTheme.StatusText(StatusKind.Alarm) : ApcTheme.C(ApcTheme.Current.Colors.Text);
        }

        private void Close2()
        {
            _weight.Commit();
            Refresh2();
            if (!_ok.Enabled) return;
            DialogResult = DialogResult.OK;
        }
    }
}
