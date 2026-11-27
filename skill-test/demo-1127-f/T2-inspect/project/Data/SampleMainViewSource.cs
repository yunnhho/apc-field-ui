using System;
using System.Collections.Generic;

namespace DiscFinalCheck.Data
{
    /// <summary>화면 확인·render용 샘플 데이터.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        // 외경, 두께, 런아웃, 평행도 (최신부터). 경계값 OK 행 포함: 280.10, 21.95, 0.030, 0.010
        private static readonly decimal[][] Values =
        {
            new[] { 280.14m, 22.02m, 0.024m, 0.008m },   // NG 외경
            new[] { 279.97m, 22.01m, 0.018m, 0.006m },
            new[] { 280.04m, 21.98m, 0.027m, 0.009m },
            new[] { 280.10m, 22.03m, 0.011m, 0.004m },   // 외경 상한 경계 OK
            new[] { 279.93m, 22.00m, 0.021m, 0.007m },
            new[] { 280.02m, 22.04m, 0.016m, 0.010m },   // 평행도 경계 OK
            new[] { 279.99m, 22.01m, 0.041m, 0.012m },   // NG 런아웃·평행도(폐기 등록됨)
            new[] { 280.06m, 21.97m, 0.023m, 0.005m },
            new[] { 280.00m, 21.95m, 0.026m, 0.003m },   // 두께 하한 경계 OK
            new[] { 279.96m, 22.05m, 0.030m, 0.009m },   // 두께 상한·런아웃 경계 OK
            new[] { 279.90m, 21.99m, 0.017m, 0.007m },   // 외경 하한 경계 OK
            new[] { 280.01m, 22.07m, 0.020m, 0.008m },   // NG 두께
            new[] { 279.95m, 22.03m, 0.012m, 0.004m },
        };

        private const int LastSerialNumber = 148;
        private static readonly DateTime LastTime = new DateTime(2026, 11, 27, 14, 32, 18);
        private static readonly int[] GapSeconds = { 47, 63, 39, 71, 52, 44, 68, 41, 58, 75, 36, 49, 66, 54, 43, 61, 38 };

        private readonly List<DiscMeasurement> _history = new List<DiscMeasurement>();
        private readonly List<NgRecord> _records = new List<NgRecord>();
        private int _total = 148, _ng = 4;

        public SampleMainViewSource()
        {
            DateTime at = LastTime;
            for (int i = 0; i < Values.Length; i++)
            {
                decimal[] v = Values[i];
                _history.Add(new DiscMeasurement(SerialOf(LastSerialNumber - i), at, v[0], v[1], v[2], v[3]));
                if (i < GapSeconds.Length) at = at.AddSeconds(-GapSeconds[i]);
            }
            // 런아웃 NG 디스크는 이미 폐기 등록됨 → 다시 스캔하면 검사 불가
            _records.Add(new NgRecord(_history[6].Serial, NgAction.Scrap, "EMP-90012", _history[6].Time.AddMinutes(2)));
        }

        private static string SerialOf(int number)
        {
            return "BD-261127-" + number.ToString("0000");
        }

        public IList<DiscMeasurement> GetTodayHistory() { return _history.ToArray(); }

        public DayCounts GetTodayCounts() { return new DayCounts(_total, _ng); }

        public NgRecord GetNgRecord(string serial)
        {
            for (int i = _records.Count - 1; i >= 0; i--) if (_records[i].Serial == serial) return _records[i];
            return null;
        }

        public DiscMeasurement Measure(string serial)
        {
            // 샘플: 시리얼마다 같은 값이 나오게(같은 디스크를 다시 재면 같은 값). 일부는 NG.
            var rnd = new Random(serial.GetHashCode());
            decimal od = 279.88m + rnd.Next(0, 25) / 100m;      // 279.88 ~ 280.12
            decimal th = 21.94m + rnd.Next(0, 13) / 100m;       // 21.94 ~ 22.06
            decimal ru = rnd.Next(5, 36) / 1000m;               // 0.005 ~ 0.035
            decimal pa = rnd.Next(1, 13) / 1000m;               // 0.001 ~ 0.012
            return new DiscMeasurement(serial, DateTime.Now, od, th, ru, pa);
        }

        public void SaveInspection(DiscMeasurement measurement)
        {
            _history.Insert(0, measurement);
            _total++;
            if (!DiscSpec.Evaluate(measurement).IsOk) _ng++;
        }

        public void SaveNgRecord(NgRecord record) { _records.Add(record); }
    }
}
