using System;
using APC.UI.Theming;

namespace Polish.Data
{
    /// <summary>
    /// 판정 기준과 판정 함수(상태 문구·램프·카드·판정판이 모두 이 함수를 쓴다).
    /// 요구사항: 저속 RPM SV 370 고정, 시작 후 3분·종료 3분 전에는 PV 이탈 알람을 내지 않는다.
    /// 허용 편차·고속 RPM 기준은 요구사항에 없어 가정한 값이다.
    /// </summary>
    internal static class PolishRules
    {
        public const int LowRpmSv = 370;
        public const int RpmTolerance = 10;       // 가정: 370 ± 10 (상·하한과 같으면 정상)
        public const int AlarmGuardSeconds = 180; // 시작 후 3분 / 종료 3분 전

        public static bool IsRpmOut(EquipmentRow r)
        {
            if (r.Phase != EquipPhase.Running || !r.IsLowSpeed || !r.Rpm.HasValue || r.Condition == null) return false;
            if (r.ElapsedSeconds < AlarmGuardSeconds) return false;
            if (r.Condition.TargetSeconds - r.ElapsedSeconds < AlarmGuardSeconds) return false;
            return Math.Abs(r.Rpm.Value - LowRpmSv) > RpmTolerance;
        }

        public static StatusKind StatusOf(EquipmentRow r)
        {
            if (IsRpmOut(r)) return StatusKind.Alarm;
            switch (r.Phase)
            {
                case EquipPhase.Running: return StatusKind.Run;
                case EquipPhase.Sent: return StatusKind.Info;
                default: return StatusKind.Stop;
            }
        }

        public static string StatusText(EquipmentRow r)
        {
            if (IsRpmOut(r)) return "가동 · RPM 이탈";
            switch (r.Phase)
            {
                case EquipPhase.Running: return "가동";
                case EquipPhase.Sent: return "전송됨";
                case EquipPhase.Idle: return "대기";
                default: return "통신 끊김";
            }
        }

        public static string Clock(int seconds)
        {
            return string.Format("{0:00}:{1:00}:{2:00}", seconds / 3600, seconds / 60 % 60, seconds % 60);
        }

        public static string RpmLimitText()
        {
            return "기준 " + LowRpmSv + " ± " + RpmTolerance;
        }
    }
}
