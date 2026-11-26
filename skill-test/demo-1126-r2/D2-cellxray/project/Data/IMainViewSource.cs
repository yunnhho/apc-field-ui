using System;
using System.Collections.Generic;

namespace CellXrayJudgeStation.Data
{
    /// <summary>메인 화면 데이터 자리. 실제 연결(X-ray 검사기, DB)은 이 인터페이스를 구현해 넣는다.</summary>
    internal interface IMainViewSource
    {
        /// <summary>스캔한 트레이의 셀 50개 측정값·자동 판정. 모르는 트레이면 null.</summary>
        TrayData LoadTray(string trayId);

        /// <summary>이미 완료(다음 공정 이관)한 트레이인가.</summary>
        bool IsCompleted(string trayId);

        /// <summary>오늘 완료한 트레이 이력, 최근 것이 앞.</summary>
        IList<HistoryRow> GetHistory();

        /// <summary>재판정 결과 저장(셀의 State·NgType·Reason 이미 바뀐 상태).</summary>
        void SaveRejudge(string trayId, CellRecord cell);

        /// <summary>트레이 완료: 재판정 대기 셀이 없을 때만 부른다. NG 셀은 빼서 빈칸으로 두고 다음 공정에 넘긴다.</summary>
        HistoryRow CompleteTray(TrayData tray, DateTime at);
    }
}
