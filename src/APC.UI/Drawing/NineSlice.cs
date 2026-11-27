using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using APC.UI.Theming;

namespace APC.UI.Drawing
{
    /// <summary>Corners drawn as is; only edges and center stretch. Rounded corners and gloss stay the same at any size.</summary>
    public static class NineSlice
    {
        /// <param name="slice">Margins at @1x (px)</param>
        /// <param name="imageScale">Scale of the img file (ApcTheme.ImageScale)</param>
        public static void Draw(Graphics g, Image img, Rectangle dest, Padding slice, float imageScale)
        {
            if (dest.Width <= 0 || dest.Height <= 0) return;

            // 원본 이미지 좌표의 여백
            int sl = (int)Math.Round(slice.Left * imageScale), st = (int)Math.Round(slice.Top * imageScale);
            int sr = (int)Math.Round(slice.Right * imageScale), sb = (int)Math.Round(slice.Bottom * imageScale);
            // 화면 좌표의 여백 — 대상이 여백보다 작으면 반씩 나눠 갖는다.
            float ds = ApcTheme.Scale;
            int dl = Clamp(slice.Left * ds, dest.Width / 2), dr = Clamp(slice.Right * ds, dest.Width - dl);
            int dt = Clamp(slice.Top * ds, dest.Height / 2), db = Clamp(slice.Bottom * ds, dest.Height - dt);

            int[] sx = { 0, sl, img.Width - sr, img.Width };
            int[] sy = { 0, st, img.Height - sb, img.Height };
            int[] dx = { dest.Left, dest.Left + dl, dest.Right - dr, dest.Right };
            int[] dy = { dest.Top, dest.Top + dt, dest.Bottom - db, dest.Bottom };

            var oldMode = g.InterpolationMode;
            var oldOffset = g.PixelOffsetMode;
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            using (var attrs = new ImageAttributes())
            {
                attrs.SetWrapMode(WrapMode.TileFlipXY);   // 조각 경계의 번짐 방지
                for (int row = 0; row < 3; row++)
                    for (int col = 0; col < 3; col++)
                    {
                        var d = Rectangle.FromLTRB(dx[col], dy[row], dx[col + 1], dy[row + 1]);
                        if (d.Width <= 0 || d.Height <= 0) continue;
                        g.DrawImage(img, d, sx[col], sy[row], sx[col + 1] - sx[col], sy[row + 1] - sy[row], GraphicsUnit.Pixel, attrs);
                    }
            }
            g.InterpolationMode = oldMode;
            g.PixelOffsetMode = oldOffset;
        }

        private static int Clamp(float value, int max)
        {
            return Math.Max(0, Math.Min((int)Math.Round(value), max));
        }
    }
}
