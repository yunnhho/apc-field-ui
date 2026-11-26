using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using BumperPaintBoothMonitor.Data;

namespace BumperPaintBoothMonitor.Views
{
    /// <summary>
    /// 이탈 조치 등록. 부스·항목·구간·영향 행거는 메인 화면에서 받아 읽기 전용으로 보이고, 이 dialog가 하는 입력은 조치 구분·확인자·비고뿐이다.
    /// 이탈이 아직 끝나지 않았으면 "영향 없음"은 등록할 수 없다(등록 버튼이 꺼지고 이유가 버튼 옆에 보인다).
    /// </summary>
    internal sealed class ExcursionActionDialog : ApcForm
    {
        public const string Repaint = "재도장", Inspect = "추가 검사", NoImpact = "영향 없음";

        private readonly bool _ongoing;
        private readonly ApcComboBox _action = new ApcComboBox();
        private readonly ApcComboBox _who = new ApcComboBox();
        private readonly ApcTextBox _note = new ApcTextBox();
        private readonly ApcButton _save = new ApcButton { Text = "등록", Variant = ApcButtonVariant.Primary };
        private readonly Label _reason;

        // render용 샘플: 진행 중 이탈에 "영향 없음"이 골라진 막힌 상태로 시작
        public ExcursionActionDialog()
            : this(new Excursion(1, "클리어", "습도", new DateTime(2026, 11, 26, 14, 26, 40), null, "77", 480, 482, "", "", ""),
                new DateTime(2026, 11, 26, 14, 30, 0), new[] { "김도장", "이품질", "박반장" }, NoImpact) { }

        public ExcursionActionDialog(Excursion ex, DateTime now, string[] confirmers, string initialAction)
        {
            SetupDialog("이탈 조치 등록", 860);
            _ongoing = ex.Ongoing;

            var spot = new ApcInfoStrip { Height = ApcTheme.Px(40) };
            spot.SetItems("부스", "항목", "시작", "종료");
            spot.SetValue("부스", ex.Booth);
            spot.SetValue("항목", ex.Item);
            spot.SetValue("시작", ex.Start.ToString("HH:mm:ss"));
            spot.SetValue("종료", ex.Ongoing ? "진행 중" : ex.End.Value.ToString("HH:mm:ss"));
            var hangers = new ApcInfoStrip { Height = ApcTheme.Px(40) };
            hangers.SetItems("영향 행거", "지속");
            hangers.SetValue("영향 행거", HangerLine(ex));
            hangers.SetValue("지속", BoothJudge.DurationText(ex.Duration(now)));

            _action.DropDownStyle = ComboBoxStyle.DropDownList;
            _action.Items.AddRange(new object[] { Repaint, Inspect, NoImpact });
            _action.SelectedItem = initialAction;
            _who.DropDownStyle = ComboBoxStyle.DropDownList;
            _who.Items.AddRange(confirmers);
            if (confirmers.Length > 0) _who.SelectedIndex = 0;
            _note.MaxLength = 100;

            AddContent(SectionTitle("이탈 구간"));
            AddContent(spot);
            AddContent(hangers);
            AddContent(SectionTitle("조치"));
            AddFieldRow("조치 구분", _action);
            AddFieldRow("확인자", _who);
            AddFieldRow("비고", _note);

            _reason = SetFooterNote("");
            _reason.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            _save.Click += (s, e) => Save();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _save);
            _action.SelectedIndexChanged += (s, e) => UpdateState();
            UpdateState();
        }

        public string ActionKind { get; private set; }
        public string Note { get; private set; }
        public string Confirmer { get; private set; }

        private static string HangerLine(Excursion ex)
        {
            if (ex.HangerCount == 0) return "없음";
            string text = ex.HangerText + " (" + ex.HangerCount + "개 · 범퍼 " + ex.HangerCount * BoothJudge.BumpersPerHanger + "개)";
            return text;
        }

        // 막는 규칙은 호출한 쪽이 준 Ongoing으로만 정해진다 — 이 dialog의 입력으로 풀 수 없다
        private bool Blocked { get { return _ongoing && (string)_action.SelectedItem == NoImpact; } }

        private void UpdateState()
        {
            _save.Enabled = !Blocked;
            _reason.Text = Blocked ? "이탈 진행 중 · 영향 없음 등록 불가" : "";
        }

        private void Save()
        {
            if (Blocked) return;
            if (_action.SelectedItem == null) { ApcDialog.Warn(this, "조치 구분을 선택하세요."); _action.Focus(); return; }
            if (_who.SelectedItem == null) { ApcDialog.Warn(this, "확인자를 선택하세요."); _who.Focus(); return; }
            ActionKind = (string)_action.SelectedItem;
            Confirmer = (string)_who.SelectedItem;
            Note = _note.Text.Trim();
            DialogResult = DialogResult.OK;
        }
    }
}
