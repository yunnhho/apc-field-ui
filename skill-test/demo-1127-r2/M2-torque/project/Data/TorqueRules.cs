using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TorqueSequence.Data
{
    /// <summary>체결 한 번의 결과(불변). OK/NG는 TorqueRules.FailReason으로 정한다.</summary>
    internal sealed class PointResult
    {
        public readonly int Point;
        public readonly decimal Torque;
        public readonly int Angle;
        public readonly DateTime At;
        /// <summary>NG 사유 짧은 글("토크 하한"). OK면 null.</summary>
        public readonly string FailReason;

        public PointResult(int point, decimal torque, int angle, DateTime at)
        {
            Point = point;
            Torque = torque;
            Angle = angle;
            At = at;
            FailReason = TorqueRules.FailReason(torque, angle);
        }

        public bool Ok { get { return FailReason == null; } }
    }

    /// <summary>토크 기준·체결 순서·바코드 형식. 판정은 모두 여기 한 곳에서 한다(상태 글·표·도식·이력 공통).</summary>
    internal static class TorqueRules
    {
        public const decimal TorqueMin = 9.0m, TorqueMax = 11.0m;   // N·m, 경계 포함 OK
        public const int AngleMin = 30, AngleMax = 60;              // °, 경계 포함 OK
        public const int NgLimitPerPoint = 2;                       // 같은 포인트 NG 2회째면 재체결 불가
        public static readonly int[] Sequence = { 1, 6, 2, 7, 3, 8, 4, 9, 5, 10 };

        private static readonly Regex SerialPattern = new Regex(@"^HC-\d{6}-\d{4}$");
        private static readonly Regex BadgePattern = new Regex(@"^EMP-\d{5}$");

        public static string FailReason(decimal torque, int angle)
        {
            if (torque < TorqueMin) return "토크 하한";
            if (torque > TorqueMax) return "토크 상한";
            if (angle < AngleMin) return "각도 하한";
            if (angle > AngleMax) return "각도 상한";
            return null;
        }

        /// <summary>NG 한 줄 설명. 예: "토크 하한 미달 8.8 N·m (하한 9.0)"</summary>
        public static string Describe(PointResult r)
        {
            switch (r.FailReason)
            {
                case "토크 하한": return "하한 미달 " + Torque(r.Torque) + " N·m (하한 " + Torque(TorqueMin) + ")";
                case "토크 상한": return "상한 초과 " + Torque(r.Torque) + " N·m (상한 " + Torque(TorqueMax) + ")";
                case "각도 하한": return "각도 하한 미달 " + r.Angle + "° (하한 " + AngleMin + ")";
                case "각도 상한": return "각도 상한 초과 " + r.Angle + "° (상한 " + AngleMax + ")";
                default: return "";
            }
        }

        public static string Torque(decimal v) { return v.ToString("0.0", CultureInfo.InvariantCulture); }

        public static bool IsSerial(string code)
        {
            DateTime d;
            return code != null && SerialPattern.IsMatch(code)
                && DateTime.TryParseExact(code.Substring(3, 6), "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
        }

        public static bool IsBadge(string code) { return code != null && BadgePattern.IsMatch(code); }

        public static bool IsSupervisorBadge(string code) { return IsBadge(code) && code.StartsWith("EMP-9"); }
    }
}
