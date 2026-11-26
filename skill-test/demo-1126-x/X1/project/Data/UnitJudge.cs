using System.Globalization;
using APC.UI.Theming;

namespace Polishing.Data
{
    /// <summary>설비 판정 한 곳. 표·판정판·카드가 모두 이 함수를 쓴다.</summary>
    internal static class UnitJudge
    {
        /// <summary>RPM 허용 편차(rpm): SV ± 이 값. 같으면 정상, 넘으면 알람.</summary>
        public const int RpmTolerance = 10;
        /// <summary>시작 후·종료 전 이 시간(분) 이내는 PV 이탈을 알람으로 보지 않는다(경계 포함).</summary>
        public const double ExcludeMin = 3.0;
        /// <summary>작업 완료 시 PV 작업시간과 SV의 허용 차이(분).</summary>
        public const double TimeGapLimitMin = 1.0;

        public static UnitVerdict Evaluate(UnitInfo u)
        {
            switch (u.State)
            {
                case UnitState.Offline: return new UnitVerdict(StatusKind.Stop, "통신 끊김", "");
                case UnitState.Waiting: return new UnitVerdict(StatusKind.Stop, "대기", "");
                case UnitState.Sent: return new UnitVerdict(StatusKind.Info, "전송됨", "");
                case UnitState.Done: return EvaluateDone(u);
                default: return EvaluateRunning(u);
            }
        }

        private static UnitVerdict EvaluateDone(UnitInfo u)
        {
            if (!u.PvMin.HasValue) return new UnitVerdict(StatusKind.Run, "완료", "");
            double gap = u.PvMin.Value - u.SvMin;
            if (gap < 0) gap = -gap;
            if (gap > TimeGapLimitMin)
                return new UnitVerdict(StatusKind.Alarm, "완료", "작업 시간 차이 초과 " + Fmt(gap) + " 분 (허용 " + Fmt(TimeGapLimitMin) + ")");
            return new UnitVerdict(StatusKind.Run, "완료", "");
        }

        private static UnitVerdict EvaluateRunning(UnitInfo u)
        {
            var run = new UnitVerdict(StatusKind.Run, "가동", "");
            if (!u.PvMin.HasValue || !u.RpmPv.HasValue) return run;
            double elapsed = u.PvMin.Value, remain = u.SvMin - elapsed;
            if (elapsed <= ExcludeMin || remain <= ExcludeMin) return run;   // 시작·종료 구간은 판정하지 않는다
            int upper = u.RpmSv + RpmTolerance, lower = u.RpmSv - RpmTolerance;
            if (u.RpmPv.Value > upper)
                return new UnitVerdict(StatusKind.Alarm, "가동", "RPM 상한 초과 " + u.RpmPv.Value + " rpm (상한 " + upper + ")");
            if (u.RpmPv.Value < lower)
                return new UnitVerdict(StatusKind.Alarm, "가동", "RPM 하한 미달 " + u.RpmPv.Value + " rpm (하한 " + lower + ")");
            return run;
        }

        private static string Fmt(double v) { return v.ToString("0.0", CultureInfo.InvariantCulture); }
    }
}
