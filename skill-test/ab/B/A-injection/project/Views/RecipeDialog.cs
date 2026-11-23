using System;
using System.Windows.Forms;
using APC.UI.Controls;
using InjectionMonitor.Data;

namespace InjectionMonitor.Views
{
    /// <summary>
    /// 레시피 편집 dialog. 저장 순서: 입력 확정 → 검증(Warn) → Confirm("저장") → 결과 속성 → OK.
    /// 메인 화면은 OK일 때 Result를 읽어 데이터 인터페이스에 넘긴다.
    /// </summary>
    internal sealed class RecipeDialog : ApcForm
    {
        private readonly ApcNumericBox _tempSet = new ApcNumericBox { Minimum = 0, Maximum = 300, DecimalPlaces = 1, Increment = 0.1m };
        private readonly ApcNumericBox _pressureUpper = new ApcNumericBox { Minimum = 20, Maximum = 500, DecimalPlaces = 1, Increment = 0.1m };
        private readonly ApcNumericBox _cycleTarget = new ApcNumericBox { Minimum = 1, Maximum = 600, DecimalPlaces = 0, Increment = 1 };

        public RecipeDialog() : this(new SampleMainViewSource().GetRecipe()) { }

        public RecipeDialog(RecipeInfo recipe)
        {
            SetupDialog("레시피 편집", 460);

            _tempSet.Value = (decimal)recipe.TempSet;
            _pressureUpper.Value = (decimal)recipe.PressureUpper;
            _cycleTarget.Value = recipe.CycleTarget;

            AddFieldRow("금형 온도 설정값 (℃)", _tempSet);
            AddFieldRow("사출 압력 상한 (bar)", _pressureUpper);
            AddFieldRow("사이클 타임 목표 (s)", _cycleTarget);

            var save = new ApcButton { Text = "저장", Variant = ApcButtonVariant.Primary };
            save.Click += (s, e) => Save();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, save);
        }

        public RecipeInfo Result { get; private set; }

        private void Save()
        {
            _tempSet.Commit(); _pressureUpper.Commit(); _cycleTarget.Commit();
            double temp = (double)_tempSet.Value;
            if (temp < Judge.TempLower || temp > Judge.TempUpper)
            {
                ApcDialog.Warn(this, "금형 온도 설정값은 하한 " + Judge.F(Judge.TempLower) + " ~ 상한 " + Judge.F(Judge.TempUpper) + " ℃ 사이여야 합니다.");
                return;
            }
            if (!ApcDialog.Confirm(this, "레시피를 저장할까요?", "저장", ApcButtonVariant.Primary)) return;

            Result = new RecipeInfo(temp, (double)_pressureUpper.Value, (int)_cycleTarget.Value);
            DialogResult = DialogResult.OK;
        }
    }
}
