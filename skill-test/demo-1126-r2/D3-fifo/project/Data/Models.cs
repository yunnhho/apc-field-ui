using System;
using System.Collections.Generic;

namespace RawMaterialIssueTerminal.Data
{
    /// <summary>지시서의 원료 한 줄. 필요량 = 배치당 사용량 x 배치 수(호출 측이 계산해 준다).</summary>
    internal sealed class RecipeLine
    {
        public readonly string Code, Name, AllergenLine;
        public readonly double RequiredKg, BagKg;
        public RecipeLine(string code, string name, string allergenLine, double requiredKg, double bagKg)
        { Code = code; Name = name; AllergenLine = allergenLine; RequiredKg = requiredKg; BagKg = bagKg; }
    }

    internal sealed class WorkOrder
    {
        public readonly string No, Product, Line, AllergenLine;
        public readonly int Batches;
        public readonly IList<RecipeLine> Lines;
        public WorkOrder(string no, string product, int batches, string line, string allergenLine, IList<RecipeLine> lines)
        { No = no; Product = product; Batches = batches; Line = line; AllergenLine = allergenLine; Lines = lines; }
    }

    /// <summary>재고 Lot 한 건. 라벨 바코드 = Lot 번호. 잔량은 출고 때 소스가 줄인다.</summary>
    internal sealed class StockLot
    {
        public readonly string Lot, MaterialCode, MaterialName, AllergenLine;   // AllergenLine: 알레르기 유발 원료의 라인 구분(밀·대두·우유), 아니면 ""
        public readonly DateTime ReceivedOn, ExpiresOn;
        public double RemainKg;
        public StockLot(string lot, string materialCode, string materialName, string allergenLine, DateTime receivedOn, DateTime expiresOn, double remainKg)
        { Lot = lot; MaterialCode = materialCode; MaterialName = materialName; AllergenLine = allergenLine; ReceivedOn = receivedOn; ExpiresOn = expiresOn; RemainKg = remainKg; }
    }

    internal sealed class IssueRecord
    {
        public readonly DateTime At;
        public readonly string WorkOrderNo, MaterialName, Lot, Result;
        public readonly double Kg;      // NG는 0
        public readonly bool Ok;
        public IssueRecord(DateTime at, string workOrderNo, string materialName, string lot, double kg, bool ok, string result)
        { At = at; WorkOrderNo = workOrderNo; MaterialName = materialName; Lot = lot; Kg = kg; Ok = ok; Result = result; }
    }
}
