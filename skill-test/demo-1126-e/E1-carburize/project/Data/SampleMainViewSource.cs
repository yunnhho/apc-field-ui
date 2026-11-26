using System;
using System.Collections.Generic;

namespace CarburizeFurnaceMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 값은 서로 맞춰 둔다(현재 시각 15:15).</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const int SampleIntervalMinutes = 5;
        private const decimal RampPerSampleTemp = 90m, RampPerSampleCp = 0.12m, CpBeforeCarburize = 0.40m, ColdTemp = 25m;
        private static readonly DateTime Today = new DateTime(2026, 11, 26);

        private readonly List<StepInfo> _steps = new List<StepInfo>
        {
            new StepInfo("가열", 880m, null, 60, null, null),
            new StepInfo("침탄", 930m, 1.10m, 180, null, null),
            new StepInfo("확산", 930m, 0.80m, 60, null, null),
            new StepInfo("강온", 850m, 0.80m, 30, null, null),
            new StepInfo("유냉", null, null, 20, 60m, 80m),
        };

        private readonly List<FurnaceInfo> _furnaces;
        private readonly List<HistoryRow> _history;

        public SampleMainViewSource()
        {
            _furnaces = new List<FurnaceInfo>
            {
                new FurnaceInfo("CF-1", 2, 45, "CF1-1126-02", "GR-4521A", 480, "GR-20", At(10, 30), 931m, 0.82m, 64m, 0, 0, false, null),
                new FurnaceInfo("CF-2", -1, 0, "", "", 0, "", Today, null, null, 42m, 0, 0, false, null),
                // 로 온도 920 = 하한(930 - 10)과 같아 정상, CP 1.00은 하한(1.05) 미달 4분째 → 알람
                new FurnaceInfo("CF-3", 1, 125, "CF3-1126-03", "GR-3307C", 480, "GR-20", At(12, 10), 920m, 1.00m, 66m, 0, 4, true, null),
                new FurnaceInfo("CF-4", 5, 20, "CF4-1126-02", "GR-4521A", 480, "GR-20", At(9, 20), null, null, 66m, 0, 0, true, At(15, 10)),
            };
            _history = new List<HistoryRow>
            {
                new HistoryRow("CF-2", "CF2-1126-02", "GR-3307C", "07:35", "13:25", true, 60.4m, 0.96m),
                new HistoryRow("CF-3", "CF3-1126-02", "GR-4521A", "06:15", "12:05", true, 62.0m, 1.20m),
                new HistoryRow("CF-1", "CF1-1126-01", "GR-4521A", "03:50", "09:40", false, 57.8m, 0.91m),
                new HistoryRow("CF-4", "CF4-1126-01", "GR-3307C", "03:20", "09:10", true, 59.1m, 0.80m),
                new HistoryRow("CF-2", "CF2-1126-01", "GR-4521A", "00:40", "06:30", false, 60.7m, 0.74m),
                new HistoryRow("CF-3", "CF3-1126-01", "GR-3307C", "00:10", "06:00", true, 58.0m, 1.05m),
                // 전날(배치 번호의 날짜로 구분)
                new HistoryRow("CF-3", "CF3-1125-04", "GR-4521A", "18:05", "23:55", true, 59.6m, 1.08m),
                new HistoryRow("CF-4", "CF4-1125-04", "GR-3307C", "17:30", "23:20", true, 60.2m, 0.98m),
                new HistoryRow("CF-2", "CF2-1125-04", "GR-4521A", "17:05", "22:55", true, 61.3m, 1.12m),
                new HistoryRow("CF-1", "CF1-1125-04", "GR-3307C", "15:40", "21:30", true, 58.4m, 0.86m),
                new HistoryRow("CF-3", "CF3-1125-03", "GR-4521A", "12:00", "17:50", true, 60.9m, 1.15m),
                new HistoryRow("CF-4", "CF4-1125-03", "GR-4521A", "11:30", "17:20", true, 59.8m, 0.94m),
                new HistoryRow("CF-2", "CF2-1125-03", "GR-3307C", "10:40", "16:30", false, 62.4m, 1.02m),
                new HistoryRow("CF-1", "CF1-1125-03", "GR-3307C", "09:45", "15:35", true, 60.0m, 0.91m),
                new HistoryRow("CF-4", "CF4-1125-02", "GR-4521A", "05:30", "11:20", true, 61.7m, 1.19m),
            };
        }

        private static DateTime At(int hour, int minute) { return Today.AddHours(hour).AddMinutes(minute); }

        public IList<FurnaceInfo> GetFurnaces() { return _furnaces.ToArray(); }

        public IList<StepInfo> GetSteps(string recipe) { return _steps; }

        public IList<HistoryRow> GetHistory() { return _history.ToArray(); }

        public bool VerifyTechnicianBadge(string code)
        {
            return code != null && code.StartsWith("TECH-", StringComparison.Ordinal) && code.Length > 5;
        }

        public IList<AlarmRecord> GetAlarms(string batchNo)
        {
            if (batchNo == "CF4-1126-02")
                return new[] { new AlarmRecord("11:42", "침탄", "CP 하한 미달 1.00 % (하한 1.05) · 3분째"), new AlarmRecord("14:41", "강온", "온도 상한 초과 862 ℃ (상한 860) · 2분째") };
            if (batchNo == "CF3-1126-03")
                return new[] { new AlarmRecord("15:11", "침탄", "CP 하한 미달 1.00 % (하한 1.05) · 4분째") };
            return new AlarmRecord[0];
        }

        public void CompleteBatch(BatchJudgement j)
        {
            int i = _furnaces.FindIndex(f => f.Id == j.FurnaceId);
            if (i < 0) return;
            FurnaceInfo f0 = _furnaces[i];
            _history.Insert(0, new HistoryRow(f0.Id, f0.BatchNo, f0.PartNo, f0.StartTime.ToString("HH:mm"), f0.EndTime.HasValue ? f0.EndTime.Value.ToString("HH:mm") : "", j.Passed, j.Hardness, j.Depth));
            _furnaces[i] = new FurnaceInfo(f0.Id, -1, 0, "", "", 0, "", Today, null, null, f0.OilTemp, 0, 0, false, null);
        }

        /// <summary>5분 간격 실측 샘플: 온도·CP는 단계 목표로 일정 속도로 오르내리고, 마지막 점은 현재값과 같다.</summary>
        public ProfileData GetProfile(string furnaceId)
        {
            FurnaceInfo f = _furnaces.Find(x => x.Id == furnaceId);
            if (f == null || !f.HasBatch) return new ProfileData(new double[0], new double[0]);

            var temp = new List<double>();
            var cp = new List<double>();
            decimal t = ColdTemp, c = CpBeforeCarburize;
            int lastStep = Math.Min(f.StepIndex, 3);   // 유냉은 로 밖이라 그리지 않는다
            for (int s = 0; s <= lastStep; s++)
            {
                int minutes = s == f.StepIndex ? f.StepElapsedMinutes : _steps[s].SetMinutes;
                int samples = minutes / SampleIntervalMinutes;
                for (int k = 0; k < samples; k++)
                {
                    t = Move(t, _steps[s].SetTemp.Value, RampPerSampleTemp);
                    temp.Add((double)t + Wobble(temp.Count, 3.0));
                    if (_steps[s].SetCp.HasValue)
                    {
                        c = Move(c, _steps[s].SetCp.Value, RampPerSampleCp);
                        cp.Add((double)c + Wobble(cp.Count, 0.02));
                    }
                }
            }
            if (f.Temp.HasValue && temp.Count > 0) temp[temp.Count - 1] = (double)f.Temp.Value;
            if (f.Cp.HasValue && cp.Count > 0) cp[cp.Count - 1] = (double)f.Cp.Value;
            return new ProfileData(temp.ToArray(), cp.ToArray());
        }

        private static decimal Move(decimal from, decimal to, decimal maxStep)
        {
            decimal d = to - from;
            return from + Math.Max(-maxStep, Math.Min(maxStep, d));
        }

        private static double Wobble(int i, double amplitude) { return Math.Sin(i * 1.7) * amplitude; }
    }
}
