using System.Collections.Generic;
using APC.UI.Theming;

namespace InjectionMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 기준: 설정 60.0 ℃(경고 64.0 / 알람 65.0 초과), 압력 상한 140.0 bar(경고 126.0), 사이클 목표 25 s(경고 27.0 / 상한 28.0).</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private Recipe _recipe = new Recipe(60.0, 140.0, 25);

        private static readonly Machine[] Machines =
        {
            new Machine("IM-01", "가동", true, true, 60.2, 118.5, 24.8, new[] { 59.8, 60.1, 60.4, 59.9, 60.2, 60.0, 59.7, 60.3, 60.5, 60.1, 59.9, 60.2, 60.4, 60.0, 59.8, 60.1, 60.2 }),
            new Machine("IM-02", "가동", true, true, 64.0, 121.4, 25.6, new[] { 60.5, 60.8, 61.0, 61.4, 61.2, 61.7, 62.0, 62.3, 62.1, 62.6, 62.9, 63.2, 63.0, 63.4, 63.7, 63.6, 64.0 }),
            new Machine("IM-03", "가동", true, true, 59.6, 125.9, 25.3, new[] { 59.5, 59.8, 59.4, 59.7, 60.0, 59.6, 59.3, 59.8, 60.1, 59.7, 59.5, 59.9, 59.6, 59.4, 59.8, 59.7, 59.6 }),
            new Machine("IM-04", "가동", true, true, 66.4, 121.0, 25.1, new[] { 60.9, 61.2, 61.5, 61.8, 62.4, 62.7, 63.1, 63.0, 63.5, 63.9, 64.2, 64.0, 64.6, 65.0, 65.3, 65.9, 66.4 }),
            new Machine("IM-05", "대기", false, false, 0, 0, 0, new double[0]),
            new Machine("IM-06", "정지", false, true, 41.3, 6.4, 24.9, new[] { 59.1, 56.8, 54.7, 52.8, 51.1, 49.6, 48.2, 47.0, 45.9, 44.9, 44.0, 43.2, 42.5, 41.9, 41.5, 41.4, 41.3 }),
        };

        // 추이 17점 = 5분 간격 80분, 마지막 점 14:55
        private static readonly string[] Labels = { "13:35", "13:55", "14:15", "14:35", "14:55" };

        private static readonly AlarmRow[] Alarms =
        {
            new AlarmRow(StatusKind.Alarm, "14:52:10", "IM-04", "금형 온도 상한 초과 66.4 ℃ (상한 65.0)"),
            new AlarmRow(StatusKind.Warn, "14:48:37", "IM-02", "금형 온도 상한 근접 64.0 ℃ (상한 65.0)"),
            new AlarmRow(StatusKind.Run, "14:26:18", "IM-03", "사이클 타임 정상 복귀 25.4 s"),
            new AlarmRow(StatusKind.Warn, "14:20:45", "IM-03", "사이클 타임 상한 근접 28.0 s (상한 28.0)"),
            new AlarmRow(StatusKind.Run, "14:09:20", "IM-01", "사출 압력 정상 복귀 119.8 bar"),
            new AlarmRow(StatusKind.Alarm, "14:05:51", "IM-01", "사출 압력 상한 초과 141.2 bar (상한 140.0)"),
            new AlarmRow(StatusKind.Info, "13:47:09", "IM-05", "레시피 변경 금형 온도 58.0 → 60.0 ℃"),
            new AlarmRow(StatusKind.Stop, "13:34:50", "IM-06", "계획 정지 (금형 교체)"),
            new AlarmRow(StatusKind.Warn, "13:32:24", "IM-02", "사출 압력 상한 근접 126.0 bar (상한 140.0)"),
            new AlarmRow(StatusKind.Stop, "13:15:40", "IM-05", "대기 전환 (자재 교체)"),
            new AlarmRow(StatusKind.Warn, "12:58:13", "IM-04", "금형 온도 하한 근접 56.0 ℃ (하한 55.0)"),
        };

        public IList<Machine> GetMachines() { return Machines; }
        public string[] GetTrendLabels() { return Labels; }
        public IList<AlarmRow> GetAlarms() { return Alarms; }
        public Recipe GetRecipe() { return _recipe; }
        public void SaveRecipe(Recipe recipe) { _recipe = recipe; }
    }
}
