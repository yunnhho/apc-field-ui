using System;

namespace TorqueSequence.Data
{
    /// <summary>화면 확인·render용 샘플 데이터: 1·6·2번 OK(경계값 포함), 7번 첫 NG로 재체결 승인 대기.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        public event EventHandler<ToolValueEventArgs> ToolValueReceived { add { } remove { } }

        public TighteningSession GetSession()
        {
            var s = new TighteningSession { Serial = "HC-261127-0042", Badge = "EMP-10458" };
            var t = new DateTime(2026, 11, 27, 9, 12, 3);
            s.Submit(1, 10.2m, 45, t);
            s.Submit(6, 9.0m, 30, t.AddSeconds(18));    // 하한 경계 = OK
            s.Submit(2, 11.0m, 60, t.AddSeconds(37));   // 상한 경계 = OK
            s.Submit(7, 11.4m, 45, t.AddSeconds(59));   // 토크 상한 초과 = NG
            return s;
        }

        public string[] GetRetightenReasons()
        {
            return new[] { "체결면 이물 제거 후 재체결", "공구 설정 확인 후 재체결", "볼트 교체 후 재체결", "기타 (반장 확인)" };
        }
    }
}
