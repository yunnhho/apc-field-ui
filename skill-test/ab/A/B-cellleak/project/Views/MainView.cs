using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CellLeakTester.Data;

namespace CellLeakTester.Views
{
    /// <summary>메인 화면: 현재 셀 바코드·LOT → 판정판 + 누설량·기준값 카드 → 금일 집계 카드 → 최근 검사 이력.</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const string SampleAdminPassword = "1234";   // 실제는 설정 저장소에서 확인

        private readonly IMainViewSource _source;
        private readonly ApcTextBox _barcode = new ApcTextBox { ReadOnly = true, Width = S(230) };
        private readonly ApcTextBox _lot = new ApcTextBox { ReadOnly = true, Width = S(190) };
        private readonly ApcTextBox _model = new ApcTextBox { ReadOnly = true, Width = S(150) };
        private readonly ApcTextBox _worker = new ApcTextBox { ReadOnly = true, Width = S(120) };
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        private readonly ApcValueCard _leak = Card("누설량", "Pa/s");
        private readonly ApcValueCard _limit = Card("기준값", "Pa/s");
        private readonly ApcValueCard _total = Card("오늘 생산", "EA");
        private readonly ApcValueCard _ng = Card("NG", "EA");
        private readonly ApcValueCard _yield = Card("수율", "%");
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("셀 누설 검사")
        {
            _source = source;

            _grid.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            _grid.Columns.Add("time", "시각");
            _grid.Columns.Add("barcode", "바코드");
            _grid.Columns.Add("leak", "누설량(Pa/s)");
            _grid.Columns.Add("result", "판정");
            foreach (DataGridViewColumn c in _grid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var lotButton = Button("LOT 입력", ApcButtonVariant.Normal, 130);
            lotButton.Click += (s, e) => EditLot();
            var adminButton = Button("기준값 변경", ApcButtonVariant.Normal, 130);
            adminButton.Click += (s, e) => AskAdmin();

            // Top 도킹은 나중에 Add한 것이 위 → 이력(Fill) 먼저, 그다음 아래 줄부터
            Body.Controls.Add(_grid);
            Body.Controls.Add(SectionTitle("최근 검사 이력")); Body.Controls[Body.Controls.Count - 1].Dock = DockStyle.Top;
            Body.Controls.Add(Columns(S(130), new Control[] { _total, _ng, _yield }));
            Body.Controls.Add(Columns(S(220), new Control[] { _judge, _leak, _limit }, new[] { 40f, 30f, 30f }));
            Body.Controls.Add(Row(Caption("바코드"), _barcode, Caption("LOT"), _lot, Caption("모델"), _model, Caption("작업자"), _worker, lotButton, adminButton));

            Show(source);
        }

        private void Show(IMainViewSource source)
        {
            var history = source.GetHistory();
            var lot = source.GetLot();
            _lot.Text = lot[0]; _model.Text = lot[1]; _worker.Text = lot[2];

            _limit.Value = Fmt(LeakJudge.Limit);
            _limit.Footer = "기준 " + Fmt(LeakJudge.Limit) + " 이하 OK";
            _limit.Status = StatusKind.Run;

            foreach (var r in history)
                _grid.Rows.Add(LeakJudge.Of(r.Leak), r.Time.ToString("HH:mm:ss"), r.Barcode, Fmt(r.Leak),
                    LeakJudge.Of(r.Leak) == StatusKind.Run ? "OK" : "NG · 상한 초과 (상한 " + Fmt(LeakJudge.Limit) + ")");
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if ((StatusKind)row.Cells[0].Value != StatusKind.Alarm) continue;
                row.Cells[3].Style.ForeColor = ApcTheme.Status(StatusKind.Alarm);
                row.Cells[3].Style.Font = ApcTheme.GetFont(FontStyle.Bold);
            }

            ShowCurrent(history.Count > 0 ? history[0] : null);

            var today = source.GetToday();
            _total.Value = today.Total.ToString("#,0"); _total.Status = StatusKind.Run;
            _total.Footer = "금일 검사 수량";
            _ng.Value = today.Ng.ToString("#,0"); _ng.Status = today.Ng > 0 ? StatusKind.Alarm : StatusKind.Run;
            _ng.Footer = "누설량 > " + Fmt(LeakJudge.Limit) + " Pa/s";
            _yield.Value = today.Total == 0 ? "" : (100.0 * (today.Total - today.Ng) / today.Total).ToString("0.0", CultureInfo.InvariantCulture);
            _yield.Status = today.Total == 0 ? StatusKind.Stop : StatusKind.Run;
            _yield.Footer = "OK / 생산";
        }

        private void ShowCurrent(LeakRecord r)
        {
            if (r == null)
            {
                _judge.Text = "READY"; _judge.SubText = "검사 대기"; _judge.Status = StatusKind.Stop;
                _leak.Value = ""; _leak.Status = StatusKind.Stop;
                Header.Status = StatusKind.Stop; Header.StatusText = "대기";
                return;
            }
            var kind = LeakJudge.Of(r.Leak);
            bool ok = kind == StatusKind.Run;
            _barcode.Text = r.Barcode;
            _judge.Text = ok ? "OK" : "NG";
            _judge.Status = kind;
            _judge.SubText = "누설 " + Fmt(r.Leak) + " Pa/s " + (ok ? "≤" : ">") + " 기준 " + Fmt(LeakJudge.Limit);
            _leak.Value = Fmt(r.Leak);
            _leak.Status = kind;
            _leak.Footer = "검사 " + r.Time.ToString("HH:mm:ss");
            Header.Status = kind;
            Header.StatusText = ok ? "가동중" : "NG 발생";
            if (!ok) Alarm.Push(r.Barcode + ": 누설 상한 초과 " + Fmt(r.Leak) + " Pa/s (상한 " + Fmt(LeakJudge.Limit) + ")");
        }

        private void EditLot()
        {
            using (var dlg = new LotInputDialog(_lot.Text, _model.Text, _worker.Text))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                // 실제로는 데이터 인터페이스에 넘긴다. 여기서는 화면만 갱신.
                _lot.Text = dlg.Lot; _model.Text = dlg.Model; _worker.Text = dlg.Worker;
            }
        }

        private void AskAdmin()
        {
            using (var dlg = new AdminPasswordDialog())
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (dlg.Password != SampleAdminPassword) { ApcDialog.Warn(FindForm(), "비밀번호가 맞지 않습니다."); return; }
                ApcDialog.Alert(FindForm(), "관리자 확인이 완료되었습니다. 기준값 변경 화면은 아직 연결되지 않았습니다.");
            }
        }

        private static string Fmt(double v) { return v.ToString("0.0", CultureInfo.InvariantCulture); }

        private static ApcValueCard Card(string title, string unit)
        {
            return new ApcValueCard { Title = title, Unit = unit, Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        }
    }
}
