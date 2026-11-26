using System.Collections.Generic;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(MES·테스터 STDF 수집 DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>테스터 T01~T06 현재 상태와 걸린 Lot.</summary>
        IList<TesterState> GetTesters();

        /// <summary>오늘 끝난 Lot 결과(최근 것이 먼저).</summary>
        IList<LotResult> GetLotHistory();

        /// <summary>Lot의 Hard Bin 1~8 수량.</summary>
        IList<BinCount> GetHardBins(string lotId);

        /// <summary>Lot의 Soft Bin 수량 상위 count개(많은 순).</summary>
        IList<BinCount> GetSoftBinTop(string lotId, int count);

        /// <summary>테스터의 최근 Lot 최종 수율(왼쪽=과거, 진행 중 Lot이 있으면 마지막). lotIds는 같은 순서의 Lot ID.</summary>
        double[] GetRecentYields(string testerId, out string[] lotIds);

        /// <summary>기준 미달 Lot의 OCAP 단계 진행.</summary>
        IList<OcapStep> GetOcapSteps(string lotId);

        /// <summary>OCAP 판정 기록.</summary>
        void SaveOcapDecision(string lotId, string decision, string reason);
    }
}
