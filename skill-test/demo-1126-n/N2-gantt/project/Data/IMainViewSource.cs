using System.Collections.Generic;

namespace MoldingShiftGantt.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(MES·성형기 PLC)은 이 인터페이스를 구현해 넣는다.
    /// 시각은 모두 교대 시작(08:00) 기준 분.
    /// </summary>
    internal interface IMainViewSource
    {
        int ShiftStartHour { get; }
        int ShiftMinutes { get; }
        int NowMin { get; }

        Machine[] GetMachines();

        /// <summary>현재 시각 기준 계획 수량(진행 비율만큼).</summary>
        double PlannedQtyAtNow(Machine m);
        int ActualQty(Machine m);
        bool IsDelayed(Machine m);
        bool IsDown(Machine m);

        JobDetail GetJobDetail(Machine m, WorkOrder wo);
        /// <summary>실적이 있는 작업 중 가장 최근 것(없으면 null) — 처음 선택용.</summary>
        WorkOrder GetRunningJob(Machine m);

        // 긴급 작업 끼워넣기
        /// <summary>작업 지시 바코드 조회. 없으면 null.</summary>
        WorkOrder FindWorkOrder(string code);
        /// <summary>끼워 넣을 수 있는 위치 이름(앞에 넣을 작업 ... 맨 뒤). 인덱스 = 삽입 위치.</summary>
        string[] GetInsertPositions(Machine m);
        InsertPreview PreviewInsert(Machine m, WorkOrder urgent, int position);
        /// <summary>생산관리 사원증 확인.</summary>
        BadgeResult CheckApprovalBadge(string code);
        void InsertUrgent(Machine m, WorkOrder urgent, int position, string approverBadge);
    }
}
