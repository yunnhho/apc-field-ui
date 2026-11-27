using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using SpotWeldMonitor.Data;

namespace SpotWeldMonitor.Views
{
    /// <summary>
    /// 셀 상세: 셀 정보(읽기 전용) + 최근 1시간 전류 추이 + 팁 사용/한계 + 팁 교체 등록(사원증 스캔).
    /// 가동 중인 셀은 등록을 막는다(값은 호출한 화면이 넘긴다 — dialog에서 바꿀 수 없다).
    /// 매개변수 없는 생성자는 render용 샘플: 가동 중 셀 → 등록이 막힌 상태.
    /// </summary>
    internal sealed class CellDetailDialog : ApcForm
    {
        private static readonly Regex BadgePattern = new Regex(@"^EMP-\d{5}$");

        private readonly string _cellName;
        private readonly ApcScanBox _badge = new ApcScanBox { ScanOnly = true };
        private readonly ApcButton _register = new ApcButton { Text = "팁 교체 등록", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;
        private readonly bool _blocked;

        public CellDetailDialog() : this(new SampleMainViewSource().GetCells()[1], new SampleMainViewSource().GetCurrentTrend("WR-02")) { }

        public CellDetailDialog(WeldCell cell, double[] trend)
        {
            _cellName = cell.Name;
            _blocked = cell.State == CellState.Running;
            SetupDialog(cell.Name + " 셀 상세", 820, 780);

            var info = new ApcInfoStrip { Height = ApcTheme.Px(40) };
            info.SetItems("셀", "상태", "오늘 타점", "최근 전류(kA)");
            info.SetValue("셀", cell.Name);
            info.SetValue("상태", WeldRules.StateWord(cell.State));
            info.SetValue("오늘 타점", WeldRules.Count(cell.ShotsToday));
            info.SetValue("최근 전류(kA)", cell.LastCurrentKa.HasValue ? WeldRules.Ka(cell.LastCurrentKa.Value) : "");
            var tip = new ApcInfoStrip { Height = ApcTheme.Px(40) };
            tip.SetItems("팁 사용 타점", "한계 타점", "드레싱 시작");
            tip.SetValue("팁 사용 타점", WeldRules.Count(cell.TipShots));
            tip.SetValue("한계 타점", WeldRules.Count(WeldRules.TipLimitShots));
            tip.SetValue("드레싱 시작", WeldRules.Count(WeldRules.TipDressingShots));

            var chart = new ApcTrendChart
            {
                Reference = (WeldRules.CurrentMinKa + WeldRules.CurrentMaxKa) / 2,
                UpperLimit = WeldRules.CurrentMaxKa,
                LowerLimit = WeldRules.CurrentMinKa,
                XLabels = new[] { "-60분", "-30분", "현재" },
            };
            chart.SetValues(trend);

            _badge.Attach(this);
            _badge.Accepts = c => BadgePattern.IsMatch(c);
            _badge.Scanned += OnBadgeScanned;

            AddContent(info);
            AddContent(tip);
            AddContent(SectionTitle("최근 1시간 용접 전류 (kA, 점선 = 기준 " + WeldRules.Ka(WeldRules.CurrentMinKa) + "~" + WeldRules.Ka(WeldRules.CurrentMaxKa) + ")"));
            AddContentFill(chart);
            AddContent(SectionTitle("팁 교체 등록"));
            AddFieldRow("작업자 사원증", _badge);

            _register.Click += OnRegister;
            var close = new ApcButton { Text = "닫기", DialogResult = DialogResult.Cancel };
            SetFooter(close, _register);
            _note = SetFooterNote("");
            UpdateRegister();
        }

        /// <summary>등록한 작업자 사원증(등록 성공 후 호출한 화면이 읽는다).</summary>
        public string Badge { get { return _badge.Text; } }
        public string CellName { get { return _cellName; } }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            e.Accepted = BadgePattern.IsMatch(e.Code);
            if (!e.Accepted) BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(this, "사원증 형식이 아닙니다: " + e.Code)));
            UpdateRegister();
        }

        private void UpdateRegister()
        {
            bool hasBadge = BadgePattern.IsMatch(_badge.Text);
            _register.Enabled = !_blocked && hasBadge;
            _note.Text = _blocked ? "가동 중 · 팁 교체 등록 불가" : (hasBadge ? "" : "사원증 스캔 필요");
        }

        private void OnRegister(object sender, System.EventArgs e)
        {
            if (!ApcDialog.Confirm(this, _cellName + " 팁 교체를 등록합니다. (" + _badge.Text + ")", "등록", ApcButtonVariant.Primary)) return;
            DialogResult = DialogResult.OK;
        }
    }
}
