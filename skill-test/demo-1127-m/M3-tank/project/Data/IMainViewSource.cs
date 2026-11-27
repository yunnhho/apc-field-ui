using System.Collections.Generic;

namespace TankFlow.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        IList<TankInfo> GetTanks();          // T1~T4 순서
        IList<ValveInfo> GetValves();        // V1~V6 순서
        IList<PumpInfo> GetPumps();          // P1, P2
        IList<AlarmInfo> GetAlarms();        // 오늘 이력, 최근이 앞

        /// <summary>밸브 열기/닫기 명령. 인터락은 구현 쪽에서도 다시 확인한다. operatorId = 사원증(EMP-nnnnn).</summary>
        void OperateValve(string valveId, bool open, string operatorId);
    }
}
