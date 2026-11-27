using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 밸브 조작 dialog. 선택한 밸브 정보는 읽기 전용, 조작은 현재 상태의 반대(열림→닫기, 닫힘→열기) 하나뿐이다.
    /// 인터락 사유(blockReason)는 호출한 화면이 데이터로 넘기며, 이 dialog에서 풀 수 없다.
    /// </summary>
    internal sealed class ValveDialog : ApcForm
    {
        private readonly ApcScanBox _badge = new ApcScanBox();
        private readonly ApcButton _confirm;
        private readonly Label _note;
        private readonly string _blockReason;

        /// <summary>조작자 사원증(EMP-nnnnn). OK로 닫힌 뒤에 읽는다.</summary>
        public string OperatorBadge { get { return _badge.Text; } }

        /// <summary>true = 열기, false = 닫기.</summary>
        public bool OpenRequested { get; private set; }

        // render용 샘플: 막힌 상태(V5 열기 · T4 레벨 92 %)로 시작한다.
        public ValveDialog() : this(
            new ValveInfo("V5", "P2 토출 · T4 입구", false, new DateTime(2026, 11, 27, 14, 52, 31), "EMP-10587"),
            "T4 레벨 92 % · 열기 차단 (알람 기준 90 % 초과)") { }

        public ValveDialog(ValveInfo valve, string blockReason)
        {
            _blockReason = blockReason;
            OpenRequested = !valve.IsOpen;
            SetupDialog("밸브 조작", 620);

            var info = new ApcInfoStrip();
            info.SetItems("밸브", "위치", "현재", "조작");
            info.SetValue("밸브", valve.Id);
            info.SetValue("위치", valve.Position);
            info.SetValue("현재", valve.IsOpen ? "열림" : "닫힘");
            info.SetValue("조작", OpenRequested ? "열기" : "닫기");
            AddContent(info);

            _badge.ScanOnly = true;
            _badge.Accepts = ProcessRules.IsBadge;
            _badge.Attach(this);
            AddFieldRow("작업자 사원증", _badge);

            _confirm = new ApcButton { Text = OpenRequested ? "열기" : "닫기", Variant = ApcButtonVariant.Primary };
            var cancel = new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel };
            SetFooter(cancel, _confirm);
            _note = SetFooterNote("");
            UpdateState();

            _badge.Scanned += OnBadgeScanned;
            _confirm.Click += (s, e) =>
            {
                if (!_confirm.Enabled) return;
                DialogResult = DialogResult.OK;
            };
        }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            if (!ProcessRules.IsBadge(e.Code))
            {
                e.Accepted = false;
                BeginInvoke(new Action(() => ApcDialog.Warn(this, "사원증(EMP-00000 형식)을 스캔하세요.")));
                return;
            }
            UpdateState();
        }

        private void UpdateState()
        {
            bool hasBadge = ProcessRules.IsBadge(_badge.Text);
            if (_blockReason != null)
            {
                _confirm.Enabled = false;
                _note.Text = _blockReason;
                _note.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            }
            else if (!hasBadge)
            {
                _confirm.Enabled = false;
                _note.Text = "사원증 스캔 필요";
            }
            else
            {
                _confirm.Enabled = true;
                _note.Text = "";
            }
        }
    }
}
