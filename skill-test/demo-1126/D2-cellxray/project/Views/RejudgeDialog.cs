using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CellXrayJudgeStation.Data;

namespace CellXrayJudgeStation.Views
{
    /// <summary>NG·재판정 대기 셀 재판정. 측정값·자동 사유는 읽기 전용. 규격 미달 셀은 OK 항목 자체가 없다.</summary>
    internal sealed class RejudgeDialog : ApcForm
    {
        private const string OkText = "OK", NgText = "NG";
        private readonly CellRecord _cell;
        private readonly ApcComboBox _verdict = new ApcComboBox();
        private readonly ApcComboBox _ngType = new ApcComboBox();
        private readonly ApcTextBox _reason = new ApcTextBox();

        // 샘플(render): 오버행 미달로 OK 불가인 셀
        public RejudgeDialog() : this(new SampleMainViewSource().GetCurrentTray().Cells[6]) { }

        public RejudgeDialog(CellRecord cell)
        {
            _cell = cell;
            SetupDialog("NG 셀 재판정", 680);

            string blocked = cell.OkBlockedReason;
            if (blocked != null)
                AddContent(new ApcNoticeBar { Status = StatusKind.Alarm, Text = "OK 불가", Detail = blocked, Height = ApcTheme.Px(44) });

            var info = new ApcInfoStrip { Height = ApcTheme.Px(40) };
            info.SetItems("셀 ID", "칸 번호", "자동 판정");
            info.SetValue("셀 ID", cell.CellId);
            info.SetValue("칸 번호", cell.Position.ToString());
            info.SetValue("자동 판정", CellSpec.Word(cell.Auto));
            AddContent(info);

            var autoReason = new ApcTextBox { ReadOnly = true, Text = cell.AutoReason };
            AddFieldRow("자동 판정 사유", autoReason);

            var table = MeasureTable.Create();
            MeasureTable.Fill(table, cell);
            table.Height = ApcGrid.HeightForRows(MeasureTable.RowCount);
            AddContent(SectionTitle("측정값 (읽기 전용)"));
            AddContent(table);

            // 규격 미달이면 목록에 OK가 없다 → 화면에서 바꿔도 OK로 풀 수 없다
            _verdict.Items.Add(NgText);
            if (blocked == null) _verdict.Items.Insert(0, OkText);
            _ngType.Items.AddRange(CellSpec.NgTypes);
            _verdict.SelectedIndexChanged += (s, e) => SyncNgType();
            _verdict.SelectedIndex = blocked == null && cell.Auto == CellVerdict.Pending ? -1 : _verdict.Items.IndexOf(NgText);
            if (_verdict.SelectedIndex == _verdict.Items.IndexOf(NgText)) _ngType.SelectedIndex = SuggestedType(cell);
            SyncNgType();

            AddContent(SectionTitle("재판정"));
            AddFieldRow("판정", _verdict);
            AddFieldRow("NG 유형", _ngType);
            AddFieldRow("사유", _reason);

            var save = new ApcButton { Text = "확정", Variant = ApcButtonVariant.Primary };
            save.Click += (s, e) => Save();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, save);
        }

        public CellVerdict Result { get; private set; }
        public string NgType { get; private set; }
        public string Reason { get; private set; }

        private static int SuggestedType(CellRecord c)
        {
            if (CellSpec.Overhang(c.OverhangLow) == CellVerdict.Ng) return 0;
            if (CellSpec.Align(c.Align) == CellVerdict.Ng) return 1;
            return c.Foreign ? 2 : -1;
        }

        private bool IsNg { get { return (string)_verdict.SelectedItem == NgText; } }

        private void SyncNgType()
        {
            _ngType.Enabled = IsNg;
            if (!IsNg) _ngType.SelectedIndex = -1;
        }

        private void Save()
        {
            if (_verdict.SelectedItem == null) { ApcDialog.Warn(this, "판정(OK/NG)을 선택하세요."); return; }
            if (IsNg && _ngType.SelectedItem == null) { ApcDialog.Warn(this, "NG 유형을 선택하세요."); return; }
            if (!IsNg && _cell.OkBlockedReason != null) { ApcDialog.Warn(this, "OK로 바꿀 수 없습니다. " + _cell.OkBlockedReason); return; }
            if (!ApcDialog.Confirm(this, "셀 " + _cell.CellId + "을(를) " + (string)_verdict.SelectedItem + "로 확정할까요?", "확정", ApcButtonVariant.Primary)) return;
            Result = IsNg ? CellVerdict.Ng : CellVerdict.Ok;
            NgType = IsNg ? (string)_ngType.SelectedItem : null;
            Reason = _reason.Text.Trim();
            DialogResult = DialogResult.OK;
        }
    }
}
