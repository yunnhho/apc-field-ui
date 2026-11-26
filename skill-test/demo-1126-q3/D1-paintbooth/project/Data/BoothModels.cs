using System;
using System.Globalization;
using APC.UI.Theming;

namespace BumperPaintBoothMonitor.Data
{
    internal enum BoothItem { Temp, Humi, Wind, Dp }

    /// <summary>한 부스의 한 관리 항목 기준(하한~상한).</summary>
    internal sealed class ItemSpec
    {
        public readonly decimal Low;
        public readonly decimal High;
        public readonly decimal Nominal;
        public readonly int Decimals;

        public ItemSpec(decimal low, decimal high, decimal nominal, int decimals)
        {
            Low = low; High = high; Nominal = nominal; Decimals = decimals;
        }

        public string Format(decimal v) { return v.ToString("F" + Decimals, CultureInfo.InvariantCulture); }
        public string Range { get { return Format(Low) + " ~ " + Format(High); } }
    }

    /// <summary>관리 범위와 판정. 모든 화면 요소(타일·카드·판정판·이력)가 Judge 하나를 쓴다.</summary>
    internal static class BoothSpec
    {
        /// <summary>경계 근접(주의) 폭 = 관리 범위 폭의 10 %. 경계값과 같으면 주의.</summary>
        public const decimal WarnMarginRate = 0.10m;
        /// <summary>이탈이 이 시간(분) 이상 이어지면 라인 투입 정지 권고.</summary>
        public const int StopRecommendMinutes = 3;

        private static readonly ItemSpec CoatTemp = new ItemSpec(21.0m, 25.0m, 23.0m, 1);
        private static readonly ItemSpec CoatHumi = new ItemSpec(55m, 75m, 65m, 0);
        private static readonly ItemSpec CoatWind = new ItemSpec(0.30m, 0.50m, 0.40m, 2);
        private static readonly ItemSpec CoatDp = new ItemSpec(5m, 15m, 10m, 0);
        private static readonly ItemSpec OvenTemp = new ItemSpec(135.0m, 145.0m, 140.0m, 1);

        /// <summary>관리하지 않는 항목(오븐의 습도·풍속·차압)은 null.</summary>
        public static ItemSpec Get(bool oven, BoothItem item)
        {
            if (oven) return item == BoothItem.Temp ? OvenTemp : null;
            switch (item)
            {
                case BoothItem.Temp: return CoatTemp;
                case BoothItem.Humi: return CoatHumi;
                case BoothItem.Wind: return CoatWind;
                default: return CoatDp;
            }
        }

        public static string Name(BoothItem item)
        {
            switch (item)
            {
                case BoothItem.Temp: return "온도";
                case BoothItem.Humi: return "습도";
                case BoothItem.Wind: return "풍속";
                default: return "차압";
            }
        }

        public static string Unit(BoothItem item)
        {
            switch (item)
            {
                case BoothItem.Temp: return "℃";
                case BoothItem.Humi: return "%RH";
                case BoothItem.Wind: return "m/s";
                default: return "Pa";
            }
        }

        /// <summary>벗어남(경계 밖) = Alarm, 경계 안쪽 10 % 폭(경계 포함) = Warn, 그 외 Run.</summary>
        public static StatusKind Judge(ItemSpec spec, decimal v)
        {
            if (v < spec.Low || v > spec.High) return StatusKind.Alarm;
            decimal margin = (spec.High - spec.Low) * WarnMarginRate;
            if (v <= spec.Low + margin || v >= spec.High - margin) return StatusKind.Warn;
            return StatusKind.Run;
        }

        public static string HangerLabel(int no) { return "H-" + no.ToString("0000"); }

        public static string Duration(TimeSpan t)
        {
            return string.Format("{0:00}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds);
        }
    }

    internal sealed class Booth
    {
        public readonly string Name;
        public readonly bool IsOven;
        public readonly bool Stopped;
        public readonly decimal?[] Values;      // BoothItem 순서: 온도, 습도, 풍속, 차압. 값 없음 = null
        public readonly string Hanger, Lot, Color;   // 투입 중이 아니면 ""
        public readonly int HangerNow;          // RefTime 시점의 행거 번호
        public readonly DateTime RefTime;
        public readonly decimal[] TempTrend, HumiTrend;   // 최근 8시간(30분 간격 17점), 마지막 = 현재값

        public Booth(string name, bool isOven, bool stopped, decimal?[] values, string hanger, string lot, string color,
            int hangerNow, DateTime refTime, decimal[] tempTrend, decimal[] humiTrend)
        {
            Name = name; IsOven = isOven; Stopped = stopped; Values = values;
            Hanger = hanger; Lot = lot; Color = color; HangerNow = hangerNow; RefTime = refTime;
            TempTrend = tempTrend; HumiTrend = humiTrend;
        }

        public ItemSpec Spec(BoothItem item) { return BoothSpec.Get(IsOven, item); }
        public decimal? Value(BoothItem item) { return Values[(int)item]; }

        /// <summary>그 항목 값의 판정. 멈춘 부스·값 없음·관리 안 함은 판정하지 않고 Stop.</summary>
        public StatusKind ItemStatus(BoothItem item)
        {
            ItemSpec spec = Spec(item);
            decimal? v = Value(item);
            if (Stopped || spec == null || !v.HasValue) return StatusKind.Stop;
            return BoothSpec.Judge(spec, v.Value);
        }

        /// <summary>타일 상태 = 정지, 아니면 항목 판정 중 가장 나쁜 것(Run &lt; Warn &lt; Alarm).</summary>
        public StatusKind Status
        {
            get
            {
                if (Stopped) return StatusKind.Stop;
                StatusKind worst = StatusKind.Run;
                foreach (BoothItem item in Enum.GetValues(typeof(BoothItem)))
                {
                    StatusKind s = ItemStatus(item);
                    if (s == StatusKind.Alarm) return s;
                    if (s == StatusKind.Warn) worst = s;
                }
                return worst;
            }
        }

        public int HangerAt(DateTime t) { return HangerNow - (int)((RefTime - t).TotalSeconds / ConveyorSeconds); }

        /// <summary>컨베이어 투입 간격 1.5분.</summary>
        public const int ConveyorSeconds = 90;
    }

    internal sealed class Deviation
    {
        public readonly Booth Booth;
        public readonly BoothItem Item;
        public readonly DateTime Start;
        public readonly DateTime? End;          // null = 아직 이탈 중
        public readonly decimal Peak;           // 관리 범위에서 가장 멀리 간 값(상한 쪽이면 최대, 하한 쪽이면 최소)
        public readonly string Action;          // null = 조치 미등록
        public readonly string Remark, Confirmer;

        public Deviation(Booth booth, BoothItem item, DateTime start, DateTime? end, decimal peak,
            string action, string remark, string confirmer)
        {
            Booth = booth; Item = item; Start = start; End = end; Peak = peak;
            Action = action; Remark = remark; Confirmer = confirmer;
        }

        public bool Open { get { return !End.HasValue; } }
        public bool Registered { get { return Action != null; } }
        public TimeSpan Duration(DateTime now) { return (End ?? now) - Start; }
        public bool AboveHigh { get { return Peak > Booth.Spec(Item).High; } }

        public Deviation WithAction(string action, string remark, string confirmer)
        {
            return new Deviation(Booth, Item, Start, End, Peak, action, remark, confirmer);
        }

        public string HangerRange(DateTime now)
        {
            int first = Booth.HangerAt(Start), last = Booth.HangerAt(End ?? now);
            return BoothSpec.HangerLabel(first) + " ~ " + BoothSpec.HangerLabel(last) + " (" + (last - first + 1) + "개)";
        }

        /// <summary>이탈로 예상되는 품질 문제.</summary>
        public string Risk
        {
            get
            {
                if (Item == BoothItem.Humi && AboveHigh) return "클리어 백화(blushing)";
                if (Item == BoothItem.Wind && !AboveHigh) return "먼지 부착 불량";
                if (Item == BoothItem.Temp) return "경화·광택 편차";
                if (Item == BoothItem.Dp) return "외기 유입 오염";
                return "도막 외관 불량";
            }
        }
    }
}
