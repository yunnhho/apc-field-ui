using System;

namespace BumperPaintBoothMonitor.Data
{
    internal enum ActionKind { None = 0, Repaint = 1, ExtraInspection = 2, NoImpact = 3 }

    internal static class ActionText
    {
        public static string Of(ActionKind kind)
        {
            switch (kind)
            {
                case ActionKind.Repaint: return "재도장";
                case ActionKind.ExtraInspection: return "추가 검사";
                case ActionKind.NoImpact: return "영향 없음";
                default: return "";
            }
        }
    }

    /// <summary>부스 현재값 한 벌. Values[(int)MeasureItem] = null이면 값 없음(관리 안 함).</summary>
    internal sealed class BoothReading
    {
        public readonly int Booth;
        public readonly bool Running;
        public readonly double?[] Values;

        public BoothReading(int booth, bool running, double?[] values)
        {
            Booth = booth; Running = running; Values = values;
        }
    }

    /// <summary>현재 투입 중인 행거·도료 Lot·색상 코드.</summary>
    internal sealed class FeedInfo
    {
        public readonly string Hanger, PaintLot, ColorCode;

        public FeedInfo(string hanger, string paintLot, string colorCode)
        {
            Hanger = hanger; PaintLot = paintLot; ColorCode = colorCode;
        }
    }

    /// <summary>이탈 구간에 투입된 행거 번호 범위. Count = 0이면 없음.</summary>
    internal sealed class HangerRange
    {
        public readonly int First, Last, Count;

        public HangerRange(int first, int last, int count)
        {
            First = first; Last = last; Count = count;
        }

        public int Bumpers { get { return Count * BoothSpec.BumpersPerHanger; } }
        public static string Name(int number) { return "H-" + number; }
        public string Text
        {
            get { return Count == 0 ? "" : Count == 1 ? Name(First) : Name(First) + "~" + Name(Last); }
        }
    }

    /// <summary>환경 이탈 한 건. End = null이면 아직 이탈 중(Duration은 지금까지).</summary>
    internal sealed class DeviationEvent
    {
        public readonly int Id, Booth;
        public readonly MeasureItem Item;
        public readonly DateTime Start;
        public readonly DateTime? End;
        public readonly double Peak;              // 최대값(하한 이탈이면 최저값)
        public readonly ActionKind Action;
        public readonly string Remark, Confirmer;

        public DeviationEvent(int id, int booth, MeasureItem item, DateTime start, DateTime? end, double peak,
            ActionKind action, string remark, string confirmer)
        {
            Id = id; Booth = booth; Item = item; Start = start; End = end; Peak = peak;
            Action = action; Remark = remark; Confirmer = confirmer;
        }

        public bool Ongoing { get { return !End.HasValue; } }
        public TimeSpan DurationAt(DateTime now) { return (End ?? now) - Start; }

        public DeviationEvent WithAction(ActionKind action, string remark, string confirmer)
        {
            return new DeviationEvent(Id, Booth, Item, Start, End, Peak, action, remark, confirmer);
        }
    }
}
