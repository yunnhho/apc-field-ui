using System.Collections.Generic;

namespace CleanroomMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(센서 게이트웨이, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        IList<ZoneReading> GetReadings();
        IList<ZoneLimits> GetLimits();
        int GetSaveIntervalSec();
        void SaveSettings(IList<ZoneLimits> limits, int saveIntervalSec);
    }
}
