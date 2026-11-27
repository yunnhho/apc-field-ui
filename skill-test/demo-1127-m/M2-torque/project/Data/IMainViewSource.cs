using System;

namespace TorqueSequence.Data
{
    /// <summary>메인 화면 데이터 자리. 실제 연결(체결 공구 통신, DB)은 이 인터페이스를 구현해 넣는다.</summary>
    internal interface IMainViewSource
    {
        /// <summary>화면을 열 때의 작업 상태(진행 중이던 부품이 있으면 이어서).</summary>
        SessionInfo GetSession();

        /// <summary>자동 모드: 체결 공구가 값을 보낸다.</summary>
        event EventHandler<ToolReading> ReadingReceived;

        /// <summary>판정된 체결 결과를 저장한다.</summary>
        void SaveResult(string serial, string employee, PointResult result);

        /// <summary>반장이 재체결을 승인했다.</summary>
        void SaveApproval(string serial, int point, string foreman, string reason);
    }
}
