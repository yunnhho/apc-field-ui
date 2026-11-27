using System;
using System.Collections.Generic;

namespace LineBalanceBoard.Data
{
    /// <summary>화면 확인·render용 샘플 데이터.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private readonly List<ProcessInfo> _processes = new List<ProcessInfo>
        {
            new ProcessInfo("OP10", "자재 투입·고정", 524, "EMP-10234"),
            new ProcessInfo("OP20", "메인 하우징 조립", 570, "EMP-10241"),   // 95 % 경계 = 경고
            new ProcessInfo("OP30", "PCB 체결", 488, "EMP-10258"),
            new ProcessInfo("OP40", "커넥터 압입", 636, "EMP-10263"),        // 106 % 병목
            new ProcessInfo("OP50", "기밀 검사", 600, "EMP-10277"),          // 100 % 경계 = 경고
            new ProcessInfo("OP60", "라벨 부착", 552, "EMP-10285"),
            new ProcessInfo("OP70", "외관 검사", 415, "EMP-10292"),
            new ProcessInfo("OP80", "포장·출하 적재", 583, "EMP-10306"),
        };

        private readonly Dictionary<string, WorkerInfo> _workers = new Dictionary<string, WorkerInfo>();

        public SampleMainViewSource()
        {
            AddWorker("EMP-10234", "김민수", "OP10", "2027-06-30", "OP20", "2027-03-31");
            AddWorker("EMP-10241", "이서연", "OP20", "2027-08-31", "OP30", "2027-01-15");
            AddWorker("EMP-10258", "박준호", "OP30", "2027-02-28", "OP40", "2026-12-31");
            AddWorker("EMP-10263", "최유진", "OP40", "2027-04-30", "OP50", "2027-04-30");
            AddWorker("EMP-10277", "정하늘", "OP50", "2026-11-30", "OP60", "2027-09-30");
            AddWorker("EMP-10285", "강도윤", "OP60", "2027-05-31", "OP70", "2027-02-28");
            AddWorker("EMP-10292", "윤서아", "OP70", "2027-07-31", "OP80", "2027-03-31");
            AddWorker("EMP-10306", "한지우", "OP80", "2027-10-31", "OP10", "2027-01-31");
            // 예비 인원(미배치): 자격 만료 / 해당 공정 자격 없음 / 자격 유효
            AddWorker("EMP-10417", "이수진", "OP40", "2026-09-30", "OP20", "2027-02-28");
            AddWorker("EMP-10428", "오태식", "OP10", "2027-05-31", "OP30", "2027-06-30");
            AddWorker("EMP-10433", "장민재", "OP40", "2027-05-31", "OP60", "2027-08-31");
        }

        public int TaktTenths { get { return 600; } }

        public string Today { get { return DateTime.Today.ToString("yyyy-MM-dd"); } }

        public IList<ProcessInfo> GetProcesses() { return _processes.ToArray(); }

        public WorkerInfo FindWorker(string workerId)
        {
            WorkerInfo w;
            return _workers.TryGetValue(workerId, out w) ? w : null;
        }

        public string FindAssignment(string workerId)
        {
            foreach (var p in _processes) if (p.WorkerId == workerId) return p.Code;
            return "";
        }

        public bool Assign(string processCode, string workerId)
        {
            for (int i = 0; i < _processes.Count; i++)
            {
                var p = _processes[i];
                if (p.Code == processCode) _processes[i] = new ProcessInfo(p.Code, p.Work, p.CycleTenths, workerId);
            }
            return true;
        }

        private void AddWorker(string id, string name, string code1, string expiry1, string code2, string expiry2)
        {
            var quals = new Dictionary<string, string>();
            quals[code1] = expiry1;
            quals[code2] = expiry2;
            _workers[id] = new WorkerInfo(id, name, quals);
        }
    }
}
