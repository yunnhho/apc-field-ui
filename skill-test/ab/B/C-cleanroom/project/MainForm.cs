using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace CleanroomMonitor
{
    internal sealed class MainForm : ApcForm
    {
        public MainForm()
        {
            float s = ApcTheme.Scale;
            TitleText = "클린룸 환경 모니터링";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size((int)(1600 * s), (int)(900 * s));
            MinimumSize = new Size((int)(1024 * s), (int)(640 * s));
            Controls.Add(new Views.MainView());
        }
    }
}
