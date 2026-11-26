using System;
using System.Windows.Forms;
using APC.UI.Controls;
using CaliperTighteningStation.Data;

namespace CaliperTighteningStation.Views
{
    /// <summary>인터락 우회 승인: 관리자 비밀번호 확인 → 우회 항목·시간 범위·사유. 확정하면 Item/Minutes/Reason에 담고 OK.</summary>
    internal sealed class BypassApprovalDialog : ApcForm
    {
        private static readonly string[] RangeText = { "30분", "1시간", "2시간", "4시간", "8시간" };
        private static readonly int[] RangeMinutes = { 30, 60, 120, 240, 480 };

        private readonly Func<string, bool> _verify;
        private readonly ApcTextBox _password = new ApcTextBox { UseSystemPasswordChar = true, MaxLength = 32 };
        private readonly ApcComboBox _item = new ApcComboBox();
        private readonly ApcComboBox _range = new ApcComboBox();
        private readonly ApcTextBox _reason = new ApcTextBox { MaxLength = 100 };

        public BypassApprovalDialog() : this(p => p.Length > 0, "E05", "초품 검사 진행 중, 4M 승인 대기") { }

        public BypassApprovalDialog(Func<string, bool> verifyAdmin, string item, string reason)
        {
            _verify = verifyAdmin;
            SetupDialog("인터락 우회 승인", 560);

            _item.Items.AddRange(Spec.BypassItems);
            _item.SelectedItem = item;
            _range.Items.AddRange(RangeText);
            _range.SelectedIndex = 2;
            _reason.Text = reason;
            _password.Text = "";

            AddContent(SectionTitle("관리자 확인"));
            AddFieldRow("비밀번호", _password);
            AddContent(SectionTitle("우회 범위"));
            AddFieldRow("우회 항목", _item);
            AddFieldRow("시간 범위", _range);
            AddFieldRow("사유 (필수)", _reason);

            var ok = new ApcButton { Text = "우회 승인", Variant = ApcButtonVariant.Danger };
            ok.Click += (s, e) => Approve();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, ok);   // Enter 기본 버튼 없음: 실수로 우회되지 않게
            Shown += (s, e) => _password.Focus();
        }

        public string Item { get { return (string)_item.SelectedItem; } }
        public int Minutes { get { return RangeMinutes[_range.SelectedIndex]; } }
        public string Reason { get { return _reason.Text.Trim(); } }

        private void Approve()
        {
            if (_password.Text.Length == 0) { Reject("관리자 비밀번호를 입력하세요.", _password); return; }
            if (!_verify(_password.Text)) { Reject("비밀번호가 맞지 않습니다. 권한자만 우회할 수 있습니다.", _password); return; }
            if (_reason.Text.Trim().Length == 0) { Reject("우회 사유를 입력하세요.", _reason); return; }
            string msg = Item + " 인터락을 " + RangeText[_range.SelectedIndex] + " 동안 우회할까요?\n우회 중 생산한 시리얼은 추가 검사 대상이 됩니다.";
            if (!ApcDialog.Confirm(this, msg, "우회 승인", ApcButtonVariant.Danger)) return;
            DialogResult = DialogResult.OK;
        }

        private void Reject(string message, ApcTextBox field)
        {
            ApcDialog.Warn(this, message);
            field.Focus();
            field.SelectAll();
        }
    }
}
