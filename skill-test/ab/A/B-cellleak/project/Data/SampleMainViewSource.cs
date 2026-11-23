using System;
using System.Collections.Generic;

namespace CellLeakTester.Data
{
    /// <summary>화면 확인·render용 샘플 데이터.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private static readonly double[] Leaks = { 3.2, 5.0, 1.8, 6.4, 2.7, 0.9, 4.1, 7.8, 3.5, 2.2, 4.6, 1.3, 5.8, 3.0, 2.4, 4.9, 1.6 };
        private static readonly int[] GapSeconds = { 0, 44, 53, 34, 49, 43, 57, 39, 51, 42, 61, 38, 43, 47, 55, 36, 48 };

        public IList<LeakRecord> GetHistory()
        {
            var list = new List<LeakRecord>();
            var at = new DateTime(2026, 11, 20, 10, 15, 42);
            for (int i = 0; i < Leaks.Length; i++)
            {
                at = at.AddSeconds(-GapSeconds[i]);
                list.Add(new LeakRecord(at, "CELL261120-" + (412 - i).ToString("0000"), Leaks[i]));
            }
            return list;
        }

        // 금일 412개 중 NG 9개(이력 17건 안의 NG 3건 포함).
        public TodaySummary GetToday() { return new TodaySummary(412, 9); }

        public string[] GetLot() { return new[] { "LOT-261120-03", "NCM-21700", "김현장" }; }
    }
}
