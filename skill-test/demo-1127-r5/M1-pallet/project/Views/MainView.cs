using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PalletPacker.Data;

namespace PalletPacker.Views
{
    /// <summary>메인 화면. 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private readonly IMainViewSource _source;
        private readonly WorkOrder _order;
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcGrid _summary = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ScrollBars = ScrollBars.None };
        private readonly PalletBoard _board = new PalletBoard { Dock = DockStyle.Fill };
        private readonly Panel _boardCell;
        private readonly ApcButton _close = Button("팔레트 마감", ApcButtonVariant.Primary, 200);
        private readonly Label _closeNote = Caption("");

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("팔레트 적재")
        {
            _source = source;
            _order = source.GetWorkOrder();
            _scan.Width = S(300);
            _scan.Scanned += (s, e) => OnBoxScanned(e);

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "적재"), new ApcLampValue(StatusKind.Warn, "다음 칸"), new ApcLampValue(StatusKind.Stop, "빈칸"));
            _boardCell = Titled("", _board, legend);

            _summary.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            _summary.Columns.Add("layer", "층");
            _summary.Columns.Add("boxes", "박스 수(개)");
            _summary.Columns.Add("done", "완료 시각");
            foreach (DataGridViewColumn c in _summary.Columns)
                if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var strip = new ApcInfoStrip();
            strip.SetItems("품번", "고객사", "목표(박스)");
            strip.SetValue("품번", _order.PartNo);
            strip.SetValue("고객사", _order.Customer);
            strip.SetValue("목표(박스)", _order.TargetBoxes.ToString());
            strip.Dock = DockStyle.None;
            strip.Width = strip.PreferredWidth;

            _close.Click += (s, e) => ClosePallet();

            // Fill(도식) 먼저, Top 줄은 화면 아래쪽 것부터
            Body.Controls.Add(_boardCell);
            Body.Controls.Add(Row(_close, _closeNote));
            Body.Controls.Add(Columns(TitledHeight(ApcGrid.HeightForRows(PalletRules.Layers)),
                new Control[] { _judge, Titled("층별 요약", _summary) }, new[] { 55f, 45f }));
            Body.Controls.Add(Row(Caption("박스 라벨"), _scan, strip));
            Reload();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);   // 생성자 시점엔 폼이 없다
        }

        private void OnBoxScanned(ScanEventArgs e)
        {
            string reason = _source.AddBox(e.Code);
            if (reason == null) { Reload(); return; }
            e.Accepted = false;
            BeginInvoke((Action)(() => ApcDialog.Warn(FindForm(), reason)));
        }

        private void ClosePallet()
        {
            PalletState pallet = _source.GetPallet();
            using (var dlg = new PalletCloseDialog(pallet.BoxCount, _order.TargetBoxes))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.ClosePallet(dlg.PalletLabel, dlg.EmployeeBadge, dlg.TotalKg);
                ApcDialog.Alert(FindForm(), "팔레트 " + dlg.PalletLabel + " 마감 완료 · 총중량 " + PalletRules.Kg(dlg.TotalKg) + " kg");
                _scan.Text = "";
                Reload();
            }
        }

        private void Reload()
        {
            PalletState p = _source.GetPallet();
            bool full = p.BoxCount >= _order.TargetBoxes;
            int layer = full ? PalletRules.Layers - 1 : p.BoxCount / PalletRules.CellsPerLayer;   // 0부터
            int inLayer = full ? PalletRules.CellsPerLayer : p.BoxCount % PalletRules.CellsPerLayer;

            _boardCell.Text = (layer + 1) + "층 · " + p.BoxCount + "/" + _order.TargetBoxes + " 박스";
            _board.Show(inLayer, layer * PalletRules.CellsPerLayer + 1);

            _judge.Status = StatusKind.Run;
            _judge.Text = full ? "적재 완료" : (layer + 1) + "층 · " + (inLayer + 1) + "번 칸";
            _judge.SubText = full ? "팔레트를 마감하세요" : "다음 박스 라벨을 스캔하세요";

            _summary.Rows.Clear();
            for (int i = 0; i < PalletRules.Layers; i++)
            {
                int boxes = Math.Max(0, Math.Min(PalletRules.CellsPerLayer, p.BoxCount - i * PalletRules.CellsPerLayer));
                bool done = boxes == PalletRules.CellsPerLayer;
                var state = done ? new ApcLampValue(StatusKind.Run, "완료")
                          : (i == layer ? new ApcLampValue(StatusKind.Warn, "적재 중") : new ApcLampValue(StatusKind.Stop, "대기"));
                _summary.Rows.Add(state, (i + 1) + "층", boxes > 0 ? boxes.ToString() : "",
                    p.LayerDoneAt[i].HasValue ? p.LayerDoneAt[i].Value.ToString("HH:mm:ss") : "");
            }

            _close.Enabled = p.BoxCount > 0;
            _closeNote.Text = p.BoxCount == 0 ? "박스를 쌓은 뒤 마감할 수 있습니다" : "";
        }
    }
}
