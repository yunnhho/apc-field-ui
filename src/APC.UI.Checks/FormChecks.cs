using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;

namespace APC.UI.Checks
{
    internal static partial class Program
    {
        private static void RunFormChecks()
        {
            using (var f = new ApcForm())
            {
                Check(f.FormBorderStyle == FormBorderStyle.None, "form is borderless");
                Check(f.ContentBounds.Top >= f.TitleBarHeight, "content starts below title bar");
                var area = Screen.FromControl(f).WorkingArea;
                Check(f.MaximizedArea.Height <= area.Height && f.MaximizedArea.Width <= area.Width, "maximize stays inside working area");
            }

            using (var f = new ApcForm())
            {
                int one = f.TitleBarHeight;
                f.FunctionText = "작업 시작 · LOT 스캔";
                Check(f.TitleBarHeight > one && f.Padding.Top == f.TitleBarHeight + 1, "form: function text grows title bar");
                f.FunctionText = null;
                Check(f.FunctionText == "" && f.TitleBarHeight == one && f.Padding.Top == one + 1, "form: clearing function text restores height");
                f.ShowLogo = false;
                f.FunctionText = "x";
                Check(f.TitleBarHeight == one, "form: dialog ignores function text");
            }
            CheckTwoLineTitleFitsLargeFont();
            CheckSettingsButtonDrawnInShownForm();

            using (var dlg = ApcDialog.Create("삭제할까요?"))
            {
                Check(dlg.CancelButton != null && dlg.AcceptButton == dlg.CancelButton, "confirm: Enter and Esc both cancel");
                ApcButton first = null;
                foreach (Control c in dlg.Controls)
                {
                    var b = c as ApcButton;
                    if (b != null && (first == null || b.TabIndex < first.TabIndex)) first = b;
                }
                Check(first != null && first.DialogResult == DialogResult.Cancel, "confirm: cancel is first in tab order (initial focus)");
                Check(first != null && first.Variant == ApcButtonVariant.Normal, "confirm: cancel is not the danger button");
            }

            using (var v = new ProbeView())
            {
                Check(v.H.Dock == DockStyle.Top && v.A.Dock == DockStyle.Bottom && v.B.Dock == DockStyle.Fill, "template view: header top, alarm bottom, body fill");
                Check(((ApcHeaderBar)v.H).Title == "probe", "template view: title goes to header");
            }
            Check(typeof(ApcTemplateView).IsPublic, "template view is public in APC.UI");

            // 설정 버튼: 최소화 왼쪽, 누르면 SettingsClick. 화면이 UseSettingsButton으로 켜고 Dispose하면 끈다.
            using (var f = new ClickForm { ClientSize = new Size(800, 400) })
            {
                Check(!f.ShowSettings, "form: settings button off by default");
                int clicks = 0;
                var view = new SettingsView(() => clicks++);
                f.Controls.Add(view);
                var handle = view.Handle;   // 숨은 폼에서는 자식 핸들이 저절로 안 생긴다(실제 창은 Show 때 생김)
                Check(f.ShowSettings, "template view: UseSettingsButton turns on host form settings button");
                var r = f.SettingsButtonBounds;
                Check(r.Right <= f.MinimizeButtonBounds.Left && r.Top == f.MinimizeButtonBounds.Top, "form: settings button sits left of minimize");
                f.ClickAt(new Point(r.X + r.Width / 2, r.Y + r.Height / 2));
                Check(clicks == 1, "form: clicking settings button raises SettingsClick");
                view.Dispose();
                Check(!f.ShowSettings, "template view: dispose turns settings button off");
                f.ClickAt(new Point(r.X + r.Width / 2, r.Y + r.Height / 2));
                Check(clicks == 1, "form: hidden settings button does nothing");
            }
        }

        private sealed class ClickForm : ApcForm
        {
            public void ClickAt(Point p)
            {
                var e = new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0);
                OnMouseDown(e); OnMouseUp(e);
            }
        }

        private sealed class SettingsView : ApcTemplateView
        {
            public SettingsView(Action onClick) : base("settings") { UseSettingsButton((s, e) => onClick()); }
        }

        private sealed class ProbeView : ApcTemplateView
        {
            public ProbeView() : base("probe") { }
            public Control H { get { return Header; } }
            public Control A { get { return Alarm; } }
            public Control B { get { return Body; } }
        }
        /// <summary>갤러리처럼 창을 띄운 뒤 화면을 끼워도 타이틀바에 톱니가 그려진다(두 줄 제목 포함).</summary>
        private static void CheckSettingsButtonDrawnInShownForm()
        {
            using (var f = new ApcForm { ClientSize = new Size(1200, 500), FunctionText = "컨트롤 · 화면 템플릿" })
            {
                var host = new Panel { Dock = DockStyle.Fill };
                f.Controls.Add(host);
                ShowOffscreen(f);
                host.Controls.Add(new SettingsView(() => { }));
                Application.DoEvents();
                using (var bmp = new Bitmap(f.ClientSize.Width, f.ClientSize.Height))
                {
                    f.DrawToBitmap(bmp, new Rectangle(Point.Empty, f.ClientSize));
                    var r = f.SettingsButtonBounds;
                    var bg = bmp.GetPixel(r.Left - 3, r.Top + r.Height / 2);
                    bool drawn = false;
                    for (int x = r.Left; x < r.Right && !drawn; x++)
                        drawn = bmp.GetPixel(x, r.Top + r.Height / 2).ToArgb() != bg.ToArgb();
                    Check(f.ShowSettings && drawn, "form: settings button drawn after view added to shown form");
                Check(f.CloseButtonBounds.Right <= f.ClientSize.Width - APC.UI.Theming.ApcTheme.Px(12), "form: window buttons keep 12px from right edge");
                }
                f.Close();
            }
        }

        /// <summary>고객사 테마 글꼴이 커도(16pt) 두 줄 제목이 타이틀바 안에 들어간다.</summary>
        private static void CheckTwoLineTitleFitsLargeFont()
        {
            string original = APC.UI.Theming.ApcTheme.ThemeDir;
            string tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "apcui-bigfont-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(tmp, "assets"));
                foreach (var f in System.IO.Directory.GetFiles(System.IO.Path.Combine(original, "assets")))
                    System.IO.File.Copy(f, System.IO.Path.Combine(tmp, "assets", System.IO.Path.GetFileName(f)));
                string json = System.IO.File.ReadAllText(System.IO.Path.Combine(original, "theme.json"));
                json = System.Text.RegularExpressions.Regex.Replace(json, @"""fontSize"":\s*\d+", @"""fontSize"": 16");
                System.IO.File.WriteAllText(System.IO.Path.Combine(tmp, "theme.json"), json);
                APC.UI.Theming.ApcTheme.Load(tmp);
                using (var f = new ApcForm { FunctionText = "작업 시작 · LOT 스캔" })
                {
                    var bold = APC.UI.Theming.ApcTheme.GetFont(FontStyle.Bold);
                    using (var big = new Font(bold.FontFamily, bold.Size * 1.5f, FontStyle.Bold))
                    using (var small = new Font(bold.FontFamily, bold.Size - 1f, FontStyle.Bold))
                    {
                        int need = TextRenderer.MeasureText("가", big).Height + TextRenderer.MeasureText("가", small).Height;
                        Check(f.TitleBarHeight >= need, "form: two-line title fits a 16pt theme font");
                    }
                }
            }
            finally
            {
                APC.UI.Theming.ApcTheme.Load(original);
                try { System.IO.Directory.Delete(tmp, true); } catch (System.IO.IOException) { }
            }
        }
    }
}
