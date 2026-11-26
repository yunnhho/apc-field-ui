using System;

namespace Polish.Data
{
    /// <summary>설비 공정 단계. 대기(LOT 없음) → 전송됨(작업 조건 전송, 가동 전) → 가동 중. 끊김은 통신 상태.</summary>
    internal enum EquipPhase { Offline, Idle, Sent, Running }

    /// <summary>SAP·마스터에서 찾은 작업 조건(구분·자재코드·목표 시간).</summary>
    internal sealed class PolishCondition
    {
        public readonly string Kind;          // 그린칩 / 소성칩
        public readonly string Material;      // 자재코드
        public readonly int TargetSeconds;    // 목표 운전 시간(초)

        public PolishCondition(string kind, string material, int targetSeconds)
        {
            Kind = kind; Material = material; TargetSeconds = targetSeconds;
        }

        public bool SameAs(PolishCondition other)
        {
            return other != null && Kind == other.Kind && Material == other.Material && TargetSeconds == other.TargetSeconds;
        }
    }

    /// <summary>표 한 행(설비 한 칸). 저속 LOT 최대 2, 고속 최대 4.</summary>
    internal sealed class EquipmentRow
    {
        public readonly string Name;
        public readonly bool IsLowSpeed;
        public readonly EquipPhase Phase;
        public readonly string[] Lots;
        public readonly string[] Barrels;
        public readonly PolishCondition Condition;   // 대기·끊김이면 null
        public readonly int ElapsedSeconds;
        public readonly int? Rpm;                    // 값 없음 = null

        public EquipmentRow(string name, bool isLowSpeed, EquipPhase phase, string[] lots, string[] barrels,
            PolishCondition condition, int elapsedSeconds, int? rpm)
        {
            Name = name; IsLowSpeed = isLowSpeed; Phase = phase; Lots = lots; Barrels = barrels;
            Condition = condition; ElapsedSeconds = elapsedSeconds; Rpm = rpm;
        }

        public int SlotCount { get { return IsLowSpeed ? 2 : 4; } }

        public EquipmentRow WithSent(string[] lots, string[] barrels, PolishCondition condition)
        {
            return new EquipmentRow(Name, IsLowSpeed, EquipPhase.Sent, lots, barrels, condition, 0, null);
        }
    }
}
