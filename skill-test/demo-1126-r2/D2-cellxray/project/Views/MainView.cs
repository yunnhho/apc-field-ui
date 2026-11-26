using System;
using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CellXrayJudgeStation.Data;

namespace CellXrayJudgeStation.Views
{
    /// <summary>메인 화면: 트레이 판정판 + 금일 집계 → 바코드 스캔 → 셀 50개 격자 · 선택 셀 측정값 → 최근 트레이 이력.</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int TileHeight = 40;
        private const int ListedSlots = 4;

        private readonly IMainViewSource _source;
        private TrayData _tray;
        private CellRecord _selected;
        private int _doneTotal, _doneOk, _doneNg, _doneRejudged;   // 완료 트레이 합(이력에서)

        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcValueCard _total = Card("금일 검사", "EA"), _ok = Card("OK", "EA"), _ng = Card("NG", "EA"), _rejudge = Card("재판정", "EA");
        private readonly ApcScanBox _scan = new ApcScanBox { Width = S(300) };
        private readonly ApcInfoStrip _spec = new ApcInfoStrip();
        private readonly ApcStatusTile[] _tiles = new ApcStatusTile[TrayData.CellCount];
        private readonly ApcButton _complete = Button("트레이 완료", ApcButtonVariant.Primary, 220);
        private readonly Label _completeReason = Caption("");
        private readonly System.Drawing.Color _noteColor;
        private readonly ApcGrid _measure = CellMeasureTable.Create();
        private readonly ApcButton _rejudgeBtn = Button("셀 재판정", ApcButtonVariant.Normal, 150);
        private readonly Label _rejudgeReason = Caption("");
        private readonly Panel _trayPanel, _cellPanel;
        private readonly ApcGrid _history = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("셀 X-ray 판정")
        {
            _source = source;
            _noteColor = _completeReason.ForeColor;
            _scan.Scanned += OnScanned;
            _spec.SetItems("셀 규격", "오버행", "정렬 편차");
            _spec.SetValue("셀 규격", "21700");
            _spec.SetValue("오버행", "0.50 mm 이상 · 경계 0.50~0.60");
            _spec.SetValue("정렬 편차", "0.30 mm 이하");
            _spec.Dock = DockStyle.None;
            _spec.Width = S(900);
            _spec.Height = _scan.Height;

            BuildHistoryColumns();
            foreach (var h in source.GetHistory()) { AddHistory(h, false); AddDone(h); }

            _complete.Click += (s, e) => CompleteTray();
            _rejudgeBtn.Click += (s, e) => Rejudge();

            _trayPanel = Titled("셀 판정", BuildTiles(), Legend(), _completeReason, _complete);
            _cellPanel = Titled("선택 셀", _measure, _rejudgeReason, _rejudgeBtn);

            // Fill(이력)을 먼저, Top 줄은 아래쪽 것부터
            Body.Controls.Add(Titled("최근 트레이 이력", _history));
            int tilesHeight = TrayData.Rows * (S(TileHeight) + S(12));
            int midHeight = TitledHeight(Math.Max(tilesHeight, ApcGrid.HeightForRows(CellMeasureTable.RowCount)), true);
            Body.Controls.Add(Columns(midHeight, new Control[] { _trayPanel, _cellPanel }, new[] { 75f, 25f }));
            Body.Controls.Add(Row(Caption("트레이 바코드"), _scan, _spec));
            Body.Controls.Add(Columns(S(120), new Control[] { _judge, _total, _ok, _ng, _rejudge }, new[] { 36f, 16f, 16f, 16f, 16f }));

            LoadTrayInto("TR-A0153");   // 샘플: 스캔한 직후 상태로 시작
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);
        }

        // ---- 배치 ----
        private Control BuildTiles()
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            var rows = new Control[TrayData.Rows];
            for (int r = 0; r < TrayData.Rows; r++)
            {
                var cells = new Control[TrayData.Columns];
                for (int c = 0; c < TrayData.Columns; c++)
                {
                    int slot = r * TrayData.Columns + c;
                    var t = new ApcStatusTile { Dock = DockStyle.Fill, Status = StatusKind.Stop };
                    t.Click += (s, e) => SelectSlot(slot);
                    _tiles[slot] = t;
                    cells[c] = t;
                }
                rows[r] = Columns(S(TileHeight), cells);
            }
            for (int r = TrayData.Rows - 1; r >= 0; r--) panel.Controls.Add(rows[r]);   // 위쪽 줄이 나중에
            return panel;
        }

        private void BuildHistoryColumns()
        {
            _history.Columns.Add(new ApcLampColumn());
            _history.Columns.Add("time", "완료 시각");
            _history.Columns.Add("tray", "트레이");
            _history.Columns.Add("total", "검사(EA)");
            _history.Columns.Add("ok", "OK(EA)");
            _history.Columns.Add("ng", "NG 제외(EA)");
            _history.Columns.Add("rej", "재판정(EA)");
            _history.Columns.Add("next", "이관");
            foreach (DataGridViewColumn c in _history.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void AddHistory(HistoryRow h, bool atTop)
        {
            object[] v = { StatusKind.Run, h.Time.ToString("HH:mm:ss"), h.TrayId, h.Total.ToString("#,0"), h.Ok.ToString("#,0"), h.Ng.ToString("#,0"), h.Rejudged.ToString("#,0"), "전해액 주입" };
            if (atTop) _history.Rows.Insert(0, v); else _history.Rows.Add(v);
        }

        private void AddDone(HistoryRow h) { _doneTotal += h.Total; _doneOk += h.Ok; _doneNg += h.Ng; _doneRejudged += h.Rejudged; }

        // ---- 동작 ----
        private void OnScanned(object sender, ScanEventArgs e)
        {
            string error = LoadTrayInto(e.Code.Trim());
            e.Accepted = error == null;
            if (error != null) BeginInvoke(new Action(() => ApcDialog.Warn(FindForm(), error)));
        }

        /// <summary>오류 문구(없으면 null). 진행 중인 트레이가 있으면 새 스캔을 막는다.</summary>
        private string LoadTrayInto(string id)
        {
            if (!ValidTrayId(id)) return "트레이 바코드 형식이 아닙니다 · " + id;
            if (_tray != null && _tray.TrayId != id) return "트레이 " + _tray.TrayId + " 미완료 · [트레이 완료] 후 스캔하세요";
            if (_tray != null) return null;
            if (_source.IsCompleted(id)) return "이미 완료한 트레이입니다 · " + id;
            var tray = _source.LoadTray(id);
            if (tray == null) return "등록되지 않은 트레이입니다 · " + id;
            _tray = tray;
            _scan.Text = id;
            _selected = FirstToReview(tray);
            Reload();
            return null;
        }

        private static bool ValidTrayId(string id)
        {
            if (id.Length != 8 || !id.StartsWith("TR-") || !char.IsLetter(id[3])) return false;
            for (int i = 4; i < 8; i++) if (!char.IsDigit(id[i])) return false;
            return true;
        }

        private static CellRecord FirstToReview(TrayData t)
        {
            foreach (var c in t.Cells) if (c.State == CellState.Pending) return c;
            foreach (var c in t.Cells) if (c.State == CellState.Ng) return c;
            return t.Cells[0];
        }

        private void SelectSlot(int index)
        {
            if (_tray == null) return;
            _selected = _tray.Cells[index];
            Reload();
        }

        private void Rejudge()
        {
            if (_tray == null || _selected == null || _selected.State == CellState.Ok) return;
            using (var dlg = new RejudgeDialog(_selected, _tray.TrayId))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _selected.State = dlg.ResultState;
                _selected.NgType = dlg.ResultNgType;
                _selected.Reason = dlg.ResultState == CellState.Ng ? dlg.ResultNgType + (dlg.ResultReason.Length > 0 ? " · " + dlg.ResultReason : "") : (dlg.ResultReason.Length > 0 ? dlg.ResultReason : "재판정 OK");
                _selected.Rejudged = true;
                _source.SaveRejudge(_tray.TrayId, _selected);
            }
            Reload();
        }

        private void CompleteTray()
        {
            if (_tray == null || _tray.Count(CellState.Pending) > 0) return;   // 버튼이 꺼져 있지만 한 번 더 막는다
            int ng = _tray.Count(CellState.Ng);
            string msg = "트레이 " + _tray.TrayId + "를 완료하고 전해액 주입으로 넘깁니다."
                + (ng > 0 ? "\nNG " + ng + "개는 NG 박스로 보내고 트레이에는 빈칸으로 남겨야 합니다." : "");
            if (!ApcDialog.Confirm(FindForm(), msg, "완료", ApcButtonVariant.Primary)) return;
            var row = _source.CompleteTray(_tray, DateTime.Now);
            AddHistory(row, true);
            AddDone(row);
            _tray = null; _selected = null;
            _scan.Text = "";
            Reload();
        }

        // ---- 화면 갱신 ----
        private void Reload()
        {
            ShowTiles();
            ShowJudge();
            ShowCounts();
            _trayPanel.Text = _tray == null ? "셀 판정" : "셀 판정 · " + _tray.TrayId;
            _cellPanel.Text = _selected == null ? "선택 셀" : "선택 셀 · " + _selected.Slot.ToString("00") + "번 · " + _selected.CellId;
            CellMeasureTable.Fill(_measure, _selected);
            ShowButtons();
        }

        private void ShowTiles()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                var t = _tiles[i];
                var c = _tray == null ? null : _tray.Cells[i];
                t.Selected = c != null && c == _selected;
                t.Status = c == null ? StatusKind.Stop : c.State == CellState.Ok ? StatusKind.Run : c.State == CellState.Ng ? StatusKind.Alarm : StatusKind.Warn;
                t.Text = c == null ? "" : c.Slot.ToString("00") + (c.State == CellState.Ok ? "" : c.State == CellState.Ng ? " NG" : " 대기");
            }
        }

        private void ShowJudge()
        {
            if (_tray == null) { _judge.Text = "READY"; _judge.Status = StatusKind.Stop; _judge.SubText = "트레이 바코드를 스캔하세요"; return; }
            int pending = _tray.Count(CellState.Pending), ng = _tray.Count(CellState.Ng);
            if (pending > 0)
            {
                _judge.Text = "재판정 대기 " + pending;
                _judge.Status = StatusKind.Warn;
                _judge.SubText = "셀 " + SlotList(CellState.Pending);
                return;
            }
            _judge.Text = "OK";
            _judge.Status = StatusKind.Run;
            _judge.SubText = ng > 0 ? "NG " + ng + "개는 NG 박스로 · 이관 가능" : "전 셀 OK · 이관 가능";
        }

        private string SlotList(CellState state)
        {
            var list = new List<string>();
            int n = 0;
            foreach (var c in _tray.Cells)
            {
                if (c.State != state) continue;
                if (list.Count < ListedSlots) list.Add(c.Slot.ToString("00")); else n++;
            }
            return string.Join(", ", list.ToArray()) + (n > 0 ? " 외 " + n : "");
        }

        private void ShowCounts()
        {
            int total = _doneTotal, ok = _doneOk, ng = _doneNg, rej = _doneRejudged, pending = 0;
            if (_tray != null)
            {
                total += _tray.Cells.Count; ok += _tray.Count(CellState.Ok); ng += _tray.Count(CellState.Ng);
                pending = _tray.Count(CellState.Pending); rej += _tray.RejudgedDone + pending;
            }
            _total.Value = total.ToString("#,0");
            _ok.Value = ok.ToString("#,0");
            _ng.Value = ng.ToString("#,0");
            _ng.ValueStatus = ng > 0 ? StatusKind.Alarm : (StatusKind?)null;   // 하루 집계: 빨간 숫자만, 테두리 없음
            _rejudge.Value = rej.ToString("#,0");
            _rejudge.Footer = pending > 0 ? "대기 " + pending + "개" : "";
        }

        private void ShowButtons()
        {
            if (_tray == null) _completeReason.Text = "트레이를 스캔하세요";
            else if (_tray.Count(CellState.Pending) > 0) _completeReason.Text = "재판정 대기 " + _tray.Count(CellState.Pending) + "개 · 완료 불가";
            else _completeReason.Text = "";
            _completeReason.ForeColor = _tray != null && _tray.Count(CellState.Pending) > 0 ? ApcTheme.StatusText(StatusKind.Alarm) : _noteColor;
            _complete.Enabled = _tray != null && _tray.Count(CellState.Pending) == 0;

            bool can = _selected != null && _selected.State != CellState.Ok;
            _rejudgeBtn.Enabled = can;
            _rejudgeReason.Text = _selected == null ? "" : can ? "" : "OK 셀은 재판정 대상이 아닙니다";
        }

        private static ApcStatusLegend Legend()
        {
            var l = new ApcStatusLegend();
            l.SetItems(new ApcLampValue(StatusKind.Run, "OK"), new ApcLampValue(StatusKind.Alarm, "NG"), new ApcLampValue(StatusKind.Warn, "대기 = 재판정 대기"));
            return l;
        }

        private static ApcValueCard Card(string title, string unit)
        {
            return new ApcValueCard { Title = title, Unit = unit, Dock = DockStyle.Fill, Status = StatusKind.Run, ShowLamp = false };
        }
    }
}
