using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using RawMaterialIssueTerminal.Data;

namespace RawMaterialIssueTerminal.Views
{
    /// <summary>
    /// 원료 창고 출고 단말: 작업지시 스캔 → 원료 포대 스캔 → 판정(OK/NG·사유) → 원료별 출고 목록·Lot 재고 갱신.
    /// 판정은 IssueRules 한 곳. NG는 화면이 막고(출고 안 됨) 사유를 판정판에 쓴다.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private readonly IMainViewSource _source;
        private readonly ApcScanBox _scan = new ApcScanBox { Width = ApcTheme.Px(300) };
        private readonly ApcInfoStrip _info = new ApcInfoStrip { Dock = DockStyle.None };
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcValueCard _metCard = new ApcValueCard { Title = "충족 원료", Unit = "종", Dock = DockStyle.Fill };
        private readonly ApcGrid _materialGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false };
        private readonly ApcGrid _lotGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _historyGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcButton _doneButton = Button("출고 완료", ApcButtonVariant.Primary, 150);
        private readonly ApcButton _fifoButton = Button("FIFO 예외 승인", ApcButtonVariant.Normal, 170);
        private readonly Label _doneReason = Caption("");
        private readonly Label _fifoReason = Caption("");

        private OrderInfo _order;
        private IList<MaterialLine> _materials = new List<MaterialLine>();
        private ScanResult _last;          // 마지막 NG 스캔(승인 대상 판단용). OK 스캔이 오면 지운다.
        private string _lastLot = "";      // 재고 표에서 굵게 보일 마지막 스캔 Lot

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("원료 출고 단말")
        {
            _source = source;
            _scan.Scanned += (s, e) => e.Accepted = HandleScan(e.Code.Trim());
            _info.SetItems("지시", "제품", "배치 수", "투입 라인", "알레르기 라인");
            _info.Width = S(1380);
            _doneButton.Click += (s, e) => CompleteOrder();
            _fifoButton.Click += (s, e) => ApproveException();
            _materialGrid.SelectionChanged += (s, e) => ShowLots();

            BuildGrids();
            Body.Controls.Add(Titled("금일 출고 이력", _historyGrid));
            Body.Controls.Add(Columns(TitledHeight(ApcGrid.HeightForRows(6), true), new Control[]
            {
                Titled("원료별 출고 목록", _materialGrid, _doneReason, _doneButton),
                Titled("선택 원료 재고 Lot", _lotGrid, _fifoReason, _fifoButton),
            }, new[] { 56f, 44f }));
            Body.Controls.Add(Row(Caption("스캔"), _scan, _info));
            Body.Controls.Add(Columns(S(150), new Control[] { _judge, _metCard }, new[] { 70f, 30f }));

            ShowReady("작업지시 바코드를 스캔하세요");
            Reload();
            foreach (string code in source.StartupScans) HandleScan(code);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);   // 생성자 시점엔 폼이 없다
        }

        // ---- 스캔 처리 ----

        private bool HandleScan(string code)
        {
            if (code.StartsWith("WO-", StringComparison.Ordinal)) return LoadOrder(code);
            if (_order == null) return Reject("작업지시 바코드를 먼저 스캔하세요.");
            BagLabel bag = BagLabel.TryParse(code);
            if (bag == null) return Reject("읽을 수 없는 라벨입니다. 원료 포대 라벨을 다시 스캔하세요.");

            ScanResult r = IssueRules.Judge(_order, _materials, _source.GetLots(bag.MaterialCode), bag, _source.Today);
            if (!r.IsOk)
            {
                _last = r;
                if (r.Material != null) SelectMaterial(r.Material.Code);
                ShowNg(r);
                Reload();
                return false;
            }
            Issue(r, "");
            return true;
        }

        private bool LoadOrder(string code)
        {
            OrderInfo o = _source.FindOrder(code);
            if (o == null) return Reject("등록되지 않은 작업지시입니다.");
            _order = o;
            _last = null;
            _lastLot = "";
            ShowReady("원료 포대 라벨을 스캔하세요");
            Reload();
            if (_materialGrid.Rows.Count > 0) _materialGrid.CurrentCell = _materialGrid.Rows[0].Cells[0];
            return true;
        }

        private bool Reject(string message)
        {
            // 스캔 이벤트 안에서 바로 띄우지 않고 처리 후에 보인다
            if (IsHandleCreated) BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(FindForm(), message)));
            return false;
        }

        private void Issue(ScanResult r, string note)
        {
            MaterialLine before = r.Material;
            _source.RecordIssue(_order.Code, r.Bag, note);
            _last = null;
            _lastLot = r.Bag.Lot;
            Reload();
            SelectMaterial(before.Code);
            ShowIssued(r, note);
        }

        private void ApproveException()
        {
            ScanResult r = _last;
            if (r == null || !r.CanApprove) return;   // 승인 불가 NG는 버튼이 꺼져 있다
            string name = r.Material.Code + " " + r.Material.Name;
            using (var dlg = new FifoExceptionDialog(name, r.Lot.Lot, r.Lot.ExpiresOn, r.FrontLot.Lot, r.FrontLot.ExpiresOn,
                _source.Today, _source.FindApproverName))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                Issue(r, "FIFO 예외 승인 · " + dlg.ApproverCode);
            }
        }

        private void CompleteOrder()
        {
            if (_order == null || !_materials.All(IssueRules.IsSatisfied)) return;   // 버튼이 꺼져 있다
            if (!ApcDialog.Confirm(FindForm(), _order.Code + " 출고를 완료합니다.", "출고 완료", ApcButtonVariant.Primary)) return;
            _source.CompleteOrder(_order.Code);
            _order = null;
            _last = null;
            ShowReady("출고를 완료했습니다. 다음 작업지시를 스캔하세요");
            Reload();
        }

        // ---- 판정판 ----

        private void ShowReady(string guide)
        {
            _judge.Text = "READY";
            _judge.Status = StatusKind.Stop;
            _judge.SubText = guide;
        }

        private void ShowNg(ScanResult r)
        {
            _judge.Text = "NG · " + IssueRules.ReasonText(r.Verdict);
            _judge.Status = StatusKind.Alarm;
            _judge.SubText = NgDetail(r);
        }

        private string NgDetail(ScanResult r)
        {
            string name = r.Material == null ? r.Bag.MaterialCode : r.Material.Name;
            switch (r.Verdict)
            {
                case ScanVerdict.FifoViolation: return name + " " + r.Lot.Lot + " · 앞 순위 " + r.FrontLot.Lot + " 먼저 출고";
                case ScanVerdict.Expired: return name + " " + r.Lot.Lot + " · 유통기한 " + r.Lot.ExpiresOn.ToString("yyyy-MM-dd");
                case ScanVerdict.AllergenMismatch: return name + " " + r.Lot.Lot + " · 보관 " + r.Lot.AllergenLine + " ≠ 지시 " + _order.AllergenLine;
                case ScanVerdict.NotInOrder: return r.Bag.MaterialCode + " · " + _order.Code + " 지시서에 없음";
                default: return r.Bag.Lot + " · " + r.Bag.MaterialCode + " 재고에 없음";
            }
        }

        private void ShowIssued(ScanResult r, string note)
        {
            MaterialLine now = _materials.First(m => m.Code == r.Material.Code);
            bool over = IssueRules.StateOf(now) == IssueState.Over;
            string weight = r.Bag.Weight.ToString("0.0", CultureInfo.InvariantCulture) + " kg 출고";
            string head = r.Material.Name + " " + r.Bag.Lot;
            if (over)
            {
                _judge.Text = "OK · 초과";
                _judge.Status = StatusKind.Warn;
                _judge.SubText = head + " · 필요량 +" + (now.Issued - now.Required).ToString("0.0", CultureInfo.InvariantCulture) + " kg";
            }
            else if (r.NearExpiry)
            {
                _judge.Text = "OK · 유통기한 주의";
                _judge.Status = StatusKind.Warn;
                _judge.SubText = head + " · D-" + IssueRules.DaysLeft(r.Lot, _source.Today);
            }
            else
            {
                _judge.Text = note.Length > 0 ? "OK · 예외 승인" : "OK";
                _judge.Status = StatusKind.Run;
                _judge.SubText = head + " · " + weight;
            }
        }

        // ---- 표 ----

        private void BuildGrids()
        {
            AddColumns(_materialGrid, "원료 코드", "원료명", "필요량(kg)", "출고량(kg)", "남은 양(kg)");
            _materialGrid.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            AddColumns(_lotGrid, "FIFO 순위", "Lot", "입고일", "유통기한", "잔량(kg)", "알레르기 라인");
            _lotGrid.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            _historyGrid.Columns.Add(new ApcLampColumn());
            AddColumns(_historyGrid, "시각", "지시 번호", "원료명", "Lot", "출고량(kg)", "판정");
            DataGridViewColumn verdict = _historyGrid.Columns[_historyGrid.Columns.Count - 1];
            foreach (DataGridViewColumn c in _historyGrid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            verdict.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            verdict.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            foreach (ApcGrid g in new[] { _materialGrid, _lotGrid })
                foreach (DataGridViewColumn c in g.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private static void AddColumns(ApcGrid grid, params string[] headers)
        {
            foreach (string h in headers) grid.Columns.Add(h, h);
        }

        private void Reload()
        {
            _materials = _order == null ? new List<MaterialLine>() : _source.GetMaterials(_order.Code);
            ShowOrderInfo();
            ShowMaterials();
            ShowLots();
            ShowHistory();
            ShowActions();
        }

        private void ShowOrderInfo()
        {
            _info.SetValue("지시", _order == null ? "" : _order.Code);
            _info.SetValue("제품", _order == null ? "" : _order.Product);
            _info.SetValue("배치 수", _order == null ? "" : _order.Batches.ToString("#,0", CultureInfo.InvariantCulture));
            _info.SetValue("투입 라인", _order == null ? "" : _order.Line);
            _info.SetValue("알레르기 라인", _order == null ? "" : _order.AllergenLine);
        }

        private void ShowMaterials()
        {
            int keep = _materialGrid.CurrentRow == null ? 0 : _materialGrid.CurrentRow.Index;
            _materialGrid.Rows.Clear();
            foreach (MaterialLine m in _materials)
            {
                IssueState st = IssueRules.StateOf(m);
                _materialGrid.Rows.Add(m.Code, m.Name, Kg(m.Required), Kg(m.Issued), Kg(m.Remaining), StateLamp(st));
            }
            if (_materialGrid.Rows.Count > 0) _materialGrid.CurrentCell = _materialGrid.Rows[Math.Min(keep, _materialGrid.Rows.Count - 1)].Cells[0];
            int met = _materials.Count(IssueRules.IsSatisfied);
            _metCard.Value = _order == null ? "" : met.ToString(CultureInfo.InvariantCulture);
            _metCard.Footer = _order == null ? "" : "전체 " + _materials.Count + "종";
            _metCard.Status = _order == null ? StatusKind.Stop : StatusKind.Run;
        }

        private static ApcLampValue StateLamp(IssueState st)
        {
            switch (st)
            {
                case IssueState.Waiting: return new ApcLampValue(StatusKind.Stop, "대기");
                case IssueState.InProgress: return new ApcLampValue(StatusKind.Run, "출고 중");
                case IssueState.Met: return new ApcLampValue(StatusKind.Run, "충족");
                default: return new ApcLampValue(StatusKind.Warn, "초과");
            }
        }

        private void SelectMaterial(string code)
        {
            for (int i = 0; i < _materials.Count; i++)
                if (_materials[i].Code == code) { _materialGrid.CurrentCell = _materialGrid.Rows[i].Cells[0]; return; }
        }

        private void ShowLots()
        {
            _lotGrid.Rows.Clear();
            if (_order == null || _materialGrid.CurrentRow == null || _materialGrid.CurrentRow.Index >= _materials.Count) return;
            MaterialLine m = _materials[_materialGrid.CurrentRow.Index];
            IList<LotStock> lots = _source.GetLots(m.Code);
            List<LotStock> queue = IssueRules.FifoQueue(lots, m, _order, _source.Today);
            var rest = lots.Except(queue).OrderBy(l => l.ExpiresOn).ThenBy(l => l.Lot, StringComparer.Ordinal);
            foreach (LotStock l in queue.Concat(rest))
            {
                int rank = queue.IndexOf(l) + 1;
                int row = _lotGrid.Rows.Add(rank > 0 ? rank.ToString(CultureInfo.InvariantCulture) : "", l.Lot, l.ReceivedOn.ToString("yyyy-MM-dd"),
                    l.ExpiresOn.ToString("yyyy-MM-dd"), Kg(l.Remaining), l.AllergenLine, LotLamp(l, m));
                if (l.Lot == _lastLot) _lotGrid.Rows[row].Cells[1].Style.Font = ApcTheme.GetFont(FontStyle.Bold);
            }
        }

        // 재고 Lot 상태: 경과 > 소진 > 라인 다름 > 7일 이내 주의 > 정상
        private ApcLampValue LotLamp(LotStock l, MaterialLine m)
        {
            if (IssueRules.IsExpired(l, _source.Today)) return new ApcLampValue(StatusKind.Alarm, "경과");
            if (l.Remaining <= 0) return new ApcLampValue(StatusKind.Stop, "소진");
            if (m.Allergen.Length > 0 && l.AllergenLine != _order.AllergenLine) return new ApcLampValue(StatusKind.Stop, "라인 다름");
            int days = IssueRules.DaysLeft(l, _source.Today);
            if (days <= IssueRules.NearExpiryDays) return new ApcLampValue(StatusKind.Warn, "D-" + days + " 주의");
            return new ApcLampValue(StatusKind.Run, "정상");
        }

        private void ShowHistory()
        {
            _historyGrid.Rows.Clear();
            foreach (IssueRecord h in _source.GetTodayHistory())
                _historyGrid.Rows.Add(h.Note.Length > 0 ? StatusKind.Warn : StatusKind.Run, h.Time.ToString("HH:mm:ss"), h.OrderCode, h.MaterialName,
                    h.Lot, Kg(h.Weight), h.Note.Length > 0 ? "OK · " + h.Note : "OK");
        }

        // ---- 버튼 ----

        private void ShowActions()
        {
            int unmet = _materials.Count(m => !IssueRules.IsSatisfied(m));
            _doneButton.Enabled = _order != null && unmet == 0;
            _doneReason.Text = _order == null ? "작업지시 스캔 전" : unmet > 0 ? "필요량 미충족 " + unmet + "종" : "";

            bool pending = _last != null && _last.CanApprove;
            _fifoButton.Enabled = pending;
            _fifoReason.Text = _last == null || pending ? ""
                : _last.Verdict == ScanVerdict.Expired ? "유통기한 경과 · 승인 불가" : "승인으로 출고할 수 없는 NG";
        }

        private static string Kg(double v) { return v.ToString("0.0", CultureInfo.InvariantCulture); }
    }
}
