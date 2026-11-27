using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PalletPacker.Data
{
    /// <summary>화면 확인·render용 샘플 데이터: 3층 5번째 박스까지(21/40) 쌓은 상태.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly Regex BoxLabel = new Regex(@"^BX-([A-Z0-9]+)-(\d{5})$");
        private readonly WorkOrder _order = new WorkOrder("AB1042", "한성전자", 40);
        private readonly List<PlacedBox> _boxes = new List<PlacedBox>();

        public SampleMainViewSource()
        {
            // 간격을 서로 다르게(되풀이 없이). 1층 8개 완료, 2층 8개 완료, 3층 5개 진행 중.
            int[] gapSeconds = { 0, 14, 11, 17, 12, 19, 13, 16, 41, 12, 15, 18, 11, 20, 14, 13, 38, 15, 12, 17, 14 };
            DateTime at = DateTime.Today.AddHours(13).AddMinutes(2).AddSeconds(9);
            for (int i = 0; i < gapSeconds.Length; i++)
            {
                at = at.AddSeconds(gapSeconds[i]);
                _boxes.Add(new PlacedBox("BX-AB1042-" + (30417 + i * 3 % 7 + i).ToString("00000"), at));
            }
        }

        public WorkOrder GetWorkOrder() { return _order; }

        public IList<PlacedBox> GetPlacedBoxes() { return _boxes.AsReadOnly(); }

        public string PlaceBox(string code)
        {
            Match m = BoxLabel.Match(code ?? "");
            if (!m.Success) return "박스 라벨 형식 오류 · " + code;
            if (m.Groups[1].Value != _order.PartNo) return "품번 불일치 · " + m.Groups[1].Value + " ≠ " + _order.PartNo;
            if (_boxes.Exists(b => b.Code == code)) return "이미 스캔한 박스 · " + code;
            if (_boxes.Count >= _order.TargetBoxes) return "팔레트 가득 참 · " + _order.TargetBoxes + "/" + _order.TargetBoxes;
            _boxes.Add(new PlacedBox(code, DateTime.Now));
            return null;
        }

        public void ClosePallet(string palletLabel, string badge, decimal totalKg) { _boxes.Clear(); }
    }
}
