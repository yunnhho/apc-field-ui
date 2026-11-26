using System.Collections.Generic;

namespace CarburizeFurnaceMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB, 시리얼)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>로 4기(CF-1~CF-4)의 현재 상태·현재 배치.</summary>
        IList<FurnaceInfo> GetFurnaces();

        /// <summary>레시피 단계별 설정값(예: GR-20).</summary>
        IList<StepInfo> GetSteps(string recipe);

        /// <summary>그 로의 이번 배치 실측 프로파일.</summary>
        ProfileData GetProfile(string furnaceId);

        /// <summary>그 배치의 공정 중 알람 기록(없으면 빈 목록).</summary>
        IList<AlarmRecord> GetAlarms(string batchNo);

        /// <summary>최근 배치 이력(최신순).</summary>
        IList<HistoryRow> GetHistory();

        /// <summary>스캔한 코드가 등록된 기술자 사원증인가.</summary>
        bool VerifyTechnicianBadge(string code);

        /// <summary>배치 종료 판정 결과를 저장한다(이력에 올라가고 로는 대기로).</summary>
        void CompleteBatch(BatchJudgement judgement);
    }
}
