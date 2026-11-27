using System;
using System.Globalization;

namespace PalletPacker.Data
{
    /// <summary>적재 규격과 총중량 판정. 화면·마감 dialog가 같은 함수를 쓴다.</summary>
    internal static class PalletRules
    {
        public const int Layers = 5;
        public const int CellsPerLayer = 8;
        public const decimal BoxKg = 12.5m;
        public const decimal EmptyPalletKg = 22.0m;
        public const decimal TolerancePercent = 2.0m;

        /// <summary>허용 범위(경계 포함 OK): 목표 박스 수 기준 522.0 kg ±2.0 % → 511.6 ~ 532.4.</summary>
        public static decimal MinKg(int targetBoxes) { return Math.Round(ExpectedKg(targetBoxes) * (1 - TolerancePercent / 100m), 1, MidpointRounding.AwayFromZero); }
        public static decimal MaxKg(int targetBoxes) { return Math.Round(ExpectedKg(targetBoxes) * (1 + TolerancePercent / 100m), 1, MidpointRounding.AwayFromZero); }
        public static decimal ExpectedKg(int boxes) { return EmptyPalletKg + boxes * BoxKg; }

        /// <summary>범위 안이면 null, 밖이면 사유. 같은 값(경계)은 OK.</summary>
        public static string WeightReason(decimal kg, int targetBoxes)
        {
            decimal min = MinKg(targetBoxes), max = MaxKg(targetBoxes);
            if (kg > max) return "상한 초과 " + Kg(kg) + " kg (상한 " + Kg(max) + ")";
            if (kg < min) return "하한 미만 " + Kg(kg) + " kg (하한 " + Kg(min) + ")";
            return null;
        }

        public static string Kg(decimal v) { return v.ToString("0.0", CultureInfo.InvariantCulture); }
    }
}
