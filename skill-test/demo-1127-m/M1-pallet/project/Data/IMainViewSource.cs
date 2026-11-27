using System;
using System.Collections.Generic;

namespace PalletPacker.Data
{
    /// <summary>현재 작업 지시.</summary>
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

    /// <summary>층별 요약 한 줄. CompletedAt이 null이면 아직 다 쌓지 않았다.</summary>
    internal sealed class LayerInfo
    {
        public readonly int Number;
        public readonly int BoxCount;
        public readonly DateTime? CompletedAt;

        public LayerInfo(int number, int boxCount, DateTime? completedAt)
        {
            Number = number; BoxCount = boxCount; CompletedAt = completedAt;
        }
    }

    /// <summary>박스 스캔 결과. 거부면 Reason에 화면에 보일 이유.</summary>
    internal sealed class BoxScanResult
    {
        public readonly bool Accepted;
        public readonly string Reason;

        public BoxScanResult(bool accepted, string reason)
        {
            Accepted = accepted; Reason = reason;
        }
    }

    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB, 스케일)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        WorkOrder GetWorkOrder();
        /// <summary>1~5층 요약(항상 5건).</summary>
        IList<LayerInfo> GetLayers();
        int GetBoxCount();
        /// <summary>박스 라벨을 판정하고 받아들이면 적재 목록에 넣는다.</summary>
        BoxScanResult ScanBox(string label);
        /// <summary>팔레트 마감 기록 후 빈 팔레트로 시작한다.</summary>
        void ClosePallet(string palletLabel, string employeeId, decimal totalKg);
    }
}
