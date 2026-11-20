using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Demo.Templates;
using APC.UI.Theming;

namespace APC.UI.Demo.Catalog
{
    /// <summary>컨트롤 카탈로그: 모든 컨트롤 × 상태를 한 화면에.</summary>
    internal sealed class ControlCatalog : ApcTemplateView
    {
        public ControlCatalog() : base("컨트롤 카탈로그 — " + ApcTheme.Current.Name)
        {
            Body.AutoScroll = true;

            // 아래에서 위로 쌓인다(Top 도킹은 나중 추가가 위) → 보여줄 순서의 역순으로 추가
            var alarmDemo = Button("알람 추가", ApcButtonVariant.Danger);
            int n = 0;
            alarmDemo.Click += (s, e) => Alarm.Push("테스트 알람 #" + (++n));
            var confirm = Button("확인창 열기", ApcButtonVariant.Normal, 130);
            confirm.Click += (s, e) => MessageBox.Show(ApcDialog.Confirm(FindForm(), "정말 삭제할까요? 되돌릴 수 없습니다.") ? "확인" : "취소");
            var save = Button("저장 확인창", ApcButtonVariant.Normal, 130);
            save.Click += (s, e) => MessageBox.Show(ApcDialog.Confirm(FindForm(), "레시피를 저장할까요?", "저장", ApcButtonVariant.Primary) ? "저장" : "취소");
            var warn = Button("경고창", ApcButtonVariant.Normal, 110);
            warn.Click += (s, e) => ApcDialog.Warn(FindForm(), "하중 하한이 상한보다 큽니다.");
            var info = Button("알림창", ApcButtonVariant.Normal, 110);
            info.Click += (s, e) => ApcDialog.Alert(FindForm(), "COM3 연결되었습니다.");
            Body.Controls.Add(Row(Caption("알람 띠 / 확인창"), alarmDemo, confirm, save, warn, info));

            var grid = new ApcGrid { Height = S(150), Dock = DockStyle.Top, ReadOnly = true };
            grid.Columns.Add(new ApcLampColumn());
            grid.Columns.Add("a", "LOT");
            grid.Columns.Add("b", "품목");
            grid.Columns["b"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns.Add(new ApcIconColumn { HeaderText = "전송", IconName = "icon_play" });
            grid.Columns.Add(new ApcIconColumn { HeaderText = "초기화", IconName = "icon_reset" });
            grid.Columns.Add(new ApcLampColumn { HeaderText = "상태(램프+문구)", Width = S(140) });
            foreach (StatusKind k in Enum.GetValues(typeof(StatusKind)))
            {
                bool can = k != StatusKind.Stop;   // 아이콘: true = 누를 수 있음(파랑), false = 회색
                grid.Rows.Add(k, "LOT-0" + (int)k, k.ToString(), can, can, new ApcLampValue(k, k == StatusKind.Stop ? "대기" : k.ToString()));
            }
            Body.Controls.Add(grid);
            Body.Controls.Add(Row(Caption("그리드 (ApcGrid + ApcLampColumn·ApcLampValue + ApcIconColumn)")));

            var combo = new ApcComboBox { Width = S(160) };
            combo.Items.AddRange(new object[] { "HB-6204", "HB-6205", "HB-6306" });
            combo.SelectedIndex = 0;
            var comboOff = new ApcComboBox { Width = S(120), Enabled = false };
            comboOff.Items.Add("비활성");
            comboOff.SelectedIndex = 0;
            var num = new ApcNumericBox { Minimum = 0, Maximum = 50, DecimalPlaces = 1, Increment = 0.5m, Value = 18 };
            var date = new ApcDateBox();
            foreach (Control c in new Control[] { combo, comboOff, num, date }) c.Margin = new Padding(0, 0, S(8), 0);
            Body.Controls.Add(Row(Caption("선택·숫자·날짜"), combo, comboOff, num, date));

            var text = new ApcTextBox { Width = S(200), Text = "입력칸" };
            var readOnly = new ApcTextBox { Width = S(160), Text = "읽기 전용", ReadOnly = true };
            var off = new ApcTextBox { Width = S(160), Text = "비활성", Enabled = false };
            foreach (Control c in new Control[] { text, readOnly, off }) c.Margin = new Padding(0, 0, S(8), 0);
            Body.Controls.Add(Row(Caption("입력칸"), text, readOnly, off));

            var tabs = new ApcTabStrip();
            tabs.SetTabs("저속 연마기 ST-GR-01~03", "저속 연마기 ST-GR-04~06", "고속 연마기 ST-GB-01~08");
            tabs.Width = tabs.PreferredTabsWidth;
            Body.Controls.Add(Row(Caption("탭 (ApcTabStrip)"), tabs));

            var scan = new ApcScanBox { Width = S(260) };
            var ok = Button("성공 플래시", ApcButtonVariant.Normal, 120);
            ok.Click += (s, e) => scan.Flash(true);
            var bad = Button("실패 플래시", ApcButtonVariant.Normal, 120);
            bad.Click += (s, e) => scan.Flash(false);
            Body.Controls.Add(Row(Caption("스캔 입력"), scan, ok, bad));

            var lamps = Row(Caption("상태 램프"));
            foreach (StatusKind k in Enum.GetValues(typeof(StatusKind)))
            {
                lamps.Controls.Add(new ApcLamp { Status = k, Blink = k == StatusKind.Alarm, Size = new Size(S(28), S(28)), Margin = new Padding(S(4), S(4), 0, 0) });
                var l = Caption(k.ToString());
                l.Margin = new Padding(S(2), S(8), S(12), 0);
                l.Font = ApcTheme.GetFont();
                lamps.Controls.Add(l);
            }
            Body.Controls.Add(lamps);

            foreach (ApcButtonVariant v in new[] { ApcButtonVariant.Danger, ApcButtonVariant.Primary, ApcButtonVariant.Normal })
            {
                var enabled = Button(v + " 버튼", v, 130);
                var disabled = Button(v + " 비활성", v, 130);
                disabled.Enabled = false;
                var wide = Button(v + " — 넓게 늘려도 모서리 유지", v, 300);
                Body.Controls.Add(Row(Caption(v.ToString().PadRight(8)), enabled, disabled, wide));
            }
            Body.Controls.Add(Row(Caption("버튼: 마우스를 올리면 hover, 누르면 pressed. Tab으로 포커스 표시 확인")));
        }
    }
}
