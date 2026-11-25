using System.Collections.Generic;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(핸들러·테스터 로그·MES DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>테스터 T01~T06 현재 상태·Lot·Bin 결과·수율 추이.</summary>
        IList<TesterInfo> GetTesters();

        /// <summary>최근 완료 Lot 결과(최신 → 과거).</summary>
        IList<HistoryRow> GetHistory();

        /// <summary>수율 미달 Lot 처리 판정 기록(Hold 유지·추가 검사·Release·Scrap)과 사유.</summary>
        void SaveOcapDecision(string lotId, string decision, string reason);
    }
}
