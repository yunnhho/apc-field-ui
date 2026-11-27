using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;

namespace APC.UI.Checks
{
    /// <summary>render z-order check: a blue Label brought to front over a red one.</summary>
    public class ZOrderFixture : ApcGroup
    {
        public static readonly Point Overlap = new Point(200, 145);

        public ZOrderFixture()
        {
            var back = new Label { BackColor = Color.Red, Bounds = new Rectangle(100, 100, 200, 100) };
            var front = new Label { BackColor = Color.Blue, Bounds = new Rectangle(150, 120, 100, 50) };
            Controls.Add(back);
            Controls.Add(front);
            front.BringToFront();
        }
    }
}
