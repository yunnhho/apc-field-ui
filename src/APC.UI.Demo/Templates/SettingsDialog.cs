using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>
    /// 통신·장비 설정 dialog 골격(참고: 연마기 불량 방지 시스템 설정). 블록(설비 묶음)이 여러 개면 묶음 하나에 블록마다 한 행,
    /// 칸 제목(COM·신호·로직·IP·포트)은 AddCaptionRow로 위에 한 번만. 아래 공통 설정 묶음, 맨 아래 왼쪽 [비밀번호 변경] / 오른쪽 [저장][취소].
    /// 저장은 Confirm("저장") 뒤 결과를 넘긴다. 실제 연결 시도·저장은 부른 쪽 데이터 인터페이스가 한다.
    /// </summary>
    internal sealed class SettingsDialog : ApcForm
    {
        private static readonly string[] Ports = { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6" };
        private static readonly string[] Signals = { "INB", "RESET", "START" };
        private static readonly string[] Logics = { "LOW", "HIGH" };

        public SettingsDialog() : this(new[] { "#1  저속 연마기 ST-GR-01~03", "#2  저속 연마기 ST-GR-04~06", "#3  고속 연마기 ST-GB-01~08" }) { }

        public SettingsDialog(string[] blocks)
        {
            SetupDialog("시스템 설정", 1000);

            var group = AddGroup("블럭별 통신");
            AddCaptionRow(group, "블럭", "타이머 COM", "신호", "로직", "속도계 IP", "포트");
            for (int i = 0; i < blocks.Length; i++)
            {
                var port = Ports[i % Ports.Length];
                var connect = new ApcButton { Text = "연결 시도", Width = ApcTheme.Px(100) };
                connect.Click += (s, e) => ApcDialog.Alert(this, port + " 연결 시도 결과를 여기에 표시(샘플)");
                AddFieldRow(group, blocks[i], Combo(Ports, i % Ports.Length, 110),
                    Combo(Signals, i == 2 ? 1 : 0, 110), Combo(Logics, 0, 100),
                    new ApcTextBox { Text = "192.168.0." + (101 + i), Width = ApcTheme.Px(140) },
                    new ApcNumericBox { Minimum = 1, Maximum = 65535, Value = 502, Width = ApcTheme.Px(90) }, connect);
            }
            AddFieldRow(AddGroup("블럭 통신 공통"), "응답대기(ms)", new ApcNumericBox { Minimum = 10, Maximum = 5000, Increment = 10, Value = 100, Width = ApcTheme.Px(110) },
                FieldCaption("속도"), Combo(new[] { "9600", "19200", "38400", "115200" }, 0, 120),
                FieldCaption("갱신주기(ms)"), new ApcNumericBox { Minimum = 100, Maximum = 10000, Increment = 100, Value = 500, Width = ApcTheme.Px(110) });

            var password = new ApcButton { Text = "비밀번호 변경" };
            password.Click += (s, e) => { using (var d = new AdminPasswordDialog("비밀번호 변경")) d.ShowDialog(this); };
            var save = new ApcButton { Text = "저장", Variant = ApcButtonVariant.Primary };
            save.Click += (s, e) => { if (ApcDialog.Confirm(this, "설정을 저장할까요?", "저장", ApcButtonVariant.Primary)) DialogResult = DialogResult.OK; };
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, save, password);
        }

        private static ApcComboBox Combo(string[] items, int selected, int width)
        {
            var c = new ApcComboBox { Width = ApcTheme.Px(width) };
            c.Items.AddRange(items);
            c.SelectedIndex = selected;
            return c;
        }
    }
}
