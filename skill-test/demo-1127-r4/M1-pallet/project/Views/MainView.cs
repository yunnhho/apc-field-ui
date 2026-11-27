using System;
using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PalletPacker.Data;

namespace PalletPacker.Views
{
    /// <summary>
    /// 메인 화면: 스캔 + 작업 지시 → 판정판(다음 칸 / 스캔 결과) → 마감 버튼 → 현재 층 도식 · 스캔 이력 → 층별 요약.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private readonly IMainViewSource _source;
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly ApcInfoStrip _order = new ApcInfoStrip();
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcButton _close = Button("팔레트 마감", ApcButtonVariant.Primary, 200);
        private readonly Label _closeReason = Caption("");
        private readonly PalletBoard _board = new PalletBoard { Dock = DockStyle.Fill };
        private readonly Panel _boardCell;
        private readonly ApcGrid _history = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _layers = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("팔레트 적재")
        {
            _source = source;
            WorkOrder wo = source.GetWorkOrder();

            _order.SetItems("품번", "고객사", "목표(박스)");
            _order.SetValue("품번", wo.PartNo);
            _order.SetValue("고객사", wo.Customer);
            _order.SetValue("목표(박스)", wo.TargetBoxes.ToString());
            _order.Width = Math.Max(_order.PreferredWidth, S(520));
            _scan.Width = S(340);
            _scan.Accepts = c => c.StartsWith("BX-");
            _judge.Margin = new Padding(0, 0, 0, 0);

            _history.Columns.Add(new ApcLampColumn());
            _history.Columns.Add("label", "박스 라벨");
            _history.Columns.Add("time", "시각");
            _history.Columns.Add("pos", "위치");
            _history.Columns.Add("result", "내용");
            foreach (DataGridViewColumn c in _history.Columns) c.SortMode = DataGridViewColumnSortMode.NotSortable;
            _history.Columns["result"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _history.Columns["result"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            _history.Columns["label"].Width = S(190);
            _history.Columns["time"].Width = S(100);
            _history.Columns["pos"].Width = S(90);

            // 층별 요약: 층을 열로 눕힌 2행 표(5층 표를 Fill 옆에 두면 행이 지나치게 커진다)
            _layers.Columns.Add("item", "항목");
            for (int i = 1; i <= PalletRules.Layers; i++) _layers.Columns.Add("l" + i, i + "층");
            foreach (DataGridViewColumn c in _layers.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "적재"), new ApcLampValue(StatusKind.Warn, "다음 칸"), new ApcLampValue(StatusKind.Stop, "빈칸"));
            _boardCell = Titled("", _board, legend);

            Reload();
            SetJudgeNext();

            // Fill 먼저, 아래 줄(Bottom) 다음, Top 줄은 아래쪽 것부터
            Body.Controls.Add(FillColumns(new Control[] { _boardCell, Titled("스캔 이력", _history) }, new[] { 60f, 40f }));
            var layerRow = Columns(TitledHeight(ApcGrid.HeightForRows(2)), new Control[] { Titled("층별 요약", _layers) });
            layerRow.Dock = DockStyle.Bottom;
            Body.Controls.Add(layerRow);
            Body.Controls.Add(Row(_close, _closeReason));
            Body.Controls.Add(Columns(S(150), new Control[] { _judge }));
            Body.Controls.Add(Row(Caption("박스 라벨"), _scan, _order));

            _scan.Scanned += OnScanned;
            _close.Click += (s, e) => ClosePallet();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);
        }

        private void OnScanned(object sender, ScanEventArgs e)
        {
            string reason;
            bool ok = _source.ScanBox(e.Code, out reason);
            e.Accepted = ok;
            Reload();
            if (ok)
            {
                IList<BoxRecord> boxes = _source.GetBoxes();
                BoxRecord last = boxes[boxes.Count - 1];
                _judge.Text = "OK";
                _judge.Status = StatusKind.Run;
                _judge.SubText = boxes.Count >= PalletRules.TargetBoxes ? last.Layer + "층 " + last.Cell + "번 적재 · 마감 필요"
                    : last.Layer + "층 " + last.Cell + "번 적재 · 다음 " + NextPosition();
                return;
            }
            _judge.Text = "NG";
            _judge.Status = StatusKind.Alarm;
            _judge.SubText = reason;
            BeginInvoke(new Action(() => ApcDialog.Warn(FindForm(), reason)));   // 스캔 이벤트가 끝난 뒤 경고 상자
        }

        private string NextPosition()
        {
            int count = _source.GetBoxes().Count;
            return (count / PalletRules.CellsPerLayer + 1) + "층 " + (count % PalletRules.CellsPerLayer + 1) + "번";
        }

        private void SetJudgeNext()
        {
            bool full = _source.GetBoxes().Count >= PalletRules.TargetBoxes;
            _judge.Status = StatusKind.Run;
            _judge.Text = full ? "적재 완료" : "다음 " + NextPosition();
            _judge.SubText = full ? "팔레트 마감 필요" : "박스 라벨 스캔 대기";
        }

        private void ClosePallet()
        {
            IList<BoxRecord> boxes = _source.GetBoxes();
            using (var dlg = new PalletCloseDialog(boxes.Count, _source.GetWorkOrder().TargetBoxes))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.ClosePallet(dlg.PalletLabel, dlg.BadgeCode, dlg.TotalKg);
                Reload();
                SetJudgeNext();
                ApcDialog.Alert(FindForm(), "팔레트 " + dlg.PalletLabel + " 마감 완료 · " + dlg.TotalKg.ToString("0.0") + " kg");
            }
        }

        /// <summary>데이터 인터페이스 값으로 도식·표·제목·마감 버튼을 다시 채운다.</summary>
        private void Reload()
        {
            IList<BoxRecord> boxes = _source.GetBoxes();
            int target = _source.GetWorkOrder().TargetBoxes;
            _board.Show(boxes);
            _boardCell.Text = PalletBoard.CurrentLayer(boxes.Count) + "층 · " + boxes.Count + "/" + target + " 박스";

            _history.Rows.Clear();
            IList<ScanEntry> log = _source.GetScanLog();
            for (int i = log.Count - 1; i >= 0; i--)
            {
                ScanEntry s = log[i];
                _history.Rows.Add(s.Accepted ? StatusKind.Run : StatusKind.Alarm, s.Label, s.At.ToString("HH:mm:ss"), s.Position,
                    s.Accepted ? "적재" : "거부 · " + s.Reason);
            }

            _layers.Rows.Clear();
            var counts = new object[PalletRules.Layers + 1];
            var times = new object[PalletRules.Layers + 1];
            counts[0] = "박스 수(개)"; times[0] = "완료 시각";
            for (int layer = 1; layer <= PalletRules.Layers; layer++)
            {
                int n = 0; string done = "";
                foreach (BoxRecord b in boxes)
                {
                    if (b.Layer != layer) continue;
                    n++;
                    if (b.Cell == PalletRules.CellsPerLayer) done = b.At.ToString("HH:mm:ss");
                }
                counts[layer] = n == 0 ? "" : n.ToString();   // 아직 안 쌓은 층은 빈 값
                times[layer] = done;
            }
            _layers.Rows.Add(counts);
            _layers.Rows.Add(times);
            foreach (DataGridViewColumn c in _layers.Columns) c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            bool ready = boxes.Count >= target;
            _close.Enabled = ready;
            _closeReason.Text = ready ? "" : "마감 불가 · 박스 " + (target - boxes.Count) + "개 부족 (" + boxes.Count + "/" + target + ")";
        }
    }
}
