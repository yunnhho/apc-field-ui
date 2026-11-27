using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using DiscFinalCheck.Data;

namespace DiscFinalCheck.Views
{
    /// <summary>
    /// 브레이크 디스크 최종 치수 검사 판정 화면: 스캔 → 판정판(OK/NG·이유) → NG 처리 등록 버튼 → 측정 4항목 → 오늘 집계 → 오늘 이력.
    /// 폐기 등록된 시리얼은 스캔해도 검사하지 않고 판정판이 이유를 보여 준다.
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private static readonly Regex SerialPattern = new Regex(@"^BD-\d{6}-\d{4}$");
        private static readonly InspectItem[] Items = { InspectItem.OuterDiameter, InspectItem.Thickness, InspectItem.Runout, InspectItem.Parallelism };
        private const string PendingNgReason = "NG 처리 등록 후 다음 검사";
        private const string NoNgReason = "NG 판정 후 등록할 수 있습니다";

        private readonly IMainViewSource _source;
        private readonly ApcScanBox _scan = new ApcScanBox { Width = S(320), ScanOnly = true };
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcButton _register = Button("NG 처리 등록", ApcButtonVariant.Primary, 200);
        private readonly Label _registerReason = Caption("");
        private readonly ApcValueCard[] _cards = new ApcValueCard[4];
        private readonly ApcValueCard _total = Card("검사 수", "EA", "");
        private readonly ApcValueCard _ok = Card("OK", "EA", "");
        private readonly ApcValueCard _ng = Card("NG", "EA", "");
        private readonly ApcValueCard _rate = Card("불량률", "%", "기준 " + DiscSpec.DefectRateWarnPercent.ToString("0.0", CultureInfo.InvariantCulture) + " 이하");
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private DayCounts _counts;
        private DiscMeasurement _pendingNg;   // 처리 등록을 기다리는 마지막 NG. 없으면 null.

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("브레이크 디스크 최종 치수 검사")
        {
            _source = source;
            _counts = source.GetTodayCounts();
            _judge.Margin = new Padding(0);

            for (int i = 0; i < Items.Length; i++)
                _cards[i] = Card(DiscSpec.ItemName(Items[i]), "mm", DiscSpec.LimitText(Items[i]));

            BuildGrid();
            foreach (DiscMeasurement m in source.GetTodayHistory()) AddHistoryRow(m, false);

            var history = source.GetTodayHistory();
            if (history.Count > 0) ShowResult(history[0]); else ShowReady();
            ShowCounts();

            // Fill(이력)을 먼저, Top 줄은 화면 아래쪽 것부터
            Body.Controls.Add(Titled("오늘 검사 이력", _grid));
            Body.Controls.Add(Columns(S(110), new Control[] { _total, _ok, _ng, _rate }));
            Body.Controls.Add(Columns(S(120), _cards));
            Body.Controls.Add(Row(_register, _registerReason));
            Body.Controls.Add(Columns(S(190), new Control[] { _judge }));
            Body.Controls.Add(Row(Caption("디스크 시리얼"), _scan));

            _scan.Scanned += (s, e) => OnScanned(e);
            _register.Click += (s, e) => RegisterNg();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);   // 생성자 시점엔 폼이 없다
        }

        private void BuildGrid()
        {
            _grid.Columns.Add(new ApcLampColumn());
            _grid.Columns.Add("time", "시각");
            _grid.Columns.Add("serial", "시리얼");
            _grid.Columns.Add("od", "외경(mm)");
            _grid.Columns.Add("th", "두께(mm)");
            _grid.Columns.Add("ru", "런아웃(mm)");
            _grid.Columns.Add("pa", "평행도(mm)");
            _grid.Columns.Add("result", "판정");
            // 짧은 값 열뿐이므로 램프 열을 뺀 모든 열을 Fill
            foreach (DataGridViewColumn c in _grid.Columns)
                if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void AddHistoryRow(DiscMeasurement m, bool atTop)
        {
            DiscVerdict v = DiscSpec.Evaluate(m);
            string result = v.IsOk ? "OK" : "NG · " + FailedNames(v);
            object[] cells = new object[]
            {
                v.IsOk ? StatusKind.Run : StatusKind.Alarm, m.Time.ToString("HH:mm:ss"), m.Serial,
                DiscSpec.Format(InspectItem.OuterDiameter, m.OuterDiameter), DiscSpec.Format(InspectItem.Thickness, m.Thickness),
                DiscSpec.Format(InspectItem.Runout, m.Runout), DiscSpec.Format(InspectItem.Parallelism, m.Parallelism), result
            };
            int index;
            if (atTop) { _grid.Rows.Insert(0, cells); index = 0; } else index = _grid.Rows.Add(cells);
            for (int i = 0; i < Items.Length; i++)
            {
                if (!v.Fails(Items[i])) continue;
                var style = _grid.Rows[index].Cells[3 + i].Style;
                style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                style.Font = ApcTheme.GetFont(System.Drawing.FontStyle.Bold);
            }
        }

        private static string FailedNames(DiscVerdict v)
        {
            var names = new List<string>();
            foreach (InspectItem item in v.Failed) names.Add(DiscSpec.ItemName(item));
            return string.Join("·", names.ToArray());
        }

        private void OnScanned(ScanEventArgs e)
        {
            string code = e.Code.Trim();
            if (!SerialPattern.IsMatch(code))
            {
                e.Accepted = false;
                WarnAfterScan("시리얼 형식이 아닙니다 · BD-yyMMdd-nnnn");
                return;
            }
            if (_pendingNg != null)
            {
                e.Accepted = false;   // 막는다: NG 처리 등록 전에는 다음 디스크를 검사하지 않는다(이유는 버튼 옆 글)
                _registerReason.Text = PendingNgReason;
                return;
            }
            NgRecord record = _source.GetNgRecord(code);
            if (record != null && record.Action == NgAction.Scrap)
            {
                e.Accepted = false;   // 막는다: 폐기 등록된 디스크는 검사하지 않는다
                ShowBlocked(code, record);
                return;
            }
            DiscMeasurement m = _source.Measure(code);
            _source.SaveInspection(m);
            AddHistoryRow(m, true);
            _counts = _source.GetTodayCounts();
            ShowResult(m);
            ShowCounts();
        }

        private void ShowReady()
        {
            _judge.Text = "READY"; _judge.Status = StatusKind.Stop; _judge.SubText = "시리얼을 스캔하세요";
            ClearCards();
            SetRegister(null);
        }

        private void ShowBlocked(string serial, NgRecord record)
        {
            _judge.Text = "검사 불가"; _judge.Status = StatusKind.Alarm;
            _judge.SubText = serial + " · 폐기 등록됨 (" + record.Time.ToString("HH:mm:ss") + ")";
            ClearCards();
            SetRegister(null);
        }

        private void ShowResult(DiscMeasurement m)
        {
            DiscVerdict v = DiscSpec.Evaluate(m);
            _judge.Text = v.IsOk ? "OK" : "NG";
            _judge.Status = v.IsOk ? StatusKind.Run : StatusKind.Alarm;
            _judge.SubText = v.IsOk ? m.Serial : m.Serial + " · " + v.Reason;
            for (int i = 0; i < Items.Length; i++)
            {
                decimal value = m.ValueOf(Items[i]);
                _cards[i].Value = DiscSpec.Format(Items[i], value);
                _cards[i].Status = DiscSpec.KindOf(Items[i], value);
            }
            SetRegister(v.IsOk ? null : m);
        }

        private void ClearCards()
        {
            foreach (ApcValueCard c in _cards) { c.Value = ""; c.Status = StatusKind.Stop; }
        }

        /// <summary>NG 처리 등록 버튼 상태: 등록 전 NG면 켜고, 아니면 끄고 이유를 옆에 보인다.</summary>
        private void SetRegister(DiscMeasurement ng)
        {
            _pendingNg = null;
            NgRecord done = ng == null ? null : _source.GetNgRecord(ng.Serial);
            if (ng != null && done == null) _pendingNg = ng;
            _register.Enabled = _pendingNg != null;
            _registerReason.Text = _pendingNg != null ? PendingNgReason : done != null ? "처리 등록됨 · " + DiscSpec.ActionName(done.Action) : NoNgReason;
        }

        private void RegisterNg()
        {
            if (_pendingNg == null) return;
            using (var dlg = new NgRegisterDialog(_pendingNg))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.SaveNgRecord(new NgRecord(_pendingNg.Serial, dlg.Disposition, dlg.SupervisorBadgeId, DateTime.Now));
                SetRegister(_pendingNg);
            }
        }

        private void ShowCounts()
        {
            _total.Value = _counts.Total.ToString("#,0", CultureInfo.InvariantCulture);
            _ok.Value = _counts.Ok.ToString("#,0", CultureInfo.InvariantCulture);
            _ng.Value = _counts.Ng.ToString("#,0", CultureInfo.InvariantCulture);
            _ng.Status = StatusKind.Run;   // 하루 집계라 테두리 없이 글자색만
            _ng.ValueStatus = _counts.Ng > 0 ? StatusKind.Alarm : (StatusKind?)null;
            double rate = _counts.Total == 0 ? 0 : 100.0 * _counts.Ng / _counts.Total;
            _rate.Value = _counts.Total == 0 ? "" : rate.ToString("0.0", CultureInfo.InvariantCulture);
            _rate.Status = _counts.Total == 0 ? StatusKind.Stop : rate > DiscSpec.DefectRateWarnPercent ? StatusKind.Warn : StatusKind.Run;
        }

        // 스캔 이벤트 안에서 바로 경고창을 띄우면 스캔 처리가 막히므로 처리 후에 띄운다.
        private void WarnAfterScan(string message)
        {
            BeginInvoke(new Action(() => ApcDialog.Warn(FindForm(), message)));
        }

        private static ApcValueCard Card(string title, string unit, string footer)
        {
            return new ApcValueCard { Title = title, Unit = unit, Footer = footer, Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        }
    }
}
