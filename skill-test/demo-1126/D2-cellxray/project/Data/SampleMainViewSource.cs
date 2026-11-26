using System;
using System.Collections.Generic;

namespace CellXrayJudgeStation.Data
{
    /// <summary>화면 확인·render용 샘플 데이터.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const string TrayPrefix = "TR-";
        private readonly List<TrayHistoryRow> _history = new List<TrayHistoryRow>();

        public SampleMainViewSource()
        {
            // 금일 완료 트레이(최근순). 셀 합계 = OK + NG = 50, 시각·NG 수를 섞는다.
            int[] ng = { 2, 0, 3, 1, 0, 4, 1, 2, 0, 3, 1, 2, 0, 1, 4, 2 };
            int[] re = { 3, 1, 5, 2, 0, 6, 2, 4, 1, 5, 2, 3, 0, 2, 7, 3 };
            int[] gapMin = { 9, 11, 8, 14, 10, 12, 9, 13, 8, 15, 10, 9, 12, 11, 10, 9 };
            var at = new DateTime(2026, 11, 26, 10, 14, 37);
            for (int i = 0; i < ng.Length; i++)
            {
                _history.Add(new TrayHistoryRow(TrayPrefix + "A" + (152 - i).ToString("0000"), at, 50 - ng[i], ng[i], re[i]));
                at = at.AddMinutes(-gapMin[i]).AddSeconds(-(i * 7 % 23));
            }
        }

        public TrayRecord GetCurrentTray() { return LoadTray("TR-A0153"); }

        public TrayRecord LoadTray(string trayId)
        {
            if (trayId == null || !trayId.StartsWith(TrayPrefix) || trayId.Length != 8) return null;
            int seed = 0;
            foreach (char c in trayId) seed = seed * 31 + c;
            var rnd = new Random(seed);
            var cells = new CellRecord[CellSpec.TrayRows * CellSpec.TrayCols];
            for (int i = 0; i < cells.Length; i++)
            {
                // 오버행 4곳: 대부분 0.62~0.80, 일부 경계·NG. 셀마다 값이 다르게
                decimal baseValue = 0.62m + rnd.Next(0, 19) / 100m;
                var oh = new decimal[CellSpec.OverhangPoints];
                for (int k = 0; k < oh.Length; k++) oh[k] = baseValue + rnd.Next(0, 8) / 100m;
                decimal align = rnd.Next(4, 28) / 100m;
                bool foreign = false;
                switch (i)
                {
                    case 6: oh[2] = 0.42m; break;                       // NG 확정(오버행 미달)
                    case 13: oh[1] = 0.55m; break;                      // 경계 → 재판정 대기
                    case 17: oh[0] = 0.50m; break;                      // 하한과 같은 값 → 경계(대기)
                    case 22: align = 0.30m; break;                      // 상한과 같은 값 → OK
                    case 25: foreign = true; break;                     // 이물 → 재판정 대기
                    case 31: align = 0.37m; break;                      // 정렬 편차 초과 → NG
                    case 38: oh[3] = 0.60m; break;                      // 경계 상한과 같은 값 → 대기
                    case 44: oh[1] = 0.46m; foreign = true; break;      // NG + 이물
                }
                cells[i] = new CellRecord(i + 1, "C2611260" + (trayId == "TR-A0153" ? (i + 1).ToString("000") : (seed % 7 * 50 + i + 1).ToString("000")),
                    oh, align, foreign);
            }
            return new TrayRecord(trayId, cells);
        }

        public void SaveRejudge(string trayId, CellRecord cell) { /* 샘플: 저장 없음 */ }

        public TrayHistoryRow CompleteTray(TrayRecord tray, DateTime at)
        {
            var row = new TrayHistoryRow(tray.TrayId, at, tray.Count(CellVerdict.Ok), tray.Count(CellVerdict.Ng), tray.RejudgedCount);
            _history.Insert(0, row);
            return row;
        }

        public IList<TrayHistoryRow> GetTodayHistory() { return _history; }
    }
}
