using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>
    /// 입력 폼 dialog 골격: 맨 위 식별 값(모델명) 한 줄, 아래 항목마다 카드 한 장(ApcFieldCard)을 AddContentRow로 나란히, 오른쪽 아래 [저장][취소].
    /// 카드 = 제목 띠(항목 이름 + 단위) + 짝 입력(하한·상한) + 아래 허용 범위 안내(숫자칸 Minimum~Maximum과 같은 값).
    /// 저장 순서: 입력 확정(Commit) → 검증(틀리면 Warn 후 그 칸으로 커서, 닫지 않음) → Confirm("저장", Primary) → 결과를 속성에 담고 OK로 닫기.
    /// 메인 화면은 ShowDialog(FindForm()) == OK일 때 결과 속성을 읽어 데이터 인터페이스에 넘긴다.
    /// </summary>
    internal sealed class RecipeDialog : ApcForm
    {
        private const decimal LoadLimit = 50.0m, DispLimit = 20.00m, SpeedMin = 0.1m, SpeedMax = 10.0m;

        private readonly ApcTextBox _name = new ApcTextBox();
        private readonly ApcNumericBox _loadMax = Number(1, 0, LoadLimit), _loadMin = Number(1, 0, LoadLimit),
            _dispMax = Number(2, 0, DispLimit), _dispMin = Number(2, 0, DispLimit), _speed = Number(1, SpeedMin, SpeedMax);

        public RecipeDialog() : this("6205-2RS", 12.0m, 8.0m, 5.20m, 4.80m, 2.0m) { }

        public RecipeDialog(string model, decimal loadMax, decimal loadMin, decimal dispMax, decimal dispMin, decimal speed)
        {
            SetupDialog("레시피 편집", 720);   // 높이는 내용으로 자동

            _name.Text = model;
            _loadMax.Value = loadMax; _loadMin.Value = loadMin;
            _dispMax.Value = dispMax; _dispMin.Value = dispMin;
            _speed.Value = speed;

            AddFieldRow("모델명", _name);
            AddContentRow(
                Card("하중 판정", "kN", "허용 0.0 ~ " + LoadLimit.ToString("0.0"), "하한", _loadMin, "상한", _loadMax),
                Card("변위 판정", "mm", "허용 0.00 ~ " + DispLimit.ToString("0.00"), "하한", _dispMin, "상한", _dispMax),
                Card("압입 조건", "mm/s", "허용 " + SpeedMin.ToString("0.0") + " ~ " + SpeedMax.ToString("0.0"), "압입 속도", _speed));

            var save = new ApcButton { Text = "저장", Variant = ApcButtonVariant.Primary };
            save.Click += (s, e) => Save();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, save);   // Esc = 취소. Enter는 기본 버튼 없음(스캔 Enter로 저장되지 않게)
        }

        public string Model { get; private set; }
        public decimal LoadMax { get; private set; }
        public decimal LoadMin { get; private set; }
        public decimal DispMax { get; private set; }
        public decimal DispMin { get; private set; }
        public decimal Speed { get; private set; }

        private void Save()
        {
            foreach (var n in new[] { _loadMax, _loadMin, _dispMax, _dispMin, _speed }) n.Commit();
            if (_name.Text.Trim().Length == 0) { Reject("모델명을 입력하세요.", _name); return; }
            if (_loadMin.Value >= _loadMax.Value) { Reject("하중 하한이 상한보다 작아야 합니다.", _loadMin.TextBox); return; }
            if (_dispMin.Value >= _dispMax.Value) { Reject("변위 하한이 상한보다 작아야 합니다.", _dispMin.TextBox); return; }
            if (!ApcDialog.Confirm(this, "레시피 '" + _name.Text.Trim() + "'를 저장할까요?", "저장", ApcButtonVariant.Primary)) return;

            Model = _name.Text.Trim();
            LoadMax = _loadMax.Value; LoadMin = _loadMin.Value;
            DispMax = _dispMax.Value; DispMin = _dispMin.Value;
            Speed = _speed.Value;
            DialogResult = DialogResult.OK;
        }

        // 경고 후 틀린 칸으로 커서 → 입력 중 칸·카드 강조(파란 테두리)로 어디를 고칠지 보인다
        private void Reject(string message, ApcTextBox field)
        {
            ApcDialog.Warn(this, message);
            field.Focus();
            field.SelectAll();
        }

        // fields: 이름표, 입력칸 짝을 위→아래 순서로
        private static ApcFieldCard Card(string title, string unit, string hint, params object[] fields)
        {
            var card = new ApcFieldCard { Text = title, SubTitle = unit, Footer = hint };
            for (int i = 0; i + 1 < fields.Length; i += 2) card.AddField((string)fields[i], (Control)fields[i + 1]);
            return card;
        }

        private static ApcNumericBox Number(int decimals, decimal min, decimal max)
        {
            return new ApcNumericBox { Minimum = min, Maximum = max, DecimalPlaces = decimals, Increment = decimals == 2 ? 0.01m : 0.1m };
        }
    }
}
