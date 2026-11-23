using System;
using APC.UI.Theming;

namespace InjectionMonitor.Data
{
    /// <summary>판정 결과: 상태색 + 짧은 사유(타일용) + 상세 문구(알람바용). 정상이면 Reason·Detail = null.</summary>
    internal struct Verdict
    {
        public readonly StatusKind Status;
        public readonly string Reason;
        public readonly string Detail;

        public Verdict(StatusKind status, string reason, string detail)
        {
            Status = status; Reason = reason; Detail = detail;
        }

        public static readonly Verdict Ok = new Verdict(StatusKind.Run, null, null);

        public int Severity { get { return Status == StatusKind.Alarm ? 2 : Status == StatusKind.Warn ? 1 : 0; } }
    }

    /// <summary>
    /// 판정 함수 한 곳. 카드·타일·헤더가 모두 이것을 쓴다.
    /// 상·하한은 허용 한계: 같은 값은 알람이 아니다(초과·미달부터). 경고 시작값과 같은 값부터 경고.
    /// 아래 기준값은 요구서에 없어 정한 값 — 여기서만 고친다.
    /// </summary>
    internal static class Judge
    {
        public const double TempUpper = 65.0;        // 금형 온도 상한 ℃
        public const double TempLower = 55.0;        // 금형 온도 하한 ℃
        public const double TempWarnUpper = 63.0;    // 이 값 이상이면 경고
        public const double TempWarnLower = 57.0;    // 이 값 이하이면 경고
        public const double PressureWarnMargin = 10.0; // 압력 경고 시작 = 상한 - 10.0 bar
        public const double CycleUpperMargin = 5.0;  // 사이클 상한 = 목표 + 5.0 s
        public const double CycleWarnMargin = 3.0;   // 사이클 경고 시작 = 목표 + 3.0 s

        public static double PressureWarn(RecipeInfo r) { return r.PressureUpper - PressureWarnMargin; }
        public static double CycleUpper(RecipeInfo r) { return r.CycleTarget + CycleUpperMargin; }
        public static double CycleWarn(RecipeInfo r) { return r.CycleTarget + CycleWarnMargin; }

        public static string F(double v) { return v.ToString("0.0"); }

        public static Verdict Temp(double? v)
        {
            return v.HasValue ? Check("온도", "금형 온도", "℃", v.Value, TempLower, TempWarnLower, TempWarnUpper, TempUpper) : Verdict.Ok;
        }

        public static Verdict Pressure(double? v, RecipeInfo r)
        {
            return v.HasValue ? Check("압력", "사출 압력", "bar", v.Value, null, null, PressureWarn(r), r.PressureUpper) : Verdict.Ok;
        }

        public static Verdict Cycle(double? v, RecipeInfo r)
        {
            return v.HasValue ? Check("사이클", "사이클 타임", "s", v.Value, null, null, CycleWarn(r), CycleUpper(r)) : Verdict.Ok;
        }

        private static Verdict Check(string brief, string full, string unit, double raw, double? lower, double? warnLower, double warnUpper, double upper)
        {
            double x = Math.Round(raw, 1);
            string v = F(x) + " " + unit;
            if (x > upper) return new Verdict(StatusKind.Alarm, brief + " 상한 초과", full + " 상한 초과 " + v + " (상한 " + F(upper) + ")");
            if (lower.HasValue && x < lower.Value) return new Verdict(StatusKind.Alarm, brief + " 하한 미달", full + " 하한 미달 " + v + " (하한 " + F(lower.Value) + ")");
            if (x >= warnUpper) return new Verdict(StatusKind.Warn, brief + " 상한 근접", full + " 상한 근접 " + v + " (상한 " + F(upper) + ")");
            if (warnLower.HasValue && x <= warnLower.Value) return new Verdict(StatusKind.Warn, brief + " 하한 근접", full + " 하한 근접 " + v + " (하한 " + F(lower.Value) + ")");
            return Verdict.Ok;
        }

        /// <summary>설비 한 대: 가동이 아니면 Stop(읽은 값은 판정하지 않는다), 가동이면 세 값 중 나쁜 것(같으면 앞쪽).</summary>
        public static Verdict Machine(MachineInfo m, RecipeInfo r)
        {
            if (!m.IsRunning) return new Verdict(StatusKind.Stop, m.Note, m.Note);
            Verdict worst = Temp(m.Temp);
            Verdict p = Pressure(m.Pressure, r), c = Cycle(m.Cycle, r);
            if (p.Severity > worst.Severity) worst = p;
            if (c.Severity > worst.Severity) worst = c;
            return worst;
        }

        public static string TileText(MachineInfo m, RecipeInfo r)
        {
            return m.Process + " · " + (Machine(m, r).Reason ?? "정상");
        }
    }
}
