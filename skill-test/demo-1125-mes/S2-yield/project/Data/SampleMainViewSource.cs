using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 수량·수율은 서로 맞춘다(Bin1 / 진행 = 최종 수율).</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        // 불량 497개 기준 Hard Bin 2~8 가중치. Lot마다 불량 수에 맞춰 나눈다.
        private static readonly string[] HardNames = { "양품", "Open/Short", "누설·전력", "기능", "타이밍", "아날로그", "메모리", "기타" };
        private static readonly int[] HardWeights = { 0, 148, 96, 82, 71, 55, 30, 15 };
        private const int WeightTotal = 497;

        // Soft Bin, 원인, 매핑 Hard Bin, 가중치(Hard Bin 가중치 안에 든다)
        private static readonly object[][] SoftDefs =
        {
            new object[] { 23, "접촉 저항 과다", 2, 120 },
            new object[] { 41, "누설 전류 초과", 3, 68 },
            new object[] { 17, "기능 패턴 오류", 4, 59 },
            new object[] { 52, "타이밍 마진 부족", 5, 47 },
            new object[] { 8, "소비 전력 초과", 3, 28 },
        };

        private readonly List<TesterInfo> _testers = new List<TesterInfo>
        {
            new TesterInfo("T01", StatusKind.Run, "테스트 중", "L2611-0431", "CUS-021", "MCU-LQFP64", "FT_MCU_v2.8.0", 25.0, 12000, 7350, 96.9, 97.4),
            new TesterInfo("T02", StatusKind.Run, "테스트 중", "L2611-0432", "CUS-017", "PMIC-QFN32", "FT_PMIC_v3.2.1", 85.0, 10000, 4980, 96.1, 96.8),
            new TesterInfo("T03", StatusKind.Stop, "대기", "", "", "", "", 0, 0, 0, 0, 0),
            new TesterInfo("T04", StatusKind.Alarm, "알람", "L2611-0425", "CUS-017", "PMIC-QFN32", "FT_PMIC_v3.2.1", 85.0, 10000, 8420, 92.6, 94.1),
            new TesterInfo("T05", StatusKind.Warn, "테스트 중", "L2611-0433", "CUS-009", "DDR-FBGA96", "FT_DDR_v1.9.4", 25.0, 8000, 2150, 94.3, 95.0),
            new TesterInfo("T06", StatusKind.Run, "테스트 중", "L2611-0434", "CUS-021", "MCU-LQFP64", "FT_MCU_v2.8.0", 25.0, 12000, 3900, 96.6, 97.1),
        };

        public IList<TesterInfo> GetTesters() { return _testers; }

        public IList<BinRow> GetHardBins(TesterInfo t)
        {
            var rows = new List<BinRow>();
            if (!t.HasLot) return rows;
            int good = (int)Math.Round(t.Progress * t.FinalYield / 100.0);
            int fail = t.Progress - good;
            int used = 0;
            var counts = new int[8];
            for (int i = 2; i <= 8; i++) { counts[i - 1] = fail * HardWeights[i - 1] / WeightTotal; used += counts[i - 1]; }
            counts[1] += fail - used;   // 나머지는 Bin 2
            counts[0] = good;
            for (int i = 0; i < 8; i++)
                rows.Add(new BinRow(i + 1, HardNames[i], counts[i], 100.0 * counts[i] / t.Progress, i + 1));
            return rows;
        }

        public IList<BinRow> GetSoftTop(TesterInfo t, int count)
        {
            var rows = new List<BinRow>();
            if (!t.HasLot) return rows;
            int fail = t.Progress - (int)Math.Round(t.Progress * t.FinalYield / 100.0);
            foreach (var d in SoftDefs)
            {
                int n = fail * (int)d[3] / WeightTotal;
                rows.Add(new BinRow((int)d[0], (string)d[1], n, 100.0 * n / t.Progress, (int)d[2]));
            }
            rows.Sort((a, b) => b.Count.CompareTo(a.Count));
            return rows.GetRange(0, Math.Min(count, rows.Count));
        }

        public IList<LotResult> GetHistory()
        {
            return new List<LotResult>
            {
                new LotResult("2026-11-25 09:42:18", "L2611-0430", "CUS-021", "MCU-LQFP64", "T06", 12000, 96.5, 97.2, ""),
                new LotResult("2026-11-25 07:15:03", "L2611-0429", "CUS-009", "DDR-FBGA96", "T05", 8000, 94.9, 96.0, ""),
                new LotResult("2026-11-24 22:48:51", "L2611-0428", "CUS-017", "PMIC-QFN32", "T04", 10000, 91.7, 94.6, "추가 검사 중"),
                new LotResult("2026-11-24 19:30:27", "L2611-0427", "CUS-021", "MCU-LQFP64", "T01", 12000, 95.8, 96.5, ""),
                new LotResult("2026-11-24 17:50:31", "L2611-0424", "CUS-033", "ADC-SOIC16", "T03", 5000, 96.0, 96.8, ""),
                new LotResult("2026-11-24 16:05:40", "L2611-0426", "CUS-009", "DDR-FBGA96", "T05", 8000, 94.1, 95.0, ""),
                new LotResult("2026-11-24 06:58:12", "L2611-0422", "CUS-021", "MCU-LQFP64", "T06", 12000, 94.7, 95.9, ""),
                new LotResult("2026-11-23 23:20:44", "L2611-0421", "CUS-017", "PMIC-QFN32", "T04", 10000, 92.9, 94.3, "Release (고객 승인)"),
                new LotResult("2026-11-23 14:26:47", "L2611-0418", "CUS-021", "MCU-LQFP64", "T01", 12000, 93.5, 93.9, "Scrap"),
            };
        }
    }
}
