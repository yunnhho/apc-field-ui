using System;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>탱크 한 개의 현재값. 새로 만드는 값 객체이므로 읽기 전용.</summary>
    internal sealed class TankInfo
    {
        public TankInfo(string id, string role, int level, double temp, bool hasTempSpec)
        {
            Id = id; Role = role; Level = level; Temp = temp; HasTempSpec = hasTempSpec;
        }
        public string Id { get; private set; }
        public string Role { get; private set; }
        public int Level { get; private set; }
        public double Temp { get; private set; }
        /// <summary>배합 온도 기준(45.0~55.0 ℃)이 있는 탱크만 true. 나머지는 표시만.</summary>
        public bool HasTempSpec { get; private set; }
    }

    internal sealed class PumpInfo
    {
        public PumpInfo(string id, bool running) { Id = id; Running = running; }
        public string Id { get; private set; }
        public bool Running { get; private set; }
    }

    /// <summary>밸브 한 개. 하류 탱크·배관 펌프·흡입 쪽 여부는 인터락 판단에 쓴다.</summary>
    internal sealed class ValveInfo
    {
        public ValveInfo(string id, string position, string downstreamTankId, string pumpId, bool suctionSide,
            bool isOpen, DateTime lastTime, string operatorId)
        {
            Id = id; Position = position; DownstreamTankId = downstreamTankId; PumpId = pumpId;
            SuctionSide = suctionSide; IsOpen = isOpen; LastTime = lastTime; OperatorId = operatorId;
        }
        public string Id { get; private set; }
        public string Position { get; private set; }
        /// <summary>하류 탱크 번호. 출하로 나가는 V6은 null.</summary>
        public string DownstreamTankId { get; private set; }
        public string PumpId { get; private set; }
        public bool SuctionSide { get; private set; }
        public bool IsOpen { get; private set; }
        public DateTime LastTime { get; private set; }
        public string OperatorId { get; private set; }

        public ValveInfo WithOperation(bool open, DateTime time, string operatorId)
        {
            return new ValveInfo(Id, Position, DownstreamTankId, PumpId, SuctionSide, open, time, operatorId);
        }
    }

    internal sealed class AlarmInfo
    {
        public AlarmInfo(StatusKind kind, DateTime time, string target, string text)
        {
            Kind = kind; Time = time; Target = target; Text = text;
        }
        public StatusKind Kind { get; private set; }
        public DateTime Time { get; private set; }
        public string Target { get; private set; }
        public string Text { get; private set; }
    }
}
