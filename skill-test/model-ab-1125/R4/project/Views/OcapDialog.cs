using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>
    /// 수율 기준 미달 Lot 처리(OCAP). Lot·수율·주요 Soft Bin·선행 단계는 읽기 전용으로 받고, 이 dialog는 판정과 사유만 입력한다.
    /// 선행 단계(원인 확인·고객 승인 등)가 끝나지 않으면 Release는 확정할 수 없다 — 단계 상태는 caller가 넘긴 값이라 여기서 바꿀 수 없다.
    /// </summary>
    internal sealed class OcapDialog : ApcForm
    {
        private const string HoldKeep = "Hold 유지", ExtraInspect = "추가 검사", Release = "Release", Scrap = "Scrap";
        private const int SoftBinRows = 3;

        private readonly OcapCase _case;
        private readonly ApcComboBox _decision = new ApcComboBox();
        private readonly ApcTextBox _reason = new ApcTextBox();
        private readonly ApcButton _confirm = new ApcButton { Text = "확정", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;

        public OcapDialog() : this(new SampleMainViewSource().GetOcapCase(new SampleMainViewSource().GetTesters()[3])) { }

        public OcapDialog(OcapCase ocapCase)
        {
            _case = ocapCase;
            SetupDialog("수율 기준 미달 Lot 처리", 920);

            AddContent(SectionTitle("대상 Lot"));
            AddContent(BuildLotInfo());
            AddContent(SectionTitle("주요 Soft Bin"));
            AddContent(BuildSoftGrid());
            AddContent(SectionTitle("OCAP 진행 단계"));
            AddContent(BuildStepGrid());
            AddContent(SectionTitle("판정"));

            _decision.Items.AddRange(new object[] { HoldKeep, ExtraInspect, Release, Scrap });
            _decision.SelectedIndex = 0;
            _decision.SelectedIndexChanged += (s, e) => ShowRelease();
            AddFieldRow("판정", _decision);
            AddFieldRow("사유", _reason);

            _confirm.Click += (s, e) => Save();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm);
            _note = SetFooterNote("");
            ShowRelease();   // Enter(AcceptButton)는 두지 않는다 — 스캐너 Enter로 확정되지 않게
        }

        public string Decision { get; private set; }
        public string Reason { get; private set; }

        // Release를 고르면 선행 단계가 남았는지 보고 확정 버튼을 끄고 이유를 보인다.
        private void ShowRelease()
        {
            string block = IsRelease ? _case.ReleaseBlockReason() : null;
            _confirm.Enabled = block == null;
            _note.Text = block ?? "";
            _note.ForeColor = block == null ? _note.ForeColor : ApcTheme.StatusText(StatusKind.Alarm);
        }

        private bool IsRelease { get { return (string)_decision.SelectedItem == Release; } }

        private void Save()
        {
            string decision = (string)_decision.SelectedItem;
            if (IsRelease && _case.ReleaseBlockReason() != null) { ApcDialog.Warn(this, _case.ReleaseBlockReason()); return; }   // 방어: 버튼이 꺼져 있어도 다시 막는다
            if (_reason.Text.Trim().Length == 0) { ApcDialog.Warn(this, "사유를 입력하세요."); _reason.Focus(); return; }

            string message = "Lot " + _case.Lot.LotId + " 판정을 '" + decision + "'(으)로 확정할까요?";
            bool ok = decision == Scrap ? ApcDialog.Confirm(this, message)
                : ApcDialog.Confirm(this, message, "확정", ApcButtonVariant.Primary);
            if (!ok) return;
            Decision = decision;
            Reason = _reason.Text.Trim();
            DialogResult = DialogResult.OK;
        }

        private Control BuildLotInfo()
        {
            var lot = _case.Lot;
            var strip = new ApcInfoStrip { Height = ApcTheme.Px(ApcTextBox.BaseHeight) };
            strip.SetItems("Lot ID", "고객", "테스터", "최초(%)", "최종(%)");
            strip.SetValue("Lot ID", lot.LotId);
            strip.SetValue("고객", lot.Customer);
            strip.SetValue("테스터", lot.Tester);
            strip.SetValue("최초(%)", YieldRules.Pct(lot.FirstYield));
            strip.SetValue("최종(%)", YieldRules.Pct(lot.FinalYield));
            return strip;
        }

        private Control BuildSoftGrid()
        {
            var grid = new ApcGrid { ReadOnly = true, Height = ApcGrid.HeightForRows(SoftBinRows) };
            grid.Columns.Add("soft", "Soft Bin");
            grid.Columns.Add("hard", "Hard Bin");
            grid.Columns.Add("qty", "수량(Unit)");
            grid.Columns.Add("rate", "비율(%)");
            foreach (DataGridViewColumn c in grid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            for (int i = 0; i < SoftBinRows && i < _case.SoftBins.Length; i++)
            {
                var sb = _case.SoftBins[i];
                grid.Rows.Add("SB" + sb.SoftNo, sb.HardNo, YieldRules.Qty(sb.Count), YieldRules.Pct(sb.Rate(_case.Lot.Tested)));
            }
            return grid;
        }

        private Control BuildStepGrid()
        {
            var grid = new ApcGrid { ReadOnly = true, Height = ApcGrid.HeightForRows(_case.Steps.Length) };
            grid.Columns.Add(new ApcLampColumn());
            grid.Columns.Add("step", "단계");
            grid.Columns.Add("state", "진행");
            grid.Columns["step"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["step"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.Columns["state"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (var s in _case.Steps) grid.Rows.Add(s.Done ? StatusKind.Run : StatusKind.Stop, s.Name, s.Note);
            return grid;
        }
    }
}
