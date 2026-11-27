using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TorqueSequence.Data
{
    /// <summary>체결 기준과 판정. 상태 문구·표·카드·도식이 모두 이 함수 하나를 쓴다.</summary>
    internal static class TorqueRules
    {
        public const double TorqueMin = 9.0, TorqueMax = 11.0;   // N·m, 경계 포함 OK
        public const int AngleMin = 30, AngleMax = 60;           // °, 경계 포함 OK
        public const int MaxNg = 2;                              // 같은 포인트 NG 2회째 = 재체결 불가
        public static readonly int[] Sequence = { 1, 6, 2, 7, 3, 8, 4, 9, 5, 10 };

        private static readonly Regex SerialPattern = new Regex(@"^HC-(\d{6})-\d{4}$");
        private static readonly Regex EmployeePattern = new Regex(@"^EMP-\d{5}$");

        public static double RoundTorque(double torque) { return Math.Round(torque, 1, MidpointRounding.AwayFromZero); }

        /// <summary>토크 사유(OK면 null). torque는 표시 자릿수(0.0)로 반올림한 값.</summary>
        public static string TorqueFault(double torque)
        {
            return torque > TorqueMax ? "토크 상한 초과" : torque < TorqueMin ? "토크 하한 미달" : null;
        }

        public static string AngleFault(int angle)
        {
            return angle > AngleMax ? "각도 상한 초과" : angle < AngleMin ? "각도 하한 미달" : null;
        }

        /// <summary>포인트 판정 사유. 토크를 먼저 본다. 둘 다 OK여야 null.</summary>
        public static string Fault(double torque, int angle) { return TorqueFault(torque) ?? AngleFault(angle); }

        public static bool IsSerial(string code)
        {
            var m = SerialPattern.Match(code ?? "");
            DateTime d;
            return m.Success && DateTime.TryParseExact(m.Groups[1].Value, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
        }

        public static bool IsEmployee(string code) { return EmployeePattern.IsMatch(code ?? ""); }

        public static bool IsForeman(string code) { return IsEmployee(code) && code.StartsWith("EMP-9", StringComparison.Ordinal); }
    }
}
