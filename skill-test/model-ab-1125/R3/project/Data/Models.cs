using System.Collections.Generic;
using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>테스터 한 대. Process: Run=테스트 중, Stop=대기, Alarm=알람. FinalYield는 최근 Lot 최종 수율(%).</summary>
    internal sealed class TesterInfo
    {
        public string Id { get; set; }
        public StatusKind Process { get; set; }
        public double FinalYield { get; set; }
    }

    internal sealed class BinCount
    {
        public int Bin { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }

    internal sealed class SoftBinCount
    {
        public int SoftBin { get; set; }
        public string Cause { get; set; }
        public int HardBin { get; set; }
        public int Count { get; set; }
    }

    /// <summary>테스터의 현재 Lot. HardBins는 1~8 전부, SoftBins는 수량 내림차순.</summary>
    internal sealed class LotDetail
    {
        public string TesterId { get; set; }
        public string LotId { get; set; }
        public string CustomerCode { get; set; }
        public string Product { get; set; }
        public string TpVersion { get; set; }
        public double TestTemp { get; set; }
        public int Input { get; set; }
        public int Processed { get; set; }
        public int FirstPassGood { get; set; }
        public int FinalGood { get; set; }
        public IList<BinCount> HardBins { get; set; }
        public IList<SoftBinCount> SoftBins { get; set; }

        public double FirstYield { get { return Percent(FirstPassGood, Input); } }
        public double FinalYield { get { return Percent(FinalGood, Input); } }

        public static double Percent(int part, int whole)
        {
            return whole == 0 ? 0 : part * 100.0 / whole;
        }
    }

    /// <summary>Lot 결과 이력 한 줄.</summary>
    internal sealed class LotRecord
    {
        public System.DateTime Time { get; set; }
        public string TesterId { get; set; }
        public string LotId { get; set; }
        public string CustomerCode { get; set; }
        public string Product { get; set; }
        public double FirstYield { get; set; }
        public double FinalYield { get; set; }
        public string TopSoftBin { get; set; }
    }

    internal enum OcapDecision { Hold, Retest, Release, Scrap }

    internal sealed class LotDecision
    {
        public string LotId { get; set; }
        public OcapDecision Decision { get; set; }
        public string Reason { get; set; }
    }
}
