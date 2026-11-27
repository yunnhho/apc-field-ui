using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 밸브 조작 dialog: 선택한 밸브 정보는 읽기 전용, 현재 상태의 반대 동작(열림이면 닫기, 닫힘이면 열기) 하나만 한다.
    /// 인터락(하류 레벨 알람 시 열기 / 흡입 펌프 가동 시 닫기)과 사원증 미스캔이면 확정 버튼이 꺼지고 이유를 보인다.
    /// 인터락은 데이터(snapshot)로만 정해진다 — 이 창에서 바꿀 수 있는 선택지는 없다.
    /// </summary>
    internal sealed class ValveDialog : ApcForm
    {
        private readonly ValveInfo _valve;
        private readonly string _blockReason;
        private readonly ApcScanBox _badge = new ApcScanBox();
        private readonly ApcButton _confirm;
        private readonly Label _note;

        // render용: V1 닫기(P1 가동 중이라 막힌 상태)
        public ValveDialog() : this(new SampleMainViewSource().GetSnapshot(), "V1") { }

        public ValveDialog(ProcessSnapshot snap, string valveId)
        {
            _valve = snap.Valve(valveId);
            WantOpen = !_valve.IsOpen;
            _blockReason = ProcessRules.BlockReason(_valve, snap, WantOpen);

            SetupDialog("밸브 조작", 600);

            TankInfo down = _valve.DownstreamTankId == null ? null : snap.Tank(_valve.DownstreamTankId);
            var strip1 = new ApcInfoStrip();
            strip1.SetItems("밸브", "위치", "현재");
            strip1.SetValue("밸브", _valve.Id);
            strip1.SetValue("위치", _valve.Position);
            strip1.SetValue("현재", _valve.IsOpen ? "열림" : "닫힘");
            var strip2 = new ApcInfoStrip();
            strip2.SetItems("하류 탱크", "흡입 펌프");
            strip2.SetValue("하류 탱크", down == null ? "출하" : down.Id + "  " + down.Level + " %");
            PumpInfo pump = _valve.SuctionPumpId == null ? null : snap.Pump(_valve.SuctionPumpId);
            strip2.SetValue("흡입 펌프", pump == null ? "해당 없음" : pump.Id + "  " + (pump.Running ? "가동" : "정지"));
            AddContent(SectionTitle("선택한 밸브"));
            AddContent(strip1);
            AddContent(strip2);

            AddContent(SectionTitle("작업자"));
            _badge.ScanOnly = true;
            _badge.Accepts = c => c.StartsWith("EMP-");
            _badge.Scanned += OnBadgeScanned;
            AddFieldRow("작업자 사원증", _badge);
            _badge.Attach(this);

            _confirm = new ApcButton { Text = WantOpen ? "밸브 열기" : "밸브 닫기", Variant = ApcButtonVariant.Primary };
            _confirm.Click += (s, e) => { OperatorId = _badge.Text; DialogResult = DialogResult.OK; };
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm);
            _note = SetFooterNote("");
            UpdateState();
        }

        public bool WantOpen { get; private set; }
        public string OperatorId { get; private set; }
        public string ValveId { get { return _valve.Id; } }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            bool ok = ProcessRules.IsBadge(e.Code);
            e.Accepted = ok;
            UpdateState();
            if (!ok) BeginInvoke(new Action(() => ApcDialog.Warn(this, "사원증이 아닙니다 (EMP-nnnnn 형식만 받습니다).")));
        }

        private void UpdateState()
        {
            bool badgeOk = ProcessRules.IsBadge(_badge.Text);
            _confirm.Enabled = _blockReason == null && badgeOk;
            if (_blockReason != null)
            {
                _note.Text = _blockReason;
                _note.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            }
            else
            {
                _note.Text = badgeOk ? "" : "사원증 스캔 필요";
                _note.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text);
            }
        }
    }
}
