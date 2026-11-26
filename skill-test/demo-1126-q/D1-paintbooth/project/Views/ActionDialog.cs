using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using BumperPaintBoothMonitor.Data;

namespace BumperPaintBoothMonitor.Views
{
    /// <summary>
    /// 이탈 조치 등록. 부스·항목·구간·영향 행거는 읽기 전용(메인 화면 데이터).
    /// 이탈이 아직 끝나지 않았으면 "영향 없음"은 고를 수 있어도 등록 버튼이 꺼진다(구간 종료 여부는 데이터가 정한다).
    /// </summary>
    internal sealed class ActionDialog : ApcForm
    {
        private const string Redo = "재도장", Inspect = "추가 검사", NoEffect = "영향 없음";

        private readonly Deviation _dev;
        private readonly ApcComboBox _action = new ApcComboBox();
        private readonly ApcTextBox _remark = new ApcTextBox { MaxLength = 100 };
        private readonly ApcButton _ok = new ApcButton { Text = "등록", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;

        // render용: 진행 중 이탈 + 영향 없음 선택 = 막힌 상태
        public ActionDialog() : this(new SampleMainViewSource().GetDeviations()[0], SampleMainViewSource.SampleNow, "김도장", NoEffect) { }

        public ActionDialog(Deviation dev, DateTime now, string confirmer) : this(dev, now, confirmer, dev.Open ? Inspect : NoEffect) { }

        private ActionDialog(Deviation dev, DateTime now, string confirmer, string initial)
        {
            _dev = dev;
            SetupDialog("이탈 조치 등록", 600);

            string item = dev.Spec.Name + " (" + dev.Spec.Unit + ")";
            string risk = dev.Risk();
            string span = dev.Start.ToString("HH:mm:ss") + " ~ " + (dev.Open ? "진행 중" : dev.End.Value.ToString("HH:mm:ss"))
                + " · " + FormatDuration(dev.Duration(now));
            int n = dev.HangerCount(now);

            AddContent(SectionTitle("이탈 내용"));
            AddFieldRow("부스", Read(dev.Booth));
            AddFieldRow("항목", Read(risk.Length > 0 ? item + " · " + risk : item));
            AddFieldRow("이탈 구간", Read(span));
            AddFieldRow("영향 행거", Read(dev.HangerRange(now) + " · " + n + "개 (범퍼 " + n * Deviation.BumpersPerHanger + "개)"));

            AddContent(SectionTitle("조치"));
            _action.Items.AddRange(new object[] { Redo, Inspect, NoEffect });
            _action.SelectedItem = initial;
            _action.SelectedIndexChanged += (s, e) => UpdateState();
            AddFieldRow("조치 구분", _action);
            AddFieldRow("비고", _remark);
            AddFieldRow("확인자", Read(confirmer));

            _note = SetFooterNote("");
            _ok.Click += (s, e) => Register();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _ok);   // Esc = 취소, Enter 기본 버튼 없음
            UpdateState();
        }

        public string ChosenAction { get { return (string)_action.SelectedItem; } }
        public string Remark { get { return _remark.Text.Trim(); } }

        private bool Blocked { get { return _dev.Open && ChosenAction == NoEffect; } }

        private void UpdateState()
        {
            _ok.Enabled = !Blocked;
            _note.Text = Blocked ? "이탈 진행 중 · 영향 없음으로 닫을 수 없음" : "";
            _note.ForeColor = Blocked ? ApcTheme.StatusText(StatusKind.Alarm) : _note.ForeColor;
        }

        private void Register()
        {
            if (Blocked) return;
            if (!ApcDialog.Confirm(this, "'" + ChosenAction + "'(으)로 조치를 등록할까요?", "등록", ApcButtonVariant.Primary)) return;
            DialogResult = DialogResult.OK;
        }

        private static ApcTextBox Read(string text) { return new ApcTextBox { Text = text, ReadOnly = true }; }

        public static string FormatDuration(TimeSpan t)
        {
            return ((int)t.TotalHours).ToString("00") + ":" + t.Minutes.ToString("00") + ":" + t.Seconds.ToString("00");
        }
    }
}
