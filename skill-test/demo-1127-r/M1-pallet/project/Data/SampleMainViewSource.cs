using System;
using System.Collections.Generic;

namespace PalletPacker.Data
{
    /// <summary>화면 확인·render용 샘플 데이터: 3층 5번째 칸까지 21박스 적재.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const string SamplePart = "AB2417";
        private const int SampleBoxes = 21;
        private readonly WorkOrder _order = new WorkOrder(SamplePart, "한빛전자", 40);
        private readonly List<string> _codes = new List<string>();
        private readonly DateTime?[] _doneAt = new DateTime?[PalletState.Layers];

        public SampleMainViewSource()
        {
            DateTime at = new DateTime(2026, 11, 27, 13, 41, 8);
            for (int i = 1; i <= SampleBoxes; i++)
            {
                at = at.AddSeconds(38 + i * 7 % 23);
                _codes.Add("BX-" + SamplePart + "-" + (i + 9000).ToString("00000"));
                if (i % PalletState.CellsPerLayer == 0) _doneAt[i / PalletState.CellsPerLayer - 1] = at;
            }
        }

        public WorkOrder GetWorkOrder() { return _order; }

        public PalletState GetPallet() { return new PalletState(_codes.Count, _order.TargetBoxes, (DateTime?[])_doneAt.Clone()); }

        public ScanResult ScanBox(string code)
        {
            string part = PalletRules.PartOfBox(code);
            if (part == null) return new ScanResult(false, "박스 라벨이 아닙니다 · " + code);
            if (part != _order.PartNo) return new ScanResult(false, "품번 불일치 · " + part + " ≠ " + _order.PartNo);
            if (_codes.Contains(code)) return new ScanResult(false, "이미 스캔한 박스입니다 · " + code);
            if (GetPallet().IsFull) return new ScanResult(false, "팔레트가 가득 찼습니다 · 팔레트 마감을 누르세요");

            _codes.Add(code);
            if (_codes.Count % PalletState.CellsPerLayer == 0) _doneAt[_codes.Count / PalletState.CellsPerLayer - 1] = DateTime.Now;
            return new ScanResult(true, "");
        }

        public bool ClosePallet(string palletLabel, string badge, decimal totalKg) { return true; }
    }
}
