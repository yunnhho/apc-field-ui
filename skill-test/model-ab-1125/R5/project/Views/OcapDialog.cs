using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>
    /// 수율 기준 미달 Lot 처리(OCAP). Lot·수율·주요 Soft Bin·앞 단계 진행은 읽기 전용(호출 측 데이터),
    /// 이 dialog가 하는 단계는 판정 + 사유 입력뿐. 앞 단계가 끝나지 않으면 Release 확정이 막힌다.
    /// </summary>
    internal sealed class OcapDialog : ApcForm
    {
        public const string HoldKeep = "Hold 유지";
        public const string ExtraInspect = "추가 검사";
        public const string Release = "Release";
        public const string Scrap = "Scrap";
        private const int MainSoftBinRows = 3;

        private readonly IList<OcapStep> _steps;
        private readonly LotResult _lot;
        private readonly ApcComboBox _decision = new ApcComboBox();
        private readonly ApcTextBox _reason = new ApcTextBox();
        private readonly ApcButton _confirm = new ApcButton { Text = "확정", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;

        /// <summary>render·확인용 샘플(Release를 고른 상태 → 원인 확인 전이라 막힘).</summary>
        public OcapDialog() : this(SampleLot(), new SampleMainViewSource().GetSoftBinTop("FT2611-0387"),
            new SampleMainViewSource().GetOcapSteps("FT2611-0387"), Release) { }

        public OcapDialog(LotResult lot, IList<SoftBinCount> softBins, IList<OcapStep> steps, string initialDecision)
        {
            SetupDialog("기준 미달 Lot 처리 (OCAP)", 860);
            _lot = lot;
            _steps = steps;

            var info = new ApcInfoStrip();
            info.SetItems("Lot ID", "테스터", "고객", "제품");
            info.SetValue("Lot ID", lot.LotId);
            info.SetValue("테스터", lot.Tester);
            info.SetValue("고객", lot.Customer);
            info.SetValue("제품", lot.Product);
            AddContent(info);

            var yields = new ApcInfoStrip();
            yields.SetItems("최초 수율 (%)", "최종 수율 (%)", "최종 수율 하한 (%)");
            yields.SetValue("최초 수율 (%)", YieldRule.Pct(lot.FirstYield));
            yields.SetValue("최종 수율 (%)", YieldRule.Pct(lot.FinalYield));
            yields.SetValue("최종 수율 하한 (%)", YieldRule.Pct(YieldRule.FinalYieldLimit));
            AddContent(yields);

            AddContent(SectionTitle("주요 Soft Bin"));
            AddContent(BuildSoftBinGrid(lot, softBins));

            AddContent(SectionTitle("OCAP 진행"));
            AddContent(BuildStepGrid(steps));

            AddContent(SectionTitle("판정"));
            _decision.Items.AddRange(new object[] { HoldKeep, ExtraInspect, Release, Scrap });
            _decision.SelectedItem = initialDecision;
            if (_decision.SelectedIndex < 0) _decision.SelectedIndex = 0;
            _decision.SelectedIndexChanged += (s, e) => UpdateConfirm();
            AddFieldRow("판정", _decision);
            AddFieldRow("사유", _reason);

            _confirm.Click += (s, e) => Save();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm);
            _note = SetFooterNote();
            UpdateConfirm();
        }

        public string Decision { get; private set; }
        public string Reason { get; private set; }

        private static ApcGrid BuildSoftBinGrid(LotResult lot, IList<SoftBinCount> softBins)
        {
            var grid = new ApcGrid { ReadOnly = true, Height = ApcGrid.HeightForRows(MainSoftBinRows) };
            grid.Columns.Add("sb", "Soft Bin");
            grid.Columns.Add("name", "불량 원인");
            grid.Columns.Add("hb", "Hard Bin");
            grid.Columns.Add("count", "수량(EA)");
            grid.Columns.Add("rate", "비율(%)");
            foreach (DataGridViewColumn c in grid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (var s in softBins.Take(MainSoftBinRows))
                grid.Rows.Add("SB" + s.SoftBin, s.Name, "Bin" + s.HardBin, YieldRule.Count(s.Count), YieldRule.Pct(YieldRule.Rate(s.Count, lot.Input)));
            return grid;
        }

        private static ApcGrid BuildStepGrid(IList<OcapStep> steps)
        {
            var grid = new ApcGrid { ReadOnly = true, Height = ApcGrid.HeightForRows(steps.Count) };
            grid.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            grid.Columns.Add("step", "단계");
            grid.Columns.Add("time", "시각");
            grid.Columns.Add("detail", "내용");
            grid.Columns["detail"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["detail"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            foreach (var s in steps) grid.Rows.Add(new ApcLampValue(s.Status, s.StateText), s.Name, s.Time, s.Detail);
            return grid;
        }

        /// <summary>Release를 막는 첫 미완료 단계. 없으면 null.</summary>
        private OcapStep FirstPendingStep()
        {
            return _steps.FirstOrDefault(s => !s.Done);
        }

        private string BlockReason()
        {
            if ((string)_decision.SelectedItem != Release) return "";
            var pending = FirstPendingStep();
            return pending == null ? "" : pending.Name + " 전 · Release 불가";
        }

        private void UpdateConfirm()
        {
            string reason = BlockReason();
            _confirm.Enabled = reason.Length == 0;
            _note.Text = reason;
        }

        private void Save()
        {
            if (BlockReason().Length > 0) return;   // 단계 건너뛴 Release는 어떤 경로로도 확정하지 않는다
            string decision = (string)_decision.SelectedItem;
            string reason = _reason.Text.Trim();
            if (reason.Length == 0)
            {
                ApcDialog.Warn(this, "판정 사유를 입력하세요.");
                _reason.Focus();
                return;
            }
            bool ok = decision == Scrap
                ? ApcDialog.Confirm(this, _lot.LotId + "를 Scrap 처리합니다. 되돌릴 수 없습니다.")
                : ApcDialog.Confirm(this, _lot.LotId + " 판정: " + decision, "확정", ApcButtonVariant.Primary);
            if (!ok) return;

            Decision = decision;
            Reason = reason;
            DialogResult = DialogResult.OK;
        }

        private static LotResult SampleLot()
        {
            return new SampleMainViewSource().GetLotHistory().First(l => l.BelowSpec);
        }
    }
}
