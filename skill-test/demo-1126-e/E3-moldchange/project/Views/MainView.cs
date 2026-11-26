using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using DieChangeManager.Data;

namespace DieChangeManager.Views
{
    /// <summary>금형 교체·수명 관리 메인 화면. 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int WarnRows = 7;
        private const int SampleSelected = 1;   // 처음 선택: DC-02

        private readonly IMainViewSource _source;
        private readonly IList<MachineInfo> _machines;
        private readonly ApcStatusTile[] _tiles;
        private readonly ApcGrid _compare = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly Label _compareNote = Caption("");
        private readonly ApcGrid _plan = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly Label _registerNote = Caption("");
        private readonly ApcButton _register = Button("금형 교체 등록", ApcButtonVariant.Primary, 170);
        private MachineInfo _selected;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("금형 교체 · 수명 관리")
        {
            _source = source;
            _machines = source.GetMachines();

            _tiles = new ApcStatusTile[_machines.Count];
            for (int i = 0; i < _machines.Count; i++)
            {
                var m = _machines[i];
                var mounted = source.FindMold(m.MountedId);
                var tile = new ApcStatusTile
                {
                    Text = m.Name + "  " + MoldRules.MachineWord(m, mounted),
                    Status = MoldRules.MachineKind(m, mounted),
                    Dock = DockStyle.Fill,
                };
                var machine = m;
                tile.Click += (s, e) => Select(machine);
                _tiles[i] = tile;
            }

            BuildPlanGrid();
            var warnGrid = BuildWarnGrid();
            BuildCompareGrid();

            _register.Click += (s, e) => OpenRegister();
            Select(_machines[SampleSelected]);

            int tableHeight = TitledHeight(ApcGrid.HeightForRows(WarnRows), true);
            Body.Controls.Add(Titled("오늘 교체 계획·실적", _plan, _registerNote, _register));
            Body.Controls.Add(Columns(tableHeight, new Control[]
            {
                Titled("금형 수명 경고 (남은 비율 10% 이하)", warnGrid, Legend()),
                Titled("선택 설비 · 장착 금형과 다음 생산 지시 금형", _compare, _compareNote),
            }, new float[] { 56, 44 }));
            Body.Controls.Add(Columns(S(84), _tiles));
            Body.Controls.Add(Columns(S(120), new Control[] { BuildJudge() }));
        }

        private ApcJudgePanel BuildJudge()
        {
            var limit = _machines
                .Select(m => _source.FindMold(m.MountedId))
                .Where(m => m != null && MoldRules.LifeKind(m) == StatusKind.Alarm)
                .OrderBy(m => m.Remaining / (double)m.Limit).ToList();
            var panel = new ApcJudgePanel { Dock = DockStyle.Fill };
            if (limit.Count > 0)
            {
                panel.Status = StatusKind.Alarm;
                panel.Text = "금형 교체 필요 " + limit.Count + "건";
                panel.SubText = string.Join(" · ", limit.Take(2).Select(m => m.Location + " " + m.Id + " " + (m.Remaining <= 0 ? "한계 도달" : "남은 " + MoldRules.Ratio(m) + " %")).ToArray());
                return panel;
            }
            int warn = _machines.Count(m => { var x = _source.FindMold(m.MountedId); return x != null && MoldRules.LifeKind(x) == StatusKind.Warn; });
            panel.Status = warn > 0 ? StatusKind.Warn : StatusKind.Run;
            panel.Text = warn > 0 ? "교체 예정 " + warn + "건" : "금형 상태 정상";
            return panel;
        }

        private ApcGrid BuildWarnGrid()
        {
            var g = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            g.Columns.Add(new ApcLampColumn());
            g.Columns.Add("id", "금형 번호");
            g.Columns.Add("machine", "장착 설비");
            g.Columns.Add("count", "누적 타수");
            g.Columns.Add("limit", "한계 타수");
            g.Columns.Add("remain", "남은 타수");
            g.Columns.Add("ratio", "남은 비율(%)");
            g.Columns.Add("word", "판정");
            FillShortColumns(g);
            foreach (var m in _source.GetLifeWarnings())
            {
                var kind = MoldRules.LifeKind(m);
                int row = g.Rows.Add(kind, m.Id, m.Location, MoldRules.Count(m.Count), MoldRules.Count(m.Limit),
                    MoldRules.Count(System.Math.Max(0, m.Remaining)), MoldRules.Ratio(m), MoldRules.LifeWord(m));
                Emphasize(g.Rows[row].Cells[6], kind);
                Emphasize(g.Rows[row].Cells[7], kind);
            }
            return g;
        }

        private void BuildCompareGrid()
        {
            _compare.Columns.Add("item", "항목");
            _compare.Columns.Add("mounted", "장착 금형");
            _compare.Columns.Add("next", "다음 지시 금형");
            FillShortColumns(_compare);
        }

        private void BuildPlanGrid()
        {
            var now = _source.Now;
            _plan.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            _plan.Columns.Add("machine", "설비");
            _plan.Columns.Add("removed", "제거 금형");
            _plan.Columns.Add("mounted", "장착 금형");
            _plan.Columns.Add("planned", "계획 시각");
            _plan.Columns.Add("start", "시작");
            _plan.Columns.Add("end", "완료");
            _plan.Columns.Add("minutes", "소요(분)");
            FillShortColumns(_plan);
            foreach (var p in _source.GetTodayPlan())
            {
                var kind = MoldRules.PlanKind(p, now);
                int? min = p.Minutes(now);
                int row = _plan.Rows.Add(new ApcLampValue(kind, MoldRules.PlanWord(p, now)), p.Machine, p.RemovedId, p.MountedId,
                    p.Planned.ToString("HH:mm"), p.Start.HasValue ? p.Start.Value.ToString("HH:mm") : "",
                    p.End.HasValue ? p.End.Value.ToString("HH:mm") : "", min.HasValue ? min.Value.ToString() : "");
                if (min.HasValue && min.Value > MoldRules.DelayMinutes) Emphasize(_plan.Rows[row].Cells[7], StatusKind.Warn);
            }
        }

        private static ApcStatusLegend Legend()
        {
            var l = new ApcStatusLegend();
            l.SetItems(new ApcLampValue(StatusKind.Alarm, "교체 필요 3% 이하"), new ApcLampValue(StatusKind.Warn, "주의 10% 이하"));
            l.Width = l.PreferredWidth; l.Height = S(32);
            return l;
        }

        private static void FillShortColumns(ApcGrid g)
        {
            foreach (DataGridViewColumn c in g.Columns)
                if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private static void Emphasize(DataGridViewCell cell, StatusKind kind)
        {
            if (kind == StatusKind.Run) return;
            cell.Style.ForeColor = ApcTheme.StatusText(kind);
            cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
        }

        private void Select(MachineInfo machine)
        {
            _selected = machine;
            for (int i = 0; i < _tiles.Length; i++) _tiles[i].Selected = _machines[i] == machine;
            ShowCompare(machine);
            var reason = MoldRules.BlockReason(machine, null);
            _register.Enabled = reason == null;
            _registerNote.Text = reason ?? "";
        }

        private void ShowCompare(MachineInfo machine)
        {
            var mounted = _source.FindMold(machine.MountedId);
            var next = _source.FindMold(machine.NextMoldId);
            _compare.Rows.Clear();
            _compare.Rows.Add("금형 번호", mounted != null ? mounted.Id : "", next != null ? next.Id : "");
            _compare.Rows.Add("누적 타수", Cell(mounted, m => MoldRules.Count(m.Count)), Cell(next, m => MoldRules.Count(m.Count)));
            _compare.Rows.Add("한계 타수", Cell(mounted, m => MoldRules.Count(m.Limit)), Cell(next, m => MoldRules.Count(m.Limit)));
            _compare.Rows.Add("남은 타수", Cell(mounted, m => MoldRules.Count(System.Math.Max(0, m.Remaining))), Cell(next, m => MoldRules.Count(System.Math.Max(0, m.Remaining))));
            _compare.Rows.Add("남은 비율 (%)", Cell(mounted, MoldRules.Ratio), Cell(next, MoldRules.Ratio));
            _compare.Rows.Add("금형 상태", Cell(mounted, MoldRules.MoldState), Cell(next, MoldRules.MoldState));
            _compare.Rows.Add("생산 지시", "", machine.NextOrder);
            if (mounted != null) Emphasize(_compare.Rows[4].Cells[1], MoldRules.LifeKind(mounted));
            if (next != null) Emphasize(_compare.Rows[4].Cells[2], MoldRules.LifeKind(next));
            _compareNote.Text = CompareText(machine, mounted);
            _compareNote.ForeColor = machine.MountedId == machine.NextMoldId || machine.Changing
                ? ApcTheme.C(ApcTheme.Current.Colors.Text) : ApcTheme.StatusText(StatusKind.Alarm);
        }

        private static string Cell(MoldInfo m, System.Func<MoldInfo, string> f) { return m == null ? "" : f(m); }

        private static string CompareText(MachineInfo machine, MoldInfo mounted)
        {
            if (machine.Changing) return machine.Name + " 교체 중 · 장착 금형 없음";
            return machine.MountedId == machine.NextMoldId ? "지시 금형과 같음" : "교체 필요 · 지시 금형과 다름";
        }

        private void OpenRegister()
        {
            using (var dlg = new MoldChangeDialog(_source, _selected.Name))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.RegisterChange(dlg.Machine, dlg.MoldId, dlg.Badge, _source.Now);
                ApcDialog.Alert(FindForm(), dlg.Machine + " 교체 시작을 등록했습니다 · " + dlg.MoldId);
            }
        }
    }
}
