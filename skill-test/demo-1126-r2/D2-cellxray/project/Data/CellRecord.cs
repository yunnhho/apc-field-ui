using System.Collections.Generic;
using System.Globalization;
using APC.UI.Theming;

namespace CellXrayJudgeStation.Data
{
    internal enum CellState { Ok, Ng, Pending }

    /// <summary>셀 한 개의 X-ray 측정값과 판정. 판정(State 등)은 재판정으로 바뀐다.</summary>
    internal sealed class CellRecord
    {
        public const int OverheadPoints = 4;
        public int Slot;                       // 트레이 안 위치 1~50
        public string CellId;
        public double[] Overhang = new double[OverheadPoints];   // 양극-음극 오버행 최소값 4곳 (mm)
        public double Align;                   // 젤리롤 정렬 편차 (mm)
        public bool Foreign;                   // 이물 검출
        public CellState State;
        public string NgType;                  // State == Ng일 때
        public string Reason;                  // 자동 판정 사유, 재판정 후엔 작업자 사유
        public bool Rejudged;

        public double MinOverhang
        {
            get { double m = Overhang[0]; for (int i = 1; i < Overhang.Length; i++) if (Overhang[i] < m) m = Overhang[i]; return m; }
        }
    }

    /// <summary>한 트레이(셀 50개, 5×10).</summary>
    internal sealed class TrayData
    {
        public const int Rows = 5, Columns = 10, CellCount = Rows * Columns;
        public string TrayId;
        public List<CellRecord> Cells = new List<CellRecord>();

        public int Count(CellState s) { int n = 0; foreach (var c in Cells) if (c.State == s) n++; return n; }
        public int RejudgedDone { get { int n = 0; foreach (var c in Cells) if (c.Rejudged) n++; return n; } }
    }

    /// <summary>완료한 트레이 이력 한 줄(DTO).</summary>
    internal sealed class HistoryRow
    {
        public readonly System.DateTime Time;
        public readonly string TrayId;
        public readonly int Total, Ok, Ng, Rejudged;
        public HistoryRow(System.DateTime time, string trayId, int total, int ok, int ng, int rejudged)
        { Time = time; TrayId = trayId; Total = total; Ok = ok; Ng = ng; Rejudged = rejudged; }
    }

    /// <summary>
    /// 판정 기준(정한 기준값)과 판정 함수. 표·판정판·dialog가 모두 이 함수를 쓴다.
    /// 오버행: 0.50 mm 이상 규격, 0.50~0.60(양 끝 포함)은 경계(재판정 대기), 0.50 미만 NG 확정. 정렬 편차 0.30 이하 OK, 초과 NG. 이물 검출은 재판정 대기.
    /// </summary>
    internal static class CellJudge
    {
        public const double OverhangMin = 0.50, OverhangBoundaryMax = 0.60, AlignMax = 0.30;
        public static readonly string[] NgTypes = { "오버행 부족", "정렬 불량", "이물", "탭 접힘", "기타" };
        public const string NgOther = "기타";

        public static string F(double v) { return v.ToString("0.00", CultureInfo.InvariantCulture); }

        public static StatusKind OverhangKind(double v)
        {
            return v < OverhangMin ? StatusKind.Alarm : v <= OverhangBoundaryMax ? StatusKind.Warn : StatusKind.Run;
        }
        public static StatusKind AlignKind(double v) { return v > AlignMax ? StatusKind.Alarm : StatusKind.Run; }
        public static StatusKind ForeignKind(bool foreign) { return foreign ? StatusKind.Warn : StatusKind.Run; }

        public static string KindWord(StatusKind k) { return k == StatusKind.Alarm ? "NG" : k == StatusKind.Warn ? "경계" : "OK"; }

        /// <summary>자동 판정: State·NgType·Reason을 채운다. 가장 나쁜 항목이 사유(같으면 오버행 → 정렬 → 이물).</summary>
        public static void Apply(CellRecord c)
        {
            c.Rejudged = false;
            c.NgType = null;
            double min = c.MinOverhang;
            if (OverhangKind(min) == StatusKind.Alarm)
            { c.State = CellState.Ng; c.NgType = NgTypes[0]; c.Reason = "오버행 부족 " + F(min) + " mm (하한 " + F(OverhangMin) + ")"; }
            else if (AlignKind(c.Align) == StatusKind.Alarm)
            { c.State = CellState.Ng; c.NgType = NgTypes[1]; c.Reason = "정렬 편차 " + F(c.Align) + " mm (상한 " + F(AlignMax) + ")"; }
            else if (OverhangKind(min) == StatusKind.Warn)
            { c.State = CellState.Pending; c.Reason = "오버행 경계 " + F(min) + " mm (" + F(OverhangMin) + " ~ " + F(OverhangBoundaryMax) + ")"; }
            else if (c.Foreign)
            { c.State = CellState.Pending; c.Reason = "이물 검출 · 이미지 확인"; }
            else
            { c.State = CellState.Ok; c.Reason = ""; }
        }

        public static string StateWord(CellState s) { return s == CellState.Ok ? "OK" : s == CellState.Ng ? "NG" : "재판정 대기"; }
    }
}
