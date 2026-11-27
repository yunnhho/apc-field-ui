using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PalletPacker.Data
{
    /// <summary>화면 확인·render용 샘플 데이터: 3층 5번째 박스까지 쌓은 팔레트(21/40).</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly Regex BoxPattern = new Regex(@"^BX-([^-]+)-\d{5}$");
        private readonly WorkOrder _order = new WorkOrder("KF3320", "대성푸드", 40);
        private readonly List<string> _boxes = new List<string>();
        private readonly DateTime?[] _layerDoneAt = new DateTime?[PalletRules.Layers];

        public SampleMainViewSource()
        {
            for (int i = 0; i < 21; i++) _boxes.Add("BX-KF3320-" + (10117 + i * 3 % 29 + i).ToString("00000"));
            _layerDoneAt[0] = new DateTime(2026, 11, 27, 9, 41, 8);
            _layerDoneAt[1] = new DateTime(2026, 11, 27, 10, 18, 52);
        }

        public WorkOrder GetWorkOrder() { return _order; }

        public PalletState GetPallet() { return new PalletState(_boxes.Count, (DateTime?[])_layerDoneAt.Clone()); }

        public string AddBox(string label)
        {
            if (_boxes.Count >= _order.TargetBoxes) return "목표 " + _order.TargetBoxes + "박스를 다 쌓았습니다 · 팔레트를 마감하세요";
            Match m = BoxPattern.Match(label);
            if (!m.Success) return "박스 라벨 형식이 아닙니다 · " + label;
            if (m.Groups[1].Value != _order.PartNo) return "품번 불일치 · " + m.Groups[1].Value + " ≠ " + _order.PartNo;
            if (_boxes.Contains(label)) return "이미 스캔한 박스입니다 · " + label;
            _boxes.Add(label);
            if (_boxes.Count % PalletRules.CellsPerLayer == 0) _layerDoneAt[_boxes.Count / PalletRules.CellsPerLayer - 1] = DateTime.Now;
            return null;
        }

        public void ClosePallet(string palletLabel, string employeeBadge, decimal totalKg)
        {
            _boxes.Clear();
            for (int i = 0; i < _layerDoneAt.Length; i++) _layerDoneAt[i] = null;
        }
    }
}
