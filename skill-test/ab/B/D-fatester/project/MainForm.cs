using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace FATester
{
    internal sealed class MainForm : ApcForm
    {
        public MainForm()
        {
            float s = ApcTheme.Scale;
            TitleText = "FA 시험기 채널 모니터링";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size((int)(1600 * s), (int)(900 * s));
            MinimumSize = new Size((int)(1024 * s), (int)(640 * s));
            Controls.Add(new Views.MainView());
        }
    }
}
