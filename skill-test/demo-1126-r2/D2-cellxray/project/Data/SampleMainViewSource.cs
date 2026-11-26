using System;
using System.Collections.Generic;

namespace CellXrayJudgeStation.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 트레이 번호가 달라도 같은 모양(재판정 4·NG 3)이 위치만 옮겨 나온다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const int BaseTrayNumber = 153;
        private readonly List<HistoryRow> _history = new List<HistoryRow>();

        public SampleMainViewSource()
        {
            // 오늘 완료 15트레이(검사 750). 시각·NG·재판정 수를 서로 다르게.
            var day = new DateTime(2026, 11, 26);
            int[] ng = { 1, 3, 2, 2, 0, 3, 1, 4, 2, 1, 5, 0, 2, 3, 1 }, rej = { 2, 4, 3, 3, 1, 4, 2, 5, 3, 2, 6, 1, 4, 3, 2 };
            int[] sec = { 5 * 3600 + 22 * 60 + 8, 5 * 3600 + 57 * 60 + 31, 6 * 3600 + 34 * 60 + 15, 7 * 3600 + 12 * 60 + 40, 7 * 3600 + 58 * 60 + 3, 8 * 3600 + 41 * 60 + 12, 9 * 3600 + 27 * 60 + 50, 10 * 3600 + 18 * 60 + 5, 10 * 3600 + 49 * 60 + 36,
                11 * 3600 + 2 * 60 + 33, 12 * 3600 + 5 * 60 + 21, 12 * 3600 + 51 * 60 + 9, 13 * 3600 + 46 * 60 + 19, 14 * 3600 + 21 * 60 + 47, 14 * 3600 + 58 * 60 + 26 };
            for (int i = 0; i < ng.Length; i++)
                _history.Insert(0, new HistoryRow(day.AddSeconds(sec[i]), "TR-A" + (138 + i).ToString("0000"), TrayData.CellCount, TrayData.CellCount - ng[i], ng[i], rej[i]));
        }

        public bool IsCompleted(string trayId)
        {
            foreach (var h in _history) if (h.TrayId == trayId) return true;
            return false;
        }

        public IList<HistoryRow> GetHistory() { return _history; }

        public void SaveRejudge(string trayId, CellRecord cell) { /* 샘플: 저장할 곳 없음 */ }

        public HistoryRow CompleteTray(TrayData tray, DateTime at)
        {
            int ng = tray.Count(CellState.Ng);
            var row = new HistoryRow(at, tray.TrayId, TrayData.CellCount, TrayData.CellCount - ng, ng, tray.RejudgedDone);
            _history.Insert(0, row);
            return row;
        }

        public TrayData LoadTray(string trayId)
        {
            int number;
            if (trayId == null || trayId.Length != 8 || !int.TryParse(trayId.Substring(4), out number)) return null;
            int shift = (((number - BaseTrayNumber) % TrayData.CellCount) + TrayData.CellCount) % TrayData.CellCount;
            var tray = new TrayData { TrayId = trayId };
            int firstSeq = 751 + (number - BaseTrayNumber) * TrayData.CellCount;   // 오늘 이어지는 셀 번호
            for (int i = 1; i <= TrayData.CellCount; i++)
            {
                var c = new CellRecord { Slot = i, CellId = "C261126" + (firstSeq + i - 1).ToString("0000") };
                double min = 0.64 + (i * 7 % 9) / 100.0;       // 0.64 ~ 0.72
                SetOverhang(c, min);
                c.Align = 0.08 + (i * 5 % 13) / 100.0;         // 0.08 ~ 0.20
                tray.Cells.Add(c);
            }
            // 눈여겨볼 셀(기준 슬롯을 shift만큼 옮긴다): 한계값과 같은 값 포함
            Special(tray, shift, 5, c => c.Align = 0.30);                            // 정렬 편차 상한과 같음 → OK
            Special(tray, shift, 9, c => SetOverhang(c, 0.42));                      // 오버행 하한 미만 → NG
            Special(tray, shift, 12, c => SetOverhang(c, 0.56));                     // 경계 → 재판정 대기
            Special(tray, shift, 21, c => c.Align = 0.36);                           // 정렬 상한 초과 → NG
            Special(tray, shift, 27, c => c.Foreign = true);                         // 이물 → 재판정 대기
            Special(tray, shift, 31, c => SetOverhang(c, 0.50));                     // 하한과 같음 → 경계(재판정 대기)
            Special(tray, shift, 38, c => SetOverhang(c, 0.60));                     // 경계 상한과 같음 → 재판정 대기
            Special(tray, shift, 44, c => SetOverhang(c, 0.47));                     // 하한 미만 → NG
            foreach (var c in tray.Cells) CellJudge.Apply(c);
            // 17번: 경계였다가 작업자가 이미 재판정 OK
            Special(tray, shift, 17, c => SetOverhang(c, 0.58));
            var done = tray.Cells[(17 - 1 + shift) % TrayData.CellCount];
            { var c = done; c.State = CellState.Ok; c.Rejudged = true; c.Reason = "이미지 확인 · 이상 없음"; }
            return tray;
        }

        private static void Special(TrayData t, int shift, int baseSlot, Action<CellRecord> edit, bool apply = true)
        {
            var c = t.Cells[(baseSlot - 1 + shift) % TrayData.CellCount];
            edit(c);
            if (apply) CellJudge.Apply(c);
        }

        private static void SetOverhang(CellRecord c, double min)
        {
            // 네 곳 중 한 곳이 최소값, 나머지는 조금 더 큼
            int at = c.Slot % CellRecord.OverheadPoints;
            double[] add = { 0.04, 0.07, 0.02, 0.05 };
            for (int p = 0; p < CellRecord.OverheadPoints; p++) c.Overhang[p] = Math.Round(min + (p == at ? 0 : add[p]), 2);
        }
    }
}
