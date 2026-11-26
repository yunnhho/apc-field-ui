using System;
using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>최종 수율 판정 기준. 모든 화면·dialog가 이 클래스 하나로 판정한다.</summary>
    internal static class YieldJudge
    {
        /// <summary>최종 수율 하한(%). 미만이면 기준 미달 → Lot Hold. 같은 값은 통과. (정한 기준값)</summary>
        public const double LimitPercent = 95.0;

        /// <summary>경고 시작(%). 이 값 이하(같은 값 포함)면 기준 근접 경고. (정한 기준값)</summary>
        public const double WarnPercent = 96.0;

        public static bool IsBelowLimit(double finalYield)
        {
            return Round(finalYield) < LimitPercent;
        }

        public static StatusKind Judge(double finalYield)
        {
            double rounded = Round(finalYield);
            if (rounded < LimitPercent) return StatusKind.Alarm;
            if (rounded <= WarnPercent) return StatusKind.Warn;
            return StatusKind.Run;
        }

        // 화면에 보이는 소수 한 자리 기준으로 판정해, 표시값과 판정이 어긋나지 않게 한다.
        private static double Round(double value)
        {
            return Math.Round(value, 1, MidpointRounding.AwayFromZero);
        }
    }
}
