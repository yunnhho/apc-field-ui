using System.Globalization;
using System.Text.RegularExpressions;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>판정·인터락 규칙 한 곳. 화면·dialog·알람 문구가 모두 여기를 쓴다.</summary>
    internal static class ProcessRules
    {
        public const int LevelWarn = 85;       // 이상이면 경고
        public const int LevelAlarm = 90;      // 초과면 알람(밸브 열기 인터락)
        public const int LevelLow = 10;        // 이하면 하한 경고
        public const double BlendTempMin = 45.0, BlendTempMax = 55.0;   // T3, 경계 포함 OK
        public const string BlendTankId = "T3";

        private static readonly Regex BadgePattern = new Regex(@"^EMP-\d{5}$");

        public static bool IsBadge(string code) { return code != null && BadgePattern.IsMatch(code); }

        public static StatusKind LevelKind(int level)
        {
            if (level > LevelAlarm) return StatusKind.Alarm;
            if (level >= LevelWarn || level <= LevelLow) return StatusKind.Warn;
            return StatusKind.Run;
        }

        public static StatusKind TempKind(string tankId, double temp)
        {
            if (tankId != BlendTankId) return StatusKind.Run;   // 표시만
            return temp < BlendTempMin || temp > BlendTempMax ? StatusKind.Alarm : StatusKind.Run;
        }

        public static string Level(int level) { return level.ToString(CultureInfo.InvariantCulture); }
        public static string Temp(double temp) { return temp.ToString("0.0", CultureInfo.InvariantCulture); }

        /// <summary>그 조작이 막혀 있으면 막은 이유, 아니면 null.</summary>
        public static string BlockReason(ValveInfo valve, ProcessSnapshot snap, bool wantOpen)
        {
            if (wantOpen)
            {
                TankInfo down = valve.DownstreamTankId == null ? null : snap.Tank(valve.DownstreamTankId);
                if (down != null && down.Level > LevelAlarm)
                    return "하류 " + down.Id + " 레벨 " + down.Level + " % (알람 " + LevelAlarm + " % 초과) · 열기 불가";
                return null;
            }
            PumpInfo pump = valve.SuctionPumpId == null ? null : snap.Pump(valve.SuctionPumpId);
            if (pump != null && pump.Running)
                return pump.Id + " 가동 중 · 흡입 밸브 " + valve.Id + " 닫기 불가";
            return null;
        }
    }
}
