using System;
using System.Globalization;
using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>수율 판정 기준과 표시 형식. 판정은 이 함수 하나만 쓴다(타일·카드·판정판·이력 표·dialog 공통).</summary>
    internal static class YieldRules
    {
        /// <summary>최종 수율 기준(%) — 이 값 이상이면 통과(같으면 통과). 요구에 값이 없어 정한 기준.</summary>
        public const double FinalYieldMin = 95.0;
        /// <summary>한 Soft Bin이 다른 테스터 평균의 이 배수 이상이면 편중으로 본다. 정한 기준.</summary>
        public const double SoftBinSkewRatio = 3.0;

        /// <summary>수율 % (소수 1자리 반올림). 표시와 같은 값으로 판정하려고 반올림한 값을 쓴다.</summary>
        public static double Yield(int good, int tested)
        {
            return tested <= 0 ? 0 : Math.Round(good * 100.0 / tested, 1, MidpointRounding.AwayFromZero);
        }

        public static bool IsBelowLimit(double finalYield) { return finalYield < FinalYieldMin; }

        public static string Pct(double value) { return value.ToString("0.0", CultureInfo.InvariantCulture); }
        public static string Qty(int value) { return value.ToString("#,0", CultureInfo.InvariantCulture); }
        public static string Temp(double value) { return value.ToString("0.0", CultureInfo.InvariantCulture); }
    }

    /// <summary>Lot 한 건의 수량·수율. 값 객체(생성 후 바뀌지 않음).</summary>
    internal sealed class LotRecord
    {
        public readonly string LotId, Customer, Product, ProgramVersion, Tester, Disposition;
        public readonly double TempC;
        public readonly int Input, Tested, FirstGood, FinalGood;
        public readonly DateTime? CompletedAt;

        public LotRecord(string lotId, string customer, string product, string programVersion, string tester, double tempC,
            int input, int tested, int firstGood, int finalGood, DateTime? completedAt, string disposition)
        {
            LotId = lotId; Customer = customer; Product = product; ProgramVersion = programVersion; Tester = tester;
            TempC = tempC; Input = input; Tested = tested; FirstGood = firstGood; FinalGood = finalGood;
            CompletedAt = completedAt; Disposition = disposition;
        }

        public double FirstYield { get { return YieldRules.Yield(FirstGood, Tested); } }
        public double FinalYield { get { return YieldRules.Yield(FinalGood, Tested); } }
        public bool BelowLimit { get { return YieldRules.IsBelowLimit(FinalYield); } }
    }

    /// <summary>Soft Bin 한 줄. OtherAvgRate = 다른 테스터 평균 발생률(%).</summary>
    internal sealed class SoftBinRow
    {
        public readonly int SoftNo, HardNo, Count;
        public readonly double OtherAvgRate;

        public SoftBinRow(int softNo, int hardNo, int count, double otherAvgRate)
        {
            SoftNo = softNo; HardNo = hardNo; Count = count; OtherAvgRate = otherAvgRate;
        }

        public double Rate(int tested) { return tested <= 0 ? 0 : Math.Round(Count * 100.0 / tested, 1, MidpointRounding.AwayFromZero); }
        public bool IsSkewed(int tested)
        {
            return OtherAvgRate > 0 && Count * 100.0 / tested >= OtherAvgRate * YieldRules.SoftBinSkewRatio;
        }
    }

    /// <summary>테스터 한 대: 상태 + 현재 Lot + Bin 분포 + 추이. Lot이 없으면(대기) 값은 모두 비어 있다.</summary>
    internal sealed class TesterInfo
    {
        public readonly string Id;
        public readonly StatusKind State;      // Run=테스트 중, Stop=대기, Alarm=알람(수율 미달 Hold)
        public readonly LotRecord Lot;         // 대기면 null
        public readonly int[] HardBins;        // Bin1~8 수량(Bin1 = 양품), 대기면 빈 배열
        public readonly SoftBinRow[] SoftBins; // 상위 5(수량 내림차순)
        public readonly double[] YieldTrend;   // 최근 Lot 최종 수율(과거→현재), 마지막 = 현재 Lot

        public TesterInfo(string id, StatusKind state, LotRecord lot, int[] hardBins, SoftBinRow[] softBins, double[] yieldTrend)
        {
            Id = id; State = state; Lot = lot; HardBins = hardBins; SoftBins = softBins; YieldTrend = yieldTrend;
        }
    }

    /// <summary>OCAP 단계 한 칸. 진행 상태는 데이터 쪽이 알려 주고 dialog는 읽기만 한다.</summary>
    internal sealed class OcapStep
    {
        public readonly string Name, Note;
        public readonly bool Done;
        public OcapStep(string name, bool done, string note) { Name = name; Done = done; Note = note; }
    }

    /// <summary>미달 Lot 처리 dialog 입력. 선행 단계 상태를 담아 넘기고, dialog는 바꾸지 못한다.</summary>
    internal sealed class OcapCase
    {
        public readonly LotRecord Lot;
        public readonly SoftBinRow[] SoftBins;
        public readonly OcapStep[] Steps;      // 판정 이전 단계(알람 ~ 고객 승인)

        public OcapCase(LotRecord lot, SoftBinRow[] softBins, OcapStep[] steps) { Lot = lot; SoftBins = softBins; Steps = steps; }

        /// <summary>Release를 막는 이유(없으면 null). 판정 이전 단계가 모두 끝나야 Release할 수 있다.</summary>
        public string ReleaseBlockReason()
        {
            foreach (var s in Steps) if (!s.Done) return s.Name + " 전 · Release 불가";
            return null;
        }
    }
}
