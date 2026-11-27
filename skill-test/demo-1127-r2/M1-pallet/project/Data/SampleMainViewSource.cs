using System;
using System.Collections.Generic;

namespace PalletPacker.Data
{
    /// <summary>화면 확인·render용 샘플 데이터: 3층 5번째 박스까지 21개 적재된 팔레트.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const string PartNo = "KF310";
        private readonly List<PlacedBox> _boxes = new List<PlacedBox>();

        public SampleMainViewSource()
        {
            // 박스 간격을 서로 다르게(13~41 s) 해서 되풀이로 보이지 않게 한다.
            var at = new DateTime(2026, 11, 27, 9, 2, 11);
            int[] gaps = { 23, 17, 31, 14, 26, 38, 19, 41, 16, 28, 35, 13, 22, 30, 18, 27, 37, 15, 24, 33 };
            for (int i = 0; i < 21; i++)
            {
                _boxes.Add(new PlacedBox(Label(i + 1), at));
                if (i < gaps.Length) at = at.AddSeconds(gaps[i]);
            }
        }

        private static string Label(int serial)
        {
            return "BX-" + PartNo + "-" + (9120 + serial).ToString("00000");
        }

        public WorkOrder GetOrder() { return new WorkOrder(PartNo, "한빛전자", PalletRules.TargetBoxes); }

        public IList<PlacedBox> GetPlacedBoxes() { return _boxes.ToArray(); }

        public string RegisterBox(string label)
        {
            string[] p = label.Split('-');
            if (p.Length != 3 || p[0] != "BX" || p[2].Length != 5) return "박스 라벨 형식 아님 · " + label;
            if (p[1] != PartNo) return "품번 불일치 · " + p[1] + " ≠ " + PartNo;
            foreach (var b in _boxes) if (b.Label == label) return "이미 스캔한 박스 · " + label;
            if (_boxes.Count >= PalletRules.TargetBoxes) return "적재 완료 · 팔레트를 마감하세요";
            _boxes.Add(new PlacedBox(label, DateTime.Now));
            return null;
        }

        public void ClosePallet(string palletLabel, string badge, decimal totalKg)
        {
            // 샘플: 기록 없이 다음 팔레트로 비운다.
            _boxes.Clear();
        }
    }
}
