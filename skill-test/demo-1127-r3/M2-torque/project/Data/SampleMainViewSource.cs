using System;

namespace TorqueSequence.Data
{
    /// <summary>화면 확인·render용 샘플 데이터.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly DateTime Base = new DateTime(2026, 11, 27, 14, 31, 40);

        // 자동 모드 샘플 수신값: 재체결 7번 OK → 3번 OK → (순서 위반 4번) → 8번 …
        private readonly PointResult[] _script =
        {
            new PointResult(7, 10.2m, 47, Base.AddSeconds(95)),
            new PointResult(3, 11.0m, 60, Base.AddSeconds(118)),
            new PointResult(4, 10.4m, 41, Base.AddSeconds(131)),
            new PointResult(8, 9.0m, 30, Base.AddSeconds(150)),
            new PointResult(4, 10.9m, 55, Base.AddSeconds(171)),
            new PointResult(9, 9.7m, 38, Base.AddSeconds(189)),
            new PointResult(5, 10.3m, 49, Base.AddSeconds(207)),
            new PointResult(10, 9.4m, 33, Base.AddSeconds(226)),
        };
        private int _next;

        public SessionSnapshot GetSession()
        {
            // 순서 1-6-2-7 중 7번이 첫 NG(토크 하한 미달), 아래 세 포인트는 경계값 OK.
            var results = new[]
            {
                new PointResult(1, 10.2m, 44, Base),
                new PointResult(6, 9.0m, 30, Base.AddSeconds(21)),
                new PointResult(2, 11.0m, 60, Base.AddSeconds(43)),
                new PointResult(7, 8.6m, 52, Base.AddSeconds(66)),
            };
            return new SessionSnapshot("HC-261127-0042", "EMP-20417", results, new[] { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 });
        }

        public PointResult ReadTool()
        {
            if (_next >= _script.Length) return null;
            PointResult r = _script[_next++];
            return new PointResult(r.Point, r.Torque, r.Angle, DateTime.Now);
        }

        public void SaveResult(string serial, string op, PointResult result) { }

        public void SaveRework(string serial, int point, string supervisor, string reason) { }
    }
}
