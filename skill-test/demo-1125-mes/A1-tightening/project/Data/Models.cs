using System;
using System.Collections.Generic;
using System.Globalization;
using APC.UI.Theming;

namespace CaliperTighteningStation.Data
{
    /// <summary>판정 기준값(요구서에 없어 정한 값은 한 곳에: 토크·각도·재체결 횟수는 도메인 정보, 우회 시간 선택지는 정한 값).</summary>
    internal static class Spec
    {
        public const double TorqueMin = 24.0, TorqueMax = 27.0;   // N·m
        public const int AngleMin = 150, AngleMax = 220;          // °
        public const int MaxRetry = 2;                            // 같은 볼트 재체결 2회까지 → 시도 최대 3
        public static readonly int[] BoltSequence = { 1, 3, 2, 4 };   // 대각선
        public static readonly string[] BypassItems = { "E01", "E02", "E03", "E04", "E05" };   // E99(시스템 오류)는 우회 불가

        public static string Torque(double v) { return v.ToString("0.0", CultureInfo.InvariantCulture); }

        /// <summary>토크·각도 판정. 한계와 같은 값은 OK(초과·미만부터 NG). 사유 없으면 null.</summary>
        public static string BoltReason(double torque, int angle)
        {
            var r = new List<string>();
            if (torque < TorqueMin) r.Add("토크 낮음");
            if (torque > TorqueMax) r.Add("토크 높음");
            if (angle < AngleMin) r.Add("각도 낮음");
            if (angle > AngleMax) r.Add("각도 높음");
            return r.Count == 0 ? null : string.Join(" · ", r);
        }

        /// <summary>화면 한가운데 큰 판정 한 곳. 인터락 NG → 체결 불가, 재체결 초과 → NG, 모두 OK → OK.</summary>
        public static FinalState Final(StationSnapshot s)
        {
            if (s == null || s.Serial.Length == 0) return new FinalState("READY", "시리얼을 스캔하세요", StatusKind.Stop);
            foreach (var i in s.Interlocks)
                if (i.Status == StatusKind.Alarm) return new FinalState("체결 불가", i.Code + " " + i.Message, StatusKind.Alarm);
            int done = 0;
            foreach (var b in s.Bolts)
            {
                if (!b.HasResult) continue;
                if (b.Reason == null) { done++; continue; }
                if (b.Attempt > MaxRetry) return new FinalState("NG", "볼트 " + b.Bolt + " " + b.Reason + " · 수리 공정으로", StatusKind.Alarm);
                return new FinalState("재체결", "볼트 " + b.Bolt + " " + b.Reason + " · 재체결 " + (MaxRetry + 1 - b.Attempt) + "회 남음", StatusKind.Warn);
            }
            if (done == s.Bolts.Count) return new FinalState("OK", "볼트 " + done + "개 체결 완료", StatusKind.Run);
            return new FinalState("체결 중", "다음 볼트 " + BoltSequence[done] + " · " + done + " / " + s.Bolts.Count, StatusKind.Info);
        }
    }

    internal sealed class FinalState
    {
        public FinalState(string text, string subText, StatusKind status) { Text = text; SubText = subText; Status = status; }
        public string Text { get; private set; }
        public string SubText { get; private set; }
        public StatusKind Status { get; private set; }
    }

    internal sealed class InterlockRow
    {
        public InterlockRow(string code, string name, StatusKind status, string message)
        { Code = code; Name = name; Status = status; Message = message; }
        public string Code { get; private set; }
        public string Name { get; private set; }
        public StatusKind Status { get; private set; }
        public string Message { get; private set; }   // OK / 우회 / NG 사유
    }

    internal sealed class BoltRow
    {
        public BoltRow(int order, int bolt, double? torque, int? angle, int attempt)
        { Order = order; Bolt = bolt; Torque = torque; Angle = angle; Attempt = attempt; }
        public int Order { get; private set; }
        public int Bolt { get; private set; }
        public double? Torque { get; private set; }
        public int? Angle { get; private set; }
        public int Attempt { get; private set; }
        public bool HasResult { get { return Torque.HasValue && Angle.HasValue; } }
        public string Reason { get { return HasResult ? Spec.BoltReason(Torque.Value, Angle.Value) : null; } }
    }

    internal sealed class StationSnapshot
    {
        public StationSnapshot(string serial, IList<InterlockRow> interlocks, IList<BoltRow> bolts)
        { Serial = serial; Interlocks = interlocks; Bolts = bolts; }
        public string Serial { get; private set; }
        public IList<InterlockRow> Interlocks { get; private set; }
        public IList<BoltRow> Bolts { get; private set; }
    }

    internal sealed class BypassInfo
    {
        public BypassInfo(string items, DateTime until, string reason) { Items = items; Until = until; Reason = reason; }
        public string Items { get; private set; }
        public DateTime Until { get; private set; }
        public string Reason { get; private set; }
    }

    internal sealed class HistoryRow
    {
        public HistoryRow(DateTime time, string serial, StatusKind status, string result, string note)
        { Time = time; Serial = serial; Status = status; Result = result; Note = note; }
        public DateTime Time { get; private set; }
        public string Serial { get; private set; }
        public StatusKind Status { get; private set; }
        public string Result { get; private set; }
        public string Note { get; private set; }
    }
}
