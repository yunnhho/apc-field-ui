using System;
using System.Collections.Generic;

namespace RawMaterialIssueTerminal.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(DB, 라벨 프린터 서버 등)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        DateTime Today { get; }

        /// <summary>화면을 열 때 이어받을 스캔(진행 중이던 지시·마지막 포대). 없으면 빈 배열.</summary>
        string[] StartupScans { get; }

        /// <summary>작업지시 바코드로 지시서 조회. 없으면 null.</summary>
        OrderInfo FindOrder(string code);
        IList<MaterialLine> GetMaterials(string orderCode);
        IList<LotStock> GetLots(string materialCode);
        IList<IssueRecord> GetTodayHistory();

        /// <summary>승인자 사번 → 이름. 승인 권한이 없거나 모르는 사번이면 null.</summary>
        string FindApproverName(string employeeCode);

        /// <summary>출고 한 건 기록. note = "" 또는 "FIFO 예외 승인 · 사번".</summary>
        void RecordIssue(string orderCode, BagLabel bag, string note);
        void CompleteOrder(string orderCode);
    }
}
