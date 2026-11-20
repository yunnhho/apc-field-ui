using System;
using System.Collections.Generic;

namespace CellLeakTester.Data
{
    /// <summary>화면 확인·render용 샘플 데이터.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        // 최근 것이 먼저. 상한 5.0과 같은 값(OK) 1건, 초과(NG) 2건 포함. 오늘 NG 9건 이내.
        private static readonly double[] Leaks = { 3.2, 2.8, 5.0, 4.1, 5.7, 1.9, 3.6, 2.4, 4.6, 6.3, 3.1, 0.8, 2.2, 4.4, 3.9, 1.3, 2.9 };
        private static readonly int[] GapSeconds = { 0, 47, 52, 44, 61, 49, 43, 55, 48, 58, 46, 51, 56, 45, 50, 42, 59 };

        private string _lot = "LOT-261120-03";
        private string _model = "CL-2170-A";
        private string _worker = "김현장";

        public string CurrentLot { get { return _lot; } }
        public string CurrentModel { get { return _model; } }
        public string CurrentWorker { get { return _worker; } }
        public IList<string> Models { get { return new[] { "CL-2170-A", "CL-2170-B", "CL-4680" }; } }

        public int TodayTotal { get { return 428; } }
        public int TodayNg { get { return 9; } }

        public IList<InspectionRow> GetHistory()
        {
            var rows = new List<InspectionRow>();
            var at = new DateTime(2026, 11, 20, 10, 15, 0);
            for (int i = 0; i < Leaks.Length; i++)
            {
                at = at.AddSeconds(-GapSeconds[i]);
                rows.Add(new InspectionRow(at, "CELL26112000" + (428 - i).ToString("000"), Leaks[i]));
            }
            return rows;
        }

        public void SaveLot(string lot, string model, string worker)
        {
            _lot = lot; _model = model; _worker = worker;
        }

        public bool VerifyAdmin(string password)
        {
            return !string.IsNullOrEmpty(password);   // 샘플: 실제 비밀번호 확인은 연결 시 구현
        }
    }
}
