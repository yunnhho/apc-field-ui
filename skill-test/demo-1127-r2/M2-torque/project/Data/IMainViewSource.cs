using System.Collections.Generic;

namespace TorqueSequence.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(체결 공구 통신, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>화면을 열 때 이어 갈 부품 시리얼(없으면 "").</summary>
        string LoadSerial();

        /// <summary>작업자 사원증(없으면 "").</summary>
        string LoadBadge();

        /// <summary>이 부품의 지금까지 체결 시도(시간 순, NG 재체결 포함).</summary>
        IList<PointResult> LoadAttempts();

        /// <summary>자동 모드: 체결 공구가 보낸 값. 공구가 보낸 포인트 번호를 그대로 돌려준다(순서 확인은 화면이 한다). expectedPoint는 샘플 전용.</summary>
        PointResult ReadToolValue(int expectedPoint);

        void SaveAttempt(string serial, string badge, PointResult result);

        void SaveRetightenApproval(string serial, int point, string supervisorBadge, string reason);

        void SaveScrap(string serial, int point);
    }
}
