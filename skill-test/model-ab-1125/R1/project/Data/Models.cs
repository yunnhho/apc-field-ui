using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>수율 판정. 화면 전체(타일·판정판·카드·이력 표·dialog)가 이 한 곳을 쓴다.</summary>
    internal static class YieldJudge
    {
        /// <summary>정한 기준값: 최종 수율 기준 95.0 % (하한만 있는 기준 — 같으면 합격).</summary>
        public const decimal MinFinalYield = 95.0m;

        /// <summary>정한 기준값: 수율은 화면 표시와 같은 소수 1자리로 비교한다.</summary>
        public static decimal Round(decimal y) { return Math.Round(y, 1, MidpointRounding.AwayFromZero); }

        public static bool IsBelow(decimal finalYield) { return Round(finalYield) < MinFinalYield; }

        public static StatusKind Kind(decimal finalYield) { return IsBelow(finalYield) ? StatusKind.Alarm : StatusKind.Run; }

        public static string Text(decimal y) { return Round(y).ToString("0.0"); }
    }

    /// <summary>Hard Bin 한 줄(1 양품, 2~8 불량 묶음).</summary>
    internal sealed class HardBinRow
    {
        public readonly int Bin; public readonly string Name; public readonly int Count;
        public HardBinRow(int bin, string name, int count) { Bin = bin; Name = name; Count = count; }
    }

    /// <summary>Soft Bin 한 줄(테스트 프로그램이 정한 상세 원인, 하나의 Hard Bin에 매핑).</summary>
    internal sealed class SoftBinRow
    {
        public readonly int SoftBin; public readonly int HardBin; public readonly string Cause; public readonly int Count;
        public SoftBinRow(int softBin, int hardBin, string cause, int count) { SoftBin = softBin; HardBin = hardBin; Cause = cause; Count = count; }
    }

    /// <summary>테스터 한 대와 지금 돌리는 Lot(없으면 Lot = null).</summary>
    internal sealed class TesterInfo
    {
        public readonly string Id; public readonly StatusKind Process; public readonly string ProcessText; public readonly LotInfo Lot;
        public TesterInfo(string id, StatusKind process, string processText, LotInfo lot) { Id = id; Process = process; ProcessText = processText; Lot = lot; }

        /// <summary>타일 상태 = 공정 상태와 수율 판정 중 더 나쁜 것.</summary>
        public StatusKind TileStatus
        {
            get { return Lot != null && YieldJudge.IsBelow(Lot.FinalYield) ? StatusKind.Alarm : Process; }
        }
    }

    /// <summary>Lot 한 개의 현재 정보와 Bin 결과.</summary>
    internal sealed class LotInfo
    {
        public readonly string LotId, Customer, Product, ProgramVersion;
        public readonly decimal TempC;
        public readonly int Input, Tested, GoodFirst;
        public readonly IList<HardBinRow> HardBins;   // Bin 1이 양품 → 최종 양품 수 = Bin 1
        public readonly IList<SoftBinRow> SoftBinTop;
        public readonly IList<double> RecentFinalYields;   // 과거 → 현재(마지막 = 이 Lot 현재 수율)

        public LotInfo(string lotId, string customer, string product, string programVersion, decimal tempC, int input, int tested, int goodFirst,
            IList<HardBinRow> hardBins, IList<SoftBinRow> softBinTop, IList<double> recentFinalYields)
        {
            LotId = lotId; Customer = customer; Product = product; ProgramVersion = programVersion; TempC = tempC;
            Input = input; Tested = tested; GoodFirst = goodFirst; HardBins = hardBins; SoftBinTop = softBinTop; RecentFinalYields = recentFinalYields;
        }

        public int GoodFinal { get { return HardBins[0].Count; } }
        public decimal FirstYield { get { return Tested == 0 ? 0m : GoodFirst * 100m / Tested; } }
        public decimal FinalYield { get { return Tested == 0 ? 0m : GoodFinal * 100m / Tested; } }
    }

    /// <summary>Lot 결과 이력 한 줄.</summary>
    internal sealed class LotResultRow
    {
        public readonly DateTime EndTime; public readonly string LotId, TesterId, Customer, Product, Disposition;
        public readonly int Input; public readonly decimal FirstYield, FinalYield;
        public LotResultRow(DateTime endTime, string lotId, string testerId, string customer, string product, int input, decimal firstYield, decimal finalYield, string disposition)
        {
            EndTime = endTime; LotId = lotId; TesterId = testerId; Customer = customer; Product = product; Input = input;
            FirstYield = firstYield; FinalYield = finalYield; Disposition = disposition;
        }
    }
}
