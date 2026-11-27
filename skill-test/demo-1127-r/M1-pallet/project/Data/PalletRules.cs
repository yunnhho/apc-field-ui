using System.Text.RegularExpressions;

namespace PalletPacker.Data
{
    /// <summary>적재·마감 규칙과 기준값. 화면·dialog·데이터가 같은 판정을 쓴다.</summary>
    internal static class PalletRules
    {
        public const decimal BoxKg = 12.5m;
        public const decimal EmptyPalletKg = 22.0m;
        public const decimal TolerancePercent = 2.0m;

        private static readonly Regex BoxLabel = new Regex(@"^BX-([A-Z0-9]+)-(\d{5})$");
        private static readonly Regex PalletLabel = new Regex(@"^PLT-\d{6}-\d{3}$");
        private static readonly Regex BadgeLabel = new Regex(@"^EMP-\d{5}$");

        public static decimal ExpectedKg(int boxes) { return EmptyPalletKg + BoxKg * boxes; }

        /// <summary>허용 하한(0.1 kg 반올림). 경계 포함 OK.</summary>
        public static decimal MinKg(int boxes) { return decimal.Round(ExpectedKg(boxes) * (100m - TolerancePercent) / 100m, 1); }

        /// <summary>허용 상한(0.1 kg 반올림). 경계 포함 OK.</summary>
        public static decimal MaxKg(int boxes) { return decimal.Round(ExpectedKg(boxes) * (100m + TolerancePercent) / 100m, 1); }

        public static bool WeightInRange(decimal kg, int boxes) { return kg >= MinKg(boxes) && kg <= MaxKg(boxes); }

        /// <summary>박스 라벨에서 품번을 꺼낸다. 형식이 아니면 null.</summary>
        public static string PartOfBox(string code)
        {
            Match m = BoxLabel.Match(code ?? "");
            return m.Success ? m.Groups[1].Value : null;
        }

        public static bool IsPalletLabel(string code) { return PalletLabel.IsMatch(code ?? ""); }
        public static bool IsBadge(string code) { return BadgeLabel.IsMatch(code ?? ""); }
    }
}
