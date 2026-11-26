using System;
using System.Collections.Generic;
using System.Globalization;

namespace PressPmChecklist.Data
{
    internal enum StepKind { Safety, Visual, Measure }

    /// <summary>점검 항목 정의. 상한·하한은 허용 범위(같으면 OK, 넘으면 NG).</summary>
    internal sealed class PmStep
    {
        public readonly int No;
        public readonly string Name;
        public readonly string Method;
        public readonly StepKind Kind;
        public readonly string Unit;
        public readonly decimal? Lower;
        public readonly decimal? Upper;
        public readonly int Decimals;

        public PmStep(int no, string name, string method, StepKind kind, string unit, decimal? lower, decimal? upper, int decimals)
        {
            No = no; Name = name; Method = method; Kind = kind; Unit = unit; Lower = lower; Upper = upper; Decimals = decimals;
        }

        public bool IsMeasure { get { return Kind == StepKind.Measure; } }

        public string Format(decimal value)
        {
            return value.ToString("F" + Decimals, CultureInfo.InvariantCulture);
        }

        /// <summary>기준 문구. 육안·안전 = "이상 없음", 측정 = "18.0 ~ 21.0 MPa" / "180 ms 이하".</summary>
        public string Standard
        {
            get
            {
                if (!IsMeasure) return "이상 없음";
                if (Lower.HasValue && Upper.HasValue) return Format(Lower.Value) + " ~ " + Format(Upper.Value) + " " + Unit;
                if (Upper.HasValue) return Format(Upper.Value) + " " + Unit + " 이하";
                return Format(Lower.Value) + " " + Unit + " 이상";
            }
        }

        /// <summary>측정값 판정 — 항목 하나에 이 함수 하나(타일·목록·dialog 공통).</summary>
        public bool IsWithin(decimal value)
        {
            if (Upper.HasValue && value > Upper.Value) return false;
            if (Lower.HasValue && value < Lower.Value) return false;
            return true;
        }

        /// <summary>목록·dialog 항목 칸: 측정 항목은 단위를 붙인다("유압 압력 측정 (MPa)").</summary>
        public string ItemLabel { get { return IsMeasure ? Name + " (" + Unit + ")" : Name; } }
    }

    /// <summary>한 단계의 결과.</summary>
    internal sealed class StepResult
    {
        public readonly PmStep Step;
        public readonly bool Ok;
        public readonly decimal? Value;
        public readonly DateTime At;

        public StepResult(PmStep step, bool ok, decimal? value, DateTime at)
        {
            Step = step; Ok = ok; Value = value; At = at;
        }

        public string ValueText { get { return Value.HasValue ? Step.Format(Value.Value) : ""; } }

        /// <summary>NG 사유 문구("16.8 MPa · 기준 18.0 ~ 21.0 MPa" / "육안 점검 NG").</summary>
        public string NgReason
        {
            get { return Value.HasValue ? ValueText + " " + Step.Unit + " · 기준 " + Step.Standard : "육안 점검 NG"; }
        }
    }

    /// <summary>진행 중이던 점검(샘플 또는 복원용). 새 점검이면 null.</summary>
    internal sealed class PmSession
    {
        public readonly string Equipment;
        public readonly string Employee;
        public readonly DateTime StartedAt;
        public readonly IList<StepResult> Results;

        public PmSession(string equipment, string employee, DateTime startedAt, IList<StepResult> results)
        {
            Equipment = equipment; Employee = employee; StartedAt = startedAt; Results = results;
        }
    }
}
