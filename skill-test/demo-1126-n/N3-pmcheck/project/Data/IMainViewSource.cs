using System;
using System.Collections.Generic;

namespace PressPmChecklist.Data
{
    /// <summary>메인 화면 데이터 자리. 실제 연결(DB, 설비 서버)은 이 인터페이스를 구현해 넣는다.</summary>
    internal interface IMainViewSource
    {
        /// <summary>점검 항목(순서대로). 첫 항목이 안전 단계.</summary>
        IList<PmStep> GetSteps();

        /// <summary>진행 중이던 점검. 새로 시작하면 null.</summary>
        PmSession GetCurrentSession();

        /// <summary>스캔한 설비 바코드가 점검 대상으로 등록된 설비인가.</summary>
        bool IsKnownEquipment(string code);

        /// <summary>스캔한 사원증이 등록된 점검자인가.</summary>
        bool IsKnownEmployee(string code);

        /// <summary>단계 하나를 끝낼 때마다 결과를 남긴다.</summary>
        void SaveStepResult(string equipment, string employee, StepResult result);

        /// <summary>NG 항목의 정비 요청을 등록하고 요청 번호를 돌려준다.</summary>
        string RegisterMaintenanceRequest(string equipment, IList<StepResult> ngResults);

        /// <summary>점검을 완료로 닫는다(소요 시간 포함).</summary>
        void CompleteSession(string equipment, string employee, TimeSpan elapsed);
    }
}
