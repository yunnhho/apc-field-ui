using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>
    /// 화면 확인·render용 샘플. T4 91 %(알람) → V5 닫힘·P2 정지, T3 90 %(경계, 경고), T3 온도 55.0 ℃(상한과 같음, OK).
    /// P1 가동 중 → 흡입 밸브 V1 닫기는 막힘.
    /// </summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly DateTime Today = new DateTime(2026, 11, 27);
        private readonly List<TankInfo> _tanks = new List<TankInfo>
        {
            new TankInfo("T1", "T1 원료 탱크", 72, 24.0, false),
            new TankInfo("T2", "T2 원료 탱크", 38, 23.5, false),
            new TankInfo("T3", "T3 배합 탱크", 90, 55.0, true),
            new TankInfo("T4", "T4 저장 탱크", 91, 31.5, false),
        };
        private readonly List<ValveInfo> _valves = new List<ValveInfo>
        {
            new ValveInfo("V1", "T1 → P1", true,  Today.AddHours(9).AddMinutes(12).AddSeconds(30), "EMP-20388", "T3", "P1", true),
            new ValveInfo("V2", "T2 → P1", false, Today.AddHours(10).AddMinutes(5).AddSeconds(52), "EMP-20417", "T3", "P1", true),
            new ValveInfo("V3", "P1 → T3", true,  Today.AddHours(9).AddMinutes(12).AddSeconds(45), "EMP-20388", "T3", "P1", false),
            new ValveInfo("V4", "T3 → P2", true,  Today.AddHours(13).AddMinutes(2).AddSeconds(18), "EMP-20417", "T4", "P2", true),
            new ValveInfo("V5", "P2 → T4", false, Today.AddHours(14).AddMinutes(48).AddSeconds(36), "EMP-20417", "T4", "P2", false),
            new ValveInfo("V6", "T4 → 출하", true, Today.AddHours(13).AddMinutes(40).AddSeconds(27), "EMP-20352", null, null, false),
        };
        private readonly List<PumpInfo> _pumps = new List<PumpInfo> { new PumpInfo("P1", true), new PumpInfo("P2", false) };
        private readonly List<AlarmInfo> _alarms = new List<AlarmInfo>
        {
            A(StatusKind.Alarm, 14, 52, 10, "T4", "레벨 초과 91 % (기준 90 % 이하)"),
            A(StatusKind.Info, 14, 48, 36, "V5", "닫힘 · EMP-20417"),
            A(StatusKind.Info, 14, 48, 21, "P2", "정지"),
            A(StatusKind.Warn, 14, 40, 12, "T4", "레벨 경고 86 % (경고 85 % 이상)"),
            A(StatusKind.Warn, 14, 31, 5, "T3", "레벨 경고 90 % (경고 85 % 이상)"),
            A(StatusKind.Info, 13, 40, 27, "V6", "열림 · EMP-20352"),
            A(StatusKind.Info, 13, 2, 18, "V4", "열림 · EMP-20417"),
            A(StatusKind.Run, 11, 37, 9, "T3", "온도 정상 복귀 54.6 ℃"),
            A(StatusKind.Alarm, 11, 20, 44, "T3", "온도 상한 초과 55.4 ℃ (상한 55.0)"),
            A(StatusKind.Info, 10, 5, 52, "V2", "닫힘 · EMP-20417"),
            A(StatusKind.Warn, 9, 48, 31, "T2", "레벨 하한 경고 10 % (경고 10 % 이하)"),
            A(StatusKind.Info, 9, 12, 58, "P1", "가동"),
            A(StatusKind.Info, 9, 12, 45, "V3", "열림 · EMP-20388"),
            A(StatusKind.Info, 9, 12, 30, "V1", "열림 · EMP-20388"),
            A(StatusKind.Info, 8, 57, 3, "V6", "닫힘 · EMP-20352"),
            A(StatusKind.Warn, 8, 31, 40, "T1", "레벨 경고 85 % (경고 85 % 이상)"),
            A(StatusKind.Run, 8, 14, 22, "T4", "레벨 정상 복귀 62 %"),
            A(StatusKind.Info, 7, 46, 9, "P2", "정지"),
            A(StatusKind.Info, 7, 45, 51, "V5", "닫힘 · EMP-20352"),
        };

        private static AlarmInfo A(StatusKind k, int h, int m, int s, string target, string text)
        {
            return new AlarmInfo(k, Today.AddHours(h).AddMinutes(m).AddSeconds(s), target, text);
        }

        public IList<TankInfo> GetTanks() { return _tanks.AsReadOnly(); }
        public IList<ValveInfo> GetValves() { return _valves.AsReadOnly(); }
        public IList<PumpInfo> GetPumps() { return _pumps.AsReadOnly(); }
        public IList<AlarmInfo> GetAlarms() { return _alarms.AsReadOnly(); }

        public void OperateValve(string valveId, bool open, string operatorId)
        {
            int i = _valves.FindIndex(v => v.Id == valveId);
            if (i < 0) throw new ArgumentException("없는 밸브: " + valveId);
            if (ProcessRules.BlockReason(_valves[i], open, _tanks, _pumps) != null)
                throw new InvalidOperationException(valveId + " 조작이 인터락으로 막혀 있습니다.");
            var now = DateTime.Now;
            _valves[i] = _valves[i].WithState(open, now, operatorId);
            _alarms.Insert(0, new AlarmInfo(StatusKind.Info, now, valveId, (open ? "열림" : "닫힘") + " · " + operatorId));
        }
    }
}
