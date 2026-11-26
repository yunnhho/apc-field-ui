using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using BumperPaintBoothMonitor.Data;

namespace BumperPaintBoothMonitor.Views
{
    /// <summary>메인 화면: 라인 판정 → 부스 타일 → 선택 부스 투입 정보·현재값·추이 → 환경 이탈 이력. 데이터는 IMainViewSource에서만 받는다.</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private static readonly BoothItem[] Items = { BoothItem.Temp, BoothItem.Humi, BoothItem.Wind, BoothItem.Dp };

        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcStatusTile[] _tiles = new ApcStatusTile[5];
        private readonly ApcInfoStrip _strip = new ApcInfoStrip { Dock = DockStyle.Fill };
        private readonly ApcValueCard[] _cards = new ApcValueCard[4];
        private readonly ApcTrendChart _tempChart = new ApcTrendChart { Dock = DockStyle.Fill };
        private readonly ApcTrendChart _humiChart = new ApcTrendChart { Dock = DockStyle.Fill };
        private readonly Panel _tempBox, _humiBox;
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false };
        private readonly ApcButton _actionButton = Button("이탈 조치 등록", ApcButtonVariant.Primary, 150);
        private readonly Label _actionReason = Caption("");

        private Booth[] _booths;
        private List<Deviation> _devs;
        private int _selected = 3;   // 처음엔 이탈 중인 클리어

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("도장 라인 모니터링")
        {
            _source = source;
            _booths = source.GetBooths();

            var tiles = new Control[_tiles.Length];
            for (int i = 0; i < _tiles.Length; i++)
            {
                int index = i;
                _tiles[i] = new ApcStatusTile { Dock = DockStyle.Fill };
                _tiles[i].Click += (s, e) => { _selected = index; ShowSelected(); };
                tiles[i] = _tiles[i];
            }
            var cards = new Control[_cards.Length];
            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i] = new ApcValueCard { Dock = DockStyle.Fill, ShowLamp = true };
                cards[i] = _cards[i];
            }
            _strip.SetItems("투입 행거", "도료 Lot", "색상 코드");

            _tempBox = Titled("", _tempChart);
            _humiBox = Titled("", _humiChart);

            BuildGrid();
            _grid.SelectionChanged += (s, e) => UpdateActionButton();
            _actionButton.Click += (s, e) => OpenActionDialog();

            Body.Controls.Add(Titled("환경 이탈 이력", _grid, _actionReason, _actionButton));
            Body.Controls.Add(Columns(TitledHeight(S(150)), new Control[] { _tempBox, _humiBox }));
            Body.Controls.Add(Columns(S(110), cards));
            Body.Controls.Add(Columns(S(44), new Control[] { _strip }));
            Body.Controls.Add(Columns(S(90), tiles));
            Body.Controls.Add(Columns(S(96), new Control[] { _judge }));

            Reload();
        }

        /// <summary>데이터 인터페이스에서 다시 읽어 화면 전체를 갱신한다.</summary>
        public void Reload()
        {
            _booths = _source.GetBooths();
            _devs = _source.GetDeviations();
            FillGrid();
            UpdateJudge();
            ShowSelected();
        }

        private void BuildGrid()
        {
            _grid.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            _grid.Columns.Add("booth", "부스");
            _grid.Columns.Add("item", "항목");
            _grid.Columns.Add("start", "시작");
            _grid.Columns.Add("end", "종료");
            _grid.Columns.Add("dur", "지속 시간");
            _grid.Columns.Add("peak", "최대값");
            _grid.Columns.Add("action", "조치");
            _grid.Columns.Add("who", "확인자");
            _grid.Columns.Add("hangers", "영향 행거");
            _grid.Columns["hangers"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns["hangers"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        }

        private void FillGrid()
        {
            int keep = _grid.CurrentRow == null ? 0 : _grid.CurrentRow.Index;
            DateTime now = _source.Now;
            _grid.Rows.Clear();
            foreach (Deviation d in _devs)
            {
                ItemSpec spec = d.Booth.Spec(d.Item);
                ApcLampValue state = d.Open ? new ApcLampValue(StatusKind.Alarm, "이탈 중")
                    : d.Registered ? new ApcLampValue(StatusKind.Run, "조치 완료")
                    : new ApcLampValue(StatusKind.Warn, "조치 필요");
                int row = _grid.Rows.Add(state, d.Booth.Name, BoothSpec.Name(d.Item) + " (" + BoothSpec.Unit(d.Item) + ")",
                    d.Start.ToString("HH:mm:ss"), d.End.HasValue ? d.End.Value.ToString("HH:mm:ss") : "",
                    BoothSpec.Duration(d.Duration(now)), spec.Format(d.Peak), d.Action ?? "", d.Confirmer ?? "", d.HangerRange(now));
                _grid.Rows[row].Tag = d;
                // 이탈 중 투입된 행거: 영향 없음으로 닫힌 것 외에는 굵은 알람색
                if (d.Action != ActionDialog.NoImpact)
                {
                    DataGridViewCellStyle style = _grid.Rows[row].Cells["hangers"].Style;
                    style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                    style.Font = ApcTheme.GetFont(FontStyle.Bold);
                }
            }
            if (_grid.Rows.Count > 0)
            {
                int index = Math.Min(keep, _grid.Rows.Count - 1);
                _grid.CurrentCell = _grid.Rows[index].Cells["booth"];
                _grid.Rows[index].Selected = true;
            }
            UpdateActionButton();
        }

        private void UpdateJudge()
        {
            Deviation open = null;
            foreach (Deviation d in _devs)
                if (d.Open && (open == null || d.Start < open.Start)) open = d;

            if (open != null)
            {
                TimeSpan t = open.Duration(_source.Now);
                bool stop = t.TotalMinutes >= BoothSpec.StopRecommendMinutes;
                _judge.Status = StatusKind.Alarm;
                _judge.Text = stop ? "라인 투입 정지 권고" : "환경 이탈 발생";
                _judge.SubText = ItemSentence(open.Booth, open.Item) + " · 지속 " + BoothSpec.Duration(t);
                return;
            }
            foreach (Booth b in _booths)
                foreach (BoothItem item in Items)
                    if (b.ItemStatus(item) == StatusKind.Warn)
                    {
                        _judge.Status = StatusKind.Warn;
                        _judge.Text = "경계 근접 주의";
                        _judge.SubText = ItemSentence(b, item);
                        return;
                    }
            _judge.Status = StatusKind.Run;
            _judge.Text = "정상";
            _judge.SubText = "전 부스 관리 범위 안";
        }

        /// <summary>"클리어 습도 상한 초과 77 %RH (상한 75)" — 판정은 BoothSpec.Judge 결과를 그대로 말한다.</summary>
        private static string ItemSentence(Booth b, BoothItem item)
        {
            ItemSpec spec = b.Spec(item);
            decimal v = b.Value(item).Value;
            bool upper = v > spec.High || (v <= spec.High && v >= spec.Nominal);
            string verb = b.ItemStatus(item) == StatusKind.Alarm
                ? (upper ? "상한 초과" : "하한 미달")
                : (upper ? "상한 근접" : "하한 근접");
            return b.Name + " " + BoothSpec.Name(item) + " " + verb + " " + spec.Format(v) + " " + BoothSpec.Unit(item)
                + " (" + (upper ? "상한 " + spec.Format(spec.High) : "하한 " + spec.Format(spec.Low)) + ")";
        }

        private void ShowSelected()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                Booth t = _booths[i];
                _tiles[i].Status = t.Status;
                _tiles[i].Text = t.Name + "  " + StateWord(t.Status);
                _tiles[i].Selected = i == _selected;
            }
            Booth b = _booths[_selected];

            _strip.SetValue("투입 행거", b.Hanger + (b.Hanger.Length > 0 && HasOpenDeviation(b) ? " (이탈 중 투입)" : ""));
            _strip.SetValue("도료 Lot", b.Lot);
            _strip.SetValue("색상 코드", b.Color);

            for (int i = 0; i < Items.Length; i++) FillCard(_cards[i], b, Items[i]);

            _tempBox.Text = b.Name + " 온도 추이 (최근 8시간)";
            _humiBox.Text = b.Name + (b.IsOven ? " 습도 (관리 안 함)" : " 습도 추이 (최근 8시간)");
            FillChart(_tempChart, b.TempTrend, b.Spec(BoothItem.Temp), "℃");
            FillChart(_humiChart, b.HumiTrend, b.Spec(BoothItem.Humi), "%RH");
        }

        private bool HasOpenDeviation(Booth b)
        {
            foreach (Deviation d in _devs) if (d.Open && d.Booth == b) return true;
            return false;
        }

        private static string StateWord(StatusKind s)
        {
            switch (s)
            {
                case StatusKind.Run: return "정상";
                case StatusKind.Warn: return "주의";
                case StatusKind.Alarm: return "이탈";
                default: return "정지";
            }
        }

        private static void FillCard(ApcValueCard card, Booth b, BoothItem item)
        {
            ItemSpec spec = b.Spec(item);
            decimal? v = b.Value(item);
            card.Title = b.Name + " " + BoothSpec.Name(item);
            card.Unit = BoothSpec.Unit(item);
            card.Value = v.HasValue && spec != null ? spec.Format(v.Value) : "";
            card.Footer = spec == null ? "관리 안 함" : "관리 범위 " + spec.Range;
            card.Status = b.ItemStatus(item);
        }

        private static void FillChart(ApcTrendChart chart, decimal[] values, ItemSpec spec, string unit)
        {
            var d = new List<double>();
            foreach (decimal v in values) d.Add((double)v);
            chart.SetValues(d);
            chart.XLabels = new[] { "-8시간", "-4시간", "현재" };
            chart.Reference = spec == null ? (double?)null : (double)spec.Nominal;
            chart.LowerLimit = spec == null ? (double?)null : (double)spec.Low;
            chart.UpperLimit = spec == null ? (double?)null : (double)spec.High;
        }

        private Deviation SelectedDeviation()
        {
            return _grid.CurrentRow == null ? null : _grid.CurrentRow.Tag as Deviation;
        }

        private void UpdateActionButton()
        {
            Deviation d = SelectedDeviation();
            _actionButton.Enabled = d != null && !d.Registered;
            _actionReason.Text = d == null ? "조치할 이력 행을 선택하세요" : d.Registered ? "조치 등록 완료 · " + d.Action : "";
        }

        private void OpenActionDialog()
        {
            Deviation d = SelectedDeviation();
            if (d == null || d.Registered) return;
            DateTime now = _source.Now;
            string interval = d.Start.ToString("HH:mm:ss") + " ~ " + (d.End.HasValue
                ? d.End.Value.ToString("HH:mm:ss") : "이탈 중 (지속 " + BoothSpec.Duration(d.Duration(now)) + ")");
            using (var dlg = new ActionDialog(d.Booth.Name, BoothSpec.Name(d.Item) + " (" + BoothSpec.Unit(d.Item) + ")",
                interval, d.HangerRange(now), d.Risk, d.Open, null))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.RegisterAction(d, dlg.Action, dlg.Remark, dlg.Confirmer);
                Reload();
            }
        }
    }
}
