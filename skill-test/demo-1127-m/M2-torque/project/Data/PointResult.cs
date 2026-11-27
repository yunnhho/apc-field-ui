using System;

namespace TorqueSequence.Data
{
    /// <summary>한 포인트의 최신 체결 결과(불변). NgCount는 지금까지 이 포인트의 NG 누적 횟수.</summary>
    internal sealed class PointResult
    {
        public PointResult(int point, double torque, int angle, DateTime time, int ngCount)
        {
            Point = point;
            Torque = TorqueRules.RoundTorque(torque);
            Angle = angle;
            Time = time;
            NgCount = ngCount;
        }

        public int Point { get; private set; }
        public double Torque { get; private set; }
        public int Angle { get; private set; }
        public DateTime Time { get; private set; }
        public int NgCount { get; private set; }

        public string Fault { get { return TorqueRules.Fault(Torque, Angle); } }
        public bool IsOk { get { return Fault == null; } }
    }

    /// <summary>체결 공구가 보낸 값(자동 모드). Point 번호가 순서와 다르면 화면이 막는다.</summary>
    internal sealed class ToolReading : EventArgs
    {
        public ToolReading(int point, double torque, int angle) { Point = point; Torque = torque; Angle = angle; }
        public int Point { get; private set; }
        public double Torque { get; private set; }
        public int Angle { get; private set; }
    }

    /// <summary>화면이 처음 열 때 받는 작업 상태.</summary>
    internal sealed class SessionInfo
    {
        public SessionInfo(string serial, string employee, PointResult[] results, bool manual)
        {
            Serial = serial; Employee = employee; Results = results; Manual = manual;
        }

        public string Serial { get; private set; }
        public string Employee { get; private set; }
        public PointResult[] Results { get; private set; }
        public bool Manual { get; private set; }
    }
}
