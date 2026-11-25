using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>
    /// 수율 기준 미달 Lot 처리(OCAP). Lot·수율·주요 Soft Bin은 읽기 전용, 아래에서 원인 확인과 판정·사유를 입력한다.
    /// 원인 확인 전 Release는 화면이 막는다(확정 버튼 비활성 + 띠에 이유). 확정 → Confirm → 결과 속성 → OK.
    /// </summary>
    internal sealed class OcapDialog : ApcForm
    {
        private const int StepCauseRow = 3, SoftRows = 3, FieldWidth = 220;
        private const string Hold = "Hold 유지", Extra = "추가 검사", Release = "Release", Scrap = "Scrap";

        private readonly string _lotId;
        private readonly ApcGrid _steps = new ApcGrid { ReadOnly = true };
        private readonly ApcComboBox _cause = new ApcComboBox { Width = ApcTheme.Px(FieldWidth), DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ApcComboBox _decision = new ApcComboBox { Width = ApcTheme.Px(FieldWidth), DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ApcTextBox _reasonBox = new ApcTextBox();
        private readonly ApcNoticeBar _notice = new ApcNoticeBar { Height = ApcTheme.Px(40) };
        private readonly ApcButton _confirm = new ApcButton { Text = "판정 확정", Variant = ApcButtonVariant.Primary };

        public OcapDialog() : this(new SampleMainViewSource().GetTesters()[3]) { }

        public OcapDialog(TesterInfo tester)
        {
            var lot = tester.Lot;
            _lotId = lot.LotId;
            SetupDialog("수율 기준 미달 Lot 처리", 980);

            AddContent(SectionTitle("대상 Lot"));
            var strip = new ApcInfoStrip { Height = ApcTheme.Px(40) };
            strip.SetItems("Lot ID", "고객", "테스터", "최초 수율 (%)", "최종 수율 (%)");
            strip.SetValue("Lot ID", lot.LotId);
            strip.SetValue("고객", lot.Customer);
            strip.SetValue("테스터", tester.Id);
            strip.SetValue("최초 수율 (%)", YieldRule.Pct(lot.FirstYield));
            strip.SetValue("최종 수율 (%)", YieldRule.Pct(lot.FinalYield));
            AddContent(strip);

            AddContent(SectionTitle("주요 Soft Bin"));
            var soft = new ApcGrid { ReadOnly = true, Height = ApcGrid.HeightForRows(SoftRows) };
            soft.Columns.Add("bin", "Soft Bin");
            soft.Columns.Add("name", "불량 원인");
            soft.Columns.Add("hard", "Hard Bin");
            soft.Columns.Add("qty", "수량 (EA)");
            soft.Columns.Add("pct", "비율 (%)");
            foreach (DataGridViewColumn c in soft.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            for (int i = 0; i < SoftRows; i++)
            {
                var s = lot.SoftTop[i];
                soft.Rows.Add(s.Soft.ToString(), s.Name, s.Hard.ToString(), YieldRule.Qty(s.Qty),
                    YieldRule.Pct((double)s.Qty * 100.0 / lot.DoneQty));
            }
            AddContent(soft);

            AddContent(SectionTitle("OCAP 진행"));
            _steps.Columns.Add("step", "단계");
            _steps.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            foreach (DataGridViewColumn c in _steps.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _steps.Rows.Add("수율 이탈 · 알람", new ApcLampValue(StatusKind.Run, "완료"));
            _steps.Rows.Add("Lot Hold", new ApcLampValue(StatusKind.Run, "완료"));
            _steps.Rows.Add("장비 사용 중지", new ApcLampValue(StatusKind.Run, "완료"));
            _steps.Rows.Add("원인 확인", new ApcLampValue(StatusKind.Warn, "미확인"));
            _steps.Height = ApcGrid.HeightForRows(4);
            AddContent(_steps);

            _cause.Items.AddRange(new object[] { "미확인", "확인 완료" });
            _cause.SelectedIndex = 0;
            _cause.SelectedIndexChanged += (s, e) => SyncState();
            AddFieldRow("원인 확인", _cause);

            AddContent(SectionTitle("판정"));
            _decision.Items.AddRange(new object[] { Hold, Extra, Release, Scrap });
            _decision.SelectedIndex = 0;
            _decision.SelectedIndexChanged += (s, e) => SyncState();
            AddFieldRow("판정", _decision);
            AddFieldRow("사유", _reasonBox);
            AddContent(_notice);

            _confirm.Click += (s, e) => Save();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm);
            SyncState();
        }

        public string Decision { get; private set; }
        public string Reason { get; private set; }

        private bool CauseConfirmed { get { return _cause.SelectedIndex == 1; } }

        // Release는 원인 확인 뒤에만 — 건너뛴 Release를 막는 한 곳
        private bool ReleaseBlocked { get { return (string)_decision.SelectedItem == Release && !CauseConfirmed; } }

        private void SyncState()
        {
            _steps.Rows[StepCauseRow].Cells[1].Value = CauseConfirmed
                ? new ApcLampValue(StatusKind.Run, "확인 완료") : new ApcLampValue(StatusKind.Warn, "미확인");
            bool blocked = ReleaseBlocked;
            _confirm.Enabled = !blocked;
            if (blocked)
            {
                _notice.Status = StatusKind.Alarm; _notice.Text = "Release 불가"; _notice.Detail = "원인 확인 전에는 Release할 수 없습니다";
            }
            else
            {
                _notice.Status = StatusKind.Info; _notice.Text = "판정 입력"; _notice.Detail = "판정과 사유를 입력하고 확정하세요";
            }
        }

        private void Save()
        {
            if (ReleaseBlocked) return;   // 버튼이 막혀 있지만 한 번 더
            string decision = (string)_decision.SelectedItem;
            if (_reasonBox.Text.Trim().Length == 0)
            {
                ApcDialog.Warn(this, "사유를 입력하세요.");
                _reasonBox.Focus();
                return;
            }
            string message = _lotId + " 판정을 '" + decision + "'로 확정할까요?";
            bool ok = decision == Scrap
                ? ApcDialog.Confirm(this, message)
                : ApcDialog.Confirm(this, message, "확정", ApcButtonVariant.Primary);
            if (!ok) return;
            Decision = decision;
            Reason = _reasonBox.Text.Trim();
            DialogResult = DialogResult.OK;
        }
    }
}
