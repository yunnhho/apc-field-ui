using System;
using System.Collections.Generic;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 수량·수율·Bin이 서로 맞는다(Bin 합 = 테스트 수량).</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        // 불량 Hard Bin 2~8이 불량 수량을 나눠 갖는 비율(%). 나머지는 Bin 2로.
        private static readonly int[] HardWeights = { 26, 22, 17, 14, 10, 6, 5 };
        // 상위 Soft Bin 후보: 번호, 이름, 속한 Hard Bin, 그 Hard Bin 중 비율(%) — { T04 외, T04 }
        private static readonly object[][] SoftTemplates =
        {
            new object[] { 23, "Idd 과다", 2, 18, 86 },
            new object[] { 58, "Vdd 편차", 2, 22, 10 },
            new object[] { 41, "Open/Short", 3, 53, 53 },
            new object[] { 7, "Func 불량", 4, 55, 55 },
            new object[] { 12, "속도 미달", 5, 60, 60 },
        };
        private static readonly double[] TrendWave = { 0.3, -0.2, 0.6, 0.1, -0.4, 0.4, 0.2, -0.1, 0.5, 0.0, -0.3 };

        public TesterSnapshot[] GetTesters()
        {
            return new[]
            {
                new TesterSnapshot("T01", MakeLot("FT2511-0408", "ACM", "LPDDR5-8Gb", "v3.2.1", 85.0, 10000, 6310, 6058, 6106, false)),
                new TesterSnapshot("T02", MakeLot("FT2511-0409", "BTX", "PMIC-3304", "v1.8.0", 25.0, 8000, 3950, 3810, 3842, false)),
                new TesterSnapshot("T03", null),
                new TesterSnapshot("T04", MakeLot("FT2511-0412", "CRL", "MCU-A32", "v2.4.0", 125.0, 10000, 7420, 6856, 6982, true)),
                new TesterSnapshot("T05", MakeLot("FT2511-0410", "ACM", "LPDDR5-8Gb", "v3.2.1", 85.0, 10000, 9100, 8719, 8740, false)),
                new TesterSnapshot("T06", MakeLot("FT2511-0411", "BTX", "PMIC-3304", "v1.8.0", 25.0, 8000, 1205, 1160, 1170, false)),
            };
        }

        public LotResultRow[] GetRecentLotResults()
        {
            DateTime d = new DateTime(2026, 11, 25);
            return new[]
            {
                Result(d, 14, 21, 8, "T05", "FT2511-0403", "ACM", "LPDDR5-8Gb", 10000, 9540, 9631),
                Result(d, 13, 48, 52, "T02", "FT2511-0402", "BTX", "PMIC-3304", 8000, 7560, 7603),   // 최종 95.0 = 기준과 같음 → 통과
                Result(d, 13, 12, 30, "T04", "FT2511-0401", "CRL", "MCU-A32", 10000, 9502, 9590),
                Result(d, 12, 40, 15, "T01", "FT2511-0400", "ACM", "LPDDR5-8Gb", 10000, 9602, 9657),
                Result(d, 12, 5, 44, "T06", "FT2511-0399", "BTX", "PMIC-3304", 8000, 7688, 7730),
                Result(d, 11, 31, 9, "T05", "FT2511-0398", "ACM", "LPDDR5-8Gb", 10000, 9577, 9702),
                Result(d, 10, 58, 36, "T02", "FT2511-0397", "BTX", "PMIC-3304", 8000, 7598, 7640),
                Result(d, 10, 12, 2, "T03", "FT2511-0396", "CRL", "MCU-A32", 10000, 9371, 9448),
                Result(d, 9, 40, 27, "T01", "FT2511-0395", "ACM", "LPDDR5-8Gb", 10000, 9611, 9689),
                Result(d, 9, 2, 51, "T06", "FT2511-0394", "BTX", "PMIC-3304", 8000, 7562, 7688),
                Result(d, 8, 21, 33, "T04", "FT2511-0393", "CRL", "MCU-A32", 10000, 9577, 9640),
            };
        }

        public void SubmitLotDecision(string lotId, string decision, string reason)
        {
            // 샘플: 기록하지 않는다. 실제 구현은 MES에 판정·사유를 저장하고 장비 재가동 승인 단계로 넘긴다.
        }

        private static LotResultRow Result(DateTime day, int h, int m, int s, string tester, string lotId, string customer,
            string product, int input, int firstGood, int finalGood)
        {
            double first = 100.0 * firstGood / input, final = 100.0 * finalGood / input;
            string disposition = YieldRule.JudgeFinal(final) == APC.UI.Theming.StatusKind.Run ? "기준 이상" : "기준 미달 · Release";
            return new LotResultRow(day.AddHours(h).AddMinutes(m).AddSeconds(s), tester, lotId, customer, product, input, first, final, disposition);
        }

        private static LotInfo MakeLot(string lotId, string customer, string product, string program, double temp,
            int input, int tested, int firstGood, int finalGood, bool isSoftBin23Hot)
        {
            int fail = tested - finalGood;
            var hard = new int[8];
            hard[0] = finalGood;
            int used = 0;
            for (int i = 0; i < HardWeights.Length; i++) { hard[i + 1] = fail * HardWeights[i] / 100; used += hard[i + 1]; }
            hard[1] += fail - used;

            var soft = new List<SoftBinRow>();
            foreach (var t in SoftTemplates)
            {
                int hb = (int)t[2];
                int share = (int)t[isSoftBin23Hot ? 4 : 3];
                soft.Add(new SoftBinRow((int)t[0], (string)t[1], hb, hard[hb - 1] * share / 100));
            }
            soft.Sort((a, b) => b.Qty.CompareTo(a.Qty));

            double finalYield = 100.0 * finalGood / tested;
            var trend = new double[12];
            for (int i = 0; i < 11; i++) trend[i] = YieldRule.Round1(96.6 + TrendWave[i]);
            trend[11] = YieldRule.Round1(finalYield);
            return new LotInfo(lotId, customer, product, program, temp, input, tested, firstGood, hard, soft.ToArray(), trend);
        }
    }
}
