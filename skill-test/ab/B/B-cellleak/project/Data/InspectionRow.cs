using System;

namespace CellLeakTester.Data
{
    /// <summary>검사 이력 한 건(값 객체).</summary>
    internal sealed class InspectionRow
    {
        public InspectionRow(DateTime time, string barcode, double leak)
        {
            Time = time;
            Barcode = barcode;
            Leak = leak;
        }

        public DateTime Time { get; private set; }
        public string Barcode { get; private set; }
        public double Leak { get; private set; }
    }
}
