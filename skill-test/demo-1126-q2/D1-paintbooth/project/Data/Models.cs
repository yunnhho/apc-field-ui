using System;
using System.Globalization;
using APC.UI.Theming;

namespace BumperPaintBoothMonitor.Data
{
    /// <summary>관리 항목 하나의 관리 범위. 관리 안 하는 항목은 Managed = false.</summary>
    internal sealed class Spec
    {
        public readonly string Item, Unit;
        public readonly int Decimals;
        public readonly decimal Lower, Upper;
        public readonly bool Managed;

        public Spec(string item, string unit, int decimals, decimal lower, decimal upper, bool managed = true)
        {
            Item = item; Unit = unit; Decimals = decimals; Lower = lower; Upper = upper; Managed = managed;
        }

        public string Format(decimal value) { return value.ToString("F" + Decimals, CultureInfo.InvariantCulture); }
        public string RangeText { get { return Managed ? Format(Lower) + "~" + Format(Upper) : "관리 안 함"; } }
    }

    /// <summary>부스 현재값. 값이 없는 항목(관리 안 함)은 null.</summary>
    internal sealed class Booth
    {
        public readonly string Name;
        public readonly bool Running;
        public readonly decimal Temp, Humi, Wind, Press;   // 관리 안 하는 항목은 0 (화면이 Spec.Managed로 거른다)

        public Booth(string name, bool running, decimal temp, decimal humi, decimal wind, decimal press)
        {
            Name = name; Running = running; Temp = temp; Humi = humi; Wind = wind; Press = press;
        }

        public decimal ValueOf(int item)
        {
            return item == 0 ? Temp : item == 1 ? Humi : item == 2 ? Wind : Press;
        }
    }

    /// <summary>환경 이탈 한 건. End가 null이면 아직 이탈 중.</summary>
    internal sealed class Excursion
    {
        public readonly int Id;
        public readonly string Booth, Item, MaxText, Action, Note, Confirmer;
        public readonly DateTime Start;
        public readonly DateTime? End;
        public readonly int FirstHanger, LastHanger;

        public Excursion(int id, string booth, string item, DateTime start, DateTime? end, string maxText,
            int firstHanger, int lastHanger, string action, string note, string confirmer)
        {
            Id = id; Booth = booth; Item = item; Start = start; End = end; MaxText = maxText;
            FirstHanger = firstHanger; LastHanger = lastHanger; Action = action ?? ""; Note = note ?? ""; Confirmer = confirmer ?? "";
        }

        public bool Ongoing { get { return !End.HasValue; } }
        public bool HasAction { get { return Action.Length > 0; } }
        public TimeSpan Duration(DateTime now) { return (End ?? now) - Start; }
        public int HangerCount { get { return LastHanger >= FirstHanger ? LastHanger - FirstHanger + 1 : 0; } }

        public string HangerText
        {
            get
            {
                if (HangerCount == 0) return "";
                return HangerCount == 1 ? HangerNo(FirstHanger) : HangerNo(FirstHanger) + " ~ " + HangerNo(LastHanger);
            }
        }

        public static string HangerNo(int n) { return "H-" + n.ToString("0000", CultureInfo.InvariantCulture); }

        public Excursion WithAction(string action, string note, string confirmer)
        {
            return new Excursion(Id, Booth, Item, Start, End, MaxText, FirstHanger, LastHanger, action, note, confirmer);
        }
    }

    /// <summary>지금 라인에 투입 중인 행거 정보.</summary>
    internal sealed class InputInfo
    {
        public readonly int Hanger;
        public readonly string PaintLot, ColorCode;
        public InputInfo(int hanger, string paintLot, string colorCode) { Hanger = hanger; PaintLot = paintLot; ColorCode = colorCode; }
    }

    /// <summary>관리 범위 판정. 상태 문구·타일·카드·이력이 모두 이 함수 하나를 쓴다.</summary>
    internal static class BoothJudge
    {
        public const decimal WarnBandRatio = 0.10m;      // 관리 범위 폭의 10% 안쪽부터 주의
        public const int StopAdviceSeconds = 180;        // 이탈 3분 이상 → 라인 투입 정지 권고
        public const int BumpersPerHanger = 2;

        public static readonly string[] Items = { "온도", "습도", "풍속", "차압" };

        // 전처리·프라이머는 관리 범위가 따로 주어지지 않아 베이스·클리어와 같게 둔다
        private static readonly Spec[] Booths =
        {
            new Spec("온도", "℃", 1, 21.0m, 25.0m), new Spec("습도", "%RH", 0, 55m, 75m),
            new Spec("풍속", "m/s", 2, 0.30m, 0.50m), new Spec("차압", "Pa", 0, 5m, 15m),
        };
        private static readonly Spec[] Oven =
        {
            new Spec("온도", "℃", 1, 135.0m, 145.0m), new Spec("습도", "%RH", 0, 0m, 0m, false),
            new Spec("풍속", "m/s", 2, 0m, 0m, false), new Spec("차압", "Pa", 0, 0m, 0m, false),
        };

        public static Spec[] SpecsOf(string booth) { return booth == "오븐" ? Oven : Booths; }

        /// <summary>범위를 벗어나면 이탈(Alarm), 경계 10% 안쪽(경계값 포함)이면 주의(Warn).</summary>
        public static StatusKind Judge(Spec spec, decimal value)
        {
            if (!spec.Managed) return StatusKind.Stop;
            if (value > spec.Upper || value < spec.Lower) return StatusKind.Alarm;
            decimal band = (spec.Upper - spec.Lower) * WarnBandRatio;
            if (value >= spec.Upper - band || value <= spec.Lower + band) return StatusKind.Warn;
            return StatusKind.Run;
        }

        public static StatusKind BoothStatus(Booth b)
        {
            if (!b.Running) return StatusKind.Stop;
            var specs = SpecsOf(b.Name);
            var worst = StatusKind.Run;
            for (int i = 0; i < specs.Length; i++)
            {
                if (!specs[i].Managed) continue;
                var s = Judge(specs[i], b.ValueOf(i));
                if (Rank(s) > Rank(worst)) worst = s;
            }
            return worst;
        }

        public static string StatusWord(StatusKind s)
        {
            return s == StatusKind.Alarm ? "이탈" : s == StatusKind.Warn ? "주의" : s == StatusKind.Stop ? "정지" : "정상";
        }

        private static int Rank(StatusKind s) { return s == StatusKind.Alarm ? 2 : s == StatusKind.Warn ? 1 : 0; }

        public static string DurationText(TimeSpan d)
        {
            return ((int)d.TotalHours).ToString("00") + ":" + d.Minutes.ToString("00") + ":" + d.Seconds.ToString("00");
        }
    }
}
