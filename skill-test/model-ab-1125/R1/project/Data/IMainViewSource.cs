using System.Collections.Generic;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(MES, 테스터·핸들러 로그)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>테스터 T01~T06 현재 상태와 Lot.</summary>
        IList<TesterInfo> GetTesters();

        /// <summary>최근 종료 Lot 결과(최신순).</summary>
        IList<LotResultRow> GetRecentResults();

        /// <summary>기준 미달 Lot 처리 결과 기록(판정 Hold 유지/추가 검사/Release/Scrap + 사유).</summary>
        void RecordDisposition(string lotId, string disposition, string reason);
    }
}
