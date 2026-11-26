using System;
using System.Collections.Generic;
using System.Linq;

namespace RawMaterialIssueTerminal.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 지시 WO-261126-007(식빵 반죽 4배치)에 소맥분 200.0 kg 등이 이미 출고된 상태.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private readonly DateTime _today = DateTime.Today;
        private readonly List<StockLot> _lots = new List<StockLot>();
        private readonly List<IssueRecord> _history = new List<IssueRecord>();
        private readonly WorkOrder _order;
        private readonly Dictionary<string, double> _issued = new Dictionary<string, double>
        { { "RM-1021", 200.0 }, { "RM-1034", 40.0 }, { "RM-1050", 10.0 }, { "RM-1068", 2.0 }, { "RM-1077", 0.0 } };

        public SampleMainViewSource()
        {
            // 필요량 = 배치당 x 4. 포대 중량: 소맥분 25, 설탕 20, 소금 10, 이스트 2, 쇼트닝 12 kg
            _order = new WorkOrder("WO-261126-007", "식빵 반죽 (FP-2301)", 4, "2라인", "밀", new[]
            {
                new RecipeLine("RM-1021", "소맥분", "밀", 400.0, 25.0),
                new RecipeLine("RM-1034", "설탕", "", 30.0, 20.0),
                new RecipeLine("RM-1050", "소금", "", 10.0, 10.0),
                new RecipeLine("RM-1068", "이스트", "", 6.0, 2.0),
                new RecipeLine("RM-1077", "쇼트닝", "", 24.0, 12.0),
            });
            AddLot("L261001-01", "RM-1021", "소맥분", "밀", -56, -2, 25.0);     // 유통기한 경과
            AddLot("L261105-01", "RM-1021", "소맥분", "밀", -21, 4, 150.0);     // 7일 이내 = 주의, FIFO 1순위
            AddLot("L261110-02", "RM-1021", "소맥분", "밀", -16, 30, 500.0);
            AddLot("L261120-03", "RM-1021", "소맥분", "밀", -6, 60, 800.0);
            AddLot("L261124-01", "RM-1021", "소맥분", "밀", -2, 90, 600.0);
            AddLot("L261030-01", "RM-1034", "설탕", "", -27, 90, 400.0);
            AddLot("L261118-02", "RM-1034", "설탕", "", -8, 120, 800.0);
            AddLot("L261015-01", "RM-1050", "소금", "", -42, 365, 100.0);
            AddLot("L261122-01", "RM-1050", "소금", "", -4, 400, 200.0);
            AddLot("L261112-04", "RM-1068", "이스트", "", -14, 6, 30.0);        // 주의
            AddLot("L261124-02", "RM-1068", "이스트", "", -2, 40, 60.0);
            AddLot("L261108-01", "RM-1077", "쇼트닝", "", -18, 20, 120.0);
            AddLot("L261121-02", "RM-1077", "쇼트닝", "", -5, 75, 240.0);
            AddLot("L261119-05", "RM-2015", "탈지분유", "우유", -7, 100, 500.0); // 이 지시서에 없는 원료(알레르기 우유)

            var now = DateTime.Now;
            Hist(now, -3, "소맥분", "L261105-01", 25.0, "OK");
            Hist(now, -9, "설탕", "L261030-01", 20.0, "OK");
            Hist(now, -16, "소금", "L261015-01", 10.0, "OK");
            Hist(now, -22, "소맥분", "L261110-02", 0.0, "NG · FIFO 위반");
            Hist(now, -27, "이스트", "L261112-04", 2.0, "OK");
            Hist(now, -41, "소맥분", "L261105-01", 24.5, "OK");
            Hist(now, -58, "탈지분유", "L261119-05", 0.0, "NG · 알레르기 라인 불일치");
            Hist(now, -92, "소맥분", "L261105-01", 25.0, "OK");
            Hist(now, -104, "쇼트닝", "L261108-01", 12.0, "OK");
            Hist(now, -118, "소금", "L261015-01", 0.0, "NG · 유통기한 경과");
            Hist(now, -131, "소맥분", "L261105-01", 18.5, "OK");
            Hist(now, -149, "이스트", "L261112-04", 1.5, "OK");
            Hist(now, -166, "설탕", "L261030-01", 12.5, "OK");
            Hist(now, -183, "소맥분", "L261105-01", 22.0, "OK");
            Hist(now, -75, "설탕", "L261118-02", 20.0, "OK · FIFO 예외 승인");
        }

        private void AddLot(string lot, string code, string name, string allergen, int receivedDays, int expiresDays, double remain)
        {
            _lots.Add(new StockLot(lot, code, name, allergen, _today.AddDays(receivedDays), _today.AddDays(expiresDays), remain));
        }

        private void Hist(DateTime now, int minutes, string material, string lot, double kg, string result)
        {
            _history.Add(new IssueRecord(now.AddMinutes(minutes), _order.No, material, lot, kg, result.StartsWith("OK"), result));
        }

        public WorkOrder FindWorkOrder(string code) { return code == _order.No ? _order : null; }

        public IDictionary<string, double> GetIssuedKg(string workOrderNo)
        {
            return new Dictionary<string, double>(_issued);
        }

        public IList<StockLot> GetLots(string materialCode)
        {
            return _lots.Where(l => l.MaterialCode == materialCode).ToList();
        }

        public StockLot FindLot(string lot) { return _lots.FirstOrDefault(l => l.Lot == lot); }

        public IList<IssueRecord> GetTodayHistory() { return _history.OrderByDescending(h => h.At).ToList(); }

        public void RecordIssue(IssueRecord record, double lotRemainAfterKg)
        {
            _history.Add(record);
            if (!record.Ok) return;
            var lot = FindLot(record.Lot);
            if (lot != null) lot.RemainKg = lotRemainAfterKg;
            var line = _order.Lines.FirstOrDefault(l => l.Name == record.MaterialName);
            if (line != null) _issued[line.Code] = (_issued.ContainsKey(line.Code) ? _issued[line.Code] : 0) + record.Kg;
        }

        public string FindApprover(string employeeNo)
        {
            if (employeeNo == "E10231") return "박승인";
            if (employeeNo == "E10458") return "이관리";
            return null;
        }
    }
}
