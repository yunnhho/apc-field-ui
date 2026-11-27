using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 밸브 조작 dialog. 선택한 밸브 정보는 읽기 전용이고, 동작은 현재 상태의 반대(열림 → 닫기, 닫힘 → 열기) 하나.
    /// 인터락(FlowRules.BlockReason)에 걸리면 확정 버튼이 꺼지고 이유가 버튼 왼쪽에 나온다. 막힘을 풀 입력칸은 없다.
    /// 사원증은 스캔으로만 받는다(EMP-nnnnn).
    /// </summary>
    internal sealed class ValveDialog : ApcForm
    {
        private static readonly Regex BadgePattern = new Regex(@"^EMP-\d{5}$");

        private readonly ApcScanBox _badge = new ApcScanBox { ScanOnly = true };
        private readonly ApcButton _confirm;
        private readonly Label _note;
        private readonly string _blockReason;

        public ValveDialog() : this(new SampleMainViewSource()) { }

        private ValveDialog(SampleMainViewSource src) : this(src.GetValves()[2], src.GetTanks(), src.GetPumps()) { }

        public ValveDialog(ValveInfo valve, IList<TankInfo> tanks, IList<PumpInfo> pumps)
        {
            SetupDialog("밸브 조작", 560);
            TargetOpen = !valve.IsOpen;
            _blockReason = FlowRules.BlockReason(valve, tanks, pumps);

            var info = new ApcInfoStrip();
            info.SetItems("밸브", "위치", "현재");
            info.SetValue("밸브", valve.Id);
            info.SetValue("위치", valve.Position);
            info.SetValue("현재", valve.IsOpen ? "열림" : "닫힘");

            var tank = valve.DownstreamTankId == null ? null : FlowRules.FindTank(tanks, valve.DownstreamTankId);
            var pump = valve.PumpId == null ? null : FlowRules.FindPump(pumps, valve.PumpId);
            var line = new ApcInfoStrip();
            line.SetItems("하류 탱크", "레벨", "펌프");
            line.SetValue("하류 탱크", tank == null ? "출하" : tank.Id);
            line.SetValue("레벨", tank == null ? "" : tank.Level + " %");
            line.SetValue("펌프", pump == null ? "" : pump.Id + (pump.Running ? " 가동" : " 정지"));

            AddContent(SectionTitle("선택한 밸브"));
            AddContent(info);
            AddContent(line);
            AddContent(SectionTitle("작업자"));
            AddFieldRow("사원증 스캔", _badge);

            _confirm = new ApcButton { Text = TargetOpen ? "밸브 열기" : "밸브 닫기", Variant = ApcButtonVariant.Primary };
            _confirm.Click += (s, e) => { if (CanConfirm()) DialogResult = DialogResult.OK; };
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm);
            _note = SetFooterNote("");

            _badge.Accepts = c => c.StartsWith("EMP-");
            _badge.Attach(this);
            _badge.Scanned += OnBadgeScanned;
            UpdateState();
        }

        /// <summary>true = 열기, false = 닫기.</summary>
        public bool TargetOpen { get; private set; }
        public string OperatorId { get { return _badge.Text; } }

        private bool CanConfirm() { return _blockReason == null && BadgePattern.IsMatch(_badge.Text); }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            if (!BadgePattern.IsMatch(e.Code))
            {
                e.Accepted = false;
                BeginInvoke(new MethodInvoker(() => ApcDialog.Warn(this, "사원증 형식이 아닙니다 (EMP-nnnnn).")));
            }
            UpdateState();
        }

        private void UpdateState()
        {
            _confirm.Enabled = CanConfirm();
            if (_blockReason != null)
            {
                _note.Text = _blockReason;
                _note.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            }
            else
            {
                _note.Text = BadgePattern.IsMatch(_badge.Text) ? "" : "사원증 스캔 필요";
                _note.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text);
            }
        }
    }
}
