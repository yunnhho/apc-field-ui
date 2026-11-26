using APC.UI.Theming;

namespace FinalTestYieldMonitor.Data
{
    /// <summary>수율 판정 기준. 정한 기준값은 이 한 곳에 둔다.</summary>
    internal static class YieldRules
    {
        /// <summary>최종 수율 기준(%). 미만이면 기준 미달 → Lot Hold. 같은 값은 기준 미달이 아니다.</summary>
        public const double SpecFinal = 95.0;
        /// <summary>경고 시작값(%). 이 값 이하(기준 이상)면 기준 근접 경고.</summary>
        public const double WarnAt = 95.0;

        public static StatusKind Judge(double finalYield)
        {
            if (finalYield < SpecFinal) return StatusKind.Alarm;
            if (finalYield <= WarnAt) return StatusKind.Warn;
            return StatusKind.Run;
        }

        public static string Pct(double v) { return v.ToString("0.0"); }
        public static string Qty(int v) { return v.ToString("#,0"); }
        public static string SpecText { get { return "기준 " + Pct(SpecFinal) + " 이상"; } }
    }

    /// <summary>테스터 한 대와 지금 시험 중인 Lot. Lot가 없으면(대기) LotId가 빈 문자열.</summary>
    internal sealed class TesterInfo
    {
        public TesterInfo(string name, StatusKind status, string state, string lotId, string customer, string product,
            string program, double temperature, int input, int progress, double firstYield, double finalYield)
        {
            Name = name; Status = status; State = state; LotId = lotId; Customer = customer; Product = product;
            ProgramVersion = program; Temperature = temperature; Input = input; Progress = progress;
            FirstYield = firstYield; FinalYield = finalYield;
        }

        public string Name { get; private set; }
        public StatusKind Status { get; private set; }
        public string State { get; private set; }
        public string LotId { get; private set; }
        public string Customer { get; private set; }
        public string Product { get; private set; }
        public string ProgramVersion { get; private set; }
        public double Temperature { get; private set; }
        public int Input { get; private set; }
        public int Progress { get; private set; }
        public double FirstYield { get; private set; }
        public double FinalYield { get; private set; }
        public bool HasLot { get { return LotId.Length > 0; } }
    }

    /// <summary>Bin 한 줄. Hard Bin 표는 HardBin = 자기 번호, Soft Bin 표는 매핑된 Hard Bin.</summary>
    internal sealed class BinRow
    {
        public BinRow(int bin, string name, int count, double percent, int hardBin)
        {
            Bin = bin; Name = name; Count = count; Percent = percent; HardBin = hardBin;
        }

        public int Bin { get; private set; }
        public string Name { get; private set; }
        public int Count { get; private set; }
        public double Percent { get; private set; }
        public int HardBin { get; private set; }
    }

    /// <summary>끝난 Lot의 결과 이력 한 줄.</summary>
    internal sealed class LotResult
    {
        public LotResult(string time, string lotId, string customer, string product, string tester,
            int input, double firstYield, double finalYield, string disposition)
        {
            Time = time; LotId = lotId; Customer = customer; Product = product; Tester = tester;
            Input = input; FirstYield = firstYield; FinalYield = finalYield; Disposition = disposition;
        }

        public string Time { get; private set; }
        public string LotId { get; private set; }
        public string Customer { get; private set; }
        public string Product { get; private set; }
        public string Tester { get; private set; }
        public int Input { get; private set; }
        public double FirstYield { get; private set; }
        public double FinalYield { get; private set; }
        /// <summary>기준 미달 Lot의 처리 상태(Hold·추가 검사 중·Release(고객 승인)…). 기준 이상이면 빈 문자열.</summary>
        public string Disposition { get; private set; }
    }
}
