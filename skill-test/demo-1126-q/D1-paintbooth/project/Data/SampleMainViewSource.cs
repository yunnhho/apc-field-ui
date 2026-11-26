using System;

namespace BumperPaintBoothMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 현재 시각은 고정(14:52:30).</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        public static readonly DateTime SampleNow = new DateTime(2026, 11, 26, 14, 52, 30);
        private const int TrendPoints = 97;   // 5분 간격 8시간

        private readonly Deviation[] _deviations = BuildDeviations();

        public DateTime Now { get { return SampleNow; } }
        public string CurrentUser { get { return "김도장"; } }

        public BoothInfo[] GetBooths()
        {
            return new[]
            {
                Booth("전처리", false, 22.8, 63, 0.41, 9, 0.0),
                Booth("프라이머", true, 22.0, 60, 0.12, 2, 1.1),
                Booth("베이스", false, 25.0, 66, 0.43, 11, 2.3),
                Booth("클리어", false, 23.4, 78, 0.32, 9, 3.4),
                Booth("오븐", false, 140.6, double.NaN, double.NaN, double.NaN, 4.2),
            };
        }

        public LineInput GetLineInput()
        {
            return new LineInput { Hanger = "H-" + Deviation.HangerNo(SampleNow), PaintLot = "PL-261124-07", ColorCode = "NH-731P" };
        }

        public Deviation[] GetDeviations() { return _deviations; }

        public void SaveAction(Deviation deviation, string action, string remark)
        {
            deviation.Action = action;
            deviation.Remark = remark;
            deviation.Confirmer = CurrentUser;
        }

        private static BoothInfo Booth(string name, bool stopped, double t, double h, double w, double p, double phase)
        {
            bool oven = name == "오븐";
            var b = new BoothInfo
            {
                Name = name, Stopped = stopped, Specs = BoothSpecs.For(name), Values = new[] { t, h, w, p },
                TempTrend = Trend(oven ? 140 : name == "베이스" ? 23.9 : 23, oven ? 1.5 : 0.5, phase, t),
                HumiTrend = oven ? new double[0] : Trend(65, 3, phase, h),
            };
            if (name == "클리어") b.HumiTrend[TrendPoints - 2] = 74;   // 14:47:30, 이탈 시작(14:48:10) 직전
            return b;
        }

        private static double[] Trend(double center, double amp, double phase, double current)
        {
            var v = new double[TrendPoints];
            for (int i = 0; i < TrendPoints - 1; i++)
                v[i] = Math.Round(center + amp * Math.Sin(i * 0.21 + phase) + amp * 0.4 * Math.Sin(i * 0.93 + phase * 2), 1);
            v[TrendPoints - 1] = current;
            return v;
        }

        private static Deviation[] BuildDeviations()
        {
            DateTime d = SampleNow.Date;
            Func<int, int, int, DateTime> at = (h, m, s) => d.AddHours(h).AddMinutes(m).AddSeconds(s);
            return new[]
            {
                new Deviation { Booth = "클리어", ItemIndex = BoothSpecs.Humi, Start = at(14, 48, 10), End = null, Worst = 79 },
                new Deviation { Booth = "베이스", ItemIndex = BoothSpecs.Temp, Start = at(13, 5, 40), End = at(13, 9, 55), Worst = 25.9, Action = "추가 검사", Confirmer = "박품질" },
                new Deviation { Booth = "클리어", ItemIndex = BoothSpecs.Wind, Start = at(11, 32, 5), End = at(11, 33, 20), Worst = 0.27, Action = "영향 없음", Confirmer = "박품질" },
                new Deviation { Booth = "오븐", ItemIndex = BoothSpecs.Temp, Start = at(10, 14, 30), End = at(10, 22, 10), Worst = 146.2, Action = "재도장", Confirmer = "김도장", Remark = "버너 재착화 후 복귀" },
                new Deviation { Booth = "클리어", ItemIndex = BoothSpecs.Press, Start = at(9, 41, 0), End = at(9, 42, 30), Worst = 4, Action = "영향 없음", Confirmer = "박품질" },
                new Deviation { Booth = "전처리", ItemIndex = BoothSpecs.Humi, Start = at(8, 20, 15), End = at(8, 24, 45), Worst = 76 },
                new Deviation { Booth = "프라이머", ItemIndex = BoothSpecs.Temp, Start = at(7, 55, 0), End = at(7, 57, 10), Worst = 25.8, Action = "영향 없음", Confirmer = "김도장" },
                new Deviation { Booth = "베이스", ItemIndex = BoothSpecs.Humi, Start = at(7, 12, 40), End = at(7, 13, 50), Worst = 76, Action = "추가 검사", Confirmer = "박품질" },
                new Deviation { Booth = "클리어", ItemIndex = BoothSpecs.Temp, Start = at(6, 48, 5), End = at(6, 51, 20), Worst = 25.4, Action = "영향 없음", Confirmer = "김도장" },
                new Deviation { Booth = "오븐", ItemIndex = BoothSpecs.Temp, Start = at(6, 30, 0), End = at(6, 34, 30), Worst = 133.8, Action = "재도장", Confirmer = "김도장" },
                new Deviation { Booth = "전처리", ItemIndex = BoothSpecs.Press, Start = at(6, 18, 40), End = at(6, 20, 5), Worst = 3, Action = "영향 없음", Confirmer = "김도장" },
                new Deviation { Booth = "베이스", ItemIndex = BoothSpecs.Wind, Start = at(6, 12, 15), End = at(6, 14, 50), Worst = 0.28, Action = "영향 없음", Confirmer = "김도장" },
                new Deviation { Booth = "프라이머", ItemIndex = BoothSpecs.Humi, Start = at(6, 6, 30), End = at(6, 9, 45), Worst = 77, Action = "추가 검사", Confirmer = "박품질" },
            };
        }
    }
}
