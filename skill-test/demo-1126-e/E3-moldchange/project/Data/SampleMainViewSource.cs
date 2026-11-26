using System;
using System.Collections.Generic;
using System.Linq;

namespace DieChangeManager.Data
{
    /// <summary>화면 확인·render용 샘플 데이터.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const string Store = "보관실";
        private static readonly DateTime SampleNow = new DateTime(2026, 11, 26, 15, 20, 0);

        private static readonly MoldInfo[] Molds =
        {
            new MoldInfo("DM-KB07-11", "DC-05", 110000, 110000),    // 한계 도달
            new MoldInfo("DM-HS21-03", "DC-02", 100000, 97000),     // 남은 3.0% = 교체 필요 경계
            new MoldInfo("DM-HS21-07", "DC-01", 120000, 112000),    // 6.7%
            new MoldInfo("DM-BR30-02", Store, 80000, 74400),        // 7.0%
            new MoldInfo("DM-CV12-09", Store, 85000, 78000),        // 8.2%
            new MoldInfo("DM-HS21-05", Store, 100000, 91000),       // 9.0%
            new MoldInfo("DM-CV12-05", Store, 90000, 81000),        // 10.0% = 주의 경계
            new MoldInfo("DM-HS21-09", Store, 120000, 4200),
            new MoldInfo("DM-BR30-04", "DC-04", 90000, 37000),
            new MoldInfo("DM-BR30-06", "DC-03", 95000, 0),
            new MoldInfo("DM-CV12-01", "DC-06", 100000, 42000),
            new MoldInfo("DM-KB07-14", Store, 105000, 0),
            new MoldInfo("DM-PL55-08", Store, 90000, 52000, true),  // 수리 대기
            new MoldInfo("DM-AX44-02", Store, 100000, 88000),
        };

        public DateTime Now { get { return SampleNow; } }

        public IList<MachineInfo> GetMachines()
        {
            return new[]
            {
                new MachineInfo("DC-01", "DM-HS21-07", "DM-HS21-09", "WO-2611-041", false, false),
                new MachineInfo("DC-02", "DM-HS21-03", "DM-HS21-05", "WO-2611-043", false, false),
                new MachineInfo("DC-03", "", "DM-BR30-06", "WO-2611-044", true, false),
                new MachineInfo("DC-04", "DM-BR30-04", "DM-BR30-04", "WO-2611-046", false, false),
                new MachineInfo("DC-05", "DM-KB07-11", "DM-KB07-14", "WO-2611-047", false, true),
                new MachineInfo("DC-06", "DM-CV12-01", "DM-CV12-01", "WO-2611-049", false, true),
            };
        }

        public MoldInfo FindMold(string moldId)
        {
            return Molds.FirstOrDefault(m => m.Id == moldId);
        }

        public IList<MoldInfo> GetLifeWarnings()
        {
            return Molds.Where(m => MoldRules.LifeKind(m) != APC.UI.Theming.StatusKind.Run)
                .OrderBy(m => Math.Max(0, m.Remaining) / (double)m.Limit).ToList();
        }

        public IList<PlanInfo> GetTodayPlan()
        {
            Func<int, int, DateTime> at = (h, m) => new DateTime(2026, 11, 26, h, m, 0);
            return new[]
            {
                new PlanInfo("DC-06", "DM-CV12-07", "DM-CV12-03", at(6, 0), at(6, 3), at(7, 5)),
                new PlanInfo("DC-02", "DM-HS21-01", "DM-HS21-03", at(6, 10), at(6, 12), at(6, 59)),
                new PlanInfo("DC-05", "DM-KB07-05", "DM-KB07-08", at(6, 40), at(6, 41), at(7, 28)),
                new PlanInfo("DC-04", "DM-BR30-03", "DM-BR30-01", at(6, 50), at(6, 55), at(7, 41)),
                new PlanInfo("DC-03", "DM-AX44-01", "DM-AX44-02", at(7, 20), at(7, 22), at(8, 7)),
                new PlanInfo("DC-01", "DM-HS21-06", "DM-HS21-02", at(7, 50), at(7, 52), at(8, 49)),
                new PlanInfo("DC-06", "DM-CV12-03", "DM-CV12-01", at(8, 30), at(8, 34), at(9, 19)),
                new PlanInfo("DC-04", "DM-BR30-01", "DM-BR30-04", at(10, 0), at(10, 5), at(11, 17)),
                new PlanInfo("DC-05", "DM-KB07-08", "DM-KB07-11", at(11, 50), at(11, 53), at(12, 40)),
                new PlanInfo("DC-01", "DM-HS21-02", "DM-HS21-07", at(12, 30), at(12, 31), at(13, 31)),
                new PlanInfo("DC-03", "DM-AX44-02", "DM-BR30-06", at(14, 50), at(15, 0), null),
                new PlanInfo("DC-05", "DM-KB07-11", "DM-KB07-14", at(15, 30), null, null),
                new PlanInfo("DC-02", "DM-HS21-03", "DM-HS21-05", at(15, 45), null, null),
                new PlanInfo("DC-01", "DM-HS21-07", "DM-HS21-09", at(16, 30), null, null),
            };
        }

        public bool IsKnownOperator(string badge) { return badge != null && badge.Length == 6 && badge.All(char.IsDigit); }

        // 샘플: 저장하지 않는다. 실제 구현은 DB에 교체 시작 기록을 남긴다.
        public void RegisterChange(string machine, string moldId, string badge, DateTime start) { }
    }
}
