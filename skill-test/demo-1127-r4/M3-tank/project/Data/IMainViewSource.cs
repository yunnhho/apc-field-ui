using System;

namespace TankFlow.Data
{
    /// <summary>탱크 한 기의 현재값(읽기 전용 값 객체).</summary>
    internal sealed class TankInfo
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Level;          // %
        public readonly double Temperature; // ℃

        public TankInfo(string id, string name, int level, double temperature)
        {
            Id = id; Name = name; Level = level; Temperature = temperature;
        }
    }

    internal sealed class ValveInfo
    {
        public readonly string Id;
        public readonly string Position;
        public readonly bool IsOpen;
        public readonly DateTime LastOperated;
        public readonly string Operator;

        public ValveInfo(string id, string position, bool isOpen, DateTime lastOperated, string op)
        {
            Id = id; Position = position; IsOpen = isOpen; LastOperated = lastOperated; Operator = op;
        }
    }

    internal sealed class PumpInfo
    {
        public readonly string Id;
        public readonly bool Running;
        public PumpInfo(string id, bool running) { Id = id; Running = running; }
    }

    internal sealed class AlarmEntry
    {
        public readonly APC.UI.Theming.StatusKind Kind;
        public readonly DateTime Time;
        public readonly string Target;
        public readonly string Text;

        public AlarmEntry(APC.UI.Theming.StatusKind kind, DateTime time, string target, string text)
        {
            Kind = kind; Time = time; Target = target; Text = text;
        }
    }

    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        TankInfo[] GetTanks();
        ValveInfo[] GetValves();
        PumpInfo[] GetPumps();
        AlarmEntry[] GetTodayAlarms();

        /// <summary>밸브를 연다/닫는다(PLC 명령 자리). 인터락 재검사는 구현 쪽 몫이다.</summary>
        void OperateValve(string valveId, bool open, string operatorBadge);
    }
}
