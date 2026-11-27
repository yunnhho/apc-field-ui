using System;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>탱크 한 개의 현재 값. TempSpec이 true인 탱크만 온도를 판정한다(T3).</summary>
    internal sealed class TankInfo
    {
        public readonly string Id;
        public readonly string Name;
        public readonly double Level;
        public readonly double Temp;
        public readonly bool TempSpec;

        public TankInfo(string id, string name, double level, double temp, bool tempSpec)
        {
            Id = id; Name = name; Level = level; Temp = temp; TempSpec = tempSpec;
        }
    }

    internal sealed class PumpInfo
    {
        public readonly string Id;
        public readonly bool Running;

        public PumpInfo(string id, bool running) { Id = id; Running = running; }
    }

    /// <summary>밸브 한 개. DownstreamTank·Pump는 해당 없으면 "". IsSuction = 펌프 흡입 쪽 밸브.</summary>
    internal sealed class ValveInfo
    {
        public readonly string Id;
        public readonly string Position;
        public readonly bool Open;
        public readonly DateTime? LastTime;
        public readonly string Operator;
        public readonly string DownstreamTank;
        public readonly string Pump;
        public readonly bool IsSuction;

        public ValveInfo(string id, string position, bool open, DateTime? lastTime, string op,
                         string downstreamTank, string pump, bool isSuction)
        {
            Id = id; Position = position; Open = open; LastTime = lastTime; Operator = op;
            DownstreamTank = downstreamTank; Pump = pump; IsSuction = isSuction;
        }

        public ValveInfo WithOperation(bool open, DateTime at, string op)
        {
            return new ValveInfo(Id, Position, open, at, op, DownstreamTank, Pump, IsSuction);
        }
    }

    internal sealed class AlarmEntry
    {
        public readonly DateTime Time;
        public readonly StatusKind Kind;
        public readonly string Source;
        public readonly string Text;

        public AlarmEntry(DateTime time, StatusKind kind, string source, string text)
        {
            Time = time; Kind = kind; Source = source; Text = text;
        }
    }
}
