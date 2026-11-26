using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace BumperPaintBoothMonitor.Views
{
    /// <summary>
    /// 이탈 조치 등록. 이탈 내역(부스·항목·구간·영향 행거)은 읽기 전용, 입력은 조치 구분·확인자·비고뿐.
    /// 이탈이 끝나지 않았으면 "영향 없음"으로 닫을 수 없다(확정 버튼 꺼짐 + 이유).
    /// </summary>
    internal sealed class ActionDialog : ApcForm
    {
        public const string Repaint = "재도장", Inspect = "추가 검사", NoImpact = "영향 없음";

        private readonly bool _open;
        private readonly ApcComboBox _action = new ApcComboBox();
        private readonly ApcScanBox _confirmer = new ApcScanBox();
        private readonly ApcTextBox _remark = new ApcTextBox();
        private readonly ApcButton _register = new ApcButton { Text = "등록", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;

        public ActionDialog() : this("클리어", "습도 (%RH)", "14:46:10 ~ 이탈 중 (현재 00:06:20)",
            "H-0636 ~ H-0640 (5개)", "클리어 백화(blushing)", true, NoImpact) { }

        public ActionDialog(string booth, string item, string interval, string hangers, string risk, bool open, string initialAction)
        {
            _open = open;
            SetupDialog("이탈 조치 등록", 640);

            AddContent(SectionTitle("이탈 내역"));
            AddFieldRow("부스", ReadOnlyBox(booth));
            AddFieldRow("항목", ReadOnlyBox(item));
            AddFieldRow("이탈 구간", ReadOnlyBox(interval));
            AddFieldRow("영향 행거", ReadOnlyBox(hangers));
            AddFieldRow("예상 불량", ReadOnlyBox(risk));

            AddContent(SectionTitle("조치"));
            _action.Items.AddRange(new object[] { Repaint, Inspect, NoImpact });
            _action.SelectedItem = initialAction;
            _action.SelectedIndexChanged += (s, e) => UpdateState();
            AddFieldRow("조치 구분", _action);
            AddFieldRow("확인자 (사원증 스캔)", _confirmer);
            AddFieldRow("비고", _remark);

            _note = SetFooterNote("");
            _register.Click += (s, e) => Register();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _register);
            _confirmer.Attach(this);
            UpdateState();
        }

        public string Action { get; private set; }
        public string Remark { get; private set; }
        public string Confirmer { get; private set; }

        /// <summary>등록을 막는 이유. 막을 것이 없으면 "".</summary>
        private string BlockReason()
        {
            if (_action.SelectedIndex < 0) return "조치 구분을 선택하세요";
            if (_open && (string)_action.SelectedItem == NoImpact) return "이탈 진행 중 · 영향 없음으로 닫을 수 없음";
            return "";
        }

        private void UpdateState()
        {
            string reason = BlockReason();
            _register.Enabled = reason.Length == 0;
            _note.Text = reason;
        }

        private void Register()
        {
            if (BlockReason().Length > 0) return;   // 버튼이 꺼져 있어도 한 번 더 막는다
            if (_confirmer.Text.Trim().Length == 0)
            {
                ApcDialog.Warn(this, "확인자 사원증을 스캔하세요.");
                _confirmer.Focus();
                return;
            }
            if (!ApcDialog.Confirm(this, "'" + _action.SelectedItem + "'으로 조치를 등록할까요?", "등록", ApcButtonVariant.Primary)) return;
            Action = (string)_action.SelectedItem;
            Remark = _remark.Text.Trim();
            Confirmer = _confirmer.Text.Trim();
            DialogResult = DialogResult.OK;
        }

        private static ApcTextBox ReadOnlyBox(string text)
        {
            return new ApcTextBox { Text = text, ReadOnly = true, TabStop = false };
        }
    }
}
