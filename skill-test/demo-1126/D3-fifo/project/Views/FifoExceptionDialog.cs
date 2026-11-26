using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using RawMaterialIssueTerminal.Data;

namespace RawMaterialIssueTerminal.Views
{
    /// <summary>
    /// FIFO 예외 출고 승인. 원료·스캔한 Lot·앞 순위 Lot는 읽기 전용(호출 화면이 준다).
    /// 이 dialog가 하는 일은 예외 사유 선택과 승인자 사번 스캔뿐이다. 유통기한이 지난 Lot은 승인 불가(승인 버튼이 꺼진 채 이유만 보인다).
    /// </summary>
    internal sealed class FifoExceptionDialog : ApcForm
    {
        private static readonly string[] Reasons =
        {
            "앞 순위 Lot 포장 훼손", "앞 순위 Lot 잔량 부족", "품질팀 출고 지시", "앞 순위 Lot 분석 대기",
        };

        private readonly ApcComboBox _reason = new ApcComboBox();
        private readonly ApcScanBox _approverScan = new ApcScanBox();
        private readonly ApcTextBox _approverName = new ApcTextBox { ReadOnly = true };
        private readonly ApcButton _approve = new ApcButton { Text = "승인", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;
        private readonly Func<string, string> _findApprover;
        private readonly bool _expired;

        // 샘플: 사유·승인자 미입력 = 막힌 상태
        public FifoExceptionDialog() : this("RM-1021 소맥분", "L261120-03", new DateTime(2027, 5, 20), "L261108-01", new DateTime(2026, 12, 2),
            new DateTime(2026, 11, 26), code => code == "EMP-2041" ? "김관리" : null) { }

        public FifoExceptionDialog(string material, string scannedLot, DateTime scannedExpiry, string frontLot, DateTime frontExpiry,
            DateTime today, Func<string, string> findApprover)
        {
            _findApprover = findApprover;
            _expired = scannedExpiry.Date < today.Date;
            SetupDialog("FIFO 예외 출고 승인", 760);

            AddFieldRow("원료", Readonly(material));
            AddFieldRow("스캔한 Lot", Readonly(scannedLot + " · 유통기한 " + scannedExpiry.ToString("yyyy-MM-dd")));
            AddFieldRow("앞 순위 Lot", Readonly(frontLot + " · 유통기한 " + frontExpiry.ToString("yyyy-MM-dd")));

            _reason.Items.AddRange(Reasons);
            AddFieldRow("예외 사유", _reason);
            AddFieldRow("승인자 사번 (스캔)", _approverScan);
            AddFieldRow("승인자", _approverName);

            _approverScan.Scanned += (s, e) => e.Accepted = AcceptApprover(e.Code);
            _reason.SelectedIndexChanged += (s, e) => UpdateState();
            _approve.Click += (s, e) => { DialogResult = DialogResult.OK; };   // 막힌 상태에서는 Enabled = false
            _note = SetFooterNote("");
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _approve);   // Enter는 스캔이 쓴다 → AcceptButton 없음
            UpdateState();
        }

        public string Reason { get { return Convert.ToString(_reason.SelectedItem); } }
        public string ApproverCode { get; private set; }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _approverScan.Attach(this);
        }

        private static ApcTextBox Readonly(string text) { return new ApcTextBox { ReadOnly = true, Text = text }; }

        private bool AcceptApprover(string code)
        {
            string name = _findApprover(code.Trim());
            if (name == null)
            {
                BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(this, "승인 권한이 없는 사번입니다.")));
                return false;
            }
            ApproverCode = code.Trim();
            _approverScan.Text = ApproverCode;
            _approverName.Text = name;
            UpdateState();
            return true;
        }

        // 막는 이유를 한 곳에서: 경과 > 사유 미선택 > 승인자 미스캔
        private void UpdateState()
        {
            string why = _expired ? "유통기한 경과 · 승인 불가"
                : _reason.SelectedIndex < 0 ? "예외 사유를 선택하세요"
                : ApproverCode == null ? "승인자 사번을 스캔하세요" : "";
            _approve.Enabled = why.Length == 0;
            _note.Text = why;
            _note.ForeColor = _expired ? ApcTheme.StatusText(StatusKind.Alarm) : _note.ForeColor;
        }
    }
}
