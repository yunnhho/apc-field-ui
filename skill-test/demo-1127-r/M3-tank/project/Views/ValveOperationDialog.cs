using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 밸브 조작. 동작은 현재 위치의 반대(닫힘 → 열기, 열림 → 닫기)로 데이터가 정하고, 사용자가 바꿀 수 없다.
    /// 인터락에 걸리면 확정 버튼이 꺼지고 이유가 버튼 옆에 나온다. 조작자는 사원증 스캔(EMP-nnnnn)으로만 받는다.
    /// </summary>
    internal sealed class ValveOperationDialog : ApcForm
    {
        private static readonly Regex BadgePattern = new Regex(@"^EMP-\d{5}$");

        private readonly ValveInfo _valve;
        private readonly string _blockReason;
        private readonly ApcScanBox _badge = new ApcScanBox();
        private readonly ApcButton _confirm;
        private readonly System.Windows.Forms.Label _note;
        private string _operatorId = "";

        public bool OpenRequested { get { return !_valve.Open; } }
        public string OperatorId { get { return _operatorId; } }

        // render용: 샘플의 V2(닫힘) 열기 — T3 레벨 알람으로 막힌 상태.
        public ValveOperationDialog() : this(new SampleMainViewSource()) { }

        private ValveOperationDialog(SampleMainViewSource s) : this(s.GetValves()[1], s.GetTanks(), s.GetPumps()) { }

        public ValveOperationDialog(ValveInfo valve, IList<TankInfo> tanks, IList<PumpInfo> pumps)
        {
            _valve = valve;
            string action = OpenRequested ? "열기" : "닫기";
            _blockReason = ProcessRules.BlockReason(valve, OpenRequested, tanks, pumps);

            SetupDialog("밸브 조작", 760);

            var valveInfo = new ApcInfoStrip();
            valveInfo.SetItems("밸브", "위치", "현재 상태", "동작");
            valveInfo.SetValue("밸브", valve.Id);
            valveInfo.SetValue("위치", valve.Position);
            valveInfo.SetValue("현재 상태", valve.Open ? "열림" : "닫힘");
            valveInfo.SetValue("동작", action);
            AddContent(valveInfo);

            var lineInfo = new ApcInfoStrip();
            lineInfo.SetItems("하류 탱크", "연결 펌프");
            TankInfo down = ProcessRules.FindTank(tanks, valve.DownstreamTank);
            PumpInfo pump = ProcessRules.FindPump(pumps, valve.Pump);
            lineInfo.SetValue("하류 탱크", down == null ? "출하" : down.Id + " " + ProcessRules.LevelText(down.Level));
            lineInfo.SetValue("연결 펌프", pump == null ? "" : pump.Id + (pump.Running ? " 가동" : " 정지"));
            AddContent(lineInfo);

            _badge.Accepts = c => BadgePattern.IsMatch(c);
            _badge.Scanned += OnBadgeScanned;
            _badge.TextChanged += delegate { if (_badge.Text != _operatorId) SetOperator(""); };
            _badge.Attach(this);
            AddFieldRow("작업자 사원증", _badge);

            _confirm = new ApcButton { Text = action, Variant = ApcButtonVariant.Primary };
            var cancel = new ApcButton { Text = "취소" };
            _confirm.Click += delegate { Confirm(action); };
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            SetFooter(cancel, _confirm);
            _note = SetFooterNote("");
            Refresh2();
        }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            if (!BadgePattern.IsMatch(e.Code)) { e.Accepted = false; return; }
            SetOperator(e.Code);
        }

        private void SetOperator(string id)
        {
            _operatorId = id;
            Refresh2();
        }

        // 확정 버튼 상태와 이유를 한곳에서 정한다(막힘 > 스캔 필요).
        private void Refresh2()
        {
            bool blocked = _blockReason.Length > 0;
            bool needScan = _operatorId.Length == 0;
            _confirm.Enabled = !blocked && !needScan;
            _note.Text = blocked ? _blockReason : (needScan ? "사원증을 스캔하세요" : "");
            _note.ForeColor = blocked ? ApcTheme.StatusText(StatusKind.Alarm) : _note.ForeColor;
        }

        private void Confirm(string action)
        {
            if (_blockReason.Length > 0 || _operatorId.Length == 0) return;
            string msg = _valve.Id + " 밸브를 " + (OpenRequested ? "엽니다" : "닫습니다") + ". 작업자 " + _operatorId;
            if (ApcDialog.Confirm(this, msg, action, ApcButtonVariant.Primary))
                DialogResult = DialogResult.OK;
        }
    }
}
