using System;
using System.Collections.Generic;
using System.Linq;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>화면 확인·render용 샘플. 레벨 T4 92 %(알람)라 V5 열기가 막히고, P1 가동 중이라 V1 닫기가 막힌다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly DateTime Today = new DateTime(2026, 11, 27);
        private readonly List<ValveInfo> _valves;

        public SampleMainViewSource()
        {
            _valves = new List<ValveInfo>
            {
                new ValveInfo("V1", "T1 출구", "T3", "P1", true, Today.AddHours(8).AddMinutes(42).AddSeconds(15), "EMP-10482"),
                new ValveInfo("V2", "T2 출구", "T3", "P1", false, Today.AddHours(8).AddMinutes(51).AddSeconds(3), "EMP-10317"),
                new ValveInfo("V3", "P1 토출 · T3 입구", "T3", null, true, Today.AddHours(8).AddMinutes(42).AddSeconds(40), "EMP-10482"),
                new ValveInfo("V4", "T3 출구", "T4", "P2", true, Today.AddHours(10).AddMinutes(5).AddSeconds(52), "EMP-10655"),
                new ValveInfo("V5", "P2 토출 · T4 입구", "T4", null, false, Today.AddHours(14).AddMinutes(31).AddSeconds(46), "EMP-10655"),
                new ValveInfo("V6", "T4 출구 · 출하", null, null, true, Today.AddHours(13).AddMinutes(18).AddSeconds(9), "EMP-10317"),
            };
        }

        public ProcessSnapshot GetSnapshot()
        {
            var tanks = new[]
            {
                new TankInfo("T1", "T1 원료", 8, 22.5),
                new TankInfo("T2", "T2 원료", 62, 23.1),
                new TankInfo("T3", "T3 배합", 86, 55.0),   // 상한과 같은 55.0 ℃ = OK
                new TankInfo("T4", "T4 저장", 92, 38.4),
            };
            var pumps = new[] { new PumpInfo("P1", true), new PumpInfo("P2", false) };
            return new ProcessSnapshot(tanks, _valves.ToArray(), pumps);
        }

        public IList<AlarmEntry> GetTodayAlarms()
        {
            return new List<AlarmEntry>
            {
                new AlarmEntry(Today.AddHours(14).AddMinutes(36).AddSeconds(50), StatusKind.Info, "V5", "열기 시도 차단 · T4 레벨 92 %"),
                new AlarmEntry(Today.AddHours(14).AddMinutes(32).AddSeconds(8), StatusKind.Alarm, "T4", "레벨 상한 초과 92 % (상한 90)"),
                new AlarmEntry(Today.AddHours(14).AddMinutes(5).AddSeconds(41), StatusKind.Warn, "T3", "레벨 상한 근접 86 % (경고 85 이상)"),
                new AlarmEntry(Today.AddHours(13).AddMinutes(12).AddSeconds(27), StatusKind.Warn, "T1", "레벨 하한 경고 8 % (하한 10)"),
                new AlarmEntry(Today.AddHours(11).AddMinutes(26).AddSeconds(48), StatusKind.Run, "T3", "온도 정상 복귀 54.6 ℃"),
                new AlarmEntry(Today.AddHours(11).AddMinutes(20).AddSeconds(15), StatusKind.Alarm, "T3", "온도 상한 초과 55.4 ℃ (상한 55.0)"),
            };
        }

        public void OperateValve(string valveId, bool open, string operatorId)
        {
            int i = _valves.FindIndex(v => v.Id == valveId);
            ValveInfo old = _valves[i];
            _valves[i] = new ValveInfo(old.Id, old.Position, old.DownstreamTankId, old.SuctionPumpId, open, DateTime.Now, operatorId);
        }
    }
}
