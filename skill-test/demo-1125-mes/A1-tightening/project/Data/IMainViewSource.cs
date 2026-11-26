using System;
using System.Collections.Generic;

namespace CaliperTighteningStation.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, 체결기, MES DB)은 이 인터페이스를 구현해 넣는다.
    /// 판정 중 예외·DB 타임아웃은 구현이 E99 NG 행으로 돌려준다(fail-safe).
    /// </summary>
    internal interface IMainViewSource
    {
        StationSnapshot Current();
        /// <summary>시리얼 스캔: 인터락 판정(OK든 NG든 기록)과 체결 시작.</summary>
        StationSnapshot Scan(string serial);
        BypassInfo GetBypass();   // 우회 중이 아니면 null, 만료되면 자동 해제
        bool VerifyAdmin(string password);
        void StartBypass(string item, int minutes, string reason);
        int TodayTotal { get; }
        int TodayOk { get; }
        int TodayNg { get; }
        IList<HistoryRow> History();
    }
}
