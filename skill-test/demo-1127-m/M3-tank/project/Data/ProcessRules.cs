using System.Collections.Generic;
using System.Globalization;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>
    /// 판정·인터락 규칙 한 곳. 화면 색·판정판·밸브 dialog가 모두 여기를 쓴다.
    /// 상한 초과(>)만 알람, 같으면 OK. 경고는 시작값 포함(>=, <=).
    /// </summary>
    internal static class ProcessRules
    {
        public const int LevelWarn = 85;     // % 이상 경고
        public const int LevelAlarm = 90;    // % 초과 알람(밸브 열기 인터락)
        public const int LevelLow = 10;      // % 이하 하한 경고
        public const double BlendTempMin = 45.0;   // T3 ℃ (경계 포함 OK)
        public const double BlendTempMax = 55.0;

        public static string Pct(int level) { return level.ToString(CultureInfo.InvariantCulture) + " %"; }
        public static string Deg(double temp) { return temp.ToString("0.0", CultureInfo.InvariantCulture) + " ℃"; }

        public static StatusKind LevelKind(int level)
        {
            if (level > LevelAlarm) return StatusKind.Alarm;
            if (level >= LevelWarn || level <= LevelLow) return StatusKind.Warn;
            return StatusKind.Run;
        }

        public static StatusKind TempKind(TankInfo tank)
        {
            if (!tank.HasTempSpec) return StatusKind.Run;
            return tank.Temp < BlendTempMin || tank.Temp > BlendTempMax ? StatusKind.Alarm : StatusKind.Run;
        }

        public static StatusKind Worse(StatusKind a, StatusKind b) { return Rank(a) >= Rank(b) ? a : b; }

        private static int Rank(StatusKind k) { return k == StatusKind.Alarm ? 2 : k == StatusKind.Warn ? 1 : 0; }

        /// <summary>
        /// 밸브 조작을 막는 이유(막지 않으면 null).
        /// 열기: 하류 탱크 레벨이 알람 기준 초과. 닫기: 그 배관 펌프가 가동 중이고 흡입 쪽 밸브.
        /// </summary>
        public static string BlockReason(ValveInfo valve, bool wantOpen, IList<TankInfo> tanks, IList<PumpInfo> pumps)
        {
            if (wantOpen == valve.IsOpen) return valve.Id + " 이미 " + (valve.IsOpen ? "열림" : "닫힘");
            if (wantOpen)
            {
                var tank = FindTank(tanks, valve.DownstreamTankId);
                if (tank != null && tank.Level > LevelAlarm)
                    return "하류 " + tank.Id + " 레벨 " + Pct(tank.Level) + " (기준 " + Pct(LevelAlarm) + " 이하) · 열기 잠금";
                return null;
            }
            if (valve.IsSuction)
            {
                foreach (var pump in pumps)
                    if (pump.Id == valve.PumpId && pump.Running)
                        return pump.Id + " 가동 중 · 흡입 밸브 " + valve.Id + " 닫기 잠금";
            }
            return null;
        }

        public static TankInfo FindTank(IList<TankInfo> tanks, string id)
        {
            if (id == null) return null;
            foreach (var t in tanks) if (t.Id == id) return t;
            return null;
        }

        public static bool IsPumpRunning(IList<PumpInfo> pumps, string id)
        {
            foreach (var p in pumps) if (p.Id == id) return p.Running;
            return false;
        }
    }
}
