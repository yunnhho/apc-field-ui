using System;
using System.Globalization;
using APC.UI.Theming;

namespace BumperPaintBoothMonitor.Data
{
    /// <summary>관리 항목 하나의 관리 범위와 판정. 항목마다 판정은 이 클래스의 Judge 하나만 쓴다.</summary>
    internal sealed class ItemSpec
    {
        /// <summary>경계에서 관리 폭의 이 비율 안쪽이면 주의(경계값 포함).</summary>
        public const double WarnBandRatio = 0.10;
        /// <summary>이탈이 이 시간(초) 이상 이어지면 라인 투입 정지 권고.</summary>
        public const int StopAdviceSeconds = 180;

        public readonly string Name, Unit;
        public readonly int Decimals;
        public readonly double Lo, Hi;
        public readonly bool Managed, Signed;

        public ItemSpec(string name, string unit, int decimals, double lo, double hi, bool signed = false)
        {
            Name = name; Unit = unit; Decimals = decimals; Lo = lo; Hi = hi; Managed = true; Signed = signed;
        }

        private ItemSpec(string name, string unit)
        {
            Name = name; Unit = unit;
        }

        public static ItemSpec Unmanaged(string name, string unit) { return new ItemSpec(name, unit); }

        public string Fmt(double v) { return v.ToString("F" + Decimals, CultureInfo.InvariantCulture); }

        public string RangeText()
        {
            string plus = Signed ? "+" : "";
            return plus + Fmt(Lo) + " ~ " + plus + Fmt(Hi);
        }

        /// <summary>범위 밖 = 이탈, 경계 10% 안쪽(경계 포함) = 주의. 표시 자릿수로 반올림한 값으로 비교한다.</summary>
        public StatusKind Judge(double raw)
        {
            double v = Math.Round(raw, Decimals);
            if (v < Lo || v > Hi) return StatusKind.Alarm;
            double band = (Hi - Lo) * WarnBandRatio + 1e-9;
            if (v <= Lo + band || v >= Hi - band) return StatusKind.Warn;
            return StatusKind.Run;
        }

        /// <summary>"상한 초과 78 %RH (상한 75)" 형태. 이탈일 때만 쓴다.</summary>
        public string LimitText(double raw)
        {
            double v = Math.Round(raw, Decimals);
            return v > Hi ? "상한 초과 " + Fmt(v) + " " + Unit + " (상한 " + Fmt(Hi) + ")"
                          : "하한 미달 " + Fmt(v) + " " + Unit + " (하한 " + Fmt(Lo) + ")";
        }
    }

    internal static class BoothSpecs
    {
        public const int Temp = 0, Humi = 1, Wind = 2, Press = 3;

        public static ItemSpec[] For(string booth)
        {
            if (booth == "오븐")
                return new[]
                {
                    new ItemSpec("온도", "℃", 1, 135.0, 145.0),
                    ItemSpec.Unmanaged("습도", "%RH"), ItemSpec.Unmanaged("풍속", "m/s"), ItemSpec.Unmanaged("차압", "Pa"),
                };
            return new[]
            {
                new ItemSpec("온도", "℃", 1, 21.0, 25.0),
                new ItemSpec("습도", "%RH", 0, 55, 75),
                new ItemSpec("풍속", "m/s", 2, 0.30, 0.50),
                new ItemSpec("차압", "Pa", 0, 5, 15, true),
            };
        }
    }

    /// <summary>부스 한 곳의 현재 상태. Values는 온도·습도·풍속·차압 순(관리 안 함 = NaN).</summary>
    internal sealed class BoothInfo
    {
        public string Name;
        public bool Stopped;
        public ItemSpec[] Specs;
        public double[] Values;
        public double[] TempTrend, HumiTrend;   // 최근 8시간, 왼쪽 = 과거, 마지막 점 = 현재값

        public StatusKind ItemStatus(int i)
        {
            if (Stopped || !Specs[i].Managed || double.IsNaN(Values[i])) return StatusKind.Stop;
            return Specs[i].Judge(Values[i]);
        }

        public StatusKind Status()
        {
            if (Stopped) return StatusKind.Stop;
            bool warn = false;
            for (int i = 0; i < Specs.Length; i++)
            {
                var s = ItemStatus(i);
                if (s == StatusKind.Alarm) return s;
                if (s == StatusKind.Warn) warn = true;
            }
            return warn ? StatusKind.Warn : StatusKind.Run;
        }
    }

    internal sealed class LineInput
    {
        public string Hanger, PaintLot, ColorCode;
    }

    /// <summary>환경 이탈 한 건. End == null이면 아직 이어지는 중.</summary>
    internal sealed class Deviation
    {
        public const int BumpersPerHanger = 2;
        private const int HangerIntervalSeconds = 90;   // 1.5분

        public string Booth;
        public int ItemIndex;
        public DateTime Start;
        public DateTime? End;
        public double Worst;                  // 가장 크게 벗어난 값
        public string Action = "", Remark = "", Confirmer = "";

        public bool Open { get { return End == null; } }
        public bool HasAction { get { return Action.Length > 0; } }
        public ItemSpec Spec { get { return BoothSpecs.For(Booth)[ItemIndex]; } }

        public TimeSpan Duration(DateTime now) { return (End ?? now) - Start; }

        // 행거 번호 = 06:00 가동 시작부터 1.5분 간격 순번
        public static int HangerNo(DateTime t) { return 1000 + (int)((t - t.Date.AddHours(6)).TotalSeconds / HangerIntervalSeconds); }

        public int FirstHanger() { return HangerNo(Start); }
        public int LastHanger(DateTime now) { return HangerNo(End ?? now); }
        public int HangerCount(DateTime now) { return LastHanger(now) - FirstHanger() + 1; }
        public string HangerRange(DateTime now) { return "H-" + FirstHanger() + " ~ H-" + LastHanger(now); }

        /// <summary>항목별 품질 위험(도메인 설명). 해당 없으면 "".</summary>
        public string Risk()
        {
            if (ItemIndex == BoothSpecs.Humi) return "백화(blushing) 위험";
            if (ItemIndex == BoothSpecs.Wind) return "먼지 부착 불량 위험";
            return "";
        }
    }
}
