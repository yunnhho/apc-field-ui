using System;
using System.Collections.Generic;

namespace TorqueSequence.Data
{
    /// <summary>화면 확인·render용 샘플. 1·6·2번 OK(경계값 포함), 7번 NG 1회 상태에서 시작한다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly DateTime Start = new DateTime(2026, 11, 27, 10, 12, 41);

        // 자동 모드 샘플 공구값(토크, 각도). 순서 위반 한 번(2번째 호출)·NG 한 번(5번째 호출) 포함.
        private static readonly decimal[] Torques = { 10.2m, 9.8m, 10.6m, 9.0m, 8.7m, 10.4m, 11.0m, 9.5m, 10.9m, 10.1m };
        private static readonly int[] Angles = { 41, 36, 50, 30, 44, 38, 60, 47, 52, 33 };
        private int _calls;

        public string LoadSerial() { return "HC-261127-0042"; }

        public string LoadBadge() { return "EMP-10482"; }

        public IList<PointResult> LoadAttempts()
        {
            return new List<PointResult>
            {
                new PointResult(1, 9.0m, 30, Start),                       // 하한 경계 = OK
                new PointResult(6, 10.4m, 45, Start.AddSeconds(14)),
                new PointResult(2, 11.0m, 60, Start.AddSeconds(33)),       // 상한 경계 = OK
                new PointResult(7, 8.8m, 28, Start.AddSeconds(49)),        // 토크·각도 모두 미달 → NG
            };
        }

        public PointResult ReadToolValue(int expectedPoint)
        {
            int i = _calls++;
            int point = i == 1 ? expectedPoint % 10 + 1 : expectedPoint;   // 샘플: 두 번째 값은 엉뚱한 포인트
            return new PointResult(point, Torques[i % Torques.Length], Angles[i % Angles.Length], DateTime.Now);
        }

        public void SaveAttempt(string serial, string badge, PointResult result) { }

        public void SaveRetightenApproval(string serial, int point, string supervisorBadge, string reason) { }

        public void SaveScrap(string serial, int point) { }
    }
}
