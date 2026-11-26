using System;

namespace FgRackLocator.Data
{
    /// <summary>랙 크기와 위치 코드 규칙(랙-단-열, 예 B-3-07).</summary>
    internal static class RackLayout
    {
        public const string RackNames = "ABCD";
        public const int Tiers = 5;
        public const int Columns = 12;
        /// <summary>팔레트 중량이 이 값(kg)을 넘으면 무거운 품번 — 1~2단에만 입고.</summary>
        public const int HeavyKg = 600;
        public const int HeavyMaxTier = 2;
        /// <summary>입고 후 이 일수를 넘기면 장기재고.</summary>
        public const int LongTermDays = 30;

        public static string Code(char rack, int tier, int col)
        {
            return rack + "-" + tier + "-" + col.ToString("00");
        }

        public static string LabelOf(string location) { return "LOC-" + location; }
    }

    internal sealed class Pallet
    {
        public readonly string Code;
        public readonly string PartNo;
        public readonly string PartAbbr;
        public readonly int WeightKg;
        public readonly DateTime InboundDate;
        /// <summary>랙 위치. 아직 입고 전이면 null.</summary>
        public readonly string Location;
        public readonly bool IsHold;

        public Pallet(string code, string partNo, string partAbbr, int weightKg, DateTime inboundDate, string location, bool isHold)
        {
            Code = code; PartNo = partNo; PartAbbr = partAbbr; WeightKg = weightKg;
            InboundDate = inboundDate; Location = location; IsHold = isHold;
        }

        public bool IsHeavy { get { return WeightKg > RackLayout.HeavyKg; } }
    }

    internal enum ScanKind { Inbound, InboundNoSpace, Outbound, OutboundHold }

    internal sealed class ScanResult
    {
        public readonly ScanKind Kind;
        public readonly Pallet Pallet;
        /// <summary>입고면 추천 위치, 출고면 팔레트가 있는 위치. 입고 불가·출고 불가면 null 또는 현재 위치.</summary>
        public readonly string Location;
        /// <summary>출고인데 같은 품번의 더 오래된 팔레트가 있으면 그 팔레트(없으면 null).</summary>
        public readonly Pallet Older;

        public ScanResult(ScanKind kind, Pallet pallet, string location, Pallet older)
        {
            Kind = kind; Pallet = pallet; Location = location; Older = older;
        }

        public bool CanConfirm { get { return Kind == ScanKind.Inbound || Kind == ScanKind.Outbound; } }
    }

    internal sealed class HistoryEntry
    {
        public readonly DateTime Time;
        public readonly string Direction;   // 입고 / 출고
        public readonly string PalletCode;
        public readonly string PartNo;
        public readonly string Location;
        public readonly string Worker;

        public HistoryEntry(DateTime time, string direction, string palletCode, string partNo, string location, string worker)
        {
            Time = time; Direction = direction; PalletCode = palletCode; PartNo = partNo; Location = location; Worker = worker;
        }
    }
}
