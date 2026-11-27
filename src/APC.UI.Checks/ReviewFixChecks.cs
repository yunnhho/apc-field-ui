using System.Drawing;
using System.IO;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Checks
{
    /// <summary>최종 리뷰 Important 1~8 재현 점검.</summary>
    internal static partial class Program
    {
        private const int WM_KEYDOWN = 0x0100, WM_CHAR = 0x0102, VK_RETURN = 0x0D;
        private static string DefaultTheme { get { return Path.Combine(Root, Path.Combine("themes", "default")); } }

        private static void RunReviewFixChecks()
        {
            // #1 같은 테마 재로드 후 컨트롤 폰트가 살아 있어야 한다
            ApcTheme.Load(DefaultTheme);
            var btn = new ApcButton();
            ApcTheme.Load(DefaultTheme);
            Check(NoThrow(() => { var h = btn.Font.Height; }), "#1 font still usable after theme reload");

            // #2 스캔 Enter를 삼킨 뒤, 다음 사람 Enter의 WM_CHAR는 통과
            var box = new ApcScanBox();
            ScanChars(box, "LOT-0001", 1000, 10, null);
            Check(box.Process(WM_KEYDOWN, VK_RETURN, 1090, null), "#2 scan Enter keydown swallowed");
            Check(!box.Process(WM_CHAR, '\r', 5000, null), "#2 next human Enter char passes through");

            // #3 판정은 메시지 시각 기준: 사람이 300ms 간격으로 친 키가 한꺼번에 처리돼도 스캔 아님
            var box3 = new ApcScanBox();
            ScanChars(box3, "1200", 10000, 300, null);
            Check(!box3.Process(WM_KEYDOWN, VK_RETURN, 11500, null), "#3 queued human keys (by message time) not a scan");

            // #4 앱이 거부하면 피드백은 실패 한 번
            var box4 = new ApcScanBox();
            box4.Scanned += (s, e) => e.Accepted = false;
            ScanChars(box4, "BAD-0001", 20000, 10, null);
            box4.Process(WM_KEYDOWN, VK_RETURN, 20090, null);
            Check(box4.LastFlashSuccess == false && box4.FlashCount == 1, "#4 rejected scan flashes failure once");

            // #5 포커스 칸 원래 값(전체 선택 상태에서 덮어써짐)이 복원
            var field = new TextBox { Text = "120" };
            field.SelectAll();
            var box5 = new ApcScanBox();
            ScanChars(box5, "LOT-0005", 30000, 10, field);
            box5.Process(WM_KEYDOWN, VK_RETURN, 30090, field);
            Check(field.Text == "120", "#5 focused field restored after scan (was '" + field.Text + "')");
            Check(box5.Text == "LOT-0005", "#5 code moved to scan box");

            // #5b 숫자만 받는 칸(일부 글자만 들어감)도 복원
            var num = new TextBox { Text = "1" };
            num.SelectionStart = 1;
            var box5b = new ApcScanBox();
            ScanChars(box5b, "LOT-0001", 40000, 10, num, digitsOnly: true);
            box5b.Process(WM_KEYDOWN, VK_RETURN, 40090, num);
            Check(num.Text == "1", "#5b filtered field restored (was '" + num.Text + "')");

            CheckScanRouting();
            CheckScanOnly();

            // #7 내용은 창을 꽉 채우고(1px 테두리만), 가장자리 리사이즈는 그 위를 덮은 자식 컨트롤에서도 된다
            using (var f = new ApcForm { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-32000, -32000), Size = new Size(800, 600) })
            {
                var fill = new Panel { Dock = DockStyle.Fill };
                f.Controls.Add(fill);
                f.Show();
                Application.DoEvents();
                Check(f.Padding.Left == 1 && f.Padding.Right == 1 && f.Padding.Bottom == 1, "#7 resizable form content fills to 1px border");
                Check(f.EdgeHit(fill, new Point(1, 100)) == 10 && f.EdgeHit(fill, new Point(fill.Width - 2, fill.Height - 2)) == 17, "#7 edge resize works over a docked child (left, bottom-right)");
                Check(f.EdgeHit(fill, new Point(100, 100)) == 1, "#7 child interior stays client");
                f.Hide();
                f.Resizable = false;
                Check(f.EdgeHit(fill, new Point(1, 100)) == 1, "#7 non-resizable form has no edge resize");
                Check(f.Padding.Left == 1, "#7 non-resizable keeps 1px border");
                Check(f.ContentBounds.Left == f.Padding.Left, "#7 content bounds follow padding");
            }

            // #8 이미지가 빠진 테마는 Load 시점에 ThemeException (기존 테마 유지)
            Check(Throws<ThemeException>(() => ApcTheme.Load(Fx("alarm-dark")), "@1x.png"), "#8 missing assets rejected at load");
            Check(ApcTheme.ThemeDir == DefaultTheme, "#8 previous theme kept after failed load");
        }

        /// <summary>스캔 전용 칸: 손으로 치거나 고칠 수 없지만 입력칸 모양(흰 바탕)이고 스캔은 받는다.</summary>
        private static void CheckScanOnly()
        {
            using (var box = new ApcScanBox { ScanOnly = true })
            {
                string got = null;
                box.Scanned += (s, e) => got = e.Code;
                Check(box.ReadOnly, "scan only: typing and editing blocked (ReadOnly)");
                Check(box.BackColor == APC.UI.Drawing.FieldFrame.Fill(true, false), "scan only: looks like an input field, not read-only gray");
                ScanChars(box, "EMP-40001", 60000, 10, null);
                box.Process(WM_KEYDOWN, VK_RETURN, 60100, box);
                Check(got == "EMP-40001" && box.Text == "EMP-40001", "scan only: scan still fills the box (" + got + ")");
            }
        }

        /// <summary>스캔 칸 둘(시리얼 + 사원증): 코드는 Accepts가 받는 칸으로 간다 — 스캔 순서·포커스와 무관하게.</summary>
        private static void CheckScanRouting()
        {
            using (var f = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-32000, -32000), Size = new Size(400, 200) })
            {
                var serial = new ApcScanBox { Accepts = c => c.StartsWith("HC-") };
                var badge = new ApcScanBox { Accepts = c => c.StartsWith("EMP-"), Top = 40 };
                f.Controls.Add(serial);
                f.Controls.Add(badge);
                f.Show();
                serial.Attach(f);
                badge.Attach(f);
                string got = null;
                serial.Scanned += (s, e) => got = "serial:" + e.Code;
                badge.Scanned += (s, e) => got = "badge:" + e.Code;

                serial.Text = "HC-261127-0042";
                serial.SelectAll();
                ScanChars(serial, "EMP-10234", 50000, 10, serial);
                serial.Process(WM_KEYDOWN, VK_RETURN, 50100, serial);
                Check(got == "badge:EMP-10234", "scan route: badge code scanned in the focused serial box goes to the badge box (" + got + ")");
                Check(serial.Text == "HC-261127-0042", "scan route: serial box keeps its value (was '" + serial.Text + "')");

                got = null;
                ScanChars(serial, "XX-0001", 51000, 10, null);
                serial.Process(WM_KEYDOWN, VK_RETURN, 51080, null);
                Check(got == "serial:XX-0001", "scan route: code no box accepts stays with the receiving box (" + got + ")");

                got = null;
                badge.Enabled = false;
                ScanChars(serial, "EMP-20001", 52000, 10, null);
                serial.Process(WM_KEYDOWN, VK_RETURN, 52100, null);
                Check(got == "serial:EMP-20001", "scan route: disabled box does not take codes (" + got + ")");

                got = null;
                badge.Enabled = true;
                f.Controls.Remove(badge);   // 화면 교체로 떼어냈지만 Dispose 안 한 칸
                ScanChars(serial, "EMP-30001", 53000, 10, null);
                serial.Process(WM_KEYDOWN, VK_RETURN, 53100, null);
                Check(got == "serial:EMP-30001", "scan route: box removed from the form does not take codes (" + got + ")");
                badge.Dispose();
                f.Hide();
            }
        }

        /// <summary>WM_CHAR를 필터에 넣고, 필터가 통과시킨 글자는 실제 디스패치처럼 field에 입력한다.</summary>
        private static void ScanChars(ApcScanBox box, string s, long t0, int gap, TextBox field, bool digitsOnly = false)
        {
            long t = t0;
            foreach (char c in s)
            {
                t += gap;
                bool swallowed = box.Process(WM_CHAR, c, t, field);
                if (!swallowed && field != null && (!digitsOnly || char.IsDigit(c))) field.SelectedText = c.ToString();
            }
        }

        private static bool NoThrow(System.Action a)
        {
            try { a(); return true; }
            catch (System.Exception) { return false; }
        }
    }
}
