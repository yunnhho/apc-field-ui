using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 밸브 조작 dialog. 밸브 정보는 읽기 전용, 동작은 현재 상태의 반대(닫힘→열기, 열림→닫기) 하나뿐이다.
    /// 인터락(하류 탱크 레벨 알람 · 흡입 밸브 + 펌프 가동)은 호출자가 넘긴 값으로 판정하고 화면에서 바꿀 수 없다.
    /// 조작자는 사원증 스캔으로만 기록한다(손으로 친 글자는 인정하지 않는다).
    /// </summary>
    internal sealed class ValveOperationDialog : ApcForm
    {
        private readonly ApcScanBox _badge = new ApcScanBox();
        private readonly ApcButton _confirm;
        private readonly Label _note;
        private readonly string _blockReason;
        private readonly Color _noteColor;

        public ValveOperationDialog() : this(
            new ValveInfo("V3", "P1", "T3", "T3", "P1", false, false, new DateTime(2026, 11, 27, 14, 15, 36), "EMP-10211"),
            new TankInfo("T3", "배합 탱크", 92, 55.0m, true), new PumpInfo("P1", false)) { }

        public ValveOperationDialog(ValveInfo valve, TankInfo downstream, PumpInfo pump)
        {
            SetupDialog("밸브 조작", 640);
            TargetOpen = !valve.IsOpen;
            _blockReason = TargetOpen ? ProcessRules.OpenBlockReason(valve, downstream) : ProcessRules.CloseBlockReason(valve, pump);

            var strip1 = new ApcInfoStrip { Height = ApcTheme.Px(ApcLayout.FieldHeight) };
            strip1.SetItems("밸브", "구간", "현재 상태");
            strip1.SetValue("밸브", valve.Id);
            strip1.SetValue("구간", valve.Route);
            strip1.SetValue("현재 상태", valve.IsOpen ? "열림" : "닫힘");
            AddContent(strip1);

            var strip2 = new ApcInfoStrip { Height = ApcTheme.Px(ApcLayout.FieldHeight) };
            strip2.SetItems("하류 탱크", "펌프");
            strip2.SetValue("하류 탱크", downstream == null ? "" : downstream.Id + " · " + downstream.Level + " %");
            strip2.SetValue("펌프", pump == null ? "" : pump.Id + (pump.Running ? " 가동" : " 정지"));
            AddContent(strip2);

            AddContent(SectionTitle("조작"));
            AddFieldRow("동작", new ApcTextBox { ReadOnly = true, Text = TargetOpen ? "열기" : "닫기" });
            _badge.Accepts = ProcessRules.IsBadge;
            _badge.Scanned += OnBadgeScanned;
            _badge.TextChanged += (s, e) => UpdateState();
            _badge.Attach(this);
            AddFieldRow("작업자 사원증", _badge);

            _confirm = new ApcButton { Text = TargetOpen ? "열기" : "닫기", Variant = ApcButtonVariant.Primary };
            _confirm.Click += (s, e) => DialogResult = DialogResult.OK;
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm);   // Enter는 스캔이 쓴다 → AcceptButton 없음
            _note = SetFooterNote("");
            _noteColor = _note.ForeColor;
            UpdateState();
        }

        public bool TargetOpen { get; private set; }
        public string Operator { get; private set; }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            if (!ProcessRules.IsBadge(e.Code))
            {
                e.Accepted = false;
                BeginInvoke(new Action(() => ApcDialog.Warn(this, "사원증(EMP-nnnnn)을 스캔하세요.")));
                return;
            }
            Operator = e.Code;
            UpdateState();
        }

        private bool OperatorScanned { get { return Operator != null && _badge.Text == Operator; } }

        private void UpdateState()
        {
            if (_confirm == null || _note == null) return;
            string reason = _blockReason ?? (OperatorScanned ? "" : "사원증 스캔 필요");
            _confirm.Enabled = reason.Length == 0;
            _note.Text = reason;
            _note.ForeColor = _blockReason != null ? ApcTheme.StatusText(StatusKind.Alarm) : _noteColor;
        }
    }
}
