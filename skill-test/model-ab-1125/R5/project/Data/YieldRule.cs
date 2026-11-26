using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>수율 판정 한 곳. 타일·카드·이력 표·추이 기준선·판정판이 모두 이것을 쓴다.</summary>
    internal static class YieldRule
    {
        /// <summary>최종 수율 하한(%). 같으면 합격, 미만이면 기준 미달 → Lot Hold.</summary>
        public const double FinalYieldLimit = 95.0;

        public const string YieldFormat = "0.0";
        public const string CountFormat = "#,0";
        public const string TempFormat = "0.0";

        public static bool IsBelowSpec(double finalYield)
        {
            return finalYield < FinalYieldLimit;
        }

        public static StatusKind Judge(double finalYield)
        {
            return IsBelowSpec(finalYield) ? StatusKind.Alarm : StatusKind.Run;
        }

        public static string Pct(double value)
        {
            return value.ToString(YieldFormat);
        }

        public static string Count(int value)
        {
            return value.ToString(CountFormat);
        }

        public static double Rate(int count, int total)
        {
            return total <= 0 ? 0.0 : count * 100.0 / total;
        }
    }
}
