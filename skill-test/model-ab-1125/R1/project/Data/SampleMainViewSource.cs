using System;
using System.Collections.Generic;
using System.Linq;
using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 양품 수·Bin·Soft Bin·추이 끝값이 서로 맞는다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly string[] HardNames = { "양품", "Open/Short", "누설 전류", "속도 불량", "기능 불량", "전원 불량", "마진 불량", "기타" };
        // 불량(Bin 2~8) 배분 비중. T04는 누설 전류(Bin 3)가 높다.
        private static readonly int[] NormalWeights = { 220, 70, 110, 90, 40, 30, 30 };
        private static readonly int[] T04Weights = { 210, 150, 120, 70, 20, 10, 10 };

        // Soft Bin, Hard Bin, 원인, 해당 Hard Bin 안 비중(%)
        private static readonly object[][] SoftMap =
        {
            new object[] { 23, 3, "IDD 누설", 85 }, new object[] { 41, 2, "단락 DC", 45 }, new object[] { 17, 2, "개방 DC", 40 },
            new object[] { 35, 4, "Fmax 미달", 65 }, new object[] { 52, 5, "기능 패턴", 77 }, new object[] { 12, 6, "Vdd 전압", 60 },
            new object[] { 58, 7, "타이밍 마진", 70 },
        };

        public IList<TesterInfo> GetTesters()
        {
            return new List<TesterInfo>
            {
                Tester("T01", StatusKind.Run, "테스트 중", Lot("L2611-0401", "CU-014", "LPDDR5 8Gb", "FT_LP5_v3.2.1", 85.0m, 10000, 6400, 6202, 6259, NormalWeights,
                    new[] { 96.8, 97.2, 96.5, 97.0, 96.9, 97.4, 96.7, 97.1, 96.6, 97.3, 96.9 })),
                Tester("T02", StatusKind.Run, "테스트 중", Lot("L2611-0398", "CU-027", "NOR Flash 128Mb", "FT_NOR_v2.0.4", 25.0m, 8000, 5200, 4976, 5013, NormalWeights,
                    new[] { 96.0, 96.8, 95.9, 96.3, 96.7, 96.1, 96.5, 95.8, 96.6, 96.2, 96.9 })),
                new TesterInfo("T03", StatusKind.Stop, "대기", null),
                Tester("T04", StatusKind.Alarm, "알람", Lot("L2611-0412", "CU-014", "LPDDR5 8Gb", "FT_LP5_v3.2.1", 85.0m, 10000, 10000, 9230, 9410, T04Weights,
                    new[] { 96.6, 97.1, 96.4, 96.9, 96.2, 95.8, 96.0, 95.3, 95.6, 95.1, 95.4 })),
                Tester("T05", StatusKind.Run, "테스트 중", Lot("L2611-0405", "CU-031", "MCU M33 512K", "FT_MCU_v1.8.0", 125.0m, 12000, 7200, 6782, 6840, NormalWeights,
                    new[] { 95.8, 96.2, 95.5, 96.0, 95.7, 96.4, 95.6, 95.9, 96.1, 95.4, 95.7 })),
                Tester("T06", StatusKind.Run, "테스트 중", Lot("L2611-0402", "CU-014", "LPDDR5 8Gb", "FT_LP5_v3.2.1", 85.0m, 10000, 3100, 2951, 2979, NormalWeights,
                    new[] { 96.7, 97.0, 96.3, 96.8, 97.2, 96.5, 96.9, 96.4, 97.1, 96.6, 96.8 })),
            };
        }

        public IList<LotResultRow> GetRecentResults()
        {
            var t = new DateTime(2026, 11, 25, 14, 12, 40);
            return new List<LotResultRow>
            {
                Row(t, "L2611-0412", "T04", "CU-014", "LPDDR5 8Gb", 10000, 92.3m, 94.1m, "Hold"),
                Row(t.AddMinutes(-58).AddSeconds(17), "L2611-0399", "T02", "CU-027", "NOR Flash 128Mb", 8000, 95.4m, 96.7m, ""),
                Row(t.AddMinutes(-131).AddSeconds(-9), "L2611-0403", "T06", "CU-014", "LPDDR5 8Gb", 10000, 95.0m, 96.2m, ""),
                Row(t.AddMinutes(-190).AddSeconds(31), "L2611-0397", "T05", "CU-031", "MCU M33 512K", 12000, 93.8m, 95.0m, ""),
                Row(t.AddMinutes(-262).AddSeconds(-24), "L2611-0396", "T01", "CU-014", "LPDDR5 8Gb", 10000, 96.1m, 97.0m, ""),
                Row(t.AddMinutes(-340).AddSeconds(8), "L2611-0394", "T04", "CU-031", "MCU M33 512K", 12000, 90.6m, 93.2m, "추가 검사"),
                Row(t.AddMinutes(-415).AddSeconds(-41), "L2611-0393", "T03", "CU-027", "NOR Flash 128Mb", 8000, 95.9m, 96.4m, ""),
                Row(t.AddDays(-1).AddHours(-1).AddMinutes(-7), "L2611-0391", "T02", "CU-027", "NOR Flash 128Mb", 8000, 94.9m, 95.8m, ""),
                Row(t.AddDays(-1).AddHours(-3).AddMinutes(-26), "L2611-0390", "T06", "CU-014", "LPDDR5 8Gb", 10000, 96.5m, 97.3m, ""),
                Row(t.AddDays(-1).AddHours(-5).AddMinutes(-52), "L2611-0388", "T01", "CU-031", "MCU M33 512K", 12000, 94.0m, 95.6m, ""),
                Row(t.AddDays(-1).AddHours(-8).AddMinutes(-3), "L2611-0386", "T05", "CU-014", "LPDDR5 8Gb", 10000, 92.7m, 94.8m, "Scrap"),
            };
        }

        public void RecordDisposition(string lotId, string disposition, string reason)
        {
            // 샘플: 기록하지 않는다. 실제 구현은 MES에 판정·사유·시각을 남긴다.
        }

        private static TesterInfo Tester(string id, StatusKind process, string text, LotInfo lot)
        {
            return new TesterInfo(id, process, text, lot);
        }

        private static LotResultRow Row(DateTime end, string lot, string tester, string customer, string product, int input, decimal first, decimal fin, string disposition)
        {
            return new LotResultRow(end, lot, tester, customer, product, input, first, fin, disposition);
        }

        private static LotInfo Lot(string lotId, string customer, string product, string program, decimal temp, int input, int tested,
            int goodFirst, int goodFinal, int[] weights, double[] previousYields)
        {
            int fail = tested - goodFinal;
            int total = weights.Sum();
            var counts = new int[7];
            int used = 0;
            for (int i = 1; i < 7; i++) { counts[i] = (int)Math.Round(fail * (double)weights[i] / total); used += counts[i]; }
            counts[0] = fail - used;

            var bins = new List<HardBinRow> { new HardBinRow(1, HardNames[0], goodFinal) };
            for (int i = 0; i < 7; i++) bins.Add(new HardBinRow(i + 2, HardNames[i + 1], counts[i]));

            var soft = new List<SoftBinRow>();
            foreach (var m in SoftMap)
            {
                int hard = (int)m[1];
                int n = (int)Math.Round(bins[hard - 1].Count * (int)m[3] / 100.0);
                if (n > 0) soft.Add(new SoftBinRow((int)m[0], hard, (string)m[2], n));
            }
            var top = soft.OrderByDescending(s => s.Count).Take(5).ToList();

            var trend = new List<double>(previousYields);
            trend.Add((double)YieldJudge.Round(goodFinal * 100m / tested));
            return new LotInfo(lotId, customer, product, program, temp, input, tested, goodFirst, bins, top, trend);
        }
    }
}
