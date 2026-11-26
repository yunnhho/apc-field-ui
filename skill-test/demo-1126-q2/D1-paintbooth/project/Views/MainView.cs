using System;
using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using BumperPaintBoothMonitor.Data;

namespace BumperPaintBoothMonitor.Views
{
    /// <summary>
    /// 도장 라인 부스 환경 모니터링. 위에서부터: 라인 판정 → 부스 타일(선택) → 투입 정보 → 선택 부스 현재값 → 8시간 추이 → 이탈 이력.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int ColStatus = 0;
        private static readonly string[] HistoryHeaders = { "부스", "항목", "시작", "종료", "지속 시간", "최대 이탈값", "영향 행거", "조치", "확인자" };

        private readonly IMainViewSource _source;
        private IList<Booth> _booths;
        private IList<Excursion> _excursions;
        private int _selected = 3;   // 처음에는 이탈 중인 부스를 보여 준다(데이터가 정함, 아래 Load에서 보정)

        private readonly ApcJudgePanel _panel = new ApcJudgePanel { Dock = DockStyle.Fill };
        private ApcStatusTile[] _tiles;
        private readonly ApcInfoStrip _strip = new ApcInfoStrip { Dock = DockStyle.Fill };
        private ApcValueCard[] _cards;
        private readonly ApcTrendChart _tempChart = new ApcTrendChart { Dock = DockStyle.Fill };
        private readonly ApcTrendChart _humiChart = new ApcTrendChart { Dock = DockStyle.Fill };
        private Panel _tempCell, _humiCell;
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false };
        private readonly Label _reason = Caption("");
        private readonly ApcButton _actionButton = Button("조치 등록", ApcButtonVariant.Primary);

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("도장 라인 모니터링")
        {
            _source = source;
            _booths = source.GetBooths();
            _excursions = source.GetExcursions();
            _selected = FirstAbnormal();

            _tiles = new ApcStatusTile[_booths.Count];
            var tileCells = new Control[_booths.Count];
            for (int i = 0; i < _booths.Count; i++)
            {
                int index = i;
                _tiles[i] = new ApcStatusTile { Dock = DockStyle.Fill };
                _tiles[i].Click += (s, e) => Select(index);
                tileCells[i] = _tiles[i];
            }

            _cards = new ApcValueCard[BoothJudge.Items.Length];
            var cardCells = new Control[_cards.Length];
            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i] = new ApcValueCard { Dock = DockStyle.Fill, ShowLamp = true };
                cardCells[i] = _cards[i];
            }

            _tempCell = Titled("온도 추이", _tempChart);
            _humiCell = Titled("습도 추이", _humiChart);

            BuildGrid();
            _grid.SelectionChanged += (s, e) => UpdateActionButton();
            _actionButton.Click += (s, e) => OpenActionDialog();

            Body.Controls.Add(Titled("환경 이탈 이력", _grid, _reason, _actionButton));
            Body.Controls.Add(Columns(TitledHeight(S(120)), new Control[] { _tempCell, _humiCell }));
            Body.Controls.Add(Columns(S(112), cardCells));
            Body.Controls.Add(Columns(S(40), new Control[] { _strip }));
            Body.Controls.Add(Columns(S(72), tileCells));
            Body.Controls.Add(Columns(S(96), new Control[] { _panel }));

            _strip.SetItems("투입 행거", "도료 Lot", "색상 코드");
            Reload();
        }

        // 이탈 중인 부스가 있으면 그 부스, 없으면 가장 나쁜 부스, 같으면 앞 순서
        private int FirstAbnormal()
        {
            int best = 0, bestRank = -1;
            for (int i = 0; i < _booths.Count; i++)
            {
                var s = BoothJudge.BoothStatus(_booths[i]);
                int rank = s == StatusKind.Alarm ? 3 : s == StatusKind.Warn ? 2 : s == StatusKind.Stop ? 1 : 0;
                if (rank > bestRank) { best = i; bestRank = rank; }
            }
            return best;
        }

        private void Select(int index)
        {
            _selected = index;
            ShowSelected();
        }

        private void Reload()
        {
            ShowPanel();
            ShowInput();
            ShowSelected();
            FillGrid();
        }

        // ---- 라인 판정: 이탈 진행 건이 있으면 그것을, 없으면 주의 부스를 말한다 ----
        private void ShowPanel()
        {
            Excursion worst = null;
            foreach (var ex in _excursions)
                if (ex.Ongoing && (worst == null || ex.Start < worst.Start)) worst = ex;
            if (worst != null)
            {
                TimeSpan d = worst.Duration(_source.Now);
                bool stop = d.TotalSeconds >= BoothJudge.StopAdviceSeconds;
                _panel.Status = StatusKind.Alarm;
                _panel.Text = stop ? "라인 투입 정지 권고" : "환경 이탈 중";
                _panel.SubText = worst.Booth + " 부스 " + worst.Item + " " + ValueText(worst.Booth, worst.Item, worst.MaxText) + " · 이탈 " + BoothJudge.DurationText(d) + " 지속";
                return;
            }
            foreach (var b in _booths)
            {
                if (BoothJudge.BoothStatus(b) != StatusKind.Warn) continue;
                _panel.Status = StatusKind.Warn;
                _panel.Text = "주의";
                _panel.SubText = b.Name + " 부스 관리 범위 경계 근접";
                return;
            }
            _panel.Status = StatusKind.Run;
            _panel.Text = "정상";
            _panel.SubText = "";
        }

        private static string ValueText(string booth, string item, string text)
        {
            foreach (var s in BoothJudge.SpecsOf(booth)) if (s.Item == item) return text + " " + s.Unit;
            return text;
        }

        private void ShowInput()
        {
            var info = _source.GetCurrentInput();
            _strip.SetValue("투입 행거", Excursion.HangerNo(info.Hanger));
            _strip.SetValue("도료 Lot", info.PaintLot);
            _strip.SetValue("색상 코드", info.ColorCode);
        }

        // ---- 선택 부스: 타일 선택 표시 · 현재값 카드 · 추이 ----
        private void ShowSelected()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                var b = _booths[i];
                var st = BoothJudge.BoothStatus(b);
                _tiles[i].Status = st;
                _tiles[i].Text = b.Name + "  " + BoothJudge.StatusWord(st);
                _tiles[i].Selected = i == _selected;
            }
            var booth = _booths[_selected];
            var specs = BoothJudge.SpecsOf(booth.Name);
            for (int i = 0; i < _cards.Length; i++)
            {
                var spec = specs[i];
                var card = _cards[i];
                card.Title = booth.Name + " " + spec.Item;
                card.Unit = spec.Unit;
                if (!spec.Managed)
                {
                    card.Value = ""; card.Status = StatusKind.Stop; card.Footer = "관리 안 함";
                    continue;
                }
                var v = booth.ValueOf(i);
                card.Value = spec.Format(v);
                card.Status = booth.Running ? BoothJudge.Judge(spec, v) : StatusKind.Stop;   // 정지 중 값은 판정하지 않는다
                card.Footer = "관리 " + spec.RangeText;
            }
            ShowChart(_tempChart, _tempCell, booth, 0);
            ShowChart(_humiChart, _humiCell, booth, 1);
        }

        private void ShowChart(ApcTrendChart chart, Panel cell, Booth booth, int item)
        {
            var spec = BoothJudge.SpecsOf(booth.Name)[item];
            if (!spec.Managed)
            {
                cell.Text = spec.Item + " 추이 (관리 안 함)";
                chart.Visible = false;
                return;
            }
            chart.Visible = true;
            cell.Text = spec.Item + " 추이 (" + spec.Unit + ", 최근 8시간)";
            chart.Reference = (double)(spec.Lower + spec.Upper) / 2;
            chart.LowerLimit = (double)spec.Lower;
            chart.UpperLimit = (double)spec.Upper;
            chart.XLabels = new[] { "-8시간", "-6시간", "-4시간", "-2시간", "현재" };
            chart.SetValues(_source.GetTrend(booth.Name, item));
        }

        // ---- 이탈 이력 ----
        private void BuildGrid()
        {
            _grid.Columns.Add(new ApcLampColumn { HeaderText = "처리" });
            foreach (var h in HistoryHeaders) _grid.Columns.Add(h, h);
            foreach (DataGridViewColumn c in _grid.Columns)
                if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns["영향 행거"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void FillGrid()
        {
            int keep = _grid.CurrentRow == null ? 0 : _grid.CurrentRow.Index;
            _grid.Rows.Clear();
            DateTime now = _source.Now;
            foreach (var ex in _excursions)
            {
                StatusKind st = ex.Ongoing ? StatusKind.Alarm : ex.HasAction ? StatusKind.Run : StatusKind.Warn;
                string word = ex.Ongoing ? "이탈 중" : ex.HasAction ? "조치 완료" : "조치 필요";
                int row = _grid.Rows.Add(new ApcLampValue(st, word), ex.Booth, ex.Item, ex.Start.ToString("HH:mm:ss"),
                    ex.Ongoing ? "진행 중" : ex.End.Value.ToString("HH:mm:ss"), BoothJudge.DurationText(ex.Duration(now)),
                    ex.MaxText, ex.HangerText, ex.HasAction ? ex.Action : "", ex.Confirmer);
                _grid.Rows[row].Tag = ex;
                // 조치가 끝나지 않은 이탈의 영향 행거는 굵은 빨간 글자로 눈에 띄게
                if (!ex.HasAction)
                {
                    var style = _grid.Rows[row].Cells["영향 행거"].Style;
                    style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                    style.Font = ApcTheme.GetFont(System.Drawing.FontStyle.Bold);
                }
            }
            if (_grid.Rows.Count > 0)
            {
                _grid.CurrentCell = _grid.Rows[Math.Min(keep, _grid.Rows.Count - 1)].Cells[1];
                _grid.Rows[_grid.CurrentCell.RowIndex].Selected = true;
            }
            UpdateActionButton();
        }

        private Excursion CurrentExcursion()
        {
            return _grid.CurrentRow == null ? null : _grid.CurrentRow.Tag as Excursion;
        }

        private void UpdateActionButton()
        {
            var ex = CurrentExcursion();
            _actionButton.Enabled = ex != null && !ex.HasAction;
            _reason.Text = ex == null ? "이력에서 행을 선택하세요" : ex.HasAction ? "조치 등록 완료" : "";
        }

        private void OpenActionDialog()
        {
            var ex = CurrentExcursion();
            if (ex == null || ex.HasAction) return;
            using (var dlg = new ExcursionActionDialog(ex, _source.Now, _source.GetConfirmers(), null))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.SaveAction(ex.Id, dlg.ActionKind, dlg.Note, dlg.Confirmer);
                _excursions = _source.GetExcursions();
                FillGrid();
            }
        }
    }
}
