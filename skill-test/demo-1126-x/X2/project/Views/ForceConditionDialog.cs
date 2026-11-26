using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using Polish.Data;

namespace Polish.Views
{
    /// <summary>레시피가 없거나 다른 조건으로 진행할 때 담당자가 조건을 직접 넣는 dialog(비밀번호 확인 뒤에만 열린다).</summary>
    internal sealed class ForceConditionDialog : ApcForm
    {
        private const int NumberWidth = 110, MaxHours = 12;
        private static readonly string[] Kinds = { "그린칩", "소성칩" };

        private readonly ApcComboBox _kind = new ApcComboBox { Width = ApcTheme.Px(NumberWidth) };
        private readonly ApcTextBox _material = new ApcTextBox { MaxLength = 30 };
        private readonly ApcNumericBox _hours = Number(0, MaxHours), _minutes = Number(0, 59);

        public ForceConditionDialog() : this("그린칩", "Q3216X7R105K101NRINN02F", 3600) { }

        public ForceConditionDialog(string kind, string material, int targetSeconds)
        {
            SetupDialog("조건 강제 입력", 520);
            _kind.Items.AddRange(Kinds);
            _kind.SelectedIndex = kind == Kinds[1] ? 1 : 0;
            _material.Text = material;
            _hours.Value = targetSeconds / 3600;
            _minutes.Value = targetSeconds / 60 % 60;

            AddFieldRow("구분", _kind);
            AddFieldRow("자재코드", _material);
            AddFieldRow("목표 시간", _hours, FieldCaption("시간"), _minutes, FieldCaption("분"));

            var ok = new ApcButton { Text = "확인", Variant = ApcButtonVariant.Primary };
            ok.Click += (s, e) => Save();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, ok);
        }

        public PolishCondition Condition { get; private set; }

        private void Save()
        {
            _hours.Commit(); _minutes.Commit();
            int seconds = (int)(_hours.Value * 3600 + _minutes.Value * 60);
            if (_material.Text.Trim().Length == 0) { Reject("자재코드를 입력하세요.", _material); return; }
            if (seconds <= 0) { Reject("목표 시간을 입력하세요.", _hours.TextBox); return; }
            Condition = new PolishCondition(_kind.SelectedItem.ToString(), _material.Text.Trim(), seconds);
            DialogResult = DialogResult.OK;
        }

        private void Reject(string message, ApcTextBox field)
        {
            ApcDialog.Warn(this, message);
            field.Focus();
            field.SelectAll();
        }

        private static ApcNumericBox Number(decimal min, decimal max)
        {
            return new ApcNumericBox { Minimum = min, Maximum = max, DecimalPlaces = 0, Increment = 1, Width = ApcTheme.Px(NumberWidth) };
        }
    }
}
