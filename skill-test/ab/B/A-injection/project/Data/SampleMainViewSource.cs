using System.Collections.Generic;
using APC.UI.Theming;

namespace InjectionMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 값·기준은 Judge, 알람 이력, 추이와 서로 맞춘다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private RecipeInfo _recipe = new RecipeInfo(60.0, 150.0, 30);

        public IList<MachineInfo> GetMachines()
        {
            return new[]
            {
                new MachineInfo("IM-01", "가동", null, 60.4, 128.5, 29.8, new[] { 59.8, 60.1, 60.3, 60.0, 59.7, 59.9, 60.2, 60.5, 60.4, 60.1, 59.8, 60.0, 60.3, 60.6, 60.5, 60.2, 59.9, 60.1, 60.3, 60.4 }),
                // 온도 63.0 = 경고 시작값과 같은 값 → 경고
                new MachineInfo("IM-02", "가동", null, 63.0, 131.2, 30.4, new[] { 61.2, 61.5, 61.9, 62.0, 62.2, 62.1, 62.4, 62.5, 62.3, 62.6, 62.8, 62.7, 62.9, 63.1, 62.9, 63.0, 62.8, 63.1, 62.9, 63.0 }),
                new MachineInfo("IM-03", "가동", null, 59.6, 131.8, 30.2, new[] { 60.1, 59.9, 59.7, 59.8, 60.0, 60.2, 60.1, 59.9, 59.6, 59.5, 59.7, 59.9, 60.0, 59.8, 59.6, 59.7, 59.9, 59.8, 59.7, 59.6 }),
                // 압력 152.3 > 상한 150.0 → 알람
                new MachineInfo("IM-04", "가동", null, 61.0, 152.3, 31.2, new[] { 60.2, 60.4, 60.6, 60.5, 60.3, 60.1, 60.4, 60.7, 60.9, 60.8, 60.6, 60.5, 60.7, 60.9, 61.1, 61.0, 60.8, 60.9, 61.1, 61.0 }),
                // 정지: 읽은 온도는 보이지만 판정하지 않는다
                new MachineInfo("IM-05", "정지", "계획 정지", 41.2, null, null, new[] { 58.0, 57.1, 56.2, 55.3, 54.5, 53.6, 52.7, 51.8, 51.0, 50.1, 49.2, 48.3, 47.5, 46.6, 45.7, 44.8, 44.0, 43.1, 42.2, 41.2 }),
                // 대기: 통신 전, 값 없음
                new MachineInfo("IM-06", "대기", "금형 교체", null, null, null, new double[0]),
            };
        }

        public string[] GetTrendLabels() { return new[] { "14:14", "14:33", "14:52" }; }

        public IList<AlarmRow> GetAlarmHistory()
        {
            return new[]
            {
                new AlarmRow(StatusKind.Alarm, "알람", "14:52:10", "IM-04", "사출 압력 상한 초과 152.3 bar (상한 150.0)"),
                new AlarmRow(StatusKind.Warn, "경고", "14:41:37", "IM-02", "금형 온도 상한 근접 63.0 ℃ (상한 65.0)"),
                new AlarmRow(StatusKind.Run, "복귀", "14:36:48", "IM-03", "사출 압력 정상 복귀 131.8 bar"),
                new AlarmRow(StatusKind.Warn, "경고", "14:30:12", "IM-03", "사출 압력 상한 근접 140.0 bar (상한 150.0)"),
                new AlarmRow(StatusKind.Run, "복귀", "14:09:31", "IM-01", "금형 온도 정상 복귀 60.8 ℃"),
                new AlarmRow(StatusKind.Warn, "경고", "14:05:22", "IM-01", "금형 온도 상한 근접 65.0 ℃ (상한 65.0)"),
                new AlarmRow(StatusKind.Stop, "정지", "13:58:12", "IM-05", "계획 정지"),
                new AlarmRow(StatusKind.Info, "변경", "13:40:05", "IM-01", "레시피 변경 금형 온도 설정 59.0 → 60.0 ℃"),
                new AlarmRow(StatusKind.Stop, "대기", "13:22:19", "IM-06", "대기 전환 (금형 교체)"),
                new AlarmRow(StatusKind.Run, "복귀", "13:14:02", "IM-04", "사이클 타임 정상 복귀 30.2 s"),
                new AlarmRow(StatusKind.Alarm, "알람", "13:10:44", "IM-04", "사이클 타임 상한 초과 35.4 s (상한 35.0)"),
            };
        }

        public RecipeInfo GetRecipe() { return _recipe; }

        public void SaveRecipe(RecipeInfo recipe) { _recipe = recipe; }
    }
}
