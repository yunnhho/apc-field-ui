using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using RawMaterialIssueTerminal.Data;

namespace RawMaterialIssueTerminal.Views
{
    /// <summary>
    /// 원료 출고 단말. 작업지시 스캔 → 원료 포대(Lot) 스캔 → 판정(OK/NG) → 출고량 누적.
    /// 스캔은 폼 어디에 포커스가 있든 받는다. WO- 로 시작하면 작업지시, 그 외는 Lot 라벨.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const string WorkOrderPrefix = "WO-";
        private const int MaterialRows = 5, LotRows = 5;

        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill, Text = "READY", SubText = "작업지시서 바코드를 스캔하세요", Status = StatusKind.Stop };
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly ApcInfoStrip _info = new ApcInfoStrip();
        private readonly ApcGrid _materials = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false };
        private readonly ApcGrid _lots = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _history = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcButton _finish = Button("출고 완료", ApcButtonVariant.Primary, 150);
        private readonly ApcButton _fifoException = Button("FIFO 예외 승인", ApcButtonVariant.Danger, 170);
        private readonly Label _finishReason = Caption("");
        private readonly Label _fifoReason = Caption("");
        private readonly Panel _lotPanel;

        private WorkOrder _order;
        private IDictionary<string, double> _issued = new Dictionary<string, double>();
        private ScanJudge.Verdict _pendingFifo;   // 직전 스캔이 FIFO 위반일 때만 예외 승인 가능

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("원료 출고 단말")
        {
            _source = source;
            _scan.Width = S(340);
            _scan.Scanned += (s, e) => { HandleScan(e.Code); };
            _info.Dock = DockStyle.None;
            _info.Width = S(980);
            _info.SetItems("지시 번호", "제품", "배합 배치 수", "투입 라인");

            BuildMaterialGrid();
            BuildLotGrid();
            BuildHistoryGrid();
            _materials.SelectionChanged += (s, e) => ShowLots();
            _finish.Click += (s, e) => FinishOrder();
            _fifoException.Click += (s, e) => ApproveFifoException();

            _lotPanel = Titled("재고 Lot (선택 원료)", _lots, _fifoReason, _fifoException);
            int tableH = TitledHeight(ApcGrid.HeightForRows(MaterialRows), true);
            // Top 도킹은 나중에 Add한 것이 위 → 이력(Fill) 먼저
            Body.Controls.Add(Titled("금일 출고 이력", _history));
            Body.Controls.Add(Columns(tableH, new Control[] { Titled("원료별 출고 목록", _materials, _finishReason, _finish), _lotPanel }, new[] { 58f, 42f }));
            Body.Controls.Add(Row(Caption("스캔 입력"), _scan, _info));
            Body.Controls.Add(Columns(S(170), new Control[] { _judge }));

            ReloadHistory();
            // 샘플 시작 상태: 지시서를 읽은 뒤 FIFO 위반 포대를 스캔한 직후
            if (source is SampleMainViewSource) { HandleScan("WO-261126-007"); HandleScan("L261110-02"); }
            else UpdateButtons();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);
        }

        // ---- 표 구성 ----
        private void BuildMaterialGrid()
        {
            _materials.Columns.Add("code", "원료 코드");
            _materials.Columns.Add("name", "원료명");
            _materials.Columns.Add("req", "필요량(kg)");
            _materials.Columns.Add("issued", "출고량(kg)");
            _materials.Columns.Add("remain", "남은 양(kg)");
            _materials.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            FillAll(_materials);
        }

        private void BuildLotGrid()
        {
            _lots.Columns.Add("lot", "Lot");
            _lots.Columns.Add("recv", "입고일");
            _lots.Columns.Add("exp", "유통기한");
            _lots.Columns.Add("remain", "잔량(kg)");
            _lots.Columns.Add("rank", "FIFO 순위");
            _lots.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            FillAll(_lots);
        }

        private void BuildHistoryGrid()
        {
            _history.Columns.Add(new ApcLampColumn());
            _history.Columns.Add("time", "시각");
            _history.Columns.Add("wo", "작업지시");
            _history.Columns.Add("mat", "원료명");
            _history.Columns.Add("lot", "Lot");
            _history.Columns.Add("kg", "중량(kg)");
            _history.Columns.Add("result", "판정");
            foreach (DataGridViewColumn c in _history.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private static void FillAll(ApcGrid g)
        {
            foreach (DataGridViewColumn c in g.Columns) { if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; c.SortMode = DataGridViewColumnSortMode.NotSortable; }
        }

        private static string Kg(double v) { return v.ToString("0.0", CultureInfo.InvariantCulture); }

        // ---- 스캔 처리 ----
        private void HandleScan(string code)
        {
            code = (code ?? "").Trim();
            if (code.StartsWith(WorkOrderPrefix, StringComparison.OrdinalIgnoreCase)) { LoadOrder(code); return; }
            if (_order == null) { ShowNg("작업지시 먼저 스캔", "원료 포대는 지시서를 스캔한 뒤 출고합니다"); return; }
            var lot = _source.FindLot(code);
            var verdict = ScanJudge.Judge(_order, lot, lot == null ? new List<StockLot>() : _source.GetLots(lot.MaterialCode), DateTime.Today);
            if (!verdict.Ok)
            {
                _pendingFifo = verdict.IsFifoViolation ? verdict : null;
                ShowNg(verdict.Reason, verdict.Detail);
                _source.RecordIssue(new IssueRecord(DateTime.Now, _order.No, lot == null ? "" : lot.MaterialName, code, 0, false, "NG · " + verdict.Reason), 0);
                if (verdict.Line != null) SelectMaterial(verdict.Line.Code);
                ReloadHistory();
                UpdateButtons();
                return;
            }
            Issue(verdict, "OK");
        }

        private void LoadOrder(string code)
        {
            var order = _source.FindWorkOrder(code);
            if (order == null) { ShowNg("미등록 작업지시", code); return; }
            _order = order;
            _pendingFifo = null;
            _issued = _source.GetIssuedKg(order.No);
            _info.SetValue("지시 번호", order.No);
            _info.SetValue("제품", order.Product);
            _info.SetValue("배합 배치 수", order.Batches.ToString("#,0", CultureInfo.InvariantCulture));
            _info.SetValue("투입 라인", order.Line + " · " + order.AllergenLine + " 라인");
            _judge.Text = "READY"; _judge.Status = StatusKind.Stop; _judge.SubText = "원료 포대 라벨을 스캔하세요";
            ReloadMaterials();
            SelectMaterial(order.Lines[0].Code);
            UpdateButtons();
        }

        private void Issue(ScanJudge.Verdict v, string result)
        {
            double before = Issued(v.Line.Code);
            double after = before + v.Kg;
            _source.RecordIssue(new IssueRecord(DateTime.Now, _order.No, v.Line.Name, v.Lot.Lot, v.Kg, true, result), v.Lot.RemainKg - v.Kg);
            _issued[v.Line.Code] = after;
            _pendingFifo = null;
            string suffix = after > v.Line.RequiredKg * (1 + ScanJudge.Tolerance) ? " · 초과"
                          : ScanJudge.IsNearExpiry(v.Lot, DateTime.Today) ? " · 유통기한 임박" : "";
            _judge.Text = "OK"; _judge.Status = StatusKind.Run;
            _judge.SubText = v.Line.Name + " " + Kg(v.Kg) + " kg" + suffix;
            ReloadMaterials(); SelectMaterial(v.Line.Code); ReloadHistory(); UpdateButtons();
        }

        private void ShowNg(string reason, string detail)
        {
            _judge.Text = "NG · " + reason; _judge.Status = StatusKind.Alarm; _judge.SubText = detail;
        }

        private double Issued(string code) { double v; return _issued.TryGetValue(code, out v) ? v : 0; }

        // ---- 화면 갱신 ----
        private void ReloadMaterials()
        {
            string keep = SelectedCode();
            _materials.Rows.Clear();
            if (_order == null) return;
            foreach (var line in _order.Lines)
            {
                double issued = Issued(line.Code);
                string word = ScanJudge.Progress(issued, line.RequiredKg);
                var kind = word == "충족" ? StatusKind.Run : word == "대기" ? StatusKind.Stop : StatusKind.Warn;
                int i = _materials.Rows.Add(line.Code, line.Name, Kg(line.RequiredKg), Kg(issued), Kg(Math.Max(0, line.RequiredKg - issued)), new ApcLampValue(kind, word));
                if (word == "초과") _materials.Rows[i].Cells[3].Style.ForeColor = ApcTheme.StatusText(StatusKind.Warn);
            }
            if (keep != null) SelectMaterial(keep);
        }

        private string SelectedCode()
        {
            return _materials.CurrentRow == null ? null : Convert.ToString(_materials.CurrentRow.Cells[0].Value);
        }

        private void SelectMaterial(string code)
        {
            foreach (DataGridViewRow r in _materials.Rows)
                if (Convert.ToString(r.Cells[0].Value) == code) { _materials.CurrentCell = r.Cells[0]; return; }
        }

        private void ShowLots()
        {
            _lots.Rows.Clear();
            string code = SelectedCode();
            if (code == null) return;
            var today = DateTime.Today;
            var all = _source.GetLots(code).Where(l => l.RemainKg > 0).ToList();
            var rank = ScanJudge.FifoOrder(all, today);
            foreach (var l in all.OrderBy(x => x.ExpiresOn).ThenBy(x => x.ReceivedOn))
            {
                bool expired = ScanJudge.IsExpired(l, today), near = ScanJudge.IsNearExpiry(l, today);
                var state = expired ? new ApcLampValue(StatusKind.Alarm, "기한 경과") : near ? new ApcLampValue(StatusKind.Warn, "기한 임박") : new ApcLampValue(StatusKind.Run, "정상");
                int i = _lots.Rows.Add(l.Lot, l.ReceivedOn.ToString(ScanJudge.DateFormat), l.ExpiresOn.ToString(ScanJudge.DateFormat), Kg(l.RemainKg),
                    expired ? "" : (rank.IndexOf(l) + 1).ToString(), state);
                if (expired || near) _lots.Rows[i].Cells[2].Style.ForeColor = ApcTheme.StatusText(expired ? StatusKind.Alarm : StatusKind.Warn);
            }
        }

        private void ReloadHistory()
        {
            _history.Rows.Clear();
            foreach (var h in _source.GetTodayHistory())
                _history.Rows.Add(h.Ok ? StatusKind.Run : StatusKind.Alarm, h.At.ToString("HH:mm:ss"), h.WorkOrderNo, h.MaterialName, h.Lot,
                    h.Ok ? Kg(h.Kg) : "", h.Result);
        }

        // [출고 완료]는 모든 원료 필요량 충족 시만, [FIFO 예외 승인]은 직전 스캔이 FIFO 위반일 때만. 막힌 이유는 버튼 옆 글자.
        private void UpdateButtons()
        {
            var unmet = _order == null ? new List<string>() : _order.Lines.Where(l => !ScanJudge.IsSatisfied(Issued(l.Code), l.RequiredKg)).Select(l => l.Name).ToList();
            _finish.Enabled = _order != null && unmet.Count == 0;
            _finishReason.Text = _order == null ? "작업지시 스캔 전" : unmet.Count == 0 ? "" : "필요량 미충족 · " + string.Join(", ", unmet.ToArray());
            _fifoException.Enabled = _pendingFifo != null;
            _fifoReason.Text = _pendingFifo != null ? "" : "FIFO 위반 스캔 시에만";
        }

        // ---- 동작 ----
        private void ApproveFifoException()
        {
            var v = _pendingFifo;
            if (v == null) return;
            using (var dlg = new FifoApprovalDialog(v.Line.Code + " " + v.Line.Name, v.Lot.Lot, v.Lot.ExpiresOn, v.AheadLot.Lot, v.AheadLot.ExpiresOn, DateTime.Today, _source.FindApprover))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                Issue(v, "OK · FIFO 예외 승인 (" + dlg.Reason + " · " + dlg.ApproverName + ")");
            }
        }

        private void FinishOrder()
        {
            if (_order == null || !_finish.Enabled) return;
            if (!ApcDialog.Confirm(FindForm(), _order.No + " 출고를 완료합니다.", "출고 완료", ApcButtonVariant.Primary)) return;
            ApcDialog.Alert(FindForm(), _order.No + " 출고가 완료되었습니다.");
            _order = null; _pendingFifo = null; _issued = new Dictionary<string, double>();
            _materials.Rows.Clear();
            _info.SetItems("지시 번호", "제품", "배합 배치 수", "투입 라인");
            _judge.Text = "READY"; _judge.Status = StatusKind.Stop; _judge.SubText = "작업지시서 바코드를 스캔하세요";
            UpdateButtons();
        }
    }
}
