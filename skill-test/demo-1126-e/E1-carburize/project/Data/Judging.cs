using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace CarburizeFurnaceMonitor.Data
{
    /// <summary>판정 규칙 한 곳. 상태 문구·카드색·타일·알람·dialog가 모두 이 함수를 쓴다.</summary>
    internal static class Judging
    {
        // 정한 기준값 — 요구에 있는 값
        public const decimal TempTolerance = 10m;          // ℃
        public const decimal CpTolerance = 0.05m;          // %
        public const int AlarmAfterMinutes = 1;            // 벗어나 이 시간을 넘으면 알람
        public const decimal HardnessMin = 58.0m, HardnessMax = 62.0m;   // HRC
        public const decimal DepthMin = 0.80m, DepthMax = 1.20m;         // mm
        public const int TraysPerBatch = 4, GearsPerTray = 120;

        public static string FormatTemp(decimal v) { return v.ToString("0"); }
        public static string FormatCp(decimal v) { return v.ToString("0.00"); }
        public static string FormatHardness(decimal v) { return v.ToString("0.0"); }
        public static string FormatDepth(decimal v) { return v.ToString("0.00"); }
        public static string FormatTime(DateTime t) { return t.ToString("HH:mm"); }
        public static string FormatMinutes(int m) { return string.Format("{0:00}:{1:00}", m / 60, m % 60); }

        /// <summary>단계 이름으로 본 로 상태 문구(타일·행에 쓴다). 알람은 별도.</summary>
        public static string PhaseName(FurnaceInfo f)
        {
            if (!f.HasBatch) return "대기";
            if (f.IsFinished) return "냉각 완료";
            switch (f.StepIndex)
            {
                case 0: return "가열";
                case 1: return "침탄";
                case 2: return "확산";
                default: return "냉각";
            }
        }

        public static bool IsOut(decimal value, decimal set, decimal tolerance)
        {
            return Math.Abs(value - set) > tolerance;   // 허용 한계값과 같으면 정상
        }

        /// <summary>값 하나의 상태: 값 없음·설정 없음 = Stop, 벗어남 1분 이하 = Warn, 1분 초과 = Alarm.</summary>
        public static StatusKind ValueStatus(decimal? value, decimal? set, decimal tolerance, int outMinutes)
        {
            if (!value.HasValue || !set.HasValue) return StatusKind.Stop;
            if (!IsOut(value.Value, set.Value, tolerance)) return StatusKind.Run;
            return outMinutes > AlarmAfterMinutes ? StatusKind.Alarm : StatusKind.Warn;
        }

        public static StatusKind TempStatus(FurnaceInfo f, IList<StepInfo> steps)
        {
            StepInfo s = CurrentStep(f, steps);
            return ValueStatus(f.Temp, s == null ? null : s.SetTemp, TempTolerance, f.TempOutMinutes);
        }

        public static StatusKind CpStatus(FurnaceInfo f, IList<StepInfo> steps)
        {
            StepInfo s = CurrentStep(f, steps);
            return ValueStatus(f.Cp, s == null ? null : s.SetCp, CpTolerance, f.CpOutMinutes);
        }

        public static StatusKind OilStatus(FurnaceInfo f, IList<StepInfo> steps)
        {
            StepInfo s = CurrentStep(f, steps);
            if (s == null || !f.OilTemp.HasValue || !s.OilMin.HasValue) return StatusKind.Run;   // 유냉 밖에서는 기준 없음
            return f.OilTemp.Value < s.OilMin.Value || f.OilTemp.Value > s.OilMax.Value ? StatusKind.Alarm : StatusKind.Run;
        }

        public static StepInfo CurrentStep(FurnaceInfo f, IList<StepInfo> steps)
        {
            return f.StepIndex >= 0 && f.StepIndex < steps.Count ? steps[f.StepIndex] : null;
        }

        /// <summary>지금 알람이면 사유 글(이탈 값·한계·이탈 시간), 아니면 "".</summary>
        public static string AlarmReason(FurnaceInfo f, IList<StepInfo> steps)
        {
            StepInfo s = CurrentStep(f, steps);
            if (s == null) return "";
            if (TempStatus(f, steps) == StatusKind.Alarm)
            {
                bool high = f.Temp.Value > s.SetTemp.Value;
                return string.Format("{0} 온도 {1} {2} ℃ ({3} {4}) · {5}분째", s.Name, high ? "상한 초과" : "하한 미달", FormatTemp(f.Temp.Value),
                    high ? "상한" : "하한", FormatTemp(s.SetTemp.Value + (high ? TempTolerance : -TempTolerance)), f.TempOutMinutes);
            }
            if (CpStatus(f, steps) == StatusKind.Alarm)
            {
                bool high = f.Cp.Value > s.SetCp.Value;
                return string.Format("{0} CP {1} {2} % ({3} {4}) · {5}분째", s.Name, high ? "상한 초과" : "하한 미달", FormatCp(f.Cp.Value),
                    high ? "상한" : "하한", FormatCp(s.SetCp.Value + (high ? CpTolerance : -CpTolerance)), f.CpOutMinutes);
            }
            return "";
        }

        /// <summary>타일·표 행 상태 = 공정 상태와 값 판정 중 나쁜 쪽.</summary>
        public static StatusKind FurnaceStatus(FurnaceInfo f, IList<StepInfo> steps)
        {
            if (!f.HasBatch) return StatusKind.Stop;
            if (AlarmReason(f, steps).Length > 0) return StatusKind.Alarm;
            if (f.IsFinished) return StatusKind.Warn;   // 종료 판정 대기
            return StatusKind.Run;
        }

        public static bool SpecimenPasses(decimal hardness, decimal depth)
        {
            return hardness >= HardnessMin && hardness <= HardnessMax && depth >= DepthMin && depth <= DepthMax;
        }
    }
}
