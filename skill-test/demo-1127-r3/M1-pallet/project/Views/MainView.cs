using System;
using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PalletPacker.Data;

namespace PalletPacker.Views
{
    /// <summary>
    /// 팔레트 적재: 스캔 줄 → 판정판(다음 칸 / NG 사유) → 마감 버튼 → 작업 지시 → 현재 층 도식 + 층별 요약.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private readonly IMainViewSource _source;
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcInfoStrip _order = new ApcInfoStrip();
        private readonly PalletBoard _board = new PalletBoard { Dock = DockStyle.Fill };
        private readonly ApcGrid _floors = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, FillRows = true };
        private readonly Panel _boardCell;
        private readonly ApcButton _close = Button("팔레트 마감", ApcButtonVariant.Primary, 180);
        private readonly Label _closeReason = Caption("");

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("팔레트 적재")
        {
            _source = source;
            _scan.Width = S(360);

            _floors.Columns.Add("floor", "층");
            _floors.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            _floors.Columns.Add("boxes", "박스 수");
            _floors.Columns.Add("done", "완료 시각");
            foreach (DataGridViewColumn c in _floors.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            _order.SetItems("품번", "고객사", "목표 박스 수");

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "적재"), new ApcLampValue(StatusKind.Warn, "다음 칸"), new ApcLampValue(StatusKind.Stop, "빈칸"));
            _boardCell = Titled("", _board, legend);

            _judge.Margin = new Padding(0);

            // Fill 먼저, Top 줄은 아래쪽부터
            Body.Controls.Add(FillColumns(new Control[] { _boardCell, Titled("층별 요약", _floors) }, new[] { 60f, 40f }));
            Body.Controls.Add(Columns(_order.Height, new Control[] { _order }));
            var closeRow = Row(_closeReason, _close);
            Body.Controls.Add(closeRow);
            Body.Controls.Add(Columns(S(170), new Control[] { _judge }));
            Body.Controls.Add(Row(Caption("박스 라벨"), _scan));

            Reload(null);
            _close.Click += (s, e) => ClosePallet();
            _scan.Scanned += (s, e) => OnScanned(e);   // 이벤트는 맨 마지막에 구독
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);
        }

        private void OnScanned(ScanEventArgs e)
        {
            string reason = _source.PlaceBox(e.Code);
            e.Accepted = reason == null;
            Reload(reason);
            if (reason != null) BeginInvoke(new Action(() => ApcDialog.Warn(FindForm(), reason)));
        }

        private void ClosePallet()
        {
            WorkOrder order = _source.GetWorkOrder();
            int count = _source.GetPlacedBoxes().Count;
            if (count < order.TargetBoxes) return;   // 버튼이 꺼져 있지만 한 번 더 막는다
            using (var dlg = new PalletCloseDialog(count, order.TargetBoxes))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.ClosePallet(dlg.PalletLabelCode, dlg.BadgeCode, dlg.TotalKg);
            }
            Reload(null);
            ApcDialog.Alert(FindForm(), "팔레트를 마감했습니다.");
        }

        /// <summary>화면 전체를 source 값으로 다시 채운다. rejectReason != null이면 판정판에 NG 사유.</summary>
        private void Reload(string rejectReason)
        {
            WorkOrder order = _source.GetWorkOrder();
            IList<PlacedBox> boxes = _source.GetPlacedBoxes();
            int count = boxes.Count, target = order.TargetBoxes;
            bool full = count >= target;
            int floor = full ? PalletRules.Floors : count / PalletRules.BoxesPerFloor + 1;
            int inFloor = full ? PalletRules.BoxesPerFloor : count % PalletRules.BoxesPerFloor;
            int firstNumber = (floor - 1) * PalletRules.BoxesPerFloor + 1;

            _order.SetValue("품번", order.PartNo);
            _order.SetValue("고객사", order.Customer);
            _order.SetValue("목표 박스 수", target.ToString());

            _boardCell.Text = floor + "층 · " + count + "/" + target + " 박스";
            _board.Show(firstNumber, inFloor);
            ShowFloors(boxes, floor);
            ShowJudge(rejectReason, full, floor, firstNumber + inFloor);

            _close.Enabled = full;
            _closeReason.Text = full ? "" : "박스 " + count + "/" + target + " · 마감 불가";
        }

        private void ShowJudge(string rejectReason, bool full, int floor, int nextNumber)
        {
            if (rejectReason != null) { _judge.Text = "NG"; _judge.Status = StatusKind.Alarm; _judge.SubText = rejectReason; }
            else if (full) { _judge.Text = "적재 완료"; _judge.Status = StatusKind.Run; _judge.SubText = "팔레트를 마감하세요"; }
            else { _judge.Text = floor + "층 · " + nextNumber + "번 칸"; _judge.Status = StatusKind.Run; _judge.SubText = "박스 라벨을 스캔하세요"; }
        }

        private void ShowFloors(IList<PlacedBox> boxes, int currentFloor)
        {
            _floors.Rows.Clear();
            for (int f = 1; f <= PalletRules.Floors; f++)
            {
                int placed = Math.Max(0, Math.Min(PalletRules.BoxesPerFloor, boxes.Count - (f - 1) * PalletRules.BoxesPerFloor));
                bool done = placed == PalletRules.BoxesPerFloor;
                bool active = !done && f == currentFloor;
                var state = done ? new ApcLampValue(StatusKind.Run, "완료")
                          : active ? new ApcLampValue(StatusKind.Warn, "적재 중")
                          : new ApcLampValue(StatusKind.Stop, "대기");
                string finished = done ? boxes[f * PalletRules.BoxesPerFloor - 1].Time.ToString("HH:mm:ss") : "";
                _floors.Rows.Add(f + "층", state, placed > 0 ? placed.ToString() : "", finished);
            }
        }
    }
}
