using System;
using System.Collections.Generic;

namespace TankFlow.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        IList<TankInfo> GetTanks();        // T1~T4
        IList<PumpInfo> GetPumps();        // P1, P2
        IList<ValveInfo> GetValves();      // V1~V6
        IList<AlarmRecord> GetTodayAlarms();   // 최근 것이 먼저
        /// <summary>밸브 조작 지시. 조작자(사원증)와 함께 기록한다.</summary>
        void OperateValve(string valveId, bool open, string badge, DateTime time);
    }
}
