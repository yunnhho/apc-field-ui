using System.Collections.Generic;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 경계. 실제 연결(MES, DB)은 이 인터페이스를 구현해 넣는다.
    /// 화면은 이 메서드들만 호출한다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>테스터 T01~T06의 공정 상태와 최근 Lot 최종 수율.</summary>
        IList<TesterInfo> GetTesters();

        /// <summary>테스터의 현재 Lot(Lot 정보·수율·Bin 분포).</summary>
        LotDetail GetLot(string testerId);

        /// <summary>테스트 구역 Lot 결과 이력, 최신순.</summary>
        IList<LotRecord> GetLotHistory();

        /// <summary>테스터의 최근 Lot 최종 수율 추이(오래된 순, 마지막 = 현재 Lot).</summary>
        IList<double> GetYieldTrend(string testerId);

        /// <summary>OCAP 판정 기록. 저장에 실패하면 false.</summary>
        bool SaveDecision(LotDecision decision);
    }
}
