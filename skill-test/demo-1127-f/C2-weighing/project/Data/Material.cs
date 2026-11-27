using System;
using System.Globalization;

namespace RubberWeighing.Data
{
    internal enum WeighVerdict { Under, Ok, Over }

    /// <summary>레시피의 원료 한 줄. 허용 오차: 1 kg 이상 ±0.5 %, 1 kg 미만 ±2.0 %(경계 포함 OK).</summary>
    internal sealed class Material
    {
        private const decimal LargeTolerancePercent = 0.5m;
        private const decimal SmallTolerancePercent = 2.0m;
        private const decimal LargeThresholdKg = 1m;
        private const decimal MinWidthFor2Decimals = 0.03m;   // 허용 폭이 0.01 kg 눈금 3칸 미만이면 0.001 kg 눈금

        public readonly int No;
        public readonly string Code;
        public readonly string Name;
        public readonly decimal Target;

        public Material(int no, string code, string name, decimal target)
        {
            No = no; Code = code; Name = name; Target = target;
        }

        /// <summary>표시 소수 자리: 1 kg 이상 0.00, 미만 0.000. 1 kg 이상이라도 허용 폭이 좁으면(산화아연) 0.000.</summary>
        public int Decimals { get { return Target >= LargeThresholdKg && Upper - Lower >= MinWidthFor2Decimals ? 2 : 3; } }
        public decimal TolerancePercent { get { return Target >= LargeThresholdKg ? LargeTolerancePercent : SmallTolerancePercent; } }
        public decimal Lower { get { return Target * (1 - TolerancePercent / 100m); } }
        public decimal Upper { get { return Target * (1 + TolerancePercent / 100m); } }

        /// <summary>판정 함수 하나 — 화면·표·dialog가 모두 이것만 쓴다. 상·하한과 같으면 OK.</summary>
        public WeighVerdict Judge(decimal measured)
        {
            if (measured < Lower) return WeighVerdict.Under;
            if (measured > Upper) return WeighVerdict.Over;
            return WeighVerdict.Ok;
        }

        /// <summary>저울 눈금 단위(Decimals)에서 실제로 합격하는 하한(눈금 위로 올림). 표시용 — 판정은 정확한 한계로 한다.</summary>
        public decimal ShownLower { get { return RoundTo(Lower, true); } }
        public decimal ShownUpper { get { return RoundTo(Upper, false); } }

        public string Fmt(decimal v) { return v.ToString(Decimals == 2 ? "0.00" : "0.000", CultureInfo.InvariantCulture); }
        public string RangeText { get { return Fmt(ShownLower) + " ~ " + Fmt(ShownUpper); } }

        /// <summary>목표 대비 오차 %, 0.00 형식, 부호 포함.</summary>
        public string ErrorPercentText(decimal measured)
        {
            decimal pct = Math.Round((measured - Target) / Target * 100m, 2, MidpointRounding.AwayFromZero);
            return (pct > 0 ? "+" : "") + pct.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private decimal RoundTo(decimal v, bool up)
        {
            decimal unit = Decimals == 2 ? 0.01m : 0.001m;
            decimal steps = v / unit;
            return (up ? Math.Ceiling(steps) : Math.Floor(steps)) * unit;
        }
    }

    /// <summary>확정된 계량 한 건.</summary>
    internal sealed class WeighRecord
    {
        public readonly int No;
        public readonly string LotNo;
        public readonly decimal Measured;
        public readonly string OperatorId;
        public readonly DateTime Time;

        public WeighRecord(int no, string lotNo, decimal measured, string operatorId, DateTime time)
        {
            No = no; LotNo = lotNo; Measured = measured; OperatorId = operatorId; Time = time;
        }
    }
}
