using System;
using System.Collections.Generic;

namespace CellXrayJudgeStation.Data
{
    /// <summary>메인 화면 데이터 자리. 실제 연결(X-ray 검사기, MES, DB)은 이 인터페이스를 구현해 넣는다.</summary>
    internal interface IMainViewSource
    {
        /// <summary>현재 작업 중인 트레이(없으면 null). 화면은 시작할 때 한 번 읽는다.</summary>
        TrayRecord GetCurrentTray();

        /// <summary>트레이 바코드를 스캔했을 때 셀 50개의 자동 판정 결과를 받는다. 형식이 틀리면 null.</summary>
        TrayRecord LoadTray(string trayId);

        /// <summary>작업자 재판정 결과 저장(셀 갱신은 호출 쪽이 이미 했다).</summary>
        void SaveRejudge(string trayId, CellRecord cell);

        /// <summary>트레이 완료: 다음 공정(전해액 주입)으로 전달. 이력 한 줄을 돌려준다.</summary>
        TrayHistoryRow CompleteTray(TrayRecord tray, DateTime at);

        /// <summary>금일 완료 트레이 이력(최근순).</summary>
        IList<TrayHistoryRow> GetTodayHistory();
    }
}
