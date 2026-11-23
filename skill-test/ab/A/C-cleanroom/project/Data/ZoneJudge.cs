using System.Globalization;
using APC.UI.Theming;

namespace CleanroomMonitor.Data
{
    /// <summary>구역 판정 결과: 항목별 상태 + 가장 나쁜 항목의 사유.</summary>
    internal sealed class ZoneJudgement
    {
        public ZoneJudgement(StatusKind status, string statusText, string reason, StatusKind temp, StatusKind humi, StatusKind particle, StatusKind diff)
        {
            Status = status;
            StatusText = statusText;
            Reason = reason;
            Temp = temp;
            Humi = humi;
            Particle = particle;
            Diff = diff;
        }

        public readonly StatusKind Status;
        public readonly string StatusText;
        public readonly string Reason;
        public readonly StatusKind Temp, Humi, Particle, Diff;
    }

    /// <summary>
    /// 판정 함수 한 곳. 상·하한과 같은 값은 알람이 아니다(초과·미만부터 알람).
    /// 경고 시작값(상한 - 여유, 하한 + 여유)부터 경고. 아래 상수가 요구서에 없어 정한 기준값이다.
    /// </summary>
    internal static class ZoneJudge
    {
        public const double TempMargin = 0.5;          // ℃
        public const double HumiMargin = 3.0;          // %RH
        public const double ParticleWarnRatio = 0.8;   // 상한의 80%부터 경고
        public const double DiffLow = 10.0;            // Pa, 차압 하한(설정 dialog 항목 아님)
        public const double DiffMargin = 2.0;          // Pa
        public const int DefaultParticleHigh = 10000;  // 개/ft3 (0.5um, ISO 7 수준)

        public static ZoneJudgement Evaluate(ZoneReading r, ZoneLimits l)
        {
            if (!r.Online)
                return new ZoneJudgement(StatusKind.Stop, "통신 끊김", "통신 끊김", StatusKind.Stop, StatusKind.Stop, StatusKind.Stop, StatusKind.Stop);

            Item t = Check("온도", r.Temp, l.TempLow, l.TempHigh, TempMargin, "℃", "0.0");
            Item h = Check("습도", r.Humi, l.HumiLow, l.HumiHigh, HumiMargin, "%RH", "0.0");
            Item p = Check("파티클", r.Particle, double.MinValue, l.ParticleHigh, l.ParticleHigh * (1 - ParticleWarnRatio), "개/ft³", "#,0");
            Item d = Check("차압", r.DiffPa, DiffLow, double.MaxValue, DiffMargin, "Pa", "0.0");

            Item worst = t;
            foreach (Item i in new[] { h, p, d })
                if (Rank(i.Kind) > Rank(worst.Kind)) worst = i;   // 같으면 앞 항목 유지

            string text = worst.Kind == StatusKind.Alarm ? "알람" : worst.Kind == StatusKind.Warn ? "경고" : "정상";
            return new ZoneJudgement(worst.Kind, text, worst.Text, t.Kind, h.Kind, p.Kind, d.Kind);
        }

        private struct Item
        {
            public StatusKind Kind;
            public string Text;
        }

        private static int Rank(StatusKind k)
        {
            return k == StatusKind.Alarm ? 2 : k == StatusKind.Warn ? 1 : 0;
        }

        private static Item Check(string name, double v, double lo, double hi, double margin, string unit, string fmt)
        {
            string val = v.ToString(fmt, CultureInfo.InvariantCulture) + " " + unit;
            string hiText = hi.ToString(fmt, CultureInfo.InvariantCulture);
            string loText = lo.ToString(fmt, CultureInfo.InvariantCulture);
            if (v > hi) return Make(StatusKind.Alarm, name + " 상한 초과 " + val + " (상한 " + hiText + ")");
            if (v < lo) return Make(StatusKind.Alarm, name + " 하한 미만 " + val + " (하한 " + loText + ")");
            if (v >= hi - margin) return Make(StatusKind.Warn, name + " 상한 근접 " + val + " (상한 " + hiText + ")");
            if (v <= lo + margin) return Make(StatusKind.Warn, name + " 하한 근접 " + val + " (하한 " + loText + ")");
            return Make(StatusKind.Run, "");
        }

        private static Item Make(StatusKind kind, string text)
        {
            return new Item { Kind = kind, Text = text };
        }
    }
}
