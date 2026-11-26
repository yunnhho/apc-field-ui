using System;
using System.Collections.Generic;

namespace MoldingShiftGantt.Data
{
    internal enum BlockKind { Job, Changeover, Downtime }

    /// <summary>작업 지시 하나. 작업 시간은 수량·캐비티·사이클타임에서 나온다.</summary>
    internal sealed class WorkOrder
    {
        public WorkOrder(string code, string part, string mold, int moldTon, int qty, double cycleSec, int cavities, bool dueToday)
        {
            Code = code; Part = part; Mold = mold; MoldTon = moldTon; Qty = qty;
            CycleSec = cycleSec; Cavities = cavities; DueToday = dueToday;
        }

        public readonly string Code, Part, Mold;
        public readonly int MoldTon, Qty, Cavities;
        public readonly double CycleSec;
        public readonly bool DueToday;

        /// <summary>계획 작업 시간(분, 올림).</summary>
        public int DurationMin
        {
            get { return (int)Math.Ceiling((double)Qty / Cavities * CycleSec / 60.0); }
        }

        /// <summary>분당 생산 수량(이상적).</summary>
        public double RatePerMin { get { return Cavities * 60.0 / CycleSec; } }
    }

    /// <summary>계획 막대(작업 또는 금형 교체). 시각은 08:00 기준 분.</summary>
    internal sealed class PlanBlock
    {
        public PlanBlock(BlockKind kind, WorkOrder wo, int startMin, int endMin)
        {
            Kind = kind; Wo = wo; StartMin = startMin; EndMin = endMin;
        }

        public readonly BlockKind Kind;
        public readonly WorkOrder Wo;       // 금형 교체면 다음 작업
        public readonly int StartMin, EndMin;
    }

    /// <summary>실적 막대(작업·금형 교체·비가동). Running = 현재 시각까지 진행 중.</summary>
    internal sealed class ActualBlock
    {
        public ActualBlock(BlockKind kind, WorkOrder wo, int startMin, int endMin, int qty, bool running)
        {
            Kind = kind; Wo = wo; StartMin = startMin; EndMin = endMin; Qty = qty; Running = running;
        }

        public readonly BlockKind Kind;
        public readonly WorkOrder Wo;
        public readonly int StartMin, EndMin, Qty;
        public readonly bool Running;
    }

    internal sealed class Machine
    {
        public Machine(string name, int ton)
        {
            Name = name; Ton = ton;
            Plan = new List<PlanBlock>();
            Actual = new List<ActualBlock>();
        }

        public readonly string Name;
        public readonly int Ton;
        public readonly List<PlanBlock> Plan;
        public readonly List<ActualBlock> Actual;
    }

    /// <summary>작업 상세 패널에 보이는 값(표시 문자열 아님, 원 값).</summary>
    internal sealed class JobDetail
    {
        public WorkOrder Wo;
        public string MachineName;
        public int ActualQty;
        public int ExpectedEndMin;   // -1 = 미정(고장 중)
    }

    /// <summary>미리보기 한 줄: 끼워 넣은 뒤 작업 순서.</summary>
    internal sealed class PreviewRow
    {
        public string Label;        // 작업 지시 코드 또는 "금형 교체"
        public string Part;
        public bool IsUrgent, IsChangeover, DueToday;
        public int OldStartMin;     // -1 = 새로 생김
        public int NewStartMin, NewEndMin;
        public int PushMin;         // 밀린 분(0 = 안 밀림)
    }

    internal sealed class InsertPreview
    {
        public readonly List<PreviewRow> Rows = new List<PreviewRow>();
        public bool ToolingFits;
        public bool NeedsApproval;      // 납기 오늘인 작업이 밀림
        public string DueTodayPushed;   // 밀린 납기 오늘 작업(쉼표 목록)
        public int OverflowMin;         // 교대 종료(20:00) 넘는 분, 0 = 안 넘음
    }

    internal sealed class BadgeResult
    {
        public bool Accepted;
        public string Message;
    }
}
