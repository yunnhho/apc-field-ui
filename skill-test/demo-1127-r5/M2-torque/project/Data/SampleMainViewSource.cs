using System;
using System.Collections.Generic;

namespace TorqueSequence.Data
{
    /// <summary>
    /// 화면 확인·render용 샘플 데이터. 1·6(NG 후 재체결 OK)·2(경계값 OK)·7(첫 NG, 승인 대기)까지 체결된 부품.
    /// </summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        public const string SampleSerial = "HC-261127-0042";
        private static readonly DateTime Base = new DateTime(2026, 11, 27, 14, 28, 12);

        public string CurrentSerial { get { return SampleSerial; } }
        public string CurrentOperator { get { return "EMP-10231"; } }

        public IList<TightenRecord> LoadRecords(string serial)
        {
            var list = new List<TightenRecord>();
            if (serial != SampleSerial) return list;
            list.Add(new TightenRecord(1, 10.2m, 45, Base));
            list.Add(new TightenRecord(6, 8.8m, 44, Base.AddSeconds(51)));
            list.Add(new TightenRecord(6, 11.0m, 60, Base.AddSeconds(128)));   // 상한·각도 상한 = 경계 OK
            list.Add(new TightenRecord(2, 9.0m, 30, Base.AddSeconds(159)));    // 하한·각도 하한 = 경계 OK
            list.Add(new TightenRecord(7, 8.7m, 28, Base.AddSeconds(202)));    // NG 1회
            return list;
        }

        public void SaveRecord(string serial, string operatorBadge, TightenRecord record) { }
        public void SaveApproval(string serial, int point, string supervisorBadge, string reason) { }
        public void SaveScrap(string serial, int point) { }

        public event Action<TightenRecord> ToolReceived { add { } remove { } }
    }
}
