using System;
using System.Collections.Generic;
using System.Linq;

namespace WireBondEapTerminal.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 장비 WB-03은 L2611-0471 진행 중이라 다음 Lot의 Track-in은 막힌다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        // 기준값(요구서에 없어 정함): Q-time 한도 4:00 = 240분
        public const int QTimeLimitMinutes = 240;

        private sealed class Lot
        {
            public string Id, Product, Recipe, HoldReason;
            public bool Hold, Qualified, RecipeMatch, Running;
            public int Q;                                   // Q-time 남은 분
            public double Urgency, Customer, Setup;         // 0~1 정규화
            public bool SameRecipe;
        }

        private readonly List<Lot> _lots = new List<Lot>
        {
            L("L2611-0482", "BGA-256", "RCP-BGA256-v7", 95, 0.9, 1.0, true, 0.0),
            L("L2611-0485", "BGA-256", "RCP-BGA256-v7", 130, 0.8, 0.6, true, 0.0),
            L("L2611-0479", "QFN-48", "RCP-QFN48-v3", 75, 0.7, 0.8, false, 0.4),
            L("L2611-0490", "QFN-48", "RCP-QFN48-v3", 200, 0.5, 0.6, false, 0.4),
            L("L2611-0493", "LGA-64", "RCP-LGA64-v2", 215, 0.6, 0.3, false, 0.8, qualified: false),
            L("L2611-0496", "BGA-256", "RCP-BGA256-v7", 225, 0.3, 0.3, true, 0.0),
            L("L2611-0477", "BGA-256", "RCP-BGA256-v7", 150, 0.9, 1.0, true, 0.0, hold: "FDC 이상"),
            L("L2611-0474", "QFN-48", "RCP-QFN48-v3", 110, 0.6, 0.6, false, 0.4, hold: "SPC 이탈"),
            L("L2611-0468", "BGA-256", "RCP-BGA256-v7", 0, 0.8, 0.6, true, 0.0, hold: "Q-time 초과"),
            L("L2611-0463", "LGA-64", "RCP-LGA64-v2", 180, 0.4, 0.3, false, 0.8, hold: "수량 불일치"),
            L("L2611-0471", "BGA-256", "RCP-BGA256-v7", 0, 0, 0, true, 0, running: true),
        };

        private static Lot L(string id, string product, string recipe, int q, double urgency, double cust, bool same, double setup,
            string hold = null, bool qualified = true, bool running = false)
        {
            return new Lot { Id = id, Product = product, Recipe = recipe, Q = q, Urgency = urgency, Customer = cust, SameRecipe = same,
                Setup = setup, Hold = hold != null, HoldReason = hold, Qualified = qualified, RecipeMatch = true, Running = running };
        }

        public string LastScannedLot { get { return "L2611-0482"; } }

        public EquipmentInfo GetEquipment()
        {
            return new EquipmentInfo
            {
                Name = "WB-03", Hsms = HsmsState.Selected, LinktestAt = "14:04:50",
                Gem = GemControl.OnlineRemote, State = EquipState.Run, StateDetail = "L2611-0471 본딩 중",
            };
        }

        public RunningLot GetRunningLot()
        {
            return new RunningLot { LotId = "L2611-0471", Product = "BGA-256", Recipe = "RCP-BGA256-v7", Strips = 400, DoneStrips = 248, ProcessEnded = false };
        }

        // RTD 점수 = 30×납기 긴급도 + 25×Q-time 임박도 + 20×고객 우선순위 + 15×(같은 레시피 연속) − 10×셋업 시간
        private static double Score(Lot l)
        {
            double qNear = 1.0 - (double)l.Q / QTimeLimitMinutes;
            return 30 * l.Urgency + 25 * qNear + 20 * l.Customer + 15 * (l.SameRecipe ? 1 : 0) - 10 * l.Setup;
        }

        public IList<WaitingLot> GetWaitingLots()
        {
            var ranked = _lots.Where(l => !l.Running && !l.Hold).OrderByDescending(Score).ToList();
            var rows = new List<WaitingLot>();
            for (int i = 0; i < ranked.Count; i++)
                rows.Add(new WaitingLot { Rank = i + 1, Score = Score(ranked[i]), LotId = ranked[i].Id, Product = ranked[i].Product, QRemainMinutes = ranked[i].Q });
            foreach (var l in _lots.Where(x => !x.Running && x.Hold))
                rows.Add(new WaitingLot { LotId = l.Id, Product = l.Product, QRemainMinutes = l.Q, HoldReason = l.HoldReason });
            return rows;
        }

        public IList<EventRow> GetEvents()
        {
            Func<int, int, int, DateTime> t = (h, m, s) => new DateTime(2026, 11, 25, h, m, s);
            return new List<EventRow>
            {
                E(EventKind.ProcessStart, t(13, 41, 7), "S6F11", "Process Start · L2611-0471"),
                E(EventKind.RemoteCommand, t(13, 40, 52), "S2F41", "PP-SELECT · START 수락 · L2611-0471"),
                E(EventKind.ProcessEnd, t(13, 38, 25), "S6F11", "Process End · L2611-0466 · 양품 395 불량 5"),
                E(EventKind.AlarmClear, t(13, 12, 40), "S5F1", "알람 해제 · Capillary 교체 (ALID 2031)"),
                E(EventKind.AlarmSet, t(12, 56, 10), "S5F1", "알람 보고 · Capillary 수명 경고 (ALID 2031)"),
                E(EventKind.ProcessEnd, t(12, 31, 48), "S6F11", "Process End · L2611-0459 · 양품 398 불량 2"),
                E(EventKind.ProcessStart, t(11, 58, 3), "S6F11", "Process Start · L2611-0459"),
                E(EventKind.RemoteCommand, t(11, 57, 40), "S2F41", "PP-SELECT · START 수락 · L2611-0459"),
                E(EventKind.AlarmClear, t(11, 20, 15), "S5F1", "알람 해제 · 와이어 LOT 교체 (ALID 3014)"),
                E(EventKind.AlarmSet, t(11, 4, 52), "S5F1", "알람 보고 · 와이어 잔량 부족 (ALID 3014)"),
                E(EventKind.ProcessEnd, t(10, 42, 9), "S6F11", "Process End · L2611-0452 · 양품 400 불량 0"),
                E(EventKind.ProcessStart, t(10, 5, 31), "S6F11", "Process Start · L2611-0452"),
                E(EventKind.ProcessEnd, t(9, 41, 20), "S6F11", "Process End · L2611-0447 · 양품 396 불량 4"),
                E(EventKind.ProcessStart, t(9, 8, 44), "S6F11", "Process Start · L2611-0447"),
                E(EventKind.RemoteCommand, t(9, 8, 21), "S2F41", "PP-SELECT · START 수락 · L2611-0447"),
            };
        }

        private static EventRow E(EventKind k, DateTime at, string msg, string text)
        {
            return new EventRow { Kind = k, At = at, Message = msg, Text = text };
        }

        public TrackInResult Verify(string lotId)
        {
            var lot = _lots.FirstOrDefault(l => l.Id == lotId);
            if (lot == null) return null;
            var eq = GetEquipment();
            var result = new TrackInResult { LotId = lotId };

            // 검증 순서 고정: MES → EAP → RMS → 명령. 첫 NG에서 멈추고 나머지는 미검증.
            var steps = new List<Tuple<string, string, Func<string>, string>>
            {
                Step("MES", "Lot 상태 WAIT", () => lot.Running ? "이미 RUN" : lot.Hold ? "HOLD · " + lot.HoldReason : null, "WAIT"),
                Step("MES", "현재 공정 = W/B", () => null, "W/B"),
                Step("MES", "Hold 없음", () => lot.Hold ? lot.HoldReason : null, "Hold 없음"),
                Step("MES", "Q-time 이내", () => lot.Q <= 0 ? "Q-time 초과 · 자동 Hold" : null, "남은 " + Format.Hm(lot.Q) + " · 한도 " + Format.Hm(QTimeLimitMinutes)),
                Step("MES", "장비 자격 (제품·레시피)", () => lot.Qualified ? null : eq.Name + " 자격 없음 · " + lot.Product, eq.Name + " 가능"),
                Step("EAP", "Online Remote", () => eq.Gem == GemControl.OnlineRemote ? null : eq.Gem == GemControl.OnlineLocal ? "Online Local · 원격 Track-in 중단" : "Offline", "Online Remote"),
                Step("EAP", "장비 Idle", () => eq.State == EquipState.Idle ? null : eq.State == EquipState.Run ? "장비 Run 중" : "장비 알람", "Idle"),
                Step("EAP", "알람 없음", () => eq.State == EquipState.Alarm ? "알람 발생 중" : null, "알람 없음"),
                Step("RMS", "장비 레시피 = 승인 버전", () => lot.RecipeMatch ? null : "장비 레시피가 승인 버전과 다름", lot.Recipe),
                Step("명령", "PP-SELECT · START (S2F41)", () => null, "전송 대기"),
            };

            bool blocked = false;
            foreach (var s in steps)
            {
                var row = new CheckRow { Group = s.Item1, Item = s.Item2 };
                if (blocked) { row.State = VerifyState.NotChecked; row.Reason = "앞 항목 NG로 중단"; }
                else
                {
                    string ng = s.Item3();
                    if (ng == null) { row.State = VerifyState.Ok; row.Reason = s.Item4; }
                    else { row.State = VerifyState.Ng; row.Reason = ng; blocked = true; result.BlockReason = "[" + s.Item1 + "] " + ng; }
                }
                result.Checks.Add(row);
            }
            result.Passed = !blocked;
            return result;
        }

        private static Tuple<string, string, Func<string>, string> Step(string group, string item, Func<string> ngReason, string okNote)
        {
            return Tuple.Create(group, item, ngReason, okNote);
        }

        public void TrackIn(string lotId) { /* 샘플: S2F41 전송 자리 */ }
    }
}
