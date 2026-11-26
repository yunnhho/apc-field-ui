using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>
    /// 수율 기준 미달 Lot 처리(OCAP). Lot·수율·주요 Soft Bin은 읽기 전용, OCAP 단계 진행 표, 판정 선택 + 사유.
    /// '판정' 앞 단계(원인 확인 등)가 끝나지 않았으면 Release를 막고 이유를 판정 줄에 적는다.
    /// </summary>
    internal sealed class OcapDialog : ApcForm
    {
        private const string Release = "Release", Scrap = "Scrap", DecisionStep = "판정";
        private static readonly string[] Decisions = { "Hold 유지", "추가 검사", Release, Scrap };

        private readonly ApcComboBox _decision = new ApcComboBox { Width = ApcTheme.Px(180) };
        private readonly ApcTextBox _reason = new ApcTextBox();
        private readonly ApcButton _confirm = new ApcButton { Text = "확정", Variant = ApcButtonVariant.Primary };
        private readonly Label _releaseBlock = FieldCaption("");
        private readonly string _lotId;
        private readonly string _pendingStep;

        public OcapDialog() : this(SampleHoldLot(), new SampleMainViewSource().GetOcapSteps(SampleMainViewSource.HoldLotId)) { }

        public OcapDialog(LotResult lot, IList<OcapStep> steps)
        {
            SetupDialog("수율 기준 미달 Lot 처리 (OCAP)", 760);
            _lotId = lot.LotId;
            var pending = steps.TakeWhile(s => s.Name != DecisionStep).FirstOrDefault(s => s.Done == null);
            _pendingStep = pending == null ? null : pending.Name;

            AddContent(SectionTitle("Lot"));
            AddFieldRow("Lot ID", ReadOnly(lot.LotId + "  (" + lot.TesterId + ")"));
            AddFieldRow("고객 · 제품", ReadOnly(lot.Customer + " · " + lot.Product));
            var final = ReadOnly(YieldJudge.Pct(lot.FinalYield), 110);
            final.ForeColor = ApcTheme.StatusText(YieldJudge.Judge(lot.FinalYield));
            final.Font = ApcTheme.GetFont(FontStyle.Bold);
            AddFieldRow("최종 수율 (%)", final, FieldCaption("최초 수율 (%)"), ReadOnly(YieldJudge.Pct(lot.FirstYield), 110),
                FieldCaption("기준 " + YieldJudge.Pct(YieldJudge.Limit) + " 이상"));
            AddFieldRow("주요 Soft Bin", ReadOnly(lot.TopSoftBin + " · " + YieldJudge.Pct(lot.TopSoftBinRate) + " %"));

            AddContent(SectionTitle("OCAP 진행"));
            AddContent(StepGrid(steps));

            AddContent(SectionTitle("판정"));
            _decision.Items.AddRange(Decisions);
            _decision.SelectedIndex = 0;
            _decision.SelectedIndexChanged += (s, e) => UpdateConfirm();
            _releaseBlock.Text = _pendingStep == null ? "" : "Release 불가 · " + _pendingStep + " 미완료";
            AddFieldRow("판정", _decision, _releaseBlock);
            AddFieldRow("사유", _reason);

            _confirm.Click += (s, e) => Confirm();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm);
            UpdateConfirm();
        }

        public string Decision { get; private set; }
        public string Reason { get; private set; }

        private bool ReleaseBlocked { get { return _pendingStep != null; } }

        private void UpdateConfirm()
        {
            _confirm.Enabled = !(ReleaseBlocked && (string)_decision.SelectedItem == Release);
        }

        private void Confirm()
        {
            var decision = (string)_decision.SelectedItem;
            if (decision == Release && ReleaseBlocked) return;
            if (_reason.Text.Trim().Length == 0)
            {
                ApcDialog.Warn(this, "사유를 입력하세요.");
                _reason.Focus();
                return;
            }
            bool ok = decision == Scrap
                ? ApcDialog.Confirm(this, _lotId + "을 Scrap 처리할까요? 되돌릴 수 없습니다.")
                : ApcDialog.Confirm(this, _lotId + " 판정을 '" + decision + "'(으)로 기록할까요?", "확정", ApcButtonVariant.Primary);
            if (!ok) return;
            Decision = decision;
            Reason = _reason.Text.Trim();
            DialogResult = DialogResult.OK;
        }

        private static ApcGrid StepGrid(IList<OcapStep> steps)
        {
            var grid = new ApcGrid { ReadOnly = true, Height = ApcGrid.HeightForRows(steps.Count) };
            grid.Columns.Add(new ApcLampColumn { Name = "state", HeaderText = "상태", Width = ApcTheme.Px(100) });
            grid.Columns.Add("step", "단계");
            grid.Columns.Add("time", "완료 시각");
            grid.Columns.Add("owner", "담당");
            for (int i = 1; i < grid.Columns.Count; i++) grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (var s in steps)
                grid.Rows.Add(s.Done == null ? new ApcLampValue(StatusKind.Stop, "대기") : new ApcLampValue(StatusKind.Run, "완료"),
                    s.Name, s.Done == null ? "" : s.Done.Value.ToString("HH:mm:ss"), s.Owner);
            return grid;
        }

        private static ApcTextBox ReadOnly(string text, int width = 0)
        {
            var box = new ApcTextBox { ReadOnly = true, Text = text, TabStop = false };
            if (width > 0) box.Width = ApcTheme.Px(width);
            return box;
        }

        private static LotResult SampleHoldLot()
        {
            return new SampleMainViewSource().GetLotHistory().First(r => r.LotId == SampleMainViewSource.HoldLotId);
        }
    }
}
