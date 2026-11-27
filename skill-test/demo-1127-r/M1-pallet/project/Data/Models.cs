using System;

namespace PalletPacker.Data
{
    /// <summary>작업 지시(읽기 전용 값 객체).</summary>
    internal sealed class WorkOrder
    {
        public WorkOrder(string partNo, string customer, int targetBoxes)
        {
            PartNo = partNo;
            Customer = customer;
            TargetBoxes = targetBoxes;
        }

        public string PartNo { get; private set; }
        public string Customer { get; private set; }
        public int TargetBoxes { get; private set; }
    }

    /// <summary>팔레트 적재 현황. 층·칸 번호는 BoxCount에서 계산한다.</summary>
    internal sealed class PalletState
    {
        public const int Layers = 5;
        public const int CellsPerLayer = 8;

        public PalletState(int boxCount, int targetBoxes, DateTime?[] layerDoneAt)
        {
            BoxCount = boxCount;
            TargetBoxes = targetBoxes;
            LayerDoneAt = layerDoneAt;
        }

        public int BoxCount { get; private set; }
        public int TargetBoxes { get; private set; }
        /// <summary>층별 완료 시각(미완료 null), 길이 = Layers.</summary>
        public DateTime?[] LayerDoneAt { get; private set; }

        public bool IsFull { get { return BoxCount >= Layers * CellsPerLayer; } }
        /// <summary>현재 층 1~5(가득 차면 5).</summary>
        public int CurrentLayer { get { return IsFull ? Layers : BoxCount / CellsPerLayer + 1; } }
        /// <summary>현재 층에 이미 놓인 박스 수(0~8).</summary>
        public int BoxesInCurrentLayer { get { return BoxCount - (CurrentLayer - 1) * CellsPerLayer; } }

        public int BoxesInLayer(int layer)
        {
            int n = BoxCount - (layer - 1) * CellsPerLayer;
            return n < 0 ? 0 : n > CellsPerLayer ? CellsPerLayer : n;
        }
    }

    /// <summary>박스 스캔 결과. 거부면 Message에 이유.</summary>
    internal sealed class ScanResult
    {
        public ScanResult(bool accepted, string message)
        {
            Accepted = accepted;
            Message = message;
        }

        public bool Accepted { get; private set; }
        public string Message { get; private set; }
    }
}
