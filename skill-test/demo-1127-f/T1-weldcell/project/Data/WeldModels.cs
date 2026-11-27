using APC.UI.Theming;

namespace SpotWeldMonitor.Data
{
    /// <summary>셀 공정 상태(가동/대기/알람/정지).</summary>
    internal enum CellState { Running, Waiting, Alarm, Stopped }

    /// <summary>용접 셀 한 대. 값은 데이터 소스가 준다. 전류는 마지막 용접값(없으면 null).</summary>
    internal sealed class WeldCell
    {
        public WeldCell(string name, CellState state, int shotsToday, int tipShots, double? lastCurrentKa)
        {
            Name = name; State = state; ShotsToday = shotsToday; TipShots = tipShots; LastCurrentKa = lastCurrentKa;
        }
        public string Name { get; private set; }
        public CellState State { get; private set; }
        public int ShotsToday { get; private set; }
        public int TipShots { get; private set; }
        public double? LastCurrentKa { get; private set; }
    }

    /// <summary>라인 값 카드 4종의 값.</summary>
    internal sealed class LineValues
    {
        public LineValues(double availabilityPercent, int ngCount, int dressingWaitingCells)
        {
            AvailabilityPercent = availabilityPercent; NgCount = ngCount; DressingWaitingCells = dressingWaitingCells;
        }
        public double AvailabilityPercent { get; private set; }
        public int NgCount { get; private set; }
        public int DressingWaitingCells { get; private set; }
    }

    /// <summary>오늘 알람 이력 한 줄.</summary>
    internal sealed class AlarmRecord
    {
        public AlarmRecord(StatusKind kind, string time, string cell, string text, string action)
        {
            Kind = kind; Time = time; Cell = cell; Text = text; Action = action;
        }
        public StatusKind Kind { get; private set; }
        public string Time { get; private set; }
        public string Cell { get; private set; }
        public string Text { get; private set; }
        public string Action { get; private set; }
    }

    /// <summary>판정판용: 라인에서 가장 나쁜 셀과 사유.</summary>
    internal sealed class WorstCell
    {
        public WorstCell(WeldCell cell, StatusKind status, string reason) { Cell = cell; Status = status; Reason = reason; }
        public WeldCell Cell { get; private set; }
        public StatusKind Status { get; private set; }
        public string Reason { get; private set; }
    }
}
