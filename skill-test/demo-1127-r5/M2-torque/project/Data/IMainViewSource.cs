using System;
using System.Collections.Generic;

namespace TorqueSequence.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(체결 공구 통신, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>화면을 열 때 이어 받을 작업 중 부품의 시리얼(없으면 빈 문자열).</summary>
        string CurrentSerial { get; }

        /// <summary>작업 중인 작업자 사원증(없으면 빈 문자열).</summary>
        string CurrentOperator { get; }

        /// <summary>부품 시리얼의 지금까지 체결 기록(시간 순, NG 포함). 없으면 빈 목록.</summary>
        IList<TightenRecord> LoadRecords(string serial);

        void SaveRecord(string serial, string operatorBadge, TightenRecord record);

        /// <summary>반장 승인으로 NG 포인트 재체결을 허용한 기록.</summary>
        void SaveApproval(string serial, int point, string supervisorBadge, string reason);

        /// <summary>같은 포인트 NG 2회로 부품 불량 처리한 기록.</summary>
        void SaveScrap(string serial, int point);

        /// <summary>자동 모드: 체결 공구가 값을 보낼 때 일어난다.</summary>
        event Action<TightenRecord> ToolReceived;
    }
}
