using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>
    /// 수율 기준 미달 Lot 처리(OCAP). Lot·수율·주요 Soft Bin은 읽기 전용.
    /// OCAP 단계(알람 → Lot Hold → 장비 사용 중지 → 원인 확인)가 끝나지 않으면 Release는 막고 이유를 글로 보인다.
    /// 결과는 Disposition·Reason 속성.
    /// </summary>
    internal sealed class OcapDialog : ApcForm
    {
        private const string Hold = "Hold 유지", Recheck = "추가 검사", Release = "Release", Scrap = "Scrap";
        private static readonly string[] Steps = { "알람 접수", "Lot Hold", "장비 사용 중지", "원인 확인" };

        private readonly ApcGrid _steps = new ApcGrid { ReadOnly = true };
        private readonly ApcComboBox _decision = new ApcComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ApcTextBox _reason = new ApcTextBox();
        private readonly Label _block = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleLeft };
        private readonly ApcButton _confirm = new ApcButton { Text = "판정 확정", Variant = ApcButtonVariant.Primary };
        private readonly ApcButton _causeDone = new ApcButton { Text = "원인 확인 완료", Width = ApcTheme.Px(150) };
        private bool _causeChecked;

        public OcapDialog() : this(SampleTester(), new SampleMainViewSource()) { }

        public OcapDialog(TesterInfo tester, IMainViewSource source)
        {
            SetupDialog("수율 기준 미달 Lot 처리", 680);

            AddContent(SectionTitle("Lot 정보 (읽기 전용)"));
            AddFieldRow("Lot", ReadOnlyBox(tester.LotId + " · " + tester.Customer + " · " + tester.Product));
            AddFieldRow("테스터", ReadOnlyBox(tester.Name + " · " + tester.ProgramVersion));
            AddFieldRow("최종 수율 (%)", ReadOnlyBox(YieldRules.Pct(tester.FinalYield) + "  (" + YieldRules.SpecText + ")"));

            AddContent(SectionTitle("주요 Soft Bin"));
            BuildSoftGrid(source.GetSoftTop(tester, 3));

            AddContent(SectionTitle("OCAP 진행 단계"));
            BuildStepGrid();

            AddContent(SectionTitle("판정"));
            _decision.Items.AddRange(new object[] { Hold, Recheck, Release, Scrap });
            _decision.SelectedIndex = 0;
            _decision.SelectedIndexChanged += (s, e) => Refresh_();
            AddFieldRow("판정", _decision);
            AddFieldRow("사유", _reason);
            _block.Height = ApcTheme.Px(ApcLayout.FieldHeight);
            AddFieldRow("제한", _block);

            _confirm.Click += (s, e) => Save();
            _causeDone.Click += (s, e) => CauseDone();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm, _causeDone);
            Refresh_();
        }

        public string Disposition { get; private set; }
        public string Reason { get; private set; }

        private static TesterInfo SampleTester()
        {
            foreach (var t in new SampleMainViewSource().GetTesters()) if (t.Name == "T04") return t;
            return null;
        }

        private static ApcTextBox ReadOnlyBox(string text) { return new ApcTextBox { Text = text, ReadOnly = true }; }

        private void BuildSoftGrid(IList<BinRow> rows)
        {
            var g = new ApcGrid { ReadOnly = true };
            g.Columns.Add("bin", "Soft Bin");
            g.Columns.Add("name", "원인");
            g.Columns.Add("hard", "Hard Bin");
            g.Columns.Add("count", "수량");
            g.Columns.Add("pct", "비율(%)");
            foreach (DataGridViewColumn c in g.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (var r in rows) g.Rows.Add(r.Bin.ToString(), r.Name, r.HardBin.ToString(), YieldRules.Qty(r.Count), YieldRules.Pct(r.Percent));
            g.Height = ApcGrid.HeightForRows(rows.Count);
            AddContent(g);
        }

        private void BuildStepGrid()
        {
            _steps.Columns.Add(new ApcLampColumn { HeaderText = "상태", Width = ApcTheme.Px(110) });
            _steps.Columns.Add("step", "단계");
            _steps.Columns.Add("note", "내용");
            _steps.Columns["step"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _steps.Columns["note"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            for (int i = 0; i < Steps.Length; i++) _steps.Rows.Add(null, Steps[i], "");
            _steps.Height = ApcGrid.HeightForRows(Steps.Length);
            ShowSteps();
            AddContent(_steps);
        }

        private void ShowSteps()
        {
            for (int i = 0; i < Steps.Length; i++)
            {
                bool done = i < 3 || _causeChecked;
                _steps.Rows[i].Cells[0].Value = new ApcLampValue(done ? StatusKind.Run : StatusKind.Stop, done ? "완료" : "미완료");
                _steps.Rows[i].Cells[2].Value = done ? "" : "Release 불가";
            }
        }

        private void CauseDone()
        {
            if (!ApcDialog.Confirm(this, "원인 확인을 완료 처리할까요?", "완료 처리", ApcButtonVariant.Primary)) return;
            _causeChecked = true;
            _causeDone.Enabled = false;
            ShowSteps();
            Refresh_();
        }

        // Release 제한: 막은 이유를 글로 보이고 확정 버튼을 끈다
        private void Refresh_()
        {
            bool blocked = (string)_decision.SelectedItem == Release && !_causeChecked;
            _block.Text = blocked ? "Release 불가 · 원인 확인 단계가 끝나지 않았습니다" : "없음";
            _block.ForeColor = blocked ? ApcTheme.StatusText(StatusKind.Alarm) : ApcTheme.C(ApcTheme.Current.Colors.TextMuted);
            _block.Font = ApcTheme.GetFont(blocked ? FontStyle.Bold : FontStyle.Regular);
            _confirm.Enabled = !blocked;
        }

        private void Save()
        {
            string decision = (string)_decision.SelectedItem;
            if (decision == Release && !_causeChecked) { Refresh_(); return; }
            if (_reason.Text.Trim().Length == 0)
            {
                ApcDialog.Warn(this, "사유를 입력하세요.");
                _reason.Focus();
                return;
            }
            bool ok = decision == Scrap
                ? ApcDialog.Confirm(this, "Lot를 Scrap 처리할까요? 되돌릴 수 없습니다.")
                : ApcDialog.Confirm(this, "판정 '" + decision + "'을 확정할까요?", "확정", ApcButtonVariant.Primary);
            if (!ok) return;
            Disposition = decision;
            Reason = _reason.Text.Trim();
            DialogResult = DialogResult.OK;
        }
    }
}
