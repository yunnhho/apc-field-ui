using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TorqueSequence.Data;

namespace TorqueSequence.Views
{
    /// <summary>
    /// 재체결 승인: NG 포인트 정보(읽기 전용) + 반장 사원증 스캔 + 사유 선택.
    /// 같은 포인트 NG 2회째(ngCount >= 2)면 승인 자체를 막고 부품 불량 처리로 안내한다(결과 Abort).
    /// </summary>
    internal sealed class RetightenDialog : ApcForm
    {
        private readonly ApcScanBox _foreman = new ApcScanBox { Width = ApcTheme.Px(300) };
        private readonly ApcComboBox _reason = new ApcComboBox { Width = ApcTheme.Px(420) };
        private readonly ApcButton _ok = new ApcButton { Text = "승인", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;
        private readonly bool _blocked;
        private string _foremanBadge;

        // 샘플: 7번 포인트 NG 2회째 — 막힌 상태(승인 버튼 꺼짐 + 이유)
        public RetightenDialog() : this(7, 11.4m, 45, new DateTime(2026, 11, 27, 9, 14, 20), 2,
            new SampleMainViewSource().GetRetightenReasons()) { }

        public RetightenDialog(int point, decimal torque, int angle, DateTime time, int ngCount, string[] reasons)
        {
            _blocked = ngCount >= TorqueRules.NgBlockCount;
            SetupDialog("재체결 승인", 760);

            var info = new ApcInfoStrip();
            info.SetItems("포인트", "토크(N·m)", "각도(°)", "NG 시각");
            info.SetValue("포인트", point + "번");
            info.SetValue("토크(N·m)", torque.ToString("0.0"));
            info.SetValue("각도(°)", angle.ToString());
            info.SetValue("NG 시각", time.ToString("HH:mm:ss"));
            AddContent(SectionTitle("NG 포인트"));
            AddContent(info);

            if (_blocked)
                AddContent(new ApcNoticeBar { Status = StatusKind.Alarm, Text = "재체결 불가",
                    Detail = point + "번 포인트 NG " + ngCount + "회째 · 부품 불량 처리로 진행" , Height = ApcTheme.MinHeight });

            AddContent(SectionTitle("승인"));
            _foreman.Accepts = c => true;   // 사원증 형식·반장 여부는 Scanned에서 검사해 거부
            _foreman.Scanned += OnForemanScanned;
            _foreman.Attach(this);
            _reason.Items.AddRange(reasons);
            _reason.SelectedIndexChanged += (s, e) => UpdateConfirm();
            AddFieldRow("반장 사원증", _foreman);
            AddFieldRow("사유", _reason);
            _foreman.Enabled = _reason.Enabled = !_blocked;

            _ok.Click += (s, e) => { DialogResult = DialogResult.OK; };
            var cancel = new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel };
            if (_blocked)
            {
                var scrap = new ApcButton { Text = "부품 불량 처리", Variant = ApcButtonVariant.Danger, Width = ApcTheme.Px(150) };
                scrap.Click += (s, e) => { DialogResult = DialogResult.Abort; };
                SetFooter(cancel, _ok, scrap);
            }
            else SetFooter(cancel, _ok);
            _note = SetFooterNote("");
            UpdateConfirm();
        }

        public string ForemanBadge { get { return _foremanBadge; } }
        public string Reason { get { return Convert.ToString(_reason.SelectedItem); } }

        private void OnForemanScanned(object sender, ScanEventArgs e)
        {
            if (!TorqueRules.IsForemanBadge(e.Code))
            {
                e.Accepted = false;
                _foremanBadge = null;
                BeginInvoke((Action)(() =>
                {
                    ApcDialog.Warn(this, TorqueRules.IsBadge(e.Code) ? "반장 사원증이 아닙니다 · " + e.Code : "사원증이 아닙니다 · " + e.Code);
                    UpdateConfirm();
                }));
                return;
            }
            _foremanBadge = e.Code;
            UpdateConfirm();
        }

        private void UpdateConfirm()
        {
            string why = _blocked ? "NG 2회째 · 재체결 불가"
                : _foremanBadge == null ? "반장 사원증 스캔 필요"
                : _reason.SelectedIndex < 0 ? "사유 선택 필요" : "";
            _ok.Enabled = why.Length == 0;
            if (_note != null) _note.Text = why;
        }
    }
}
