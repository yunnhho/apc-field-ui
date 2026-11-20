using APC.UI.Theming;

namespace FATester.Data
{
    /// <summary>시험 채널 한 개의 현재 상태(읽기 전용 값 객체). 값이 없으면 null.</summary>
    internal sealed class ChannelInfo
    {
        public ChannelInfo(string name, StatusKind process, string processText, int progress,
            string sampleId, string testItem, double? resistance, double? leakage,
            double chamberTemp, double chamberHumidity, int cycles, string lastErrorCode, string lastErrorText)
        {
            Name = name; Process = process; ProcessText = processText; Progress = progress;
            SampleId = sampleId; TestItem = testItem; Resistance = resistance; Leakage = leakage;
            ChamberTemp = chamberTemp; ChamberHumidity = chamberHumidity; Cycles = cycles;
            LastErrorCode = lastErrorCode; LastErrorText = lastErrorText;
        }

        public string Name { get; private set; }
        /// <summary>공정 상태 색(대기 Stop, 시험중·완료 Run, 오류 Alarm).</summary>
        public StatusKind Process { get; private set; }
        /// <summary>시험중 / 완료 / 오류 / 대기</summary>
        public string ProcessText { get; private set; }
        public int Progress { get; private set; }
        public string SampleId { get; private set; }
        public string TestItem { get; private set; }
        public double? Resistance { get; private set; }
        public double? Leakage { get; private set; }
        public double ChamberTemp { get; private set; }
        public double ChamberHumidity { get; private set; }
        public int Cycles { get; private set; }
        public string LastErrorCode { get; private set; }
        public string LastErrorText { get; private set; }
    }

    /// <summary>불량 분류 결과 한 행.</summary>
    internal sealed class DefectRow
    {
        public DefectRow(string sampleId, string mode, string location, string judgedAt)
        {
            SampleId = sampleId; Mode = mode; Location = location; JudgedAt = judgedAt;
        }

        public string SampleId { get; private set; }
        public string Mode { get; private set; }
        public string Location { get; private set; }
        public string JudgedAt { get; private set; }
    }
}
