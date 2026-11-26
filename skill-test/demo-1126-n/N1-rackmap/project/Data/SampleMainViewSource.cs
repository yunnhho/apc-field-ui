using System;
using System.Collections.Generic;
using System.Linq;

namespace FgRackLocator.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 랙 240칸 중 일부를 채우고, 시연용 팔레트 몇 개를 고정해 둔다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        // 시연용 고정 팔레트 (render·점검에서 쓴다)
        public const string DemoInboundHeavy = "PL-261126-0107";   // PM-450 720 kg, 입고 전
        public const string DemoOutboundFifo = "PL-261105-0044";   // HX-320, 더 오래된 팔레트가 있음
        public const string DemoOutboundHold = "PL-261008-0031";   // 품질 보류
        public const string DemoBadge = "EMP-0007";                // 담당 사원증

        private static readonly string[][] Parts =
        {
            // 품번, 약어, 기본 중량(kg)
            new[] { "HX-320", "HX3", "540" },
            new[] { "BR-210", "BR2", "380" },
            new[] { "PM-450", "PM4", "720" },
            new[] { "CV-118", "CV1", "260" },
            new[] { "GD-660", "GD6", "650" },
            new[] { "SL-905", "SL9", "430" },
            new[] { "RT-742", "RT7", "310" },
            new[] { "FN-081", "FN0", "190" },
        };
        private static readonly string[] Workers = { "김태수", "박민호", "이정훈" };
        private static readonly string[] AuthorizedBadges = { DemoBadge, "EMP-0012" };

        private readonly Dictionary<string, Pallet> _byLocation = new Dictionary<string, Pallet>();
        private readonly Dictionary<string, Pallet> _byCode = new Dictionary<string, Pallet>();
        private readonly List<HistoryEntry> _history = new List<HistoryEntry>();
        private readonly DateTime _today = new DateTime(2026, 11, 26);

        public SampleMainViewSource()
        {
            Build();
        }

        public DateTime Today { get { return _today; } }
        public string CurrentWorker { get { return Workers[0]; } }

        public Pallet FindPallet(string palletCode)
        {
            Pallet p;
            return _byCode.TryGetValue(palletCode, out p) ? p : null;
        }

        public Pallet GetPalletAt(string location)
        {
            Pallet p;
            return _byLocation.TryGetValue(location, out p) ? p : null;
        }

        public bool IsAuthorizedBadge(string badge) { return AuthorizedBadges.Contains(badge); }

        public IList<HistoryEntry> GetTodayHistory() { return _history.OrderByDescending(h => h.Time).ToList(); }

        public ScanResult Evaluate(string palletCode)
        {
            Pallet p = FindPallet(palletCode);
            if (p == null) return null;
            if (p.Location == null)
            {
                string loc = RecommendLocation(p);
                return new ScanResult(loc == null ? ScanKind.InboundNoSpace : ScanKind.Inbound, p, loc, null);
            }
            if (p.IsHold) return new ScanResult(ScanKind.OutboundHold, p, p.Location, null);
            return new ScanResult(ScanKind.Outbound, p, p.Location, FindOlder(p));
        }

        // 같은 품번에서 더 오래된(입고일이 빠른) 출고 가능 팔레트. 보류 팔레트는 나갈 수 없으니 건너뛴다.
        private Pallet FindOlder(Pallet p)
        {
            return _byLocation.Values
                .Where(o => o.PartNo == p.PartNo && !o.IsHold && o.Code != p.Code && o.InboundDate < p.InboundDate)
                .OrderBy(o => o.InboundDate).ThenBy(o => o.Code).FirstOrDefault();
        }

        // 추천: 무거운 품번은 1~2단, 가벼운 품번은 3~5단을 먼저(1~2단은 무거운 품번 몫으로 남김).
        // 같은 단이면 랙 A→D, 열 1→12 순서의 첫 빈칸.
        private string RecommendLocation(Pallet p)
        {
            int[] tiers = p.IsHeavy ? new[] { 1, 2 } : new[] { 3, 4, 5, 1, 2 };
            foreach (int tier in tiers)
                foreach (char rack in RackLayout.RackNames)
                    for (int col = 1; col <= RackLayout.Columns; col++)
                    {
                        string loc = RackLayout.Code(rack, tier, col);
                        if (!_byLocation.ContainsKey(loc)) return loc;
                    }
            return null;
        }

        public void ConfirmInbound(Pallet pallet, string location)
        {
            var moved = new Pallet(pallet.Code, pallet.PartNo, pallet.PartAbbr, pallet.WeightKg, pallet.InboundDate, location, pallet.IsHold);
            _byCode[moved.Code] = moved;
            _byLocation[location] = moved;
            _history.Add(new HistoryEntry(DateTime.Now, "입고", pallet.Code, pallet.PartNo, location, CurrentWorker));
        }

        public void ConfirmOutbound(Pallet pallet, string approverBadge)
        {
            _byLocation.Remove(pallet.Location);
            _byCode.Remove(pallet.Code);
            _history.Add(new HistoryEntry(DateTime.Now, "출고", pallet.Code, pallet.PartNo, pallet.Location, CurrentWorker));
        }

        // ---- 샘플 생성 ----

        private void Build()
        {
            var rnd = new Random(26);
            double[] fill = { 0.88, 0.70, 0.52, 0.36 };   // 랙별 적재 수준을 달리
            var seq = new Dictionary<DateTime, int>();
            for (int r = 0; r < RackLayout.RackNames.Length; r++)
                for (int tier = 1; tier <= RackLayout.Tiers; tier++)
                    for (int col = 1; col <= RackLayout.Columns; col++)
                    {
                        if (rnd.NextDouble() > fill[r]) continue;
                        string[] part = PickPart(rnd, tier);
                        int days = rnd.Next(100) < 20 ? 31 + rnd.Next(25) : (rnd.Next(100) < 12 ? 0 : 1 + rnd.Next(29));
                        bool hold = rnd.Next(100) < 5;
                        Place(NewPallet(seq, part, rnd, _today.AddDays(-days), RackLayout.Code(RackLayout.RackNames[r], tier, col), hold));
                    }
            PlaceDemoPallets(seq, rnd);
            BuildHistory(seq, rnd);
        }

        private static string[] PickPart(Random rnd, int tier)
        {
            string[][] pool = tier <= RackLayout.HeavyMaxTier ? Parts : Parts.Where(p => int.Parse(p[2]) <= RackLayout.HeavyKg).ToArray();
            return pool[rnd.Next(pool.Length)];
        }

        private static Pallet NewPallet(Dictionary<DateTime, int> seq, string[] part, Random rnd, DateTime date, string loc, bool hold)
        {
            int n;
            seq.TryGetValue(date, out n);
            seq[date] = ++n;
            int kg = int.Parse(part[2]) + rnd.Next(-4, 5) * 5;
            return new Pallet("PL-" + date.ToString("yyMMdd") + "-" + (n * 3 + 10).ToString("0000"), part[0], part[1], kg, date, loc, hold);
        }

        private void Place(Pallet p)
        {
            _byLocation[p.Location] = p;
            _byCode[p.Code] = p;
        }

        private void PlaceDemoPallets(Dictionary<DateTime, int> seq, Random rnd)
        {
            // FIFO 시연: HX-320 가장 오래된 팔레트(B-1-04, 10-02)와 이를 앞지르는 C-3-07(11-05)
            Replace("B-1-04", new Pallet("PL-261002-0012", "HX-320", "HX3", 545, new DateTime(2026, 10, 2), "B-1-04", false));
            Replace("C-3-07", new Pallet(DemoOutboundFifo, "HX-320", "HX3", 535, new DateTime(2026, 11, 5), "C-3-07", false));
            // 품질 보류 시연
            Replace("B-2-09", new Pallet(DemoOutboundHold, "BR-210", "BR2", 385, new DateTime(2026, 10, 8), "B-2-09", true));
            // 오래된 HX-320 보다 먼저 들어온 같은 품번이 없도록, 다른 HX-320 중 10-02 이전은 정리
            foreach (Pallet o in _byLocation.Values.Where(x => x.PartNo == "HX-320" && x.InboundDate < new DateTime(2026, 10, 2)).ToList())
            {
                _byLocation.Remove(o.Location); _byCode.Remove(o.Code);
            }
            // 입고 전(바닥 대기) 팔레트
            AddWaiting(new Pallet(DemoInboundHeavy, "PM-450", "PM4", 720, _today, null, false));
            AddWaiting(new Pallet("PL-261126-0108", "CV-118", "CV1", 265, _today, null, false));
            AddWaiting(new Pallet("PL-261126-0109", "FN-081", "FN0", 195, _today, null, false));
        }

        private void Replace(string loc, Pallet p)
        {
            Pallet old;
            if (_byLocation.TryGetValue(loc, out old)) _byCode.Remove(old.Code);
            Place(p);
        }

        private void AddWaiting(Pallet p) { _byCode[p.Code] = p; }

        // 오늘 이력: 오늘 입고분(랙에 실제로 있는 팔레트) + 오늘 출고분(지금은 빈칸이거나 다른 팔레트가 있는 위치)
        private void BuildHistory(Dictionary<DateTime, int> seq, Random rnd)
        {
            var todayIn = _byLocation.Values.Where(p => p.InboundDate == _today).OrderBy(p => p.Code).Take(15).ToList();
            int minute = 8 * 60 + 3;
            foreach (Pallet p in todayIn)
            {
                minute += 7 + rnd.Next(0, 19);
                _history.Add(new HistoryEntry(_today.AddMinutes(minute), "입고", p.Code, p.PartNo, p.Location, Workers[rnd.Next(Workers.Length)]));
            }
            string[] outs = { "A-4-02", "B-3-11", "A-2-05", "C-1-09", "D-5-03", "B-5-12", "C-2-02", "A-5-08", "D-1-10", "C-4-06", "A-3-09", "D-4-11", "B-1-02", "C-5-07", "A-1-04", "D-2-06", "B-4-10" };
            minute = 8 * 60 + 15;
            foreach (string loc in outs)
            {
                minute += 9 + rnd.Next(0, 22);
                string[] part = Parts[rnd.Next(Parts.Length)];
                int d = 1 + rnd.Next(40);
                _history.Add(new HistoryEntry(_today.AddMinutes(minute), "출고",
                    "PL-" + _today.AddDays(-d).ToString("yyMMdd") + "-" + (rnd.Next(1, 90) * 3 + 11).ToString("0000"), part[0], loc, Workers[rnd.Next(Workers.Length)]));
            }
        }
    }
}
