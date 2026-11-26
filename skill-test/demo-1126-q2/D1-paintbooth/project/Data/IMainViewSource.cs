using System;
using System.Collections.Generic;

namespace BumperPaintBoothMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB)은 이 인터페이스를 구현해 넣는다.
    /// 행거 범위(Excursion.FirstHanger~LastHanger)는 이탈 구간에 투입된 행거로 데이터 쪽이 계산해 준다.
    /// </summary>
    internal interface IMainViewSource
    {
        DateTime Now { get; }
        IList<Booth> GetBooths();
        /// <summary>최근 8시간 5분 간격 값(왼쪽=과거, 마지막 = 현재값). item: 0 온도, 1 습도.</summary>
        double[] GetTrend(string booth, int item);
        InputInfo GetCurrentInput();
        /// <summary>최신순 이탈 이력.</summary>
        IList<Excursion> GetExcursions();
        string[] GetConfirmers();
        void SaveAction(int excursionId, string action, string note, string confirmer);
    }
}
