using System;

namespace ShipPackVerifier.Data
{
    /// <summary>출하 지시.</summary>
    internal sealed class ShipOrder
    {
        public ShipOrder(string orderNo, string customer, string destination, string partNo, int quantity, int boxCapacity)
        {
            OrderNo = orderNo; Customer = customer; Destination = destination;
            PartNo = partNo; Quantity = quantity; BoxCapacity = boxCapacity;
        }

        public readonly string OrderNo, Customer, Destination, PartNo;
        public readonly int Quantity, BoxCapacity;

        /// <summary>전체 박스 수(마지막 박스는 남은 수량).</summary>
        public int TotalBoxes { get { return (Quantity + BoxCapacity - 1) / BoxCapacity; } }
    }

    /// <summary>박스에 담은 시리얼 한 건.</summary>
    internal sealed class PackedSerial
    {
        public PackedSerial(int no, string serial, DateTime scannedAt) { No = no; Serial = serial; ScannedAt = scannedAt; }
        public readonly int No;
        public readonly string Serial;
        public readonly DateTime ScannedAt;
    }

    /// <summary>라벨을 출력한 완료 박스.</summary>
    internal sealed class PackedBox
    {
        public PackedBox(string boxNo, int quantity, DateTime labelPrintedAt) { BoxNo = boxNo; Quantity = quantity; LabelPrintedAt = labelPrintedAt; }
        public readonly string BoxNo;
        public readonly int Quantity;
        public readonly DateTime LabelPrintedAt;
    }

    /// <summary>스캔 판정. Ok면 박스에 담겼다. Reason은 NG 이유("중복 시리얼 · B03에 담김").</summary>
    internal sealed class ScanResult
    {
        public ScanResult(bool ok, string reason) { Ok = ok; Reason = reason; }
        public readonly bool Ok;
        public readonly string Reason;
    }

    /// <summary>현재 박스 상태. 라벨 출력 가능 여부와 막힌 이유를 데이터 쪽이 정한다.</summary>
    internal sealed class BoxProgress
    {
        public BoxProgress(string boxNo, int count, int capacity, bool canPrint, string printBlockedReason, bool orderDone)
        {
            BoxNo = boxNo; Count = count; Capacity = capacity;
            CanPrint = canPrint; PrintBlockedReason = printBlockedReason; OrderDone = orderDone;
        }
        public readonly string BoxNo;
        public readonly int Count, Capacity;
        public readonly bool CanPrint, OrderDone;
        public readonly string PrintBlockedReason;
    }
}
