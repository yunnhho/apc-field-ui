using System.Globalization;

namespace CellLeakTester.Data
{
    /// <summary>누설량 판정 기준과 판정 함수 한 곳. 화면의 카드·표·판정판이 모두 이것을 쓴다.</summary>
    internal static class LeakJudge
    {
        /// <summary>누설량 상한(Pa/s). 요청 기준값. 상한과 같은 값은 OK, 초과부터 NG.</summary>
        public const double UpperLimit = 5.0;

        public const string Unit = "Pa/s";

        public static bool IsNg(double leak)
        {
            return leak > UpperLimit;
        }

        public static string Format(double leak)
        {
            return leak.ToString("0.0", CultureInfo.InvariantCulture);
        }

        public static string LimitText
        {
            get { return Format(UpperLimit) + " 이하 OK"; }
        }
    }
}
