using System;
using System.Collections.Generic;

namespace PressPmChecklist.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 1~6단계 끝, 7단계 진행 중(3·5단계 NG, 4단계는 기준값 180 ms 그대로 OK).</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private readonly List<PmStep> _steps = new List<PmStep>
        {
            new PmStep(1, "전원 차단·LOTO 잠금 확인", "주전원 차단, 잠금장치와 표지 부착을 확인", StepKind.Safety, "", null, null, 0),
            new PmStep(2, "유압 작동유 레벨", "유면계에서 레벨이 상·하한선 사이인지 육안 확인", StepKind.Visual, "", null, null, 0),
            new PmStep(3, "유압 압력 측정", "정상 가동 압력 게이지 지시값을 읽어 입력", StepKind.Measure, "MPa", 18.0m, 21.0m, 1),
            new PmStep(4, "클러치·브레이크 정지 시간 측정", "정지 시간 측정기 표시값을 읽어 입력", StepKind.Measure, "ms", null, 180m, 0),
            new PmStep(5, "슬라이드 평행도 측정", "다이얼 게이지로 슬라이드 좌우 높이 차이를 측정", StepKind.Measure, "mm", null, 0.05m, 2),
            new PmStep(6, "윤활유 공급 점검", "급유 라인 토출과 급유 지점 도포 상태를 육안 확인", StepKind.Visual, "", null, null, 0),
            new PmStep(7, "안전 광커튼 차광 시험", "시험봉으로 차광했을 때 슬라이드가 정지하는지 확인", StepKind.Visual, "", null, null, 0),
            new PmStep(8, "비상정지 버튼 작동", "비상정지 버튼을 눌러 즉시 정지하는지 확인", StepKind.Visual, "", null, null, 0),
        };

        public IList<PmStep> GetSteps() { return _steps; }

        public PmSession GetCurrentSession()
        {
            DateTime start = DateTime.Now.AddMinutes(-7).AddSeconds(-35);
            var results = new List<StepResult>
            {
                new StepResult(_steps[0], true, null, start.AddSeconds(58)),
                new StepResult(_steps[1], true, null, start.AddSeconds(131)),
                new StepResult(_steps[2], false, 16.8m, start.AddSeconds(262)),
                new StepResult(_steps[3], true, 180m, start.AddSeconds(371)),
                new StepResult(_steps[4], false, 0.06m, start.AddSeconds(498)),
                new StepResult(_steps[5], true, null, start.AddSeconds(560)),
            };
            return new PmSession("EQ-PRS-03", "EMP-10482", start, results);
        }

        /// <summary>완료 dialog render용: 8단계 전부, NG 2건(3단계 16.8 MPa, 5단계 0.06 mm), 4단계는 한계값과 같은 180 ms(OK).</summary>
        public static IList<StepResult> CompletedResults(IList<PmStep> steps)
        {
            DateTime start = DateTime.Now.AddMinutes(-14);
            decimal?[] values = { null, null, 16.8m, 180m, 0.06m, null, null, null };
            bool[] ok = { true, true, false, true, false, true, true, true };
            int[] seconds = { 58, 131, 262, 371, 498, 560, 702, 801 };
            var list = new List<StepResult>();
            for (int i = 0; i < steps.Count; i++) list.Add(new StepResult(steps[i], ok[i], values[i], start.AddSeconds(seconds[i])));
            return list;
        }

        public bool IsKnownEquipment(string code) { return code != null && code.StartsWith("EQ-PRS-", StringComparison.Ordinal) && code.Length == 9; }

        public bool IsKnownEmployee(string code) { return code != null && code.StartsWith("EMP-", StringComparison.Ordinal) && code.Length == 9; }

        public void SaveStepResult(string equipment, string employee, StepResult result) { }

        public string RegisterMaintenanceRequest(string equipment, IList<StepResult> ngResults)
        {
            return "MR-" + DateTime.Now.ToString("yyyyMMdd") + "-01";
        }

        public void CompleteSession(string equipment, string employee, TimeSpan elapsed) { }
    }
}
