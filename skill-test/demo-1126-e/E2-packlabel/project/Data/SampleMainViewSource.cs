using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ShipPackVerifier.Data
{
    /// <summary>
    /// 화면 확인·render용 샘플 데이터(출하 지시 1건, 완료 9박스 + 현재 박스 22개).
    /// 시리얼 형식 품번-YYMMDD-5자리, 박스 번호 SO번호-B두자리.
    /// </summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const string SerialDate = "261126";
        private const int CompletedBoxCount = 9, CurrentBoxCount = 22;
        private static readonly Regex SerialFormat = new Regex(@"^(.+)-(\d{6})-(\d{5})$");
        // 품질 시스템에서 내려온 보류 시리얼(샘플)
        private static readonly string[] HoldSerials = { "ECU-7720A-261126-00651", "ECU-7720A-261126-00688", "ECU-7720A-261126-00703" };
        // 반장 사원증(샘플)
        private static readonly string[] SupervisorBadges = { "SV-1007", "SV-1033" };
        private static readonly string[] SupervisorNames = { "김만석 반장", "이정훈 반장" };

        private readonly ShipOrder _order = new ShipOrder("SO-1126-0142", "HM", "울산 2공장", "ECU-7720A", 250, 24);
        private readonly List<PackedSerial> _current = new List<PackedSerial>();      // 오래된 것이 앞
        private readonly List<PackedBox> _completed = new List<PackedBox>();          // 오래된 것이 앞
        private readonly Dictionary<string, string> _boxOfSerial = new Dictionary<string, string>();
        private ScanResult _lastScan;
        private string _lastSerial = "";

        public SampleMainViewSource()
        {
            DateTime start = new DateTime(2026, 11, 26, 8, 5, 12);
            int n = 0;
            for (int box = 1; box <= CompletedBoxCount + 1; box++)
            {
                int count = box <= CompletedBoxCount ? _order.BoxCapacity : CurrentBoxCount;
                DateTime last = start;
                for (int i = 0; i < count; i++)
                {
                    n++;
                    last = start.AddSeconds(n * 24 + (n * 7 % 13) * 3);
                    string serial = MakeSerial(n);
                    _boxOfSerial[serial] = BoxNo(box);
                    if (box > CompletedBoxCount) _current.Add(new PackedSerial(i + 1, serial, last));
                }
                if (box <= CompletedBoxCount) _completed.Add(new PackedBox(BoxNo(box), count, last.AddSeconds(3 + box % 3)));
            }
            _lastSerial = HoldSerials[0];
            _lastScan = new ScanResult(false, "품질 보류");
        }

        private string MakeSerial(int n) { return _order.PartNo + "-" + SerialDate + "-" + (400 + n).ToString("00000", CultureInfo.InvariantCulture); }
        private string BoxNo(int box) { return _order.OrderNo + "-B" + box.ToString("00", CultureInfo.InvariantCulture); }

        private int DoneQuantity() { int q = 0; foreach (PackedBox b in _completed) q += b.Quantity; return q; }

        public ShipOrder GetOrder() { return _order; }

        public BoxProgress GetCurrentBox()
        {
            int doneQty = DoneQuantity();
            bool orderDone = doneQty >= _order.Quantity;
            int target = Math.Min(_order.BoxCapacity, _order.Quantity - doneQty);   // 마지막 박스는 남은 수량
            string boxNo = BoxNo(_completed.Count + 1);
            if (orderDone) return new BoxProgress(BoxNo(_completed.Count), 0, 0, false, "지시 수량 완료", true);
            bool canPrint = _current.Count == target;
            string reason = canPrint ? "" : "박스 미완료 · " + _current.Count + " / " + target;
            return new BoxProgress(boxNo, _current.Count, target, canPrint, reason, false);
        }

        public IList<PackedSerial> GetCurrentSerials()
        {
            var list = new List<PackedSerial>(_current);
            list.Reverse();
            return list;
        }

        public IList<PackedBox> GetCompletedBoxes()
        {
            var list = new List<PackedBox>(_completed);
            list.Reverse();
            return list;
        }

        public ScanResult GetLastScan(out string serial) { serial = _lastSerial; return _lastScan; }

        public ScanResult Scan(string serial)
        {
            ScanResult result = Judge(serial);
            _lastScan = result;
            _lastSerial = serial;
            if (result.Ok)
            {
                _current.Add(new PackedSerial(_current.Count + 1, serial, DateTime.Now));
                _boxOfSerial[serial] = GetCurrentBox().BoxNo;
            }
            return result;
        }

        private ScanResult Judge(string serial)
        {
            Match m = SerialFormat.Match(serial);
            if (!m.Success) return new ScanResult(false, "시리얼 형식 오류");
            if (m.Groups[1].Value != _order.PartNo) return new ScanResult(false, "다른 품번 · " + m.Groups[1].Value + " ≠ " + _order.PartNo);
            string box;
            if (_boxOfSerial.TryGetValue(serial, out box)) return new ScanResult(false, "이미 담음 · " + box.Substring(box.Length - 3));
            if (Array.IndexOf(HoldSerials, serial) >= 0) return new ScanResult(false, "품질 보류");
            BoxProgress p = GetCurrentBox();
            if (p.OrderDone) return new ScanResult(false, "지시 수량 완료");
            if (p.Count >= p.Capacity) return new ScanResult(false, "박스 입수 초과 · " + p.Count + " / " + p.Capacity);
            return new ScanResult(true, "");
        }

        public bool PrintCurrentLabel()
        {
            BoxProgress p = GetCurrentBox();
            if (!p.CanPrint) return false;
            _completed.Add(new PackedBox(p.BoxNo, p.Count, DateTime.Now));
            _current.Clear();
            return true;
        }

        public string FindSupervisor(string badge)
        {
            int i = Array.IndexOf(SupervisorBadges, badge);
            return i < 0 ? null : SupervisorNames[i];
        }

        public bool ReprintLabel(string boxNo, string reason, string supervisorBadge)
        {
            if (FindSupervisor(supervisorBadge) == null) return false;
            foreach (PackedBox b in _completed) if (b.BoxNo == boxNo) return true;   // 실제로는 프린터로 전송
            return false;
        }
    }
}
