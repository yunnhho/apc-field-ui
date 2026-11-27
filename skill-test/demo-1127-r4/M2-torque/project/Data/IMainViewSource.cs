using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TorqueSequence.Data
{
    /// <summary>체결 한 번의 결과(불변). 같은 포인트가 NG 후 재체결되면 시도마다 하나씩 쌓인다.</summary>
    internal sealed class BoltAttempt
    {
        public readonly int Point;
        public readonly decimal Torque;
        public readonly int Angle;
        public readonly DateTime Time;

        public BoltAttempt(int point, decimal torque, int angle, DateTime time)
        {
            Point = point; Torque = torque; Angle = angle; Time = time;
        }

        public bool Ok { get { return TorqueRules.Check(Torque, Angle).Ok; } }
    }

    /// <summary>체결 공구(또는 수동 입력)가 보내는 포인트별 측정값(불변).</summary>
    internal sealed class ToolReading
    {
        public readonly int Point;
        public readonly decimal Torque;
        public readonly int Angle;

        public ToolReading(int point, decimal torque, int angle)
        {
            Point = point; Torque = torque; Angle = angle;
        }
    }

    /// <summary>판정 결과. Short = 표의 사유, Detail = 판정판 한 줄(값과 기준 포함).</summary>
    internal sealed class Verdict
    {
        public readonly bool Ok;
        public readonly string Short;
        public readonly string Detail;

        public Verdict(bool ok, string shortText, string detail)
        {
            Ok = ok; Short = shortText; Detail = detail;
        }
    }

    /// <summary>체결 기준과 판정. 상·하한 값과 같으면 OK. 화면·표·카드가 모두 이 함수를 쓴다.</summary>
    internal static class TorqueRules
    {
        public const decimal TorqueMin = 9.0m, TorqueMax = 11.0m;   // N·m
        public const int AngleMin = 30, AngleMax = 60;              // °
        public static readonly int[] Sequence = { 1, 6, 2, 7, 3, 8, 4, 9, 5, 10 };

        private static readonly Regex SerialPattern = new Regex(@"^HC-\d{6}-\d{4}$");
        private static readonly Regex BadgePattern = new Regex(@"^EMP-\d{5}$");

        public static bool IsSerial(string code)
        {
            DateTime d;
            return code != null && SerialPattern.IsMatch(code)
                && DateTime.TryParseExact(code.Substring(3, 6), "yyMMdd", null, System.Globalization.DateTimeStyles.None, out d);
        }

        public static bool IsBadge(string code) { return code != null && BadgePattern.IsMatch(code); }
        public static bool IsSupervisorBadge(string code) { return IsBadge(code) && code.StartsWith("EMP-9"); }

        public static decimal RoundTorque(decimal t) { return Math.Round(t, 1, MidpointRounding.AwayFromZero); }

        public static Verdict Check(decimal torque, int angle)
        {
            decimal t = RoundTorque(torque);
            if (t > TorqueMax) return new Verdict(false, "토크 상한 초과", "토크 " + t.ToString("0.0") + " N·m 상한 초과 (상한 " + TorqueMax.ToString("0.0") + ")");
            if (t < TorqueMin) return new Verdict(false, "토크 하한 미달", "토크 " + t.ToString("0.0") + " N·m 하한 미달 (하한 " + TorqueMin.ToString("0.0") + ")");
            if (angle > AngleMax) return new Verdict(false, "각도 상한 초과", "각도 " + angle + "° 상한 초과 (상한 " + AngleMax + ")");
            if (angle < AngleMin) return new Verdict(false, "각도 하한 미달", "각도 " + angle + "° 하한 미달 (하한 " + AngleMin + ")");
            return new Verdict(true, "", "");
        }
    }

    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(체결 공구 통신, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>시작 시 이어갈 부품 시리얼(없으면 빈 문자열).</summary>
        string GetCurrentSerial();

        /// <summary>시작 시 이어갈 작업자 사원증(없으면 빈 문자열).</summary>
        string GetCurrentBadge();

        /// <summary>시작 시 이어갈 체결 이력(시도 순서대로).</summary>
        IList<BoltAttempt> GetAttempts();

        /// <summary>자동 모드에서 체결 공구가 보낸 다음 값. 받은 값이 없으면 null.</summary>
        ToolReading ReadTool();

        /// <summary>부품이 바뀔 때(불량 처리·새 부품) 공구 통신 상태를 초기화한다.</summary>
        void ResetTool();
    }
}
