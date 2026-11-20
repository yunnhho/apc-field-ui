using System.Linq;

namespace CleanroomMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 추이 마지막 점 = 현재값, 기준은 설정 dialog 기본값과 같다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        public const int TrendPoints = 13;   // 5분 간격, 10:20 ~ 11:20

        private static readonly double[] TempFlat = { -0.4, -0.3, -0.35, -0.2, -0.25, -0.1, -0.15, 0.0, -0.1, 0.1, -0.05, 0.05, 0 };
        private static readonly double[] TempRising = { -1.4, -1.3, -1.3, -1.1, -1.0, -0.9, -0.8, -0.7, -0.5, -0.4, -0.3, -0.1, 0 };
        private static readonly double[] HumiFlat = { -1.6, -1.2, -1.4, -0.8, -1.0, -0.4, -0.6, 0.0, -0.4, 0.4, -0.2, 0.2, 0 };

        private MonitorSettings _settings;

        public SampleMainViewSource()
        {
            var limits = new ZoneLimits[8];
            for (int i = 0; i < 8; i++)
                limits[i] = new ZoneLimits(20.0, 24.0, 40.0, 60.0, i < 4 ? 100 : 1000);   // A구역 100, B구역 1,000 개/ft³
            _settings = new MonitorSettings(limits, 10);
        }

        public ZoneReading[] GetZones()
        {
            return new[]
            {
                Zone("A1", 22.1, 45.2, 32, 15.0, TempFlat),
                Zone("A2", 23.6, 47.8, 54, 14.2, TempFlat),
                Zone("A3", 24.6, 51.0, 88, 13.5, TempRising),
                Zone("A4", 21.3, 60.0, 20, 12.8, TempFlat),
                Zone("B1", 22.8, 48.5, 412, 12.4, TempFlat),
                Zone("B2", 21.9, 52.4, 760, 11.6, TempFlat),
                Zone("B3", 22.0, 44.0, 640, 13.1, TempFlat),
                new ZoneReading("B4", false, null, null, null, null, new double[0], new double[0]),
            };
        }

        public MonitorSettings GetSettings() { return _settings; }

        public void SaveSettings(MonitorSettings settings) { _settings = settings; }

        private static ZoneReading Zone(string name, double temp, double humi, double particles, double pressure, double[] tempPattern)
        {
            return new ZoneReading(name, true, temp, humi, particles, pressure,
                tempPattern.Select(d => System.Math.Round(temp + d, 1)).ToArray(),
                HumiFlat.Select(d => System.Math.Round(humi + d, 1)).ToArray());
        }
    }
}
