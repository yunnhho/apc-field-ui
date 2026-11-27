using System;
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
        private const int CellMaxW = 400, CellMaxH = 600, CellGap = 12, CellsPerRow = 4;

        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcInfoStrip _job = new ApcInfoStrip { Dock = DockStyle.Fill };
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly Panel _diagram = new Panel { Dock = DockStyle.Fill };
        private readonly Label[] _cells = new Label[PalletState.CellsPerLayer];
        private readonly ApcGrid _layers = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, FillRows = true };
        private readonly Panel _palletBlock;
        private readonly Panel _layerBlock;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("포장 라인 팔레트 적재")
        {
            _source = source;
            _job.SetItems("품번", "고객사", "목표 박스");
            _scan.Width = S(460);
            _scan.Scanned += OnBoxScanned;

            var close = Button("팔레트 마감", ApcButtonVariant.Primary, 170);
            close.Click += (s, e) => ClosePallet();

            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = ApcTheme.GetFont(FontStyle.Bold),
                    ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text) };
                _diagram.Controls.Add(_cells[i]);
            }
            _diagram.Resize += (s, e) => PlaceCells();

            _layers.Columns.Add("layer", "층");
            _layers.Columns.Add("boxes", "박스 수");
            _layers.Columns.Add("done", "완료 시각");
            _layers.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            foreach (DataGridViewColumn c in _layers.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "적재"), new ApcLampValue(StatusKind.Warn, "다음 칸"), new ApcLampValue(StatusKind.Stop, "빈칸"));
            _palletBlock = Titled("팔레트", _diagram, legend);
            _layerBlock = Titled("층별 요약", _layers);

            var scanRow = Row(Caption("박스 라벨"), _scan, close);
            Body.Controls.Add(FillColumns(new Control[] { _palletBlock, _layerBlock }, new[] { 55f, 45f }));
            Body.Controls.Add(scanRow);
            Body.Controls.Add(Columns(S(52), new Control[] { _job }));
            Body.Controls.Add(Columns(S(140), new Control[] { _judge }));

            WorkOrder order = _source.GetWorkOrder();
            _job.SetValue("품번", order.PartNo);
            _job.SetValue("고객사", order.Customer);
            _job.SetValue("목표 박스", order.TargetBoxes + " 박스");
            Reload();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null)
            {
                _scan.Attach(form);
                form.Text = "포장 라인 팔레트 적재";
            }
        }

        private void OnBoxScanned(object sender, ScanEventArgs e)
        {
            ScanResult result = _source.ScanBox(e.Code);
            e.Accepted = result.Accepted;
            Reload();
            if (!result.Accepted) BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(FindForm(), result.Message)));
        }

        private void ClosePallet()
        {
            PalletState pallet = _source.GetPallet();
            using (var dlg = new PalletCloseDialog(pallet.BoxCount, pallet.TargetBoxes))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (_source.ClosePallet(dlg.PalletLabel, dlg.Badge, dlg.TotalKg))
                    ApcDialog.Alert(FindForm(), "팔레트를 마감했습니다 · " + dlg.PalletLabel);
            }
        }

        private void Reload()
        {
            PalletState p = _source.GetPallet();
            int next = p.IsFull ? -1 : p.BoxesInCurrentLayer;

            _judge.Status = StatusKind.Run;
            _judge.Text = p.IsFull ? "적재 완료" : p.CurrentLayer + "층 " + (next + 1) + "번 칸";
            _judge.SubText = p.IsFull ? "팔레트 마감을 누르세요" : "박스 라벨을 스캔하세요";
            _palletBlock.Text = "팔레트  " + p.CurrentLayer + "층 · " + p.BoxCount + "/" + p.TargetBoxes + " 박스";

            int layerFirstBox = (p.CurrentLayer - 1) * PalletState.CellsPerLayer;
            for (int i = 0; i < _cells.Length; i++)
            {
                StatusKind kind = i < p.BoxesInCurrentLayer ? StatusKind.Run : i == next ? StatusKind.Warn : StatusKind.Stop;
                string word = kind == StatusKind.Run ? "적재" : kind == StatusKind.Warn ? "다음" : "빈칸";
                _cells[i].BackColor = ApcTheme.Status(kind);
                _cells[i].Text = (layerFirstBox + i + 1) + Environment.NewLine + word;
            }

            _layers.Rows.Clear();
            for (int layer = 1; layer <= PalletState.Layers; layer++)
            {
                int n = p.BoxesInLayer(layer);
                bool done = n == PalletState.CellsPerLayer;
                StatusKind kind = done ? StatusKind.Run : n > 0 ? StatusKind.Warn : StatusKind.Stop;
                DateTime? at = p.LayerDoneAt[layer - 1];
                _layers.Rows.Add(layer + "층", n > 0 ? n.ToString() : "", at.HasValue ? at.Value.ToString("HH:mm:ss") : "",
                    new ApcLampValue(kind, done ? "완료" : n > 0 ? "적재 중" : "대기"));
            }
            _layers.ClearSelection();
            PlaceCells();
            _scan.Focus();
        }

        // 2×4 칸을 영역에 맞춰 놓는다(칸 최대 크기 제한, 가운데 정렬). 그리기는 하지 않고 Label 위치만 계산.
        private void PlaceCells()
        {
            int gap = S(CellGap), rows = PalletState.CellsPerLayer / CellsPerRow;
            int w = Math.Min(S(CellMaxW), (_diagram.Width - gap * (CellsPerRow - 1)) / CellsPerRow);
            int h = Math.Min(S(CellMaxH), (_diagram.Height - gap * (rows - 1)) / rows);
            if (w <= 0 || h <= 0) return;
            int left = (_diagram.Width - (w * CellsPerRow + gap * (CellsPerRow - 1))) / 2;
            int top = 0;
            for (int i = 0; i < _cells.Length; i++)
                _cells[i].SetBounds(left + (i % CellsPerRow) * (w + gap), top + (i / CellsPerRow) * (h + gap), w, h);
        }
    }
}
