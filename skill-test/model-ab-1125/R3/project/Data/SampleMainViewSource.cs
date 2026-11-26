using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 투입은 모두 10,000 Unit, Bin 합계가 투입과 맞게 만들어진다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const int SampleInput = 10000;
        private const int OtherSoftBin = 80;
        private const int OtherHardBin = 8;

        // 정한 기준값: Hard Bin 이름(업무 문서에 없어 임의로 정함)
        private static readonly string[] HardNames =
        {
            "", "양품", "누설 전류", "접촉 불량", "기능 불량", "속도 불량", "온도 불량", "외관 불량", "기타"
        };

        private static readonly Dictionary<int, string> SoftCauses = new Dictionary<int, string>
        {
            { 12, "접촉 불량 (핀 3)" },
            { 23, "게이트 누설 초과" },
            { 41, "기능 시퀀스 실패" },
            { 42, "기능 타이밍 경계" },
            { 55, "속도 스펙 미달" },
            { 61, "온도 드리프트" },
            { 71, "외관 긁힘" },
            { OtherSoftBin, "기타" },
        };

        private static readonly Dictionary<string, StatusKind> Processes = new Dictionary<string, StatusKind>
        {
            { "T01", StatusKind.Run }, { "T02", StatusKind.Run }, { "T03", StatusKind.Stop },
            { "T04", StatusKind.Alarm }, { "T05", StatusKind.Run }, { "T06", StatusKind.Run },
        };

        private static readonly Dictionary<string, double[]> Trends = new Dictionary<string, double[]>
        {
            { "T01", new[] { 96.9, 97.0, 96.5, 96.8, 96.4, 96.6, 96.1, 96.2 } },
            { "T02", new[] { 96.2, 96.0, 95.9, 96.1, 95.8, 95.9, 96.0, 95.7 } },
            { "T03", new[] { 95.8, 96.1, 96.3, 96.0, 96.2, 96.6, 96.4, 96.5 } },
            { "T04", new[] { 96.6, 96.3, 95.9, 96.4, 95.4, 96.1, 95.1, 93.8 } },
            { "T05", new[] { 96.8, 97.0, 96.6, 96.9, 97.1, 96.7, 97.0, 97.3 } },
            { "T06", new[] { 97.4, 97.2, 96.9, 97.0, 96.6, 96.8, 97.2, 97.1 } },
        };

        private readonly Dictionary<string, LotDetail> _lots;
        private readonly List<LotRecord> _history;

        public SampleMainViewSource()
        {
            _lots = new Dictionary<string, LotDetail>
            {
                { "T01", MakeLot("T01", "L261125-05", "A01", "QF48-5X5", "TP_3.2.1", 125.0, 9610, 9620,
                    Soft(12, 3, 150), Soft(41, 4, 120), Soft(71, 7, 40)) },
                { "T02", MakeLot("T02", "L261125-11", "B02", "BG32-7X7", "TP_2.8.0", 85.0, 9580, 9570,
                    Soft(12, 3, 200), Soft(55, 5, 110), Soft(61, 6, 60)) },
                { "T03", MakeLot("T03", "L261125-09", "C03", "LF64-9X9", "TP_4.1.0", 25.0, 9600, 9650,
                    Soft(42, 4, 200), Soft(71, 7, 100)) },
                { "T04", MakeLot("T04", "L261125-07", "A01", "QF48-5X5", "TP_3.2.1", 125.0, 9250, 9380,
                    Soft(23, 2, 180), Soft(41, 4, 150), Soft(12, 3, 95), Soft(55, 5, 70),
                    Soft(42, 4, 60), Soft(61, 6, 30), Soft(71, 7, 15)) },
                { "T05", MakeLot("T05", "L261125-12", "A01", "QF48-5X5", "TP_3.2.1", 125.0, 9690, 9730,
                    Soft(55, 5, 150), Soft(12, 3, 60)) },
                { "T06", MakeLot("T06", "L261125-13", "C03", "LF64-9X9", "TP_4.1.0", 125.0, 9680, 9710,
                    Soft(61, 6, 120), Soft(41, 4, 80)) },
            };

            _history = new List<LotRecord>
            {
                Record(15, 2, 44, "T05", "L261125-12", "A01", "QF48-5X5", 96.9, 97.3, "SB55 속도 스펙 미달"),
                Record(15, 0, 58, "T06", "L261125-13", "C03", "LF64-9X9", 96.8, 97.1, "SB61 온도 드리프트"),
                Record(14, 52, 10, "T04", "L261125-07", "A01", "QF48-5X5", 92.5, 93.8, "SB23 게이트 누설 초과"),
                Record(14, 41, 37, "T02", "L261125-11", "B02", "BG32-7X7", 95.8, 95.7, "SB12 접촉 불량 (핀 3)"),
                Record(14, 20, 5, "T01", "L261125-05", "A01", "QF48-5X5", 96.1, 96.2, "SB12 접촉 불량 (핀 3)"),
                Record(13, 58, 19, "T04", "L261125-06", "A01", "QF48-5X5", 95.6, 95.1, "SB23 게이트 누설 초과"),
                Record(13, 20, 5, "T03", "L261125-09", "C03", "LF64-9X9", 96.0, 96.5, "SB42 기능 타이밍 경계"),
                Record(12, 58, 40, "T06", "L261125-08", "C03", "LF64-9X9", 96.8, 95.0, "SB61 온도 드리프트"),
                Record(12, 31, 22, "T05", "L261125-04", "A01", "QF48-5X5", 96.4, 96.6, "SB55 속도 스펙 미달"),
                Record(11, 58, 7, "T02", "L261125-03", "B02", "BG32-7X7", 95.9, 96.0, "SB61 온도 드리프트"),
                Record(11, 20, 50, "T01", "L261125-02", "A01", "QF48-5X5", 97.0, 97.2, "SB41 기능 시퀀스 실패"),
                Record(10, 47, 33, "T06", "L261125-01", "C03", "LF64-9X9", 97.1, 97.4, "SB41 기능 시퀀스 실패"),
            };
        }

        public IList<TesterInfo> GetTesters()
        {
            var testers = new List<TesterInfo>();
            foreach (KeyValuePair<string, StatusKind> pair in Processes)
            {
                testers.Add(new TesterInfo
                {
                    Id = pair.Key,
                    Process = pair.Value,
                    FinalYield = _lots[pair.Key].FinalYield,
                });
            }
            testers.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return testers;
        }

        public LotDetail GetLot(string testerId)
        {
            return _lots[testerId];
        }

        public IList<LotRecord> GetLotHistory()
        {
            return new List<LotRecord>(_history);
        }

        public IList<double> GetYieldTrend(string testerId)
        {
            return new List<double>(Trends[testerId]);
        }

        public bool SaveDecision(LotDecision decision)
        {
            return decision != null && !string.IsNullOrEmpty(decision.LotId);
        }

        private static SoftBinCount Soft(int softBin, int hardBin, int count)
        {
            return new SoftBinCount { SoftBin = softBin, Cause = SoftCauses[softBin], HardBin = hardBin, Count = count };
        }

        // 양품 수(최종)는 Hard Bin 1, 나머지 Soft Bin은 Hard Bin에 합산. 모자라는 수는 '기타'로 채워 투입과 맞춘다.
        private static LotDetail MakeLot(string testerId, string lotId, string customer, string product,
            string tpVersion, double testTemp, int firstGood, int finalGood, params SoftBinCount[] softs)
        {
            var softList = new List<SoftBinCount>(softs);
            int otherCount = SampleInput - finalGood;
            foreach (SoftBinCount s in softs) otherCount -= s.Count;
            if (otherCount > 0) softList.Add(Soft(OtherSoftBin, OtherHardBin, otherCount));

            var hard = new int[OtherHardBin + 1];
            hard[1] = finalGood;
            foreach (SoftBinCount s in softList) hard[s.HardBin] += s.Count;

            var hardBins = new List<BinCount>();
            for (int bin = 1; bin <= OtherHardBin; bin++)
            {
                hardBins.Add(new BinCount { Bin = bin, Name = HardNames[bin], Count = hard[bin] });
            }

            softList.Sort((a, b) => b.Count.CompareTo(a.Count));
            return new LotDetail
            {
                TesterId = testerId,
                LotId = lotId,
                CustomerCode = customer,
                Product = product,
                TpVersion = tpVersion,
                TestTemp = testTemp,
                Input = SampleInput,
                Processed = SampleInput,
                FirstPassGood = firstGood,
                FinalGood = finalGood,
                HardBins = hardBins,
                SoftBins = softList,
            };
        }

        private static LotRecord Record(int hour, int minute, int second, string testerId, string lotId,
            string customer, string product, double firstYield, double finalYield, string topSoft)
        {
            return new LotRecord
            {
                Time = new DateTime(2026, 11, 25, hour, minute, second),
                TesterId = testerId,
                LotId = lotId,
                CustomerCode = customer,
                Product = product,
                FirstYield = firstYield,
                FinalYield = finalYield,
                TopSoftBin = topSoft,
            };
        }
    }
}
