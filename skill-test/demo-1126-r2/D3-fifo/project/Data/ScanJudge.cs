using System;
using System.Collections.Generic;
using System.Linq;

namespace RawMaterialIssueTerminal.Data
{
    /// <summary>원료 포대(Lot) 스캔 판정. 상태 문구·표·판정판이 모두 이 한 곳의 규칙을 쓴다.</summary>
    internal static class ScanJudge
    {
        public const double Tolerance = 0.005;   // 필요량 허용 오차 ±0.5 %
        public const int WarnDays = 7;           // 유통기한 7일 이내 = 주의(출고 가능)
        public const string DateFormat = "yyyy-MM-dd";

        public sealed class Verdict
        {
            public bool Ok;
            public string Reason = "";          // NG 사유(판정판 큰 글자)
            public string Detail = "";          // 판정판 아래 한 줄
            public bool IsFifoViolation;        // 예외 승인으로 풀 수 있는 NG
            public StockLot Lot, AheadLot;      // 스캔한 Lot, 앞 순위 Lot(FIFO 위반일 때)
            public RecipeLine Line;
            public double Kg;                   // 출고량(OK일 때)
        }

        public static bool IsExpired(StockLot lot, DateTime today) { return lot.ExpiresOn.Date < today.Date; }

        public static bool IsNearExpiry(StockLot lot, DateTime today)
        {
            return !IsExpired(lot, today) && (lot.ExpiresOn.Date - today.Date).TotalDays <= WarnDays;
        }

        /// <summary>같은 원료에서 유통기한이 빠른 순(같으면 입고일 순). 기한 지난 Lot과 잔량 없는 Lot은 순위에서 뺀다.</summary>
        public static IList<StockLot> FifoOrder(IEnumerable<StockLot> lots, DateTime today)
        {
            return lots.Where(l => l.RemainKg > 0 && !IsExpired(l, today))
                .OrderBy(l => l.ExpiresOn).ThenBy(l => l.ReceivedOn).ToList();
        }

        public static Verdict Judge(WorkOrder order, StockLot lot, IList<StockLot> sameMaterialLots, DateTime today)
        {
            var v = new Verdict { Lot = lot };
            if (lot == null) return Ng(v, "미등록 Lot", "재고에 없는 라벨입니다");
            if (IsExpired(lot, today))
                return Ng(v, "유통기한 경과", lot.Lot + " · 유통기한 " + lot.ExpiresOn.ToString(DateFormat) + " 경과, 출고 불가");
            if (lot.AllergenLine.Length > 0 && lot.AllergenLine != order.AllergenLine)
                return Ng(v, "알레르기 라인 불일치", lot.MaterialName + " " + lot.AllergenLine + " 라인 ≠ 지시 " + order.AllergenLine + " 라인");
            v.Line = order.Lines.FirstOrDefault(l => l.Code == lot.MaterialCode);
            if (v.Line == null) return Ng(v, "지시서에 없는 원료", lot.MaterialName + " · " + lot.Lot);
            if (lot.RemainKg <= 0) return Ng(v, "Lot 잔량 없음", lot.Lot);

            var order1 = FifoOrder(sameMaterialLots, today);
            var ahead = order1.FirstOrDefault(l => l.Lot != lot.Lot && Earlier(l, lot));
            if (ahead != null)
            {
                v.AheadLot = ahead;
                v.IsFifoViolation = true;
                return Ng(v, "FIFO 위반", lot.Lot + " · 앞 순위 " + ahead.Lot + " (유통기한 " + ahead.ExpiresOn.ToString(DateFormat) + ") 먼저");
            }
            v.Ok = true;
            v.Kg = Math.Min(v.Line.BagKg, lot.RemainKg);
            return v;
        }

        private static bool Earlier(StockLot a, StockLot b)
        {
            return a.ExpiresOn < b.ExpiresOn || (a.ExpiresOn == b.ExpiresOn && a.ReceivedOn < b.ReceivedOn);
        }

        private static Verdict Ng(Verdict v, string reason, string detail)
        {
            v.Ok = false; v.Reason = reason; v.Detail = detail;
            return v;
        }

        // 출고량 대비 필요량 상태: 대기 / 출고 중 / 충족 / 초과
        public static string Progress(double issued, double required)
        {
            if (issued > required * (1 + Tolerance)) return "초과";
            if (issued >= required * (1 - Tolerance)) return "충족";
            return issued > 0 ? "출고 중" : "대기";
        }

        public static bool IsSatisfied(double issued, double required) { return issued >= required * (1 - Tolerance); }
    }
}
