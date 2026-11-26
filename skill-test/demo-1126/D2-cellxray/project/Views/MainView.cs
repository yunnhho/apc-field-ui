using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CellXrayJudgeStation.Data;

namespace CellXrayJudgeStation.Views
{
    /// <summary>X-ray 셀 판정 메인 화면. 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int MaxListedCells = 5;   // 판정판에 칸 번호를 적는 최대 개수

        private readonly IMainViewSource _source;
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcValueCard _total = Card("금일 검사", "개");
        private readonly ApcValueCard _ok = Card("OK", "개");
        private readonly ApcValueCard _ng = Card("NG", "개");
        private readonly ApcValueCard _rejudged = Card("재판정", "개");
        private readonly ApcStatusTile[] _tiles = new ApcStatusTile[CellSpec.TrayRows * CellSpec.TrayCols];
        private readonly ApcGrid _measure = MeasureTable.Create();
        private readonly ApcGrid _history = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly Panel _trayPanel, _measurePanel;
        private readonly Label _completeReason = Caption("");
        private readonly Label _rejudgeReason = Caption("");
        private readonly ApcButton _complete = Button("트레이 완료", ApcButtonVariant.Primary, 190);
        private readonly ApcButton _rejudge = Button("재판정", ApcButtonVariant.Normal, 130);
        private TrayRecord _tray;
        private int _selected = -1;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("X-ray 셀 판정")
        {
            _source = source;
            _scan.Width = S(300);
            _scan.Scanned += OnScanned;
            _complete.Click += (s, e) => CompleteTray();
            _rejudge.Click += (s, e) => Rejudge();

            _history.Columns.Add(new ApcLampColumn());
            _history.Columns.Add("tray", "트레이");
            _history.Columns.Add("time", "완료 시각");
            _history.Columns.Add("ok", "OK(개)");
            _history.Columns.Add("ng", "NG(개)");
            _history.Columns.Add("re", "재판정(개)");
            foreach (DataGridViewColumn c in _history.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (var h in source.GetTodayHistory())
                _history.Rows.Add(new ApcLampValue(StatusKind.Run, "전달됨"), h.TrayId, h.DoneAt.ToString("HH:mm:ss"),
                    h.Ok.ToString("#,0"), h.Ng.ToString("#,0"), h.Rejudged.ToString("#,0"));

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "OK"), new ApcLampValue(StatusKind.Warn, "재판정 대기"), new ApcLampValue(StatusKind.Alarm, "NG"));
            legend.Width = legend.PreferredWidth;
            _trayPanel = Titled("트레이 셀 판정", BuildTiles(), legend, _completeReason, _complete);
            _measurePanel = Titled("선택 셀 측정값", _measure, _rejudgeReason, _rejudge);
            var right = new Panel { Dock = DockStyle.Fill };
            var historyPanel = Titled("최근 트레이 이력", _history);
            historyPanel.Dock = DockStyle.Fill;
            right.Controls.Add(historyPanel);
            right.Controls.Add(Columns(TitledHeight(ApcGrid.HeightForRows(MeasureTable.RowCount), true), new Control[] { _measurePanel }));

            // Fill 먼저, Top 줄은 아래쪽 것부터
            Body.Controls.Add(FillColumns(new Control[] { _trayPanel, right }, new[] { 66f, 34f }));
            Body.Controls.Add(Columns(S(150), new Control[] { _judge, _total, _ok, _ng, _rejudged }, new[] { 40f, 15f, 15f, 15f, 15f }));
            Body.Controls.Add(Row(Caption("트레이 바코드"), _scan));

            ShowTray(source.GetCurrentTray());
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);   // 생성자 시점엔 폼이 없다
        }

        private Control BuildTiles()
        {
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = CellSpec.TrayCols, RowCount = CellSpec.TrayRows, Padding = Padding.Empty };
            for (int c = 0; c < CellSpec.TrayCols; c++) table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / CellSpec.TrayCols));
            for (int r = 0; r < CellSpec.TrayRows; r++) table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / CellSpec.TrayRows));
            for (int i = 0; i < _tiles.Length; i++)
            {
                int index = i;
                bool lastCol = i % CellSpec.TrayCols == CellSpec.TrayCols - 1, lastRow = i / CellSpec.TrayCols == CellSpec.TrayRows - 1;
                var tile = new ApcStatusTile { Dock = DockStyle.Fill, Margin = new Padding(0, 0, lastCol ? 0 : S(6), lastRow ? 0 : S(6)) };
                tile.Click += (s, e) => Select(index);
                _tiles[i] = tile;
                table.Controls.Add(tile, i % CellSpec.TrayCols, i / CellSpec.TrayCols);
            }
            return table;
        }

        // ---- 동작 ----

        private void OnScanned(object sender, ScanEventArgs e)
        {
            string code = (e.Code ?? "").Trim();
            if (_tray != null)
            {
                e.Accepted = false;
                WarnAfterScan("진행 중인 트레이 " + _tray.TrayId + "를 먼저 [트레이 완료]하세요.");
                return;
            }
            var tray = _source.LoadTray(code);
            if (tray == null)
            {
                e.Accepted = false;
                WarnAfterScan("트레이 바코드가 아닙니다 · " + code);
                return;
            }
            ShowTray(tray);
        }

        private void WarnAfterScan(string message)
        {
            BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(FindForm(), message)));
        }

        private void Select(int index)
        {
            if (_tray == null) return;
            _selected = index;
            RefreshSelection();
        }

        private void Rejudge()
        {
            var cell = SelectedCell();
            if (cell == null || cell.Auto == CellVerdict.Ok) return;
            using (var dlg = new RejudgeDialog(cell))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                cell.Final = dlg.Result;
                cell.NgType = dlg.NgType;
                cell.Reason = dlg.Reason;
                cell.Rejudged = true;
                _source.SaveRejudge(_tray.TrayId, cell);
            }
            Refresh_();
        }

        private void CompleteTray()
        {
            if (_tray == null || _tray.Count(CellVerdict.Pending) > 0) return;   // 버튼도 꺼져 있다. 이중 방어
            int ng = _tray.Count(CellVerdict.Ng);
            string msg = "트레이 " + _tray.TrayId + "를 완료하고 다음 공정(전해액 주입)으로 넘깁니다."
                + (ng > 0 ? "\nNG " + ng.ToString("#,0") + "개는 트레이에서 빼 NG 박스로 보내세요(빈칸으로 남습니다)." : "");
            if (!ApcDialog.Confirm(FindForm(), msg, "완료", ApcButtonVariant.Primary)) return;
            var row = _source.CompleteTray(_tray, DateTime.Now);
            _history.Rows.Insert(0, new ApcLampValue(StatusKind.Run, "전달됨"), row.TrayId, row.DoneAt.ToString("HH:mm:ss"),
                row.Ok.ToString("#,0"), row.Ng.ToString("#,0"), row.Rejudged.ToString("#,0"));
            ShowTray(null);
        }

        // ---- 표시 ----

        private void ShowTray(TrayRecord tray)
        {
            _tray = tray;
            _selected = -1;
            if (tray != null) for (int i = 0; i < tray.Cells.Length; i++) if (tray.Cells[i].Final == CellVerdict.Pending) { _selected = i; break; }
            Refresh_();
        }

        private CellRecord SelectedCell() { return _tray == null || _selected < 0 ? null : _tray.Cells[_selected]; }

        private void Refresh_()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                var t = _tiles[i];
                t.Selected = i == _selected;
                if (_tray == null) { t.Text = ""; t.Status = StatusKind.Stop; continue; }
                var c = _tray.Cells[i];
                t.Text = c.Position.ToString("00");
                t.Status = CellSpec.Kind(c.Final);
            }
            ShowJudge();
            ShowCounts();
            RefreshSelection();
        }

        private void ShowJudge()
        {
            int pending = _tray == null ? 0 : _tray.Count(CellVerdict.Pending);
            if (_tray == null)
            {
                _judge.Text = "READY"; _judge.Status = StatusKind.Stop; _judge.SubText = "트레이 바코드를 스캔하세요";
                _trayPanel.Text = "트레이 셀 판정";
                SetComplete(false, "스캔한 트레이 없음");
                return;
            }
            _trayPanel.Text = "트레이 셀 판정 · " + _tray.TrayId;
            if (pending > 0)
            {
                var list = new List<string>();
                foreach (var c in _tray.Cells) if (c.Final == CellVerdict.Pending && list.Count < MaxListedCells) list.Add(c.Position.ToString());
                _judge.Text = "재판정 " + pending.ToString("#,0") + "건 남음"; _judge.Status = StatusKind.Warn;
                _judge.SubText = "칸 " + string.Join(", ", list.ToArray()) + (pending > MaxListedCells ? " 외" : "") + " · 완료 불가";
                SetComplete(false, "대기 " + pending.ToString("#,0") + "건 · 완료 불가");
                return;
            }
            int ng = _tray.Count(CellVerdict.Ng);
            _judge.Text = "트레이 OK"; _judge.Status = StatusKind.Run;
            _judge.SubText = ng > 0 ? "NG " + ng.ToString("#,0") + "개는 NG 박스로 · 완료 가능" : "전 셀 OK · 완료 가능";
            SetComplete(true, "");
        }

        private void SetComplete(bool enabled, string reason)
        {
            _complete.Enabled = enabled;
            _completeReason.Text = reason;
        }

        private void ShowCounts()
        {
            int[] sum = new int[4];
            foreach (var h in _source.GetTodayHistory()) { sum[0] += h.Ok + h.Ng; sum[1] += h.Ok; sum[2] += h.Ng; sum[3] += h.Rejudged; }
            if (_tray != null)
            {
                sum[0] += _tray.Cells.Length; sum[1] += _tray.Count(CellVerdict.Ok);
                sum[2] += _tray.Count(CellVerdict.Ng); sum[3] += _tray.RejudgedCount;
            }
            int pending = _tray == null ? 0 : _tray.Count(CellVerdict.Pending);
            _total.Value = sum[0].ToString("#,0", CultureInfo.InvariantCulture);
            _total.Footer = pending > 0 ? "재판정 대기 " + pending.ToString("#,0") + " 포함" : "";
            _ok.Value = sum[1].ToString("#,0", CultureInfo.InvariantCulture);
            _ng.Value = sum[2].ToString("#,0", CultureInfo.InvariantCulture);
            _ng.ValueStatus = sum[2] > 0 ? StatusKind.Alarm : (StatusKind?)null;   // 하루 집계: 글자색만, 알람 테두리 없음
            _rejudged.Value = sum[3].ToString("#,0", CultureInfo.InvariantCulture);
        }

        private void RefreshSelection()
        {
            var cell = SelectedCell();
            MeasureTable.Fill(_measure, cell);
            for (int i = 0; i < _tiles.Length; i++) _tiles[i].Selected = i == _selected;
            if (cell == null)
            {
                _measurePanel.Text = "선택 셀 측정값";
                _rejudge.Enabled = false;
                _rejudgeReason.Text = _tray == null ? "" : "셀을 선택하세요";
                return;
            }
            _measurePanel.Text = cell.CellId + " · 칸 " + cell.Position.ToString("00");
            bool can = cell.Auto != CellVerdict.Ok;
            _rejudge.Enabled = can;
            _rejudgeReason.Text = can ? (cell.Rejudged ? "재판정 " + CellSpec.Word(cell.Final) + (cell.NgType != null ? " · " + cell.NgType : "") : cell.AutoReason)
                                      : "자동 OK 셀은 재판정 대상이 아닙니다";
        }

        private static ApcValueCard Card(string title, string unit)
        {
            return new ApcValueCard { Title = title, Unit = unit, Footer = "", Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        }
    }
}
