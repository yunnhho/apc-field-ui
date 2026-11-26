using System;
using System.Collections.Generic;

namespace DieChangeManager.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        DateTime Now { get; }
        IList<MachineInfo> GetMachines();
        /// <summary>금형 번호로 조회. 없으면 null.</summary>
        MoldInfo FindMold(string moldId);
        /// <summary>남은 비율 10% 이하 금형(남은 비율 낮은 순).</summary>
        IList<MoldInfo> GetLifeWarnings();
        IList<PlanInfo> GetTodayPlan();
        /// <summary>사원증 번호가 등록된 작업자인지.</summary>
        bool IsKnownOperator(string badge);
        /// <summary>교체 시작을 등록한다(설비·장착 금형·작업자·예열 확인).</summary>
        void RegisterChange(string machine, string moldId, string badge, DateTime start);
    }
}
