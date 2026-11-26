using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace Polishing.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 값은 서로 맞는다(Lot 조회 결과 = 설비 표의 자재코드·바렐수·시간).</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const int High = 0, Low = 1;
        private const int HighRpm = 450, LowRpm = 370;   // 저속 370 고정(요구사항). 고속 SV는 요구사항에 없어 임시값

        private const string MatA = "Q3216X7R105K101NRINN02F", MatB = "Q3225X5R475K500NRINN01F", MatC = "S1005X5R104K160NRINN02F";

        private readonly List<UnitInfo> _units = new List<UnitInfo>();
        private readonly Dictionary<string, LotRecipe> _lots = new Dictionary<string, LotRecipe>();

        public SampleMainViewSource()
        {
            AddLot("B052610003412", MatA, "그린칩", 174056, 2, 60);
            AddLot("B052610003413", MatA, "그린칩", 168320, 2, 60);
            AddLot("B052610003498", MatB, "그린칩", 86240, 1, 90);
            AddLot("B052610003601", MatB, "그린칩", 86240, 1, 90);
            AddLot("S052609208731", MatC, "소성칩", 52800, 2, 120);

            var lot = new[] { "B052610003412", "B052610003413" };
            _units.Add(Unit(High, 1, UnitState.Running, lot, MatA, "그린칩", 2, 60, 24.5, 452));
            _units.Add(Unit(High, 2, UnitState.Running, new[] { "B052610003498", "B052610003511", "B052610003512", "B052610003519" }, MatB, "그린칩", 4, 90, 31.0, 460));
            _units.Add(Unit(High, 3, UnitState.Running, new[] { "B052610002871" }, MatA, "그린칩", 1, 60, 18.0, 436));
            _units.Add(Unit(High, 4, UnitState.Running, new[] { "B052610003077", "B052610003078" }, MatA, "그린칩", 2, 60, 1.5, 410));
            _units.Add(Unit(High, 5, UnitState.Sent, new[] { "B052610003601" }, MatB, "그린칩", 1, 90, null, null));
            _units.Add(Unit(High, 6, UnitState.Done, new[] { "B052610002655", "B052610002656" }, MatA, "그린칩", 2, 60, 60.4, null));
            _units.Add(Unit(High, 7, UnitState.Running, new[] { "B052610003190" }, MatB, "그린칩", 1, 90, 88.5, 471));
            _units.Add(Unit(High, 8, UnitState.Done, new[] { "B052610002490", "B052610002491", "B052610002492" }, MatA, "그린칩", 3, 60, 62.5, null));

            _units.Add(Unit(Low, 1, UnitState.Running, new[] { "S052609208731", "S052609208732" }, MatC, "소성칩", 2, 120, 47.2, 371));
            _units.Add(Unit(Low, 2, UnitState.Running, new[] { "B052610003340" }, MatB, "그린칩", 1, 60, 12.0, 380));
            _units.Add(Unit(Low, 3, UnitState.Waiting, null, "", "", 0, 0, null, null));
            _units.Add(Unit(Low, 4, UnitState.Running, new[] { "S052609208801" }, MatC, "소성칩", 1, 120, 85.3, 351));
            _units.Add(Unit(Low, 5, UnitState.Done, new[] { "B052610002210" }, MatB, "그린칩", 1, 60, 59.6, null));
            _units.Add(Unit(Low, 6, UnitState.Offline, null, "", "", 0, 0, null, null));
        }

        public IList<SendRecord> GetSendHistory()
        {
            var list = new List<SendRecord>();
            string[][] rows =
            {
                new[] { "15:31:08", "고속연마 5호기", "B052610003601", "작업 조건", "ok" },
                new[] { "15:24:52", "고속연마 6호기", "B052610002655", "완료", "ok" },
                new[] { "15:12:40", "저속연마 5호기", "B052610002210", "완료", "ok" },
                new[] { "15:03:17", "고속연마 8호기", "B052610002490", "완료", "retry" },
                new[] { "14:48:05", "저속연마 4호기", "S052609208801", "시작 5분 후", "ok" },
                new[] { "14:43:21", "저속연마 4호기", "S052609208801", "작업 조건", "ok" },
                new[] { "14:26:39", "고속연마 7호기", "B052610003190", "시작 5분 후", "ok" },
                new[] { "14:21:44", "고속연마 7호기", "B052610003190", "작업 조건", "ok" },
                new[] { "13:58:12", "고속연마 3호기", "B052610002871", "시작 5분 후", "ok" },
                new[] { "13:53:30", "고속연마 3호기", "B052610002871", "작업 조건", "ok" },
                new[] { "13:35:09", "저속연마 2호기", "B052610003340", "시작 5분 후", "ok" },
                new[] { "13:30:26", "저속연마 2호기", "B052610003340", "작업 조건", "ok" },
                new[] { "13:07:51", "고속연마 2호기", "B052610003498", "시작 5분 후", "ok" },
                new[] { "13:02:48", "고속연마 2호기", "B052610003498", "작업 조건", "ok" },
                new[] { "12:41:15", "고속연마 1호기", "B052610003412", "시작 5분 후", "ok" },
            };
            foreach (var r in rows)
                list.Add(new SendRecord(r[0], r[1], r[2], r[3], r[4] == "ok" ? StatusKind.Run : StatusKind.Warn, r[4] == "ok" ? "전송됨" : "재전송 대기"));
            return list;
        }

        public IList<string> GetGroups() { return new[] { "고속 연마", "저속 연마" }; }

        public IList<UnitInfo> GetUnits() { return _units; }

        public int GetSlotCount(UnitInfo unit) { return unit.Group == High ? 4 : 2; }

        public LotRecipe LookupLot(UnitInfo unit, string lot)
        {
            LotRecipe recipe;
            return _lots.TryGetValue(lot, out recipe) ? recipe : null;
        }

        public bool SendCondition(UnitInfo unit, IList<LotRecipe> lots, IList<string> barrelNos) { return true; }

        private void AddLot(string lot, string mat, string chip, int qty, int barrels, int timeMin)
        {
            _lots[lot] = new LotRecipe(lot, mat, chip, qty, barrels, timeMin);
        }

        private static UnitInfo Unit(int group, int no, UnitState state, string[] lots, string mat, string chip, int barrels, int svMin, double? pv, int? rpm)
        {
            string name = (group == High ? "고속연마 " : "저속연마 ") + no + "호기";
            return new UnitInfo(name, group, state, lots ?? new string[0], mat, chip, barrels, svMin, pv, group == High ? HighRpm : LowRpm, rpm);
        }
    }
}
