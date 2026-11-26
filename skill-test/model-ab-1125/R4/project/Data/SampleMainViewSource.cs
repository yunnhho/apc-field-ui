using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 수량·수율·Bin·추이가 서로 맞는다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly DateTime Day = new DateTime(2026, 11, 26);
        private readonly List<TesterInfo> _testers;
        private readonly List<LotRecord> _history;

        public SampleMainViewSource()
        {
            _testers = new List<TesterInfo>
            {
                // Bin1(양품) = 최종 양품 수. 나머지 Bin 합 = 테스트 수량 - 양품. Soft Bin 수량은 속한 Hard Bin 수량 이하.
                new TesterInfo("T01", StatusKind.Run,
                    new LotRecord("FT261101", "C-AXS", "PMIC-A2", "v3.12.4", "T01", 25.0, 12000, 8400, 8090, 8180, null, "테스트 중"),
                    new[] { 8180, 60, 40, 35, 30, 25, 20, 10 },
                    new[] { Soft(11, 2, 40, 0.5), Soft(5, 3, 30, 0.4), Soft(23, 4, 26, 0.3), Soft(18, 5, 22, 0.3), Soft(40, 6, 19, 0.2) },
                    new[] { 96.8, 97.1, 96.5, 97.4, 96.9, 97.0, 97.6, 96.7, 97.2, 97.3, 97.0, 97.4 }),
                new TesterInfo("T02", StatusKind.Run,
                    new LotRecord("FT261102", "C-BTR", "MCU-B7", "v2.08.1", "T02", 85.0, 10000, 5500, 5230, 5300, null, "테스트 중"),
                    new[] { 5300, 70, 45, 30, 25, 15, 10, 5 },
                    new[] { Soft(11, 2, 38, 0.5), Soft(5, 3, 28, 0.4), Soft(23, 4, 17, 0.3), Soft(18, 5, 14, 0.3), Soft(40, 6, 9, 0.2) },
                    new[] { 96.2, 95.8, 96.5, 96.0, 95.6, 96.4, 95.9, 96.1, 96.7, 95.7, 96.3, 96.4 }),
                new TesterInfo("T03", StatusKind.Stop, null, new int[0], new SoftBinRow[0], new double[0]),
                new TesterInfo("T04", StatusKind.Alarm,
                    new LotRecord("FT261104", "C-AXS", "PMIC-A2", "v3.12.4", "T04", 85.0, 10000, 10000, 9310, 9410, Day.AddHours(13).AddMinutes(48).AddSeconds(20), "Hold"),
                    new[] { 9410, 90, 70, 260, 60, 40, 40, 30 },
                    new[] { Soft(23, 4, 230, 0.3), Soft(11, 2, 62, 0.5), Soft(5, 3, 58, 0.4), Soft(18, 5, 48, 0.3), Soft(31, 4, 30, 0.2) },
                    new[] { 96.0, 96.4, 95.8, 96.1, 95.7, 95.4, 95.6, 95.0, 95.3, 95.1, 94.6, 94.1 }),
                new TesterInfo("T05", StatusKind.Run,
                    new LotRecord("FT261105", "C-DLK", "MEMS-C1", "v1.04.0", "T05", -40.0, 10000, 6000, 5490, 5700, null, "테스트 중"),
                    new[] { 5700, 80, 60, 50, 40, 30, 25, 15 },
                    new[] { Soft(11, 2, 45, 0.5), Soft(5, 3, 36, 0.4), Soft(23, 4, 31, 0.3), Soft(18, 5, 27, 0.3), Soft(40, 6, 16, 0.2) },
                    new[] { 95.6, 96.0, 95.4, 95.8, 96.2, 95.5, 95.1, 95.7, 95.3, 95.9, 95.2, 95.0 }),
                new TesterInfo("T06", StatusKind.Run,
                    new LotRecord("FT261106", "C-BTR", "MCU-B7", "v2.08.1", "T06", 25.0, 12000, 9800, 9520, 9640, null, "테스트 중"),
                    new[] { 9640, 50, 30, 25, 20, 15, 12, 8 },
                    new[] { Soft(11, 2, 30, 0.5), Soft(5, 3, 20, 0.4), Soft(23, 4, 16, 0.3), Soft(18, 5, 14, 0.3), Soft(40, 6, 8, 0.2) },
                    new[] { 98.0, 97.8, 98.3, 98.1, 97.6, 98.2, 98.5, 97.9, 98.0, 98.3, 98.2, 98.4 }),
            };

            _history = new List<LotRecord>
            {
                Done("FT261104", "C-AXS", "PMIC-A2", "v3.12.4", "T04", 85.0, 10000, 9310, 9410, 13, 48, 20, "Hold · 고객 승인 대기"),
                Done("FT261098", "C-DLK", "MEMS-C1", "v1.04.0", "T01", -40.0, 9000, 8640, 8760, 13, 12, 41, "정상"),
                Done("FT261097", "C-DLK", "MEMS-C1", "v1.04.0", "T05", -40.0, 10000, 9420, 9500, 12, 36, 9, "정상"),
                Done("FT261096", "C-BTR", "MCU-B7", "v2.08.1", "T02", 85.0, 12000, 11360, 11490, 11, 58, 52, "정상"),
                Done("FT261095", "C-BTR", "MCU-B7", "v2.08.1", "T06", 25.0, 12000, 11620, 11780, 11, 20, 33, "정상"),
                Done("FT261094", "C-AXS", "PMIC-A2", "v3.12.4", "T04", 85.0, 10000, 9420, 9460, 10, 41, 15, "Release · 고객 승인"),
                Done("FT261093", "C-AXS", "PMIC-A2", "v3.12.4", "T01", 25.0, 8000, 7700, 7820, 9, 57, 4, "정상"),
                Done("FT261092", "C-DLK", "MEMS-C1", "v1.04.0", "T03", -40.0, 6000, 5560, 5620, 9, 14, 46, "Scrap"),
                Done("FT261091", "C-AXS", "PMIC-A2", "v3.12.4", "T06", 25.0, 12000, 11580, 11730, 8, 33, 27, "정상"),
                Done("FT261090", "C-BTR", "MCU-B7", "v2.08.1", "T02", 85.0, 10000, 9490, 9610, 7, 49, 10, "정상"),
                Done("FT261089", "C-DLK", "MEMS-C1", "v1.04.0", "T05", -40.0, 8000, 7480, 7590, 7, 8, 55, "추가 검사"),
            };
        }

        public IList<TesterInfo> GetTesters() { return _testers; }
        public IList<LotRecord> GetLotHistory() { return _history; }

        public OcapCase GetOcapCase(TesterInfo tester)
        {
            // 샘플: 알람·Hold·장비 중지는 끝났고 원인 확인과 고객 승인이 아직 → Release는 막힌다.
            return new OcapCase(tester.Lot, tester.SoftBins, new[]
            {
                new OcapStep("수율 이탈 알람", true, "완료 · 13:48:20"),
                new OcapStep("Lot Hold", true, "완료 · 13:48:25"),
                new OcapStep("장비 사용 중지", true, "완료 · 13:49:02"),
                new OcapStep("원인 확인", false, "미완료"),
                new OcapStep("고객 처리 승인", false, "미완료"),
            });
        }

        public void SaveDisposition(string lotId, string decision, string reason) { }

        private static SoftBinRow Soft(int softNo, int hardNo, int count, double otherAvg) { return new SoftBinRow(softNo, hardNo, count, otherAvg); }

        private static LotRecord Done(string lot, string cust, string prod, string prog, string tester, double temp, int input,
            int firstGood, int finalGood, int h, int m, int s, string disposition)
        {
            return new LotRecord(lot, cust, prod, prog, tester, temp, input, input, firstGood, finalGood, Day.AddHours(h).AddMinutes(m).AddSeconds(s), disposition);
        }
    }
}
