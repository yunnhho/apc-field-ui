using System;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CellLeakTester.Data;

namespace CellLeakTester.Views
{
    /// <summary>메인 화면. 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private readonly IMainViewSource _source;
        private readonly ApcTextBox _barcode = new ApcTextBox { ReadOnly = true, Width = S(240) };
        private readonly ApcTextBox _lot = new ApcTextBox { ReadOnly = true, Width = S(200) };
        private readonly ApcTextBox _model = new ApcTextBox { ReadOnly = true, Width = S(160) };
        private readonly ApcTextBox _worker = new ApcTextBox { ReadOnly = true, Width = S(140) };
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill, Text = "READY", SubText = "검사 대기", Status = StatusKind.Stop };
        private readonly ApcValueCard _leak = Card("누설량", LeakJudge.Unit, "");
        private readonly ApcValueCard _limit = Card("기준값 (상한)", LeakJudge.Unit, LeakJudge.LimitText);
        private readonly ApcValueCard _total = Card("오늘 생산", "EA", "");
        private readonly ApcValueCard _ok = Card("OK", "EA", "");
        private readonly ApcValueCard _ng = Card("NG", "EA", "");
        private readonly ApcValueCard _yield = Card("수율", "%", "OK / 생산");
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("셀 누설 검사")
        {
            _source = source;

            _limit.Value = LeakJudge.Format(LeakJudge.UpperLimit);
            _limit.Status = StatusKind.Run;
            _judge.Margin = new Padding(0, 0, S(12), 0);

            _grid.Columns.Add("time", "시각");
            _grid.Columns.Add("barcode", "바코드");
            _grid.Columns.Add("leak", "누설량(" + LeakJudge.Unit + ")");
            _grid.Columns.Add(new ApcLampColumn { HeaderText = "판정", Width = S(110) });
            foreach (DataGridViewColumn c in _grid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var lotButton = Button("LOT 입력", ApcButtonVariant.Normal, 120);
            lotButton.Click += (s, e) => EditLot();
            var adminButton = Button("관리자 확인", ApcButtonVariant.Normal, 130);
            adminButton.Click += (s, e) => CheckAdmin();

            // Top 도킹은 나중에 Add한 것이 위 → 이력(Fill) 먼저, 아래 줄부터
            Body.Controls.Add(_grid);
            var historyTitle = SectionTitle("최근 검사 이력");
            historyTitle.Dock = DockStyle.Top;
            Body.Controls.Add(historyTitle);
            Body.Controls.Add(Columns(S(130), new Control[] { _total, _ok, _ng, _yield }));
            Body.Controls.Add(Columns(S(220), new Control[] { _judge, _leak, _limit }, new[] { 50f, 25f, 25f }));
            Body.Controls.Add(Row(Caption("바코드"), _barcode, Caption("LOT"), _lot, Caption("모델"), _model,
                Caption("작업자"), _worker, lotButton, adminButton));

            ShowLot();
            ShowHistory();
            ShowCounts();
        }

        private void ShowLot()
        {
            _lot.Text = _source.CurrentLot;
            _model.Text = _source.CurrentModel;
            _worker.Text = _source.CurrentWorker;
        }

        private void ShowHistory()
        {
            _grid.Rows.Clear();
            var rows = _source.GetHistory();
            foreach (var r in rows)
            {
                bool ng = LeakJudge.IsNg(r.Leak);
                int i = _grid.Rows.Add(r.Time.ToString("HH:mm:ss"), r.Barcode, LeakJudge.Format(r.Leak),
                    new ApcLampValue(ng ? StatusKind.Alarm : StatusKind.Run, ng ? "NG" : "OK"));
                if (ng)
                {
                    var style = _grid.Rows[i].Cells[2].Style;
                    style.ForeColor = ApcTheme.Status(StatusKind.Alarm);
                    style.Font = ApcTheme.GetFont(System.Drawing.FontStyle.Bold);
                }
            }
            if (rows.Count == 0) return;

            var cur = rows[0];
            bool curNg = LeakJudge.IsNg(cur.Leak);
            _barcode.Text = cur.Barcode;
            _leak.Value = LeakJudge.Format(cur.Leak);
            _leak.Status = curNg ? StatusKind.Alarm : StatusKind.Run;
            _leak.Footer = cur.Time.ToString("HH:mm:ss") + " 검사";
            _judge.Text = curNg ? "NG" : "OK";
            _judge.Status = curNg ? StatusKind.Alarm : StatusKind.Run;
            _judge.SubText = curNg
                ? cur.Barcode + " · 누설량 " + LeakJudge.Format(cur.Leak) + " " + LeakJudge.Unit + " > 상한 " + LeakJudge.Format(LeakJudge.UpperLimit)
                : cur.Barcode + " · 누설량 " + LeakJudge.Format(cur.Leak) + " " + LeakJudge.Unit + " (" + LeakJudge.LimitText + ")";
            Header.Status = curNg ? StatusKind.Alarm : StatusKind.Run;
            Header.StatusText = curNg ? "NG 발생" : "가동중";
            if (curNg) Alarm.Push(cur.Barcode + ": 누설량 " + LeakJudge.Format(cur.Leak) + " " + LeakJudge.Unit + " (상한 " + LeakJudge.Format(LeakJudge.UpperLimit) + ")");
        }

        private void ShowCounts()
        {
            int total = _source.TodayTotal, ng = _source.TodayNg, ok = total - ng;
            _total.Value = total.ToString("#,0", CultureInfo.InvariantCulture);
            _ok.Value = ok.ToString("#,0", CultureInfo.InvariantCulture);
            _ng.Value = ng.ToString("#,0", CultureInfo.InvariantCulture);
            _ng.Status = ng > 0 ? StatusKind.Alarm : StatusKind.Run;
            _total.Status = _ok.Status = _yield.Status = StatusKind.Run;
            _yield.Value = total == 0 ? "" : (100.0 * ok / total).ToString("0.0", CultureInfo.InvariantCulture);
        }

        private void EditLot()
        {
            using (var dlg = new LotEntryDialog(_source.CurrentLot, _source.Models, _source.CurrentModel, _source.CurrentWorker))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.SaveLot(dlg.Lot, dlg.Model, dlg.Worker);
                ShowLot();
            }
        }

        private void CheckAdmin()
        {
            using (var dlg = new AdminPasswordDialog())
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (!_source.VerifyAdmin(dlg.Password)) { ApcDialog.Warn(FindForm(), "비밀번호가 올바르지 않습니다."); return; }
                ApcDialog.Alert(FindForm(), "관리자 확인이 완료되었습니다.");
            }
        }

        private static ApcValueCard Card(string title, string unit, string footer)
        {
            return new ApcValueCard { Title = title, Unit = unit, Footer = footer, Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        }
    }
}
