using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PalletPacker.Data;

namespace PalletPacker.Views
{
    /// <summary>메인 화면. 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int CellGap = 12, CellAspectPct = 150;   // 칸 가로:세로 = 3:2

        private readonly IMainViewSource _source;
        private readonly WorkOrder _order;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly ApcButton _close;
        private readonly Label _closeReason = Caption("");
        private readonly ApcGrid _layers = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly Panel _pallet = new Panel { Dock = DockStyle.Fill };
        private readonly Label[] _cells = new Label[PalletRules.CellsPerLayer];
        private readonly Panel _palletBox;
        private IList<PlacedBox> _boxes;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("팔레트 적재")
        {
            _source = source;
            _order = source.GetOrder();
            _boxes = source.GetPlacedBoxes();
            _scan.Width = S(380);

            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = new Label { TextAlign = ContentAlignment.MiddleCenter, Font = ApcTheme.GetFont(FontStyle.Bold), ForeColor = Color.Black };
                _pallet.Controls.Add(_cells[i]);
            }
            _pallet.Resize += (s, e) => LayoutCells();

            _scan.Scanned += (s, e) => OnScan(e);
            _close = Button("팔레트 마감", ApcButtonVariant.Primary, 160);
            _close.Click += (s, e) => ClosePallet();

            _layers.Columns.Add("item", "구분");
            for (int l = 1; l <= PalletRules.Layers; l++) _layers.Columns.Add("l" + l, l + "층");
            foreach (DataGridViewColumn c in _layers.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "적재"), new ApcLampValue(StatusKind.Warn, "다음 놓을 칸"), new ApcLampValue(StatusKind.Stop, "빈칸"));
            _palletBox = Titled("", _pallet, legend);

            var order = new ApcInfoStrip();
            order.SetItems("품번", "고객사", "목표 박스 수");
            order.SetValue("품번", _order.PartNo);
            order.SetValue("고객사", _order.Customer);
            order.SetValue("목표 박스 수", _order.TargetBoxes + "개");
            order.Dock = DockStyle.None;
            order.Height = S(44);
            var orderRow = Columns(order.Height, new Control[] { order });

            var scanRow = Row(Caption("박스 라벨"), _scan, _close, _closeReason);

            // Fill 먼저, 그다음 Top 줄을 아래쪽 것부터
            Body.Controls.Add(_palletBox);
            Body.Controls.Add(Columns(TitledHeight(ApcGrid.HeightForRows(2)), new Control[] { Titled("층별 요약", _layers) }));
            Body.Controls.Add(orderRow);
            Body.Controls.Add(scanRow);
            Body.Controls.Add(Columns(S(170), new Control[] { _judge }));
            Reload();
            ShowReady();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);
        }

        private int Count { get { return _boxes.Count; } }
        private bool Full { get { return Count >= _order.TargetBoxes; } }
        // 가득 차면 마지막 층에 머문다
        private int Layer { get { return Full ? PalletRules.Layers : Count / PalletRules.CellsPerLayer + 1; } }

        private void OnScan(ScanEventArgs e)
        {
            string reject = Full ? "적재 완료 · 팔레트를 마감하세요" : _source.RegisterBox(e.Code);
            Reload();
            if (reject != null)
            {
                e.Accepted = false;
                _judge.Text = "NG";
                _judge.Status = StatusKind.Alarm;
                _judge.SubText = reject;
                return;
            }
            ShowReady();
        }

        private void ShowReady()
        {
            _judge.Status = StatusKind.Run;
            if (Full)
            {
                _judge.Text = "적재 완료";
                _judge.SubText = "팔레트를 마감하세요";
                return;
            }
            int cell = Count % PalletRules.CellsPerLayer + 1;
            _judge.Text = Layer + "층 " + cell + "번 칸";
            _judge.SubText = (Count + 1) + "번째 박스 · 남은 " + (_order.TargetBoxes - Count) + "박스";
        }

        private void Reload()
        {
            _boxes = _source.GetPlacedBoxes();
            _palletBox.Text = Layer + "층 · " + Count + "/" + _order.TargetBoxes + " 박스";
            ShowLayers();
            LayoutCells();
            ShowCloseReason();
        }

        private void ShowLayers()
        {
            var counts = new object[PalletRules.Layers + 1];
            var times = new object[PalletRules.Layers + 1];
            counts[0] = "박스 수(개)";
            times[0] = "완료 시각";
            for (int l = 0; l < PalletRules.Layers; l++)
            {
                int inLayer = Math.Max(0, Math.Min(PalletRules.CellsPerLayer, Count - l * PalletRules.CellsPerLayer));
                counts[l + 1] = inLayer == 0 ? "" : inLayer.ToString();
                bool done = inLayer == PalletRules.CellsPerLayer;
                times[l + 1] = done ? _boxes[(l + 1) * PalletRules.CellsPerLayer - 1].Time.ToString("HH:mm:ss") : "";
            }
            _layers.Rows.Clear();
            _layers.Rows.Add(counts);
            _layers.Rows.Add(times);
        }

        private void LayoutCells()
        {
            int baseNo = (Layer - 1) * PalletRules.CellsPerLayer;
            int inLayer = Math.Min(PalletRules.CellsPerLayer, Count - baseNo);
            int gap = S(CellGap);
            int w = (_pallet.ClientSize.Width - gap * (PalletRules.Columns - 1)) / PalletRules.Columns;
            int h = (_pallet.ClientSize.Height - gap * (PalletRules.Rows - 1)) / PalletRules.Rows;
            if (w <= 0 || h <= 0) return;
            // 칸이 한쪽으로만 커지지 않게 3:2로 맞추고 가운데에 놓는다
            if (w * 100 > h * CellAspectPct) w = h * CellAspectPct / 100; else h = w * 100 / CellAspectPct;
            int left = (_pallet.ClientSize.Width - (w * PalletRules.Columns + gap * (PalletRules.Columns - 1))) / 2;
            int top = (_pallet.ClientSize.Height - (h * PalletRules.Rows + gap * (PalletRules.Rows - 1))) / 2;
            for (int i = 0; i < _cells.Length; i++)
            {
                int row = i / PalletRules.Columns, col = i % PalletRules.Columns;
                StatusKind kind = i < inLayer ? StatusKind.Run : (i == inLayer && !Full ? StatusKind.Warn : StatusKind.Stop);
                string word = kind == StatusKind.Run ? "적재" : kind == StatusKind.Warn ? "다음" : "빈칸";
                _cells[i].SetBounds(left + col * (w + gap), top + row * (h + gap), w, h);
                _cells[i].BackColor = ApcTheme.Status(kind);
                _cells[i].Text = (baseNo + i + 1) + "\n" + word;
            }
        }

        private void ShowCloseReason()
        {
            _close.Enabled = Full;
            _closeReason.Text = Full ? "" : "박스 " + Count + "/" + _order.TargetBoxes + " · 목표 수량 후 마감";
        }

        private void ClosePallet()
        {
            using (var dlg = new PalletCloseDialog(Count, _order.TargetBoxes))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.ClosePallet(dlg.PalletLabel, dlg.Badge, dlg.TotalKg);
            }
            ApcDialog.Alert(FindForm(), "팔레트를 마감했습니다. 다음 팔레트를 시작합니다.");
            Reload();
            ShowReady();
        }
    }
}
