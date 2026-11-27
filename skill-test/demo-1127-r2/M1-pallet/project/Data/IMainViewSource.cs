using System;
using System.Collections.Generic;

namespace PalletPacker.Data
{
    /// <summary>현재 작업 지시.</summary>
    internal sealed class WorkOrder
    {
        public readonly string PartNo, Customer;
        public readonly int TargetBoxes;
        public WorkOrder(string partNo, string customer, int targetBoxes)
        {
            PartNo = partNo; Customer = customer; TargetBoxes = targetBoxes;
        }
    }

    /// <summary>팔레트에 적재된 박스 한 개(적재 순서대로).</summary>
    internal sealed class PlacedBox
    {
        public readonly string Label;
        public readonly DateTime Time;
        public PlacedBox(string label, DateTime time) { Label = label; Time = time; }
    }

    /// <summary>
    /// 메인 화면 데이터. 실제 연결(PLC, DB, 시리얼)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        WorkOrder GetOrder();

        /// <summary>현재 팔레트에 적재된 박스(적재 순서).</summary>
        IList<PlacedBox> GetPlacedBoxes();

        /// <summary>박스 라벨 등록. 거부하면 사유(화면에 그대로 보임), 받으면 null.</summary>
        string RegisterBox(string label);

        /// <summary>팔레트 마감 기록. 다음 팔레트는 호출 측이 새로 시작한다.</summary>
        void ClosePallet(string palletLabel, string badge, decimal totalKg);
    }
}
