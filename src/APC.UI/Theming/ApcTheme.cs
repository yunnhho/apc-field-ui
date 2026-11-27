using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace APC.UI.Theming
{
    /// <summary>The single current theme. Load once at app start; controls then take colors, fonts and images from here.</summary>
    public static class ApcTheme
    {
        public const int BaseMinHeight = 28;

        private static readonly Dictionary<string, Image> Images = new Dictionary<string, Image>();
        private static readonly Dictionary<FontStyle, Font> Fonts = new Dictionary<FontStyle, Font>();

        public static ThemeData Current { get; private set; }
        public static string ThemeDir { get; private set; }
        public static float Dpi { get; private set; } = AssetResolver.BaseDpi;

        public static event EventHandler Changed;

        public static float Scale { get { return Dpi / AssetResolver.BaseDpi; } }
        public static int MinHeight { get { return (int)Math.Round(BaseMinHeight * Scale); } }
        public static string AssetsDir { get { return Path.Combine(ThemeDir, "assets"); } }

        /// <summary>Converts 96-dpi px to the current scale. Use for sizes and margins anywhere in screens and dialogs (same as ApcTemplateView's S()).</summary>
        public static int Px(int px) { return (int)Math.Round(px * Scale); }

        /// <summary>Required images. Load checks that the @1x files exist so failure happens at startup, not on the first OnPaint.</summary>
        internal static readonly string[] RequiredImages = BuildRequiredImages();

        private static string[] BuildRequiredImages()
        {
            var states = new[] { "normal", "hover", "pressed", "disabled" };
            var list = new List<string> { "lamp_run", "lamp_warn", "lamp_alarm", "lamp_stop", "lamp_info" };
            foreach (var v in new[] { "btn_normal", "btn_primary", "btn_danger", "win_close", "win_minimize" })
                foreach (var s in states) list.Add(v + "_" + s);
            return list.ToArray();
        }

        /// <exception cref="ThemeException">theme.json problem or a missing required image. On failure the previous theme is kept.</exception>
        public static void Load(string themeDir)
        {
            var data = ThemeLoader.Load(themeDir);
            var assets = Path.Combine(themeDir, "assets");
            var names = new List<string>(RequiredImages) { data.Logo };
            foreach (var name in names)
            {
                try { AssetResolver.Resolve(assets, name, AssetResolver.BaseDpi); }
                catch (FileNotFoundException ex) { throw new ThemeException(ex.Message, ex); }
            }

            ClearCaches();
            Current = data;
            ThemeDir = themeDir;
            using (var g = Graphics.FromHwnd(IntPtr.Zero)) Dpi = g.DpiX;
            var handler = Changed;
            if (handler != null) handler(null, EventArgs.Empty);
        }

        /// <summary>Cached image. Callers must not Dispose it.</summary>
        public static Image GetImage(string baseName)
        {
            EnsureLoaded();
            Image img;
            if (Images.TryGetValue(baseName, out img)) return img;

            var path = AssetResolver.Resolve(AssetsDir, baseName, Dpi);
            // 메모리로 복사해서 열어야 파일이 잠기지 않는다(테마 교체·재렌더 가능).
            img = new Bitmap(new MemoryStream(File.ReadAllBytes(path)));
            img.Tag = path.EndsWith("@2x.png") ? 2f : path.EndsWith("@1.5x.png") ? 1.5f : 1f;
            Images[baseName] = img;
            return img;
        }

        /// <summary>Scale of the image file (for converting 9-slice margins).</summary>
        public static float ImageScale(Image img)
        {
            return img.Tag is float ? (float)img.Tag : 1f;
        }

        public static Font GetFont(FontStyle style = FontStyle.Regular)
        {
            EnsureLoaded();
            Font f;
            if (!Fonts.TryGetValue(style, out f))
            {
                f = new Font(Current.FontFamily, Current.FontSize, style);
                Fonts[style] = f;
            }
            return f;
        }

        public static Color Status(StatusKind kind)
        {
            EnsureLoaded();
            return ThemeLoader.GetStatusColor(Current, kind);
        }

        /// <summary>
        /// Status color for text on light backgrounds (white, panel). Raw status yellow (warning) and green have low contrast on white and are unreadable from a distance →
        /// only lightness is lowered to reach 4.5:1 contrast or more. Hue is unchanged. Lamp and banner fill colors use Status.
        /// </summary>
        public static Color StatusText(StatusKind kind)
        {
            var c = Status(kind);
            for (int i = 0; i < 20 && Contrast(c, Color.White) < MinTextContrast; i++)
                c = Color.FromArgb((int)(c.R * 0.92), (int)(c.G * 0.92), (int)(c.B * 0.92));
            return c;
        }

        private const double MinTextContrast = 4.5;

        internal static double Contrast(Color a, Color b)
        {
            double la = Luminance(a), lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        private static double Luminance(Color c)
        {
            return 0.2126 * Lin(c.R) + 0.7152 * Lin(c.G) + 0.0722 * Lin(c.B);
        }

        private static double Lin(int v)
        {
            double s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        public static Color C(string hex)
        {
            return ThemeLoader.ParseColor(hex);
        }

        private static void EnsureLoaded()
        {
            if (Current == null) throw new InvalidOperationException("ApcTheme.Load(themeDir)를 먼저 호출해야 합니다.");
        }

        private static void ClearCaches()
        {
            foreach (var img in Images.Values) img.Dispose();
            Images.Clear();
            // ponytail: 옛 Font는 Dispose하지 않고 GC에 맡긴다. Control.Font setter는 Equals가 같으면 교체하지 않아
            // 컨트롤이 옛 객체를 계속 쥐고 있을 수 있다. 테마 재로드는 드물어서 누수 비용은 무시할 만하다.
            Fonts.Clear();
        }
    }
}
