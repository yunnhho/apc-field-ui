using System;

namespace BumperPaintBoothMonitor.Data
{
    /// <summary>메인 화면 데이터 자리. 실제 연결(PLC, DB)은 이 인터페이스를 구현해 넣는다.</summary>
    internal interface IMainViewSource
    {
        DateTime Now { get; }
        string CurrentUser { get; }
        BoothInfo[] GetBooths();
        LineInput GetLineInput();
        /// <summary>최근 이탈 이력, 최신순.</summary>
        Deviation[] GetDeviations();
        /// <summary>이탈 조치 등록(조치 구분, 비고). 확인자는 CurrentUser로 기록된다.</summary>
        void SaveAction(Deviation deviation, string action, string remark);
    }
}
