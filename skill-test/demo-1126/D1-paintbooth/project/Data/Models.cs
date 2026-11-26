using System;
using APC.UI.Theming;

namespace BumperPaintBoothMonitor.Data
{
    internal enum MonitorItem { Temperature, Humidity, WindSpeed, Pressure }

    /// <summary>관리 범위와 판정. 상태 문구·카드·타일·판정판이 모두 이 한 곳의 Judge를 쓴다.</summary>
    internal sealed class ControlRange
    {
        /// <summary>주의 구간 = 관리 폭(상한 - 하한)의 10 %. 경계값 포함(상한 25.0 → 24.6 이상이면 주의).</summary>
        public const double WarnRatio = 0.10;
        /// <summary>이탈이 이 시간 이상 이어지면 라인 투입 정지 권고.</summary>
        public static readonly TimeSpan StopAdviceAfter = TimeSpan.FromMinutes(3);

        public readonly string Name, Unit, Format;
        public readonly double Low, High;

        public ControlRange(string name, string unit, string format, double low, double high)
        {
            Name = name; Unit = unit; Format = format; Low = low; High = high;
        }

        public string Text(double value) { return value.ToString(Format); }
        public string RangeText { get { return Text(Low) + " ~ " + Text(High); } }

        public StatusKind Judge(double value)
        {
            double v = Math.Round(value, Format.Length > 2 ? Format.Length - 2 : 0);
            double band = (High - Low) * WarnRatio;
            if (v > High || v < Low) return StatusKind.Alarm;
            if (v >= High - band - 1e-9 || v <= Low + band + 1e-9) return StatusKind.Warn;
            return StatusKind.Run;
        }

        public static StatusKind Worse(StatusKind a, StatusKind b) { return Rank(b) > Rank(a) ? b : a; }
        private static int Rank(StatusKind k) { return k == StatusKind.Alarm ? 2 : k == StatusKind.Warn ? 1 : 0; }
    }

    /// <summary>한 부스의 한 항목 현재값. Range == null = 관리 안 함(값 없음).</summary>
    internal sealed class ItemReading
    {
        public readonly MonitorItem Item;
        public readonly ControlRange Range;
        public readonly double? Value;
        public ItemReading(MonitorItem item, ControlRange range, double? value) { Item = item; Range = range; Value = value; }

        public StatusKind Status
        {
            get { return Range == null || !Value.HasValue ? StatusKind.Stop : Range.Judge(Value.Value); }
        }
    }

    internal sealed class BoothSnapshot
    {
        public readonly string Name;
        public readonly bool Stopped;
        public readonly ItemReading[] Readings;   // MonitorItem 순서 4개
        public BoothSnapshot(string name, bool stopped, ItemReading[] readings) { Name = name; Stopped = stopped; Readings = readings; }

        /// <summary>부스 상태 = 정지면 Stop, 아니면 항목 중 가장 나쁜 것.</summary>
        public StatusKind Status
        {
            get
            {
                if (Stopped) return StatusKind.Stop;
                var worst = StatusKind.Run;
                foreach (var r in Readings) if (r.Range != null && r.Value.HasValue) worst = ControlRange.Worse(worst, r.Status);
                return worst;
            }
        }

        public static string StateWord(StatusKind k)
        {
            return k == StatusKind.Alarm ? "이탈" : k == StatusKind.Warn ? "주의" : k == StatusKind.Stop ? "정지" : "정상";
        }
    }

    internal sealed class CurrentInput
    {
        public readonly string Hanger, PaintLot, ColorCode;
        public CurrentInput(string hanger, string paintLot, string colorCode) { Hanger = hanger; PaintLot = paintLot; ColorCode = colorCode; }
    }

    /// <summary>환경 이탈 한 건. End == null = 아직 이탈 중. Action 비어 있음 = 조치 미등록.</summary>
    internal sealed class Excursion
    {
        public const string ActionRepaint = "재도장", ActionInspect = "추가 검사", ActionNoImpact = "영향 없음";

        public readonly int Id;
        public readonly string Booth, ItemName, PeakText, Action, Confirmer;
        public readonly DateTime Start;
        public readonly DateTime? End;
        public readonly int FirstHanger, LastHanger;   // 이탈 구간 중 투입된 행거. Last < First = 없음

        public Excursion(int id, string booth, string itemName, DateTime start, DateTime? end, string peakText,
            int firstHanger, int lastHanger, string action, string confirmer)
        {
            Id = id; Booth = booth; ItemName = itemName; Start = start; End = end; PeakText = peakText;
            FirstHanger = firstHanger; LastHanger = lastHanger; Action = action ?? ""; Confirmer = confirmer ?? "";
        }

        public string PeakValue { get { int i = PeakText.IndexOf(' '); return i < 0 ? PeakText : PeakText.Substring(0, i); } }
        public string Unit { get { int i = PeakText.IndexOf(' '); return i < 0 ? "" : PeakText.Substring(i + 1); } }
        public bool Ongoing { get { return !End.HasValue; } }
        public bool HasAction { get { return Action.Length > 0; } }
        public int HangerCount { get { return Math.Max(0, LastHanger - FirstHanger + 1); } }
        public TimeSpan Duration(DateTime now) { return (End ?? now) - Start; }

        public string HangerRange
        {
            get
            {
                if (HangerCount == 0) return "";
                return HangerCount == 1 ? "H-" + FirstHanger : "H-" + FirstHanger + " ~ H-" + LastHanger;
            }
        }

        public Excursion WithAction(string action, string confirmer)
        {
            return new Excursion(Id, Booth, ItemName, Start, End, PeakText, FirstHanger, LastHanger, action, confirmer);
        }

        public static string DurationText(TimeSpan d)
        {
            return (int)d.TotalMinutes + "분 " + d.Seconds.ToString("00") + "초";
        }
    }
}
