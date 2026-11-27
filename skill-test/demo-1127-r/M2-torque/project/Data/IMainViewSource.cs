using System;

namespace TorqueSequence.Data
{
    /// <summary>체결 공구(자동 모드)가 보낸 포인트별 체결값.</summary>
    internal sealed class ToolValueEventArgs : EventArgs
    {
        public ToolValueEventArgs(int point, decimal torque, int angle) { Point = point; Torque = torque; Angle = angle; }
        public int Point { get; private set; }
        public decimal Torque { get; private set; }
        public int Angle { get; private set; }
    }

    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(체결 공구 통신, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>현재 부품의 체결 진행 상태(화면이 열릴 때 한 번).</summary>
        TighteningSession GetSession();

        /// <summary>재체결 승인 dialog의 사유 목록.</summary>
        string[] GetRetightenReasons();

        /// <summary>자동 모드: 체결 공구가 체결값을 보냈다(UI 스레드로 올려 줄 것).</summary>
        event EventHandler<ToolValueEventArgs> ToolValueReceived;
    }
}
