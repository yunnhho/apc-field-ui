using System.Collections.Generic;
using System.Text;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>판정·인터락 기준 한 곳. 화면, 알람 문구, 밸브 dialog가 모두 이 함수를 쓴다.</summary>
    internal static class FlowRules
    {
        public const int AlarmLevel = 90;      // 90 % 초과 = 알람(밸브 열기 인터락). 90 %는 경고
        public const int WarnLevel = 85;       // 85 % 이상 경고
        public const int LowLevel = 10;        // 10 % 이하 하한 경고
        public const double TempMin = 45.0;    // T3 배합 온도 45.0~55.0 ℃, 경계 포함 OK
        public const double TempMax = 55.0;

        public static StatusKind LevelKind(int level)
        {
            if (level > AlarmLevel) return StatusKind.Alarm;
            if (level >= WarnLevel || level <= LowLevel) return StatusKind.Warn;
            return StatusKind.Run;
        }

        public static StatusKind TempKind(double temp)
        {
            return temp < TempMin || temp > TempMax ? StatusKind.Alarm : StatusKind.Run;
        }

        public static TankInfo FindTank(IList<TankInfo> tanks, string id)
        {
            foreach (var t in tanks) if (t.Id == id) return t;
            return null;
        }

        public static PumpInfo FindPump(IList<PumpInfo> pumps, string id)
        {
            foreach (var p in pumps) if (p.Id == id) return p;
            return null;
        }

        /// <summary>열기: 하류 탱크 레벨이 알람이면 막는다. 닫기: 그 배관 펌프가 가동 중이면 흡입 쪽 밸브를 막는다. 막히지 않으면 null.</summary>
        public static string BlockReason(ValveInfo valve, IList<TankInfo> tanks, IList<PumpInfo> pumps)
        {
            if (!valve.IsOpen)
            {
                var tank = valve.DownstreamTankId == null ? null : FindTank(tanks, valve.DownstreamTankId);
                if (tank != null && LevelKind(tank.Level) == StatusKind.Alarm)
                    return tank.Id + " 레벨 " + tank.Level + " % 알람 · 열기 불가";
                return null;
            }
            var pump = valve.PumpId == null ? null : FindPump(pumps, valve.PumpId);
            if (valve.SuctionSide && pump != null && pump.Running)
                return pump.Id + " 가동 중 · 흡입 밸브 닫기 불가";
            return null;
        }

        public sealed class JudgeResult
        {
            public StatusKind Kind;
            public string Text;
            public string Sub;
        }

        /// <summary>전 공정에서 가장 나쁜 항목 하나(같은 등급이 여럿이면 건수 + 앞 두 개).</summary>
        public static JudgeResult Judge(IList<TankInfo> tanks, IList<ValveInfo> valves)
        {
            var worst = StatusKind.Run;
            var names = new List<string>();      // "T3 레벨"
            var details = new List<string>();    // 단일 건일 때 SubText
            foreach (var t in tanks)
            {
                Add(LevelKind(t.Level), t.Id + " 레벨", LevelDetail(t, valves), ref worst, names, details);
                if (t.HasTempSpec)
                    Add(TempKind(t.Temp), t.Id + " 온도", t.Temp.ToString("0.0") + " ℃ · 기준 "
                        + TempMin.ToString("0.0") + "~" + TempMax.ToString("0.0"), ref worst, names, details);
            }
            if (worst == StatusKind.Run)
                return new JudgeResult { Kind = StatusKind.Run, Text = "정상", Sub = "전 탱크 레벨·온도 정상" };

            string word = worst == StatusKind.Alarm ? "알람" : "경고";
            if (names.Count == 1)
                return new JudgeResult { Kind = worst, Text = names[0] + " " + (names[0].EndsWith("온도") ? (worst == StatusKind.Alarm ? "이탈" : "경고") : word), Sub = details[0] };
            string sub = names[0] + "·" + names[1] + (names.Count > 2 ? " 외" : "");
            return new JudgeResult { Kind = worst, Text = word + " " + names.Count + "건", Sub = sub };
        }

        private static void Add(StatusKind kind, string name, string detail, ref StatusKind worst, List<string> names, List<string> details)
        {
            if (Rank(kind) == 0 || Rank(kind) < Rank(worst)) return;
            if (Rank(kind) > Rank(worst)) { names.Clear(); details.Clear(); worst = kind; }
            names.Add(name);
            details.Add(detail);
        }

        private static int Rank(StatusKind kind)
        {
            if (kind == StatusKind.Alarm) return 2;
            return kind == StatusKind.Warn ? 1 : 0;
        }

        private static string LevelDetail(TankInfo t, IList<ValveInfo> valves)
        {
            var kind = LevelKind(t.Level);
            if (kind == StatusKind.Alarm)
            {
                var sb = new StringBuilder();
                foreach (var v in valves)
                    if (!v.IsOpen && v.DownstreamTankId == t.Id)
                        sb.Append(sb.Length == 0 ? v.Id : "·" + v.Id);
                return "레벨 " + t.Level + " %" + (sb.Length == 0 ? "" : " · " + sb + " 열기 막힘");
            }
            return t.Level <= LowLevel ? t.Level + " % · 하한 " + LowLevel + " % 이하" : t.Level + " % · 경고 " + WarnLevel + " % 이상";
        }
    }
}
