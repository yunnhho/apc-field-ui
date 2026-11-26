using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PressLineOee.Data;

namespace PressLineOee.Views
{
    /// <summary>
    /// 비가동 사유 입력. 설비·시작·종료는 읽기 전용, 대분류·세부 사유는 목록에서 고른다(키보드 입력 없음).
    /// 저장 순서: 선택 검증(틀리면 Warn) → Confirm("저장") → 결과 속성 → OK.
    /// </summary>
    internal sealed class StopReasonDialog : ApcForm
    {
        private const int ComboWidth = 260;

        private readonly ApcComboBox _category = new ApcComboBox { Width = ApcTheme.Px(ComboWidth) };
        private readonly ApcComboBox _detail = new ApcComboBox { Width = ApcTheme.Px(ComboWidth) };
        private readonly ApcTextBox _note = new ApcTextBox();

        public StopReasonDialog() : this("P04", "2026-11-25 09:42:10", "") { }

        /// <param name="endText">진행 중이면 빈 문자열.</param>
        public StopReasonDialog(string machine, string startText, string endText)
        {
            SetupDialog("비가동 사유 입력", 680);

            _category.Items.AddRange(ReasonCatalog.GetCategories());
            _category.SelectedIndexChanged += (s, e) => LoadDetails();

            AddContent(SectionTitle("정지 정보"));
            AddFieldRow("설비", ReadOnlyBox(machine));
            AddFieldRow("시작", ReadOnlyBox(startText));
            AddFieldRow("종료", ReadOnlyBox(endText.Length == 0 ? "정지 중" : endText));

            AddContent(SectionTitle("사유"));
            AddFieldRow("대분류", _category, Hint("5분 이상 정지만 입력"));
            AddFieldRow("세부 사유", _detail, Hint("대분류를 먼저 고르세요"));
            AddFieldRow("비고", _note);

            var save = new ApcButton { Text = "저장", Variant = ApcButtonVariant.Primary };
            save.Click += (s, e) => Save();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, save);
        }

        public string Category { get; private set; }
        public string Detail { get; private set; }
        public string Note { get; private set; }

        private void LoadDetails()
        {
            _detail.Items.Clear();
            _detail.Items.AddRange(ReasonCatalog.GetDetails(_category.SelectedItem as string));
        }

        private void Save()
        {
            if (_category.SelectedItem == null) { Reject("대분류를 선택하세요.", _category); return; }
            if (_detail.SelectedItem == null) { Reject("세부 사유를 선택하세요.", _detail); return; }
            if (!ApcDialog.Confirm(this, "사유를 저장할까요? (" + _category.SelectedItem + " · " + _detail.SelectedItem + ")",
                "저장", ApcButtonVariant.Primary)) return;

            Category = (string)_category.SelectedItem;
            Detail = (string)_detail.SelectedItem;
            Note = _note.Text.Trim();
            DialogResult = DialogResult.OK;
        }

        private void Reject(string message, Control field)
        {
            ApcDialog.Warn(this, message);
            field.Focus();
        }

        private static ApcTextBox ReadOnlyBox(string text)
        {
            return new ApcTextBox { Text = text, ReadOnly = true, TabStop = false };
        }

        private static Label Hint(string text)
        {
            var l = FieldCaption(text);
            l.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.TextMuted);
            return l;
        }
    }
}
