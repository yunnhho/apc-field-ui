using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. T3 레벨 알람(91 %), P1 정지, P2 가동 상태.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private readonly List<TankInfo> _tanks = new List<TankInfo>();
        private readonly List<PumpInfo> _pumps = new List<PumpInfo>();
        private readonly List<ValveInfo> _valves = new List<ValveInfo>();
        private readonly List<AlarmEntry> _alarms = new List<AlarmEntry>();

        private static DateTime At(int h, int m, int s) { return DateTime.Today.AddHours(h).AddMinutes(m).AddSeconds(s); }

        public SampleMainViewSource()
        {
            _tanks.Add(new TankInfo("T1", "원료 탱크", 85, 23.4, false));    // 경고 시작값과 같음
            _tanks.Add(new TankInfo("T2", "원료 탱크", 62, 21.8, false));
            _tanks.Add(new TankInfo("T3", "배합 탱크", 91, 55.0, true));     // 알람 / 온도는 상한과 같아 OK
            _tanks.Add(new TankInfo("T4", "저장 탱크", 38, 20.4, false));

            _pumps.Add(new PumpInfo("P1", false));
            _pumps.Add(new PumpInfo("P2", true));

            _valves.Add(new ValveInfo("V1", "T1 출구", true, At(8, 30, 12), "EMP-10432", "T3", "P1", true));
            _valves.Add(new ValveInfo("V2", "T2 출구", false, At(10, 55, 12), "EMP-10587", "T3", "P1", true));
            _valves.Add(new ValveInfo("V3", "P1 → T3", true, At(8, 31, 40), "EMP-10432", "T3", "P1", false));
            _valves.Add(new ValveInfo("V4", "T3 출구", true, At(8, 40, 5), "EMP-10315", "T4", "P2", true));
            _valves.Add(new ValveInfo("V5", "P2 → T4", true, At(8, 40, 31), "EMP-10315", "T4", "P2", false));
            _valves.Add(new ValveInfo("V6", "T4 출하", false, null, "", "", "", false));

            _alarms.Add(new AlarmEntry(At(14, 32, 8), StatusKind.Alarm, "T3", "레벨 알람 91 % (알람 기준 90 % 초과)"));
            _alarms.Add(new AlarmEntry(At(14, 31, 20), StatusKind.Stop, "P1", "정지 (T3 레벨 상승)"));
            _alarms.Add(new AlarmEntry(At(14, 12, 45), StatusKind.Warn, "T3", "레벨 경고 86 % (경고 기준 85 % 이상)"));
            _alarms.Add(new AlarmEntry(At(13, 47, 3), StatusKind.Warn, "T1", "레벨 경고 85 % (경고 기준 85 % 이상)"));
            _alarms.Add(new AlarmEntry(At(13, 09, 34), StatusKind.Run, "T2", "레벨 경고 해제 (84 %)"));
            _alarms.Add(new AlarmEntry(At(12, 48, 15), StatusKind.Warn, "T2", "레벨 경고 88 % (경고 기준 85 % 이상)"));
            _alarms.Add(new AlarmEntry(At(12, 41, 55), StatusKind.Run, "T3", "레벨 경고 해제 (83 %)"));
            _alarms.Add(new AlarmEntry(At(12, 30, 18), StatusKind.Warn, "T3", "레벨 경고 87 % (경고 기준 85 % 이상)"));
            _alarms.Add(new AlarmEntry(At(11, 24, 2), StatusKind.Run, "T3", "배합 온도 정상 복귀 45.6 ℃"));
            _alarms.Add(new AlarmEntry(At(11, 20, 36), StatusKind.Alarm, "T3", "배합 온도 하한 이탈 44.8 ℃ (하한 45.0)"));
            _alarms.Add(new AlarmEntry(At(10, 55, 12), StatusKind.Info, "V2", "닫힘 조작 · EMP-10587"));
            _alarms.Add(new AlarmEntry(At(9, 30, 51), StatusKind.Warn, "T4", "레벨 하한 경고 10 % (하한 기준 10 % 이하)"));
            _alarms.Add(new AlarmEntry(At(8, 41, 19), StatusKind.Run, "P2", "가동 시작"));
            _alarms.Add(new AlarmEntry(At(8, 31, 52), StatusKind.Run, "P1", "가동 시작"));
        }

        public IList<TankInfo> GetTanks() { return _tanks; }
        public IList<PumpInfo> GetPumps() { return _pumps; }
        public IList<ValveInfo> GetValves() { return _valves; }
        public IList<AlarmEntry> GetAlarms() { return _alarms; }

        public void OperateValve(string valveId, bool open, string operatorId, DateTime at)
        {
            for (int i = 0; i < _valves.Count; i++)
            {
                if (_valves[i].Id != valveId) continue;
                _valves[i] = _valves[i].WithOperation(open, at, operatorId);
                _alarms.Insert(0, new AlarmEntry(at, StatusKind.Info, valveId,
                    (open ? "열림" : "닫힘") + " 조작 · " + operatorId));
                return;
            }
        }
    }
}
