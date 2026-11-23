namespace CleanroomMonitor.Data
{
    /// <summary>구역별 판정 기준(설정 dialog에서 고치는 값).</summary>
    internal sealed class ZoneLimits
    {
        public ZoneLimits(string zone, double tempLow, double tempHigh, double humiLow, double humiHigh, int particleHigh)
        {
            Zone = zone;
            TempLow = tempLow;
            TempHigh = tempHigh;
            HumiLow = humiLow;
            HumiHigh = humiHigh;
            ParticleHigh = particleHigh;
        }

        public readonly string Zone;
        public readonly double TempLow;
        public readonly double TempHigh;
        public readonly double HumiLow;
        public readonly double HumiHigh;
        public readonly int ParticleHigh;
    }

    /// <summary>구역 한 곳의 현재 측정값과 최근 추이(Online=false면 통신 끊김, 값 없음).</summary>
    internal sealed class ZoneReading
    {
        public ZoneReading(string zone, bool online, double temp, double humi, int particle, double diffPa, double[] tempTrend, double[] humiTrend)
        {
            Zone = zone;
            Online = online;
            Temp = temp;
            Humi = humi;
            Particle = particle;
            DiffPa = diffPa;
            TempTrend = tempTrend;
            HumiTrend = humiTrend;
        }

        public readonly string Zone;
        public readonly bool Online;
        public readonly double Temp;
        public readonly double Humi;
        public readonly int Particle;
        public readonly double DiffPa;
        public readonly double[] TempTrend;
        public readonly double[] HumiTrend;
    }
}
