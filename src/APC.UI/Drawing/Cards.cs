using System.Drawing;
using APC.UI.Theming;

namespace APC.UI.Drawing
{
    /// <summary>Shared rules for card-style display controls (value cards, equipment tiles).</summary>
    internal static class Cards
    {
        /// <summary>Value/status text color: status color only for warning/alarm, dimmed for stop, default text color otherwise. Painting normal values green hides the abnormal ones.</summary>
        internal static Color EmphasisColor(StatusKind status)
        {
            var c = ApcTheme.Current.Colors;
            if (status == StatusKind.Warn || status == StatusKind.Alarm) return ApcTheme.StatusText(status);
            return ApcTheme.C(status == StatusKind.Stop ? c.TextMuted : c.Text);
        }

        /// <summary>Panel gradient + border. Thick alarm-color border on alarm (so the card is identifiable from a distance).</summary>
        internal static void DrawFrame(Graphics g, Size size, StatusKind status, bool highlight, bool selected = false)
        {
            var c = ApcTheme.Current.Colors;
            if (selected) g.Clear(ApcTheme.C(c.GridSelected));   // 선택 = 표 선택 행과 같은 바탕
            else
            {
                Gradients.Fill(g, new Rectangle(Point.Empty, size), c.Panel);
                using (var hi = new Pen(Color.White)) g.DrawLine(hi, 1, 1, size.Width - 2, 1);
            }
            using (var pen = new Pen(ApcTheme.C(c.Border))) g.DrawRectangle(pen, 0, 0, size.Width - 1, size.Height - 1);
            if (status == StatusKind.Alarm)
            {
                int w = (int)System.Math.Round(3 * ApcTheme.Scale);
                using (var pen = new Pen(ApcTheme.Status(StatusKind.Alarm), w))
                    g.DrawRectangle(pen, w / 2, w / 2, size.Width - w, size.Height - w);
            }
            else if (highlight)
            {
                using (var pen = new Pen(FieldFrame.FocusColor))
                {
                    g.DrawRectangle(pen, 0, 0, size.Width - 1, size.Height - 1);
                    g.DrawRectangle(pen, 1, 1, size.Width - 3, size.Height - 3);
                }
            }
        }

        internal static Image Lamp(StatusKind status)
        {
            return ApcTheme.GetImage("lamp_" + status.ToString().ToLowerInvariant());
        }
    }
}
