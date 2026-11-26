using System;
using System.Globalization;
using APC.UI.Theming;

namespace DieChangeManager.Data
{
    /// <summary>금형 한 개. 불변.</summary>
    internal sealed class MoldInfo
    {
        public MoldInfo(string id, string location, int limit, int count, bool repairWaiting = false)
        {
            Id = id; Location = location; Limit = limit; Count = count; RepairWaiting = repairWaiting;
        }

        public string Id { get; private set; }
        /// <summary>장착 설비 이름(DC-01 …) 또는 보관실.</summary>
        public string Location { get; private set; }
        public int Limit { get; private set; }
        public int Count { get; private set; }
        public bool RepairWaiting { get; private set; }
        public int Remaining { get { return Limit - Count; } }
    }

    /// <summary>다이캐스팅기 한 대와 장착 금형·다음 생산 지시. 불변.</summary>
    internal sealed class MachineInfo
    {
        public MachineInfo(string name, string mountedId, string nextMoldId, string nextOrder, bool changing, bool stopped)
        {
            Name = name; MountedId = mountedId; NextMoldId = nextMoldId; NextOrder = nextOrder; Changing = changing; Stopped = stopped;
        }

        public string Name { get; private set; }
        /// <summary>장착 금형 번호. 교체 중이라 떼어 낸 상태면 "".</summary>
        public string MountedId { get; private set; }
        public string NextMoldId { get; private set; }
        public string NextOrder { get; private set; }
        public bool Changing { get; private set; }
        public bool Stopped { get; private set; }
    }

    /// <summary>오늘 교체 계획·실적 한 줄. 불변.</summary>
    internal sealed class PlanInfo
    {
        public PlanInfo(string machine, string removedId, string mountedId, DateTime planned, DateTime? start, DateTime? end)
        {
            Machine = machine; RemovedId = removedId; MountedId = mountedId; Planned = planned; Start = start; End = end;
        }

        public string Machine { get; private set; }
        public string RemovedId { get; private set; }
        public string MountedId { get; private set; }
        public DateTime Planned { get; private set; }
        public DateTime? Start { get; private set; }
        public DateTime? End { get; private set; }

        /// <summary>소요 분(정수). 시작 전이면 null, 진행 중이면 지금까지.</summary>
        public int? Minutes(DateTime now)
        {
            if (!Start.HasValue) return null;
            return (int)Math.Round(((End ?? now) - Start.Value).TotalMinutes);
        }
    }

    /// <summary>금형 수명·교체 시간·설비 상태 판정. 화면의 모든 곳(판정판·타일·표·dialog)이 이 함수만 쓴다.</summary>
    internal static class MoldRules
    {
        public const int WarnPercent = 10;        // 남은 비율 10% 이하 = 주의 (같으면 주의)
        public const int ReplacePercent = 3;      // 남은 비율 3% 이하 = 교체 필요 (같으면 교체 필요)
        public const int StandardMinutes = 45;    // 교체 표준 시간
        public const int DelayMinutes = 60;       // 이 값을 넘으면 지연 (같으면 지연 아님)
        public const int PreheatMin = 180, PreheatMax = 220;   // ℃

        public static StatusKind LifeKind(MoldInfo m)
        {
            if (m.Remaining <= 0 || m.Remaining * 100L <= m.Limit * (long)ReplacePercent) return StatusKind.Alarm;
            if (m.Remaining * 100L <= m.Limit * (long)WarnPercent) return StatusKind.Warn;
            return StatusKind.Run;
        }

        public static string LifeWord(MoldInfo m)
        {
            if (m.Remaining <= 0) return "한계 도달";
            var k = LifeKind(m);
            return k == StatusKind.Alarm ? "교체 필요" : k == StatusKind.Warn ? "주의" : "정상";
        }

        public static string Count(int n) { return n.ToString("#,0", CultureInfo.InvariantCulture); }

        public static string Ratio(MoldInfo m)
        {
            double r = m.Limit > 0 ? Math.Max(0, m.Remaining) * 100.0 / m.Limit : 0;
            return r.ToString("0.0", CultureInfo.InvariantCulture);
        }

        public static string MoldState(MoldInfo m) { return m.RepairWaiting ? "수리 대기" : "정상"; }

        // 설비 상태 한 곳: 교체 중 > 정지 > 교체 예정(장착 금형 남은 10% 이하) > 생산
        public static string MachineWord(MachineInfo m, MoldInfo mounted)
        {
            if (m.Changing) return "교체 중";
            if (m.Stopped) return "정지";
            if (mounted != null && LifeKind(mounted) != StatusKind.Run) return "교체 예정";
            return "생산";
        }

        public static StatusKind MachineKind(MachineInfo m, MoldInfo mounted)
        {
            if (m.Changing) return StatusKind.Info;
            bool limit = mounted != null && LifeKind(mounted) == StatusKind.Alarm;
            if (m.Stopped) return limit ? StatusKind.Alarm : StatusKind.Stop;
            if (mounted == null) return StatusKind.Stop;
            return LifeKind(mounted);
        }

        public static StatusKind PlanKind(PlanInfo p, DateTime now)
        {
            if (!p.Start.HasValue) return StatusKind.Stop;
            if (Minutes(p, now) > DelayMinutes) return StatusKind.Warn;
            return p.End.HasValue ? StatusKind.Run : StatusKind.Info;
        }

        public static string PlanWord(PlanInfo p, DateTime now)
        {
            if (!p.Start.HasValue) return "예정";
            bool late = Minutes(p, now) > DelayMinutes;
            if (p.End.HasValue) return late ? "지연 완료" : "완료";
            return late ? "지연 중" : "진행 중";
        }

        private static int Minutes(PlanInfo p, DateTime now) { return p.Minutes(now) ?? 0; }

        /// <summary>장착 금형을 등록할 수 없는 이유. 등록 가능하면 null.</summary>
        public static string BlockReason(MachineInfo machine, MoldInfo scanned)
        {
            if (machine.Changing) return machine.Name + " 교체 진행 중 · 등록 불가";
            if (scanned == null) return null;
            if (scanned.RepairWaiting) return "수리 대기 금형 · 장착 불가";
            if (scanned.Remaining <= 0) return "남은 타수 " + Count(Math.Max(0, scanned.Remaining)) + " · 장착 불가";
            if (scanned.Id != machine.NextMoldId) return "지시 금형 불일치 · " + scanned.Id + " ≠ " + machine.NextMoldId;
            return null;
        }
    }
}
