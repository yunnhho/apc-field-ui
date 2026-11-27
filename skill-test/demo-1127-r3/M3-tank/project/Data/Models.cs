using System;
using APC.UI.Theming;

namespace TankFlow.Data
{
    internal sealed class TankInfo
    {
        public TankInfo(string id, string name, int level, double temp) { Id = id; Name = name; Level = level; Temp = temp; }
        public string Id { get; private set; }
        public string Name { get; private set; }
        public int Level { get; private set; }     // %
        public double Temp { get; private set; }   // ℃
    }

    internal sealed class ValveInfo
    {
        public ValveInfo(string id, string position, string downstreamTankId, string suctionPumpId, bool isOpen, DateTime lastTime, string operatorId)
        {
            Id = id; Position = position; DownstreamTankId = downstreamTankId; SuctionPumpId = suctionPumpId;
            IsOpen = isOpen; LastTime = lastTime; OperatorId = operatorId;
        }
        public string Id { get; private set; }
        public string Position { get; private set; }
        public string DownstreamTankId { get; private set; }   // 없으면 null(출하)
        public string SuctionPumpId { get; private set; }      // 이 밸브가 흡입 쪽인 펌프, 없으면 null
        public bool IsOpen { get; private set; }
        public DateTime LastTime { get; private set; }
        public string OperatorId { get; private set; }
    }

    internal sealed class PumpInfo
    {
        public PumpInfo(string id, bool running) { Id = id; Running = running; }
        public string Id { get; private set; }
        public bool Running { get; private set; }
    }

    internal sealed class AlarmEntry
    {
        public AlarmEntry(DateTime time, StatusKind kind, string target, string text) { Time = time; Kind = kind; Target = target; Text = text; }
        public DateTime Time { get; private set; }
        public StatusKind Kind { get; private set; }
        public string Target { get; private set; }
        public string Text { get; private set; }
    }

    internal sealed class ProcessSnapshot
    {
        public ProcessSnapshot(TankInfo[] tanks, ValveInfo[] valves, PumpInfo[] pumps) { Tanks = tanks; Valves = valves; Pumps = pumps; }
        public TankInfo[] Tanks { get; private set; }
        public ValveInfo[] Valves { get; private set; }
        public PumpInfo[] Pumps { get; private set; }

        public TankInfo Tank(string id) { return Array.Find(Tanks, t => t.Id == id); }
        public ValveInfo Valve(string id) { return Array.Find(Valves, v => v.Id == id); }
        public PumpInfo Pump(string id) { return Array.Find(Pumps, p => p.Id == id); }
    }
}
