using System.Collections.Generic;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(테스터·핸들러·MES DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        IList<TesterInfo> GetTesters();
        /// <summary>그 테스터 Lot의 Hard Bin 1~8 분포(Lot가 없으면 빈 목록).</summary>
        IList<BinRow> GetHardBins(TesterInfo tester);
        /// <summary>그 Lot의 Soft Bin 상위 count(수량 큰 순).</summary>
        IList<BinRow> GetSoftTop(TesterInfo tester, int count);
        /// <summary>끝난 Lot 결과, 최근 것이 앞.</summary>
        IList<LotResult> GetHistory();
    }
}
