using System;

namespace PalletPacker.Data
{
    /// <summary>현재 작업 지시.</summary>
    internal sealed class WorkOrder
    {
        public readonly string PartNo, Customer;
        public readonly int TargetBoxes;
        public WorkOrder(string partNo, string customer, int targetBoxes) { PartNo = partNo; Customer = customer; TargetBoxes = targetBoxes; }
    }

    /// <summary>팔레트 적재 현황. LayerDoneAt[i]는 (i+1)층을 다 쌓은 시각, 아직이면 null.</summary>
    internal sealed class PalletState
    {
        public readonly int BoxCount;
        public readonly DateTime?[] LayerDoneAt;
        public PalletState(int boxCount, DateTime?[] layerDoneAt) { BoxCount = boxCount; LayerDoneAt = layerDoneAt; }
    }

    /// <summary>
    /// 메인 화면 데이터. 실제 연결(DB, 라인 PLC)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        WorkOrder GetWorkOrder();
        PalletState GetPallet();

        /// <summary>박스 라벨을 적재한다. 받아들이면 null, 거부하면 사유(형식 오류·품번 불일치·중복·목표 도달).</summary>
        string AddBox(string label);

        /// <summary>팔레트를 마감하고 새 팔레트를 시작한다.</summary>
        void ClosePallet(string palletLabel, string employeeBadge, decimal totalKg);
    }
}
