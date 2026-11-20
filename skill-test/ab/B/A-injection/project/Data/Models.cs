using APC.UI.Theming;

namespace InjectionMonitor.Data
{
    /// <summary>사출기 한 대의 현재 상태·측정값. 값이 없으면(대기·통신 전) null.</summary>
    internal sealed class MachineInfo
    {
        public MachineInfo(string name, string process, string note, double? temp, double? pressure, double? cycle, double[] trend)
        {
            Name = name; Process = process; Note = note;
            Temp = temp; Pressure = pressure; Cycle = cycle; Trend = trend;
        }

        public string Name { get; private set; }
        /// <summary>공정 상태: 가동 / 대기 / 정지</summary>
        public string Process { get; private set; }
        /// <summary>가동이 아닐 때의 사유(계획 정지, 금형 교체 등)</summary>
        public string Note { get; private set; }
        /// <summary>금형 온도 ℃</summary>
        public double? Temp { get; private set; }
        /// <summary>사출 압력 bar</summary>
        public double? Pressure { get; private set; }
        /// <summary>사이클 타임 s</summary>
        public double? Cycle { get; private set; }
        /// <summary>금형 온도 추이(시간순, 마지막 = 현재값)</summary>
        public double[] Trend { get; private set; }

        public bool IsRunning { get { return Process == "가동"; } }
    }

    /// <summary>레시피: 금형 온도 설정값 ℃, 사출 압력 상한 bar, 사이클 타임 목표 s(정수).</summary>
    internal sealed class RecipeInfo
    {
        public RecipeInfo(double tempSet, double pressureUpper, int cycleTarget)
        {
            TempSet = tempSet; PressureUpper = pressureUpper; CycleTarget = cycleTarget;
        }

        public double TempSet { get; private set; }
        public double PressureUpper { get; private set; }
        public int CycleTarget { get; private set; }
    }

    /// <summary>알람 이력 한 줄.</summary>
    internal sealed class AlarmRow
    {
        public AlarmRow(StatusKind status, string statusText, string time, string machine, string text)
        {
            Status = status; StatusText = statusText; Time = time; Machine = machine; Text = text;
        }

        public StatusKind Status { get; private set; }
        public string StatusText { get; private set; }
        public string Time { get; private set; }
        public string Machine { get; private set; }
        public string Text { get; private set; }
    }
}
