using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace CellXrayJudgeStation.Data
{
    internal enum CellVerdict { Ok, Ng, Pending }

    /// <summary>셀 규격 21700 판정 기준과 항목별 판정. 화면·표·dialog가 모두 이 함수를 쓴다.</summary>
    internal static class CellSpec
    {
        public const decimal OverhangMin = 0.50m;        // 정한 기준값: 오버행 하한(이상 OK 범위의 시작)
        public const decimal OverhangBorderMax = 0.60m;  // 0.50 ~ 0.60 경계(재판정 대기). 0.60 초과 OK
        public const decimal AlignMax = 0.30m;           // 정렬 편차 상한(이하 OK)
        public const int OverhangPoints = 4;
        public const int TrayRows = 5, TrayCols = 10;
        public static readonly string[] NgTypes = { "오버행 부족", "정렬 불량", "이물", "탭 접힘", "기타" };

        public static CellVerdict Overhang(decimal mm)
        {
            if (mm < OverhangMin) return CellVerdict.Ng;
            return mm <= OverhangBorderMax ? CellVerdict.Pending : CellVerdict.Ok;
        }

        public static CellVerdict Align(decimal mm) { return mm > AlignMax ? CellVerdict.Ng : CellVerdict.Ok; }

        public static CellVerdict Foreign(bool found) { return found ? CellVerdict.Pending : CellVerdict.Ok; }

        public static StatusKind Kind(CellVerdict v)
        {
            return v == CellVerdict.Ok ? StatusKind.Run : v == CellVerdict.Ng ? StatusKind.Alarm : StatusKind.Warn;
        }

        public static string Word(CellVerdict v) { return v == CellVerdict.Ok ? "OK" : v == CellVerdict.Ng ? "NG" : "재판정 대기"; }
    }

    /// <summary>트레이의 셀 한 개. 측정값은 읽기 전용.</summary>
    internal sealed class CellRecord
    {
        public CellRecord(int position, string cellId, decimal[] overhang, decimal align, bool foreign)
        {
            Position = position; CellId = cellId; Overhang = overhang; Align = align; Foreign = foreign;
            Final = Auto;
        }

        public int Position { get; private set; }          // 1~50 (트레이 칸 번호)
        public string CellId { get; private set; }
        public decimal[] Overhang { get; private set; }    // 4곳
        public decimal Align { get; private set; }
        public bool Foreign { get; private set; }

        /// <summary>현재 판정(자동 판정 → 작업자 재판정 후 갱신).</summary>
        public CellVerdict Final { get; set; }
        public string NgType { get; set; }
        public string Reason { get; set; }
        public bool Rejudged { get; set; }

        public decimal OverhangLow { get { decimal m = Overhang[0]; foreach (decimal v in Overhang) if (v < m) m = v; return m; } }

        /// <summary>자동 판정: 하나라도 NG면 NG, 아니면 하나라도 대기면 대기.</summary>
        public CellVerdict Auto
        {
            get
            {
                var list = new List<CellVerdict> { CellSpec.Overhang(OverhangLow), CellSpec.Align(Align), CellSpec.Foreign(Foreign) };
                if (list.Contains(CellVerdict.Ng)) return CellVerdict.Ng;
                return list.Contains(CellVerdict.Pending) ? CellVerdict.Pending : CellVerdict.Ok;
            }
        }

        /// <summary>규격 미달 측정값이 있으면 OK로 바꿀 수 없다(이유 글을 돌려준다).</summary>
        public string OkBlockedReason
        {
            get
            {
                if (CellSpec.Overhang(OverhangLow) == CellVerdict.Ng)
                    return "오버행 최소 " + OverhangLow.ToString("0.00") + " mm < 하한 " + CellSpec.OverhangMin.ToString("0.00");
                if (CellSpec.Align(Align) == CellVerdict.Ng)
                    return "정렬 편차 " + Align.ToString("0.00") + " mm > 상한 " + CellSpec.AlignMax.ToString("0.00");
                return null;
            }
        }

        /// <summary>자동 판정 사유(한 줄).</summary>
        public string AutoReason
        {
            get
            {
                string ng = OkBlockedReason;
                if (ng != null) return "자동 NG · " + ng;
                if (CellSpec.Overhang(OverhangLow) == CellVerdict.Pending)
                    return "오버행 경계 · 최소 " + OverhangLow.ToString("0.00") + " mm (0.50 ~ 0.60)";
                if (Foreign) return "이물 검출 · 이미지 확인 필요";
                return "자동 OK";
            }
        }
    }

    internal sealed class TrayRecord
    {
        public TrayRecord(string trayId, CellRecord[] cells) { TrayId = trayId; Cells = cells; }
        public string TrayId { get; private set; }
        public CellRecord[] Cells { get; private set; }

        public int Count(CellVerdict v) { int n = 0; foreach (var c in Cells) if (c.Final == v) n++; return n; }
        public int RejudgedCount { get { int n = 0; foreach (var c in Cells) if (c.Rejudged) n++; return n; } }
    }

    /// <summary>완료한 트레이 이력 한 줄.</summary>
    internal sealed class TrayHistoryRow
    {
        public TrayHistoryRow(string trayId, DateTime doneAt, int ok, int ng, int rejudged)
        {
            TrayId = trayId; DoneAt = doneAt; Ok = ok; Ng = ng; Rejudged = rejudged;
        }
        public string TrayId { get; private set; }
        public DateTime DoneAt { get; private set; }
        public int Ok { get; private set; }
        public int Ng { get; private set; }
        public int Rejudged { get; private set; }
    }
}
