using System.Collections.Generic;

namespace CellLeakTester.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB, 시리얼)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        string CurrentLot { get; }
        string CurrentModel { get; }
        string CurrentWorker { get; }
        IList<string> Models { get; }

        /// <summary>오늘 검사 이력. 최근 것이 먼저.</summary>
        IList<InspectionRow> GetHistory();
        int TodayTotal { get; }
        int TodayNg { get; }

        void SaveLot(string lot, string model, string worker);
        bool VerifyAdmin(string password);
    }
}
