using System;
using System.Collections.Generic;

namespace BumperPaintBoothMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC 부스 센서, 컨베이어 투입 이력, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>화면 기준 현재 시각(이탈 지속 시간 계산용).</summary>
        DateTime Now { get; }

        /// <summary>부스 5곳(전처리, 프라이머, 베이스, 클리어, 오븐) 현재값.</summary>
        IList<BoothSnapshot> GetBooths();

        /// <summary>부스 한 곳, 항목 하나의 최근 8시간 값(10분 간격 49개, 왼쪽=과거, 마지막 = 현재값). 관리 안 하면 빈 배열.</summary>
        double[] GetTrend(int boothIndex, MonitorItem item);

        CurrentInput GetCurrentInput();

        /// <summary>환경 이탈 이력, 최근 시작이 먼저.</summary>
        IList<Excursion> GetExcursions();

        /// <summary>이탈 조치 등록. 이탈이 끝나지 않았으면 영향 없음은 받지 않는다(false).</summary>
        bool RegisterAction(int excursionId, string action, string confirmer, string memo);
    }
}
