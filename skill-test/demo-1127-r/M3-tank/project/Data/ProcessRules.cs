using System.Collections.Generic;
using System.Text;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>레벨·온도 판정, 밸브 인터락, 화면 전체 판정. 화면의 모든 색·문구는 여기서 나온다.</summary>
    internal static class ProcessRules
    {
        public const double LevelWarn = 85;        // 이상이면 경고
        public const double LevelAlarm = 90;       // 초과면 알람(밸브 열기 인터락)
        public const double LevelLowWarn = 10;     // 이하면 하한 경고
        public const double BlendTempMin = 45.0;   // T3 배합 온도, 경계 포함 OK
        public const double BlendTempMax = 55.0;

        public static StatusKind LevelKind(double level)
        {
            if (level > LevelAlarm) return StatusKind.Alarm;
            if (level >= LevelWarn || level <= LevelLowWarn) return StatusKind.Warn;
            return StatusKind.Run;
        }

        public static bool TempOut(TankInfo t)
        {
            return t.TempSpec && (t.Temp < BlendTempMin || t.Temp > BlendTempMax);
        }

        public static StatusKind TankKind(TankInfo t)
        {
            return TempOut(t) ? StatusKind.Alarm : LevelKind(t.Level);
        }

        public static string LevelText(double level) { return level.ToString("0") + " %"; }
        public static string TempText(double temp) { return temp.ToString("0.0") + " ℃"; }

        public static TankInfo FindTank(IList<TankInfo> tanks, string id)
        {
            foreach (TankInfo t in tanks) if (t.Id == id) return t;
            return null;
        }

        public static PumpInfo FindPump(IList<PumpInfo> pumps, string id)
        {
            foreach (PumpInfo p in pumps) if (p.Id == id) return p;
            return null;
        }

        /// <summary>조작이 막히면 이유, 허용이면 "". 열기 = 하류 탱크 레벨 알람, 닫기 = 가동 중 펌프의 흡입 쪽 밸브.</summary>
        public static string BlockReason(ValveInfo v, bool open, IList<TankInfo> tanks, IList<PumpInfo> pumps)
        {
            if (open)
            {
                TankInfo down = FindTank(tanks, v.DownstreamTank);
                if (down != null && LevelKind(down.Level) == StatusKind.Alarm)
                    return down.Id + " 레벨 " + LevelText(down.Level) + " · 알람 기준 " + LevelAlarm.ToString("0") + " % 초과로 열기 차단";
            }
            else if (v.IsSuction)
            {
                PumpInfo pump = FindPump(pumps, v.Pump);
                if (pump != null && pump.Running)
                    return pump.Id + " 가동 중 · 흡입 밸브 " + v.Id + " 닫기 차단";
            }
            return "";
        }

        /// <summary>화면 전체 판정(가장 나쁜 탱크 하나). 알람 탱크면 열기가 막힌 닫힌 밸브를 함께 적는다.</summary>
        public static void Judge(IList<TankInfo> tanks, IList<ValveInfo> valves, IList<PumpInfo> pumps,
                                 out StatusKind kind, out string text, out string sub)
        {
            TankInfo worst = null;
            StatusKind worstKind = StatusKind.Run;
            foreach (TankInfo t in tanks)
            {
                StatusKind k = TankKind(t);
                if (k > worstKind || (worst == null && k != StatusKind.Run)) { worst = t; worstKind = k; }
            }
            kind = worstKind;
            if (worst == null)
            {
                text = "정상 운전"; sub = "탱크 레벨·온도 이상 없음";
                return;
            }
            if (TempOut(worst))
            {
                text = worst.Id + " 배합 온도 이탈  " + TempText(worst.Temp);
                sub = "기준 " + BlendTempMin.ToString("0.0") + "~" + BlendTempMax.ToString("0.0") + " ℃";
                return;
            }
            StatusKind lk = LevelKind(worst.Level);
            if (lk == StatusKind.Alarm)
            {
                text = worst.Id + " 레벨 알람  " + LevelText(worst.Level);
                sub = "알람 기준 " + LevelAlarm.ToString("0") + " % 초과";
                string blocked = BlockedOpenList(worst.Id, valves);
                if (blocked.Length > 0) sub += " · " + blocked + " 열기 차단";
            }
            else if (worst.Level <= LevelLowWarn)
            {
                text = worst.Id + " 레벨 하한 경고  " + LevelText(worst.Level);
                sub = "하한 기준 " + LevelLowWarn.ToString("0") + " % 이하";
            }
            else
            {
                text = worst.Id + " 레벨 경고  " + LevelText(worst.Level);
                sub = "경고 기준 " + LevelWarn.ToString("0") + " % 이상";
            }
        }

        // 하류가 이 탱크인 닫힌 밸브(= 지금 열 수 없는 밸브).
        private static string BlockedOpenList(string tankId, IList<ValveInfo> valves)
        {
            var sb = new StringBuilder();
            foreach (ValveInfo v in valves)
            {
                if (v.Open || v.DownstreamTank != tankId) continue;
                if (sb.Length > 0) sb.Append("·");
                sb.Append(v.Id);
            }
            return sb.ToString();
        }
    }
}
