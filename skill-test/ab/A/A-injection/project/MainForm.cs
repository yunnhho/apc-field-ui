using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace InjectionMonitor
{
    internal sealed class MainForm : ApcForm
    {
        public MainForm()
        {
            float s = ApcTheme.Scale;
            TitleText = "사출 성형 라인 모니터링";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size((int)(1600 * s), (int)(900 * s));
            MinimumSize = new Size((int)(1024 * s), (int)(640 * s));
            Controls.Add(new Views.MainView());
        }
    }
}
