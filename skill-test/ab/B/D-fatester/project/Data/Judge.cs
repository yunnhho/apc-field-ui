using APC.UI.Theming;

namespace FATester.Data
{
    /// <summary>측정값 판정 함수 하나(타일·카드·헤더가 모두 이것을 쓴다). 기준값은 요구서에 없어 정한 값.</summary>
    internal static class Judge
    {
        public const double ResistanceUpper = 50.0, ResistanceWarn = 45.0;   // mΩ
        public const double LeakageUpper = 100.0, LeakageWarn = 90.0;        // nA

        public static StatusKind Resistance(double v) { return Grade(v, ResistanceUpper, ResistanceWarn); }
        public static StatusKind Leakage(double v) { return Grade(v, LeakageUpper, LeakageWarn); }

        // 상한 초과부터 알람, 경고 시작값과 같으면 경고
        private static StatusKind Grade(double v, double upper, double warn)
        {
            if (v > upper) return StatusKind.Alarm;
            return v >= warn ? StatusKind.Warn : StatusKind.Run;
        }

        /// <summary>채널 상태 = 공정 상태와 값 판정 중 나쁜 쪽. 시험중인 채널만 값을 판정한다.</summary>
        public static StatusKind Channel(ChannelInfo c, out string reason)
        {
            reason = null;
            if (c.Process != StatusKind.Run || c.ProcessText != "시험중") return c.Process;
            StatusKind r = c.Resistance.HasValue ? Resistance(c.Resistance.Value) : StatusKind.Run;
            StatusKind l = c.Leakage.HasValue ? Leakage(c.Leakage.Value) : StatusKind.Run;
            if (r == StatusKind.Run && l == StatusKind.Run) return StatusKind.Run;
            bool resistanceWorse = (int)r >= (int)l && r != StatusKind.Run;
            StatusKind worst = resistanceWorse ? r : l;
            reason = (resistanceWorse ? "저항 " : "누설전류 ") + (worst == StatusKind.Alarm ? "상한 초과" : "한계 근접");
            return worst;
        }
    }
}
