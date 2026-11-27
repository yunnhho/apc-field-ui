using System;
using System.Globalization;
using System.Text.RegularExpressions;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>레벨·온도 판정과 밸브 인터락. 화면·표·알람·dialog가 모두 이 함수를 쓴다.</summary>
    internal static class ProcessRules
    {
        public const int LevelWarn = 85;      // % 이상 경고
        public const int LevelAlarm = 90;     // % 초과 알람(밸브 열기 인터락)
        public const int LevelLow = 10;       // % 이하 하한 경고
        public const double BlendTempMin = 45.0; // ℃ (경계 포함 OK)
        public const double BlendTempMax = 55.0;

        private static readonly Regex Badge = new Regex(@"^EMP-\d{5}$");

        public static bool IsBadge(string code) { return code != null && Badge.IsMatch(code); }

        public static StatusKind LevelStatus(int level)
        {
            if (level > LevelAlarm) return StatusKind.Alarm;
            if (level >= LevelWarn || level <= LevelLow) return StatusKind.Warn;
            return StatusKind.Run;
        }

        /// <summary>T3 배합 온도만 판정한다. 나머지 탱크는 판정 없음(null).</summary>
        public static StatusKind? TempStatus(string tankId, double temp)
        {
            if (tankId != "T3") return null;
            return (temp < BlendTempMin || temp > BlendTempMax) ? StatusKind.Alarm : StatusKind.Run;
        }

        public static string Pct(int level) { return level.ToString(CultureInfo.InvariantCulture) + " %"; }
        public static string Temp(double t) { return t.ToString("0.0", CultureInfo.InvariantCulture); }

        // 배관: T1 -V1- P1 -V3- T3, T2 -V2- P1, T3 -V4- P2 -V5- T4, T4 -V6- 출하
        public static string DownstreamTank(string valve)
        {
            switch (valve)
            {
                case "V1": case "V2": case "V3": return "T3";
                case "V4": case "V5": return "T4";
                default: return null;
            }
        }

        /// <summary>흡입 쪽 밸브(V1·V2·V4)가 속한 펌프. 아니면 null.</summary>
        public static string SuctionPump(string valve)
        {
            switch (valve)
            {
                case "V1": case "V2": return "P1";
                case "V4": return "P2";
                default: return null;
            }
        }

        /// <summary>조작을 막아야 하면 이유 글, 아니면 null.</summary>
        public static string BlockReason(string valve, bool open, TankInfo[] tanks, PumpInfo[] pumps)
        {
            if (open)
            {
                string tankId = DownstreamTank(valve);
                foreach (var t in tanks)
                    if (t.Id == tankId && t.Level > LevelAlarm)
                        return t.Id + " 레벨 " + Pct(t.Level) + " · 열기 차단 (알람 기준 " + LevelAlarm + " % 초과)";
                return null;
            }
            string pumpId = SuctionPump(valve);
            foreach (var p in pumps)
                if (p.Id == pumpId && p.Running)
                    return pumpId + " 가동 중 · 흡입 밸브 닫기 차단 (" + pumpId + " 정지 후 조작)";
            return null;
        }
    }
}
