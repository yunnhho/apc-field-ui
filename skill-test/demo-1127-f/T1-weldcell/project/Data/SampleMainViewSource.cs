using System;
using APC.UI.Theming;

namespace SpotWeldMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 셀·카드·이력·추이 값이 서로 맞는다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const int TrendPoints = 61;   // 1분 간격 최근 1시간

        // 이름, 상태, 오늘 타점, 팁 사용 타점, 마지막 용접 전류(kA)
        // WR-02는 전류 하한(9.5)과 같고 팁 2,700(경고 시작)과 같다. WR-03은 팁 3,000(알람).
        private static readonly WeldCell[] Cells =
        {
            new WeldCell("WR-01", CellState.Running, 1284, 1420, 10.2),
            new WeldCell("WR-02", CellState.Running, 1376, 2700, 9.5),
            new WeldCell("WR-03", CellState.Alarm, 612, 3000, 10.8),
            new WeldCell("WR-04", CellState.Waiting, 845, 1950, 10.4),
            new WeldCell("WR-05", CellState.Stopped, 1120, 2210, 11.5),
            new WeldCell("WR-06", CellState.Running, 1198, 880, 10.9),
        };

        private static readonly AlarmRecord[] History =
        {
            new AlarmRecord(StatusKind.Alarm, "14:52:10", "WR-03", "팁 사용 3,000 타점 (한계 3,000)", "팁 교체 대기"),
            new AlarmRecord(StatusKind.Warn, "14:31:48", "WR-02", "팁 드레싱 대기 2,700 타점", "드레싱 예정"),
            new AlarmRecord(StatusKind.Stop, "14:18:40", "WR-05", "비상 정지 입력", "작업자 확인 중"),
            new AlarmRecord(StatusKind.Alarm, "14:18:05", "WR-05", "용접 전류 이탈 11.8 kA (기준 9.5~11.5)", "해당 차체 재용접"),
            new AlarmRecord(StatusKind.Run, "13:57:22", "WR-04", "가동 재개", "자동 복귀"),
            new AlarmRecord(StatusKind.Stop, "13:44:09", "WR-04", "차체 도착 대기", "없음"),
            new AlarmRecord(StatusKind.Alarm, "13:26:51", "WR-01", "용접 전류 이탈 9.2 kA (기준 9.5~11.5)", "전극 접촉 확인"),
            new AlarmRecord(StatusKind.Info, "13:02:15", "WR-06", "용접 조건표 변경 No.3 → No.4", "반장 승인"),
            new AlarmRecord(StatusKind.Run, "12:48:33", "WR-06", "냉각수 유량 정상 복귀", "없음"),
            new AlarmRecord(StatusKind.Alarm, "12:21:07", "WR-06", "냉각수 유량 부족", "밸브 점검"),
                new AlarmRecord(StatusKind.Alarm, "11:39:56", "WR-02", "용접 전류 이탈 11.7 kA (기준 9.5~11.5)", "트랜스 점검"),
            new AlarmRecord(StatusKind.Info, "10:55:02", "WR-05", "팁 교체 등록 완료 (EMP-20417)", "없음"),
            new AlarmRecord(StatusKind.Alarm, "10:12:44", "WR-01", "용접 전류 이탈 11.6 kA (기준 9.5~11.5)", "전극 접촉 확인"),
            new AlarmRecord(StatusKind.Stop, "09:36:18", "WR-04", "계획 정지", "없음"),
            new AlarmRecord(StatusKind.Alarm, "08:47:29", "WR-03", "로봇 통신 끊김", "케이블 재체결"),
        };

        public WeldCell[] GetCells() { return Cells; }

        public LineValues GetLineValues()
        {
            // 가동률은 소스가 주는 값(가동 시간 기준). 드레싱 대기 = 팁 2,700 이상 3,000 미만인 셀 수.
            int waiting = 0;
            foreach (var c in Cells) if (WeldRules.TipStatus(c.TipShots) == StatusKind.Warn) waiting++;
            return new LineValues(82.4, 7, waiting);
        }

        public AlarmRecord[] GetAlarmHistory() { return History; }

        public double[] GetCurrentTrend(string cellName)
        {
            double last = 10.5;
            int seed = 0;
            foreach (var c in Cells) if (c.Name == cellName) { if (c.LastCurrentKa.HasValue) last = c.LastCurrentKa.Value; seed = c.Name[4] - '0'; }
            var v = new double[TrendPoints];
            for (int i = 0; i < TrendPoints; i++)
            {
                double x = 10.5 + 0.55 * Math.Sin(i / 4.0 + seed) + 0.25 * Math.Sin(i * 1.7 + seed * 2);
                v[i] = Math.Round(Math.Max(WeldRules.CurrentMinKa + 0.1, Math.Min(WeldRules.CurrentMaxKa - 0.1, x)), 1);
            }
            v[TrendPoints - 1] = last;   // 마지막 점 = 셀의 마지막 전류
            return v;
        }

        public bool RegisterTipChange(string cellName, string badge)
        {
            foreach (var c in Cells) if (c.Name == cellName) return c.State != CellState.Running;
            return false;
        }
    }
}
