using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Checks
{
    internal static partial class Program
    {
        private static void RunLampChecks()
        {
            var lamp = new ApcLamp { Status = StatusKind.Warn };
            Check(lamp.ImageName == "lamp_warn", "lamp image name follows status");
        }
    }
}
