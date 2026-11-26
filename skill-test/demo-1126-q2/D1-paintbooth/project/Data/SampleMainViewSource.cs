using System;
using System.Collections.Generic;

namespace BumperPaintBoothMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 현재 시각 14:30:00, H-0482 투입 중(1.5분 간격).</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const int CurrentHanger = 482;
        private const double HangerIntervalSeconds = 90.0;
        private const int TrendPoints = 97;   // 8시간 / 5분 + 1

        private static readonly DateTime SampleNow = new DateTime(2026, 11, 26, 14, 30, 0);
        private readonly List<Excursion> _excursions = new List<Excursion>();

        public SampleMainViewSource()
        {
            // 클리어 습도는 14:26:40부터 진행 중(3분 20초). 최대 77 %RH
            Add("클리어", "습도", "14:26:40", null, "77", "", "", "");
            Add("클리어", "온도", "11:20:05", "11:21:30", "25.4", "", "", "");
            Add("베이스", "풍속", "10:48:10", "10:52:40", "0.27", "추가 검사", "도막 두께 샘플 3대 확인", "이품질");
            Add("프라이머", "습도", "10:05:00", "10:12:20", "76", "재도장", "먼지 부착 확인, 해당 행거 전량", "김도장");
            Add("오븐", "온도", "09:12:30", "09:14:00", "145.6", "영향 없음", "도어 개방 순간 상승", "김도장");
            Add("베이스", "차압", "08:31:15", "08:37:05", "3", "추가 검사", "", "이품질");
            Add("클리어", "풍속", "07:40:00", "07:41:10", "0.28", "영향 없음", "", "이품질");
            Add("전처리", "온도", "06:55:20", "06:58:10", "25.8", "영향 없음", "", "김도장");
            Add("프라이머", "차압", "06:31:45", "06:33:05", "4", "영향 없음", "", "박반장");
            Add("베이스", "습도", "06:12:00", "06:16:30", "54", "추가 검사", "", "이품질");
            Add("클리어", "차압", "05:48:25", "05:55:10", "17", "재도장", "양압 과다로 먼지 유입 의심", "김도장");
            Add("오븐", "온도", "05:22:10", "05:23:40", "133.8", "영향 없음", "", "박반장");
            Add("베이스", "온도", "04:57:30", "05:00:05", "25.3", "영향 없음", "", "이품질");
        }

        public DateTime Now { get { return SampleNow; } }

        public IList<Booth> GetBooths()
        {
            return new List<Booth>
            {
                new Booth("전처리", true, 22.8m, 63m, 0.41m, 9m),
                new Booth("프라이머", true, 23.2m, 73m, 0.38m, 8m),     // 습도 73 = 주의 시작값
                new Booth("베이스", true, 23.4m, 66m, 0.40m, 10m),
                new Booth("클리어", true, 23.1m, 77m, 0.44m, 12m),      // 습도 77 > 상한 75 → 이탈
                new Booth("오븐", false, 98.0m, 0m, 0m, 0m),            // 정지 중에도 현재 온도는 표시
            };
        }

        public double[] GetTrend(string booth, int item)
        {
            var specs = BoothJudge.SpecsOf(booth);
            var now = FindBooth(booth);
            double target = (double)now.ValueOf(item);
            double center = (double)(specs[item].Lower + specs[item].Upper) / 2;
            double amp = (double)(specs[item].Upper - specs[item].Lower) * 0.12;
            int seed = booth.Length * 7 + item * 3;
            var v = new double[TrendPoints];
            for (int i = 0; i < TrendPoints; i++)
            {
                double wave = Math.Sin((i + seed) / 6.0) * amp + Math.Sin((i + seed) / 2.3) * amp * 0.4;
                v[i] = Math.Round(center + wave, item == 0 ? 1 : 0);
            }
            int ramp = booth == "오븐" ? 20 : 6;   // 마지막 구간이 현재값으로 이어지게
            for (int k = 0; k < ramp; k++)
            {
                int i = TrendPoints - ramp + k;
                double t = (k + 1) / (double)ramp;
                v[i] = Math.Round(v[TrendPoints - ramp - 1] + (target - v[TrendPoints - ramp - 1]) * t, item == 0 ? 1 : 0);
            }
            v[TrendPoints - 1] = target;
            return v;
        }

        public InputInfo GetCurrentInput() { return new InputInfo(CurrentHanger, "B-2611-047", "NH-731P"); }

        public IList<Excursion> GetExcursions() { return new List<Excursion>(_excursions); }

        public string[] GetConfirmers() { return new[] { "김도장", "이품질", "박반장" }; }

        public void SaveAction(int excursionId, string action, string note, string confirmer)
        {
            for (int i = 0; i < _excursions.Count; i++)
                if (_excursions[i].Id == excursionId) _excursions[i] = _excursions[i].WithAction(action, note, confirmer);
        }

        private Booth FindBooth(string name)
        {
            foreach (var b in GetBooths()) if (b.Name == name) return b;
            throw new ArgumentException(name);
        }

        private void Add(string booth, string item, string start, string end, string max, string action, string note, string who)
        {
            DateTime s = Parse(start);
            DateTime? e = end == null ? (DateTime?)null : Parse(end);
            // 투입 시각이 [시작, 종료(진행 중이면 지금)] 안인 행거
            DateTime to = e ?? SampleNow;
            int first = (int)Math.Ceiling((s - SampleNow).TotalSeconds / HangerIntervalSeconds) + CurrentHanger;
            int last = (int)Math.Floor((to - SampleNow).TotalSeconds / HangerIntervalSeconds) + CurrentHanger;
            _excursions.Add(new Excursion(_excursions.Count + 1, booth, item, s, e, max, first, last, action, note, who));
        }

        private static DateTime Parse(string hms)
        {
            return SampleNow.Date + TimeSpan.Parse(hms);
        }
    }
}
