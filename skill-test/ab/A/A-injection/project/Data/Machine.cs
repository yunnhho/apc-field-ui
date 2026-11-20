using APC.UI.Theming;

namespace InjectionMonitor.Data
{
    /// <summary>사출기 한 대의 현재 상태. HasValue=false면 읽은 값 없음(대기).</summary>
    internal sealed class Machine
    {
        public Machine(string name, string processText, bool running, bool hasValue,
                       double temp, double pressure, double cycle, double[] tempTrend)
        {
            Name = name; ProcessText = processText; Running = running; HasValue = hasValue;
            Temp = temp; Pressure = pressure; Cycle = cycle; TempTrend = tempTrend;
        }

        public string Name { get; private set; }
        /// <summary>공정 상태 문구: 가동 / 대기 / 정지</summary>
        public string ProcessText { get; private set; }
        public bool Running { get; private set; }
        public bool HasValue { get; private set; }
        public double Temp { get; private set; }
        public double Pressure { get; private set; }
        public double Cycle { get; private set; }
        /// <summary>금형 온도 추이(과거 → 현재). 마지막 점 = Temp.</summary>
        public double[] TempTrend { get; private set; }
    }

    /// <summary>알람 이력 한 줄.</summary>
    internal sealed class AlarmRow
    {
        public AlarmRow(StatusKind status, string time, string machine, string text)
        {
            Status = status; Time = time; Machine = machine; Text = text;
        }

        public StatusKind Status { get; private set; }
        public string Time { get; private set; }
        public string Machine { get; private set; }
        public string Text { get; private set; }
    }
}
