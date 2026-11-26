using System;
using APC.UI.Theming;

namespace BumperPaintBoothMonitor.Data
{
    internal enum MeasureItem { Temperature = 0, Humidity = 1, WindSpeed = 2, Pressure = 3 }

    /// <summary>한 부스·한 항목의 관리 범위. Managed=false면 관리 안 함(값·판정 없음).</summary>
    internal sealed class ItemSpec
    {
        public readonly bool Managed;
        public readonly double Lower, Upper;
        public readonly int Decimals;
        public readonly string Name, Unit;

        public ItemSpec(string name, string unit, int decimals, double lower, double upper, bool managed)
        {
            Name = name; Unit = unit; Decimals = decimals; Lower = lower; Upper = upper; Managed = managed;
        }

        public double Center { get { return (Lower + Upper) / 2; } }
        public string Format(double value) { return value.ToString("F" + Decimals); }
        public string RangeText { get { return Format(Lower) + "~" + Format(Upper); } }
    }

    /// <summary>
    /// 관리 범위와 판정(화면 전체가 이 한 곳만 쓴다: 타일·카드·이력·dialog).
    /// 정한 기준값: 전처리·프라이머는 베이스·클리어와 같은 범위(요청에 없음), 오븐 차압은 관리 안 함.
    /// </summary>
    internal static class BoothSpec
    {
        public static readonly string[] BoothNames = { "전처리", "프라이머", "베이스", "클리어", "오븐" };
        public const int OvenIndex = 4;
        public const double WarnBandRatio = 0.10;        // 관리 범위 폭의 10 % 안쪽 = 주의
        public static readonly TimeSpan StopRecommend = TimeSpan.FromMinutes(3);   // 이탈 3분 이상 = 투입 정지 권고
        public static readonly TimeSpan FeedInterval = TimeSpan.FromSeconds(90);   // 행거 투입 간격
        public const int BumpersPerHanger = 2;

        public static ItemSpec Get(int booth, MeasureItem item)
        {
            if (booth == OvenIndex)
            {
                return item == MeasureItem.Temperature
                    ? new ItemSpec("온도", "℃", 1, 135.0, 145.0, true)
                    : new ItemSpec(ItemName(item), ItemUnit(item), 0, 0, 0, false);
            }
            switch (item)
            {
                case MeasureItem.Temperature: return new ItemSpec("온도", "℃", 1, 21.0, 25.0, true);
                case MeasureItem.Humidity: return new ItemSpec("습도", "%RH", 0, 55, 75, true);
                case MeasureItem.WindSpeed: return new ItemSpec("풍속", "m/s", 2, 0.30, 0.50, true);
                default: return new ItemSpec("차압", "Pa", 0, 5, 15, true);
            }
        }

        /// <summary>지속 시간 표시 HH:mm:ss.</summary>
        public static string DurationText(TimeSpan t)
        {
            return ((int)t.TotalHours).ToString("00") + ":" + t.Minutes.ToString("00") + ":" + t.Seconds.ToString("00");
        }

        public static string ItemName(MeasureItem item)
        {
            return item == MeasureItem.Temperature ? "온도" : item == MeasureItem.Humidity ? "습도" : item == MeasureItem.WindSpeed ? "풍속" : "차압";
        }

        public static string ItemUnit(MeasureItem item)
        {
            return item == MeasureItem.Temperature ? "℃" : item == MeasureItem.Humidity ? "%RH" : item == MeasureItem.WindSpeed ? "m/s" : "Pa";
        }

        /// <summary>
        /// 항목 하나의 판정. 표시 자릿수로 반올림한 값으로 비교한다.
        /// 상·하한을 벗어나면 이탈, 경계에서 폭의 10 % 안쪽(경계 포함)이면 주의. 관리 안 함 = Stop.
        /// </summary>
        public static StatusKind Judge(ItemSpec spec, double value)
        {
            if (!spec.Managed) return StatusKind.Stop;
            double v = Math.Round(value, spec.Decimals);
            if (v < spec.Lower || v > spec.Upper) return StatusKind.Alarm;
            double band = (spec.Upper - spec.Lower) * WarnBandRatio;
            double eps = 1e-9;
            if (v <= spec.Lower + band + eps || v >= spec.Upper - band - eps) return StatusKind.Warn;
            return StatusKind.Run;
        }

        /// <summary>판정 글: 이탈 "상한 초과" / 주의 "상한 근접" / 그 외 "".</summary>
        public static string JudgeText(ItemSpec spec, double value)
        {
            var kind = Judge(spec, value);
            if (kind != StatusKind.Alarm && kind != StatusKind.Warn) return "";
            bool upper = Math.Abs(value - spec.Upper) <= Math.Abs(value - spec.Lower);
            string side = upper ? "상한" : "하한";
            return kind == StatusKind.Alarm ? side + " 초과" : side + " 근접";
        }
    }
}
