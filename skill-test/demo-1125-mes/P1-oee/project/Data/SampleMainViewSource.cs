using System;
using System.Collections.Generic;

namespace PressLineOee.Data
{
    /// <summary>화면 확인·render용 샘플. 2026-11-25 10:20:10(주간, 생산일자 06:00부터 260분).</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly DateTime Today = new DateTime(2026, 11, 25);
        private readonly List<StopEvent> _events = new List<StopEvent>();

        public SampleMainViewSource()
        {
            // 최근 시작 순. 설비별 정지 합계가 아래 MachineInfo 집계와 맞는다.
            Add("E17", "P03", StopKind.Unplanned, "09:50:30", "09:59:30", null, null);
            Add("E16", "P04", StopKind.Unplanned, "09:42:10", null, null, null);                      // 진행 중 38분
            Add("E15", "P05", StopKind.Planned, "09:40:00", null, Oee.PlannedCategory, "정기 점검");
            Add("E14", "P01", StopKind.Unplanned, "09:30:15", "09:35:15", null, null);                // 경계: 정확히 5분
            Add("E13", "P06", StopKind.Unplanned, "09:10:00", "09:17:00", "자재 대기", "부자재 대기");
            Add("E12", "P04", StopKind.Unplanned, "08:50:00", "08:54:00", Oee.AutoCategory, "설비 신호");
            Add("E11", "P03", StopKind.Unplanned, "08:40:00", "08:58:00", "인원 부족", "교대 지연");
            Add("E10", "P02", StopKind.Unplanned, "08:31:10", "08:34:10", Oee.AutoCategory, "설비 신호");
            Add("E09", "P06", StopKind.Unplanned, "08:20:00", "08:45:00", "고장", "금형 파손");
            Add("E08", "P01", StopKind.Unplanned, "08:05:20", "08:17:20", "셋업", "금형 교체");
            Add("E07", "P03", StopKind.Unplanned, "07:50:00", "07:53:00", Oee.AutoCategory, "설비 신호");
            Add("E06", "P05", StopKind.Unplanned, "07:20:00", "07:30:00", "품질 대기", "초물 검사 대기");
            Add("E05", "P04", StopKind.Unplanned, "07:15:30", "07:32:30", "자재 대기", "코일 공급 대기");
            Add("E04", "P02", StopKind.Unplanned, "06:55:00", "07:10:00", "고장", "클러치·브레이크");
            Add("E03", "P01", StopKind.Unplanned, "06:41:00", "06:44:00", Oee.AutoCategory, "설비 신호");
            Add("E02", "P06", StopKind.Unplanned, "06:12:00", "06:15:00", Oee.AutoCategory, "설비 신호");
            Add("E01", "P05", StopKind.Unplanned, "06:05:00", "06:25:00", "셋업", "코일·소재 교체");
        }

        public DateTime Now { get { return Today.AddHours(10).AddMinutes(20).AddSeconds(10); } }
        public string CurrentUser { get { return "이반장"; } }

        public IList<MachineInfo> GetMachines()
        {
            // P06은 성능효율 100% 초과(표준 CT 오류 의심) 사례
            return new List<MachineInfo>
            {
                new MachineInfo("P01", 0.5, 450, 441),
                new MachineInfo("P02", 0.6, 380, 372),
                new MachineInfo("P03", 0.5, 400, 392),
                new MachineInfo("P04", 0.5, 350, 335),
                new MachineInfo("P05", 0.6, 290, 286),
                new MachineInfo("P06", 0.5, 520, 509),
            };
        }

        public IList<StopEvent> GetStopEvents() { return new List<StopEvent>(_events); }

        public void SaveReason(string eventId, string category, string detail, string note)
        {
            int i = _events.FindIndex(e => e.Id == eventId);
            if (i >= 0) _events[i] = _events[i].WithReason(category, detail, note, CurrentUser);
        }

        private void Add(string id, string machine, StopKind kind, string start, string end, string category, string detail)
        {
            string user = category == null ? null : category == Oee.AutoCategory ? "설비 자동"
                : kind == StopKind.Planned ? "생산계획" : CurrentUser;
            _events.Add(new StopEvent(id, machine, kind, Parse(start), end == null ? (DateTime?)null : Parse(end),
                category, detail, null, user));
        }

        private static DateTime Parse(string hhmmss) { return Today + TimeSpan.Parse(hhmmss); }
    }
}
