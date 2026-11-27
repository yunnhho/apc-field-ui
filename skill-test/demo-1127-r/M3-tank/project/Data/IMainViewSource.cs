using System;
using System.Collections.Generic;

namespace TankFlow.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        IList<TankInfo> GetTanks();
        IList<PumpInfo> GetPumps();
        IList<ValveInfo> GetValves();
        /// <summary>오늘 알람 이력, 최신이 앞.</summary>
        IList<AlarmEntry> GetAlarms();
        /// <summary>밸브 조작 지시와 기록(조작자 사원증, 시각). 인터락은 호출 전에 화면이 막는다.</summary>
        void OperateValve(string valveId, bool open, string operatorId, DateTime at);
    }
}
