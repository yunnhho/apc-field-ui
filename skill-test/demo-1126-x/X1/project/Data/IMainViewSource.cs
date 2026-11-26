using System.Collections.Generic;

namespace Polishing.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, SAP RFC)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>설비군 이름(탭 순서). 0 = 고속, 1 = 저속.</summary>
        IList<string> GetGroups();

        /// <summary>설비 현재 값(컨트롤러 PV 모니터링 + 마지막 전송 조건). 전 설비.</summary>
        IList<UnitInfo> GetUnits();

        /// <summary>오늘 SAP 리턴 전송 이력(최근 것이 앞).</summary>
        IList<SendRecord> GetSendHistory();

        /// <summary>설비 한 대에 한 번에 스캔할 수 있는 Lot 수(저속 2, 고속 4).</summary>
        int GetSlotCount(UnitInfo unit);

        /// <summary>Lot 스캔값으로 SAP·연마 기준정보 조회. 레시피가 없으면 null.</summary>
        LotRecipe LookupLot(UnitInfo unit, string lot);

        /// <summary>조건 확인 후 컨트롤러에 시간(SV) 전송 + SAP 실시간 정보 리턴. 성공 시 true.</summary>
        bool SendCondition(UnitInfo unit, IList<LotRecipe> lots, IList<string> barrelNos);
    }
}
