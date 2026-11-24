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
    }
}
