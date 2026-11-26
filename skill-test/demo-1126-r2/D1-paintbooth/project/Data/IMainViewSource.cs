using System;
using System.Collections.Generic;

namespace BumperPaintBoothMonitor.Data
{
    /// <summary>메인 화면 데이터 자리. 실제 연결(PLC, DB)은 이 인터페이스를 구현해 넣는다.</summary>
    internal interface IMainViewSource
    {
        DateTime Now { get; }
        string OperatorName { get; }
        BoothReading[] GetReadings();
        /// <summary>최근 8시간, 5분 간격 값(과거 → 현재, 마지막 = 현재값). 관리 안 함이면 빈 배열.</summary>
        double[] GetTrend(int booth, MeasureItem item);
        FeedInfo GetFeed();
        /// <summary>최신 이탈이 위.</summary>
        IList<DeviationEvent> GetDeviations();
        /// <summary>[start, end] 사이(end null = 지금까지)에 투입된 행거.</summary>
        HangerRange GetAffectedHangers(DateTime start, DateTime? end);
        /// <summary>조치 등록. 이탈 중인 건은 ActionKind.NoImpact로 등록할 수 없다(false 반환).</summary>
        bool RegisterAction(int eventId, ActionKind action, string remark);
    }
}
