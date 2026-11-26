using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>테스터 공정 상태.</summary>
    internal enum TesterState { Testing, Waiting, Alarm }

    /// <summary>테스터 타일 한 칸. Yield = 현재 Lot 수율(진행 중이면 최초 수율, 끝났으면 최종 수율). 대기면 null.</summary>
    internal sealed class TesterSummary
    {
        public readonly string Name;
        public readonly TesterState State;
        public readonly double? Yield;

        public TesterSummary(string name, TesterState state, double? yield)
        {
            Name = name; State = state; Yield = yield;
        }
    }

    /// <summary>Soft Bin 한 줄(상세 불량 원인 → 매핑된 Hard Bin).</summary>
    internal sealed class SoftBinCount
    {
        public readonly int SoftBin;
        public readonly string Name;
        public readonly int HardBin;
        public readonly int Count;

        public SoftBinCount(int softBin, string name, int hardBin, int count)
        {
            SoftBin = softBin; Name = name; HardBin = hardBin; Count = count;
        }
    }

    /// <summary>선택 테스터의 현재 Lot. 대기 테스터는 LotId가 빈 값.</summary>
    internal sealed class LotDetail
    {
        public readonly string Tester;
        public readonly string LotId;
        public readonly string Customer;
        public readonly string Product;
        public readonly string Program;
        public readonly double TempC;
        public readonly int Input;
        public readonly int Tested;
        public readonly double? FirstYield;
        /// <summary>재테스트까지 끝난 Lot만 값이 있다.</summary>
        public readonly double? FinalYield;
        /// <summary>Bin1~Bin8 수량(인덱스 0 = Bin1 양품).</summary>
        public readonly int[] HardBins;
        public readonly IList<SoftBinCount> SoftBinTop;
        /// <summary>이 테스터 최근 Lot 최종 수율(왼쪽 = 과거, 마지막 = 현재 Lot).</summary>
        public readonly double[] RecentFinalYields;

        public LotDetail(string tester, string lotId, string customer, string product, string program, double tempC,
            int input, int tested, double? firstYield, double? finalYield, int[] hardBins, IList<SoftBinCount> softBinTop, double[] recentFinalYields)
        {
            Tester = tester; LotId = lotId; Customer = customer; Product = product; Program = program; TempC = tempC;
            Input = input; Tested = tested; FirstYield = firstYield; FinalYield = finalYield;
            HardBins = hardBins; SoftBinTop = softBinTop; RecentFinalYields = recentFinalYields;
        }

        public bool HasLot { get { return !string.IsNullOrEmpty(LotId); } }
    }

    /// <summary>Lot 결과 이력 한 줄(테스트 구역 전체).</summary>
    internal sealed class LotResult
    {
        public readonly DateTime EndTime;
        public readonly string Tester;
        public readonly string LotId;
        public readonly string Customer;
        public readonly string Product;
        public readonly int Input;
        public readonly double FirstYield;
        public readonly double FinalYield;
        public readonly int TopSoftBin;
        public readonly double TopSoftBinRate;

        public LotResult(DateTime endTime, string tester, string lotId, string customer, string product, int input,
            double firstYield, double finalYield, int topSoftBin, double topSoftBinRate)
        {
            EndTime = endTime; Tester = tester; LotId = lotId; Customer = customer; Product = product; Input = input;
            FirstYield = firstYield; FinalYield = finalYield; TopSoftBin = topSoftBin; TopSoftBinRate = topSoftBinRate;
        }

        public bool BelowSpec { get { return YieldRule.IsBelowSpec(FinalYield); } }
    }

    /// <summary>OCAP 단계 하나의 진행 상태(데이터에서 받는다 — dialog에서 바꿀 수 없다).</summary>
    internal sealed class OcapStep
    {
        public readonly string Name;
        public readonly StatusKind Status;
        public readonly string StateText;
        public readonly string Time;
        public readonly string Detail;
        public readonly bool Done;

        public OcapStep(string name, StatusKind status, string stateText, string time, string detail, bool done)
        {
            Name = name; Status = status; StateText = stateText; Time = time; Detail = detail; Done = done;
        }
    }
}
