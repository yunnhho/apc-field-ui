using System;
using System.Collections.Generic;
using System.Linq;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>
    /// 화면 확인·render용 샘플 데이터. T04를 지난 Lot만 Soft Bin 23(VOH 하한 이탈)이 높고, 마지막 Lot FT26K0417은 최종 93.6 %로 Hold.
    /// 경계 예: FT26K0414 최종 95.0 %(기준과 같음 → 합격·기준 근접), FT26K0418 96.0 %(경고 시작과 같음 → 기준 근접).
    /// </summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        public const string HoldLotId = "FT26K0417";

        private static readonly DateTime Today = new DateTime(2026, 11, 25);

        private static readonly string[] HardBinNames = { "", "양품", "Open/Short", "누설 전류", "DC 특성", "기능", "AC 특성", "IDD", "기타" };
        // 일반 Lot 불량의 Hard Bin 2~8 비중(%)
        private static readonly int[] HardBinWeights = { 0, 0, 8, 10, 22, 30, 15, 10, 5 };

        private static readonly TesterState[] Testers =
        {
            new TesterState("T01", TesterProcess.Testing, new LotInfo("FT26K0421", "NXA", "QM8250-BGA", "FT_QM8250_v3.12", 85.0, 12000, 7450, 7129, 7256)),
            new TesterState("T02", TesterProcess.Testing, new LotInfo("FT26K0423", "QMC", "SR4410-QFN", "FT_SR4410_v1.08", 25.0, 8000, 2310, 2214, 2262)),
            new TesterState("T03", TesterProcess.Idle, null),
            new TesterState("T04", TesterProcess.Alarm, new LotInfo(HoldLotId, "NXA", "QM8250-BGA", "FT_QM8250_v3.12", 85.0, 10000, 10000, 9212, 9362)),
            new TesterState("T05", TesterProcess.Testing, new LotInfo("FT26K0424", "SLD", "PW7720-LGA", "FT_PW7720_v2.04", -40.0, 6000, 4120, 3866, 3943)),
            new TesterState("T06", TesterProcess.Testing, new LotInfo("FT26K0422", "QMC", "SR4410-QFN", "FT_SR4410_v1.08", 25.0, 8000, 5880, 5590, 5692)),
        };

        private static readonly LotResult[] History =
        {
            Lot(14, 38, 12, "T04", HoldLotId, "NXA", "QM8250-BGA", 10000, 92.1, 93.6, "SB23 VOH 하한 이탈", 3.4),
            Lot(14, 5, 47, "T06", "FT26K0419", "QMC", "SR4410-QFN", 8000, 95.9, 97.6, "SB41 기능 패턴 Fail", 0.8),
            Lot(13, 31, 9, "T02", "FT26K0418", "QMC", "SR4410-QFN", 8000, 94.8, 96.0, "SB41 기능 패턴 Fail", 1.1),
            Lot(12, 52, 40, "T01", "FT26K0416", "NXA", "QM8250-BGA", 12000, 96.3, 98.1, "SB18 IDDQ 상한 이탈", 0.5),
            Lot(12, 10, 3, "T04", "FT26K0414", "NXA", "QM8250-BGA", 10000, 93.9, 95.0, "SB23 VOH 하한 이탈", 1.9),
            Lot(11, 26, 55, "T05", "FT26K0415", "SLD", "PW7720-LGA", 6000, 93.2, 95.4, "SB52 출력 지연", 1.2),
            Lot(10, 48, 21, "T03", "FT26K0413", "HMS", "HM2105-WLCSP", 15000, 96.8, 98.4, "SB07 Open", 0.3),
            Lot(10, 2, 14, "T06", "FT26K0412", "QMC", "SR4410-QFN", 8000, 96.1, 97.8, "SB41 기능 패턴 Fail", 0.7),
            Lot(9, 17, 38, "T04", "FT26K0410", "NXA", "QM8250-BGA", 10000, 94.6, 95.8, "SB23 VOH 하한 이탈", 1.6),
            Lot(8, 40, 5, "T01", "FT26K0411", "NXA", "QM8250-BGA", 12000, 96.6, 98.0, "SB18 IDDQ 상한 이탈", 0.6),
        };

        // 어제 이전 Lot 최종 수율(오래된 것부터) — 추이 앞부분
        private static readonly Dictionary<string, double[]> EarlierYields = new Dictionary<string, double[]>
        {
            { "T01", new[] { 97.9, 98.3, 97.6, 98.0, 97.8 } },
            { "T02", new[] { 97.4, 97.8, 96.9, 97.5, 97.2 } },
            { "T03", new[] { 98.2, 98.5, 98.1, 98.7, 98.3 } },
            { "T04", new[] { 97.8, 97.5, 97.1, 97.3, 96.9 } },
            { "T05", new[] { 96.8, 97.2, 96.4, 96.7, 97.0 } },
            { "T06", new[] { 97.1, 97.6, 97.3, 96.8, 97.4 } },
        };

        public IList<TesterState> GetTesters() { return Testers; }

        public IList<LotResult> GetLotHistory() { return History; }

        public IList<BinCount> GetHardBins(string lotId)
        {
            if (lotId == HoldLotId)
                return Hard(9362, 52, 61, 389, 74, 38, 19, 5);

            var lot = FindLot(lotId);
            int fail = lot.TestedQty - lot.FinalGood, left = fail;
            var counts = new int[8];
            counts[0] = lot.FinalGood;
            for (int b = 2; b <= 7; b++) { counts[b - 1] = fail * HardBinWeights[b] / 100; left -= counts[b - 1]; }
            counts[7] = left;
            return Hard(counts);
        }

        public IList<BinCount> GetSoftBinTop(string lotId, int count)
        {
            if (lotId == HoldLotId)
                return new[]
                {
                    new BinCount(23, "VOH 하한 이탈", 4, 342), new BinCount(41, "기능 패턴 Fail", 5, 61),
                    new BinCount(12, "입력 누설 전류", 3, 49), new BinCount(7, "Open", 2, 33), new BinCount(31, "Setup 시간 위반", 6, 27),
                }.Take(count).ToList();

            var lot = FindLot(lotId);
            int fail = lot.TestedQty - lot.FinalGood;
            return new[]
            {
                new BinCount(41, "기능 패턴 Fail", 5, fail * 22 / 100), new BinCount(18, "IDDQ 상한 이탈", 4, fail * 14 / 100),
                new BinCount(12, "입력 누설 전류", 3, fail * 9 / 100), new BinCount(52, "출력 지연", 6, fail * 8 / 100),
                new BinCount(7, "Open", 2, fail * 6 / 100),
            }.Take(count).ToList();
        }

        public double[] GetRecentYields(string testerId, out string[] lotIds)
        {
            var labels = new List<string>();
            var values = new List<double>();
            double[] earlier;
            if (EarlierYields.TryGetValue(testerId, out earlier))
                for (int i = 0; i < earlier.Length; i++) { labels.Add("FT26K03" + (40 + i * 7 + testerId[2] - '0')); values.Add(earlier[i]); }
            foreach (var r in History.Where(h => h.TesterId == testerId).OrderBy(h => h.EndTime)) { labels.Add(r.LotId); values.Add(r.FinalYield); }
            var tester = Testers.First(t => t.Id == testerId);
            if (tester.Lot != null && tester.Process == TesterProcess.Testing) { labels.Add(tester.Lot.LotId); values.Add(tester.Lot.FinalYield); }
            lotIds = labels.ToArray();
            return values.ToArray();
        }

        public IList<OcapStep> GetOcapSteps(string lotId)
        {
            var at = History.First(h => h.LotId == lotId).EndTime;
            return new[]
            {
                new OcapStep("이탈 감지", at, "자동"),
                new OcapStep("알람", at.AddSeconds(2), "자동"),
                new OcapStep("Lot Hold", at.AddSeconds(5), "자동"),
                new OcapStep("장비 사용 중지", at.AddMinutes(1).AddSeconds(14), "반장 박정훈"),
                new OcapStep("원인 확인", null, ""),
                new OcapStep("판정", null, ""),
                new OcapStep("장비 재가동 승인", null, ""),
                new OcapStep("기록", null, ""),
            };
        }

        public void SaveOcapDecision(string lotId, string decision, string reason)
        {
            // 샘플: 기록하지 않는다. 실제 구현은 MES OCAP 이력에 남긴다.
        }

        private static LotInfo FindLot(string lotId)
        {
            return Testers.First(t => t.Lot != null && t.Lot.LotId == lotId).Lot;
        }

        private static IList<BinCount> Hard(params int[] counts)
        {
            var list = new List<BinCount>();
            for (int i = 0; i < counts.Length; i++) list.Add(new BinCount(i + 1, HardBinNames[i + 1], i + 1, counts[i]));
            return list;
        }

        private static LotResult Lot(int h, int m, int s, string tester, string lotId, string customer, string product, int input,
            double first, double final, string softBin, double softRate)
        {
            return new LotResult(Today.AddHours(h).AddMinutes(m).AddSeconds(s), tester, lotId, customer, product, input, first, final, softBin, softRate);
        }
    }
}
