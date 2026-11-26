using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace RawMaterialIssueTerminal.Views
{
    /// <summary>
    /// FIFO 예외 출고 승인. 원료·Lot 정보는 호출 측이 준 값을 읽기 전용으로 보여 주고,
    /// 입력은 예외 사유와 승인자 사번 스캔뿐이다. 유통기한이 지난 Lot은 승인이 있어도 확정할 수 없다.
    /// </summary>
    internal sealed class FifoApprovalDialog : ApcForm
    {
        private const string ReasonNone = "예외 사유를 선택하세요";
        private const string ApproverNone = "승인자 사번을 스캔하세요";
        private readonly ApcComboBox _reason = new ApcComboBox();
        private readonly ApcScanBox _approver = new ApcScanBox();
        private readonly ApcButton _ok = new ApcButton { Text = "예외 출고 승인", Variant = ApcButtonVariant.Danger };
        private readonly Label _note;
        private readonly bool _expired;
        private readonly Func<string, string> _findApprover;
        private string _approverNo = "", _approverName = "";

        // 샘플: 막힌 상태(사유·승인자 미입력)로 시작
        public FifoApprovalDialog() : this("RM-1021 소맥분", "L261110-02", DateTime.Today.AddDays(30), "L261105-01", DateTime.Today.AddDays(4),
            DateTime.Today, no => no == "E10231" ? "박승인" : null) { }

        public FifoApprovalDialog(string material, string scannedLot, DateTime scannedExpiry, string aheadLot, DateTime aheadExpiry,
            DateTime today, Func<string, string> findApprover)
        {
            _findApprover = findApprover;
            _expired = scannedExpiry.Date < today.Date;
            SetupDialog("FIFO 예외 출고 승인", 760);

            AddFieldRow("원료", ReadOnlyBox(material));
            AddFieldRow("스캔한 Lot", ReadOnlyBox(scannedLot + " · 유통기한 " + scannedExpiry.ToString("yyyy-MM-dd")));
            AddFieldRow("앞 순위 Lot", ReadOnlyBox(aheadLot + " · 유통기한 " + aheadExpiry.ToString("yyyy-MM-dd")));

            _reason.Items.AddRange(new object[] { "생산 긴급", "앞 순위 Lot 포대 손상", "앞 순위 Lot 잔량 부족", "품질팀 지시" });
            _reason.SelectedIndexChanged += (s, e) => UpdateState();
            AddFieldRow("예외 사유", _reason);

            _approver.Scanned += (s, e) => OnApproverScanned(e);
            _approver.TextChanged += (s, e) => { if (_approver.Text != _approverNo) ClearApprover(); };
            AddFieldRow("승인자 사번 (스캔)", _approver);

            _ok.Click += (s, e) => DialogResult = DialogResult.OK;
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _ok);   // Enter는 스캔이 쓴다 → AcceptButton 없음
            _note = SetFooterNote();
            UpdateState();
        }

        public string Reason { get { return Convert.ToString(_reason.SelectedItem); } }
        public string ApproverNo { get { return _approverNo; } }
        public string ApproverName { get { return _approverName; } }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _approver.Attach(this);
        }

        private static ApcTextBox ReadOnlyBox(string text) { return new ApcTextBox { Text = text, ReadOnly = true }; }

        private void OnApproverScanned(ScanEventArgs e)
        {
            string name = _findApprover(e.Code);
            if (name == null) { e.Accepted = false; ClearApprover(); _note.Text = "승인 권한이 없는 사번 · " + e.Code; _note.ForeColor = ApcTheme.StatusText(StatusKind.Alarm); return; }
            _approverNo = e.Code;
            _approverName = name;
            BeginInvoke((Action)UpdateState);
        }

        private void ClearApprover() { _approverNo = ""; _approverName = ""; UpdateState(); }

        // 확정 조건: 기한 안 지남 + 사유 선택 + 승인자 확인. 사유는 막힌 이유 한 줄로 보여 준다.
        private void UpdateState()
        {
            if (_note == null) return;
            string why = _expired ? "유통기한 경과 Lot · 승인해도 출고 불가"
                       : _reason.SelectedIndex < 0 ? ReasonNone
                       : _approverName.Length == 0 ? ApproverNone : "";
            _ok.Enabled = why.Length == 0;
            _note.Text = why.Length == 0 ? "승인자 " + _approverName + " (" + _approverNo + ")" : why;
            _note.ForeColor = _expired ? ApcTheme.StatusText(StatusKind.Alarm) : ApcTheme.C(ApcTheme.Current.Colors.Text);
        }
    }
}
