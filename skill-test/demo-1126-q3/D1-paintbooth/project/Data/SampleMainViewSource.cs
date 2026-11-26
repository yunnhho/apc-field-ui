using System;
using System.Collections.Generic;

namespace BumperPaintBoothMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const int TrendPoints = 17;   // 8시간, 30분 간격
        private static readonly DateTime SampleNow = new DateTime(2026, 11, 26, 14, 52, 30);

        private readonly Booth[] _booths;
        private readonly List<Deviation> _deviations;

        public SampleMainViewSource()
        {
            DateTime now = SampleNow;
            Booth pre = new Booth("전처리", false, false, V(22.6m, 63m, 0.41m, 9m), "H-0655", "", "", 655, now,
                Gen(23m, 0.6m, 0.2, 1, 22.6m), Gen(65m, 3m, 1.0, 0, 63m));
            Booth primer = new Booth("프라이머", false, false, V(23.2m, 66m, 0.38m, 10m), "H-0648", "PR-1126-03", "PR-GRY", 648, now,
                Gen(23m, 0.7m, 0.9, 1, 23.2m), Gen(65m, 3m, 2.1, 0, 66m));
            Booth bas = new Booth("베이스", false, false, V(23.8m, 75m, 0.40m, 11m), "H-0644", "BS-1126-05", "NH-883P", 644, now,
                Gen(23m, 0.8m, 1.7, 1, 23.8m), Gen(65m, 4m, 0.4, 0, 75m, 71m, 73m));
            Booth clear = new Booth("클리어", false, false, V(23.4m, 77m, 0.32m, 9m), "H-0640", "CL-1126-02", "NH-883P", 640, now,
                Gen(23m, 0.7m, 2.6, 1, 23.4m), Gen(65m, 4m, 1.3, 0, 77m, 70m, 73m));
            DateTime ovenStop = new DateTime(2026, 11, 26, 14, 31, 0);
            Booth oven = new Booth("오븐", true, true, V(96.0m, null, null, null), "", "", "", 612, ovenStop,
                Gen(140m, 1.5m, 0.5, 1, 96.0m, 128m, 112m), new decimal[0]);
            _booths = new[] { pre, primer, bas, clear, oven };

            _deviations = new List<Deviation>
            {
                Dev(clear, BoothItem.Humi, 14, 46, 10, null, 77m, null, "", ""),
                Dev(bas, BoothItem.Wind, 13, 31, 20, new DateTime(2026, 11, 26, 13, 36, 5), 0.27m, null, "", ""),
                Dev(oven, BoothItem.Temp, 12, 48, 0, new DateTime(2026, 11, 26, 12, 49, 40), 146.1m, "영향 없음", "승온 직후 일시 초과", "정품질"),
                Dev(clear, BoothItem.Dp, 11, 20, 35, new DateTime(2026, 11, 26, 11, 27, 50), 3m, "재도장", "문 개방 중 차압 저하", "이도장"),
                Dev(primer, BoothItem.Temp, 10, 12, 5, new DateTime(2026, 11, 26, 10, 13, 15), 25.4m, "영향 없음", "", "이도장"),
                Dev(clear, BoothItem.Humi, 9, 5, 40, new DateTime(2026, 11, 26, 9, 14, 10), 76m, "재도장", "백화 확인", "정품질"),
                Dev(pre, BoothItem.Temp, 8, 42, 0, new DateTime(2026, 11, 26, 8, 44, 30), 20.7m, "추가 검사", "", "박반장"),
                Dev(bas, BoothItem.Humi, 7, 35, 15, new DateTime(2026, 11, 26, 7, 36, 45), 76m, "영향 없음", "", "박반장"),
                Dev(oven, BoothItem.Temp, 7, 10, 0, new DateTime(2026, 11, 26, 7, 12, 20), 133.8m, "추가 검사", "경화 상태 확인", "정품질"),
                Dev(clear, BoothItem.Wind, 6, 25, 10, new DateTime(2026, 11, 26, 6, 30, 0), 0.27m, "재도장", "먼지 부착 확인", "이도장"),
                Dev(pre, BoothItem.Dp, 5, 52, 45, new DateTime(2026, 11, 26, 5, 54, 5), 4m, "영향 없음", "라인 기동 직후", "박반장"),
            };
        }

        public DateTime Now { get { return SampleNow; } }
        public Booth[] GetBooths() { return _booths; }
        public List<Deviation> GetDeviations() { return _deviations; }

        public void RegisterAction(Deviation deviation, string action, string remark, string confirmer)
        {
            int i = _deviations.IndexOf(deviation);
            if (i >= 0) _deviations[i] = deviation.WithAction(action, remark, confirmer);
        }

        private static decimal?[] V(decimal? temp, decimal? humi, decimal? wind, decimal? dp)
        {
            return new[] { temp, humi, wind, dp };
        }

        private static Deviation Dev(Booth b, BoothItem item, int h, int m, int s, DateTime? end, decimal peak,
            string action, string remark, string confirmer)
        {
            return new Deviation(b, item, new DateTime(2026, 11, 26, h, m, s), end, peak, action, remark, confirmer);
        }

        /// <summary>샘플 추이: 기준값 둘레의 완만한 흔들림 + 끝의 tail(마지막 직전 점들) + 마지막 = 현재값.</summary>
        private static decimal[] Gen(decimal nominal, decimal amp, double phase, int decimals, decimal current, params decimal[] tail)
        {
            var v = new decimal[TrendPoints];
            for (int i = 0; i < TrendPoints; i++)
                v[i] = Math.Round(nominal + amp * (decimal)Math.Sin(i * 0.8 + phase), decimals);
            for (int j = 0; j < tail.Length; j++) v[TrendPoints - 1 - tail.Length + j] = tail[j];
            v[TrendPoints - 1] = current;
            return v;
        }
    }
}
