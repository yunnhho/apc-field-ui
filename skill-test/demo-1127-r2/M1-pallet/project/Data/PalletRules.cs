namespace PalletPacker.Data
{
    /// <summary>적재 패턴·중량 기준. 값이 바뀌면 여기만 고친다.</summary>
    internal static class PalletRules
    {
        public const int Rows = 2, Columns = 4, Layers = 5;
        public const int CellsPerLayer = Rows * Columns;      // 8
        public const int TargetBoxes = CellsPerLayer * Layers;  // 40

        // 박스 12.5 kg × 40 + 빈 팔레트 22.0 kg = 522.0 kg, 허용 ±2.0 % → 511.6 ~ 532.4 kg(경계 포함 OK)
        public const decimal BoxKg = 12.5m, EmptyPalletKg = 22.0m;
        public const decimal WeightMinKg = 511.6m, WeightMaxKg = 532.4m;

        /// <summary>총중량이 허용 범위(경계 포함) 안인가.</summary>
        public static bool WeightOk(decimal kg)
        {
            return kg >= WeightMinKg && kg <= WeightMaxKg;
        }
    }
}
