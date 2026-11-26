using System.Collections.Generic;

namespace ShipPackVerifier.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(품질 시스템, 프린터, DB)은 이 인터페이스를 구현해 넣는다.
    /// 판정 규칙(품번·중복·보류·입수 초과·라벨 출력 조건·반장 확인)은 데이터 쪽이 정하고 화면은 결과만 보여 준다.
    /// </summary>
    internal interface IMainViewSource
    {
        ShipOrder GetOrder();
        BoxProgress GetCurrentBox();
        /// <summary>현재 박스에 담은 시리얼(최근 스캔이 앞).</summary>
        IList<PackedSerial> GetCurrentSerials();
        /// <summary>완료 박스(최근 출력이 앞).</summary>
        IList<PackedBox> GetCompletedBoxes();
        /// <summary>마지막 스캔 결과. 스캔 전이면 null.</summary>
        ScanResult GetLastScan(out string serial);
        /// <summary>시리얼 한 건을 판정하고 OK면 현재 박스에 담는다.</summary>
        ScanResult Scan(string serial);
        /// <summary>현재 박스 라벨 출력. 조건 미충족이면 false.</summary>
        bool PrintCurrentLabel();
        /// <summary>반장 사원증이면 이름, 아니면 null.</summary>
        string FindSupervisor(string badge);
        /// <summary>완료 박스 라벨 재출력. 반장 확인이 안 되면 false.</summary>
        bool ReprintLabel(string boxNo, string reason, string supervisorBadge);
    }
}
