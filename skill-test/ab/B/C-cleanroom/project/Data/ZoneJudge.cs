using System.Globalization;
using APC.UI.Theming;

namespace CleanroomMonitor.Data
{
    /// <summary>항목 하나의 판정 결과(상태 + 사유 문구). 정상이면 Reason = "".</summary>
    internal sealed class ItemJudgement
    {
        public readonly StatusKind Kind;
        public readonly string Reason;
        public ItemJudgement(StatusKind kind, string reason) { Kind = kind; Reason = reason; }
    }

    /// <summary>구역 전체 판정. 항목별 판정(셀 색)과 가장 나쁜 사유(상태 문구)를 함께 준다.</summary>
    internal sealed class ZoneJudgement
    {
        public readonly StatusKind Kind;
        public readonly string StatusText;     // 표 상태 칸 문구: "정상" / "경고 · 사유" / "알람 · 사유" / "통신 끊김"
        public readonly string AlarmMessage;   // 알람바 문구(알람일 때만)
        public readonly ItemJudgement Temp, Humi, Particle, Pressure;

        public ZoneJudgement(StatusKind kind, string statusText, string alarmMessage,
            ItemJudgement temp, ItemJudgement humi, ItemJudgement particle, ItemJudgement pressure)
        {
            Kind = kind; StatusText = statusText; AlarmMessage = alarmMessage;
            Temp = temp; Humi = humi; Particle = particle; Pressure = pressure;
        }
    }

    /// <summary>
    /// 판정 함수 한 곳. 상·하한은 허용 한계: 같은 값은 알람이 아니고, 초과(미만)부터 알람.
    /// 경고 구간(근접)은 시작값과 같은 값부터 경고. 아래 상수가 요구서에 없어 정한 기준값이다.
    /// </summary>
    internal static class ZoneJudge
    {
        public const double TempWarnMargin = 0.5;        // ℃: 상·하한에서 이만큼 안쪽부터 경고
        public const double HumiWarnMargin = 3.0;        // %RH
        public const double ParticleWarnRatio = 0.8;     // 파티클 상한의 80%부터 경고
        public const double PressureMin = 10.0;          // Pa: 차압 하한(미만 알람)
        public const double PressureWarnMargin = 1.0;    // Pa: 하한 + 1.0 이하부터 경고

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string T(double v) { return v.ToString("0.0", Inv); }
        public static string P(double v) { return v.ToString("#,0", Inv); }

        public static ZoneJudgement Judge(ZoneReading z, ZoneLimits lim)
        {
            var ok = new ItemJudgement(StatusKind.Run, "");
            if (!z.Connected) return new ZoneJudgement(StatusKind.Stop, "통신 끊김", null, ok, ok, ok, ok);

            var temp = JudgeRange("온도", z.Temperature.Value, lim.TempLow, lim.TempHigh, TempWarnMargin, "℃");
            var humi = JudgeRange("습도", z.Humidity.Value, lim.HumiLow, lim.HumiHigh, HumiWarnMargin, "%RH");
            var part = JudgeParticle(z.Particles.Value, lim.ParticleMax);
            var pres = JudgePressure(z.Pressure.Value);

            var worst = temp;                                   // 같으면 항목 순서 앞쪽
            foreach (var j in new[] { humi, part, pres }) if (Rank(j.Kind) > Rank(worst.Kind)) worst = j;

            if (worst.Kind == StatusKind.Alarm)
                return new ZoneJudgement(StatusKind.Alarm, "알람 · " + worst.Reason, z.Name + " · " + worst.Reason, temp, humi, part, pres);
            if (worst.Kind == StatusKind.Warn)
                return new ZoneJudgement(StatusKind.Warn, "경고 · " + worst.Reason, null, temp, humi, part, pres);
            return new ZoneJudgement(StatusKind.Run, "정상", null, temp, humi, part, pres);
        }

        private static int Rank(StatusKind k) { return k == StatusKind.Alarm ? 2 : k == StatusKind.Warn ? 1 : 0; }

        private static ItemJudgement JudgeRange(string name, double v, double lo, double hi, double margin, string unit)
        {
            string lim = " " + unit;
            if (v > hi) return new ItemJudgement(StatusKind.Alarm, name + " 상한 초과 " + T(v) + lim + " (상한 " + T(hi) + ")");
            if (v < lo) return new ItemJudgement(StatusKind.Alarm, name + " 하한 미달 " + T(v) + lim + " (하한 " + T(lo) + ")");
            if (v >= hi - margin) return new ItemJudgement(StatusKind.Warn, name + " 상한 근접 " + T(v) + lim + " (상한 " + T(hi) + ")");
            if (v <= lo + margin) return new ItemJudgement(StatusKind.Warn, name + " 하한 근접 " + T(v) + lim + " (하한 " + T(lo) + ")");
            return new ItemJudgement(StatusKind.Run, "");
        }

        private static ItemJudgement JudgeParticle(double v, double max)
        {
            if (v > max) return new ItemJudgement(StatusKind.Alarm, "파티클 상한 초과 " + P(v) + " 개/ft³ (상한 " + P(max) + ")");
            if (v >= max * ParticleWarnRatio) return new ItemJudgement(StatusKind.Warn, "파티클 상한 근접 " + P(v) + " 개/ft³ (상한 " + P(max) + ")");
            return new ItemJudgement(StatusKind.Run, "");
        }

        private static ItemJudgement JudgePressure(double v)
        {
            if (v < PressureMin) return new ItemJudgement(StatusKind.Alarm, "차압 하한 미달 " + T(v) + " Pa (하한 " + T(PressureMin) + ")");
            if (v <= PressureMin + PressureWarnMargin) return new ItemJudgement(StatusKind.Warn, "차압 하한 근접 " + T(v) + " Pa (하한 " + T(PressureMin) + ")");
            return new ItemJudgement(StatusKind.Run, "");
        }
    }
}
