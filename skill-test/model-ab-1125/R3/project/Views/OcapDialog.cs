using System;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>
    /// 기준 미달 Lot OCAP 판정. Lot·수율·주요 Soft Bin은 읽기 전용.
    /// Release는 추가 검사 합격 후에만 저장된다(단계 건너뛰기 차단).
    /// </summary>
    internal sealed class OcapDialog : ApcForm
    {
        private const int TopSoftCount = 3;
        private const int ReasonMaxLength = 100;
        private const int InspectionPassIndex = 1;
        private const int YieldCardHeight = 110;

        private static readonly string[] InspectionItems = { "미실시", "합격", "불합격" };
        private static readonly string[] DecisionItems = { "Hold 유지", "추가 검사", "Release", "Scrap" };
        private static readonly OcapDecision[] DecisionOrder =
        {
            OcapDecision.Hold, OcapDecision.Retest, OcapDecision.Release, OcapDecision.Scrap
        };

        private readonly ApcComboBox _inspection = new ApcComboBox();
        private readonly ApcComboBox _decision = new ApcComboBox();
        private readonly ApcTextBox _reason = new ApcTextBox { MaxLength = ReasonMaxLength };
        private string _lotId = "";

        public OcapDecision Decision { get; private set; }
        public string Reason { get; private set; }

        public OcapDialog() : this(new SampleMainViewSource().GetLot("T04")) { }

        public OcapDialog(LotDetail lot)
        {
            _lotId = lot.LotId;
            SetupDialog("기준 미달 Lot 처리 (OCAP)", 760);

            AddContentRow(
                YieldCard("최종 수율", lot.FinalYield, YieldJudge.Judge(lot.FinalYield),
                    "양품 " + lot.FinalGood.ToString("#,0") + " / " + lot.Input.ToString("#,0")),
                YieldCard("최초 수율", lot.FirstYield, StatusKind.Run,
                    "재테스트 전 양품 " + lot.FirstPassGood.ToString("#,0")));

            AddContent(SectionTitle("주요 Soft Bin · 원인 확인"));
            AddContent(BuildSoftGrid(lot));

            var info = AddGroup("Lot 정보");
            AddFieldRow(info, "Lot ID", ReadOnlyBox(lot.LotId));
            AddFieldRow(info, "테스터", ReadOnlyBox(lot.TesterId));
            AddFieldRow(info, "고객 · 제품", ReadOnlyBox(lot.CustomerCode + " · " + lot.Product));

            var decide = AddGroup("판정");
            _inspection.Items.AddRange(InspectionItems);
            _inspection.SelectedIndex = 0;
            AddFieldRow(decide, "추가 검사", _inspection);
            _decision.Items.AddRange(DecisionItems);
            _decision.SelectedIndex = 0;
            AddFieldRow(decide, "판정", _decision);
            AddFieldRow(decide, "사유", _reason);
            AddContent(new Label
            {
                Text = "Release는 추가 검사 합격 후에만 저장된다.",
                AutoSize = true,
                Font = ApcTheme.GetFont(),
                ForeColor = ApcTheme.C(ApcTheme.Current.Colors.TextMuted),
            });

            var cancel = new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel };
            var save = new ApcButton { Text = "판정 저장", Variant = ApcButtonVariant.Primary };
            save.Click += OnSave;
            SetFooter(cancel, save);
        }

        private static ApcValueCard YieldCard(string title, double yield, StatusKind status, string footer)
        {
            return new ApcValueCard
            {
                Title = title,
                Value = yield.ToString("0.0"),
                Unit = "%",
                Status = status,
                Footer = footer,
                ShowLamp = true,
                Height = ApcTheme.Px(YieldCardHeight),
            };
        }

        private static ApcGrid BuildSoftGrid(LotDetail lot)
        {
            var grid = new ApcGrid { ReadOnly = true, Height = ApcGrid.HeightForRows(TopSoftCount) };
            foreach (string header in new[] { "Soft Bin", "원인", "Hard Bin", "수량" })
            {
                grid.Columns.Add(header, header);
            }
            grid.Columns["원인"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["원인"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            foreach (SoftBinCount soft in lot.SoftBins.Take(TopSoftCount))
            {
                grid.Rows.Add("SB" + soft.SoftBin, soft.Cause, soft.HardBin.ToString(), soft.Count.ToString("#,0"));
            }
            return grid;
        }

        private static ApcTextBox ReadOnlyBox(string text)
        {
            return new ApcTextBox { Text = text, ReadOnly = true };
        }

        private void OnSave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_reason.Text))
            {
                ApcDialog.Warn(this, "사유를 입력한다.");
                _reason.Focus();
                return;
            }

            OcapDecision decision = DecisionOrder[_decision.SelectedIndex];
            if (decision == OcapDecision.Release && _inspection.SelectedIndex != InspectionPassIndex)
            {
                ApcDialog.Warn(this, "추가 검사에 합격하지 않아 Release할 수 없다. 추가 검사를 먼저 한다.");
                return;
            }

            bool confirmed = decision == OcapDecision.Scrap
                ? ApcDialog.Confirm(this, "Scrap은 되돌릴 수 없다. Lot " + _lotId + "을 Scrap 처리할까요?")
                : ApcDialog.Confirm(this, _lotId + " " + _decision.SelectedItem + " 처리를 저장할까요?", "저장", ApcButtonVariant.Primary);
            if (!confirmed) return;

            Decision = decision;
            Reason = _reason.Text.Trim();
            DialogResult = DialogResult.OK;
        }
    }
}
