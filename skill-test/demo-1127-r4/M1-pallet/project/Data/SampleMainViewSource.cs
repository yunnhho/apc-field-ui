using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PalletPacker.Data
{
    /// <summary>화면 확인·render용 샘플 데이터: 3층 5번까지 21박스 적재 + 거부 3건.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly Regex BoxPattern = new Regex(@"^BX-([A-Za-z0-9]+)-(\d{5})$");
        private readonly WorkOrder _order = new WorkOrder("KD4410", "대성모터스", PalletRules.TargetBoxes);
        private readonly List<BoxRecord> _boxes = new List<BoxRecord>();
        private readonly List<ScanEntry> _log = new List<ScanEntry>();

        public SampleMainViewSource()
        {
            // 박스 간격을 일정하지 않게(31~58초), 박스 번호는 건너뛰기도 한다.
            DateTime at = DateTime.Today.AddHours(8).AddMinutes(47).AddSeconds(12);
            int serial = 20318;
            for (int seq = 1; seq <= 21; seq++)
            {
                at = at.AddSeconds(31 + seq * 11 % 28);
                serial += seq % 6 == 0 ? 2 : 1;
                string label = "BX-KD4410-" + serial.ToString("00000");
                var box = new BoxRecord(seq, label, at);
                _boxes.Add(box);
                if (seq >= 7) _log.Add(new ScanEntry(at, label, box.Layer + "층 " + box.Cell + "번", true, ""));
                if (seq == 9) _log.Add(new ScanEntry(at.AddSeconds(14), "BX-KD4420-07751", "", false, "품번 불일치 · KD4420 ≠ KD4410"));
                if (seq == 14) _log.Add(new ScanEntry(at.AddSeconds(9), _boxes[12].Label, "", false, "이미 스캔한 박스"));
                if (seq == 18) _log.Add(new ScanEntry(at.AddSeconds(22), "BX-KD4410-2031", "", false, "라벨 형식 오류"));
            }
        }

        public WorkOrder GetWorkOrder() { return _order; }
        public IList<BoxRecord> GetBoxes() { return _boxes.AsReadOnly(); }
        public IList<ScanEntry> GetScanLog() { return _log.AsReadOnly(); }

        public bool ScanBox(string label, out string reason)
        {
            reason = Check(label);
            DateTime now = DateTime.Now;
            if (reason != null)
            {
                _log.Add(new ScanEntry(now, label, "", false, reason));
                return false;
            }
            var box = new BoxRecord(_boxes.Count + 1, label, now);
            _boxes.Add(box);
            _log.Add(new ScanEntry(now, label, box.Layer + "층 " + box.Cell + "번", true, ""));
            return true;
        }

        private string Check(string label)
        {
            if (_boxes.Count >= _order.TargetBoxes) return "팔레트 가득 참 · 마감 필요";
            Match m = BoxPattern.Match(label);
            if (!m.Success) return "라벨 형식 오류";
            if (m.Groups[1].Value != _order.PartNo) return "품번 불일치 · " + m.Groups[1].Value + " ≠ " + _order.PartNo;
            foreach (BoxRecord b in _boxes) if (b.Label == label) return "이미 스캔한 박스";
            return null;
        }

        public void ClosePallet(string palletLabel, string badgeCode, decimal totalKg)
        {
            // 실제로는 마감 기록 저장. 샘플은 빈 팔레트로 돌아간다.
            _boxes.Clear();
            _log.Clear();
        }
    }
}
