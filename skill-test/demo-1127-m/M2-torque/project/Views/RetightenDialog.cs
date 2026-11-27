using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TorqueSequence.Data;

namespace TorqueSequence.Views
{
    /// <summary>
    /// 재체결 승인: NG 포인트 정보(읽기 전용) + 반장 사원증 스캔 + 사유 선택.
    /// 같은 포인트 NG가 MaxNg회째면 승인할 수 없고 부품 불량 처리로 안내한다(스캔·사유를 바꿔도 풀리지 않음).
    /// </summary>
    internal sealed class RetightenDialog : ApcForm
    {
        private const string ReasonPlaceholder = "사유 선택";
        private readonly ApcScanBox _foremanScan = new ApcScanBox();
        private readonly ApcComboBox _reason = new ApcComboBox();
        private readonly ApcButton _ok = new ApcButton { Text = "승인", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;
        private readonly bool _blocked;
        private string _foreman = "";

        // 샘플: 막힌 상태(NG 2회)부터 보여 준다.
        public RetightenDialog() : this(7, 11.4, 52, 2) { }

        public RetightenDialog(int point, double torque, int angle, int ngCount)
        {
            _blocked = ngCount >= TorqueRules.MaxNg;
            SetupDialog("재체결 승인", 900);

            var info = new ApcInfoStrip();
            info.SetItems("포인트", "토크 (N·m)", "각도 (°)", "NG 횟수");
            info.SetValue("포인트", point + "번");
            info.SetValue("토크 (N·m)", torque.ToString("0.0"));
            info.SetValue("각도 (°)", angle.ToString());
            info.SetValue("NG 횟수", ngCount + "회");
            AddContent(info);

            if (_blocked)
                AddContent(new ApcNoticeBar { Status = StatusKind.Alarm, Text = "재체결 불가", Detail = "같은 포인트 NG " + TorqueRules.MaxNg + "회 · 부품 불량 처리" });

            _reason.Items.AddRange(new object[] { ReasonPlaceholder, "공구 슬립", "볼트 이물질", "체결 자세 불량", "측정값 재확인", "기타" });
            _reason.SelectedIndex = 0;
            _reason.SelectedIndexChanged += (s, e) => UpdateState();
            AddFieldRow("반장 사원증", _foremanScan);
            AddFieldRow("재체결 사유", _reason);

            _foremanScan.Attach(this);
            _foremanScan.Scanned += OnForemanScanned;

            _ok.Click += (s, e) => { DialogResult = DialogResult.OK; };
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _ok);
            _note = SetFooterNote();
            UpdateState();
        }

        public string Foreman { get { return _foreman; } }
        public string Reason { get { return Convert.ToString(_reason.SelectedItem); } }

        private void OnForemanScanned(object sender, ScanEventArgs e)
        {
            if (_blocked) { e.Accepted = false; return; }
            if (!TorqueRules.IsForeman(e.Code))
            {
                e.Accepted = false;
                BeginInvoke(new Action(() => ApcDialog.Warn(this, "반장 사원증(EMP-9…)이 아닙니다.")));
                return;
            }
            _foreman = e.Code;
            UpdateState();
        }

        // 승인 조건은 이 함수 하나가 정한다. 막힘 사유는 버튼 옆에 글로 보인다.
        private void UpdateState()
        {
            string why = _blocked ? "NG " + TorqueRules.MaxNg + "회 · 재체결 불가"
                       : _foreman.Length == 0 ? "반장 사원증을 스캔하세요"
                       : _reason.SelectedIndex <= 0 ? "사유를 선택하세요" : "";
            _ok.Enabled = why.Length == 0;
            _note.Text = why;
            _note.ForeColor = _blocked ? ApcTheme.StatusText(StatusKind.Alarm) : ApcTheme.C(ApcTheme.Current.Colors.Text);
            _reason.Enabled = !_blocked;
            _foremanScan.Enabled = !_blocked;
        }
    }
}
