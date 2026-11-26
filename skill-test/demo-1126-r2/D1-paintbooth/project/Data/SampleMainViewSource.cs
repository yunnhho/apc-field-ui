using System;
using System.Collections.Generic;

namespace BumperPaintBoothMonitor.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 현재 시각 14:52:20, H-1047이 14:51:00에 투입됐다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const int TrendPoints = 97;                       // 8시간 / 5분 + 1
        private const int LastHangerNumber = 1047;
        private static readonly TimeSpan TrendStep = TimeSpan.FromMinutes(5);
        private static readonly DateTime SampleNow = new DateTime(2026, 11, 26, 14, 52, 20);
        private static readonly DateTime LastFeedAt = new DateTime(2026, 11, 26, 14, 51, 0);

        private readonly List<DeviationEvent> _events = new List<DeviationEvent>();

        public SampleMainViewSource()
        {
            DateTime d = SampleNow.Date;
            _events.Add(Ev(1, 2, MeasureItem.Humidity, d, "14:48:05", null, 76, ActionKind.None, "", ""));
            _events.Add(Ev(2, 3, MeasureItem.WindSpeed, d, "13:22:40", "13:31:10", 0.27, ActionKind.ExtraInspection, "먼지 부착 샘플 확인", "박품질"));
            _events.Add(Ev(3, 1, MeasureItem.Temperature, d, "12:05:12", "12:06:50", 25.3, ActionKind.NoImpact, "", "박품질"));
            _events.Add(Ev(4, 3, MeasureItem.Humidity, d, "10:41:00", "10:58:34", 77, ActionKind.Repaint, "백화 우려 전량 재도장", "박품질"));
            _events.Add(Ev(5, 4, MeasureItem.Temperature, d, "09:15:30", "09:19:02", 133.8, ActionKind.None, "", ""));
            _events.Add(Ev(6, 2, MeasureItem.Pressure, d, "08:20:10", "08:24:50", 3, ActionKind.ExtraInspection, "", "김반장"));
            _events.Add(Ev(7, 0, MeasureItem.Temperature, d, "07:35:00", "07:36:20", 25.2, ActionKind.NoImpact, "", "김반장"));
            _events.Add(Ev(8, 3, MeasureItem.Temperature, d, "06:41:15", "06:44:03", 25.4, ActionKind.NoImpact, "", "박품질"));
            _events.Add(Ev(9, 1, MeasureItem.Humidity, d, "06:10:40", "06:13:55", 54, ActionKind.ExtraInspection, "", "김반장"));
            _events.Add(Ev(10, 4, MeasureItem.Temperature, d, "05:32:08", "05:33:30", 145.8, ActionKind.NoImpact, "", "김반장"));
            _events.Add(Ev(11, 0, MeasureItem.Pressure, d, "05:05:00", "05:07:41", 4, ActionKind.NoImpact, "", "박품질"));
            _events.Add(Ev(12, 3, MeasureItem.WindSpeed, d, "04:20:22", "04:29:50", 0.52, ActionKind.ExtraInspection, "", "박품질"));
        }

        public DateTime Now { get { return SampleNow; } }
        public string OperatorName { get { return "김반장"; } }

        public BoothReading[] GetReadings()
        {
            return new[]
            {
                new BoothReading(0, false, new double?[] { 18.6, 58, 0.00, 0 }),
                new BoothReading(1, true, new double?[] { 25.0, 62, 0.41, 9 }),
                new BoothReading(2, true, new double?[] { 23.4, 76, 0.36, 10 }),
                new BoothReading(3, true, new double?[] { 23.1, 66, 0.38, 11 }),
                new BoothReading(4, true, new double?[] { 141.8, null, null, null }),
            };
        }

        public double[] GetTrend(int booth, MeasureItem item)
        {
            var spec = BoothSpec.Get(booth, item);
            var values = new double[spec.Managed ? TrendPoints : 0];
            if (values.Length == 0) return values;
            double amp = item == MeasureItem.Temperature ? (booth == BoothSpec.OvenIndex ? 1.5 : 0.7) : 3.0;
            double noise = amp / 3;
            for (int i = 0; i < values.Length; i++)
                values[i] = spec.Center + amp * Math.Sin(i * 0.15 + booth) + noise * Math.Sin(i * 1.7 + booth * 3);

            foreach (var e in _events)
                if (e.Booth == booth && e.Item == item) Overlay(values, e);

            values[values.Length - 1] = GetReadings()[booth].Values[(int)item].Value;
            return values;
        }

        // 이탈 구간의 점은 최대값으로, 구간 바로 앞 점은 중간값으로(급변이 아니라 오르는 모양)
        private static void Overlay(double[] values, DeviationEvent e)
        {
            DateTime first = SampleNow - TimeSpan.FromTicks(TrendStep.Ticks * (TrendPoints - 1));
            int from = (int)Math.Ceiling((e.Start - first).Ticks / (double)TrendStep.Ticks);
            int to = (int)Math.Floor(((e.End ?? SampleNow) - first).Ticks / (double)TrendStep.Ticks);
            if (to < from) { from = to = (int)Math.Round((e.Start - first).Ticks / (double)TrendStep.Ticks); }
            for (int i = from; i <= to; i++)
                if (i >= 0 && i < values.Length) values[i] = e.Peak;
            if (from - 1 >= 0 && from - 1 < values.Length) values[from - 1] = (values[from - 1] + e.Peak) / 2;
        }

        public FeedInfo GetFeed() { return new FeedInfo(HangerRange.Name(LastHangerNumber), "PL-261126-B07", "C-3412"); }

        public IList<DeviationEvent> GetDeviations() { return _events.ToArray(); }

        public HangerRange GetAffectedHangers(DateTime start, DateTime? end)
        {
            double unit = BoothSpec.FeedInterval.TotalSeconds;
            int first = LastHangerNumber + (int)Math.Ceiling((start - LastFeedAt).TotalSeconds / unit);
            int last = LastHangerNumber + (int)Math.Floor(((end ?? SampleNow) - LastFeedAt).TotalSeconds / unit);
            return new HangerRange(first, last, Math.Max(0, last - first + 1));
        }

        public bool RegisterAction(int eventId, ActionKind action, string remark)
        {
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].Id != eventId) continue;
                if (_events[i].Ongoing && action == ActionKind.NoImpact) return false;
                _events[i] = _events[i].WithAction(action, remark, OperatorName);
                return true;
            }
            return false;
        }

        private static DeviationEvent Ev(int id, int booth, MeasureItem item, DateTime day, string start, string end,
            double peak, ActionKind action, string remark, string confirmer)
        {
            DateTime? endTime = end == null ? (DateTime?)null : day + TimeSpan.Parse(end);
            return new DeviationEvent(id, booth, item, day + TimeSpan.Parse(start), endTime, peak, action, remark, confirmer);
        }
    }
}
