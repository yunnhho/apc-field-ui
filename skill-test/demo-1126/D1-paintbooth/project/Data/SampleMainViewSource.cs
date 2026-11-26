using System;
using System.Collections.Generic;

namespace BumperPaintBoothMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 현재 14:36:05, 클리어 부스 습도가 14:31:20부터 이탈 중.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const int TrendPoints = 49;                 // 8시간 / 10분 + 1
        private const double HangerIntervalMin = 1.5;       // 컨베이어 투입 간격
        private const int LatestHanger = 2047;              // 14:36:00 투입
        private static readonly DateTime SampleNow = new DateTime(2026, 11, 26, 14, 36, 5);
        private static readonly DateTime LatestHangerAt = new DateTime(2026, 11, 26, 14, 36, 0);

        // 관리 범위: 베이스·클리어는 요구 기준. 전처리·프라이머는 기준이 없어 같은 값을 쓴다(가정). 오븐은 온도만.
        private static readonly ControlRange Temp = new ControlRange("온도", "℃", "0.0", 21.0, 25.0);
        private static readonly ControlRange Humi = new ControlRange("습도", "%RH", "0", 55, 75);
        private static readonly ControlRange Wind = new ControlRange("풍속", "m/s", "0.00", 0.30, 0.50);
        private static readonly ControlRange Pres = new ControlRange("차압", "Pa", "0", 5, 15);
        private static readonly ControlRange OvenTemp = new ControlRange("온도", "℃", "0.0", 135.0, 145.0);

        private readonly List<BoothSnapshot> _booths;
        private List<Excursion> _excursions;

        public SampleMainViewSource()
        {
            _booths = new List<BoothSnapshot>
            {
                Booth("전처리", false, 22.9, 61, 0.38, 8),
                Booth("프라이머", true, 23.1, 64, 0.12, 2),
                Booth("베이스", false, 25.0, 66, 0.42, 9),      // 온도 = 상한 → 주의
                Booth("클리어", false, 24.2, 77, 0.34, 10),     // 습도 상한 초과 → 이탈
                new BoothSnapshot("오븐", false, new[]
                {
                    new ItemReading(MonitorItem.Temperature, OvenTemp, 141.2),
                    new ItemReading(MonitorItem.Humidity, null, null),
                    new ItemReading(MonitorItem.WindSpeed, null, null),
                    new ItemReading(MonitorItem.Pressure, null, null),
                }),
            };
            _excursions = new List<Excursion>
            {
                Ex(1, "클리어", "습도", At(14, 31, 20), null, "77 %RH", "", ""),
                Ex(2, "베이스", "풍속", At(12, 48, 10), At(12, 52, 40), "0.27 m/s", "추가 검사", "김반장"),
                Ex(3, "클리어", "온도", At(11, 20, 5), At(11, 21, 35), "25.4 ℃", "영향 없음", "박품질"),
                Ex(4, "오븐", "온도", At(10, 42, 30), At(10, 49, 10), "133.4 ℃", "", ""),
                Ex(5, "프라이머", "차압", At(9, 55, 0), At(9, 56, 20), "3 Pa", "영향 없음", "박품질"),
                Ex(6, "전처리", "습도", At(9, 10, 45), At(9, 18, 5), "78 %RH", "추가 검사", "김반장"),
                Ex(7, "베이스", "온도", At(8, 31, 40), At(8, 33, 50), "25.3 ℃", "영향 없음", "최도장"),
                Ex(8, "클리어", "차압", At(7, 44, 10), At(7, 45, 0), "4 Pa", "영향 없음", "박품질"),
                Ex(9, "오븐", "온도", At(6, 58, 20), At(7, 4, 30), "146.2 ℃", "재도장", "김반장"),
                Ex(10, "프라이머", "풍속", At(6, 21, 15), At(6, 23, 30), "0.52 m/s", "영향 없음", "최도장"),
                Ex(11, "전처리", "차압", At(6, 5, 40), At(6, 7, 0), "4 Pa", "영향 없음", "최도장"),
                Ex(12, "베이스", "습도", At(5, 38, 10), At(5, 44, 30), "77 %RH", "추가 검사", "박품질"),
                Ex(13, "클리어", "풍속", At(5, 2, 25), At(5, 3, 50), "0.28 m/s", "영향 없음", "김반장"),
                Ex(14, "프라이머", "온도", At(4, 26, 0), At(4, 30, 15), "25.6 ℃", "재도장", "최도장"),
            };
        }

        public DateTime Now { get { return SampleNow; } }

        public IList<BoothSnapshot> GetBooths() { return _booths; }

        public CurrentInput GetCurrentInput() { return new CurrentInput("H-" + LatestHanger, "PL-261126-07", "NH-731P"); }

        public IList<Excursion> GetExcursions() { return _excursions.ToArray(); }

        public bool RegisterAction(int excursionId, string action, string confirmer, string memo)
        {
            for (int i = 0; i < _excursions.Count; i++)
            {
                if (_excursions[i].Id != excursionId) continue;
                if (_excursions[i].Ongoing && action == Excursion.ActionNoImpact) return false;
                _excursions[i] = _excursions[i].WithAction(action, confirmer);
                return true;
            }
            return false;
        }

        public double[] GetTrend(int boothIndex, MonitorItem item)
        {
            var reading = _booths[boothIndex].Readings[(int)item];
            if (reading.Range == null || !reading.Value.HasValue) return new double[0];
            double center = (reading.Range.Low + reading.Range.High) / 2, amp = (reading.Range.High - reading.Range.Low) * 0.12;
            var v = new double[TrendPoints];
            for (int i = 0; i < TrendPoints; i++)
                v[i] = Math.Round(center + amp * Math.Sin(i * 0.55 + boothIndex + (int)item) + amp * 0.5 * Math.Sin(i * 1.7 + boothIndex), 3);
            v[TrendPoints - 1] = reading.Value.Value;
            // 클리어 습도: 마지막 두 점에서 상한을 넘어섰다
            if (boothIndex == 3 && item == MonitorItem.Humidity) v[TrendPoints - 2] = 73;
            return v;
        }

        private static BoothSnapshot Booth(string name, bool stopped, double t, double h, double w, double p)
        {
            return new BoothSnapshot(name, stopped, new[]
            {
                new ItemReading(MonitorItem.Temperature, Temp, t), new ItemReading(MonitorItem.Humidity, Humi, h),
                new ItemReading(MonitorItem.WindSpeed, Wind, w), new ItemReading(MonitorItem.Pressure, Pres, p),
            });
        }

        private static DateTime At(int h, int m, int s) { return new DateTime(2026, 11, 26, h, m, s); }

        // 이탈 구간 중 투입된 행거 범위를 투입 간격으로 계산한다.
        private static Excursion Ex(int id, string booth, string item, DateTime start, DateTime? end, string peak, string action, string by)
        {
            int first = LatestHanger - (int)Math.Floor((LatestHangerAt - start).TotalMinutes / HangerIntervalMin);
            int last = end.HasValue ? LatestHanger - (int)Math.Ceiling((LatestHangerAt - end.Value).TotalMinutes / HangerIntervalMin) : LatestHanger;
            return new Excursion(id, booth, item, start, end, peak, first, last, action, by);
        }
    }
}
