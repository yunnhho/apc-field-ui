using System;
using System.Collections.Generic;

namespace PalletPacker.Data
{
    /// <summary>현재 작업 지시(읽기 전용 값 객체).</summary>
    internal sealed class WorkOrder
    {
        public WorkOrder(string partNo, string customer, int targetBoxes) { PartNo = partNo; Customer = customer; TargetBoxes = targetBoxes; }
        public string PartNo { get; private set; }
        public string Customer { get; private set; }
        public int TargetBoxes { get; private set; }
    }

    /// <summary>팔레트에 쌓은 박스 한 개.</summary>
    internal sealed class PlacedBox
    {
        public PlacedBox(string code, DateTime time) { Code = code; Time = time; }
        public string Code { get; private set; }
        public DateTime Time { get; private set; }
    }

    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB, 시리얼)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        WorkOrder GetWorkOrder();

        /// <summary>지금 팔레트에 쌓은 박스(스캔 순서).</summary>
        IList<PlacedBox> GetPlacedBoxes();

        /// <summary>박스 라벨 한 건 적재. 받으면 null, 거부하면 사유(품번 불일치·중복·형식 오류·팔레트 가득 참).</summary>
        string PlaceBox(string code);

        /// <summary>팔레트 마감 확정 — 쌓은 박스를 비우고 다음 팔레트를 시작한다.</summary>
        void ClosePallet(string palletLabel, string badge, decimal totalKg);
    }
}
