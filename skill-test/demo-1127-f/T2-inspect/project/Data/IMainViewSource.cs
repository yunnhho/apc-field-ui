using System.Collections.Generic;

namespace DiscFinalCheck.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(검사기 PLC, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>오늘 검사 이력, 최신이 앞.</summary>
        IList<DiscMeasurement> GetTodayHistory();

        /// <summary>오늘 집계(검사 수, NG 수).</summary>
        DayCounts GetTodayCounts();

        /// <summary>시리얼의 NG 처리 등록 기록. 없으면 null.</summary>
        NgRecord GetNgRecord(string serial);

        /// <summary>검사기에서 이 디스크를 측정해 온다. 실제로는 PLC가 준다.</summary>
        DiscMeasurement Measure(string serial);

        void SaveInspection(DiscMeasurement measurement);

        void SaveNgRecord(NgRecord record);
    }
}
