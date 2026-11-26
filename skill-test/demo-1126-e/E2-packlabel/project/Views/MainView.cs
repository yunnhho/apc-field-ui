using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using ShipPackVerifier.Data;

namespace ShipPackVerifier.Views
{
    /// <summary>출하 포장 검수 메인 화면. 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcValueCard _boxCard = Card("현재 박스");
        private readonly ApcValueCard _orderCard = Card("완료 박스");
        private readonly ApcInfoStrip _info = new ApcInfoStrip();
        private readonly ApcScanBox _scan = new ApcScanBox { Width = 380 };
        private readonly ApcGrid _serialGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _boxGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcButton _print = Button("박스 라벨 출력", ApcButtonVariant.Primary, 200);
        private readonly ApcButton _reprint = Button("라벨 재출력", ApcButtonVariant.Normal, 160);
        private readonly Label _printReason = Caption("");
        private Panel _serialPanel;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("출하 포장 검수")
        {
            _source = source;
            _scan.Width = S(380);
            _judge.Margin = new Padding(0, 0, S(12), 0);

            _info.SetItems("지시 번호", "고객사", "납품처", "품번", "지시 수량(EA)", "박스 입수(EA)");
            AddColumns(_serialGrid, "순번", "시리얼", "스캔 시각");
            AddColumns(_boxGrid, "박스 번호", "수량(EA)", "라벨 출력 시각");

            _scan.Scanned += OnScanned;
            _print.Click += (s, e) => PrintLabel();
            _reprint.Click += (s, e) => Reprint();

            _serialPanel = Titled("현재 박스 담은 시리얼", _serialGrid, _printReason, _print);
            var boxPanel = Titled("완료 박스", _boxGrid, _reprint);

            // Top 도킹은 나중에 Add한 것이 위 → 목록(Fill) 먼저, 아래 줄부터
            Body.Controls.Add(FillColumns(new Control[] { _serialPanel, boxPanel }, new[] { 55f, 45f }));
            Body.Controls.Add(Row(Caption("시리얼 스캔"), _scan));
            Body.Controls.Add(Columns(_info.Height, new Control[] { _info }));
            Body.Controls.Add(Columns(S(190), new Control[] { _judge, _boxCard, _orderCard }, new[] { 50f, 25f, 25f }));
            Reload();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);   // 생성자 시점엔 폼이 없다
        }

        private void OnScanned(object sender, ScanEventArgs e)
        {
            ScanResult result = _source.Scan(e.Code);
            e.Accepted = result.Ok;
            Reload();
            if (result.Ok) return;
            string message = result.Reason + "\n" + e.Code;
            BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(FindForm(), message)));
        }

        private void PrintLabel()
        {
            if (!_source.PrintCurrentLabel())
            {
                ApcDialog.Warn(FindForm(), "라벨을 출력할 수 없습니다: " + _source.GetCurrentBox().PrintBlockedReason);
                return;
            }
            Reload();
        }

        private void Reprint()
        {
            IList<PackedBox> boxes = _source.GetCompletedBoxes();
            if (boxes.Count == 0) return;
            using (var dlg = new ReprintDialog(boxes, _source.FindSupervisor))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (_source.ReprintLabel(dlg.BoxNo, dlg.Reason, dlg.SupervisorBadge))
                    ApcDialog.Alert(FindForm(), dlg.BoxNo + " 라벨을 재출력했습니다 (" + dlg.Reason + ")");
                else
                    ApcDialog.Warn(FindForm(), "재출력할 수 없습니다: 반장 확인 실패");
            }
        }

        /// <summary>데이터 쪽 상태를 화면 전체에 다시 그린다.</summary>
        private void Reload()
        {
            ShipOrder order = _source.GetOrder();
            BoxProgress box = _source.GetCurrentBox();
            IList<PackedBox> done = _source.GetCompletedBoxes();

            _info.SetValue("지시 번호", order.OrderNo);
            _info.SetValue("고객사", order.Customer);
            _info.SetValue("납품처", order.Destination);
            _info.SetValue("품번", order.PartNo);
            _info.SetValue("지시 수량(EA)", order.Quantity.ToString("#,0", CultureInfo.InvariantCulture));
            _info.SetValue("박스 입수(EA)", order.BoxCapacity.ToString("#,0", CultureInfo.InvariantCulture));

            ShowJudge();
            ShowCards(order, box, done);
            _print.Enabled = box.CanPrint;
            _printReason.Text = box.CanPrint ? "" : box.PrintBlockedReason;
            _serialPanel.Text = "현재 박스 " + ShortBox(box.BoxNo) + " 담은 시리얼";
            FillSerials();
            FillBoxes(done);
        }

        private void ShowJudge()
        {
            string serial;
            ScanResult last = _source.GetLastScan(out serial);
            if (last == null)
            {
                _judge.Status = StatusKind.Stop; _judge.Text = "READY"; _judge.SubText = "시리얼을 스캔하세요";
                return;
            }
            _judge.Status = last.Ok ? StatusKind.Run : StatusKind.Alarm;
            _judge.Text = last.Ok ? "OK" : "NG";
            _judge.SubText = last.Ok ? serial : last.Reason + " · " + serial;
        }

        private void ShowCards(ShipOrder order, BoxProgress box, IList<PackedBox> done)
        {
            int doneQty = 0;
            foreach (PackedBox b in done) doneQty += b.Quantity;
            _boxCard.Title = box.OrderDone ? "현재 박스" : "현재 박스 " + ShortBox(box.BoxNo);
            _boxCard.Value = box.OrderDone ? "" : box.Count.ToString(CultureInfo.InvariantCulture);
            _boxCard.Unit = box.OrderDone ? "" : "/ " + box.Capacity + " EA";
            _boxCard.Footer = box.OrderDone ? "지시 완료" : box.CanPrint ? "박스 가득 · 라벨 출력" : (box.Capacity - box.Count) + " EA 더 담기";
            _boxCard.Status = box.OrderDone ? StatusKind.Stop : StatusKind.Run;
            _orderCard.Value = done.Count.ToString(CultureInfo.InvariantCulture);
            _orderCard.Unit = "/ " + order.TotalBoxes + " 박스";
            _orderCard.Footer = "누계 " + doneQty.ToString("#,0", CultureInfo.InvariantCulture) + " / " + order.Quantity.ToString("#,0", CultureInfo.InvariantCulture) + " EA";
            _orderCard.Status = StatusKind.Run;
        }

        private void FillSerials()
        {
            _serialGrid.Rows.Clear();
            foreach (PackedSerial s in _source.GetCurrentSerials())
                _serialGrid.Rows.Add(s.No, s.Serial, s.ScannedAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
        }

        private void FillBoxes(IList<PackedBox> done)
        {
            _boxGrid.Rows.Clear();
            foreach (PackedBox b in done)
                _boxGrid.Rows.Add(b.BoxNo, b.Quantity.ToString("#,0", CultureInfo.InvariantCulture), b.LabelPrintedAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
        }

        private static string ShortBox(string boxNo) { return boxNo.Substring(boxNo.Length - 3); }

        private static void AddColumns(ApcGrid grid, params string[] headers)
        {
            for (int i = 0; i < headers.Length; i++) grid.Columns.Add("c" + i, headers[i]);
            foreach (DataGridViewColumn c in grid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private static ApcValueCard Card(string title)
        {
            return new ApcValueCard { Title = title, Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        }
    }
}
