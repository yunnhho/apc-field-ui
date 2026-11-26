using System;
using System.Collections.Generic;
using System.Linq;

namespace RawMaterialIssueTerminal.Data
{
    internal enum ScanVerdict { Ok, FifoViolation, Expired, AllergenMismatch, NotInOrder, UnknownLot }

    internal enum IssueState { Waiting, InProgress, Met, Over }

    /// <summary>포대 스캔 판정 결과. Reason = 판정판에 크게 쓰는 사유 문구, Detail = 한 줄 설명.</summary>
    internal sealed class ScanResult
    {
        public readonly ScanVerdict Verdict;
        public readonly BagLabel Bag;
        public readonly MaterialLine Material;
        public readonly LotStock Lot, FrontLot;
        public readonly bool NearExpiry;

        public ScanResult(ScanVerdict verdict, BagLabel bag, MaterialLine material, LotStock lot, LotStock frontLot, bool nearExpiry)
        {
            Verdict = verdict; Bag = bag; Material = material; Lot = lot; FrontLot = frontLot; NearExpiry = nearExpiry;
        }

        public bool IsOk { get { return Verdict == ScanVerdict.Ok; } }
        /// <summary>예외 승인으로 출고할 수 있는 NG는 FIFO 위반뿐이다(유통기한 경과·알레르기 불일치는 승인으로도 불가).</summary>
        public bool CanApprove { get { return Verdict == ScanVerdict.FifoViolation; } }
    }

    /// <summary>출고 규칙. 판정은 전부 여기 한 곳(화면·표·dialog가 같은 함수를 쓴다).</summary>
    internal static class IssueRules
    {
        // 정한 기준값
        public const double TolerancePercent = 0.5;   // 필요량 허용 오차 ±0.5%
        public const int NearExpiryDays = 7;          // 유통기한 7일 이내 = 주의(출고 가능)

        public static string ReasonText(ScanVerdict v)
        {
            switch (v)
            {
                case ScanVerdict.FifoViolation: return "FIFO 위반";
                case ScanVerdict.Expired: return "유통기한 경과";
                case ScanVerdict.AllergenMismatch: return "알레르기 라인 불일치";
                case ScanVerdict.NotInOrder: return "지시서에 없는 원료";
                case ScanVerdict.UnknownLot: return "미등록 Lot";
                default: return "OK";
            }
        }

        public static IssueState StateOf(MaterialLine m)
        {
            double tol = m.Required * TolerancePercent / 100.0;
            const double eps = 1e-9;
            if (m.Issued <= 0) return IssueState.Waiting;
            if (m.Issued > m.Required + tol + eps) return IssueState.Over;
            if (m.Issued >= m.Required - tol - eps) return IssueState.Met;
            return IssueState.InProgress;
        }

        public static bool IsSatisfied(MaterialLine m)
        {
            IssueState s = StateOf(m);
            return s == IssueState.Met || s == IssueState.Over;
        }

        public static bool IsExpired(LotStock lot, DateTime today) { return lot.ExpiresOn.Date < today.Date; }

        public static int DaysLeft(LotStock lot, DateTime today) { return (int)(lot.ExpiresOn.Date - today.Date).TotalDays; }

        /// <summary>FIFO 대상 Lot: 경과하지 않았고 잔량이 있고, 알레르기 원료면 지시 라인과 같은 라인에 보관된 것.</summary>
        public static bool IsEligible(LotStock lot, MaterialLine m, OrderInfo order, DateTime today)
        {
            return !IsExpired(lot, today) && lot.Remaining > 0 && (m.Allergen.Length == 0 || lot.AllergenLine == order.AllergenLine);
        }

        /// <summary>FIFO 순서 대상 Lot를 유통기한 → 입고일 → Lot 순으로 줄 세운다. 순위는 인덱스 + 1.</summary>
        public static List<LotStock> FifoQueue(IEnumerable<LotStock> lots, MaterialLine m, OrderInfo order, DateTime today)
        {
            return lots.Where(l => IsEligible(l, m, order, today))
                .OrderBy(l => l.ExpiresOn).ThenBy(l => l.ReceivedOn).ThenBy(l => l.Lot, StringComparer.Ordinal).ToList();
        }

        public static ScanResult Judge(OrderInfo order, IList<MaterialLine> materials, IList<LotStock> lotsOfMaterial, BagLabel bag, DateTime today)
        {
            MaterialLine m = materials.FirstOrDefault(x => x.Code == bag.MaterialCode);
            if (m == null) return new ScanResult(ScanVerdict.NotInOrder, bag, null, null, null, false);
            LotStock lot = lotsOfMaterial.FirstOrDefault(l => l.Lot == bag.Lot);
            if (lot == null) return new ScanResult(ScanVerdict.UnknownLot, bag, m, null, null, false);
            if (IsExpired(lot, today)) return new ScanResult(ScanVerdict.Expired, bag, m, lot, null, false);
            if (m.Allergen.Length > 0 && lot.AllergenLine != order.AllergenLine)
                return new ScanResult(ScanVerdict.AllergenMismatch, bag, m, lot, null, false);

            List<LotStock> queue = FifoQueue(lotsOfMaterial, m, order, today);
            if (queue.Count > 0 && queue[0].Lot != lot.Lot)
                return new ScanResult(ScanVerdict.FifoViolation, bag, m, lot, queue[0], false);
            return new ScanResult(ScanVerdict.Ok, bag, m, lot, null, DaysLeft(lot, today) <= NearExpiryDays);
        }
    }
}
