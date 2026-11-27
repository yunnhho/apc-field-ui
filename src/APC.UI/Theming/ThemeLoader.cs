using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace APC.UI.Theming
{
    public static class ThemeLoader
    {
        public const string FileName = "theme.json";
        private const float GraySaturationMax = 0.15f;

        public static ThemeData Load(string themeDir)
        {
            var path = Path.Combine(themeDir ?? string.Empty, FileName);
            ThemeData data;
            try
            {
                var bytes = File.ReadAllBytes(path);
                // 메모장·PowerShell은 UTF-8 BOM을 붙인다. 직렬화기는 BOM을 문법 오류로 본다.
                int skip = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
                using (var stream = new MemoryStream(bytes, skip, bytes.Length - skip))
                {
                    data = (ThemeData)new DataContractJsonSerializer(typeof(ThemeData)).ReadObject(stream);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SerializationException || ex is ArgumentException)
            {
                throw new ThemeException(path + ": 읽을 수 없음 — " + ex.Message, ex);
            }

            Validate(data, path);
            return data;
        }

        public static Color ParseColor(string hex)
        {
            if (hex == null || hex.Length != 7 || hex[0] != '#')
                throw new FormatException("색 형식은 #RRGGBB: " + hex);
            int rgb = int.Parse(hex.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }

        public static Color GetStatusColor(ThemeData t, StatusKind k)
        {
            switch (k)
            {
                case StatusKind.Run: return ParseColor(t.Status.Run);
                case StatusKind.Warn: return ParseColor(t.Status.Warn);
                case StatusKind.Alarm: return ParseColor(t.Status.Alarm);
                case StatusKind.Stop: return ParseColor(t.Status.Stop);
                default: return ParseColor(t.Status.Info);
            }
        }

        private static void Validate(ThemeData t, string path)
        {
            Require(t, path, "(root)");
            Require(t.FontFamily, path, "fontFamily");
            if (t.FontSize <= 0) Fail(path, "fontSize", "0보다 커야 함");
            Require(t.Colors, path, "colors");
            Require(t.Status, path, "status");
            Require(t.Scan, path, "scan");
            Require(t.Logo, path, "logo");

            ApplyDefaults(t.Colors);
            Pair(t.Colors.Background, path, "colors.background");
            Pair(t.Colors.Header, path, "colors.header");
            Pair(t.Colors.TitleBar, path, "colors.titleBar");
            Pair(t.Colors.GridHeader, path, "colors.gridHeader");
            ColorField(t.Colors.Border, path, "colors.border");
            ColorField(t.Colors.Text, path, "colors.text");
            ColorField(t.Colors.TextMuted, path, "colors.textMuted");
            ColorField(t.Colors.TextInverse, path, "colors.textInverse");
            ColorField(t.Colors.GridLine, path, "colors.gridLine");
            ColorField(t.Colors.GridSelected, path, "colors.gridSelected");
            Pair(t.Colors.Panel, path, "colors.panel");
            ColorField(t.Colors.BorderStrong, path, "colors.borderStrong");
            ColorField(t.Colors.Brand, path, "colors.brand");

            if (t.ButtonSlice == null || t.ButtonSlice.Length != 4) Fail(path, "buttonSlice", "[좌,위,우,아래] 4개 값 필요");
            if (t.Scan.MaxKeyIntervalMs <= 0) Fail(path, "scan.maxKeyIntervalMs", "0보다 커야 함");
            if (t.Scan.MinLength <= 0) Fail(path, "scan.minLength", "0보다 커야 함");

            HueIn(t.Status.Run, path, "status.run", 90, 160);
            HueIn(t.Status.Warn, path, "status.warn", 40, 65);
            HueIn(t.Status.Alarm, path, "status.alarm", 345, 15);
            HueIn(t.Status.Info, path, "status.info", 200, 245);
            var stop = ColorField(t.Status.Stop, path, "status.stop");
            if (stop.GetSaturation() > GraySaturationMax) Fail(path, "status.stop", "회색이어야 함(채도 ≤ 0.15)");
        }

        /// <summary>Status colors may change lightness only. from &gt; to means a range crossing 0°.</summary>
        private static void HueIn(string hex, string path, string field, float from, float to)
        {
            var c = ColorField(hex, path, field);
            float h = c.GetHue();
            bool ok = from <= to ? (h >= from && h <= to) : (h >= from || h <= to);
            if (!ok || c.GetSaturation() <= GraySaturationMax)
                Fail(path, field, "상태색의 색상은 바꿀 수 없음(명도만 조정 가능). 허용 hue " + from + "~" + to + "°, 현재 " + h.ToString("0") + "°");
        }

        /// <summary>theme.json before 1.20 has no panel, borderStrong or brand. Fill them so it looks as before (white cards, existing border, no red).</summary>
        private static void ApplyDefaults(ThemeColors c)
        {
            if (c.Panel == null) c.Panel = new[] { "#FFFFFF", "#FFFFFF" };
            if (c.BorderStrong == null) c.BorderStrong = c.Border;
            if (c.Brand == null) c.Brand = c.Text;
        }

        private static Color ColorField(string hex, string path, string field)
        {
            Require(hex, path, field);
            try { return ParseColor(hex); }
            catch (FormatException ex) { Fail(path, field, ex.Message); throw; }
        }

        private static void Pair(string[] pair, string path, string field)
        {
            if (pair == null || pair.Length != 2) Fail(path, field, "[시작, 끝] 2개 색 필요");
            ColorField(pair[0], path, field + "[0]");
            ColorField(pair[1], path, field + "[1]");
        }

        private static void Require(object value, string path, string field)
        {
            if (value == null) Fail(path, field, "필수 항목 누락");
        }

        private static void Fail(string path, string field, string reason)
        {
            throw new ThemeException(path + ": " + field + " — " + reason);
        }
    }
}
