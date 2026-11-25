using System;
using System.Globalization;

namespace FinalTestYieldMonitor.Data
{
    internal enum TesterState { Testing, Idle, Alarm }

    /// <summary>수율 판정 기준(한 곳). 최종 수율 95.0 % 이상이면 합격, 표시 자리(0.0) 기준으로 95.0 미만이면 기준 미달.</summary>
    internal static class YieldRule
    {
        public const double Limit = 95.0;
        public static bool IsBelow(double yield) { return Math.Round(yield, 1, MidpointRounding.AwayFromZero) < Limit; }
        public static string Pct(double yield) { return yield.ToString("0.0", CultureInfo.InvariantCulture); }
        public static string Qty(int qty) { return qty.ToString("#,0", CultureInfo.InvariantCulture); }
    }

    internal sealed class SoftBinRow
    {
        public SoftBinRow(int soft, int hard, string name, int qty) { Soft = soft; Hard = hard; Name = name; Qty = qty; }
        public int Soft { get; private set; }
        public int Hard { get; private set; }
        public string Name { get; private set; }
        public int Qty { get; private set; }
    }

    internal sealed class LotInfo
    {
        public LotInfo(string lotId, string customer, string product, string programVersion, double temperature,
            int inputQty, int doneQty, double firstYield, int[] hardBins, SoftBinRow[] softTop)
        {
            LotId = lotId; Customer = customer; Product = product; ProgramVersion = programVersion; Temperature = temperature;
            InputQty = inputQty; DoneQty = doneQty; FirstYield = firstYield; HardBins = hardBins; SoftTop = softTop;
        }
        public string LotId { get; private set; }
        public string Customer { get; private set; }
        public string Product { get; private set; }
        public string ProgramVersion { get; private set; }
        public double Temperature { get; private set; }
        public int InputQty { get; private set; }
        public int DoneQty { get; private set; }
        public double FirstYield { get; private set; }
        /// <summary>Hard Bin 1~8 수량(인덱스 0 = Bin 1 양품). 합 = DoneQty.</summary>
        public int[] HardBins { get; private set; }
        public SoftBinRow[] SoftTop { get; private set; }
        /// <summary>최종 수율 = 양품(Bin 1) / 테스트한 Unit.</summary>
        public double FinalYield { get { return (double)HardBins[0] * 100.0 / DoneQty; } }
    }

    internal sealed class TesterInfo
    {
        public TesterInfo(string id, TesterState state, LotInfo lot, double[] trend) { Id = id; State = state; Lot = lot; Trend = trend; }
        public string Id { get; private set; }
        public TesterState State { get; private set; }
        /// <summary>대기 중이면 null.</summary>
        public LotInfo Lot { get; private set; }
        /// <summary>이 테스터의 최근 Lot 최종 수율(과거 → 최근). 진행 중 Lot이 있으면 마지막 값 = 현재 Lot.</summary>
        public double[] Trend { get; private set; }
    }

    internal sealed class HistoryRow
    {
        public HistoryRow(string time, string lotId, string customer, string product, string tester, int inputQty,
            double firstYield, double finalYield, string disposition)
        {
            Time = time; LotId = lotId; Customer = customer; Product = product; Tester = tester; InputQty = inputQty;
            FirstYield = firstYield; FinalYield = finalYield; Disposition = disposition;
        }
        public string Time { get; private set; }
        public string LotId { get; private set; }
        public string Customer { get; private set; }
        public string Product { get; private set; }
        public string Tester { get; private set; }
        public int InputQty { get; private set; }
        public double FirstYield { get; private set; }
        public double FinalYield { get; private set; }
        /// <summary>기준 미달 Lot의 처리 결과(Hold·Release 승인·Scrap …). 합격 Lot은 빈 값.</summary>
        public string Disposition { get; private set; }
    }
}
