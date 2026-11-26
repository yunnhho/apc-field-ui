using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CellXrayJudgeStation.Data;

namespace CellXrayJudgeStation.Views
{
    /// <summary>
    /// NG·재판정 대기 셀 재판정. 셀 ID·자동 판정·사유·측정값은 읽기 전용.
    /// 오버행 최소값이 하한 미만이면 OK를 고를 수 없다(확정 버튼이 꺼지고 이유가 보인다).
    /// </summary>
    internal sealed class RejudgeDialog : ApcForm
    {
        private const int OkIndex = 0, NgIndex = 1;
        private readonly CellRecord _cell;
        private readonly ApcComboBox _verdict = new ApcComboBox();
        private readonly ApcComboBox _type = new ApcComboBox();
        private readonly ApcTextBox _reason = new ApcTextBox();
        private readonly ApcButton _confirm = new ApcButton { Text = "확정", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;
        private readonly System.Drawing.Color _noteColor;

        /// <summary>render용: 오버행 0.42 mm 셀에서 OK가 골라진 막힌 상태.</summary>
        public RejudgeDialog() : this(SampleCell(), "TR-A0153", OkIndex) { }

        public RejudgeDialog(CellRecord cell, string trayId) : this(cell, trayId, cell.State == CellState.Ng ? NgIndex : -1) { }

        private RejudgeDialog(CellRecord cell, string trayId, int initialVerdict)
        {
            _cell = cell;
            SetupDialog("셀 재판정", 640);

            var info = new ApcInfoStrip();
            info.SetItems("셀 ID", "트레이", "자동 판정");
            info.SetValue("셀 ID", cell.CellId);
            info.SetValue("트레이", trayId);
            info.SetValue("자동 판정", CellJudge.StateWord(cell.State));
            info.Height = ApcTheme.Px(40);
            AddContent(info);
            var autoReason = new ApcTextBox { ReadOnly = true, Text = cell.Reason };
            AddFieldRow("자동 판정 사유", autoReason);

            AddContent(SectionTitle("측정값"));
            var table = CellMeasureTable.Create();
            table.Dock = DockStyle.None;
            table.Height = ApcGrid.HeightForRows(CellMeasureTable.RowCount);
            CellMeasureTable.Fill(table, cell);
            AddContent(table);

            AddContent(SectionTitle("재판정"));
            _verdict.Items.AddRange(new object[] { "OK", "NG" });
            _type.Items.AddRange(CellJudge.NgTypes);
            _verdict.SelectedIndex = initialVerdict;
            if (initialVerdict == NgIndex && cell.NgType != null) _type.SelectedItem = cell.NgType;
            AddFieldRow("판정", _verdict);
            AddFieldRow("NG 유형", _type);
            AddFieldRow("사유", _reason);

            _note = SetFooterNote("");
            _noteColor = _note.ForeColor;
            _confirm.Click += (s, e) => Confirm();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm);   // 스캔 Enter로 확정되지 않게 기본 버튼 없음

            _verdict.SelectedIndexChanged += (s, e) => { if (_verdict.SelectedIndex != NgIndex) _type.SelectedIndex = -1; Update2(); };
            _type.SelectedIndexChanged += (s, e) => Update2();
            _reason.TextChanged += (s, e) => Update2();
            Update2();
        }

        public CellState ResultState { get; private set; }
        public string ResultNgType { get; private set; }
        public string ResultReason { get; private set; }

        // 확정 가능 여부와 막는 이유. 하한 미만 셀의 OK는 화면이 막는다.
        private string BlockReason()
        {
            if (_verdict.SelectedIndex < 0) return "판정을 선택하세요";
            if (_verdict.SelectedIndex == OkIndex)
                return _cell.MinOverhang < CellJudge.OverhangMin
                    ? "오버행 " + CellJudge.F(_cell.MinOverhang) + " mm < 하한 " + CellJudge.F(CellJudge.OverhangMin) + " · OK 불가"
                    : "";
            if (_type.SelectedIndex < 0) return "NG 유형을 선택하세요";
            if ((string)_type.SelectedItem == CellJudge.NgOther && _reason.Text.Trim().Length == 0) return "유형 '기타'는 사유 입력 필요";
            return "";
        }

        private void Update2()
        {
            _type.Enabled = _verdict.SelectedIndex == NgIndex;
            string block = BlockReason();
            _confirm.Enabled = block.Length == 0;
            _note.Text = block;
            _note.ForeColor = _verdict.SelectedIndex == OkIndex && block.Length > 0 ? ApcTheme.StatusText(StatusKind.Alarm) : _noteColor;
        }

        private void Confirm()
        {
            if (BlockReason().Length > 0) return;   // 버튼이 꺼져 있어 오지 않지만 한 번 더 막는다
            bool ng = _verdict.SelectedIndex == NgIndex;
            ResultState = ng ? CellState.Ng : CellState.Ok;
            ResultNgType = ng ? (string)_type.SelectedItem : null;
            ResultReason = _reason.Text.Trim();
            DialogResult = DialogResult.OK;
        }

        private static CellRecord SampleCell()
        {
            var c = new CellRecord { Slot = 9, CellId = "C2611260309", Align = 0.14 };
            double[] v = { 0.49, 0.42, 0.47, 0.44 };
            Array.Copy(v, c.Overhang, v.Length);
            CellJudge.Apply(c);
            return c;
        }
    }
}
