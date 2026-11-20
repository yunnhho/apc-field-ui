using System;
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
        private readonly IList<MachineInfo> _machines;
        private readonly string[] _trendLabels;
        private readonly ApcStatusTile[] _tiles;
        private readonly ApcValueCard _tempCard = Card(), _pressureCard = Card(), _cycleCard = Card();
        private readonly ApcTrendChart _chart = new ApcTrendChart { Dock = DockStyle.Top, Height = S(200) };
        private readonly Label _cardsTitle = SectionTitle("");
        private readonly Label _chartTitle = SectionTitle("");
        private RecipeInfo _recipe;
        private int _selected;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("사출 성형 라인 모니터링")
        {
            _source = source;
            _machines = source.GetMachines();
            _recipe = source.GetRecipe();
            _trendLabels = source.GetTrendLabels();
            _chart.XLabels = _trendLabels;

            _tiles = new ApcStatusTile[_machines.Count];
            var tileCells = new Control[_machines.Count];
            for (int i = 0; i < _machines.Count; i++)
            {
                int index = i;
                var tile = new ApcStatusTile { Dock = DockStyle.Fill };
                tile.Click += (s, e) => { _selected = index; Refresh_(); };
                _tiles[i] = tile;
                tileCells[i] = tile;
                var v = Judge.Machine(_machines[i], _recipe);
                if (v.Status == StatusKind.Alarm && _machines[i].IsRunning) Alarm.Push(_machines[i].Name + ": " + v.Detail);
            }
            // 처음 선택: 알람 설비가 있으면 그 중 첫 설비
            for (int i = _machines.Count - 1; i >= 0; i--)
                if (Judge.Machine(_machines[i], _recipe).Status == StatusKind.Alarm) _selected = i;

            var recipeButton = Button("레시피 편집", ApcButtonVariant.Normal, 130);
            recipeButton.Click += (s, e) => EditRecipe();

            var cardCells = new Control[] { _tempCard, _pressureCard, _cycleCard };

            Body.Controls.Add(BuildHistoryGrid());
            Body.Controls.Add(Row(SectionTitle("최근 알람 이력")));
            Body.Controls.Add(_chart);
            Body.Controls.Add(Row(_chartTitle));
            Body.Controls.Add(Columns(S(120), cardCells));
            Body.Controls.Add(Row(_cardsTitle, recipeButton));
            Body.Controls.Add(Columns(S(90), tileCells));

            Refresh_();
        }

        private static ApcValueCard Card()
        {
            return new ApcValueCard { ShowLamp = true, Dock = DockStyle.Fill };
        }

        // 타일·헤더·카드·그래프를 현재 설비·레시피로 다시 채운다.
        private void Refresh_()
        {
            int alarm = 0, warn = 0, stop = 0, standby = 0;
            for (int i = 0; i < _machines.Count; i++)
            {
                var m = _machines[i];
                var v = Judge.Machine(m, _recipe);
                _tiles[i].Text = m.Name;
                _tiles[i].Status = v.Status;
                _tiles[i].StatusText = Judge.TileText(m, _recipe);
                _tiles[i].Selected = i == _selected;
                if (v.Status == StatusKind.Alarm) alarm++;
                else if (v.Status == StatusKind.Warn) warn++;
                else if (m.Process == "정지") stop++;
                else if (m.Process == "대기") standby++;
            }

            var parts = new List<string> { "전체" };
            if (alarm > 0) parts.Add("알람 " + alarm);
            if (warn > 0) parts.Add("경고 " + warn);
            if (stop > 0) parts.Add("정지 " + stop);
            if (standby > 0) parts.Add("대기 " + standby);
            if (parts.Count == 1) parts.Add("정상");
            Header.StatusText = string.Join(" · ", parts.ToArray());
            Header.Status = alarm > 0 ? StatusKind.Alarm : warn > 0 ? StatusKind.Warn : (stop + standby) > 0 ? StatusKind.Stop : StatusKind.Run;

            ShowSelected();
        }

        private void ShowSelected()
        {
            var m = _machines[_selected];
            _cardsTitle.Text = m.Name + " 현재값";
            _chartTitle.Text = m.Name + " 금형 온도 추이";

            SetCard(_tempCard, m, m.Name + " 금형 온도", m.Temp, "℃", Judge.Temp(m.Temp),
                "설정 " + Judge.F(_recipe.TempSet) + " · 상한 " + Judge.F(Judge.TempUpper));
            SetCard(_pressureCard, m, m.Name + " 사출 압력", m.Pressure, "bar", Judge.Pressure(m.Pressure, _recipe),
                "상한 " + Judge.F(_recipe.PressureUpper) + " · 경고 " + Judge.F(Judge.PressureWarn(_recipe)));
            SetCard(_cycleCard, m, m.Name + " 사이클 타임", m.Cycle, "s", Judge.Cycle(m.Cycle, _recipe),
                "목표 " + _recipe.CycleTarget + " · 상한 " + Judge.F(Judge.CycleUpper(_recipe)));

            _chart.SetValues(m.Trend);
            _chart.Reference = _recipe.TempSet;
            _chart.UpperLimit = Judge.TempUpper;
            _chart.LowerLimit = Judge.TempLower;
        }

        // 값이 없거나 가동이 아니면 판정하지 않는다(카드 Stop).
        private static void SetCard(ApcValueCard card, MachineInfo m, string title, double? value, string unit, Verdict verdict, string footer)
        {
            card.Title = title;
            card.Value = value.HasValue ? Judge.F(value.Value) : "";
            card.Unit = unit;
            card.Footer = footer;
            card.Status = m.IsRunning && value.HasValue ? verdict.Status : StatusKind.Stop;
        }

        private Control BuildHistoryGrid()
        {
            var grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            grid.Columns.Add(new ApcLampColumn { HeaderText = "상태", Name = "status", Width = S(90) });
            grid.Columns.Add("time", "시각");
            grid.Columns.Add("unit", "설비");
            grid.Columns.Add("text", "내용");
            grid.Columns["text"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["text"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            foreach (var r in _source.GetAlarmHistory())
                grid.Rows.Add(new ApcLampValue(r.Status, r.StatusText), r.Time, r.Machine, r.Text);
            return grid;
        }

        private void EditRecipe()
        {
            using (var dlg = new RecipeDialog(_recipe))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _recipe = dlg.Result;
                _source.SaveRecipe(_recipe);
                Refresh_();
            }
        }
    }
}
