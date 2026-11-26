using System;
using System.Collections.Generic;

namespace BumperPaintBoothMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB)은 이 인터페이스를 구현해 넣는다.
    /// 값이 바뀌면 화면의 Reload()를 불러 다시 읽게 한다(주기 갱신은 연결하는 쪽 몫).
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>화면이 지속 시간을 셈하는 기준 시각.</summary>
        DateTime Now { get; }

        /// <summary>전처리, 프라이머, 베이스, 클리어, 오븐 순 5곳.</summary>
        Booth[] GetBooths();

        /// <summary>환경 이탈 이력, 시작이 최근인 것부터.</summary>
        List<Deviation> GetDeviations();

        /// <summary>이탈 조치 등록. action = 재도장 / 추가 검사 / 영향 없음.</summary>
        void RegisterAction(Deviation deviation, string action, string remark, string confirmer);
    }
}
