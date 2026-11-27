using System.Collections.Generic;
using APC.UI.Theming;

namespace LineBalanceBoard.Data
{
    /// <summary>공정 한 줄. 사이클 타임은 0.1초 단위 정수(594 = 59.4초)로 받아 비교 오차를 없앤다.</summary>
    internal sealed class ProcessInfo
    {
        public ProcessInfo(string code, string work, int cycleTenths, string workerId)
        {
            Code = code; Work = work; CycleTenths = cycleTenths; WorkerId = workerId;
        }

        public readonly string Code;
        public readonly string Work;
        public readonly int CycleTenths;
        /// <summary>배치 작업자 사번(EMP-nnnnn). 미배치는 "".</summary>
        public readonly string WorkerId;
    }

    /// <summary>작업자와 공정별 자격(공정 코드 → 만료일 yyyy-MM-dd, 만료일 당일까지 유효).</summary>
    internal sealed class WorkerInfo
    {
        public WorkerInfo(string id, string name, IDictionary<string, string> qualExpiry)
        {
            Id = id; Name = name; QualExpiry = qualExpiry;
        }

        public readonly string Id;
        public readonly string Name;
        public readonly IDictionary<string, string> QualExpiry;
    }

    internal enum QualState { Valid, Expired, None }

    /// <summary>판정 기준값과 판정 함수. 화면 어디서나 이 함수만 쓴다.</summary>
    internal static class BalanceRules
    {
        public const int WarnPercent = 95;     // 택트 대비 95 % 이상 = 경고(경계 포함)
        public const int AlarmPercent = 100;   // 100 % 초과 = 병목(경계 100 %는 경고)
        public const int SecondsPerHour = 3600;

        public static StatusKind Judge(int cycleTenths, int taktTenths)
        {
            if (cycleTenths * 100 > AlarmPercent * taktTenths) return StatusKind.Alarm;
            if (cycleTenths * 100 >= WarnPercent * taktTenths) return StatusKind.Warn;
            return StatusKind.Run;
        }

        public static string JudgeWord(StatusKind kind)
        {
            return kind == StatusKind.Alarm ? "병목" : kind == StatusKind.Warn ? "경고" : "정상";
        }

        /// <summary>택트 대비 %, 표시용 정수(반올림).</summary>
        public static int Percent(int cycleTenths, int taktTenths)
        {
            return (cycleTenths * 100 + taktTenths / 2) / taktTenths;
        }

        /// <summary>라인 예상 시간당 생산량 = 3600 / 최대 사이클 타임, 정수 내림.</summary>
        public static int PerHour(int maxCycleTenths)
        {
            return SecondsPerHour * 10 / maxCycleTenths;
        }

        public static string Seconds(int tenths)
        {
            return (tenths / 10).ToString() + "." + (tenths % 10).ToString();
        }

        public static QualState Qualification(WorkerInfo worker, string processCode, string today)
        {
            string expiry;
            if (worker == null || !worker.QualExpiry.TryGetValue(processCode, out expiry)) return QualState.None;
            return string.CompareOrdinal(expiry, today) >= 0 ? QualState.Valid : QualState.Expired;
        }
    }
}
