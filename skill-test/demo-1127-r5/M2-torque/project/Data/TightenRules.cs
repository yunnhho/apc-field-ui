using System;
using System.Globalization;

namespace TorqueSequence.Data
{
    /// <summary>체결 한 번의 측정값(공구 통신값 또는 수동 입력). 값 객체.</summary>
    internal sealed class TightenRecord
    {
        public TightenRecord(int point, decimal torque, int angle, DateTime time)
        {
            Point = point; Torque = torque; Angle = angle; Time = time;
        }

        public int Point { get; private set; }
        public decimal Torque { get; private set; }
        public int Angle { get; private set; }
        public DateTime Time { get; private set; }
    }

    /// <summary>체결 기준과 판정. 상태 문구·표·도식·재체결 dialog가 모두 이 판정 하나를 쓴다.</summary>
    internal static class TightenRules
    {
        public const decimal TorqueMin = 9.0m, TorqueMax = 11.0m;   // N·m, 경계 포함 OK
        public const int AngleMin = 30, AngleMax = 60;              // °, 경계 포함 OK
        public const int MaxNgPerPoint = 2;                         // 같은 포인트 NG 2회째 → 재체결 불가
        public static readonly int[] Sequence = { 1, 6, 2, 7, 3, 8, 4, 9, 5, 10 };

        public static bool TorqueOk(decimal torque) { return torque >= TorqueMin && torque <= TorqueMax; }
        public static bool AngleOk(int angle) { return angle >= AngleMin && angle <= AngleMax; }
        public static bool IsOk(TightenRecord r) { return TorqueOk(r.Torque) && AngleOk(r.Angle); }

        /// <summary>NG 사유("토크 8.7 (하한 9.0) · 각도 28 (하한 30)"). OK면 null.</summary>
        public static string NgReason(TightenRecord r)
        {
            string t = r.Torque < TorqueMin ? "토크 " + Torque(r.Torque) + " (하한 " + Torque(TorqueMin) + ")"
                     : r.Torque > TorqueMax ? "토크 " + Torque(r.Torque) + " (상한 " + Torque(TorqueMax) + ")" : null;
            string a = r.Angle < AngleMin ? "각도 " + r.Angle + " (하한 " + AngleMin + ")"
                     : r.Angle > AngleMax ? "각도 " + r.Angle + " (상한 " + AngleMax + ")" : null;
            if (t == null && a == null) return null;
            return t == null ? a : a == null ? t : t + " · " + a;
        }

        /// <summary>이탈한 항목 이름("토크", "각도", "토크·각도"). OK면 null.</summary>
        public static string NgItems(TightenRecord r)
        {
            bool t = !TorqueOk(r.Torque), a = !AngleOk(r.Angle);
            return t && a ? "토크·각도" : t ? "토크" : a ? "각도" : null;
        }

        public static string Torque(decimal v) { return v.ToString("0.0", CultureInfo.InvariantCulture); }
    }
}
