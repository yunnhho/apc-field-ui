using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using BumperPaintBoothMonitor.Data;

namespace BumperPaintBoothMonitor.Views
{
    /// <summary>
    /// 이탈 조치 등록. 부스·항목·이탈 구간·영향 행거는 메인 화면이 넘긴 값을 읽기 전용으로 보인다.
    /// 이탈이 아직 끝나지 않았으면 "영향 없음"은 등록 버튼이 꺼지고 이유가 버튼 옆에 나온다.
    /// 결과: Action, Remark (OK로 닫힐 때만 유효).
    /// </summary>
    internal sealed class ActionDialog : ApcForm
    {
        private static readonly ActionKind[] Choices = { ActionKind.Repaint, ActionKind.ExtraInspection, ActionKind.NoImpact };

        private readonly bool _ongoing;
        private readonly ApcComboBox _action = new ApcComboBox();
        private readonly ApcTextBox _remark = new ApcTextBox { Multiline = true, MaxLength = 200 };
        private readonly ApcButton _register = new ApcButton { Text = "조치 등록", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;

        /// <summary>render용 샘플: 이탈 중인 건 + "영향 없음" 선택 = 막힌 상태.</summary>
        public ActionDialog()
            : this(FirstOngoing(new SampleMainViewSource()), new SampleMainViewSource(), ActionKind.NoImpact) { }

        public ActionDialog(DeviationEvent e, IMainViewSource source, ActionKind preselect)
        {
            SetupDialog("이탈 조치 등록", 700);
            EventId = e.Id;
            _ongoing = e.Ongoing;
            var spec = BoothSpec.Get(e.Booth, e.Item);
            DateTime now = source.Now;
            HangerRange range = source.GetAffectedHangers(e.Start, e.End);

            var target = new ApcInfoStrip();
            target.SetItems("부스", "항목", "최대값");
            target.SetValue("부스", BoothSpec.BoothNames[e.Booth]);
            target.SetValue("항목", spec.Name + " (" + spec.Unit + ")");
            target.SetValue("최대값", spec.Format(e.Peak) + "  관리 " + spec.RangeText);
            target.Height = ApcTheme.Px(40);

            var span = new ApcInfoStrip();
            span.SetItems("시작", "종료", "지속");
            span.SetValue("시작", e.Start.ToString("HH:mm:ss"));
            span.SetValue("종료", e.Ongoing ? "진행 중" : e.End.Value.ToString("HH:mm:ss"));
            span.SetValue("지속", BoothSpec.DurationText(e.DurationAt(now)));
            span.Height = ApcTheme.Px(40);

            var hangers = new ApcInfoStrip();
            hangers.SetItems("영향 행거", "행거", "범퍼");
            hangers.SetValue("영향 행거", range.Count == 0 ? "없음" : range.Text + (e.Ongoing ? " (계속 투입 중)" : ""));
            hangers.SetValue("행거", range.Count + "개");
            hangers.SetValue("범퍼", range.Bumpers + "개");
            hangers.Height = ApcTheme.Px(40);

            AddContent(SectionTitle("이탈 내용"));
            AddContent(target);
            AddContent(span);
            AddContent(SectionTitle("영향 범위"));
            AddContent(hangers);
            AddContent(SectionTitle("조치"));

            foreach (var c in Choices) _action.Items.Add(ActionText.Of(c));
            _action.SelectedIndex = Array.IndexOf(Choices, preselect);
            _remark.Height = ApcTheme.Px(84);
            AddFieldRow("조치 구분", _action);
            AddFieldRow("비고", _remark);

            _register.Click += (s, ev) => Register();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _register);
            _note = SetFooterNote("");
            _action.SelectedIndexChanged += (s, ev) => UpdateGate();
            UpdateGate();
        }

        public int EventId { get; private set; }
        public ActionKind Action { get; private set; }
        public string Remark { get; private set; }

        private ActionKind Selected()
        {
            return _action.SelectedIndex < 0 ? ActionKind.None : Choices[_action.SelectedIndex];
        }

        // 막는 규칙은 한 곳: 선택이 없거나, 이탈 중인데 영향 없음이면 등록할 수 없다.
        private string BlockReason()
        {
            var kind = Selected();
            if (kind == ActionKind.None) return "조치 구분을 선택하세요";
            if (kind == ActionKind.NoImpact && _ongoing) return "이탈이 끝나지 않아 영향 없음으로 닫을 수 없음";
            return "";
        }

        private void UpdateGate()
        {
            string reason = BlockReason();
            _register.Enabled = reason.Length == 0;
            _note.Text = reason;
            _note.ForeColor = Selected() == ActionKind.NoImpact && _ongoing
                ? ApcTheme.StatusText(StatusKind.Alarm) : ApcTheme.C(ApcTheme.Current.Colors.Text);
        }

        private void Register()
        {
            string reason = BlockReason();
            if (reason.Length > 0) { ApcDialog.Warn(this, reason + "."); return; }
            if (!ApcDialog.Confirm(this, "'" + ActionText.Of(Selected()) + "'으로 조치를 등록할까요?", "등록", ApcButtonVariant.Primary)) return;
            Action = Selected();
            Remark = _remark.Text.Trim();
            DialogResult = DialogResult.OK;
        }

        private static DeviationEvent FirstOngoing(IMainViewSource source)
        {
            foreach (var e in source.GetDeviations()) if (e.Ongoing) return e;
            throw new InvalidOperationException("샘플에 이탈 중인 건이 없다.");
        }
    }
}
