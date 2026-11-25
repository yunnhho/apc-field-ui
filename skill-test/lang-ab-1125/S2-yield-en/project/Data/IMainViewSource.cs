using System;
using System.Globalization;
using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>수율 판정 규칙. 타일·카드·판정판·이력 표·dialog가 모두 이 한 곳을 쓴다.</summary>
    internal static class YieldRule
    {
        /// <summary>최종 수율 기준(이상 통과). 95.0 % 미만이면 기준 미달 → Lot Hold. 정한 기준값: 요구에 있음.</summary>
        public const double FinalYieldCriterion = 95.0;

        /// <summary>표시 자릿수(0.0)로 반올림한 값으로 판정한다(화면에 95.0으로 보이면 통과).</summary>
        public static StatusKind JudgeFinal(double finalYield)
        {
            return Round1(finalYield) >= FinalYieldCriterion ? StatusKind.Run : StatusKind.Alarm;
        }

        public static double Round1(double v) { return Math.Round(v, 1, MidpointRounding.AwayFromZero); }
        public static string Pct(double v) { return Round1(v).ToString("0.0", CultureInfo.InvariantCulture); }
        public static string Qty(int v) { return v.ToString("#,0", CultureInfo.InvariantCulture); }
        public static string Temp(double v) { return v.ToString("0.0", CultureInfo.InvariantCulture); }
    }

    internal sealed class SoftBinRow
    {
        public readonly int SoftBin;
        public readonly string Name;
        public readonly int HardBin;
        public readonly int Qty;
        public SoftBinRow(int softBin, string name, int hardBin, int qty) { SoftBin = softBin; Name = name; HardBin = hardBin; Qty = qty; }
    }

    /// <summary>테스터에 지금 올라간 Lot. 양품 = Hard Bin 1, 수율 = 양품 / 테스트 수량.</summary>
    internal sealed class LotInfo
    {
        public readonly string LotId, CustomerCode, Product, ProgramVersion;
        public readonly double TemperatureC;
        public readonly int InputQty, TestedQty, FirstPassGood;
        public readonly int[] HardBin;          // Hard Bin 1~8 수량
        public readonly SoftBinRow[] SoftTop;   // Soft Bin 상위 5(수량 큰 순)
        public readonly double[] YieldTrend;    // 최근 Lot 최종 수율, 마지막 = 이 Lot

        public LotInfo(string lotId, string customerCode, string product, string programVersion, double temperatureC,
            int inputQty, int testedQty, int firstPassGood, int[] hardBin, SoftBinRow[] softTop, double[] yieldTrend)
        {
            LotId = lotId; CustomerCode = customerCode; Product = product; ProgramVersion = programVersion; TemperatureC = temperatureC;
            InputQty = inputQty; TestedQty = testedQty; FirstPassGood = firstPassGood;
            HardBin = hardBin; SoftTop = softTop; YieldTrend = yieldTrend;
        }

        public int FinalGood { get { return HardBin[0]; } }
        public double FirstPassYield { get { return 100.0 * FirstPassGood / TestedQty; } }
        public double FinalYield { get { return 100.0 * FinalGood / TestedQty; } }
    }

    /// <summary>테스터 한 대. Lot이 null이면 대기(Lot 없음).</summary>
    internal sealed class TesterSnapshot
    {
        public readonly string Name;
        public readonly LotInfo Lot;
        public TesterSnapshot(string name, LotInfo lot) { Name = name; Lot = lot; }
    }

    /// <summary>끝난 Lot의 결과 한 줄(이력 표).</summary>
    internal sealed class LotResultRow
    {
        public readonly DateTime EndTime;
        public readonly string Tester, LotId, CustomerCode, Product, Disposition;
        public readonly int InputQty;
        public readonly double FirstYield, FinalYield;

        public LotResultRow(DateTime endTime, string tester, string lotId, string customerCode, string product,
            int inputQty, double firstYield, double finalYield, string disposition)
        {
            EndTime = endTime; Tester = tester; LotId = lotId; CustomerCode = customerCode; Product = product;
            InputQty = inputQty; FirstYield = firstYield; FinalYield = finalYield; Disposition = disposition;
        }
    }

    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(테스터·핸들러 통신, MES DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>테스터 T01~T06 현재 상태와 Lot.</summary>
        TesterSnapshot[] GetTesters();

        /// <summary>최근에 끝난 Lot 결과(새것부터).</summary>
        LotResultRow[] GetRecentLotResults();

        /// <summary>수율 기준 미달 Lot 처리 판정을 기록한다(decision: Hold 유지·추가 검사·Release·Scrap).</summary>
        void SubmitLotDecision(string lotId, string decision, string reason);
    }
}
