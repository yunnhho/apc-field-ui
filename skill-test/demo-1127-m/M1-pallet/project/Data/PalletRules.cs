using System.Text.RegularExpressions;

namespace PalletPacker.Data
{
    /// <summary>적재 패턴·중량 기준·라벨 형식 상수와 판정 함수. 화면·dialog·데이터가 같은 기준을 쓴다.</summary>
    internal static class PalletRules
    {
        public const int Layers = 5;
        public const int CellsPerLayer = 8;      // 2 x 4
        public const int CellColumns = 4;
        public const int TargetBoxes = Layers * CellsPerLayer;

        public const decimal BoxKg = 12.5m;
        public const decimal EmptyPalletKg = 22.0m;
        public const decimal NominalKg = 522.0m;     // 22.0 + 40 x 12.5
        public const decimal MinKg = 511.6m;         // 기준 -2.0 %, 경계 포함 OK
        public const decimal MaxKg = 532.4m;         // 기준 +2.0 %, 경계 포함 OK

        public static readonly Regex BoxLabel = new Regex(@"^BX-(.+)-(\d{5})$");
        public static readonly Regex PalletLabel = new Regex(@"^PLT-\d{6}-\d{3}$");
        public static readonly Regex EmployeeLabel = new Regex(@"^EMP-\d{5}$");

        /// <summary>총중량이 허용 범위(경계 포함) 안인가.</summary>
        public static bool WeightInRange(decimal kg)
        {
            return kg >= MinKg && kg <= MaxKg;
        }

        public static string FormatKg(decimal kg)
        {
            return kg.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
