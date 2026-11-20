using System.Collections.Generic;

namespace InjectionMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB, 시리얼)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        IList<Machine> GetMachines();
        /// <summary>금형 온도 추이 가로축 글자(양끝·가운데 등).</summary>
        string[] GetTrendLabels();
        /// <summary>최근 알람 이력(최신이 위).</summary>
        IList<AlarmRow> GetAlarms();
        Recipe GetRecipe();
        void SaveRecipe(Recipe recipe);
    }
}
