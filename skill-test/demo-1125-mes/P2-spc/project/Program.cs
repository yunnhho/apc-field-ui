using System;
using System.IO;
using System.Windows.Forms;
using APC.UI.Theming;

namespace ShaftSpcMonitor
{
    internal static class Program
    {
        private const string Customer = "default";

        [STAThread]
        private static int Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string themeDir = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "themes"), Customer);
            try
            {
                ApcTheme.Load(themeDir);
            }
            catch (ThemeException ex)
            {
                MessageBox.Show(ex.Message, "테마 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }

            Application.Run(new MainForm());
            return 0;
        }
    }
}
