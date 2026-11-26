using System.Collections.Generic;

namespace RawMaterialIssueTerminal.Data
{
    /// <summary>메인 화면 데이터 자리. 실제 연결(MES/ERP DB)은 이 인터페이스를 구현해 넣는다.</summary>
    internal interface IMainViewSource
    {
        /// <summary>작업지시 바코드로 지시서를 읽는다. 없으면 null.</summary>
        WorkOrder FindWorkOrder(string code);

        /// <summary>이 지시서에 이미 출고된 원료별 합계(kg, 원료 코드 기준).</summary>
        IDictionary<string, double> GetIssuedKg(string workOrderNo);

        /// <summary>원료의 재고 Lot 전부(유통기한 경과 포함, 순서 무관).</summary>
        IList<StockLot> GetLots(string materialCode);

        /// <summary>Lot 번호로 재고 Lot 한 건. 없으면 null.</summary>
        StockLot FindLot(string lot);

        /// <summary>금일 출고·NG 이력(최신 순).</summary>
        IList<IssueRecord> GetTodayHistory();

        /// <summary>출고 한 건 기록: 이력 추가 + Lot 잔량 차감. 성공이면 true.</summary>
        void RecordIssue(IssueRecord record, double lotRemainAfterKg);

        /// <summary>사번 → 승인자 이름. 승인 권한이 없거나 모르는 사번이면 null.</summary>
        string FindApprover(string employeeNo);
    }
}
