using System;
using System.Collections.Generic;
using System.Text;

namespace MoldingShiftGantt.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 현재 시각 13:20 고정. 계획·실적·끼워넣기 계산이 모두 같은 규칙으로 서로 맞는다.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        // 표준·판정 기준(모두 여기 한 곳)
        public const int ChangeoverMin = 40;           // 금형 교체 표준 40분
        public const double DelayRatio = 0.10;         // 실적이 계획보다 10% 이상 적으면 지연
        private const int SampleNowMin = 320;          // 13:20
        private const int ShiftLengthMin = 720;        // 08:00~20:00
        private const int Stopped = 9999;              // 고장 종료 시각 미정

        // 품번 → 금형, 필요 톤수, 사이클타임(초), 캐비티
        private sealed class PartSpec
        {
            public string Part, Mold; public int Ton, Cavities; public double CycleSec;
        }

        private static readonly PartSpec[] Parts =
        {
            new PartSpec { Part = "PN-4410", Mold = "M-2201", Ton = 150, CycleSec = 24.0, Cavities = 4 },
            new PartSpec { Part = "PN-4425", Mold = "M-2315", Ton = 170, CycleSec = 26.5, Cavities = 4 },
            new PartSpec { Part = "PN-4460", Mold = "M-2410", Ton = 180, CycleSec = 28.0, Cavities = 2 },
            new PartSpec { Part = "PN-5120", Mold = "M-3510", Ton = 320, CycleSec = 32.0, Cavities = 2 },
            new PartSpec { Part = "PN-5133", Mold = "M-3622", Ton = 300, CycleSec = 35.5, Cavities = 2 },
            new PartSpec { Part = "PN-5180", Mold = "M-3705", Ton = 350, CycleSec = 40.0, Cavities = 1 },
            new PartSpec { Part = "PN-5290", Mold = "M-3800", Ton = 380, CycleSec = 45.0, Cavities = 1 },
        };

        private readonly Machine[] _machines;
        private readonly List<WorkOrder> _urgentOrders = new List<WorkOrder>();
        private int _seq = 100;

        public SampleMainViewSource()
        {
            _machines = new[]
            {
                Build("IM-01", 180, 1.00, 0, -1, -1, Job("PN-4410", 150), Job("PN-4425", 140), Job("PN-4410", 150), Job("PN-4460", 120, true)),
                Build("IM-02", 180, 0.98, 0, 100, 125, Job("PN-4460", 200), Job("PN-4410", 180), Job("PN-4425", 150, true)),
                Build("IM-03", 180, 0.84, 0, -1, -1, Job("PN-4425", 120), Job("PN-4460", 160), Job("PN-4410", 200, true), Job("PN-4425", 100)),
                Build("IM-04", 180, 0.98, 0, -1, -1, Job("PN-4410", 100), Job("PN-4460", 130), Job("PN-4425", 180), Job("PN-4410", 110)),
                Build("IM-05", 350, 0.93, 0, -1, -1, Job("PN-5120", 180), Job("PN-5133", 150), Job("PN-5120", 200, true)),
                Build("IM-06", 350, 1.00, 0, 270, Stopped, Job("PN-5133", 200), Job("PN-5180", 160), Job("PN-5133", 180, true)),
                Build("IM-07", 350, 0.90, 10, -1, -1, Job("PN-5180", 140), Job("PN-5120", 190), Job("PN-5133", 130)),
                Build("IM-08", 350, 0.97, 0, -1, -1, Job("PN-5120", 160), Job("PN-5180", 170), Job("PN-5120", 180)),
            };
            // 긴급 작업 지시(스캔으로 조회) — 수량 정수, 마지막은 380 t 금형(어느 성형기에도 안 맞음)
            _urgentOrders.Add(Order("WO-261126-901", "PN-4460", 400, false));
            _urgentOrders.Add(Order("WO-261126-902", "PN-5120", 400, false));
            _urgentOrders.Add(Order("WO-261126-903", "PN-5290", 300, false));
            _urgentOrders.Add(Order("WO-261126-904", "PN-4410", 800, true));
        }

        public int ShiftStartHour { get { return 8; } }
        public int ShiftMinutes { get { return ShiftLengthMin; } }
        public int NowMin { get { return SampleNowMin; } }
        public Machine[] GetMachines() { return _machines; }

        // ---- 계획·실적 만들기 ----------------------------------------------------

        private static PartSpec Spec(string part)
        {
            foreach (var p in Parts) if (p.Part == part) return p;
            throw new ArgumentException(part);
        }

        private static WorkOrder Order(string code, string part, int qty, bool dueToday)
        {
            var p = Spec(part);
            return new WorkOrder(code, part, p.Mold, p.Ton, qty, p.CycleSec, p.Cavities, dueToday);
        }

        private sealed class JobSpec { public string Part; public int DurationMin; public bool DueToday; }

        private static JobSpec Job(string part, int durationMin) { return Job(part, durationMin, false); }
        private static JobSpec Job(string part, int durationMin, bool dueToday)
        {
            return new JobSpec { Part = part, DurationMin = durationMin, DueToday = dueToday };
        }

        private Machine Build(string name, int ton, double efficiency, int lateStartMin, int downStart, int downEnd, params JobSpec[] specs)
        {
            var m = new Machine(name, ton);
            int cursor = 0;
            string last = null;
            foreach (var js in specs)
            {
                var p = Spec(js.Part);
                int qty = (int)(js.DurationMin * 60.0 / p.CycleSec) * p.Cavities;
                var wo = Order("WO-261126-" + (++_seq).ToString("000"), js.Part, qty, js.DueToday);
                if (last != null && last != wo.Part)
                {
                    m.Plan.Add(new PlanBlock(BlockKind.Changeover, wo, cursor, cursor + ChangeoverMin));
                    cursor += ChangeoverMin;
                }
                m.Plan.Add(new PlanBlock(BlockKind.Job, wo, cursor, cursor + wo.DurationMin));
                cursor += wo.DurationMin;
                last = wo.Part;
            }
            BuildActual(m, efficiency, lateStartMin, downStart, downEnd);
            return m;
        }

        /// <summary>계획 순서대로 실적을 만든다: 효율만큼 작업이 늘어지고, 고장 구간은 막대를 둘로 가른다. 현재 시각에서 자른다.</summary>
        private void BuildActual(Machine m, double eff, int lag, int downStart, int downEnd)
        {
            int cur = lag;
            bool downDone = downStart < 0;
            foreach (var b in m.Plan)
            {
                if (cur >= SampleNowMin) break;
                int s = Math.Max(cur, b.StartMin);
                int remaining = b.Kind == BlockKind.Job ? b.Wo.Qty : 0;
                double rate = b.Kind == BlockKind.Job ? b.Wo.RatePerMin * eff : 0;
                int need = b.Kind == BlockKind.Job ? (int)Math.Ceiling(remaining / rate) : b.EndMin - b.StartMin;
                if (!downDone && s <= downStart && downStart < s + need)
                {
                    int made = 0, s0 = s;
                    if (downStart > s)
                    {
                        made = b.Kind == BlockKind.Job ? (int)Math.Round(rate * (downStart - s)) : 0;
                        m.Actual.Add(new ActualBlock(b.Kind, b.Wo, s, downStart, made, false));
                    }
                    int de = Math.Min(downEnd, SampleNowMin);
                    m.Actual.Add(new ActualBlock(BlockKind.Downtime, null, downStart, de, 0, downEnd >= SampleNowMin));
                    downDone = true;
                    if (downEnd >= SampleNowMin) return;
                    s = downEnd;
                    remaining -= made;
                    need = b.Kind == BlockKind.Job ? (int)Math.Ceiling(remaining / rate) : need - (downStart - s0);
                }
                int e = s + need;
                bool clipped = e > SampleNowMin;
                if (clipped) e = SampleNowMin;
                int qty = b.Kind != BlockKind.Job ? 0 : (clipped ? (int)Math.Round(rate * (e - s)) : remaining);
                m.Actual.Add(new ActualBlock(b.Kind, b.Wo, s, e, qty, clipped));
                cur = e;
            }
        }

        // ---- 집계 ----------------------------------------------------------------

        public double PlannedQtyAtNow(Machine m)
        {
            double sum = 0;
            foreach (var b in m.Plan)
            {
                if (b.Kind != BlockKind.Job) continue;
                double f = (double)(NowMin - b.StartMin) / (b.EndMin - b.StartMin);
                sum += b.Wo.Qty * Math.Max(0, Math.Min(1, f));
            }
            return sum;
        }

        public int ActualQty(Machine m)
        {
            int sum = 0;
            foreach (var a in m.Actual) sum += a.Qty;
            return sum;
        }

        public bool IsDelayed(Machine m)
        {
            double planned = PlannedQtyAtNow(m);
            return planned > 0 && (planned - ActualQty(m)) >= planned * DelayRatio;
        }

        public bool IsDown(Machine m)
        {
            return m.Actual.Count > 0 && m.Actual[m.Actual.Count - 1].Kind == BlockKind.Downtime && m.Actual[m.Actual.Count - 1].Running;
        }

        public WorkOrder GetRunningJob(Machine m)
        {
            for (int i = m.Actual.Count - 1; i >= 0; i--)
                if (m.Actual[i].Kind == BlockKind.Job) return m.Actual[i].Wo;
            return null;
        }

        private static int QtyOf(Machine m, WorkOrder wo)
        {
            int sum = 0;
            foreach (var a in m.Actual) if (a.Wo == wo) sum += a.Qty;
            return sum;
        }

        public JobDetail GetJobDetail(Machine m, WorkOrder wo)
        {
            return new JobDetail { Wo = wo, MachineName = m.Name, ActualQty = QtyOf(m, wo), ExpectedEndMin = ExpectedEnd(m, wo) };
        }

        /// <summary>예상 종료(분). 끝난 작업 = 실제 종료, 진행 중 = 남은 수량 ÷ 현재 속도, 대기 = 계획 종료 + 현재 밀림. 고장 중이면 -1.</summary>
        private int ExpectedEnd(Machine m, WorkOrder wo)
        {
            var cur = GetRunningJob(m);
            int made = QtyOf(m, wo);
            if (made >= wo.Qty)
            {
                int end = 0;
                foreach (var a in m.Actual) if (a.Wo == wo && a.Kind == BlockKind.Job) end = Math.Max(end, a.EndMin);
                return end;
            }
            if (IsDown(m)) return -1;
            double eff = Efficiency(m, cur);
            int curEnd = cur == null ? 0 : NowMin + (int)Math.Ceiling((cur.Qty - QtyOf(m, cur)) / (cur.RatePerMin * eff));
            if (wo == cur) return curEnd;
            int lag = 0;
            foreach (var b in m.Plan) if (b.Kind == BlockKind.Job && b.Wo == cur) lag = Math.Max(0, curEnd - b.EndMin);
            foreach (var b in m.Plan) if (b.Kind == BlockKind.Job && b.Wo == wo) return b.EndMin + lag;
            return -1;
        }

        /// <summary>현재 속도 비율: 진행 중 작업의 실적 속도 ÷ 이상 속도(가장 최근 실행 구간 기준).</summary>
        private double Efficiency(Machine m, WorkOrder cur)
        {
            if (cur == null) return 1.0;
            for (int i = m.Actual.Count - 1; i >= 0; i--)
            {
                var a = m.Actual[i];
                if (a.Wo == cur && a.Kind == BlockKind.Job && a.EndMin > a.StartMin)
                {
                    double r = a.Qty / (double)(a.EndMin - a.StartMin) / cur.RatePerMin;
                    if (r > 0.3) return Math.Min(r, 1.0);
                }
            }
            return 1.0;
        }

        // ---- 긴급 작업 끼워넣기 ---------------------------------------------------

        public WorkOrder FindWorkOrder(string code)
        {
            foreach (var w in _urgentOrders) if (w.Code == code) return w;
            return null;
        }

        private static List<PlanBlock> PendingJobs(Machine m)
        {
            var list = new List<PlanBlock>();
            foreach (var b in m.Plan)
            {
                if (b.Kind != BlockKind.Job) continue;
                bool started = false;
                foreach (var a in m.Actual) if (a.Wo == b.Wo) started = true;
                if (!started) list.Add(b);
            }
            return list;
        }

        public string[] GetInsertPositions(Machine m)
        {
            var pending = PendingJobs(m);
            var names = new List<string>();
            foreach (var b in pending) names.Add(b.Wo.Code + " 앞  (" + b.Wo.Part + ")");
            names.Add("맨 뒤");
            return names.ToArray();
        }

        /// <summary>끼워 넣은 뒤의 꼬리 블록. headLen = 그대로 두는 앞쪽 블록 수.</summary>
        private List<PlanBlock> Reflow(Machine m, int position, WorkOrder urgent, out int headLen)
        {
            var pending = PendingJobs(m);
            var order = new List<WorkOrder>();
            if (urgent != null) order.Add(urgent);
            if (position < pending.Count)
            {
                int idx = m.Plan.IndexOf(pending[position]);
                headLen = (idx > 0 && m.Plan[idx - 1].Kind == BlockKind.Changeover) ? idx - 1 : idx;
                for (int i = position; i < pending.Count; i++) order.Add(pending[i].Wo);
            }
            else headLen = m.Plan.Count;

            int cursor = 0;
            string last = null;
            for (int i = headLen - 1; i >= 0; i--)
                if (m.Plan[i].Kind == BlockKind.Job) { cursor = m.Plan[i].EndMin; last = m.Plan[i].Wo.Part; break; }

            var tail = new List<PlanBlock>();
            foreach (var wo in order)
            {
                if (last != null && last != wo.Part)
                {
                    tail.Add(new PlanBlock(BlockKind.Changeover, wo, cursor, cursor + ChangeoverMin));
                    cursor += ChangeoverMin;
                }
                tail.Add(new PlanBlock(BlockKind.Job, wo, cursor, cursor + wo.DurationMin));
                cursor += wo.DurationMin;
                last = wo.Part;
            }
            return tail;
        }

        public InsertPreview PreviewInsert(Machine m, WorkOrder urgent, int position)
        {
            var pv = new InsertPreview { ToolingFits = urgent.MoldTon <= m.Ton };
            int headLen;
            var tail = Reflow(m, position, urgent, out headLen);
            var due = new StringBuilder();
            foreach (var b in tail)
            {
                var row = new PreviewRow { NewStartMin = b.StartMin, NewEndMin = b.EndMin, OldStartMin = -1 };
                if (b.Kind == BlockKind.Changeover)
                {
                    row.Label = "금형 교체"; row.Part = b.Wo.Mold + " 장착"; row.IsChangeover = true;
                }
                else
                {
                    row.Label = b.Wo.Code; row.Part = b.Wo.Part; row.DueToday = b.Wo.DueToday;
                    row.IsUrgent = b.Wo == urgent;
                    if (!row.IsUrgent)
                    {
                        foreach (var old in m.Plan) if (old.Kind == BlockKind.Job && old.Wo == b.Wo) row.OldStartMin = old.StartMin;
                        row.PushMin = b.StartMin - row.OldStartMin;
                        if (row.DueToday && row.PushMin > 0)
                        {
                            if (due.Length > 0) due.Append(", ");
                            due.Append(b.Wo.Code);
                        }
                    }
                }
                pv.Rows.Add(row);
            }
            pv.DueTodayPushed = due.ToString();
            pv.NeedsApproval = due.Length > 0;
            pv.OverflowMin = Math.Max(0, tail[tail.Count - 1].EndMin - ShiftMinutes);
            return pv;
        }

        public BadgeResult CheckApprovalBadge(string code)
        {
            if (code == "EMP-1031") return new BadgeResult { Accepted = true, Message = "생산관리 · 확인됨" };
            if (code == "EMP-1042") return new BadgeResult { Accepted = false, Message = "생산관리 사원증이 아닙니다" };
            return new BadgeResult { Accepted = false, Message = "등록되지 않은 사원증입니다" };
        }

        public void InsertUrgent(Machine m, WorkOrder urgent, int position, string approverBadge)
        {
            int headLen;
            var tail = Reflow(m, position, urgent, out headLen);
            m.Plan.RemoveRange(headLen, m.Plan.Count - headLen);
            m.Plan.AddRange(tail);
            _urgentOrders.Remove(urgent);
        }
    }
}
