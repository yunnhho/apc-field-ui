namespace PalletPacker.Data
{
    /// <summary>적재·마감 기준값. 판정은 이 상수와 PalletRules 함수 하나로만 한다.</summary>
    internal static class PalletRules
    {
        public const int Layers = 5;
        public const int CellsPerLayer = 8;
        public const int TargetBoxes = Layers * CellsPerLayer;
        public const decimal BoxKg = 12.5m;
        public const decimal EmptyPalletKg = 22.0m;
        public const decimal NominalKg = 522.0m;      // 22.0 + 40 x 12.5
        public const decimal MinKg = 511.6m;          // 522.0 -2.0 %
        public const decimal MaxKg = 532.4m;          // 522.0 +2.0 %

        /// <summary>총중량이 허용 범위 안인가(경계 포함 OK).</summary>
        public static bool WeightInRange(decimal kg)
        {
            return kg >= MinKg && kg <= MaxKg;
        }
    }
}
