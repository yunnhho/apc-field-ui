using System.Collections.Generic;
using System.Globalization;
using APC.UI.Theming;

namespace DiscFinalCheck.Data
{
    internal enum InspectItem { OuterDiameter, Thickness, Runout, Parallelism }

    /// <summary>한 디스크의 측정값 한 건(불변).</summary>
    internal sealed class DiscMeasurement
    {
        public DiscMeasurement(string serial, System.DateTime time, decimal outerDiameter, decimal thickness, decimal runout, decimal parallelism)
        {
            Serial = serial; Time = time; OuterDiameter = outerDiameter; Thickness = thickness; Runout = runout; Parallelism = parallelism;
        }

        public string Serial { get; private set; }
        public System.DateTime Time { get; private set; }
        public decimal OuterDiameter { get; private set; }
        public decimal Thickness { get; private set; }
        public decimal Runout { get; private set; }
        public decimal Parallelism { get; private set; }

        public decimal ValueOf(InspectItem item)
        {
            switch (item)
            {
                case InspectItem.OuterDiameter: return OuterDiameter;
                case InspectItem.Thickness: return Thickness;
                case InspectItem.Runout: return Runout;
                default: return Parallelism;
            }
        }
    }

    internal enum NgAction { Rework, Scrap, Retest }

    /// <summary>NG 처리 등록 한 건(불변). 폐기(Scrap)가 등록된 시리얼은 다시 검사하지 못한다.</summary>
    internal sealed class NgRecord
    {
        public NgRecord(string serial, NgAction action, string supervisorBadge, System.DateTime time)
        {
            Serial = serial; Action = action; SupervisorBadge = supervisorBadge; Time = time;
        }

        public string Serial { get; private set; }
        public NgAction Action { get; private set; }
        public string SupervisorBadge { get; private set; }
        public System.DateTime Time { get; private set; }
    }

    /// <summary>오늘 집계(불변).</summary>
    internal sealed class DayCounts
    {
        public DayCounts(int total, int ng) { Total = total; Ng = ng; }
        public int Total { get; private set; }
        public int Ng { get; private set; }
        public int Ok { get { return Total - Ng; } }
    }

    /// <summary>한 디스크의 판정 결과. 판정 규칙은 DiscSpec 한 곳에만 있다.</summary>
    internal sealed class DiscVerdict
    {
        public DiscVerdict(IList<InspectItem> failed, string reason) { Failed = failed; Reason = reason; }
        public IList<InspectItem> Failed { get; private set; }
        public bool IsOk { get { return Failed.Count == 0; } }
        /// <summary>패널·대화상자용 첫 NG 이유 문장(항목이 더 있으면 "외 n항목").</summary>
        public string Reason { get; private set; }
        public bool Fails(InspectItem item) { return Failed.Contains(item); }
    }

    /// <summary>판정 기준값과 판정 함수. 경계값은 OK(기준 이하·이상 포함).</summary>
    internal static class DiscSpec
    {
        public const decimal OuterNominal = 280.00m, OuterTolerance = 0.10m;
        public const decimal ThicknessNominal = 22.00m, ThicknessTolerance = 0.05m;
        public const decimal RunoutMax = 0.030m, ParallelismMax = 0.010m;
        public const double DefectRateWarnPercent = 2.0;   // 초과하면 경고

        public static string ItemName(InspectItem item)
        {
            switch (item)
            {
                case InspectItem.OuterDiameter: return "외경";
                case InspectItem.Thickness: return "두께";
                case InspectItem.Runout: return "런아웃";
                default: return "평행도";
            }
        }

        public static string Format(InspectItem item, decimal value)
        {
            bool twoDecimals = item == InspectItem.OuterDiameter || item == InspectItem.Thickness;
            return value.ToString(twoDecimals ? "0.00" : "0.000", CultureInfo.InvariantCulture);
        }

        public static string LimitText(InspectItem item)
        {
            switch (item)
            {
                case InspectItem.OuterDiameter: return "기준 " + Format(item, OuterNominal) + " ± " + Format(item, OuterTolerance);
                case InspectItem.Thickness: return "기준 " + Format(item, ThicknessNominal) + " ± " + Format(item, ThicknessTolerance);
                case InspectItem.Runout: return "기준 " + Format(item, RunoutMax) + " 이하";
                default: return "기준 " + Format(item, ParallelismMax) + " 이하";
            }
        }

        private static decimal Lower(InspectItem item)
        {
            return item == InspectItem.OuterDiameter ? OuterNominal - OuterTolerance
                 : item == InspectItem.Thickness ? ThicknessNominal - ThicknessTolerance : 0m;
        }

        private static decimal Upper(InspectItem item)
        {
            switch (item)
            {
                case InspectItem.OuterDiameter: return OuterNominal + OuterTolerance;
                case InspectItem.Thickness: return ThicknessNominal + ThicknessTolerance;
                case InspectItem.Runout: return RunoutMax;
                default: return ParallelismMax;
            }
        }

        /// <summary>항목 한 개 판정 — 카드·표·패널·대화상자가 모두 이 함수를 쓴다. 경계값은 OK.</summary>
        public static bool Passes(InspectItem item, decimal value)
        {
            return value <= Upper(item) && value >= Lower(item);
        }

        /// <summary>NG 이유 한 줄. 예: "외경 상한 초과 280.14 mm (상한 280.10)". OK이면 빈 문자열.</summary>
        public static string ItemReason(InspectItem item, decimal value)
        {
            if (Passes(item, value)) return "";
            bool over = value > Upper(item);
            decimal limit = over ? Upper(item) : Lower(item);
            return ItemName(item) + (over ? " 상한 초과 " : " 하한 미달 ") + Format(item, value) + " mm (" + (over ? "상한 " : "하한 ") + Format(item, limit) + ")";
        }

        public static DiscVerdict Evaluate(DiscMeasurement m)
        {
            var failed = new List<InspectItem>();
            foreach (InspectItem item in new[] { InspectItem.OuterDiameter, InspectItem.Thickness, InspectItem.Runout, InspectItem.Parallelism })
                if (!Passes(item, m.ValueOf(item))) failed.Add(item);
            if (failed.Count == 0) return new DiscVerdict(failed, "");
            string reason = ItemReason(failed[0], m.ValueOf(failed[0]));
            if (failed.Count > 1) reason += " 외 " + (failed.Count - 1) + "항목";
            return new DiscVerdict(failed, reason);
        }

        public static StatusKind KindOf(InspectItem item, decimal value)
        {
            return Passes(item, value) ? StatusKind.Run : StatusKind.Alarm;
        }

        public static string ActionName(NgAction action)
        {
            return action == NgAction.Rework ? "재가공" : action == NgAction.Scrap ? "폐기" : "재검사";
        }
    }
}
