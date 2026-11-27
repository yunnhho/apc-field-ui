using System;
using APC.UI.Theming;

namespace TankFlow.Data
{
    /// <summary>탱크 한 개의 현재값. 레벨은 정수 %, 온도는 ℃.</summary>
    internal sealed class TankInfo
    {
        public TankInfo(string id, string name, int level, double temp, bool hasTempSpec)
        {
            Id = id; Name = name; Level = level; Temp = temp; HasTempSpec = hasTempSpec;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public int Level { get; private set; }
        public double Temp { get; private set; }
        /// <summary>온도 기준(배합 탱크 T3)이 있는 탱크만 true. 나머지는 표시만.</summary>
        public bool HasTempSpec { get; private set; }
    }

    /// <summary>밸브 한 개. 하류 탱크·펌프·흡입 쪽 여부는 배관 구성(인터락 판정용).</summary>
    internal sealed class ValveInfo
    {
        public ValveInfo(string id, string position, bool isOpen, DateTime lastTime, string operatorId,
            string downstreamTankId, string pumpId, bool isSuction)
        {
            Id = id; Position = position; IsOpen = isOpen; LastTime = lastTime; Operator = operatorId;
            DownstreamTankId = downstreamTankId; PumpId = pumpId; IsSuction = isSuction;
        }

        public string Id { get; private set; }
        public string Position { get; private set; }
        public bool IsOpen { get; private set; }
        public DateTime LastTime { get; private set; }
        public string Operator { get; private set; }
        /// <summary>이 밸브 하류 쪽 탱크(없으면 null — 출하).</summary>
        public string DownstreamTankId { get; private set; }
        /// <summary>이 배관의 펌프(없으면 null).</summary>
        public string PumpId { get; private set; }
        /// <summary>펌프 흡입 쪽 밸브면 true.</summary>
        public bool IsSuction { get; private set; }

        public ValveInfo WithState(bool isOpen, DateTime time, string operatorId)
        {
            return new ValveInfo(Id, Position, isOpen, time, operatorId, DownstreamTankId, PumpId, IsSuction);
        }
    }

    internal sealed class PumpInfo
    {
        public PumpInfo(string id, bool running) { Id = id; Running = running; }
        public string Id { get; private set; }
        public bool Running { get; private set; }
    }

    /// <summary>오늘 알람·조작 이력 한 줄.</summary>
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
