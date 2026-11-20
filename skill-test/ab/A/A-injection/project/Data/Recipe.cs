namespace InjectionMonitor.Data
{
    /// <summary>레시피(사람이 넣는 설정값). 판정 기준은 Judge가 여기서 파생한다.</summary>
    internal sealed class Recipe
    {
        public Recipe(double moldTempSet, double pressureUpper, int cycleTarget)
        {
            MoldTempSet = moldTempSet;
            PressureUpper = pressureUpper;
            CycleTarget = cycleTarget;
        }

        /// <summary>금형 온도 설정값 (℃)</summary>
        public double MoldTempSet { get; private set; }
        /// <summary>사출 압력 상한 (bar)</summary>
        public double PressureUpper { get; private set; }
        /// <summary>사이클 타임 목표 (s, 정수)</summary>
        public int CycleTarget { get; private set; }
    }
}
