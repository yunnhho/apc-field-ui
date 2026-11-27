using System;

namespace TorqueSequence.Data
{
    /// <summary>한 포인트의 체결값(공구 통신값 또는 수동 입력값). 값 객체.</summary>
    internal sealed class PointResult
    {
        public readonly int Point;
        public readonly decimal Torque;
        public readonly int Angle;
        public readonly DateTime Time;

        public PointResult(int point, decimal torque, int angle, DateTime time)
        {
            Point = point; Torque = torque; Angle = angle; Time = time;
        }
    }

    /// <summary>작업 중인 부품의 상태. 화면은 이것으로 시작한다(실제는 DB 등에서 복원).</summary>
    internal sealed class SessionSnapshot
    {
        public readonly string Serial;
        public readonly string Operator;
        public readonly PointResult[] Results;   // 포인트별 가장 최근 체결값
        public readonly int[] NgCounts;          // 인덱스 = 포인트 번호(1~10), 그 포인트의 NG 누적 횟수

        public SessionSnapshot(string serial, string op, PointResult[] results, int[] ngCounts)
        {
            Serial = serial; Operator = op; Results = results; NgCounts = ngCounts;
        }
    }

    /// <summary>체결 기준과 판정. 토크·각도 모두 경계값 포함 OK. 판정은 이 한 곳만 쓴다.</summary>
    internal static class TorqueJudge
    {
        public const decimal TorqueMin = 9.0m, TorqueMax = 11.0m;
        public const int AngleMin = 30, AngleMax = 60;
        public const int PointCount = 10;
        public const int MaxNgPerPoint = 2;   // 같은 포인트 NG 2회째면 재체결 불가

        /// <summary>고정 체결 순서(지그재그).</summary>
        public static readonly int[] Sequence = { 1, 6, 2, 7, 3, 8, 4, 9, 5, 10 };

        /// <summary>NG 사유. OK면 null. 토크를 먼저 본다.</summary>
        public static string Reason(PointResult r)
        {
            if (r.Torque < TorqueMin) return "토크 하한 미달 " + FormatTorque(r.Torque) + " N·m (하한 " + FormatTorque(TorqueMin) + ")";
            if (r.Torque > TorqueMax) return "토크 상한 초과 " + FormatTorque(r.Torque) + " N·m (상한 " + FormatTorque(TorqueMax) + ")";
            if (r.Angle < AngleMin) return "각도 하한 미달 " + r.Angle + " ° (하한 " + AngleMin + ")";
            if (r.Angle > AngleMax) return "각도 상한 초과 " + r.Angle + " ° (상한 " + AngleMax + ")";
            return null;
        }

        public static bool TorqueOk(decimal t) { return t >= TorqueMin && t <= TorqueMax; }
        public static bool AngleOk(int a) { return a >= AngleMin && a <= AngleMax; }
        public static string FormatTorque(decimal t) { return t.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture); }
    }
}
