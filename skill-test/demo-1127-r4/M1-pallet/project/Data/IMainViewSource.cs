using System;
using System.Collections.Generic;

namespace PalletPacker.Data
{
    /// <summary>작업 지시(값 객체).</summary>
    internal sealed class WorkOrder
    {
        public readonly string PartNo;
        public readonly string Customer;
        public readonly int TargetBoxes;

        public WorkOrder(string partNo, string customer, int targetBoxes)
        {
            PartNo = partNo; Customer = customer; TargetBoxes = targetBoxes;
        }
    }

    /// <summary>적재된 박스 한 개. Seq는 팔레트 안 순번(1~40).</summary>
    internal sealed class BoxRecord
    {
        public readonly int Seq;
        public readonly string Label;
        public readonly DateTime At;

        public BoxRecord(int seq, string label, DateTime at)
        {
            Seq = seq; Label = label; At = at;
        }

        public int Layer { get { return (Seq - 1) / PalletRules.CellsPerLayer + 1; } }
        public int Cell { get { return (Seq - 1) % PalletRules.CellsPerLayer + 1; } }
    }

    /// <summary>스캔 한 건의 기록(적재·거부 모두).</summary>
    internal sealed class ScanEntry
    {
        public readonly DateTime At;
        public readonly string Label;
        public readonly string Position;   // 적재 위치("3층 5번"), 거부는 ""
        public readonly bool Accepted;
        public readonly string Reason;     // 거부 사유, 적재는 ""

        public ScanEntry(DateTime at, string label, string position, bool accepted, string reason)
        {
            At = at; Label = label; Position = position; Accepted = accepted; Reason = reason;
        }
    }

    /// <summary>
    /// 메인 화면 데이터. 실제 연결(스캐너 후처리, DB, 중량계)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        WorkOrder GetWorkOrder();
        /// <summary>적재된 박스(순번 순).</summary>
        IList<BoxRecord> GetBoxes();
        /// <summary>스캔 기록(오래된 것부터).</summary>
        IList<ScanEntry> GetScanLog();
        /// <summary>박스 라벨 스캔 판정·적재. 거부면 null이 아닌 사유를 reason에 준다.</summary>
        bool ScanBox(string label, out string reason);
        /// <summary>팔레트 마감 저장 후 새 팔레트로.</summary>
        void ClosePallet(string palletLabel, string badgeCode, decimal totalKg);
    }
}
