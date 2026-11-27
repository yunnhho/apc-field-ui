using System.Text.RegularExpressions;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>레벨·온도 판정과 밸브 인터락. 화면·dialog·알람이 모두 이 함수를 쓴다.</summary>
    internal static class ProcessRules
    {
        public const int LevelWarn = 85;     // 이상이면 경고
        public const int LevelAlarm = 90;    // 초과면 알람 (90은 경고)
        public const int LevelLow = 10;      // 이하면 하한 경고
        public const decimal TempMin = 45.0m, TempMax = 55.0m;   // T3 배합 온도, 경계 포함 OK

        private static readonly Regex Badge = new Regex(@"^EMP-\d{5}$");

        public static bool IsBadge(string code) { return code != null && Badge.IsMatch(code); }

        public static StatusKind LevelStatus(int level)
        {
            if (level > LevelAlarm) return StatusKind.Alarm;
            if (level >= LevelWarn || level <= LevelLow) return StatusKind.Warn;
            return StatusKind.Run;
        }

        public static StatusKind TempStatus(TankInfo t)
        {
            if (!t.HasTempSpec) return StatusKind.Run;
            return (t.Temp < TempMin || t.Temp > TempMax) ? StatusKind.Alarm : StatusKind.Run;
        }

        /// <summary>열기를 막는 이유(없으면 null): 하류 탱크 레벨이 알람 기준 초과.</summary>
        public static string OpenBlockReason(ValveInfo v, TankInfo downstream)
        {
            if (downstream == null || LevelStatus(downstream.Level) != StatusKind.Alarm) return null;
            return downstream.Id + " 레벨 " + downstream.Level + " % · 열기 불가";
        }

        /// <summary>닫기를 막는 이유(없으면 null): 그 배관 펌프 가동 중 + 흡입 쪽 밸브.</summary>
        public static string CloseBlockReason(ValveInfo v, PumpInfo pump)
        {
            if (pump == null || !v.IsSuction || !pump.Running) return null;
            return pump.Id + " 가동 중 · 흡입 밸브 닫기 불가";
        }
    }
}
