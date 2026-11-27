using System.IO;

namespace APC.UI.Theming
{
    /// <summary>Image name → actual file path. Rule: {baseName}@{1x|1.5x|2x}.png</summary>
    public static class AssetResolver
    {
        public const float BaseDpi = 96f;
        public const string FallbackScale = "1x";

        /// <summary>Boundary values (125%, 175%) pick the larger file — downscaling blurs less than upscaling.</summary>
        public static string PickScale(float dpi)
        {
            float ratio = dpi / BaseDpi;
            if (ratio < 1.25f) return "1x";
            if (ratio < 1.75f) return "1.5x";
            return "2x";
        }

        public static string Resolve(string assetsDir, string baseName, float dpi)
        {
            var preferred = Path.Combine(assetsDir, baseName + "@" + PickScale(dpi) + ".png");
            if (File.Exists(preferred)) return preferred;

            var fallback = Path.Combine(assetsDir, baseName + "@" + FallbackScale + ".png");
            if (File.Exists(fallback)) return fallback;

            throw new FileNotFoundException("이미지 파일 없음: " + fallback, fallback);
        }
    }
}
