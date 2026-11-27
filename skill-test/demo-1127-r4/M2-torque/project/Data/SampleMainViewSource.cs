using System;
using System.Collections.Generic;

namespace TorqueSequence.Data
{
    /// <summary>화면 확인·render용 샘플. 1-6-2 체결 완료, 7번 첫 NG 상태에서 시작한다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly DateTime Start = new DateTime(2026, 11, 27, 10, 41, 2);

        // 공구가 이어서 보낼 값: 7(OK) 3 8, 순서 어긋난 9, 4 NG 후 재체결 OK, 9 5 10.
        private static readonly ToolReading[] Script =
        {
            new ToolReading(7, 10.1m, 44), new ToolReading(3, 9.6m, 38), new ToolReading(8, 10.4m, 52),
            new ToolReading(9, 10.0m, 40), new ToolReading(4, 8.8m, 41), new ToolReading(4, 9.7m, 36),
            new ToolReading(9, 10.7m, 57), new ToolReading(5, 9.3m, 33), new ToolReading(10, 10.9m, 47)
        };
        private int _next;

        public string GetCurrentSerial() { return "HC-261127-0042"; }
        public string GetCurrentBadge() { return "EMP-10477"; }

        public IList<BoltAttempt> GetAttempts()
        {
            return new List<BoltAttempt>
            {
                new BoltAttempt(1, 10.2m, 45, Start),
                new BoltAttempt(6, 9.0m, 30, Start.AddSeconds(19)),     // 하한 경계 = OK
                new BoltAttempt(2, 11.0m, 60, Start.AddSeconds(38)),    // 상한 경계 = OK
                new BoltAttempt(7, 11.4m, 48, Start.AddSeconds(63))     // 토크 상한 초과 = NG
            };
        }

        public ToolReading ReadTool() { return _next < Script.Length ? Script[_next++] : null; }
        public void ResetTool() { _next = 0; }
    }
}
