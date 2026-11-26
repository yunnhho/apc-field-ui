using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FgRackLocator.Data;

namespace FgRackLocator.Views
{
    /// <summary>
    /// 완제품 창고 랙 위치 맵: 팔레트 스캔 → 입고 추천 위치 / 출고 대상 위치를 맵에서 강조 → 위치 확정 dialog.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const double WhiteMix = 0.55;   // 칸 바탕 = 상태색을 흰색에 섞어 연하게(글자가 읽히게)

        private readonly IMainViewSource _source;
        private readonly ApcScanBox _scan = new ApcScanBox { Dock = DockStyle.Fill };
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcButton _confirm = Button("위치 확정", ApcButtonVariant.Primary, 130);
        private readonly Label _reason = Caption("");
        private readonly ApcGrid _history = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly Dictionary<char, ApcGrid> _racks = new Dictionary<char, ApcGrid>();
        private readonly Dictionary<char, Panel> _rackPanels = new Dictionary<char, Panel>();
        private ScanResult _current;

        public MainView() : this(new SampleMainViewSource(), SampleMainViewSource.DemoInboundHeavy) { }

        /// <param name="initialScan">시작할 때 이미 스캔한 상태로 보여 줄 팔레트 코드(null이면 대기).</param>
        public MainView(IMainViewSource source, string initialScan) : base("완제품 창고 랙 위치 관리")
        {
            _source = source;
            _scan.Scanned += (s, e) => e.Accepted = HandleScan(e.Code);
            _confirm.Click += (s, e) => OpenConfirm();

            BuildHistoryColumns();
            var left = new Panel { Dock = DockStyle.Fill };
            int rackHeight = TitledHeight(ApcGrid.HeightForRows(RackLayout.Tiers));
            TableLayoutPanel rowCd = Columns(rackHeight, new Control[] { RackPanel('C'), RackPanel('D') });
            TableLayoutPanel rowAb = Columns(rackHeight, new Control[] { RackPanel('A'), RackPanel('B') });
            TableLayoutPanel legendRow = Columns(S(34), new Control[] { BuildLegend() });
            TableLayoutPanel topRow = Columns(S(130), new Control[] { _judge, Titled("팔레트 스캔", BuildScanCell(), _reason, _confirm) }, new[] { 55f, 45f });
            left.Controls.Add(rowCd);
            left.Controls.Add(rowAb);
            left.Controls.Add(legendRow);
            left.Controls.Add(topRow);
            // 랙 두 줄이 남는 높이를 나눠 갖는다(창 크기가 달라도 아래가 비지 않게). 칸 높이도 따라간다.
            left.Resize += (s, e) =>
            {
                int each = (left.ClientSize.Height - topRow.Height - legendRow.Height) / 2;
                if (each > rackHeight) { rowAb.Height = each; rowCd.Height = each; }
            };
            foreach (ApcGrid g in _racks.Values) g.Resize += (s, e) => FitRackRows((ApcGrid)s);
            Body.Controls.Add(FillColumns(new Control[] { left, Titled("오늘 입출고 이력", _history) }, new[] { 68f, 32f }));

            RefreshAll();
            if (initialScan != null) { _current = source.Evaluate(initialScan); ShowResult(); }
            else ShowReady("팔레트 바코드를 스캔하세요");
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);   // 생성자 시점엔 폼이 없다
        }

        // ---- 스캔·판정 ----

        private bool HandleScan(string code)
        {
            if (!code.StartsWith("PL-", StringComparison.Ordinal))
                return Reject(code.StartsWith("LOC-", StringComparison.Ordinal)
                    ? "위치 라벨은 위치 확정 창에서 스캔합니다 · " + code
                    : "팔레트 바코드가 아닙니다 · " + code);
            ScanResult result = _source.Evaluate(code);
            if (result == null) return Reject("등록되지 않은 팔레트입니다 · " + code);
            _current = result;
            ShowResult();
            return true;
        }

        private bool Reject(string message)
        {
            BeginInvoke((Action)(() => ApcDialog.Warn(FindForm(), message)));
            return false;
        }

        private void ShowReady(string sub)
        {
            _current = null;
            _judge.Status = StatusKind.Stop;
            _judge.Text = "READY";
            _judge.SubText = sub;
            _confirm.Enabled = false;
            _reason.Text = "스캔 후 확정할 수 있습니다";
            RefreshRacks(null);
        }

        private void ShowResult()
        {
            ScanResult r = _current;
            Pallet p = r.Pallet;
            string who = p.PartNo + " · " + p.WeightKg + " kg";
            switch (r.Kind)
            {
                case ScanKind.Inbound:
                    SetJudge(StatusKind.Run, "입고 → " + r.Location, p.IsHeavy ? who + "  (무거운 품번 1~2단)" : who, "");
                    break;
                case ScanKind.InboundNoSpace:
                    SetJudge(StatusKind.Alarm, "입고 불가", "빈 칸 없음 · " + p.PartNo, "빈 칸 없음");
                    break;
                case ScanKind.OutboundHold:
                    SetJudge(StatusKind.Alarm, "출고 불가", "품질 보류 · " + r.Location, "품질 보류 팔레트");
                    break;
                default:
                    if (r.Older != null)
                        SetJudge(StatusKind.Warn, "출고 → " + r.Location, "더 오래된 " + r.Older.Location + " 먼저 · " + r.Older.InboundDate.ToString("MM-dd") + " 입고", "FIFO · 사원증 필요");
                    else
                        SetJudge(StatusKind.Run, "출고 ← " + r.Location, who + " · " + p.InboundDate.ToString("MM-dd") + " 입고", "");
                    break;
            }
            RefreshRacks(r.Location);
        }

        private void SetJudge(StatusKind status, string text, string sub, string blockedReason)
        {
            _judge.Status = status;
            _judge.Text = text;
            _judge.SubText = sub;
            bool ok = _current != null && _current.CanConfirm;
            _confirm.Enabled = ok;
            _reason.Text = blockedReason;
        }

        private void OpenConfirm()
        {
            if (_current == null || !_current.CanConfirm) return;
            using (var dlg = new LocationConfirmDialog(_current, _source))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (_current.Kind == ScanKind.Inbound) _source.ConfirmInbound(_current.Pallet, _current.Location);
                else _source.ConfirmOutbound(_current.Pallet, dlg.ApproverBadge);
                string done = (_current.Kind == ScanKind.Inbound ? "입고 " : "출고 ") + _current.Location + " 확정 · 다음 팔레트를 스캔하세요";
                RefreshAll();
                ShowReady(done);
            }
        }

        // ---- 그리기 ----

        private Panel RackPanel(char rack)
        {
            var grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            grid.Columns.Add("tier", "");
            grid.Columns[0].Width = S(46);
            grid.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
            for (int c = 1; c <= RackLayout.Columns; c++)
            {
                int i = grid.Columns.Add("c" + c, c.ToString("00"));
                grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                grid.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                grid.Columns[i].MinimumWidth = 10;
            }
            for (int tier = RackLayout.Tiers; tier >= 1; tier--) grid.Rows.Add(tier + "단");
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _racks[rack] = grid;
            Panel p = Titled(rack + "랙", grid);
            _rackPanels[rack] = p;
            return p;
        }

        private Control BuildScanCell()
        {
            var cell = new Panel { Dock = DockStyle.Fill };
            _scan.Dock = DockStyle.Top;
            Label hint = Caption("바코드 스캐너로 스캔 (키보드 입력 없음)");
            hint.Dock = DockStyle.Top;
            cell.Controls.Add(hint);
            cell.Controls.Add(_scan);
            return cell;
        }

        private static void FitRackRows(ApcGrid grid)
        {
            int each = (grid.ClientSize.Height - grid.ColumnHeadersHeight) / RackLayout.Tiers;
            if (each < ApcGrid.HeightForRows(1) - grid.ColumnHeadersHeight) return;
            foreach (DataGridViewRow r in grid.Rows) r.Height = each;
        }

        private static Control BuildLegend()
        {
            var legend = new ApcStatusLegend { Dock = DockStyle.Left };
            legend.SetItems(
                new ApcLampValue(StatusKind.Stop, "빈칸"),
                new ApcLampValue(StatusKind.Info, "적재"),
                new ApcLampValue(StatusKind.Alarm, "보류"),
                new ApcLampValue(StatusKind.Warn, "장기재고"),
                new ApcLampValue(StatusKind.Run, "추천·출고 위치"));
            legend.Width = legend.PreferredWidth;
            return legend;
        }

        private void RefreshAll()
        {
            RefreshRacks(null);
            RefreshHistory();
        }

        private void RefreshRacks(string highlight)
        {
            foreach (char rack in RackLayout.RackNames)
            {
                ApcGrid grid = _racks[rack];
                int used = 0;
                for (int tier = 1; tier <= RackLayout.Tiers; tier++)
                    for (int col = 1; col <= RackLayout.Columns; col++)
                    {
                        string loc = RackLayout.Code(rack, tier, col);
                        Pallet p = _source.GetPalletAt(loc);
                        if (p != null) used++;
                        StyleCell(grid[col, RackLayout.Tiers - tier], p, loc == highlight);
                    }
                int total = RackLayout.Tiers * RackLayout.Columns;
                SetTitle(_rackPanels[rack], rack + "랙", rack + "랙  ·  적재율 " + (100 * used / total) + " %  (" + used + " / " + total + "칸)");
            }
        }

        private void StyleCell(DataGridViewCell cell, Pallet p, bool highlighted)
        {
            StatusKind kind = p == null ? StatusKind.Stop
                : p.IsHold ? StatusKind.Alarm
                : (_source.Today - p.InboundDate).Days > RackLayout.LongTermDays ? StatusKind.Warn
                : StatusKind.Info;
            Color back = highlighted ? ApcTheme.Status(StatusKind.Run) : Mix(ApcTheme.Status(kind), WhiteMix);
            Color fore = Color.Black;
            cell.Value = p == null ? "" : p.PartAbbr;
            cell.Style.BackColor = back;
            cell.Style.SelectionBackColor = back;
            cell.Style.ForeColor = fore;
            cell.Style.SelectionForeColor = fore;
            cell.Style.Font = highlighted ? ApcTheme.GetFont(FontStyle.Bold) : null;
            if (highlighted && p == null) cell.Value = "▶";
        }

        private static Color Mix(Color c, double weight)
        {
            return Color.FromArgb(
                (int)(255 - (255 - c.R) * weight),
                (int)(255 - (255 - c.G) * weight),
                (int)(255 - (255 - c.B) * weight));
        }

        private static void SetTitle(Control titled, string oldPrefix, string text)
        {
            foreach (Control c in titled.Controls)
            {
                Label l = c as Label;
                if (l != null && l.Text.StartsWith(oldPrefix, StringComparison.Ordinal)) { l.Text = text; return; }
                if (c.HasChildren) SetTitle(c, oldPrefix, text);
            }
        }

        private void BuildHistoryColumns()
        {
            _history.Columns.Add("time", "시각");
            _history.Columns.Add("dir", "구분");
            _history.Columns.Add("pallet", "팔레트");
            _history.Columns[2].FillWeight = 190;
            _history.Columns.Add("part", "품번");
            _history.Columns.Add("loc", "위치");
            _history.Columns.Add("worker", "작업자");
            foreach (DataGridViewColumn c in _history.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _history.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void RefreshHistory()
        {
            _history.Rows.Clear();
            foreach (HistoryEntry h in _source.GetTodayHistory())
                _history.Rows.Add(h.Time.ToString("HH:mm"), h.Direction, h.PalletCode, h.PartNo, h.Location, h.Worker);
        }
    }
}
