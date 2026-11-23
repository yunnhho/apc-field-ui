namespace CleanroomMonitor.Data
{
    /// <summary>구역 하나의 현재 측정값과 추이. 통신이 끊기면 Connected = false, 값은 null.</summary>
    internal sealed class ZoneReading
    {
        public readonly string Name;
        public readonly bool Connected;
        public readonly double? Temperature;
        public readonly double? Humidity;
        public readonly double? Particles;
        public readonly double? Pressure;
        public readonly double[] TemperatureTrend;
        public readonly double[] HumidityTrend;

        public ZoneReading(string name, bool connected, double? temperature, double? humidity, double? particles, double? pressure,
            double[] temperatureTrend, double[] humidityTrend)
        {
            Name = name; Connected = connected;
            Temperature = temperature; Humidity = humidity; Particles = particles; Pressure = pressure;
            TemperatureTrend = temperatureTrend; HumidityTrend = humidityTrend;
        }
    }

    /// <summary>구역별 기준(설정 dialog에서 바꾼다).</summary>
    internal sealed class ZoneLimits
    {
        public readonly double TempLow, TempHigh, HumiLow, HumiHigh, ParticleMax;

        public ZoneLimits(double tempLow, double tempHigh, double humiLow, double humiHigh, double particleMax)
        {
            TempLow = tempLow; TempHigh = tempHigh; HumiLow = humiLow; HumiHigh = humiHigh; ParticleMax = particleMax;
        }
    }

    /// <summary>설정 dialog 결과: 구역 순서대로의 기준 + 데이터 저장 주기(초).</summary>
    internal sealed class MonitorSettings
    {
        public readonly ZoneLimits[] Limits;
        public readonly int SaveIntervalSec;

        public MonitorSettings(ZoneLimits[] limits, int saveIntervalSec)
        {
            Limits = limits; SaveIntervalSec = saveIntervalSec;
        }
    }
}
