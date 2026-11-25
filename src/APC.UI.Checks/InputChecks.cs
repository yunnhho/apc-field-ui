using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Checks
{
    internal static partial class Program
    {
        // 입력 중인 칸만 연한 파랑 바탕(11/24 결정: 입력칸 ② 강조). 포커스를 옮기면 원래 흰 바탕으로.
        private static void CheckFocusedFieldFill()
        {
            using (var f = new ApcForm { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-32000, -32000) })
            {
                var a = new ApcTextBox { Top = 60, Width = 150 };
                var b = new ApcTextBox { Top = 120, Width = 150 };
                f.Controls.Add(a); f.Controls.Add(b);
                f.Show();
                Application.DoEvents();
                a.Focus(); Application.DoEvents();
                int fill = APC.UI.Drawing.FieldFrame.FocusFill.ToArgb(), white = Color.White.ToArgb();
                bool first = a.Focused && a.BackColor.ToArgb() == fill && b.BackColor.ToArgb() == white;
                b.Focus(); Application.DoEvents();
                Check(first && b.Focused && b.BackColor.ToArgb() == fill && a.BackColor.ToArgb() == white, "textbox: focused field gets focus fill, others white");
            }
        }

        private static void RunInputChecks()
        {
            RunTabStripChecks();
            int fieldH = (int)Math.Round(ApcTextBox.BaseHeight * ApcTheme.Scale);

            using (var box = new ApcTextBox { Width = 200 })
            {
                var h = box.Handle;   // 비클라이언트 계산은 핸들이 있어야 일어난다
                Check(box.Height == fieldH, "textbox: default height 34*dpi (was " + box.Height + ")");
                Check(box.ClientSize.Height == box.Font.Height, "textbox: client area is one text line (vertically centered)");
                Check(box.ClientSize.Width < box.Width, "textbox: side padding reserved");
                var ins = box.FrameInsets(box.Height);
                Check(Math.Abs(ins.Top - ins.Bottom) <= 1, "textbox: text line centered (top " + ins.Top + " / bottom " + ins.Bottom + ")");
                box.Height = 10;
                Check(box.Height >= ApcTheme.MinHeight, "textbox: clamps to min height");
                box.Height = fieldH;
                using (var bmp = new Bitmap(box.Width, box.Height))
                {
                    box.DrawToBitmap(bmp, new Rectangle(0, 0, box.Width, box.Height));
                    Check(bmp.GetPixel(0, box.Height / 2).ToArgb() == ApcTheme.C(ApcTheme.Current.Colors.BorderStrong).ToArgb(),
                        "textbox: render draws themed border (WM_PRINT path)");
                }
                box.ReadOnly = true;
                Check(box.BackColor.ToArgb() == ApcTheme.C(ApcTheme.Current.Colors.GridLine).ToArgb(), "textbox: read-only fill");
            }
            Check(typeof(ApcTextBox).IsAssignableFrom(typeof(ApcScanBox)), "scanbox shares the field look (derives ApcTextBox)");
            CheckFocusedFieldFill();

            using (var cb = new ApcComboBox())
            {
                cb.Items.AddRange(new object[] { "A", "B" });
                var h = cb.Handle;
                Check(cb.DropDownStyle == ComboBoxStyle.DropDownList, "combo: list only");
                Check(cb.Height == fieldH, "combo: same height as text field (was " + cb.Height + ")");
            }

            decimal v;
            Check(ApcNumericBox.TryParse("12.345", 0, 100, 2, out v) && v == 12.35m, "numeric: rounds to decimal places");
            Check(ApcNumericBox.TryParse("150", 0, 100, 0, out v) && v == 100m, "numeric: clamps to maximum");
            Check(ApcNumericBox.TryParse("-5", 0, 100, 0, out v) && v == 0m, "numeric: clamps to minimum");
            Check(!ApcNumericBox.TryParse("abc", 0, 100, 0, out v), "numeric: rejects non-number");
            using (var nb = new ApcNumericBox { Maximum = 10 })
            {
                int changed = 0;
                nb.ValueChanged += (s, e) => changed++;
                nb.Value = 20;
                Check(nb.Value == 10 && nb.TextBox.Text == "10" && changed == 1, "numeric: value clamped and shown, event once");
                nb.TextBox.Text = "x";
                nb.Commit();
                Check(nb.Value == 10 && nb.TextBox.Text == "10", "numeric: invalid text reverts");
                nb.TextBox.Text = "7";
                nb.Commit();
                Check(nb.Value == 7 && changed == 2, "numeric: typed value committed");
                nb.Minimum = 8;
                Check(nb.Value == 8, "numeric: raising minimum re-clamps value");

                // 배치: 버튼은 칸 안 오른쪽(콤보와 같은 모양) — 별도 버튼 컨트롤 없이 ▲ 위 / ▼ 아래
                nb.Size = new Size(ApcTheme.Px(150), fieldH);
                nb.PerformLayout();
                var area = nb.ButtonArea;
                Check(nb.Controls.Count == 1 && nb.TextBox.Right <= area.Left && area.Right == nb.Width && area.Height == nb.Height,
                    "numeric: up/down area inside the field, right of the text");
                Check(nb.SegmentAt(new Point(area.Left + 2, 2)) == 0 && nb.SegmentAt(new Point(area.Left + 2, nb.Height - 3)) == 1
                    && nb.SegmentAt(new Point(2, 2)) == -1, "numeric: top half = up, bottom half = down");
                nb.OnSegmentClick(0);
                bool up = nb.Value == 9;
                nb.OnSegmentClick(1);
                Check(up && nb.Value == 8, "numeric: up/down step by increment");
            }

            DateTime d;
            Check(ApcDateBox.TryParse("20261117", out d) && d == new DateTime(2026, 11, 17), "date: accepts yyyyMMdd");
            Check(!ApcDateBox.TryParse("2026-13-01", out d), "date: rejects bad month");
            Check(!ApcDateBox.TryParse("0001-01-01", out d), "date: rejects dates outside 1900~2999");
            using (var edge = new ApcDateBox { Value = DateTime.MinValue })
                Check(edge.Value == ApcDateBox.MinDate, "date: value clamped to MinDate");
            Check(TrianglePixels(true) == TrianglePixels(false) && TrianglePixels(true) > 0, "field: up and down triangles are the same size");
            using (var db = new ApcDateBox())
            {
                db.PerformLayout();
                Check(db.Controls.Count == 1 && db.TextBox.Right <= db.ButtonArea.Left && db.ButtonArea.Right == db.Width
                    && db.SegmentAt(new Point(db.Width - 3, db.Height / 2)) == 0, "date: calendar button area inside the field");
            }
            using (var cal = new ApcCalendar(ApcDateBox.MinDate))
            {
                cal.ShiftMonth(-1);
                Check(cal.Month == ApcDateBox.MinDate, "calendar: cannot move before MinDate");
            }
            using (var multi = new ApcTextBox { Multiline = true })
                Check(multi.BorderStyle == BorderStyle.FixedSingle, "textbox: multiline falls back to native border (room for scrollbars)");
            using (var db = new ApcDateBox { Value = new DateTime(2026, 11, 17, 15, 30, 0) })
            {
                Check(db.TextBox.Text == "2026-11-17" && db.Value.TimeOfDay == TimeSpan.Zero, "date: shows yyyy-MM-dd, drops time");
                db.TextBox.Text = "어제";
                db.Commit();
                Check(db.TextBox.Text == "2026-11-17", "date: invalid text reverts");
            }

            Check(ApcCalendar.FirstCell(new DateTime(2026, 12, 1)) == new DateTime(2026, 11, 29), "calendar: grid starts on Sunday before the 1st");
            Check(ApcCalendar.FirstCell(new DateTime(2026, 11, 20)) == new DateTime(2026, 11, 1), "calendar: month starting Sunday has no lead days");
            using (var cal = new ApcCalendar(new DateTime(2026, 12, 15)))
            {
                int s40 = (int)Math.Round(40 * ApcTheme.Scale), s28 = (int)Math.Round(28 * ApcTheme.Scale);
                Check(cal.DateAt(new Point(1, s40 + s28 + 1)) == new DateTime(2026, 11, 29), "calendar: first cell hit");
                Check(cal.DateAt(new Point(s40 * 6 + 1, s40 + s28 + s40 * 5 + 1)) == new DateTime(2027, 1, 9), "calendar: last cell hit");
                Check(cal.DateAt(new Point(5, 5)) == null, "calendar: header is not a date");
                cal.ShiftMonth(1);
                Check(cal.Month == new DateTime(2027, 1, 1), "calendar: next month crosses year");
            }

            using (var dlg = ApcDialog.Create("저장할까요?", "저장", ApcButtonVariant.Primary))
            {
                ApcButton ok = null;
                foreach (Control c in dlg.Controls) { var b = c as ApcButton; if (b != null && b.DialogResult == DialogResult.OK) ok = b; }
                Check(ok != null && ok.Text == "저장" && ok.Variant == ApcButtonVariant.Primary, "confirm: ok text/variant overridable");
                Check(dlg.AcceptButton == dlg.CancelButton, "confirm: Enter still cancels with custom ok");
            }
            using (var warn = ApcDialog.CreateAlert("하중 하한이 상한보다 큽니다", true))
            using (var info = ApcDialog.CreateAlert("COM3 연결됨", false))
            {
                Func<Form, int> icons = f => { int n = 0; foreach (Control c in f.Controls) if (c is PictureBox) n++; return n; };
                Check(warn.TitleText == "경고" && info.TitleText == "알림" && icons(warn) == 1 && icons(info) == 1, "alert: warning and info differ by title and lamp");
            }
            using (var alert = ApcDialog.CreateAlert("하중 하한이 상한보다 큽니다", true))
            {
                int buttons = 0;
                foreach (Control c in alert.Controls) if (c is ApcButton) buttons++;
                var only = alert.AcceptButton as ApcButton;
                Check(buttons == 1 && only != null && alert.CancelButton == only && only.DialogResult == DialogResult.OK,
                    "alert: single ok button closes on Enter and Esc");
            }
        }

        private static int TrianglePixels(bool up)
        {
            using (var bmp = new Bitmap(40, 40))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                APC.UI.Drawing.FieldFrame.DrawTriangle(g, new Rectangle(0, 0, 40, 20), Color.Black, up);
                int n = 0;
                for (int y = 0; y < 40; y++) for (int x = 0; x < 40; x++) if (bmp.GetPixel(x, y).R < 128) n++;
                return n;
            }
        }

        private static void RunTabStripChecks()
        {
            using (var tabs = new ApcTabStrip())
            {
                int fired = 0;
                tabs.SelectedIndexChanged += (s, e) => fired++;
                tabs.SetTabs("저속 연마기 ST-GR-01~03", "B", "고속 연마기 ST-GB-01~08 (긴 이름)");
                Check(tabs.SelectedIndex == 0 && fired == 1, "tabstrip: SetTabs selects first tab once");
                tabs.SelectedIndex = 0;
                Check(fired == 1, "tabstrip: same index does not fire");
                tabs.SelectedIndex = 2;
                Check(fired == 2, "tabstrip: index change fires");
                var b0 = tabs.TabBounds(0); var b1 = tabs.TabBounds(1); var b2 = tabs.TabBounds(2);
                Check(b1.Left == b0.Right + ApcTheme.Px(4) && b2.Left == b1.Right + ApcTheme.Px(4), "tabstrip: buttons side by side with 4px gap");
                Check(b1.Width >= ApcTheme.Px(120), "tabstrip: short tab keeps min width");
                int need = TextRenderer.MeasureText(tabs.GetTabText(2), ApcTheme.GetFont(FontStyle.Bold)).Width;
                Check(b2.Width > need, "tabstrip: long tab widens to fit bold text (no cut)");
                Check(tabs.PreferredTabsWidth == b2.Right, "tabstrip: preferred width = buttons + gaps");
                bool threw = false;
                try { tabs.SelectedIndex = 3; } catch (ArgumentOutOfRangeException) { threw = true; }
                Check(threw && tabs.SelectedIndex == 2, "tabstrip: out-of-range index rejected");
            }
        }
    }
}
