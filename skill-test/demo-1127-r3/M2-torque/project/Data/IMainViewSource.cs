namespace TorqueSequence.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(체결 공구 통신, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>화면이 처음 보여 줄 작업 상태(진행 중인 부품 복원).</summary>
        SessionSnapshot GetSession();

        /// <summary>자동 모드: 체결 공구가 보낸 최근 체결값. 없으면 null. (실제는 통신 이벤트로 받는다)</summary>
        PointResult ReadTool();

        /// <summary>포인트 체결 결과 저장(OK·NG 모두).</summary>
        void SaveResult(string serial, string op, PointResult result);

        /// <summary>재체결 승인 기록(반장 사원증·사유).</summary>
        void SaveRework(string serial, int point, string supervisor, string reason);
    }
}
