using System;
using System.Collections.Generic;
using System.Linq;

namespace RawMaterialIssueTerminal.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 지시 WO-261126-007(12배치), 오늘 2026-11-26.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const string OrderCode = "WO-261126-007";
        private static readonly DateTime Now = new DateTime(2026, 11, 26, 10, 42, 18);

        private readonly OrderInfo _order = new OrderInfo(OrderCode, "소프트 쿠키 반죽", 12, "2라인", "A (밀·우유)");
        private List<MaterialLine> _materials = new List<MaterialLine>
        {
            new MaterialLine("RM-1021", "소맥분", "밀", 300.0, 150.0),
            new MaterialLine("RM-1033", "설탕", "", 96.0, 96.0),
            new MaterialLine("RM-1058", "버터", "우유", 60.0, 60.0),
            new MaterialLine("RM-1064", "우유분말", "우유", 36.0, 0.0),
            new MaterialLine("RM-1072", "대두레시틴", "대두", 12.0, 12.5),
            new MaterialLine("RM-1090", "소금", "", 4.8, 2.4),
        };
        private readonly List<LotStock> _lots = new List<LotStock>
        {
            Lot("L261022-05", "RM-1021", 10, 22, 2026, 11, 25, 25.0, "A (밀·우유)"),
            Lot("L261108-01", "RM-1021", 11, 8, 2026, 12, 2, 75.0, "A (밀·우유)"),
            Lot("L261120-03", "RM-1021", 11, 20, 2027, 5, 20, 400.0, "A (밀·우유)"),
            Lot("L261122-01", "RM-1021", 11, 22, 2027, 6, 2, 500.0, "B (대두)"),
            Lot("L261103-02", "RM-1033", 11, 3, 2027, 11, 3, 800.0, ""),
            Lot("L261112-01", "RM-1058", 11, 12, 2027, 1, 14, 120.0, "A (밀·우유)"),
            Lot("L261117-02", "RM-1064", 11, 17, 2027, 2, 9, 200.0, "A (밀·우유)"),
            Lot("L261119-01", "RM-1072", 11, 19, 2027, 3, 18, 40.0, "A (밀·우유)"),
            Lot("L261101-01", "RM-1090", 11, 1, 2028, 10, 31, 90.0, ""),
        };
        private readonly List<IssueRecord> _history = BuildHistory();

        public DateTime Today { get { return Now.Date; } }

        public string[] StartupScans
        {
            get { return new[] { OrderCode, "RM-1021/L261120-03/25.0" }; }   // 뒤 순위 Lot를 스캔한 직후(FIFO 위반)
        }

        public OrderInfo FindOrder(string code) { return code == OrderCode ? _order : null; }

        public IList<MaterialLine> GetMaterials(string orderCode) { return _materials.ToList(); }

        public IList<LotStock> GetLots(string materialCode) { return _lots.Where(l => l.MaterialCode == materialCode).ToList(); }

        public IList<IssueRecord> GetTodayHistory() { return _history.ToList(); }

        public string FindApproverName(string employeeCode)
        {
            if (employeeCode == "EMP-2041") return "김관리";
            if (employeeCode == "EMP-2077") return "박품질";
            return null;
        }

        public void RecordIssue(string orderCode, BagLabel bag, string note)
        {
            MaterialLine m = _materials.First(x => x.Code == bag.MaterialCode);
            _materials = _materials.Select(x => x == m
                ? new MaterialLine(x.Code, x.Name, x.Allergen, x.Required, Math.Round(x.Issued + bag.Weight, 1)) : x).ToList();
            int i = _lots.FindIndex(l => l.Lot == bag.Lot);
            LotStock lot = _lots[i];
            _lots[i] = new LotStock(lot.Lot, lot.MaterialCode, lot.ReceivedOn, lot.ExpiresOn, Math.Max(0.0, lot.Remaining - bag.Weight), lot.AllergenLine);
            _history.Insert(0, new IssueRecord(DateTime.Now, orderCode, m.Name, bag.Lot, bag.Weight, note));
        }

        public void CompleteOrder(string orderCode) { /* 실제 연결에서 지시 종료 처리 */ }

        private static LotStock Lot(string lot, string material, int rm, int rd, int ey, int em, int ed, double remaining, string line)
        {
            return new LotStock(lot, material, new DateTime(2026, rm, rd), new DateTime(ey, em, ed), remaining, line);
        }

        // 금일 이력: 다른 지시분 포함. 소맥분 007분 150.0 kg(25.0 x 6)과 다른 원료 출고량이 지시 출고량과 맞는다.
        private static List<IssueRecord> BuildHistory()
        {
            var t = new DateTime(2026, 11, 26, 10, 31, 6);
            const string ex = "FIFO 예외 승인 · EMP-2041";
            return new List<IssueRecord>
            {
                new IssueRecord(t, OrderCode, "소맥분", "L261108-01", 25.0, ""),
                new IssueRecord(t.AddMinutes(-4).AddSeconds(-17), OrderCode, "소금", "L261101-01", 2.4, ""),
                new IssueRecord(t.AddMinutes(-9).AddSeconds(11), OrderCode, "소맥분", "L261108-01", 25.0, ""),
                new IssueRecord(t.AddMinutes(-13).AddSeconds(-5), OrderCode, "대두레시틴", "L261119-01", 12.5, ""),
                new IssueRecord(t.AddMinutes(-18).AddSeconds(23), OrderCode, "버터", "L261112-01", 25.0, ""),
                new IssueRecord(t.AddMinutes(-21).AddSeconds(-40), OrderCode, "버터", "L261112-01", 25.0, ""),
                new IssueRecord(t.AddMinutes(-27).AddSeconds(9), OrderCode, "소맥분", "L261108-01", 25.0, ""),
                new IssueRecord(t.AddMinutes(-33).AddSeconds(-2), OrderCode, "설탕", "L261103-02", 25.0, ""),
                new IssueRecord(t.AddMinutes(-39).AddSeconds(31), OrderCode, "버터", "L261112-01", 10.0, ""),
                new IssueRecord(t.AddMinutes(-46).AddSeconds(-14), OrderCode, "설탕", "L261103-02", 25.0, ""),
                new IssueRecord(t.AddMinutes(-52).AddSeconds(8), OrderCode, "소맥분", "L261120-03", 25.0, ex),
                new IssueRecord(t.AddMinutes(-58).AddSeconds(-29), OrderCode, "설탕", "L261103-02", 25.0, ""),
                new IssueRecord(t.AddMinutes(-66).AddSeconds(16), OrderCode, "설탕", "L261103-02", 21.0, ""),
                new IssueRecord(t.AddMinutes(-74).AddSeconds(-7), OrderCode, "소맥분", "L261108-01", 25.0, ""),
                new IssueRecord(t.AddMinutes(-81).AddSeconds(19), OrderCode, "소맥분", "L261108-01", 25.0, ""),
                new IssueRecord(t.AddMinutes(-90).AddSeconds(27), "WO-261126-006", "소금", "L261101-01", 5.5, ""),
            };
        }
    }
}
