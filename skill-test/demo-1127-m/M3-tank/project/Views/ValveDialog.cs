using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 밸브 조작 dialog. 밸브 정보는 읽기 전용, 열기/닫기 선택, 사원증 스캔으로 조작자 기록.
    /// 인터락(하류 레벨·펌프 가동)은 데이터에서 판정한다 — 화면의 선택으로 풀 수 없다. 막히면 [실행]이 꺼지고 이유가 글로 나온다.
    /// 샘플은 막힌 상태(V5 열기, T4 91 %)로 시작한다.
    /// </summary>
    internal sealed class ValveDialog : ApcForm
    {
        private static readonly Regex Badge = new Regex(@"^EMP-\d{5}$");

        private readonly ValveInfo _valve;
        private readonly IList<TankInfo> _tanks;
        private readonly IList<PumpInfo> _pumps;
        private readonly ApcTabStrip _tabs = new ApcTabStrip();
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly ApcButton _run = new ApcButton { Variant = ApcButtonVariant.Primary };
        private readonly Label _note;

        public ValveDialog() : this(new SampleMainViewSource()) { }

        private ValveDialog(SampleMainViewSource s) : this(s.GetValves()[4], s.GetTanks(), s.GetPumps()) { }

        public ValveDialog(ValveInfo valve, IList<TankInfo> tanks, IList<PumpInfo> pumps)
        {
            _valve = valve; _tanks = tanks; _pumps = pumps;
            SetupDialog("밸브 조작", 640);

            var info = new ApcInfoStrip();
            info.SetItems("밸브", "위치", "현재 상태");
            info.SetValue("밸브", valve.Id);
            info.SetValue("위치", valve.Position);
            info.SetValue("현재 상태", valve.IsOpen ? "열림" : "닫힘");
            var last = new ApcInfoStrip();
            last.SetItems("마지막 조작", "조작자");
            last.SetValue("마지막 조작", valve.LastTime.ToString("HH:mm:ss"));
            last.SetValue("조작자", valve.Operator);
            AddContent(info);
            AddContent(last);

            _tabs.SetTabs("열기", "닫기");
            _tabs.Width = _tabs.PreferredTabsWidth;
            _tabs.SelectedIndex = valve.IsOpen ? 1 : 0;   // 기본 = 지금 상태의 반대
            _tabs.SelectedIndexChanged += (s, e) => UpdateState();
            AddFieldRow("조작", _tabs);

            _scan.Attach(this);
            _scan.Scanned += OnScanned;
            AddFieldRow("작업자 사원증", _scan);

            _run.Click += (s, e) => { if (CanRun()) DialogResult = DialogResult.OK; };
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _run);   // Enter는 스캔이 쓴다 → AcceptButton 없음
            _note = SetFooterNote();
            UpdateState();
        }

        public bool WantOpen { get { return _tabs.SelectedIndex == 0; } }
        public string Operator { get; private set; }

        private string Reason() { return ProcessRules.BlockReason(_valve, WantOpen, _tanks, _pumps); }
        private bool CanRun() { return Reason() == null && Operator != null; }

        private void OnScanned(object sender, ScanEventArgs e)
        {
            if (!Badge.IsMatch(e.Code))
            {
                e.Accepted = false;
                string code = e.Code;
                BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(this, "사원증이 아닙니다: " + code)));
                return;
            }
            Operator = e.Code;
            UpdateState();
        }

        private void UpdateState()
        {
            _run.Text = WantOpen ? "열기 실행" : "닫기 실행";
            string reason = Reason();
            if (reason != null)
            {
                _note.Text = reason;
                _note.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            }
            else
            {
                _note.Text = Operator == null ? "사원증을 스캔하세요" : "";
                _note.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text);
            }
            _run.Enabled = CanRun();
        }
    }
}
