using System.Collections.Generic;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(MES·테스터 데이터 수집)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>테스터 T01~T06 상태(화면 순서).</summary>
        IList<TesterSummary> GetTesters();

        /// <summary>테스터의 현재 Lot 정보·Bin 분포·최근 Lot 수율.</summary>
        LotDetail GetCurrentLot(string tester);

        /// <summary>Lot 결과 이력(최근 것 먼저).</summary>
        IList<LotResult> GetLotHistory();

        /// <summary>Lot의 OCAP 단계 진행 상태(이탈 알람 ~ 기록).</summary>
        IList<OcapStep> GetOcapSteps(string lotId);

        /// <summary>Lot의 주요 Soft Bin(OCAP dialog 읽기 전용 표시용).</summary>
        IList<SoftBinCount> GetSoftBinTop(string lotId);

        /// <summary>OCAP 판정 기록.</summary>
        void SaveOcapDecision(string lotId, string decision, string reason);
    }
}
