using APC.UI.Theming;

namespace FATester.Data
{
    /// <summary>시험 채널 한 개의 현재 값(읽기 전용). 값 없음은 빈 문자열.</summary>
    internal sealed class ChannelRow
    {
        public const string Testing = "시험중", Done = "완료", Error = "오류", Waiting = "대기";

        public ChannelRow(string name, string state, string reason, int progress, string sampleId, string testItem,
            string resistance, string leakage, string chamberTemp, string chamberHumi, string cycles, string targetCycles, string errorCode)
        {
            Name = name; State = state; Reason = reason; Progress = progress; SampleId = sampleId; TestItem = testItem;
            Resistance = resistance; Leakage = leakage; ChamberTemp = chamberTemp; ChamberHumi = chamberHumi;
            Cycles = cycles; TargetCycles = targetCycles; ErrorCode = errorCode;
        }

        public string Name { get; private set; }
        public string State { get; private set; }
        public string Reason { get; private set; }
        public int Progress { get; private set; }
        public string SampleId { get; private set; }
        public string TestItem { get; private set; }
        public string Resistance { get; private set; }   // mΩ, 0.0
        public string Leakage { get; private set; }      // nA, 0.0
        public string ChamberTemp { get; private set; }  // ℃, 0.0
        public string ChamberHumi { get; private set; }  // %RH, 0.0
        public string Cycles { get; private set; }       // #,0
        public string TargetCycles { get; private set; }
        public string ErrorCode { get; private set; }    // "" = 최근 오류 없음

        /// <summary>공정 상태 → 상태색. 화면·타일·헤더·dialog가 이 함수 하나를 쓴다.</summary>
        public StatusKind Status
        {
            get
            {
                switch (State)
                {
                    case Error: return StatusKind.Alarm;
                    case Waiting: return StatusKind.Stop;
                    default: return StatusKind.Run;   // 시험중·완료
                }
            }
        }

        /// <summary>"공정 상태 · 사유" 문구.</summary>
        public string StatusText { get { return Reason.Length > 0 ? State + " · " + Reason : State; } }
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
        public string JudgedAt { get; private set; }   // HH:mm:ss
    }
}
