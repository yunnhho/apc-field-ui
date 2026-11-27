using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using RubberWeighing.Data;

namespace RubberWeighing.Views
{
    /// <summary>
    /// 고무 배합 원료 계량: 저울 현재값 판정판 → LOT 스캔 + 계량 확정 → 진행 막대 → 배치 정보 → 원료 순서 목록.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int ScaleReadMs = 500;
        private const decimal ScaleSpan = 0.05m;   // 진행 막대 구간 = 목표 ±5 %
        private static readonly Regex LotPattern = new Regex(@"^ML-([A-Z]+)-(\d{6})-(\d{2})$");

        private readonly IMainViewSource _source;
        private readonly IList<Material> _recipe;
        private readonly List<WeighRecord> _done = new List<WeighRecord>();
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcScanBox _scan = new ApcScanBox { ScanOnly = true, Width = S(380) };
        private readonly ApcButton _confirm;
        private readonly Label _reason = Caption("");
        private readonly WeighBar _bar = new WeighBar { Dock = DockStyle.Fill };
        private readonly ApcInfoStrip _strip = new ApcInfoStrip { Dock = DockStyle.Fill };
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true, ClearInitialSelection = false, FillRows = true };
        private readonly Timer _timer = new Timer { Interval = ScaleReadMs };
        private string _lot;          // 현재 원료에 스캔된 LOT, 없으면 null
        private decimal _weight;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("고무 배합 원료 계량")
        {
            _source = source;
            _recipe = source.GetRecipe();
            _done.AddRange(source.GetDoneRecords());
            _confirm = Button("계량 확정", ApcButtonVariant.Primary, 170);

            _strip.SetItems("레시피", "배치 번호", "배치 진행");
            _strip.SetValue("레시피", source.RecipeName);
            _strip.SetValue("배치 번호", source.BatchNo);

            _grid.Columns.Add(new ApcLampColumn());
            _grid.Columns.Add("no", "순번");
            _grid.Columns.Add("name", "원료");
            _grid.Columns.Add("target", "목표(kg)");
            _grid.Columns.Add("range", "허용 범위(kg)");
            _grid.Columns.Add("measured", "실측(kg)");
            _grid.Columns.Add("verdict", "판정");
            _grid.Columns.Add("time", "확정 시각");
            foreach (DataGridViewColumn c in _grid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (Material m in _recipe)
                _grid.Rows.Add(StatusKind.Stop, m.No, m.Name, m.Fmt(m.Target), m.RangeText, "", "", "");

            // 이어하기: 이미 LOT가 스캔된 원료가 있으면 그 상태로 시작한다.
            if (_source.ActiveLot != null && Current != null)
            {
                _lot = _source.ActiveLot;
                _scan.Text = _lot;
                _weight = _source.ReadScale();
            }

            // Top 도킹은 나중에 Add한 것이 위 → Fill 먼저, 그다음 아래 줄부터
            Body.Controls.Add(Titled("원료 순서", _grid));
            Body.Controls.Add(Columns(_strip.Height, new Control[] { _strip }));
            Body.Controls.Add(Columns(S(110), new Control[] { _bar }));
            Body.Controls.Add(Row(Caption("원료 LOT"), _scan, _confirm, _reason));
            Body.Controls.Add(Columns(S(210), new Control[] { _judge }));
            Reload();

            _scan.Scanned += OnLotScanned;
            _confirm.Click += (s, e) => ConfirmWeighing();
            _timer.Tick += (s, e) => { if (_lot != null && Current != null) { _weight = _source.ReadScale(); Reload(); } };
        }

        private Material Current { get { return _done.Count < _recipe.Count ? _recipe[_done.Count] : null; } }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);   // 생성자 시점엔 폼이 없다
            _timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }

        private void OnLotScanned(object sender, ScanEventArgs e)
        {
            string problem = CheckLot(e.Code);
            e.Accepted = problem == null;
            if (problem != null) { WarnAfterScan(problem); return; }
            Material m = Current;
            bool replacing = _lot != null;   // 같은 원료의 다른 LOT는 교체로 받는다(계량 값 유지)
            _lot = e.Code;
            if (!replacing) { _source.StartWeighing(m); _weight = _source.ReadScale(); }
            Reload();
        }

        /// <summary>스캔한 LOT가 지금 계량할 원료와 맞는지. 거부 사유를 돌려주고, 맞으면 null.</summary>
        private string CheckLot(string code)
        {
            Material m = Current;
            if (m == null) return "모든 원료 계량 완료 · 스캔 불필요";
            Match lot = LotPattern.Match(code);
            DateTime d;
            if (!lot.Success || !DateTime.TryParseExact(lot.Groups[2].Value, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return "원료 LOT 형식 오류 · " + code;
            if (lot.Groups[1].Value != m.Code)
                return "원료 LOT 불일치 · 스캔 " + lot.Groups[1].Value + " ≠ 필요 " + m.Code + " (" + m.Name + ")";
            return null;
        }

        private void WarnAfterScan(string message)
        {
            BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(FindForm(), message)));
        }

        private void ConfirmWeighing()
        {
            Material m = Current;
            if (m == null || _lot == null) return;
            decimal snapshot = _weight;   // 확정 대상 값은 누른 순간 값으로 고정
            if (m.Judge(snapshot) != WeighVerdict.Ok) return;
            using (var dlg = new WeighConfirmDialog(m, _lot, snapshot))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                var record = new WeighRecord(m.No, _lot, snapshot, dlg.OperatorId, DateTime.Now);
                _source.Record(record);
                _done.Add(record);
            }
            _lot = null;
            _weight = 0m;
            _scan.Text = "";
            Reload();
            _scan.Focus();
        }

        private void Reload()
        {
            Material m = Current;
            bool armed = m != null && _lot != null;
            Material shown = m ?? _recipe[_recipe.Count - 1];
            _strip.SetValue("배치 진행", _done.Count + " / " + _recipe.Count);
            ShowJudge(m, armed);
            ShowConfirm(m, armed);
            decimal lo = shown.Target * (1 - ScaleSpan), hi = shown.Target * (1 + ScaleSpan);
            _bar.SetState(shown.Lower, shown.Upper, lo, hi, armed ? (decimal?)_weight : null, VerdictKind(armed ? shown.Judge(_weight) : WeighVerdict.Ok),
                shown.Fmt(lo) + " kg", "허용 " + shown.RangeText + " kg", shown.Fmt(hi) + " kg");
            ShowGrid();
        }

        private static StatusKind VerdictKind(WeighVerdict v)
        {
            return v == WeighVerdict.Ok ? StatusKind.Run : v == WeighVerdict.Under ? StatusKind.Stop : StatusKind.Alarm;
        }

        private void ShowJudge(Material m, bool armed)
        {
            if (m == null)
            {
                _judge.Text = "배치 완료"; _judge.Status = StatusKind.Run;
                _judge.SubText = _source.RecipeName + " · 원료 " + _recipe.Count + "종 계량 완료";
                return;
            }
            string head = m.Name + " 목표 " + m.Fmt(m.Target) + " kg (허용 " + m.RangeText + ")";
            if (!armed)
            {
                _judge.Text = "원료 LOT 스캔"; _judge.Status = StatusKind.Stop;
                _judge.SubText = head + " · 스캔해야 계량 시작";
                return;
            }
            WeighVerdict v = m.Judge(_weight);
            _judge.Text = m.Fmt(_weight) + " kg";
            _judge.Status = VerdictKind(v);
            _judge.SubText = head + " · " + (v == WeighVerdict.Ok ? "허용 범위 안" : v == WeighVerdict.Under ? "투입 중" : "상한 초과");
        }

        private void ShowConfirm(Material m, bool armed)
        {
            WeighVerdict v = armed ? m.Judge(_weight) : WeighVerdict.Ok;
            _confirm.Enabled = armed && v == WeighVerdict.Ok;
            _reason.Text = m == null ? "계량할 원료 없음" : !armed ? "원료 LOT 스캔 필요" : v == WeighVerdict.Ok ? "" : "허용 범위 밖 · 확정 불가";
            _reason.ForeColor = v == WeighVerdict.Over ? ApcTheme.StatusText(StatusKind.Alarm) : ApcTheme.C(ApcTheme.Current.Colors.Text);
        }

        private void ShowGrid()
        {
            for (int i = 0; i < _recipe.Count; i++)
            {
                Material m = _recipe[i];
                DataGridViewCellCollection c = _grid.Rows[i].Cells;
                bool done = i < _done.Count, current = i == _done.Count;
                WeighRecord rec = done ? _done[i] : null;
                c[0].Value = done ? StatusKind.Run : current ? StatusKind.Warn : StatusKind.Stop;
                c[5].Value = done ? m.Fmt(rec.Measured) : "";
                c[6].Value = done ? "OK" : current ? (_lot != null ? "계량 중" : "LOT 대기") : "대기";
                c[7].Value = done ? rec.Time.ToString("HH:mm:ss") : "";
                _grid.Rows[i].DefaultCellStyle.Font = current ? ApcTheme.GetFont(FontStyle.Bold) : null;
            }
            _grid.ClearSelection();
            if (_done.Count < _recipe.Count) _grid.Rows[_done.Count].Selected = true;
        }
    }
}
