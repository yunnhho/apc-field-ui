using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using APC.UI.Tools;

namespace APC.UI.Checks
{
    internal static partial class Program
    {
        private static void RunDialogLayoutChecks()
        {
            ApcTheme.Load(Path.Combine(Root, Path.Combine("themes", "default")));
            Check(ApcLayout.Pad == 16 && ApcLayout.RowGap == 12 && ApcLayout.GroupGap == 16 && ApcLayout.LabelGap == 12
                  && ApcLayout.ItemGap == 8 && ApcLayout.FooterButtonWidth == 110 && ApcLayout.FooterButtonHeight == 40
                  && ApcLayout.FieldHeight == 34, "layout: fixed values");
            CheckFieldDialog();
            CheckFillDialog();
            Check(Throws(() => new FillDialog(0, false)), "dialog: fill without height throws");
            Check(Throws(() => new FillDialog(400, true)), "dialog: second fill throws");
            CheckReviewFixes();
            CheckHelperGuards();
            CheckContentRow();
            CheckCaptionRow();
        }

        private static void CheckCaptionRow()
        {
            using (var d = new CaptionDialog())
            {
                ShowOffscreen(d);
                Check(!d.ShowLogo, "dialog: SetupDialog hides logo");
                var caps = d.Group.Controls.Cast<Control>().OfType<Label>().Where(l => l.Text == "COM" || l.Text == "포트").OrderBy(l => l.Left).ToArray();
                Check(caps.Length == 2 && caps[0].Left == d.Com1.Left && caps[0].Width == d.Com1.Width && caps[1].Left == d.Port1.Left,
                      "dialog: caption row aligns over next row's fields");
                Check(caps.Length == 2 && caps[0].Bottom + ApcTheme.Px(4) == d.Com1.Top && d.Com2.Top - d.Com1.Bottom == ApcTheme.Px(ApcLayout.RowGap),
                      "dialog: caption row above first row, row gap unchanged");
                Check(d.Group.Height - d.Com2.Bottom == d.Group.Padding.Bottom, "dialog: group height includes caption row (" + d.Group.Bottom + "," + d.Com2.Bottom + "," + d.Group.Padding.Bottom + ")");
                Check(d.Common.Left < d.Com1.Left, "dialog: long row name in caption-row group does not widen other groups' label column (" + d.Common.Left + "," + d.Com1.Left + ")");
                var issues = LayoutLint.Inspect(d);
                foreach (var i in issues) Console.WriteLine("      " + i);
                Check(issues.Count == 0, "dialog: caption row dialog lint clean");
            }
        }

        private sealed class CaptionDialog : ApcForm
        {
            public readonly ApcComboBox Com1 = new ApcComboBox { Width = ApcTheme.Px(100) }, Com2 = new ApcComboBox { Width = ApcTheme.Px(100) };
            public readonly ApcTextBox Port1 = new ApcTextBox { Width = ApcTheme.Px(80) };
            public readonly ApcTextBox Common = new ApcTextBox { Width = ApcTheme.Px(80) };
            public readonly ApcGroup Group;

            public CaptionDialog()
            {
                SetupDialog("t", 500);
                Group = AddGroup("g");
                AddCaptionRow(Group, "", "COM", "포트");
                AddFieldRow(Group, "#1  저속 연마기 ST-GR-01~03", Com1, Port1);
                AddFieldRow(Group, "#2", Com2, new ApcTextBox { Width = ApcTheme.Px(80) });
                AddFieldRow(AddGroup("공통"), "응답", Common, new ApcTextBox { Width = ApcTheme.Px(80) });
                SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, new ApcButton { Text = "저장" });
            }
        }

        private static void CheckContentRow()
        {
            using (var d = new RowDialog())
            {
                ShowOffscreen(d);
                int gap = ApcTheme.Px(ApcLayout.ItemGap), pad = ApcTheme.Px(ApcLayout.Pad);
                Check(d.A.Height == d.B.Height && d.A.Height == ApcTheme.Px(120) && d.A.Top == d.B.Top,
                      "dialog: content row cells share the tallest height (" + d.A.Height + "," + d.B.Height + ")");
                Check(d.B.Left - d.A.Right == gap && d.A.Left == d.ContentBounds.Left + pad && d.B.Right == d.ContentBounds.Right - pad,
                      "dialog: content row spans width with ItemGap");
                Check(d.C.Top - d.A.Bottom == ApcTheme.Px(ApcLayout.GroupGap), "dialog: second content row below first");
                var issues = LayoutLint.Inspect(d);
                foreach (var i in issues) Console.WriteLine("      " + i);
                Check(issues.Count == 0, "lint: content row dialog clean");
            }
            Check(Throws<ArgumentException>(() => new RowDialog(true)), "dialog: empty content row throws");
            var g = new ApcGroup();
            Check(g.HeightFor(100) == g.Padding.Vertical + 100 && g.Padding.Top > g.Padding.Bottom, "group: HeightFor adds title band and padding");
        }

        private sealed class RowDialog : ApcForm
        {
            public readonly ApcValueCard A = new ApcValueCard { Title = "a", Height = ApcTheme.Px(120) };
            public readonly ApcValueCard B = new ApcValueCard { Title = "b", Height = ApcTheme.Px(100) };
            public readonly ApcValueCard C = new ApcValueCard { Title = "c", Height = ApcTheme.Px(120) };

            public RowDialog(bool empty = false)
            {
                SetupDialog("t", 500);
                if (empty) { AddContentRow(); return; }
                AddContentRow(A, B);
                AddContentRow(C, new ApcValueCard { Title = "d", Height = ApcTheme.Px(120) });
                SetFooter(null, new ApcButton { Text = "닫기", DialogResult = DialogResult.OK });
            }
        }

        private static void CheckHelperGuards()
        {
            // SetupDialog만 쓰고 직접 배치한 dialog는 높이를 건드리지 않는다
            using (var d = new ManualDialog())
            {
                int before = d.ClientSize.Height;
                ShowOffscreen(d);
                Check(d.ClientSize.Height == before, "dialog: SetupDialog alone keeps manual height (" + before + " → " + d.ClientSize.Height + ")");
            }
            // 표시 뒤 헬퍼 호출은 배치에 반영되지 않으므로 막는다
            using (var d = new ManualDialog())
            {
                ShowOffscreen(d);
                Check(Throws<InvalidOperationException>(() => d.AddRowLate()), "dialog: helper after show throws");
            }
            Check(Throws<ArgumentNullException>(() => new GuardDialog(1)), "dialog: AddFieldRow with null group throws ArgumentNullException");
            Check(Throws<InvalidOperationException>(() => new GuardDialog(2)), "dialog: SetupDialog height 0 after fill throws");
        }

        private sealed class ManualDialog : ApcForm
        {
            public ManualDialog()
            {
                SetupDialog("t", 400, 300);
                SetupDialog("t", 400);   // 높이 자동이지만 헬퍼 배치 없음 → 300 유지
                Controls.Add(new ApcTextBox { Bounds = new Rectangle(20, 60, 200, 34) });
            }

            public void AddRowLate() { AddFieldRow("늦음", new ApcTextBox()); }
        }

        private sealed class GuardDialog : ApcForm
        {
            public GuardDialog(int mode)
            {
                SetupDialog("t", 400, 300);
                if (mode == 1) AddFieldRow((ApcGroup)null, "가", new ApcTextBox());
                if (mode != 2) return;
                AddContentFill(new ApcTrendChart());
                SetupDialog("t", 400);
            }
        }

        private static void CheckReviewFixes()
        {
            // 확정 버튼 하나(DialogResult 없음)는 Esc용 CancelButton이 되면 안 된다 — 누르면 Click 처리 전에 Cancel로 닫힌다
            using (var d = new SingleButtonDialog())
                Check(d.Save.DialogResult == DialogResult.None && d.CancelButton == null, "dialog: SetFooter(null, confirm) keeps confirm's DialogResult");
            // 꺼진 확정 버튼의 이유: 하단 줄 [확정] 바로 왼쪽, 같은 높이, 점검 0건
            using (var d = new FooterNoteDialog())
            {
                ShowOffscreen(d);
                var issues = LayoutLint.Inspect(d);
                foreach (var i in issues) Console.WriteLine("      " + i);
                Check(d.Note.Top == d.Save.Top && d.Note.Height == d.Save.Height && d.Note.Right < d.Save.Left && d.Note.Width > d.Save.Width, "dialog footer note: left of confirm on the button row");
                Check(issues.Count == 0, "dialog footer note: no layout issues");
            }
            // 행 a, b → 묶음 → 행 c: 묶음을 사이에 둔 간격은 행 간격 비교에서 뺀다
            using (var d = new MixedDialog())
            {
                ShowOffscreen(d);
                var issues = LayoutLint.Inspect(d);
                foreach (var i in issues) Console.WriteLine("      " + i);
                Check(issues.Count == 0, "lint: rows split by a group are not uneven");
            }
        }

        private sealed class SingleButtonDialog : ApcForm
        {
            public readonly ApcButton Save = new ApcButton { Text = "저장", Variant = ApcButtonVariant.Primary };

            public SingleButtonDialog()
            {
                SetupDialog("t", 400);
                AddFieldRow("이름", new ApcTextBox());
                SetFooter(null, Save);
            }
        }

        private sealed class FooterNoteDialog : ApcForm
        {
            public readonly ApcButton Save = new ApcButton { Text = "Release", Variant = ApcButtonVariant.Primary, Enabled = false };
            public readonly Label Note;

            public FooterNoteDialog()
            {
                SetupDialog("t", 520);
                AddFieldRow("Lot", new ApcTextBox());
                SetFooter(new ApcButton { Text = "취소" }, Save);
                Note = SetFooterNote("원인 확인 전 · Release 불가");
            }
        }

        private sealed class MixedDialog : ApcForm
        {
            public MixedDialog()
            {
                SetupDialog("t", 460);
                AddFieldRow("가", new ApcTextBox());
                AddFieldRow("나", new ApcTextBox());
                AddFieldRow(AddGroup("묶음"), "다", new ApcTextBox());
                AddFieldRow("라", new ApcTextBox());
                SetFooter(new ApcButton { Text = "취소" }, new ApcButton { Text = "저장", Variant = ApcButtonVariant.Primary });
            }
        }

        private static void CheckFieldDialog()
        {
            using (var d = new FieldDialog())
            {
                ShowOffscreen(d);
                int px(int v) => ApcTheme.Px(v);
                int left0 = d.PointToClient(d.Name0.Parent.PointToScreen(d.Name0.Location)).X;
                int left1 = d.PointToClient(d.Speed.Parent.PointToScreen(d.Speed.Location)).X;
                int left2 = d.PointToClient(d.InGroup.Parent.PointToScreen(d.InGroup.Location)).X;
                Check(left0 == left1 && left1 == left2, "dialog: field left edges equal (" + left0 + "," + left1 + "," + left2 + ")");
                Check(d.Speed.Top - d.Name0.Bottom == px(ApcLayout.RowGap), "dialog: row gap is RowGap");
                var cb = d.ContentBounds;
                Check(d.Save.Right + px(ApcLayout.ItemGap) == d.Cancel.Left && d.Cancel.Right == cb.Right - px(ApcLayout.Pad)
                      && d.Save.Size == new Size(px(110), px(40)) && d.Cancel.Size == d.Save.Size && d.CancelButton == d.Cancel,
                      "dialog: footer order and size");
                Check(d.Save.Bottom == cb.Bottom - px(ApcLayout.Pad) && d.Save.Top - d.Group.Bottom == px(ApcLayout.GroupGap),
                      "dialog: auto height");
                Check(d.TitleText == "t" && !d.Resizable && !d.MinimizeBox && !d.ShowInTaskbar, "dialog: SetupDialog sets dialog properties");
            }
        }

        private static void CheckFillDialog()
        {
            using (var d = new FillDialog(400, false))
            {
                ShowOffscreen(d);
                Check(d.Head.Dock == DockStyle.None && d.Head.Left == d.ContentBounds.Left + ApcTheme.Px(ApcLayout.Pad), "dialog: docked content is placed by helper");
                Check(d.Chart.Bottom + ApcTheme.Px(ApcLayout.GroupGap) == d.CloseButton.Top && d.Chart.Height > 0 && d.CancelButton == d.CloseButton,
                      "dialog: fill content");
            }
        }

        private static void ShowOffscreen(Form f)
        {
            f.StartPosition = FormStartPosition.Manual;
            f.Location = new Point(-32000, -32000);
            f.Show();
            Application.DoEvents();
        }

        private static bool Throws(Action a) { return Throws<InvalidOperationException>(a); }

        private static bool Throws<T>(Action a) where T : Exception
        {
            try
            {
                a();
                return false;
            }
            catch (T) { return true; }
            catch (Exception) { return false; }
        }

        private sealed class FieldDialog : ApcForm
        {
            public readonly ApcTextBox Name0 = new ApcTextBox();
            public readonly ApcNumericBox Speed = new ApcNumericBox();
            public readonly ApcTextBox InGroup = new ApcTextBox();
            public readonly ApcButton Cancel = new ApcButton { Text = "취소" };
            public readonly ApcButton Save = new ApcButton { Text = "저장", Variant = ApcButtonVariant.Primary };
            public readonly ApcGroup Group;

            public FieldDialog()
            {
                SetupDialog("t", 460);
                AddFieldRow("모델명", Name0);
                AddFieldRow("압입 속도 상한 (mm/s)", Speed);
                Group = AddGroup("묶음");
                AddFieldRow(Group, "비고", InGroup);
                SetFooter(Cancel, Save);
            }
        }

        private sealed class FillDialog : ApcForm
        {
            public readonly ApcTrendChart Chart = new ApcTrendChart();
            public readonly ApcInfoStrip Head = new ApcInfoStrip { Dock = DockStyle.Top };
            public readonly ApcButton CloseButton = new ApcButton { Text = "닫기", DialogResult = DialogResult.OK };

            public FillDialog(int height, bool twoFills)
            {
                SetupDialog("t", 600, height);
                AddContent(Head);
                AddContent(new ApcGrid { Height = ApcTheme.Px(100) });
                AddContentFill(Chart);
                if (twoFills) AddContentFill(new ApcTrendChart());
                SetFooter(null, CloseButton);
            }
        }
    }
}
