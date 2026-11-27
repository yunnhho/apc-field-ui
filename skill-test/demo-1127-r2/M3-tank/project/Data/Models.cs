using System;
using APC.UI.Theming;

namespace TankFlow.Data
{
    internal sealed class TankInfo
    {
        public readonly string Id, Name;
        public readonly int Level;          // %
        public readonly decimal Temp;       // ℃
        public readonly bool HasTempSpec;   // T3만 배합 온도 범위가 있다

        public TankInfo(string id, string name, int level, decimal temp, bool hasTempSpec)
        { Id = id; Name = name; Level = level; Temp = temp; HasTempSpec = hasTempSpec; }
    }

    internal sealed class PumpInfo
    {
        public readonly string Id;
        public readonly bool Running;
        public PumpInfo(string id, bool running) { Id = id; Running = running; }
    }

    internal sealed class ValveInfo
    {
        public readonly string Id, From, To;
        public readonly string DownstreamTank;   // 열기 인터락이 보는 하류 탱크 (없으면 null)
        public readonly string PumpId;           // 그 배관 펌프 (없으면 null)
        public readonly bool IsSuction;          // 펌프 흡입 쪽 밸브
        public readonly bool IsOpen;
        public readonly DateTime LastTime;
        public readonly string Operator;

        public ValveInfo(string id, string from, string to, string downstreamTank, string pumpId, bool isSuction,
            bool isOpen, DateTime lastTime, string op)
        {
            Id = id; From = from; To = to; DownstreamTank = downstreamTank; PumpId = pumpId; IsSuction = isSuction;
            IsOpen = isOpen; LastTime = lastTime; Operator = op;
        }

        public ValveInfo Operated(bool open, DateTime time, string op)
        {
            return new ValveInfo(Id, From, To, DownstreamTank, PumpId, IsSuction, open, time, op);
        }

        public string Route { get { return From + " → " + To; } }
    }

    internal sealed class AlarmRecord
    {
        public readonly StatusKind Kind;
        public readonly DateTime Time;
        public readonly string Target, Text;
        public AlarmRecord(StatusKind kind, DateTime time, string target, string text)
        { Kind = kind; Time = time; Target = target; Text = text; }
    }
}
