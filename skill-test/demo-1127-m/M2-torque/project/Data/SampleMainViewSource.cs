using System;

namespace TorqueSequence.Data
{
    /// <summary>화면 확인·render용 샘플. 체결 4번째(7번)가 토크 상한 초과 NG인 상태.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        // 화면이 읽기만 하는 샘플이라 이벤트는 쓰지 않는다(실제 공구 연결이 올리는 자리).
        public event EventHandler<ToolReading> ReadingReceived { add { } remove { } }

        public SessionInfo GetSession()
        {
            var t = new DateTime(2026, 11, 27, 10, 14, 2);
            return new SessionInfo("HC-261127-0042", "EMP-10234", new[]
            {
                new PointResult(1, 10.2, 45, t, 0),
                new PointResult(6, 9.0, 30, t.AddSeconds(14), 0),     // 하한·각도 하한 = 경계값, OK
                new PointResult(2, 11.0, 60, t.AddSeconds(29), 0),    // 상한·각도 상한 = 경계값, OK
                new PointResult(7, 11.4, 52, t.AddSeconds(45), 1),    // 토크 상한 초과 NG 1회
            }, true);
        }

        public void SaveResult(string serial, string employee, PointResult result) { }

        public void SaveApproval(string serial, int point, string foreman, string reason) { }
    }
}
