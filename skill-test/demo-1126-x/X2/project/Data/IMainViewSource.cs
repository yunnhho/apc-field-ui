using System.Collections.Generic;

namespace Polish.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(컨트롤러 PLC, SAP RFC)은 이 인터페이스를 구현해 넣는다.
    /// 컨트롤러 주소: 시간 SV 400001~2 · 시간 PV 301004~5 · RPM PV 31003~4 · 시작 000001 (요구사항 파일 기준, 시트 확인 필요).
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>설비군 탭 이름(저속 1~3호기, 저속 4~6호기, 고속 1~4호기, 고속 5~8호기).</summary>
        string[] GroupNames { get; }

        /// <summary>설비군 하나의 행(컨트롤러 PV 실시간 값 포함).</summary>
        IList<EquipmentRow> GetEquipment(int group);

        /// <summary>LOT 바코드 → SAP·연마 기준정보 마스터 조회. 레시피가 없으면 null.</summary>
        PolishCondition FindCondition(string lot);

        /// <summary>담당자(엔지니어·블록장) 비밀번호 확인.</summary>
        bool CheckEngineerPassword(string password);

        /// <summary>작업 조건(시간 SV)을 설비 컨트롤러에 쓰고 SAP RFC(ZWORK3033_A)에 알린다. 성공 여부.</summary>
        bool SendCondition(string equipment, string[] lots, string[] barrels, PolishCondition condition);
    }
}
