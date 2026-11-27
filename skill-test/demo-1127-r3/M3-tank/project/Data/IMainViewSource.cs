using System.Collections.Generic;

namespace TankFlow.Data
{
    /// <summary>메인 화면 데이터. 실제 연결(PLC, DB)은 이 인터페이스를 구현해 넣는다.</summary>
    internal interface IMainViewSource
    {
        /// <summary>탱크 레벨·온도, 밸브 위치·마지막 조작, 펌프 가동 상태의 현재 값.</summary>
        ProcessSnapshot GetSnapshot();

        /// <summary>오늘 알람 이력(최근 것이 앞).</summary>
        IList<AlarmEntry> GetTodayAlarms();

        /// <summary>밸브 조작 명령. 조작자는 사원증 코드(EMP-nnnnn).</summary>
        void OperateValve(string valveId, bool open, string operatorId);
    }
}
