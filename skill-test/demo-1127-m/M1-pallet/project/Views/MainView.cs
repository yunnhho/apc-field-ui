using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PalletPacker.Data;

namespace PalletPacker.Views
{
    /// <summary>
    /// 팔레트 적재 메인 화면: 판정판(다음 놓을 칸 / 거부 이유) → 작업 지시 → 박스 스캔 + 마감 →
    /// 적재 도식(위에서 본 2x4) | 층별 요약. 데이터는 IMainViewSource에서만 받는다.
    /// 도식은 Label을 Panel 안에 배치해 조립한다(그리기 없음).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int JudgeHeight = 150;
        private const int CellGap = 12;
        private const int ScanWidth = 380;

        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcScanBox _scan = new ApcScanBox { Width = S(ScanWidth) };
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly Panel _pallet = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        private readonly Label[] _cells = new Label[PalletRules.CellsPerLayer];
        private readonly Panel _palletTitled;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("팔레트 적재")
        {
            _source = source;
            var order = source.GetWorkOrder();

            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = ApcTheme.GetFont(FontStyle.Bold) };
                _pallet.Controls.Add(_cells[i]);
            }
            _pallet.Resize += (s, e) => PlaceCells();

            var legend = new ApcStatusLegend { Dock = DockStyle.Top };
            legend.SetItems(new ApcLampValue(StatusKind.Run, "적재"), new ApcLampValue(StatusKind.Info, "다음 놓을 칸"), new ApcLampValue(StatusKind.Stop, "빈칸"));
            var palletBox = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            palletBox.Controls.Add(_pallet);   // Fill 먼저
            palletBox.Controls.Add(legend);
            _palletTitled = Titled("", palletBox);

            // 층별 요약은 가로로 눕힌다(층 = 열, 항목 = 행): 고정 5층이라 세로로 두면 표 아래가 빈다
            _grid.Columns.Add("item", "항목");
            for (int n = 1; n <= PalletRules.Layers; n++) _grid.Columns.Add("l" + n, n + "층");
            foreach (DataGridViewColumn c in _grid.Columns)
            {
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            _grid.Rows.Add("박스 수");
            _grid.Rows.Add("완료 시각");

            var info = new ApcInfoStrip();
            info.SetItems("품번", "고객사", "목표 박스 수");
            info.SetValue("품번", order.PartNo);
            info.SetValue("고객사", order.Customer);
            info.SetValue("목표 박스 수", order.TargetBoxes + " 박스");

            var close = Button("팔레트 마감", ApcButtonVariant.Primary, 170);
            close.Click += (s, e) => ClosePallet();
            _scan.Scanned += (s, e) => OnBoxScanned(e);

            // Top 도킹은 나중에 Add한 것이 위 → Fill 먼저, 아래 줄부터
            Body.Controls.Add(_palletTitled);
            Body.Controls.Add(Columns(TitledHeight(ApcGrid.HeightForRows(2)), new Control[] { Titled("층별 요약", _grid) }));
            Body.Controls.Add(Row(Caption("박스 라벨"), _scan, close));
            Body.Controls.Add(Columns(info.Height, new Control[] { info }));
            Body.Controls.Add(Columns(S(JudgeHeight), new Control[] { _judge }));
            Reload(null);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);   // 생성자 시점엔 폼이 없다
        }

        private void OnBoxScanned(ScanEventArgs e)
        {
            var result = _source.ScanBox(e.Code);
            e.Accepted = result.Accepted;
            Reload(result.Accepted ? null : result.Reason);
            if (!result.Accepted)
            {
                string reason = result.Reason;
                BeginInvoke(new Action(() => ApcDialog.Warn(FindForm(), "박스를 받지 않았습니다. " + reason)));
            }
        }

        private void ClosePallet()
        {
            using (var dlg = new PalletCloseDialog(_source.GetBoxCount(), _source.GetWorkOrder().TargetBoxes, 0m))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.ClosePallet(dlg.PalletLabel, dlg.EmployeeId, dlg.TotalKg);
                ApcDialog.Alert(FindForm(), "팔레트 " + dlg.PalletLabel + " 마감 완료 · " + PalletRules.FormatKg(dlg.TotalKg) + " kg");
                Reload(null);
            }
        }

        /// <summary>데이터를 다시 읽어 화면 전체를 갱신한다. rejectReason이 있으면 판정판이 거부 이유를 보인다.</summary>
        private void Reload(string rejectReason)
        {
            int total = _source.GetBoxCount();
            int target = _source.GetWorkOrder().TargetBoxes;
            bool full = total >= target;
            int layer = Math.Min(total / PalletRules.CellsPerLayer + 1, PalletRules.Layers);
            int inLayer = full ? PalletRules.CellsPerLayer : total % PalletRules.CellsPerLayer;

            _palletTitled.Text = layer + "층 · " + total + "/" + target + " 박스";
            ShowJudge(rejectReason, full, layer, inLayer);
            ShowCells(layer, inLayer, full);
            ShowLayers();
        }

        private void ShowJudge(string rejectReason, bool full, int layer, int inLayer)
        {
            if (rejectReason != null)
            {
                _judge.Status = StatusKind.Alarm; _judge.Text = "박스 거부"; _judge.SubText = rejectReason;
            }
            else if (full)
            {
                _judge.Status = StatusKind.Warn; _judge.Text = "적재 완료"; _judge.SubText = "팔레트 마감을 누르세요";
            }
            else
            {
                _judge.Status = StatusKind.Run; _judge.Text = layer + "층 · " + (inLayer + 1) + "번 칸"; _judge.SubText = "박스 라벨을 스캔하세요";
            }
        }

        private void ShowCells(int layer, int inLayer, bool full)
        {
            for (int k = 0; k < _cells.Length; k++)
            {
                StatusKind kind = k < inLayer ? StatusKind.Run : (k == inLayer && !full ? StatusKind.Info : StatusKind.Stop);
                int seq = (layer - 1) * PalletRules.CellsPerLayer + k + 1;
                _cells[k].BackColor = ApcTheme.Status(kind);
                _cells[k].ForeColor = ApcTheme.C(kind == StatusKind.Stop ? ApcTheme.Current.Colors.TextMuted : ApcTheme.Current.Colors.TextInverse);
                _cells[k].Text = kind == StatusKind.Stop ? "" : seq.ToString();
            }
        }

        private void ShowLayers()
        {
            foreach (LayerInfo l in _source.GetLayers())
            {
                _grid.Rows[0].Cells[l.Number].Value = l.BoxCount + " / " + PalletRules.CellsPerLayer;
                _grid.Rows[1].Cells[l.Number].Value = l.CompletedAt.HasValue ? l.CompletedAt.Value.ToString("HH:mm:ss") : "";
            }
        }

        // 위에서 본 팔레트: 4열 x 2행을 영역에 맞춰 나눈다
        private void PlaceCells()
        {
            int gap = S(CellGap);
            int cols = PalletRules.CellColumns, rows = PalletRules.CellsPerLayer / cols;
            int w = (_pallet.ClientSize.Width - gap * (cols - 1)) / cols;
            int h = (_pallet.ClientSize.Height - gap * (rows - 1)) / rows;
            if (w <= 0 || h <= 0) return;
            for (int k = 0; k < _cells.Length; k++)
                _cells[k].SetBounds((k % cols) * (w + gap), (k / cols) * (h + gap), w, h);
        }
    }
}
