using System;
using System.Globalization;

namespace PalletPacker.Data
{
    /// <summary>적재 패턴·중량 기준. 화면·dialog·샘플 데이터가 같은 값을 쓴다.</summary>
    internal static class PalletRules
    {
        public const int Floors = 5;
        public const int BoxesPerFloor = 8;      // 층마다 2 x 4칸
        public const decimal BoxKg = 12.5m;
        public const decimal EmptyPalletKg = 22.0m;
        public const decimal TolerancePercent = 2.0m;   // 허용 +-2.0 %

        /// <summary>목표 박스 수 기준 총중량(40박스 = 522.0 kg).</summary>
        public static decimal ExpectedKg(int targetBoxes) { return EmptyPalletKg + BoxKg * targetBoxes; }

        /// <summary>허용 하한 — 표시(0.0 kg)와 같은 자리로 맞춘다(40박스 = 511.6).</summary>
        public static decimal MinKg(int targetBoxes) { return Math.Round(ExpectedKg(targetBoxes) * (100m - TolerancePercent) / 100m, 1, MidpointRounding.AwayFromZero); }

        /// <summary>허용 상한(40박스 = 532.4).</summary>
        public static decimal MaxKg(int targetBoxes) { return Math.Round(ExpectedKg(targetBoxes) * (100m + TolerancePercent) / 100m, 1, MidpointRounding.AwayFromZero); }

        public static string Kg(decimal value) { return value.ToString("0.0", CultureInfo.InvariantCulture) + " kg"; }

        /// <summary>마감을 막는 사유(없으면 null). 경계값은 허용(OK). weight는 입력됐을 때만 판정한다.</summary>
        public static string CloseBlockReason(int boxes, int targetBoxes, decimal? weight)
        {
            if (boxes < targetBoxes) return "박스 " + boxes + "/" + targetBoxes + " · 마감 불가";
            if (weight.HasValue && (weight.Value < MinKg(targetBoxes) || weight.Value > MaxKg(targetBoxes)))
                return "총중량 " + Kg(weight.Value) + " · 허용 " + MinKg(targetBoxes).ToString("0.0", CultureInfo.InvariantCulture)
                    + " ~ " + Kg(MaxKg(targetBoxes));
            return null;
        }
    }
}
