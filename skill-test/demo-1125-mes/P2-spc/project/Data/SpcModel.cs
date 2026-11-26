using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ShaftSpcMonitor.Data
{
    /// <summary>측정 항목 하나의 규격과 Phase I 관리 한계(고정). 정한 기준값은 SampleMainViewSource에 모여 있다.</summary>
    internal sealed class Characteristic
    {
        public const double A2 = 0.577, D4 = 2.114, D2 = 2.326;   // n = 5 계수
        public const int SubgroupSize = 5;

        public Characteristic(string name, double nominal, double tolerance, double rBar)
        {
            Name = name; Nominal = nominal; Tolerance = tolerance; RBar = rBar;
        }

        public string Name { get; private set; }
        public double Nominal { get; private set; }
        public double Tolerance { get; private set; }
        public double RBar { get; private set; }

        public double Lsl { get { return Math.Round(Nominal - Tolerance, 3); } }
        public double Usl { get { return Math.Round(Nominal + Tolerance, 3); } }
        public double XUcl { get { return Math.Round(Nominal + A2 * RBar, 3); } }
        public double XLcl { get { return Math.Round(Nominal - A2 * RBar, 3); } }
        public double RUcl { get { return Math.Round(D4 * RBar, 3); } }
        public double SigmaWithin { get { return RBar / D2; } }
        public double SigmaXbar { get { return (XUcl - Nominal) / 3; } }
    }

    internal sealed class Subgroup
    {
        public Subgroup(DateTime time, double[] values)
        {
            Time = time;
            Values = values;
            XBar = Math.Round(values.Average(), 3);
            R = Math.Round(values.Max() - values.Min(), 3);
        }

        public DateTime Time { get; private set; }
        public double[] Values { get; private set; }
        public double XBar { get; private set; }
        public double R { get; private set; }
    }

    internal sealed class Violation
    {
        public DateTime Time { get; set; }
        public string ItemName { get; set; }
        public string Rule { get; set; }
        public string Content { get; set; }
        public string Action { get; set; }
        public bool Open { get; set; }   // 가장 최근 부분군의 위반 = 아직 조치 전
    }

    /// <summary>한 항목의 부분군 목록에서 관리도 통계·이상 규칙 1~5·이력을 계산한다. 화면 표시는 모두 여기서 나온 값만 쓴다.</summary>
    internal sealed class SpcAnalysis
    {
        public const double CpkGood = 1.33, CpkCaution = 1.00;
        private static readonly string[] RuleNames =
            { "", "1점 3σ 밖", "3점 중 2점 2σ 밖", "5점 중 4점 1σ 밖", "8점 한쪽 연속", "6점 연속 " };

        public SpcAnalysis(Characteristic item, IList<Subgroup> groups)
        {
            Item = item;
            Groups = groups;
            Violations = new List<Violation>();
            var x = groups.Select(g => g.XBar).ToArray();
            for (int i = 0; i < groups.Count; i++)
            {
                bool open = i == groups.Count - 1;
                for (int rule = 1; rule <= 5; rule++)
                {
                    string content = Check(x, i, rule);
                    // 같은 규칙이 계속 성립하는 동안은 처음 한 번만 기록(이력 표가 되풀이로 차지 않게).
                    if (content == null || (i > 0 && Check(x, i - 1, rule) != null)) { if (content != null && open) HasOpenViolation = true; continue; }
                    Add(groups[i], rule, content, open);
                    if (open) HasOpenViolation = true;
                }
                if (groups[i].R > item.RUcl)
                {
                    Violations.Add(new Violation { Time = groups[i].Time, ItemName = item.Name, Rule = "R 관리 한계 초과",
                        Content = "R " + F(groups[i].R) + " > UCL " + F(item.RUcl), Action = open ? OpenAction : "설비 점검 후 정상 판정", Open = open });
                    if (open) HasOpenViolation = true;
                }
            }
        }

        public const string OpenAction = "측정 재확인 대기";

        public Characteristic Item { get; private set; }
        public IList<Subgroup> Groups { get; private set; }
        public List<Violation> Violations { get; private set; }
        public bool HasOpenViolation { get; private set; }

        public Subgroup Last { get { return Groups[Groups.Count - 1]; } }
        public double Mean { get { return Groups.Average(g => g.XBar); } }
        public double Cpk { get { return Math.Min(Item.Usl - Mean, Mean - Item.Lsl) / (3 * Item.SigmaWithin); } }

        public double Ppk
        {
            get
            {
                var v = Groups.SelectMany(g => g.Values).ToArray();
                double mean = v.Average();
                double sd = Math.Sqrt(v.Sum(a => (a - mean) * (a - mean)) / (v.Length - 1));
                return Math.Min(Item.Usl - mean, mean - Item.Lsl) / (3 * sd);
            }
        }

        public static string F(double v) { return v.ToString("0.000", CultureInfo.InvariantCulture); }

        /// <summary>규칙 하나가 i번째 점에서 성립하면 설명, 아니면 null. 한계와 같은 값은 밖이 아니다(초과부터).</summary>
        private string Check(double[] x, int i, int rule)
        {
            double c = Item.Nominal, s = Item.SigmaXbar, v = x[i];
            string side = v > c ? "상측" : "하측";
            switch (rule)
            {
                case 1:
                    return Math.Abs(v - c) > 3 * s ? "X̄ " + F(v) + (v > c ? " > UCL " + F(Item.XUcl) : " < LCL " + F(Item.XLcl)) : null;
                case 2:
                    return OutsideCount(x, i, 3, 2 * s) >= 2 && Math.Abs(v - c) > 2 * s ? "X̄ " + F(v) + " · " + side + " 2σ 밖 2점" : null;
                case 3:
                    return OutsideCount(x, i, 5, s) >= 4 && Math.Abs(v - c) > s ? "X̄ " + F(v) + " · " + side + " 1σ 밖 4점" : null;
                case 4:
                    if (i < 7) return null;
                    bool above = x.Skip(i - 7).Take(8).All(a => a > c), below = x.Skip(i - 7).Take(8).All(a => a < c);
                    return above || below ? "X̄ " + F(v) + " · 중심선 " + (above ? "위" : "아래") + " 8점" : null;
                default:
                    if (i < 5) return null;
                    bool up = true, down = true;
                    for (int k = i - 4; k <= i; k++) { up &= x[k] > x[k - 1]; down &= x[k] < x[k - 1]; }
                    return up || down ? "X̄ " + F(v) + " · 공구 마모 의심" : null;
            }
        }

        /// <summary>점 i가 속한 최근 n점 중 점 i와 같은 쪽에서 중심선에서 limit 넘게 떨어진 점 수.</summary>
        private int OutsideCount(double[] x, int i, int n, double limit)
        {
            if (i < n - 1) return 0;
            double c = Item.Nominal;
            bool upper = x[i] > c;
            return x.Skip(i - n + 1).Take(n).Count(a => upper ? a - c > limit : c - a > limit);
        }

        private void Add(Subgroup g, int rule, string content, bool open)
        {
            string name = RuleNames[rule];
            if (rule == 5) name += g.XBar > Groups[Groups.IndexOf(g) - 1].XBar ? "증가" : "감소";
            Violations.Add(new Violation
            {
                Time = g.Time, ItemName = Item.Name, Rule = "규칙 " + rule + " · " + name, Content = content,
                Action = open ? OpenAction : rule == 5 ? "공구 교체 후 정상 판정" : rule == 1 ? "설비 점검 후 정상 판정" : rule == 4 ? "중심 조정 후 정상 판정" : "측정 재확인 후 정상 판정",
                Open = open
            });
        }
    }
}
