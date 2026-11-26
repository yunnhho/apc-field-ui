using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CaliperTighteningStation.Data;

namespace CaliperTighteningStation.Views
{
    /// <summary>체결 스테이션 메인 화면: 스캔 → 인터락 판정 → 볼트 체결 → 큰 판정 + 금일 집계 + 이력.</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private readonly IMainViewSource _source;
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly ApcJudgePanel _bypassBanner = new ApcJudgePanel { Dock = DockStyle.Fill, Status = StatusKind.Warn };
        private readonly TableLayoutPanel _bypassRow;
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        private readonly ApcValueCard _total = Card("금일 생산");
        private readonly ApcValueCard _ok = Card("OK");
        private readonly ApcValueCard _ng = Card("NG");
        private readonly ApcGrid _interlock = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _bolts = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcGrid _history = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcButton _bypassButton = Button("인터락 우회 승인", ApcButtonVariant.Danger, 190);

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("브레이크 캘리퍼 체결")
        {
            _source = source;
            _scan.Width = S(420);
            _scan.Scanned += (s, e) => { e.Accepted = e.Code.Length >= 10; if (e.Accepted) Show(_source.Scan(e.Code)); else WarnAfterScan("시리얼 형식이 아닙니다: " + e.Code); };
            _bypassButton.Click += (s, e) => ApproveBypass();

            BuildGrids();
            var scanHint = Caption("툴 NR-05 CH1 · Pset 3 · 볼트 순서 1-3-2-4");
            _bypassRow = Columns(S(80), new Control[] { _bypassBanner });

            var title = SectionTitle("최근 체결 이력"); title.Dock = DockStyle.Top;
            Body.Controls.Add(_history);
            Body.Controls.Add(title);
            Body.Controls.Add(Columns(S(194) + S(12), new Control[] { Section("인터락 판정 (E01~E05)", _interlock), Section("볼트 체결", _bolts) }, new[] { 46f, 54f }));
            Body.Controls.Add(Columns(S(170), new Control[] { _judge, _total, _ok, _ng }, new[] { 46f, 18f, 18f, 18f }));
            Body.Controls.Add(_bypassRow);
            Body.Controls.Add(Row(Caption("시리얼 스캔"), _scan, scanHint, _bypassButton));
            Refresh(_source.Current());
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var f = FindForm();
            if (f != null) _scan.Attach(f);
        }

        private void BuildGrids()
        {
            _interlock.Columns.Add(new ApcLampColumn { Width = S(60) });
            Col(_interlock, "code", "항목", 70);
            Col(_interlock, "name", "검사 내용", 250, true);
            var res = Col(_interlock, "res", "판정 · 사유", 0, true);
            res.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            _bolts.Columns.Add(new ApcLampColumn { Width = S(60) });
            foreach (var c in new[] { "order|순서", "bolt|볼트", "torque|토크(N·m)", "angle|각도(°)", "attempt|시도" })
            {
                var p = c.Split('|');
                Col(_bolts, p[0], p[1], 0).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            var br = Col(_bolts, "res", "판정", 0, true);
            br.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            _history.Columns.Add(new ApcLampColumn { Width = S(60) });
            Col(_history, "time", "시각", 120);
            Col(_history, "serial", "시리얼", 300);
            Col(_history, "res", "결과", 140);
            Col(_history, "note", "내용", 0, true).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private static DataGridViewColumn Col(ApcGrid g, string name, string header, int width, bool left = false)
        {
            int i = g.Columns.Add(name, header);
            var c = g.Columns[i];
            if (width > 0) c.Width = S(width);
            if (left) c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            return c;
        }

        private static Panel Section(string title, Control body)
        {
            var p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
            var t = SectionTitle(title); t.Dock = DockStyle.Top;
            p.Controls.Add(body);
            p.Controls.Add(t);
            return p;
        }

        private static ApcValueCard Card(string title)
        {
            return new ApcValueCard { Title = title, Unit = "EA", Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        }

        // 화면 전체를 데이터(스냅샷·집계·이력·우회 상태)에서 다시 채운다.
        private void Refresh(StationSnapshot snap)
        {
            Show(snap);
            _total.Value = _source.TodayTotal.ToString("#,0");
            _ok.Value = _source.TodayOk.ToString("#,0");
            _ng.Value = _source.TodayNg.ToString("#,0");
            _ng.Status = _source.TodayNg > 0 ? StatusKind.Alarm : StatusKind.Run;
            _history.Rows.Clear();
            foreach (var h in _source.History())
                _history.Rows.Add(h.Status, h.Time.ToString("HH:mm:ss"), h.Serial, h.Result, h.Note);
            for (int i = 0; i < _history.Rows.Count; i++)
                if (_history.Rows[i].Cells["res"].Value.ToString() != "OK")
                {
                    var c = _history.Rows[i].Cells["res"];
                    c.Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                    c.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
                }
            ShowBypass();
        }

        private void Show(StationSnapshot snap)
        {
            if (snap.Serial.Length > 0 && _scan.Text != snap.Serial) _scan.Text = snap.Serial;
            var f = Spec.Final(snap);
            _judge.Text = f.Text; _judge.SubText = f.SubText; _judge.Status = f.Status;

            _interlock.Rows.Clear();
            foreach (var i in snap.Interlocks)
            {
                int r = _interlock.Rows.Add(i.Status, i.Code, i.Name, i.Message);
                if (i.Status != StatusKind.Run) Emphasize(_interlock.Rows[r].Cells["res"], i.Status);
            }

            _bolts.Rows.Clear();
            if (snap.Bolts.Count == 0) { _bolts.Rows.Add(StatusKind.Stop, "", "", "", "", "", "인터락 NG · 체결 불가"); return; }
            foreach (var b in snap.Bolts)
            {
                string reason = b.Reason;
                StatusKind st = !b.HasResult ? StatusKind.Stop : reason == null ? StatusKind.Run : StatusKind.Alarm;
                string text = !b.HasResult ? "대기" : reason == null ? "OK" : "NG · " + reason;
                int r = _bolts.Rows.Add(st, b.Order, b.Bolt,
                    b.Torque.HasValue ? Spec.Torque(b.Torque.Value) : "",
                    b.Angle.HasValue ? b.Angle.Value.ToString(CultureInfo.InvariantCulture) : "",
                    b.Attempt == 0 ? "" : b.Attempt.ToString(), text);
                if (st == StatusKind.Alarm)
                {
                    Emphasize(_bolts.Rows[r].Cells["res"], st);
                    if (b.Torque < Spec.TorqueMin || b.Torque > Spec.TorqueMax) Emphasize(_bolts.Rows[r].Cells["torque"], st);
                    if (b.Angle < Spec.AngleMin || b.Angle > Spec.AngleMax) Emphasize(_bolts.Rows[r].Cells["angle"], st);
                }
            }
        }

        private static void Emphasize(DataGridViewCell cell, StatusKind kind)
        {
            cell.Style.ForeColor = ApcTheme.StatusText(kind);
            cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
        }

        private void ShowBypass()
        {
            var b = _source.GetBypass();
            bool on = b != null;   // 만료 자동 해제는 데이터 쪽이 GetBypass()를 null로 돌려준다
            _bypassRow.Visible = on;
            if (!on) return;
            _bypassBanner.Text = "인터락 " + b.Items + " 우회 중";
            _bypassBanner.SubText = b.Until.ToString("HH:mm") + "까지 · 생산분은 추가 검사 대상";
        }

        private void ApproveBypass()
        {
            using (var dlg = new BypassApprovalDialog(_source.VerifyAdmin, "E05", ""))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.StartBypass(dlg.Item, dlg.Minutes, dlg.Reason);
                Refresh(_source.Current());
            }
        }

        private void WarnAfterScan(string message)
        {
            BeginInvoke(new Action(() => ApcDialog.Warn(FindForm(), message)));
        }
    }
}
