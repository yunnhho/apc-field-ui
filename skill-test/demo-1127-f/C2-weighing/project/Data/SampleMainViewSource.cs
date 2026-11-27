using System;
using System.Collections.Generic;

namespace RubberWeighing.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 2종 확정(경계값 포함), 3번째 원료 대기 중.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const int RampSteps = 14;
        private readonly List<Material> _recipe = new List<Material>
        {
            new Material(1, "NBR", "생고무", 25.00m),
            new Material(2, "CB", "카본블랙", 12.50m),
            new Material(3, "OIL", "오일", 4.00m),
            new Material(4, "ZNO", "산화아연", 1.25m),
            new Material(5, "STA", "스테아르산", 0.50m),
            new Material(6, "SUL", "황", 0.40m),
        };
        private readonly List<WeighRecord> _done = new List<WeighRecord>();
        private Material _current;
        private int _tick;

        public SampleMainViewSource()
        {
            var t = new DateTime(2026, 11, 27, 9, 12, 41);
            _done.Add(new WeighRecord(1, "ML-NBR-261124-02", 25.12m, "EMP-10427", t));            // 상한(25.125) 이내 경계
            _current = _recipe[2];
            _tick = RampSteps;
            _done.Add(new WeighRecord(2, "ML-CB-261125-01", 12.44m, "EMP-10427", t.AddMinutes(4).AddSeconds(17)));  // 하한(12.4375) 이내 경계
        }

        // 샘플은 오일(3번)을 LOT 스캔한 채 목표 가득 올린 상태로 시작한다(상한과 같은 값 = OK 경계).
        public string ActiveLot { get { return "ML-OIL-261126-01"; } }

        public string RecipeName { get { return "RC-NBR-07"; } }
        public string BatchNo { get { return "B261127-03"; } }
        public IList<Material> GetRecipe() { return _recipe; }
        public IList<WeighRecord> GetDoneRecords() { return _done; }

        public decimal ReadScale()
        {
            if (_current == null) return 0m;
            // 샘플: 목표까지 올라가 상한에 걸친 값에서 멈춘다. 실제는 저울에서 읽는다.
            if (_tick < RampSteps) _tick++;
            decimal unit = _current.Decimals == 2 ? 0.01m : 0.001m;
            decimal v = _current.Upper * _tick / RampSteps;
            return Math.Floor(v / unit) * unit;
        }

        public void StartWeighing(Material material) { _current = material; _tick = 0; }
        public void Record(WeighRecord record) { _done.Add(record); }
    }
}
