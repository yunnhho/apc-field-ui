using System.Collections.Generic;

namespace TankFlow.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB)은 이 인터페이스를 구현해 넣는다.
    /// 값은 읽기만 하고, 밸브 조작만 화면이 일으키는 동작이다.
    /// </summary>
    internal interface IMainViewSource
    {
        IList<TankInfo> GetTanks();
        IList<ValveInfo> GetValves();
        IList<PumpInfo> GetPumps();
        /// <summary>오늘 알람 이력, 최근 것이 먼저.</summary>
        IList<AlarmInfo> GetAlarms();
        /// <summary>밸브 열기/닫기 지시. 인터락 판단은 화면(FlowRules)이 이미 했다. operatorId = 스캔한 사원증.</summary>
        void OperateValve(string valveId, bool open, string operatorId);
    }
}
