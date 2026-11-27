using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>화면 확인·render용 샘플. 서로 맞는 값: T3 92 %(알람) → V1~V3 열기 막힘, P2 가동 → V4 닫기 막힘.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly DateTime Today = new DateTime(2026, 11, 27);
        private readonly List<ValveInfo> _valves = new List<ValveInfo>
        {
            new ValveInfo("V1", "T1 → P1", "T3", "P1", true, false, At(13, 20, 44), "EMP-10231"),
            new ValveInfo("V2", "T2 → P1", "T3", "P1", true, false, At(13, 20, 51), "EMP-10231"),
            new ValveInfo("V3", "P1 → T3", "T3", "P1", false, false, At(14, 3, 5), "EMP-10377"),
            new ValveInfo("V4", "T3 → P2", "T4", "P2", true, true, At(14, 6, 12), "EMP-10377"),
            new ValveInfo("V5", "P2 → T4", "T4", "P2", false, true, At(13, 18, 30), "EMP-10408"),
            new ValveInfo("V6", "T4 → 출하", null, null, false, false, At(12, 47, 9), "EMP-10408"),
        };

        private static DateTime At(int h, int m, int s) { return Today.AddHours(h).AddMinutes(m).AddSeconds(s); }

        public IList<TankInfo> GetTanks()
        {
            return new List<TankInfo>
            {
                new TankInfo("T1", "원료", 10, 21.5, false),   // 하한 경고 경계(10 % 이하)
                new TankInfo("T2", "원료", 48, 22.0, false),
                new TankInfo("T3", "배합", 92, 55.0, true),    // 알람(90 % 초과), 온도는 상한 경계(55.0 OK)
                new TankInfo("T4", "저장", 85, 38.4, false),   // 경고 경계(85 % 이상)
            };
        }

        public IList<ValveInfo> GetValves() { return new List<ValveInfo>(_valves); }

        public IList<PumpInfo> GetPumps()
        {
            return new List<PumpInfo> { new PumpInfo("P1", false), new PumpInfo("P2", true) };
        }

        public IList<AlarmInfo> GetAlarms()
        {
            return new List<AlarmInfo>
            {
                new AlarmInfo(StatusKind.Warn, At(14, 11, 47), "T4", "레벨 경고 85 % (경고 85 % 이상)"),
                new AlarmInfo(StatusKind.Warn, At(14, 9, 55), "T1", "레벨 하한 경고 10 % (10 % 이하)"),
                new AlarmInfo(StatusKind.Run, At(14, 6, 40), "P2", "가동 시작"),
                new AlarmInfo(StatusKind.Info, At(14, 6, 12), "V4", "열림 · EMP-10377"),
                new AlarmInfo(StatusKind.Info, At(14, 3, 5), "V3", "닫힘 · EMP-10377"),
                new AlarmInfo(StatusKind.Stop, At(14, 1, 50), "P1", "정지"),
                new AlarmInfo(StatusKind.Alarm, At(14, 1, 18), "T3", "레벨 알람 92 % (알람 기준 90 % 초과)"),
                new AlarmInfo(StatusKind.Warn, At(13, 52, 30), "T3", "레벨 경고 86 % (경고 85 % 이상)"),
                new AlarmInfo(StatusKind.Info, At(13, 20, 51), "V2", "닫힘 · EMP-10231"),
                new AlarmInfo(StatusKind.Info, At(13, 20, 44), "V1", "닫힘 · EMP-10231"),
                new AlarmInfo(StatusKind.Run, At(11, 38, 41), "T3", "온도 정상 복귀 54.7 ℃"),
                new AlarmInfo(StatusKind.Alarm, At(11, 32, 15), "T3", "온도 상한 초과 55.6 ℃ (상한 55.0)"),
                new AlarmInfo(StatusKind.Info, At(10, 15, 3), "V5", "열림 · EMP-10408"),
                new AlarmInfo(StatusKind.Warn, At(9, 44, 26), "T2", "레벨 하한 경고 9 % (10 % 이하)"),
                new AlarmInfo(StatusKind.Run, At(9, 12, 51), "P1", "가동 시작"),
                new AlarmInfo(StatusKind.Info, At(8, 58, 4), "V6", "닫힘 · EMP-10408"),
                new AlarmInfo(StatusKind.Warn, At(8, 30, 37), "T4", "레벨 경고 87 % (경고 85 % 이상)"),
            };
        }

        public void OperateValve(string valveId, bool open, string operatorId)
        {
            for (int i = 0; i < _valves.Count; i++)
                if (_valves[i].Id == valveId) _valves[i] = _valves[i].WithOperation(open, DateTime.Now, operatorId);
        }
    }
}
