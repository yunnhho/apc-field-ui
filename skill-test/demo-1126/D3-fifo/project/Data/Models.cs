using System;
using System.Globalization;

namespace RawMaterialIssueTerminal.Data
{
    /// <summary>생산 지시서. AllergenLine = 이 지시가 쓰는 알레르기 라인 구분.</summary>
    internal sealed class OrderInfo
    {
        public readonly string Code, Product, Line, AllergenLine;
        public readonly int Batches;

        public OrderInfo(string code, string product, int batches, string line, string allergenLine)
        {
            Code = code; Product = product; Batches = batches; Line = line; AllergenLine = allergenLine;
        }
    }

    /// <summary>지시서의 원료 한 줄(필요량·지금까지 출고량, kg).</summary>
    internal sealed class MaterialLine
    {
        public readonly string Code, Name, Allergen;
        public readonly double Required, Issued;

        /// <param name="allergen">밀·대두·우유 등 알레르기 유발 원료명, 아니면 "".</param>
        public MaterialLine(string code, string name, string allergen, double required, double issued)
        {
            Code = code; Name = name; Allergen = allergen; Required = required; Issued = issued;
        }

        public double Remaining { get { return Math.Max(0.0, Required - Issued); } }
    }

    /// <summary>재고 Lot. AllergenLine = 이 Lot가 보관된 알레르기 라인 구분(알레르기 원료만 의미 있음).</summary>
    internal sealed class LotStock
    {
        public readonly string Lot, MaterialCode, AllergenLine;
        public readonly DateTime ReceivedOn, ExpiresOn;
        public readonly double Remaining;

        public LotStock(string lot, string materialCode, DateTime receivedOn, DateTime expiresOn, double remaining, string allergenLine)
        {
            Lot = lot; MaterialCode = materialCode; ReceivedOn = receivedOn; ExpiresOn = expiresOn; Remaining = remaining; AllergenLine = allergenLine;
        }
    }

    /// <summary>금일 출고 이력 한 줄. Note = "" 또는 "FIFO 예외 승인 · EMP-2041".</summary>
    internal sealed class IssueRecord
    {
        public readonly DateTime Time;
        public readonly string OrderCode, MaterialName, Lot, Note;
        public readonly double Weight;

        public IssueRecord(DateTime time, string orderCode, string materialName, string lot, double weight, string note)
        {
            Time = time; OrderCode = orderCode; MaterialName = materialName; Lot = lot; Weight = weight; Note = note;
        }
    }

    /// <summary>원료 포대 라벨 바코드 "원료코드/Lot/중량(kg)" 예: RM-1021/L261120-03/25.0</summary>
    internal sealed class BagLabel
    {
        public readonly string MaterialCode, Lot;
        public readonly double Weight;

        public BagLabel(string materialCode, string lot, double weight) { MaterialCode = materialCode; Lot = lot; Weight = weight; }

        public static BagLabel TryParse(string code)
        {
            string[] p = (code ?? "").Trim().Split('/');
            double w;
            if (p.Length != 3 || p[0].Length == 0 || p[1].Length == 0) return null;
            if (!double.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out w) || w <= 0) return null;
            return new BagLabel(p[0], p[1], w);
        }
    }
}
