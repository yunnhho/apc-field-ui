using System;
using System.Collections.Generic;

namespace ShaftSpcMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 25부분군(2시간 간격), 항목마다 다른 이상 규칙이 한 번씩 들어 있다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        public const int IntervalHours = 2;
        private const int SampleCount = 25;

        // 정한 기준값(요구서에 없는 것): 축 지름 B 18.000 ± 0.200 R̄ 0.100, 전장 150.000 ± 0.500 R̄ 0.300. A는 요구서 값.
        private readonly Characteristic[] _items =
        {
            new Characteristic("축 지름 A", 25.000, 0.300, 0.200),
            new Characteristic("축 지름 B", 18.000, 0.200, 0.100),
            new Characteristic("전장", 150.000, 0.500, 0.300),
        };

        // X̄가 중심선에서 떨어진 정도(σ 단위, 위 +). A는 마지막 6점 증가, B는 3σ 밖·8점 한쪽·R 초과, 전장은 2σ 밖 2점.
        private static readonly double[][] Deviation =
        {
            new[] { 0.4, -0.6, 0.9, -0.3, 0.2, -3.3, 0.6, 0.8, -0.5, 0.3, 1.1, 1.3, 1.2, 0.4, 1.6, -1.3, 0.1, 0.8, -0.3, -0.6, -0.2, 0.4, 0.9, 1.4, 2.0 },
            new[] { 0.5, -0.4, 0.8, -0.7, 0.3, -0.2, 1.0, -0.6, 3.4, -0.5, 0.2, -0.9, 0.4, -0.3, 0.6, 1.2, 0.3, 1.4, 1.1, 1.3, 0.2, 0.6, -0.1, 0.5, 0.3 },
            new[] { 0.2, -0.5, 0.6, -0.3, 0.1, -3.1, 0.5, 0.7, -0.2, 0.4, -0.6, 0.3, -0.4, 2.2, 0.3, 2.4, -0.4, -1.3, -1.2, -1.2, -1.1, -0.3, 0.6, -0.5, -0.8 },
        };

        // 범위 R이 R̄의 몇 배인지. 평균 약 1.0.
        private static readonly double[] RangeFactor =
        {
            1.0, 0.8, 1.2, 0.9, 1.1, 0.7, 1.3, 1.0, 0.85, 1.15, 0.95, 1.25, 0.75, 1.05, 0.9, 1.1, 1.4, 0.8, 1.0, 1.2, 0.9, 0.85, 1.1, 1.0, 0.95,
        };

        // 합이 0, 범위 1인 5점 배치(부분군 값이 X̄·R을 그대로 갖게).
        private static readonly double[] Pattern = { -0.50, 0.10, -0.15, 0.50, 0.05 };

        private readonly List<Subgroup>[] _groups = new List<Subgroup>[3];

        public SampleMainViewSource()
        {
            DateTime last = new DateTime(2026, 11, 25, 12, 0, 0);
            for (int k = 0; k < _items.Length; k++)
            {
                _groups[k] = new List<Subgroup>();
                var item = _items[k];
                for (int i = 0; i < SampleCount; i++)
                {
                    double xbar = item.Nominal + Deviation[k][i] * item.SigmaXbar;
                    double f = k == 1 && i == 11 ? 2.3 : RangeFactor[i];   // 축 지름 B 12번째 R이 UCL 초과
                    double r = item.RBar * f;
                    var v = new double[Characteristic.SubgroupSize];
                    for (int j = 0; j < v.Length; j++) v[j] = Math.Round(xbar + r * Pattern[(j + i) % 5], 3);
                    var t = last.AddHours(-IntervalHours * (SampleCount - 1 - i)).AddMinutes(2 + (i * 3 + k) % 9).AddSeconds((i * 17 + k * 9) % 60);
                    _groups[k].Add(new Subgroup(t, v));
                }
            }
            Now = new DateTime(2026, 11, 25, 13, 25, 0);
            NextMeasurement = last.AddHours(IntervalHours);
        }

        public IList<Characteristic> Characteristics { get { return _items; } }
        public IList<Subgroup> GetSubgroups(int item) { return _groups[item]; }
        public DateTime Now { get; private set; }
        public DateTime NextMeasurement { get; private set; }

        public void AddSubgroup(int item, double[] values)
        {
            _groups[item].Add(new Subgroup(Now, values));
            NextMeasurement = NextMeasurement.AddHours(IntervalHours);
        }
    }
}
