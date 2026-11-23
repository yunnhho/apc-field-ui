using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;

namespace CellLeakTester.Views
{
    /// <summary>LOT 입력 dialog: LOT 번호·모델·작업자. 저장 = 검증(Warn) → Confirm → 결과 속성 → OK.</summary>
    internal sealed class LotEntryDialog : ApcForm
    {
        private readonly ApcTextBox _lot = new ApcTextBox { MaxLength = 20 };
        private readonly ApcComboBox _model = new ApcComboBox();
        private readonly ApcTextBox _worker = new ApcTextBox { MaxLength = 20 };

        public LotEntryDialog() : this("LOT-261120-03", new[] { "CL-2170-A", "CL-2170-B", "CL-4680" }, "CL-2170-A", "김현장") { }

        public LotEntryDialog(string lot, IList<string> models, string model, string worker)
        {
            SetupDialog("LOT 입력", 460);

            _lot.Text = lot;
            foreach (var m in models) _model.Items.Add(m);
            _model.SelectedItem = model;
            _worker.Text = worker;

            AddFieldRow("LOT 번호", _lot);
            AddFieldRow("모델", _model);
            AddFieldRow("작업자", _worker);

            var save = new ApcButton { Text = "저장", Variant = ApcButtonVariant.Primary };
            save.Click += (s, e) => Save();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, save);
        }

        public string Lot { get; private set; }
        public string Model { get; private set; }
        public string Worker { get; private set; }

        private void Save()
        {
            string error = _lot.Text.Trim().Length == 0 ? "LOT 번호를 입력하세요."
                         : _model.SelectedItem == null ? "모델을 선택하세요."
                         : _worker.Text.Trim().Length == 0 ? "작업자를 입력하세요." : null;
            if (error != null) { ApcDialog.Warn(this, error); return; }
            if (!ApcDialog.Confirm(this, "LOT '" + _lot.Text.Trim() + "'를 저장할까요?", "저장", ApcButtonVariant.Primary)) return;

            Lot = _lot.Text.Trim();
            Model = (string)_model.SelectedItem;
            Worker = _worker.Text.Trim();
            DialogResult = DialogResult.OK;
        }
    }
}
