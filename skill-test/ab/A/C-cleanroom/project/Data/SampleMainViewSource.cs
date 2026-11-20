using System;
using System.Collections.Generic;

namespace CleanroomMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 추이 마지막 점 = 현재값.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const int TrendPoints = 13;   // 5분 간격 1시간
        private static readonly double[] Wobble = { 0.3, 0.1, 0.5, 0.2, 0.4, 0.1, 0.35, 0.15, 0.45, 0.25, 0.05, 0.3 };

        private IList<ZoneLimits> _limits = DefaultLimits();
        private int _saveIntervalSec = 10;

        public IList<ZoneReading> GetReadings()
        {
            return new List<ZoneReading>
            {
                Reading("A1", 22.1, 48.5, 1240, 15.2, 0),
                Reading("A2", 22.4, 47.2, 2310, 14.8, 3),
                Reading("A3", 23.6, 51.0, 3870, 13.5, 5),   // 온도 경고 시작(상한 24.0 - 0.5)보다 위
                Reading("A4", 22.8, 60.0, 5120, 12.6, 7),   // 습도 = 상한 → 경고(경계값)
                Reading("B1", 21.9, 44.3, 4860, 14.1, 2),
                Reading("B2", 22.6, 49.8, 14820, 13.2, 9),  // 파티클 상한 초과 → 알람
                new ZoneReading("B3", false, 0, 0, 0, 0, new double[0], new double[0]),   // 통신 끊김
                Reading("B4", 22.0, 45.1, 3300, 10.0, 4),   // 차압 = 하한 → 경고(경계값)
            };
        }

        public IList<ZoneLimits> GetLimits()
        {
            return _limits;
        }

        public int GetSaveIntervalSec()
        {
            return _saveIntervalSec;
        }

        public void SaveSettings(IList<ZoneLimits> limits, int saveIntervalSec)
        {
            _limits = limits;
            _saveIntervalSec = saveIntervalSec;
        }

        private static IList<ZoneLimits> DefaultLimits()
        {
            var list = new List<ZoneLimits>();
            foreach (string z in new[] { "A1", "A2", "A3", "A4", "B1", "B2", "B3", "B4" })
                list.Add(new ZoneLimits(z, 20.0, 24.0, 40.0, 60.0, ZoneJudge.DefaultParticleHigh));
            return list;
        }

        private static ZoneReading Reading(string zone, double temp, double humi, int particle, double diffPa, int shift)
        {
            return new ZoneReading(zone, true, temp, humi, particle, diffPa,
                Trend(temp, 1.0, shift), Trend(humi, 4.0, shift + 1));
        }

        /// <summary>현재값 이하로 흔들리는 추이(마지막 점 = 현재값).</summary>
        private static double[] Trend(double current, double amplitude, int shift)
        {
            var v = new double[TrendPoints];
            for (int i = 0; i < TrendPoints - 1; i++)
                v[i] = Math.Round(current - Wobble[(i + shift) % Wobble.Length] * amplitude, 1);
            v[TrendPoints - 1] = current;
            return v;
        }
    }
}
