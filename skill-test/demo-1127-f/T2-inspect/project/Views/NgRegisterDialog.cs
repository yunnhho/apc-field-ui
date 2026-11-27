using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using DiscFinalCheck.Data;

namespace DiscFinalCheck.Views
{
    /// <summary>
    /// NG 처리 등록: NG 디스크 정보(읽기 전용) + 처리 선택(재가공/폐기/재검사) + 반장 사원증 스캔.
    /// 처리를 고르고 반장 사원증(EMP-9nnnn)을 스캔하기 전에는 [등록]이 꺼져 있다. 사원증은 스캔으로만 들어온다.
    /// </summary>
    internal sealed class NgRegisterDialog : ApcForm
    {
        private static readonly Regex SupervisorBadge = new Regex(@"^EMP-9\d{4}$");
        private static readonly Regex AnyBadge = new Regex(@"^EMP-\d{5}$");
        private static readonly InspectItem[] Items = { InspectItem.OuterDiameter, InspectItem.Thickness, InspectItem.Runout, InspectItem.Parallelism };
        private static readonly NgAction[] Actions = { NgAction.Rework, NgAction.Scrap, NgAction.Retest };

        private readonly ApcComboBox _action = new ApcComboBox();
        private readonly ApcScanBox _badge = new ApcScanBox { ScanOnly = true };
        private readonly ApcButton _confirm = new ApcButton { Text = "등록", Variant = ApcButtonVariant.Primary };
        private readonly System.Windows.Forms.Label _note;

        public NgRegisterDialog() : this(new DiscMeasurement("BD-261127-0148", new DateTime(2026, 11, 27, 14, 32, 18), 280.14m, 22.02m, 0.024m, 0.008m)) { }

        public NgRegisterDialog(DiscMeasurement ng)
        {
            SetupDialog("NG 처리 등록", 640);   // 높이는 내용으로 자동
            DiscVerdict verdict = DiscSpec.Evaluate(ng);

            AddContent(SectionTitle("NG 디스크"));
            var strip = new ApcInfoStrip();
            strip.SetItems("시리얼", "검사 시각");
            strip.SetValue("시리얼", ng.Serial);
            strip.SetValue("검사 시각", ng.Time.ToString("HH:mm:ss"));
            AddContent(strip);
            AddContent(BuildResultGrid(ng, verdict));

            AddContent(SectionTitle("처리"));
            foreach (NgAction a in Actions) _action.Items.Add(DiscSpec.ActionName(a));
            _action.SelectedIndex = -1;
            AddFieldRow("처리 선택", _action);
            AddFieldRow("반장 사원증", _badge);

            _confirm.Enabled = false;
            _note = SetFooterNote("");
            _confirm.Click += (s, e) => Confirm();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm);   // Esc = 취소. Enter 기본 버튼 없음(스캔 Enter 방지)
            _badge.Attach(this);
            UpdateConfirm();

            _action.SelectedIndexChanged += (s, e) => UpdateConfirm();
            _badge.Scanned += (s, e) => OnBadgeScanned(e);
        }

        public NgAction Disposition { get; private set; }
        public string SupervisorBadgeId { get; private set; }

        private static ApcGrid BuildResultGrid(DiscMeasurement m, DiscVerdict v)
        {
            var grid = new ApcGrid { ReadOnly = true, Height = ApcGrid.HeightForRows(Items.Length) };
            grid.Columns.Add(new ApcLampColumn());
            grid.Columns.Add("item", "항목");
            grid.Columns.Add("value", "측정값(mm)");
            grid.Columns.Add("limit", "기준");
            grid.Columns.Add("result", "판정");
            foreach (DataGridViewColumn c in grid.Columns)
                if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (InspectItem item in Items)
            {
                decimal value = m.ValueOf(item);
                bool fail = v.Fails(item);
                int row = grid.Rows.Add(fail ? StatusKind.Alarm : StatusKind.Run, DiscSpec.ItemName(item), DiscSpec.Format(item, value),
                    DiscSpec.LimitText(item).Replace("기준 ", ""), fail ? "NG · " + (value > UpperOf(item) ? "상한 초과" : "하한 미달") : "OK");
                if (!fail) continue;
                var style = grid.Rows[row].Cells[2].Style;
                style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                style.Font = ApcTheme.GetFont(System.Drawing.FontStyle.Bold);
            }
            return grid;
        }

        private static decimal UpperOf(InspectItem item)
        {
            switch (item)
            {
                case InspectItem.OuterDiameter: return DiscSpec.OuterNominal + DiscSpec.OuterTolerance;
                case InspectItem.Thickness: return DiscSpec.ThicknessNominal + DiscSpec.ThicknessTolerance;
                case InspectItem.Runout: return DiscSpec.RunoutMax;
                default: return DiscSpec.ParallelismMax;
            }
        }

        private bool BadgeIsSupervisor { get { return SupervisorBadge.IsMatch(_badge.Text); } }

        private void OnBadgeScanned(ScanEventArgs e)
        {
            string code = e.Code.Trim();
            if (SupervisorBadge.IsMatch(code)) { e.Accepted = true; UpdateConfirm(); return; }
            e.Accepted = false;
            string message = AnyBadge.IsMatch(code) ? "반장 사원증이 아닙니다 · EMP-9로 시작하는 사원증을 스캔하세요"
                                                    : "사원증이 아닙니다 · EMP-nnnnn";
            BeginInvoke(new Action(() => ApcDialog.Warn(this, message)));
            UpdateConfirm();
        }

        private void UpdateConfirm()
        {
            bool hasAction = _action.SelectedIndex >= 0;
            _confirm.Enabled = hasAction && BadgeIsSupervisor;
            _note.Text = _confirm.Enabled ? "" : !hasAction ? "처리 선택 필요" : "반장 사원증 스캔 필요";
        }

        private void Confirm()
        {
            if (!_confirm.Enabled) return;
            NgAction action = Actions[_action.SelectedIndex];
            string disc = DiscSpec.ActionName(action);
            bool ok = action == NgAction.Scrap
                ? ApcDialog.Confirm(this, "폐기로 등록하면 이 시리얼은 다시 검사할 수 없습니다. 등록할까요?")
                : ApcDialog.Confirm(this, "처리 '" + disc + "'를 등록할까요?", "등록", ApcButtonVariant.Primary);
            if (!ok) return;
            Disposition = action;
            SupervisorBadgeId = _badge.Text;
            DialogResult = DialogResult.OK;
        }
    }
}
