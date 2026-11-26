using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using BumperPaintBoothMonitor.Data;

namespace BumperPaintBoothMonitor.Views
{
    /// <summary>
    /// 이탈 조치 등록. 부스·항목·구간·영향 행거는 읽기 전용(데이터에서 받음), 입력은 조치 구분·확인자·비고뿐.
    /// 이탈이 끝나지 않았으면 "영향 없음"은 확정할 수 없다 — 이탈 여부는 데이터이므로 이 화면에서 바꿀 수 없다.
    /// </summary>
    internal sealed class ExcursionActionDialog : ApcForm
    {
        private const int DialogWidth = 860, StripWidth = 800;

        private readonly Excursion _excursion;
        private readonly ApcComboBox _action = new ApcComboBox();
        private readonly ApcScanBox _confirmer = new ApcScanBox();
        private readonly ApcTextBox _memo = new ApcTextBox();
        private readonly ApcButton _save = new ApcButton { Text = "등록", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;

        /// <summary>render용 샘플: 이탈 진행 중 + "영향 없음" 선택 = 막힌 상태.</summary>
        public ExcursionActionDialog() : this(new SampleMainViewSource().GetExcursions()[0], new SampleMainViewSource().Now, Excursion.ActionNoImpact) { }

        public ExcursionActionDialog(Excursion excursion, DateTime now, string initialAction = null)
        {
            _excursion = excursion;
            SetupDialog("이탈 조치 등록", DialogWidth);

            var who = new ApcInfoStrip { Width = ApcTheme.Px(StripWidth) };
            who.SetItems("부스", "항목", "최대값");
            who.SetValue("부스", excursion.Booth);
            who.SetValue("항목", excursion.ItemName);
            who.SetValue("최대값", excursion.PeakText);
            AddContent(who);

            var span = new ApcInfoStrip { Width = ApcTheme.Px(StripWidth) };
            span.SetItems("이탈 구간", "영향 행거");
            span.SetValue("이탈 구간", excursion.Start.ToString("HH:mm:ss") + " ~ "
                + (excursion.End.HasValue ? excursion.End.Value.ToString("HH:mm:ss") : "이탈 중")
                + " · " + Excursion.DurationText(excursion.Duration(now)));
            span.SetValue("영향 행거", excursion.HangerCount == 0 ? "없음"
                : excursion.HangerRange + " · " + excursion.HangerCount + "개 (범퍼 " + excursion.HangerCount * 2 + "개)");
            AddContent(span);

            AddContent(SectionTitle("조치"));
            _action.Items.AddRange(new object[] { Excursion.ActionRepaint, Excursion.ActionInspect, Excursion.ActionNoImpact });
            _action.SelectedIndexChanged += (s, e) => UpdateBlock();
            AddFieldRow("조치 구분", _action);
            AddFieldRow("확인자", _confirmer);
            AddFieldRow("비고", _memo);

            _save.Click += (s, e) => Save();
            _note = SetFooterNote("");
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _save);

            if (initialAction != null) _action.SelectedItem = initialAction;
            UpdateBlock();
        }

        public string Action { get; private set; }
        public string Confirmer { get; private set; }
        public string Memo { get; private set; }

        private bool Blocked { get { return _excursion.Ongoing && (string)_action.SelectedItem == Excursion.ActionNoImpact; } }

        private void UpdateBlock()
        {
            _save.Enabled = !Blocked;
            _note.Text = Blocked ? "이탈 진행 중 · 영향 없음으로 닫을 수 없음" : "";
            _note.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
        }

        private void Save()
        {
            if (Blocked) return;
            if (_action.SelectedItem == null) { Reject("조치 구분을 선택하세요.", _action); return; }
            if (_confirmer.Text.Trim().Length == 0) { Reject("확인자를 스캔하거나 입력하세요.", _confirmer); return; }
            string action = (string)_action.SelectedItem;
            if (!ApcDialog.Confirm(this, _excursion.Booth + " " + _excursion.ItemName + " 이탈을 '" + action + "'로 등록할까요?", "등록", ApcButtonVariant.Primary)) return;
            Action = action; Confirmer = _confirmer.Text.Trim(); Memo = _memo.Text.Trim();
            DialogResult = DialogResult.OK;
        }

        private void Reject(string message, Control field)
        {
            ApcDialog.Warn(this, message);
            field.Focus();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _confirmer.Attach(this);   // 사원증 스캔을 어느 칸에 포커스가 있어도 확인자로 받는다
        }
    }
}
