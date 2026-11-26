using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace PressLineOee.Data
{
    /// <summary>OEE·생산일자·사유 필요 판정. 화면은 이 함수만 쓴다(기준값은 상수 한 곳).</summary>
    internal static class Oee
    {
        /// <summary>하루 시작 시각(시). 주간 06~14 / 오후 14~22 / 야간 22~06.</summary>
        public const int DayStartHour = 6;
        /// <summary>이 시간(분) 이상 정지만 작업자가 사유를 입력한다. 미만은 설비 신호 자동 기록(순간정지).</summary>
        public const int ReasonRequiredMin = 5;
        public const string AutoCategory = "순간정지(자동)";
        public const string PlannedCategory = "계획정지";

        public sealed class Metrics
        {
            public int ElapsedMin, PlannedMin, LoadMin, DownMin, RunMin, Produced, Good;
            public double TheoreticalCtMin, Availability, Performance, Quality, Value;
        }

        public static DateTime ProductionDayStart(DateTime now)
        {
            var d = now.Date.AddHours(DayStartHour);
            return now < d ? d.AddDays(-1) : d;
        }

        public static string ShiftName(DateTime now)
        {
            if (now.Hour >= 6 && now.Hour < 14) return "주간";
            if (now.Hour >= 14 && now.Hour < 22) return "오후";
            return "야간";
        }

        public static int DurationMin(StopEvent e, DateTime now)
        {
            return (int)Math.Round(((e.End ?? now) - e.Start).TotalMinutes);
        }

        public static bool NeedsReason(StopEvent e, DateTime now)
        {
            return e.Kind == StopKind.Unplanned && string.IsNullOrEmpty(e.Category) && DurationMin(e, now) >= ReasonRequiredMin;
        }

        /// <summary>사유를 입력할 수 없는 이유. 입력할 수 있으면 null.</summary>
        public static string WhyNoReason(StopEvent e, DateTime now)
        {
            if (e.Kind == StopKind.Planned) return "계획정지는 사유 입력 대상이 아닙니다.";
            if (!string.IsNullOrEmpty(e.Category))
                return DurationMin(e, now) < ReasonRequiredMin ? "5분 미만 정지는 설비가 자동 기록합니다." : "이미 입력된 정지입니다.";
            return DurationMin(e, now) < ReasonRequiredMin ? "5분 미만 정지는 입력하지 않습니다." : null;
        }

        /// <summary>지금 상태: 가동=Run, 비가동=Alarm, 계획정지=Stop.</summary>
        public static StatusKind MachineStatus(string machine, IEnumerable<StopEvent> events)
        {
            foreach (var e in events)
                if (e.Machine == machine && e.End == null)
                    return e.Kind == StopKind.Planned ? StatusKind.Stop : StatusKind.Alarm;
            return StatusKind.Run;
        }

        public static Metrics Compute(MachineInfo m, IEnumerable<StopEvent> events, DateTime now)
        {
            var r = new Metrics { Produced = m.Produced, Good = m.Good, TheoreticalCtMin = m.TheoreticalCtMin };
            r.ElapsedMin = (int)Math.Floor((now - ProductionDayStart(now)).TotalMinutes);
            foreach (var e in events)
            {
                if (e.Machine != m.Id) continue;
                if (e.Kind == StopKind.Planned) r.PlannedMin += DurationMin(e, now);
                else r.DownMin += DurationMin(e, now);
            }
            r.LoadMin = r.ElapsedMin - r.PlannedMin;                       // 부하시간 = 계획정지 제외
            r.RunMin = Math.Max(r.LoadMin - r.DownMin, 0);
            r.Availability = r.LoadMin > 0 ? (double)r.RunMin / r.LoadMin : 0;
            r.Performance = r.RunMin > 0 ? m.TheoreticalCtMin * m.Produced / r.RunMin : 0;
            r.Quality = m.Produced > 0 ? (double)m.Good / m.Produced : 0;
            r.Value = r.Availability * r.Performance * r.Quality;
            return r;
        }
    }
}
