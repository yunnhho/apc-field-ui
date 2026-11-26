using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using BumperPaintBoothMonitor.Data;
using System.Drawing;

namespace BumperPaintBoothMonitor.Views
{
    /// <summary>
    /// 도장 라인 부스 환경 모니터링. 맨 위 판정판 = 진행 중인 이탈(없으면 전체 정상/주의),
    /// 부스 타일 → 선택 부스의 현재값 카드·8시간 추이, 투입 정보, 환경 이탈 이력.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private readonly IMainViewSource _source;
        private BoothInfo[] _booths;
        private Deviation[] _devs;
        private int _selected;

        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcStatusTile[] _tiles = new ApcStatusTile[5];
        private readonly ApcValueCard[] _cards = new ApcValueCard[4];
        private readonly ApcTrendChart _tempChart = new ApcTrendChart { Dock = DockStyle.Fill };
        private readonly ApcTrendChart _humiChart = new ApcTrendChart { Dock = DockStyle.Fill };
        private readonly Panel _tempBox, _humiBox;
        private readonly ApcInfoStrip _info = new ApcInfoStrip();
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false };
        private readonly Label _reason = Caption("");
        private readonly ApcButton _register = Button("조치 등록", ApcButtonVariant.Primary);

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("도장 라인 부스 모니터링")
        {
            _source = source;
            _booths = source.GetBooths();
            _selected = Array.FindIndex(_booths, b => b.Status() == StatusKind.Alarm);
            if (_selected < 0) _selected = 0;

            for (int i = 0; i < _tiles.Length; i++)
            {
                int index = i;
                _tiles[i] = new ApcStatusTile { Text = _booths[i].Name, Dock = DockStyle.Fill };
                _tiles[i].Click += (s, e) => { _selected = index; ReloadBooths(); };
            }
            for (int i = 0; i < _cards.Length; i++) _cards[i] = new ApcValueCard { ShowLamp = true, Dock = DockStyle.Fill };
            _info.SetItems("투입 행거", "도료 Lot", "색상 코드", "투입 주기");
            _info.SetValue("투입 주기", "1.5분 · 행거 1개 = 범퍼 2개");

            BuildGrid();
            _tempBox = Titled("", _tempChart);
            _humiBox = Titled("", _humiChart);

            _register.Click += (s, e) => OpenAction();
            _grid.SelectionChanged += (s, e) => UpdateRegisterButton();

            Body.Controls.Add(Titled("환경 이탈 이력", _grid, _reason, _register));
            Body.Controls.Add(Columns(TitledHeight(S(120)), new Control[] { _tempBox, _humiBox }));
            Body.Controls.Add(Columns(S(110), _cards));
            Body.Controls.Add(Columns(_info.Height, new Control[] { _info }));
            Body.Controls.Add(Columns(S(72), _tiles));
            Body.Controls.Add(Columns(S(96), new Control[] { _judge }));

            ReloadAll();
        }

        private void ReloadAll()
        {
            _booths = _source.GetBooths();
            _devs = _source.GetDeviations();
            ReloadBooths();
            ReloadInput();
            ReloadGrid();
            ReloadJudge();
        }

        private void ReloadBooths()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                _tiles[i].Status = _booths[i].Status();
                _tiles[i].Selected = i == _selected;
            }
            var b = _booths[_selected];
            for (int i = 0; i < _cards.Length; i++) FillCard(_cards[i], b, i);

            _tempBox.Text = b.Name + " 온도 추이 · 최근 8시간 (℃)";
            _humiBox.Text = b.Name + (b.Specs[BoothSpecs.Humi].Managed ? " 습도 추이 · 최근 8시간 (%RH)" : " 습도 · 관리 안 함");
            FillChart(_tempChart, b.TempTrend, b.Specs[BoothSpecs.Temp]);
            FillChart(_humiChart, b.HumiTrend, b.Specs[BoothSpecs.Humi]);
        }

        private static void FillCard(ApcValueCard card, BoothInfo b, int i)
        {
            var spec = b.Specs[i];
            card.Title = b.Name + " " + spec.Name;
            card.Unit = spec.Unit;
            card.Status = b.ItemStatus(i);
            if (!spec.Managed || double.IsNaN(b.Values[i]))
            {
                card.Value = "";
                card.Footer = "관리 안 함";
                return;
            }
            card.Value = spec.Fmt(b.Values[i]);
            string range = "관리 범위 " + spec.RangeText();
            switch (card.Status)
            {
                case StatusKind.Alarm: range += " · 이탈"; break;
                case StatusKind.Warn: range += " · 주의"; break;
            }
            card.Footer = range;
        }

        private static void FillChart(ApcTrendChart chart, double[] values, ItemSpec spec)
        {
            chart.Reference = spec.Managed ? (spec.Lo + spec.Hi) / 2 : (double?)null;
            chart.UpperLimit = spec.Managed ? spec.Hi : (double?)null;
            chart.LowerLimit = spec.Managed ? spec.Lo : (double?)null;
            chart.XLabels = new[] { "-8시간", "-6시간", "-4시간", "-2시간", "현재" };
            chart.SetValues(values);
        }

        private void ReloadInput()
        {
            var input = _source.GetLineInput();
            _info.SetValue("투입 행거", input.Hanger);
            _info.SetValue("도료 Lot", input.PaintLot);
            _info.SetValue("색상 코드", input.ColorCode);
        }

        // 맨 위 판정판: 진행 중 이탈이 있으면 그 이탈, 없으면 부스 상태 요약
        private void ReloadJudge()
        {
            var now = _source.Now;
            var open = Array.Find(_devs, d => d.Open);
            if (open != null)
            {
                var spec = open.Spec;
                double current = Array.Find(_booths, b => b.Name == open.Booth).Values[open.ItemIndex];
                bool advise = open.Duration(now).TotalSeconds >= ItemSpec.StopAdviceSeconds;
                _judge.Status = StatusKind.Alarm;
                _judge.Text = open.Booth + " " + spec.Name + " 이탈" + (advise ? " · 라인 투입 정지 권고" : "");
                _judge.SubText = spec.LimitText(current) + " · " + ActionDialog.FormatDuration(open.Duration(now)) + " 지속";
                return;
            }
            var worst = Array.Find(_booths, b => b.Status() == StatusKind.Warn);
            _judge.Status = worst != null ? StatusKind.Warn : StatusKind.Run;
            _judge.Text = worst != null ? worst.Name + " 부스 주의" : "전 부스 정상";
            _judge.SubText = "";
        }

        private void BuildGrid()
        {
            _grid.Columns.Add(new ApcLampColumn());
            _grid.Columns.Add("booth", "부스");
            _grid.Columns.Add("item", "항목");
            _grid.Columns.Add("start", "시작");
            _grid.Columns.Add("end", "종료");
            _grid.Columns.Add("dur", "지속 시간");
            _grid.Columns.Add("max", "최대 이탈값");
            _grid.Columns.Add("hanger", "영향 행거");
            _grid.Columns.Add("action", "조치");
            _grid.Columns.Add("by", "확인자");
            _grid.Columns["hanger"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns["item"].Width = S(110);
        }

        private void ReloadGrid()
        {
            var now = _source.Now;
            int keep = _grid.CurrentRow != null ? _grid.CurrentRow.Index : -1;
            _grid.Rows.Clear();
            foreach (var d in _devs)
            {
                var spec = d.Spec;
                int row = _grid.Rows.Add(
                    d.Open ? StatusKind.Alarm : d.HasAction ? StatusKind.Run : StatusKind.Warn,
                    d.Booth, spec.Name + " (" + spec.Unit + ")",
                    d.Start.ToString("HH:mm:ss"), d.Open ? "진행 중" : d.End.Value.ToString("HH:mm:ss"),
                    ActionDialog.FormatDuration(d.Duration(now)), spec.Fmt(d.Worst),
                    d.HangerRange(now) + (d.Open ? " · 투입 중" : " · " + d.HangerCount(now) + "개"),
                    d.HasAction ? d.Action : "미등록", d.Confirmer);
                // 이탈 중 투입된 행거 = 굵게, 조치가 없는 건은 알람색
                var style = _grid.Rows[row].Cells["hanger"].Style;
                style.Font = ApcTheme.GetFont(FontStyle.Bold);
                if (!d.HasAction) style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            }
            int select = keep >= 0 && keep < _grid.Rows.Count ? keep : Array.FindIndex(_devs, d => !d.HasAction);
            if (select >= 0) { _grid.CurrentCell = _grid.Rows[select].Cells[1]; _grid.Rows[select].Selected = true; }
            UpdateRegisterButton();
        }

        private Deviation SelectedDeviation()
        {
            var row = _grid.CurrentRow;
            return row == null || row.Index >= _devs.Length ? null : _devs[row.Index];
        }

        private void UpdateRegisterButton()
        {
            var d = SelectedDeviation();
            _register.Enabled = d != null && !d.HasAction;
            _reason.Text = d == null ? "이력에서 이탈 건을 선택하세요" : d.HasAction ? "이미 조치가 등록된 이탈입니다" : "";
        }

        private void OpenAction()
        {
            var d = SelectedDeviation();
            if (d == null || d.HasAction) return;
            using (var dlg = new ActionDialog(d, _source.Now, _source.CurrentUser))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.SaveAction(d, dlg.ChosenAction, dlg.Remark);
            }
            ReloadAll();
        }
    }
}
