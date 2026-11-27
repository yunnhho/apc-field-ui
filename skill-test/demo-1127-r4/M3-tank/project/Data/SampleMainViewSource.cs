using System;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 밸브 조작은 메모리 값만 바꾼다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly DateTime Today = new DateTime(2026, 11, 27);

        private readonly TankInfo[] _tanks =
        {
            new TankInfo("T1", "원료 탱크", 62, 24.5),
            new TankInfo("T2", "원료 탱크", 8, 23.8),
            new TankInfo("T3", "배합 탱크", 87, 55.0),   // 온도 상한과 같음 = OK
            new TankInfo("T4", "저장 탱크", 92, 21.3),   // 알람 기준 초과
        };

        private readonly ValveInfo[] _valves =
        {
            new ValveInfo("V1", "T1 출구 · P1 흡입", true, Today.AddHours(8).AddMinutes(12).AddSeconds(40), "EMP-10231"),
            new ValveInfo("V2", "T2 출구 · P1 흡입", false, Today.AddHours(13).AddMinutes(20).AddSeconds(15), "EMP-10231"),
            new ValveInfo("V3", "P1 토출 · T3 입구", true, Today.AddHours(8).AddMinutes(12).AddSeconds(58), "EMP-10231"),
            new ValveInfo("V4", "T3 출구 · P2 흡입", true, Today.AddHours(9).AddMinutes(41).AddSeconds(7), "EMP-10428"),
            new ValveInfo("V5", "P2 토출 · T4 입구", false, Today.AddHours(14).AddMinutes(52).AddSeconds(31), "EMP-10587"),
            new ValveInfo("V6", "T4 출구 · 출하", false, Today.AddHours(14).AddMinutes(12).AddSeconds(5), "EMP-10428"),
        };

        private readonly PumpInfo[] _pumps = { new PumpInfo("P1", true), new PumpInfo("P2", false) };

        private AlarmEntry[] _alarms = BuildAlarms();

        public TankInfo[] GetTanks() { return _tanks; }
        public ValveInfo[] GetValves() { return _valves; }
        public PumpInfo[] GetPumps() { return _pumps; }
        public AlarmEntry[] GetTodayAlarms() { return _alarms; }

        public void OperateValve(string valveId, bool open, string operatorBadge)
        {
            for (int i = 0; i < _valves.Length; i++)
            {
                if (_valves[i].Id != valveId) continue;
                var now = DateTime.Now;
                _valves[i] = new ValveInfo(valveId, _valves[i].Position, open, now, operatorBadge);
                var list = new System.Collections.Generic.List<AlarmEntry>(_alarms);
                list.Insert(0, new AlarmEntry(StatusKind.Info, now, valveId, valveId + (open ? " 열기" : " 닫기") + " · " + operatorBadge));
                _alarms = list.ToArray();
                return;
            }
        }

        private static AlarmEntry A(StatusKind k, string hms, string target, string text)
        {
            return new AlarmEntry(k, Today + TimeSpan.Parse(hms), target, text);
        }

        private static AlarmEntry[] BuildAlarms()
        {
            return new[]
            {
                A(StatusKind.Info, "14:52:31", "V5", "V5 닫기 · EMP-10587"),
                A(StatusKind.Stop, "14:52:24", "P2", "P2 정지"),
                A(StatusKind.Alarm, "14:52:10", "T4", "T4 레벨 알람 92 % (기준 90 % 초과)"),
                A(StatusKind.Warn, "14:44:03", "T4", "T4 레벨 경고 90 %"),
                A(StatusKind.Warn, "13:58:42", "T3", "T3 레벨 경고 87 %"),
                A(StatusKind.Run, "10:19:30", "T3", "T3 온도 정상 복귀 45.0 ℃"),
            };
        }
    }
}
