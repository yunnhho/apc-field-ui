using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Demo.Templates;

namespace APC.UI.Demo.Catalog
{
    /// <summary>dialog 템플릿 목록: 버튼을 누르면 해당 dialog를 샘플 값으로 띄운다.</summary>
    internal sealed class DialogCatalog : ApcTemplateView
    {
        public DialogCatalog() : base("dialog 템플릿")
        {
            // Top 도킹은 나중 추가가 위 → 보여줄 순서의 역순
            Add("통신·장비 설정 (ApcGroup 묶음, 저장 확인)", () => new SettingsDialog());
            Add("칸별 LOT 입력 (표에 바로 스캔, 빈 칸 Alert)", () => new LotInputDialog());
            Add("관리자 비밀번호 확인", () => new AdminPasswordDialog());
            Add("입력 폼: 레시피 편집 (검증 Warn → 저장 Confirm)", () => new RecipeDialog());
            Add("보기용 상세: 상태 줄 + 센서 표 + 추이", () => new EquipmentDetailDialog());
        }

        private void Add(string text, Func<Form> create)
        {
            var open = Button("열기", ApcButtonVariant.Normal, 90);
            open.Click += (s, e) => { using (var d = create()) d.ShowDialog(FindForm()); };
            Body.Controls.Add(Row(open, Caption(text)));
        }
    }
}
