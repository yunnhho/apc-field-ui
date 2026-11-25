using System.Collections.Generic;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 수율 = Hard Bin 1 / 테스트 Unit, Hard Bin 합 = 테스트 Unit, Soft Bin ≤ 매핑된 Hard Bin.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        // Soft Bin → Hard Bin 매핑: 23→2, 41·08→3, 12→4, 57→5, 33→6
        private static SoftBinRow Soft(int soft, int qty)
        {
            switch (soft)
            {
                case 23: return new SoftBinRow(23, 2, "Open/Short", qty);
                case 41: return new SoftBinRow(41, 3, "Leakage IDD", qty);
                case 8: return new SoftBinRow(8, 3, "Vref Out", qty);
                case 12: return new SoftBinRow(12, 4, "Func Fail", qty);
                case 57: return new SoftBinRow(57, 5, "Speed Bin", qty);
                default: return new SoftBinRow(33, 6, "Freq Out", qty);
            }
        }

        private static int[] Bins(int good, params int[] bad)
        {
            var all = new int[8];
            all[0] = good;
            for (int i = 0; i < 7; i++) all[i + 1] = bad[i];
            return all;
        }

        public IList<TesterInfo> GetTesters()
        {
            return new List<TesterInfo>
            {
                new TesterInfo("T01", TesterState.Testing,
                    new LotInfo("FT2611-0412", "ACM", "AX310-QFN48", "v3.2.1", 25.0, 12000, 7400, 96.1,
                        Bins(7208, 60, 48, 30, 24, 15, 10, 5),
                        new[] { Soft(41, 25), Soft(23, 20), Soft(12, 18), Soft(57, 14), Soft(8, 9) }),
                    new[] { 96.2, 96.8, 95.9, 97.1, 96.0, 97.3, 96.7, 96.1, 96.5, 97.0, 97.4 }),
                new TesterInfo("T02", TesterState.Testing,
                    new LotInfo("FT2611-0413", "BTL", "BX220-BGA256", "v2.8.0", 85.0, 8000, 5200, 95.4,
                        Bins(5034, 50, 40, 30, 20, 15, 8, 3),
                        new[] { Soft(41, 22), Soft(23, 18), Soft(12, 15), Soft(57, 11), Soft(33, 9) }),
                    new[] { 96.5, 97.0, 96.2, 96.9, 95.8, 96.6, 97.1, 95.8, 96.4, 96.8 }),
                new TesterInfo("T03", TesterState.Idle, null,
                    new[] { 95.7, 96.4, 96.0, 96.2, 95.3, 96.6, 94.8, 96.9 }),
                new TesterInfo("T04", TesterState.Alarm,
                    new LotInfo("FT2611-0409", "CNX", "CX540-QFN64", "v3.2.1", 125.0, 10000, 10000, 91.8,
                        Bins(9410, 210, 140, 95, 70, 40, 25, 10),
                        new[] { Soft(23, 170), Soft(41, 95), Soft(12, 60), Soft(57, 45), Soft(8, 35) }),
                    new[] { 96.0, 96.4, 95.8, 96.2, 95.5, 96.1, 95.3, 94.6, 94.1 }),
                new TesterInfo("T05", TesterState.Testing,
                    new LotInfo("FT2611-0414", "ACM", "AX310-QFN48", "v3.2.1", 25.0, 6000, 3000, 93.4,
                        Bins(2850, 48, 38, 26, 18, 12, 6, 2),
                        new[] { Soft(41, 30), Soft(23, 25), Soft(12, 20), Soft(57, 14), Soft(8, 6) }),
                    new[] { 95.9, 96.3, 95.2, 96.1, 95.5, 95.4, 95.0 }),
                new TesterInfo("T06", TesterState.Testing,
                    new LotInfo("FT2611-0415", "DLT", "DX110-SOP16", "v1.9.4", 25.0, 20000, 12500, 94.8,
                        Bins(12013, 150, 120, 80, 60, 40, 25, 12),
                        new[] { Soft(41, 70), Soft(23, 60), Soft(12, 48), Soft(57, 35), Soft(8, 30) }),
                    new[] { 96.4, 96.0, 96.8, 95.9, 96.5, 96.0, 96.3, 96.1 }),
            };
        }

        public IList<HistoryRow> GetHistory()
        {
            return new List<HistoryRow>
            {
                new HistoryRow("13:42", "FT2611-0409", "CNX", "CX540-QFN64", "T04", 10000, 91.8, 94.1, "Hold"),
                new HistoryRow("13:15", "FT2611-0411", "ACM", "AX310-QFN48", "T01", 12000, 95.6, 97.0, ""),
                new HistoryRow("12:50", "FT2611-0410", "DLT", "DX110-SOP16", "T06", 20000, 94.9, 96.3, ""),
                new HistoryRow("12:21", "FT2611-0408", "BTL", "BX220-BGA256", "T02", 8000, 95.1, 96.4, ""),
                new HistoryRow("11:57", "FT2611-0407", "CNX", "CX540-QFN64", "T04", 9500, 91.4, 94.6, "Release 승인"),
                new HistoryRow("11:30", "FT2611-0406", "ACM", "AX310-QFN48", "T05", 6000, 93.8, 95.4, ""),
                new HistoryRow("11:02", "FT2611-0405", "CNX", "CX540-QFN64", "T04", 12000, 94.0, 95.3, ""),
                new HistoryRow("10:38", "FT2611-0404", "BTL", "BX220-BGA256", "T03", 8000, 95.7, 96.9, ""),
                new HistoryRow("10:05", "FT2611-0403", "ACM", "AX310-QFN48", "T01", 12000, 94.9, 96.5, ""),
                new HistoryRow("09:41", "FT2611-0402", "DLT", "DX110-SOP16", "T06", 20000, 95.4, 96.0, ""),
                new HistoryRow("08:44", "FT2611-0400", "DLT", "DX110-SOP16", "T03", 15000, 93.9, 94.8, "Scrap"),
            };
        }

        public void SaveOcapDecision(string lotId, string decision, string reason) { }
    }
}
