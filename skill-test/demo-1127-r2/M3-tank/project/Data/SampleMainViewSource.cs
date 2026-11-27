using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. T3 레벨 알람 + P2 가동 중(T3 → T4 이송).</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly DateTime Today = new DateTime(2026, 11, 27);
        private readonly List<ValveInfo> _valves;
        private readonly List<AlarmRecord> _alarms;

        public SampleMainViewSource()
        {
            _valves = new List<ValveInfo>
            {
                new ValveInfo("V1", "T1", "P1", "T3", "P1", true, false, Today.AddHours(12).AddMinutes(58).AddSeconds(20), "EMP-10211"),
                new ValveInfo("V2", "T2", "P1", "T3", "P1", true, false, Today.AddHours(11).AddMinutes(30).AddSeconds(41), "EMP-10377"),
                new ValveInfo("V3", "P1", "T3", "T3", "P1", false, false, Today.AddHours(14).AddMinutes(15).AddSeconds(36), "EMP-10211"),
                new ValveInfo("V4", "T3", "P2", "T4", "P2", true, true, Today.AddHours(14).AddMinutes(21).AddSeconds(45), "EMP-10432"),
                new ValveInfo("V5", "P2", "T4", "T4", "P2", false, true, Today.AddHours(14).AddMinutes(21).AddSeconds(52), "EMP-10432"),
                new ValveInfo("V6", "T4", "출하", null, null, false, false, Today.AddHours(9).AddMinutes(46).AddSeconds(3), "EMP-10377"),
            };
            _alarms = new List<AlarmRecord>
            {
                new AlarmRecord(StatusKind.Warn, Today.AddHours(14).AddMinutes(40).AddSeconds(12), "T4", "레벨 경고 86 % (85 % 이상)"),
                new AlarmRecord(StatusKind.Run, Today.AddHours(14).AddMinutes(22).AddSeconds(10), "P2", "펌프 가동 시작"),
                new AlarmRecord(StatusKind.Info, Today.AddHours(14).AddMinutes(21).AddSeconds(52), "V5", "밸브 열림 · EMP-10432"),
                new AlarmRecord(StatusKind.Info, Today.AddHours(14).AddMinutes(21).AddSeconds(45), "V4", "밸브 열림 · EMP-10432"),
                new AlarmRecord(StatusKind.Alarm, Today.AddHours(14).AddMinutes(18).AddSeconds(3), "T3", "레벨 알람 93 % (90 % 초과)"),
                new AlarmRecord(StatusKind.Warn, Today.AddHours(8).AddMinutes(12).AddSeconds(47), "T2", "레벨 하한 경고 8 % (10 % 이하)"),
            };
        }

        public IList<TankInfo> GetTanks()
        {
            return new List<TankInfo>
            {
                new TankInfo("T1", "원료 탱크", 72, 24.6m, false),
                new TankInfo("T2", "원료 탱크", 8, 23.8m, false),
                new TankInfo("T3", "배합 탱크", 92, 55.0m, true),   // 온도 상한 경계값 = OK
                new TankInfo("T4", "저장 탱크", 86, 38.5m, false),
            };
        }

        public IList<PumpInfo> GetPumps()
        {
            return new List<PumpInfo> { new PumpInfo("P1", false), new PumpInfo("P2", true) };
        }

        public IList<ValveInfo> GetValves() { return new List<ValveInfo>(_valves); }

        public IList<AlarmRecord> GetTodayAlarms() { return new List<AlarmRecord>(_alarms); }

        public void OperateValve(string valveId, bool open, string badge, DateTime time)
        {
            int i = _valves.FindIndex(v => v.Id == valveId);
            if (i < 0) throw new ArgumentException("알 수 없는 밸브: " + valveId);
            _valves[i] = _valves[i].Operated(open, time, badge);
            _alarms.Insert(0, new AlarmRecord(StatusKind.Info, time, valveId, (open ? "밸브 열림 · " : "밸브 닫힘 · ") + badge));
        }
    }
}
