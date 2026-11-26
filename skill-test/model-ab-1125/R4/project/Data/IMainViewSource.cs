using System.Collections.Generic;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(MES, 핸들러, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>테스터 T01~T06 (상태, 현재 Lot, Bin 분포, 수율 추이).</summary>
        IList<TesterInfo> GetTesters();

        /// <summary>최근 완료 Lot 결과 (최신이 앞).</summary>
        IList<LotRecord> GetLotHistory();

        /// <summary>미달 Lot 처리 dialog에 넘길 OCAP 진행 상태.</summary>
        OcapCase GetOcapCase(TesterInfo tester);

        /// <summary>dialog에서 확정한 판정을 기록한다(Hold 유지·추가 검사·Release·Scrap + 사유).</summary>
        void SaveDisposition(string lotId, string decision, string reason);
    }
}
