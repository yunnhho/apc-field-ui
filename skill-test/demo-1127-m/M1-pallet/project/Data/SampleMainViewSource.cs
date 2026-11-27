using System;
using System.Collections.Generic;

namespace PalletPacker.Data
{
    /// <summary>화면 확인·render용 샘플 데이터: 3층 5개째까지 쌓은 팔레트(21/40).</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private readonly WorkOrder _order = new WorkOrder("KF3320A", "한빛모터스", PalletRules.TargetBoxes);
        private readonly List<string> _boxes = new List<string>();
        private readonly DateTime?[] _completed = new DateTime?[PalletRules.Layers];

        public SampleMainViewSource()
        {
            for (int i = 0; i < 21; i++) _boxes.Add("BX-KF3320A-" + (10412 + i * 3 + i / 5).ToString("00000"));
            _completed[0] = DateTime.Today.AddHours(9).AddMinutes(12).AddSeconds(41);
            _completed[1] = DateTime.Today.AddHours(9).AddMinutes(31).AddSeconds(7);
        }

        public WorkOrder GetWorkOrder() { return _order; }

        public int GetBoxCount() { return _boxes.Count; }

        public IList<LayerInfo> GetLayers()
        {
            var list = new List<LayerInfo>();
            for (int i = 0; i < PalletRules.Layers; i++)
            {
                int count = Math.Max(0, Math.Min(PalletRules.CellsPerLayer, _boxes.Count - i * PalletRules.CellsPerLayer));
                list.Add(new LayerInfo(i + 1, count, _completed[i]));
            }
            return list;
        }

        public BoxScanResult ScanBox(string label)
        {
            var m = PalletRules.BoxLabel.Match(label ?? "");
            if (!m.Success) return new BoxScanResult(false, "박스 라벨 형식이 아님");
            if (_boxes.Count >= _order.TargetBoxes) return new BoxScanResult(false, "팔레트가 가득 참 · 마감 필요");
            if (m.Groups[1].Value != _order.PartNo) return new BoxScanResult(false, "품번 불일치 · " + m.Groups[1].Value + " ≠ " + _order.PartNo);
            if (_boxes.Contains(label)) return new BoxScanResult(false, "이미 스캔한 박스 · " + label);

            _boxes.Add(label);
            if (_boxes.Count % PalletRules.CellsPerLayer == 0) _completed[_boxes.Count / PalletRules.CellsPerLayer - 1] = DateTime.Now;
            return new BoxScanResult(true, "");
        }

        public void ClosePallet(string palletLabel, string employeeId, decimal totalKg)
        {
            _boxes.Clear();
            for (int i = 0; i < _completed.Length; i++) _completed[i] = null;
        }
    }
}
