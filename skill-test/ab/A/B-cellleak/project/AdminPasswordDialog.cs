using System.Windows.Forms;
using APC.UI.Controls;

namespace CellLeakTester
{
    /// <summary>관리자 확인 dialog. 입력값 확인은 부른 쪽이 한다. Enter = 확인.</summary>
    internal sealed class AdminPasswordDialog : ApcForm
    {
        private readonly ApcTextBox _password = new ApcTextBox { UseSystemPasswordChar = true, MaxLength = 32 };

        public AdminPasswordDialog()
        {
            SetupDialog("관리자 확인", 400);
            AddFieldRow("비밀번호", _password);

            var ok = new ApcButton { Text = "확인", Variant = ApcButtonVariant.Primary, DialogResult = DialogResult.OK };
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, ok);
            AcceptButton = ok;
            Shown += (s, e) => _password.Focus();
        }

        public string Password { get { return _password.Text; } }
    }
}
