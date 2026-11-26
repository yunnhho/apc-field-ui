using System;
using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>테스터 공정 상태.</summary>
    internal enum TesterProcess { Testing, Idle, Alarm }

    /// <summary>테스터에 걸린 현재 Lot. 수율은 진행 수량 기준(최종 = 재테스트 회복분 포함).</summary>
    internal sealed class LotInfo
    {
        public LotInfo(string lotId, string customer, string product, string program, double temperature,
            int inputQty, int testedQty, int firstPassGood, int finalGood)
        {
            LotId = lotId; Customer = customer; Product = product; Program = program; Temperature = temperature;
            InputQty = inputQty; TestedQty = testedQty; FirstPassGood = firstPassGood; FinalGood = finalGood;
        }

        public string LotId { get; }
        public string Customer { get; }
        public string Product { get; }
        public string Program { get; }
        public double Temperature { get; }
        public int InputQty { get; }
        public int TestedQty { get; }
        public int FirstPassGood { get; }
        public int FinalGood { get; }
        public double FirstYield { get { return YieldJudge.Yield(FirstPassGood, TestedQty); } }
        public double FinalYield { get { return YieldJudge.Yield(FinalGood, TestedQty); } }
    }

    internal sealed class TesterState
    {
        public TesterState(string id, TesterProcess process, LotInfo lot)
        {
            Id = id; Process = process; Lot = lot;
        }

        public string Id { get; }
        public TesterProcess Process { get; }
        /// <summary>대기면 null.</summary>
        public LotInfo Lot { get; }
    }

    /// <summary>끝난 Lot 한 건(Lot 결과 이력).</summary>
    internal sealed class LotResult
    {
        public LotResult(DateTime endTime, string testerId, string lotId, string customer, string product, int inputQty,
            double firstYield, double finalYield, string topSoftBin, double topSoftBinRate)
        {
            EndTime = endTime; TesterId = testerId; LotId = lotId; Customer = customer; Product = product; InputQty = inputQty;
            FirstYield = firstYield; FinalYield = finalYield; TopSoftBin = topSoftBin; TopSoftBinRate = topSoftBinRate;
        }

        public DateTime EndTime { get; }
        public string TesterId { get; }
        public string LotId { get; }
        public string Customer { get; }
        public string Product { get; }
        public int InputQty { get; }
        public double FirstYield { get; }
        public double FinalYield { get; }
        public string TopSoftBin { get; }
        public double TopSoftBinRate { get; }
        public bool IsHold { get { return YieldJudge.Judge(FinalYield) == StatusKind.Alarm; } }
    }

    internal sealed class BinCount
    {
        public BinCount(int bin, string name, int hardBin, int count)
        {
            Bin = bin; Name = name; HardBin = hardBin; Count = count;
        }

        public int Bin { get; }
        public string Name { get; }
        /// <summary>Soft Bin이면 매핑된 Hard Bin, Hard Bin이면 자기 번호.</summary>
        public int HardBin { get; }
        public int Count { get; }
    }

    /// <summary>OCAP 단계 한 줄. 완료 전이면 Done = null.</summary>
    internal sealed class OcapStep
    {
        public OcapStep(string name, DateTime? done, string owner)
        {
            Name = name; Done = done; Owner = owner;
        }

        public string Name { get; }
        public DateTime? Done { get; }
        public string Owner { get; }
    }
}
