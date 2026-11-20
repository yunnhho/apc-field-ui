using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using InjectionMonitor.Data;

namespace InjectionMonitor.Views
{
    /// <summary>
    /// 사출 성형 라인 모니터링: 설비 타일 6개(누르면 선택) → 선택 설비 현재값 카드 3개 → 금형 온도 추이 → 최근 알람 이력.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private readonly IMainViewSource _source;
        private readonly IList<Machine> _machines;
        private readonly ApcStatusTile[] _tiles;
        private readonly ApcValueCard _tempCard, _pressureCard, _cycleCard;
        private readonly ApcTrendChart _chart = new ApcTrendChart { Dock = DockStyle.Fill };
        private readonly Label _chartTitle = SectionTitle("");
        private Judge _judge;
        private int _selected;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("사출 성형 라인 모니터링")
        {
            _source = source;
            _machines = source.GetMachines();
            _judge = new Judge(source.GetRecipe());

            _tiles = new ApcStatusTile[_machines.Count];
            var tileCells = new Control[_machines.Count];
            for (int i = 0; i < _machines.Count; i++)
            {
                int index = i;
                _tiles[i] = new ApcStatusTile { Text = _machines[i].Name, Dock = DockStyle.Fill };
                _tiles[i].Click += (s, e) => Select(index);
                tileCells[i] = _tiles[i];
            }

            _tempCard = NewCard(); _pressureCard = NewCard(); _cycleCard = NewCard();

            var edit = Button("레시피 편집", ApcButtonVariant.Normal, 130);
            edit.Click += (s, e) => EditRecipe();
            var chartHead = Row(_chartTitle, edit);

            var grid = BuildAlarmGrid(source.GetAlarms());
            _chart.XLabels = source.GetTrendLabels();

            // 먼저 Add한 것이 안쪽: 이력(Fill) → 아래 줄부터 위로
            Body.Controls.Add(grid);
            Body.Controls.Add(Row(SectionTitle("최근 알람 이력")));
            Body.Controls.Add(Columns(S(220), new Control[] { _chart }));
            Body.Controls.Add(chartHead);
            Body.Controls.Add(Columns(S(130), new Control[] { _tempCard, _pressureCard, _cycleCard }));
            Body.Controls.Add(Columns(S(90), tileCells));

            foreach (var m in _machines)
            {
                var v = _judge.Machine(m);
                if (v.Status == StatusKind.Alarm) Alarm.Push(m.Name + ": " + _judge.TileText(m));
            }

            Refresh(0);
        }

        private static ApcValueCard NewCard()
        {
            return new ApcValueCard { ShowLamp = true, Dock = DockStyle.Fill };
        }

        private static ApcGrid BuildAlarmGrid(IList<AlarmRow> alarms)
        {
            var grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            grid.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            grid.Columns.Add("time", "시각");
            grid.Columns.Add("unit", "설비");
            grid.Columns.Add("text", "내용");
            grid.Columns["text"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["text"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            foreach (var a in alarms) grid.Rows.Add(a.Status, a.Time, a.Machine, a.Text);
            return grid;
        }

        private void Select(int index)
        {
            _selected = index;
            Refresh(index);
        }

        /// <summary>타일·헤더 요약·선택 설비의 카드와 그래프를 지금 데이터·레시피로 다시 채운다.</summary>
        private void Refresh(int selected)
        {
            int alarm = 0, warn = 0, stop = 0, standby = 0;
            for (int i = 0; i < _machines.Count; i++)
            {
                var m = _machines[i];
                var v = _judge.Machine(m);
                _tiles[i].Status = v.Status;
                _tiles[i].StatusText = _judge.TileText(m);
                _tiles[i].Selected = i == selected;
                if (v.Status == StatusKind.Alarm) alarm++;
                else if (v.Status == StatusKind.Warn) warn++;
                else if (v.Status == StatusKind.Stop) { if (m.ProcessText == "대기") standby++; else stop++; }
            }
            SetHeader(alarm, warn, stop, standby);
            ShowMachine(_machines[selected]);
        }

        private void SetHeader(int alarm, int warn, int stop, int standby)
        {
            var parts = new List<string> { "전체" };
            if (alarm > 0) parts.Add("알람 " + alarm);
            if (warn > 0) parts.Add("경고 " + warn);
            if (stop > 0) parts.Add("정지 " + stop);
            if (standby > 0) parts.Add("대기 " + standby);
            if (parts.Count == 1) parts.Add("정상");
            Header.StatusText = string.Join(" · ", parts.ToArray());
            Header.Status = alarm > 0 ? StatusKind.Alarm : warn > 0 ? StatusKind.Warn : stop + standby > 0 ? StatusKind.Stop : StatusKind.Run;
        }

        private void ShowMachine(Machine m)
        {
            var recipe = _source.GetRecipe();
            bool judged = m.Running && m.HasValue;

            Fill(_tempCard, "금형 온도 · " + m.Name, m.HasValue ? m.Temp.ToString("0.0") : "", "℃", judged ? _judge.Temp(m.Temp) : null,
                 "설정 " + recipe.MoldTempSet.ToString("0.0") + " · 상한 " + _judge.TempUpper.ToString("0.0"));
            Fill(_pressureCard, "사출 압력 · " + m.Name, m.HasValue ? m.Pressure.ToString("0.0") : "", "bar", judged ? _judge.Pressure(m.Pressure) : null,
                 "경고 " + _judge.PressureWarn.ToString("0.0") + " · 상한 " + recipe.PressureUpper.ToString("0.0"));
            Fill(_cycleCard, "사이클 타임 · " + m.Name, m.HasValue ? m.Cycle.ToString("0.0") : "", "s", judged ? _judge.Cycle(m.Cycle) : null,
                 "목표 " + recipe.CycleTarget + " · 상한 " + _judge.CycleUpper.ToString("0.0"));

            _chartTitle.Text = "금형 온도 추이 · " + m.Name;
            _chart.Reference = recipe.MoldTempSet;
            _chart.UpperLimit = _judge.TempUpper;
            _chart.LowerLimit = _judge.TempLower;
            _chart.SetValues(m.TempTrend);
        }

        /// <summary>verdict가 null이면 판정하지 않는다(값 없음·대기·정지): 카드 Stop.</summary>
        private static void Fill(ApcValueCard card, string title, string value, string unit, Verdict v, string footer)
        {
            card.Title = title;
            card.Value = value;
            card.Unit = unit;
            card.Status = v == null ? StatusKind.Stop : v.Status;
            card.Footer = v != null && v.Phrase != null ? v.Phrase + " · " + footer : footer;
        }

        private void EditRecipe()
        {
            using (var dlg = new RecipeDialog(_source.GetRecipe()))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.SaveRecipe(dlg.Result);
                _judge = new Judge(dlg.Result);
                Refresh(_selected);
            }
        }
    }
}
