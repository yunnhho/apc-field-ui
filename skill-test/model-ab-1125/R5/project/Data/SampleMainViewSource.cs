using System;
using System.Collections.Generic;
using System.Linq;
using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>
    /// 화면 확인·render용 샘플 데이터. 심어 둔 원인: T04를 지난 Lot만 Soft Bin 23(Contact Open)이 높고,
    /// 현재 Lot FT2611-0387은 최종 수율 93.8 %로 기준 미달 → Hold. FT2611-0384는 95.0 %(하한과 같음 → 합격).
    /// </summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const string HoldLot = "FT2611-0387";

        // Hard Bin 2~8 불량 비율 가중치(진행 중 Lot 샘플 분포)
        private static readonly int[] DefectWeights = { 22, 15, 18, 10, 14, 12, 9 };

        // Hard Bin 2~8에 매핑되는 대표 Soft Bin(진행 중 Lot 샘플)
        private static readonly SoftBinCount[] SoftBinOfHardBin =
        {
            new SoftBinCount(41, "Leakage High", 2, 0),
            new SoftBinCount(17, "IDD Over", 3, 0),
            new SoftBinCount(8, "Continuity Short", 4, 0),
            new SoftBinCount(31, "Function Fail", 5, 0),
            new SoftBinCount(55, "Vth Low", 6, 0),
            new SoftBinCount(62, "Tpd Slow", 7, 0),
            new SoftBinCount(90, "기타", 8, 0),
        };

        private static readonly Dictionary<string, double[]> RecentYields = new Dictionary<string, double[]>
        {
            { "T01", new[] { 97.6, 98.1, 97.4, 97.8, 98.0, 97.2, 97.7, 98.2, 97.9, 97.3 } },
            { "T02", new[] { 96.8, 97.3, 96.5, 97.1, 96.6, 97.4, 96.9, 97.2, 97.0, 96.9 } },
            { "T03", new[] { 98.4, 98.8, 98.2, 98.6, 99.0, 98.5, 98.7, 98.3, 98.6, 98.9 } },
            { "T04", new[] { 97.1, 96.8, 97.3, 96.9, 97.0, 96.6, 96.1, 95.7, 95.0, 93.8 } },
            { "T05", new[] { 98.2, 98.6, 98.0, 98.5, 98.8, 98.1, 98.3, 98.9, 98.4, 98.7 } },
            { "T06", new[] { 96.9, 97.4, 96.7, 97.1, 96.5, 97.3, 96.8, 97.0, 96.6, 97.2 } },
        };

        public IList<TesterSummary> GetTesters()
        {
            return new[] { "T01", "T02", "T03", "T04", "T05", "T06" }.Select(Summary).ToList();
        }

        private TesterSummary Summary(string tester)
        {
            var lot = GetCurrentLot(tester);
            if (!lot.HasLot) return new TesterSummary(tester, TesterState.Waiting, null);
            if (lot.FinalYield.HasValue)
                return new TesterSummary(tester, YieldRule.IsBelowSpec(lot.FinalYield.Value) ? TesterState.Alarm : TesterState.Waiting, lot.FinalYield);
            return new TesterSummary(tester, TesterState.Testing, lot.FirstYield);
        }

        public LotDetail GetCurrentLot(string tester)
        {
            switch (tester)
            {
                case "T01": return Testing("T01", "FT2611-0388", "A12", "LP5X-16G", "LP5X_FT_v3.2.1", 85.0, 12000, 7412, 7249);
                case "T02": return Testing("T02", "FT2611-0389", "B07", "PMIC-7330", "PM7330_FT_v1.8.0", 125.0, 8000, 3906, 3765);
                case "T03": return Testing("T03", "FT2611-0390", "C31", "MCU-F48", "MCUF48_FT_v2.0.4", 25.0, 15000, 11280, 11122);
                case "T04": return HoldLotDetail();
                case "T06": return Testing("T06", "FT2611-0391", "B07", "PMIC-7330", "PM7330_FT_v1.8.0", -40.0, 8000, 1215, 1165);
                default:
                    return new LotDetail(tester, "", "", "", "", 0.0, 0, 0, null, null, new int[8], new List<SoftBinCount>(), RecentYields[tester]);
            }
        }

        // T04 현재 Lot: 재테스트까지 끝남. 최초 9,120 → 재테스트 후 Bin1 9,380(최종 93.8 %). Bin4 341 = SB23 310 + SB08 31.
        private static LotDetail HoldLotDetail()
        {
            var hardBins = new[] { 9380, 85, 52, 341, 38, 47, 31, 26 };
            var top = new List<SoftBinCount>
            {
                new SoftBinCount(23, "Contact Open", 4, 310),
                new SoftBinCount(41, "Leakage High", 2, 62),
                new SoftBinCount(17, "IDD Over", 3, 44),
                new SoftBinCount(55, "Vth Low", 6, 40),
                new SoftBinCount(8, "Continuity Short", 4, 31),
            };
            return new LotDetail("T04", HoldLot, "A12", "LP5X-16G", "LP5X_FT_v3.2.1", 85.0, 10000, 10000,
                YieldRule.Rate(9120, 10000), YieldRule.Rate(hardBins[0], 10000), hardBins, top, RecentYields["T04"]);
        }

        private static LotDetail Testing(string tester, string lotId, string customer, string product, string program, double temp,
            int input, int tested, int good)
        {
            var bins = new int[8];
            bins[0] = good;
            int defects = tested - good, used = 0;
            for (int i = 0; i < DefectWeights.Length; i++)
            {
                bins[i + 1] = defects * DefectWeights[i] / 100;
                used += bins[i + 1];
            }
            bins[1] += defects - used;   // 나머지는 Bin2에

            var top = SoftBinOfHardBin
                .Select(s => new SoftBinCount(s.SoftBin, s.Name, s.HardBin, bins[s.HardBin - 1] * 4 / 5))
                .OrderByDescending(s => s.Count).Take(5).ToList();
            return new LotDetail(tester, lotId, customer, product, program, temp, input, tested,
                YieldRule.Rate(good, tested), null, bins, top, RecentYields[tester]);
        }

        public IList<LotResult> GetLotHistory()
        {
            var d = new DateTime(2026, 11, 26);
            return new List<LotResult>
            {
                new LotResult(d.AddHours(14).AddMinutes(20).AddSeconds(31), "T04", HoldLot, "A12", "LP5X-16G", 10000, 91.2, 93.8, 23, 3.1),
                new LotResult(d.AddHours(13).AddMinutes(47).AddSeconds(12), "T05", "FT2611-0386", "C31", "MCU-F48", 15000, 98.1, 98.7, 31, 0.4),
                new LotResult(d.AddHours(12).AddMinutes(58).AddSeconds(40), "T02", "FT2611-0385", "B07", "PMIC-7330", 8000, 95.6, 96.9, 41, 0.9),
                new LotResult(d.AddHours(12).AddMinutes(5).AddSeconds(9), "T04", "FT2611-0384", "A12", "LP5X-16G", 10000, 93.1, 95.0, 23, 1.6),
                new LotResult(d.AddHours(11).AddMinutes(32).AddSeconds(55), "T06", "FT2611-0383", "B07", "PMIC-7330", 8000, 96.0, 97.2, 17, 0.6),
                new LotResult(d.AddHours(10).AddMinutes(41).AddSeconds(18), "T01", "FT2611-0382", "A12", "LP5X-16G", 12000, 96.9, 97.3, 55, 0.5),
                new LotResult(d.AddHours(9).AddMinutes(56).AddSeconds(3), "T04", "FT2611-0381", "A12", "LP5X-16G", 10000, 94.0, 95.7, 23, 1.2),
                new LotResult(d.AddHours(9).AddMinutes(12).AddSeconds(47), "T03", "FT2611-0380", "C31", "MCU-F48", 15000, 98.3, 98.9, 31, 0.3),
                new LotResult(d.AddHours(8).AddMinutes(20).AddSeconds(36), "T05", "FT2611-0379", "C31", "MCU-F48", 15000, 97.9, 98.4, 62, 0.5),
                new LotResult(d.AddHours(7).AddMinutes(44).AddSeconds(10), "T04", "FT2611-0378", "A12", "LP5X-16G", 10000, 95.1, 96.1, 23, 0.9),
                new LotResult(d.AddHours(6).AddMinutes(58).AddSeconds(22), "T02", "FT2611-0376", "B07", "PMIC-7330", 8000, 96.2, 97.0, 41, 0.7),
                new LotResult(d.AddHours(6).AddMinutes(15).AddSeconds(51), "T01", "FT2611-0375", "A12", "LP5X-16G", 12000, 97.0, 97.9, 8, 0.4),
                new LotResult(d.AddHours(5).AddMinutes(27).AddSeconds(4), "T06", "FT2611-0374", "B07", "PMIC-7330", 8000, 95.8, 96.6, 55, 0.8),            };
        }

        public IList<OcapStep> GetOcapSteps(string lotId)
        {
            return new List<OcapStep>
            {
                new OcapStep("이탈 알람", StatusKind.Run, "완료", "14:20:31", "최종 수율 93.8 % (하한 95.0)", true),
                new OcapStep("Lot Hold", StatusKind.Run, "완료", "14:20:35", "MES Hold 등록", true),
                new OcapStep("장비 사용 중지", StatusKind.Run, "완료", "14:22:10", "T04 Lot 투입 중지", true),
                new OcapStep("원인 확인", StatusKind.Warn, "확인 중", "14:31:44", "SB23 Contact Open · Probe 접촉부 점검 중", false),
                new OcapStep("고객 승인", StatusKind.Stop, "회신 대기", "14:33:05", "A12 Lot 처리 승인 요청", false),
            };
        }

        public IList<SoftBinCount> GetSoftBinTop(string lotId)
        {
            return HoldLotDetail().SoftBinTop;
        }

        public void SaveOcapDecision(string lotId, string decision, string reason)
        {
            // 샘플: 저장하지 않는다. 실제 구현은 MES에 판정·사유를 기록한다.
        }
    }
}
