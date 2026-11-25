using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>
    /// 수율 기준 미달 Lot 처리(OCAP). 위는 읽기 전용(Lot·수율·주요 Soft Bin), 아래에서 판정과 사유를 고른다.
    /// 원인 확인 전에는 Release를 화면이 막고 이유를 띠로 보인다. 결과는 Decision·Reason 속성.
    /// </summary>
    internal sealed class OcapDialog : ApcForm
    {
        private const string Hold = "Hold 유지", Extra = "추가 검사", Release = "Release", Scrap = "Scrap";
        private static readonly string[] Reasons = { "프로브 카드 접촉 불량", "테스트 프로그램 버전 오류", "온도 편차", "핸들러 Jam·접촉 불량", "소자 자체 불량(Wafer)" };

        private readonly ApcComboBox _decision = new ApcComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ApcComboBox _reason = new ApcComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ApcNoticeBar _bar = new ApcNoticeBar();
        private readonly ApcGrid _steps = new ApcGrid { ReadOnly = true };
        private readonly ApcButton _confirm = new ApcButton { Text = "판정 확정", Variant = ApcButtonVariant.Primary };
        private readonly ApcButton _causeDone = new ApcButton { Text = "원인 확인 완료" };
        private bool _causeConfirmed;

        public string Decision { get; private set; }
        public string Reason { get; private set; }

        public OcapDialog() : this("T04", new SampleMainViewSource().GetTesters()[3].Lot) { }

        public OcapDialog(string tester, LotInfo lot)
        {
            SetupDialog("수율 기준 미달 Lot 처리", 760);

            var strip = new ApcInfoStrip { Height = ApcTheme.Px(ApcLayout.FieldHeight) };
            strip.SetItems("Lot ID", "테스터", "고객", "제품");
            strip.SetValue("Lot ID", lot.LotId); strip.SetValue("테스터", tester);
            strip.SetValue("고객", lot.CustomerCode); strip.SetValue("제품", lot.Product);
            AddContent(strip);

            var finalCard = new ApcValueCard { Title = "최종 수율", Value = YieldRule.Pct(lot.FinalYield), Unit = "%", ShowLamp = true,
                Status = YieldRule.JudgeFinal(lot.FinalYield), Footer = "기준 " + YieldRule.Pct(YieldRule.FinalYieldCriterion) + " 이상" };
            var firstCard = new ApcValueCard { Title = "최초 수율", Value = YieldRule.Pct(lot.FirstPassYield), Unit = "%", Status = StatusKind.Run,
                Footer = "투입 " + YieldRule.Qty(lot.InputQty) + " · 진행 " + YieldRule.Qty(lot.TestedQty) };
            finalCard.Height = firstCard.Height = ApcTheme.Px(96);
            AddContentRow(finalCard, firstCard);

            AddContent(SectionTitle("주요 Soft Bin"));
            var soft = new ApcGrid { ReadOnly = true, Height = ApcGrid.HeightForRows(3) };
            soft.Columns.Add("bin", "Soft Bin"); soft.Columns.Add("name", "불량 원인"); soft.Columns.Add("hard", "Hard Bin");
            soft.Columns.Add("qty", "수량"); soft.Columns.Add("rate", "비율(%)");
            foreach (DataGridViewColumn c in soft.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            for (int i = 0; i < 3 && i < lot.SoftTop.Length; i++)
            {
                var s = lot.SoftTop[i];
                soft.Rows.Add(s.SoftBin.ToString("00"), s.Name, "Bin " + s.HardBin, YieldRule.Qty(s.Qty), YieldRule.Pct(100.0 * s.Qty / lot.TestedQty));
            }
            AddContent(soft);

            AddContent(SectionTitle("OCAP 진행"));
            _steps.Columns.Add("step", "단계");
            _steps.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            _steps.Columns["step"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _steps.Columns["step"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            _steps.Columns[1].Width = ApcTheme.Px(160);
            _steps.Height = ApcGrid.HeightForRows(5);
            for (int i = 0; i < 5; i++) _steps.Rows.Add("", null);
            AddContent(_steps);

            _decision.Items.AddRange(new object[] { Hold, Extra, Release, Scrap });
            _decision.SelectedIndex = 0;
            _reason.Items.AddRange(Reasons);
            _decision.SelectedIndexChanged += (s, e) => Refresh2();
            _reason.SelectedIndexChanged += (s, e) => Refresh2();
            AddFieldRow("판정", _decision);
            AddFieldRow("사유", _reason);
            AddContent(_bar);

            _causeDone.Click += (s, e) => ConfirmCause();
            _confirm.Click += (s, e) => Submit();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm, _causeDone);
            Refresh2();
        }

        private bool ReleaseBlocked { get { return (string)_decision.SelectedItem == Release && !_causeConfirmed; } }

        // 판정·원인 확인 상태에 맞춰 진행 표·안내 띠·확정 버튼을 한 번에 맞춘다
        private void Refresh2()
        {
            string d = (string)_decision.SelectedItem;
            SetStep(0, "이탈 · 알람 · Lot Hold · 장비 사용 중지", StatusKind.Run, "완료");
            SetStep(1, "원인 확인", _causeConfirmed ? StatusKind.Run : StatusKind.Stop, _causeConfirmed ? "완료" : "대기");
            SetStep(2, "판정", StatusKind.Stop, d ?? "선택 대기");
            SetStep(3, "장비 재가동 승인", StatusKind.Stop, "대기");
            SetStep(4, "기록", StatusKind.Stop, "대기");

            if (ReleaseBlocked) { _bar.Status = StatusKind.Alarm; _bar.Text = "Release 불가"; _bar.Detail = "원인 확인 전 · 사유를 고르고 [원인 확인 완료]를 누르세요"; }
            else if (d == Release) { _bar.Status = StatusKind.Run; _bar.Text = "Release 가능"; _bar.Detail = "원인 확인 완료"; }
            else if (d == Scrap) { _bar.Status = StatusKind.Warn; _bar.Text = "Scrap"; _bar.Detail = "되돌릴 수 없음 · 확정 때 한 번 더 확인합니다"; }
            else if (d == Extra) { _bar.Status = StatusKind.Info; _bar.Text = "추가 검사"; _bar.Detail = "결과 확인 후 다시 판정"; }
            else { _bar.Status = StatusKind.Info; _bar.Text = "Hold 유지"; _bar.Detail = "장비는 사용 중지 상태로 둡니다"; }

            _confirm.Enabled = !ReleaseBlocked;
            _causeDone.Visible = !_causeConfirmed;
        }

        private void SetStep(int row, string name, StatusKind kind, string text)
        {
            _steps.Rows[row].Cells[0].Value = name;
            _steps.Rows[row].Cells[1].Value = new ApcLampValue(kind, text);
        }

        private void ConfirmCause()
        {
            if (_reason.SelectedIndex < 0) { ApcDialog.Warn(this, "사유(원인)를 먼저 고르세요."); _reason.Focus(); return; }
            _causeConfirmed = true;
            Refresh2();
        }

        private void Submit()
        {
            if (ReleaseBlocked) { ApcDialog.Warn(this, "원인 확인 전에는 Release할 수 없습니다."); return; }
            if (_reason.SelectedIndex < 0) { ApcDialog.Warn(this, "사유를 고르세요."); _reason.Focus(); return; }
            string d = (string)_decision.SelectedItem;
            bool ok = d == Scrap
                ? ApcDialog.Confirm(this, "Lot을 Scrap 처리할까요? 되돌릴 수 없습니다.")
                : ApcDialog.Confirm(this, d + "(으)로 확정할까요?", "확정", ApcButtonVariant.Primary);
            if (!ok) return;
            Decision = d;
            Reason = (string)_reason.SelectedItem;
            DialogResult = DialogResult.OK;
        }
    }
}
