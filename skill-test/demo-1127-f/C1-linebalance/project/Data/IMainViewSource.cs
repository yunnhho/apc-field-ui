using System.Collections.Generic;

namespace LineBalanceBoard.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB, 시리얼)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>택트 타임(0.1초 단위, 600 = 60.0초).</summary>
        int TaktTenths { get; }

        /// <summary>오늘 날짜 yyyy-MM-dd (자격 만료 판정 기준).</summary>
        string Today { get; }

        /// <summary>공정 OP10~OP80, 흐름 순서.</summary>
        IList<ProcessInfo> GetProcesses();

        /// <summary>사번으로 작업자 조회. 등록되지 않았으면 null.</summary>
        WorkerInfo FindWorker(string workerId);

        /// <summary>현재 그 작업자가 배치된 공정 코드. 미배치면 "".</summary>
        string FindAssignment(string workerId);

        /// <summary>배치 확정. 저장 실패 시 false.</summary>
        bool Assign(string processCode, string workerId);
    }
}
