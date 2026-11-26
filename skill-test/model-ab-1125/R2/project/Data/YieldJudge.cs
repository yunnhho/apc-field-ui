using System;
using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>최종 수율 판정 하나. 타일·카드·이력·OCAP dialog가 모두 이것을 쓴다.</summary>
    internal static class YieldJudge
    {
        /// <summary>최종 수율 기준(이상 합격, 미만이면 Lot Hold).</summary>
        public const double Limit = 95.0;
        /// <summary>정한 기준값: 경고 시작(이하이면 기준 근접).</summary>
        public const double WarnStart = 96.0;

        public static double Yield(int good, int tested)
        {
            return tested == 0 ? 0 : good * 100.0 / tested;
        }

        /// <summary>표시 자릿수(0.0)로 반올림한 값으로 판정 — 보이는 값과 판정이 어긋나지 않게.</summary>
        public static StatusKind Judge(double yield)
        {
            double y = Math.Round(yield, 1, MidpointRounding.AwayFromZero);
            if (y < Limit) return StatusKind.Alarm;
            if (y <= WarnStart) return StatusKind.Warn;
            return StatusKind.Run;
        }

        public static string Word(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Alarm: return "Hold";
                case StatusKind.Warn: return "기준 근접";
                default: return "Pass";
            }
        }

        public static string Pct(double value)
        {
            return value.ToString("0.0");
        }
    }
}
