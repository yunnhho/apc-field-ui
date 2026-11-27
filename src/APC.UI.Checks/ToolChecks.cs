using System;
using System.Drawing;
using System.IO;
using APC.UI.Tools;

namespace APC.UI.Checks
{
    internal static partial class Program
    {
        private static void ThrowForWhere() { throw new IndexOutOfRangeException("t"); }

        private static void RunToolChecks()
        {
            try { ThrowForWhere(); }
            catch (Exception ex)
            {
                string w = Renderer.Where(ex, typeof(Program).Assembly);
                Check(w.Contains("ThrowForWhere") && w.Contains("ToolChecks.cs:"), "render: construction failure names app code location (" + w + ")");
            }
            string dll = Path.Combine(Root, @"src\APC.UI\bin\Debug\APC.UI.dll");
            var md = ApiWriter.Write(dll);
            Check(md.StartsWith("# APC.UI API (v" + typeof(APC.UI.Controls.ApcButton).Assembly.GetName().Version + ")"), "api: header carries version");
            Check(md.Contains("## ApcTemplateView") && md.Contains("## ApcButton"), "api: lists public controls");
            Check(md.Contains("## ApcJudgePanel") && md.Contains("string Text { get; set; }") && md.Contains("static int Px(int px)"), "api: documents Text overrides and ApcTheme.Px");
            Check(md.Contains("Load(string themeDir)"), "api: lists ApcTheme.Load signature");
            Check(md.Contains("Common frame of every function screen"), "api: includes xml summary text");
            Check(md == ApiWriter.Write(dll), "api: deterministic");
            Check(md.Contains("\nContents: ") &&md.Contains(" · ApcTemplateView"), "api: table of contents lists types");
            Check(md.Contains("Clicking raises SettingsClick"), "api: see cref rendered as member name");

            string demo = Path.Combine(Root, @"src\APC.UI.Demo\bin\Debug\APC.UI.Demo.exe");
            string theme = Path.Combine(Root, @"themes\default");
            using (var bmp = Renderer.Render(demo, "ProductionView", theme, new Size(1920, 1080)))
            {
                Check(bmp.Width == 1920 && bmp.Height == 1080, "render: requested size");
                Check(bmp.GetPixel(10, 10) != bmp.GetPixel(960, 540), "render: not a blank image");
            }
            using (var bmp = Renderer.Render(typeof(Program).Assembly.Location, "ZOrderFixture", theme, new Size(400, 300)))
                Check(bmp.GetPixel(ZOrderFixture.Overlap.X, ZOrderFixture.Overlap.Y).ToArgb() == Color.Blue.ToArgb(), "render: front control drawn over back control (z-order)");
            Check(Throws<RenderException>(() => Renderer.Render(demo, "ProductonView", theme, new Size(800, 600)), "ProductionView"), "render: typo lists candidates");
            Check(Throws<RenderException>(() => Renderer.Render(demo, "ProductionView", Path.Combine(Root, "nope"), new Size(800, 600)), "nope"), "render: bad theme path reported");
            using (var a = new Bitmap(100, 50))
            using (var b = new Bitmap(200, 100))
            using (var s = Renderer.SideBySide(a, b))
                Check(s.Width == 400 && s.Height == 100, "render: side-by-side scales to taller height");

            string old = File.ReadAllText(Fx(@"csproj\old-winforms.csproj"));
            string once = CsprojPatcher.Patch(old);
            Check(once.Contains(@"<HintPath>lib\APC.UI.dll</HintPath>") && once.Contains(@"themes\**\*"), "attach: adds reference and theme copy");
            Check(CsprojPatcher.Patch(once) == once, "attach: idempotent");
            Check(Throws<InvalidOperationException>(() => CsprojPatcher.Patch(File.ReadAllText(Fx(@"csproj\sdk.csproj"))), "SDK"), "attach: rejects SDK-style");
            Check(Throws<InvalidOperationException>(() => CsprojPatcher.Patch(File.ReadAllText(Fx(@"csproj\net48-console.csproj"))), "WinForms"), "attach: rejects non-WinForms");
            string hinted = CsprojPatcher.Patch(File.ReadAllText(Fx(@"csproj\old-with-hintpath.csproj")));
            Check(hinted.Split(new[] { "Include=\"APC.UI" }, StringSplitOptions.None).Length == 2 && hinted.Contains(@"lib\APC.UI.dll") && !hinted.Contains("shared"), "attach: rewrites existing APC.UI HintPath without duplicating");
            string v35 = old.Replace("v4.8", "v3.5");
            Check(Throws<InvalidOperationException>(() => CsprojPatcher.Patch(v35), "v3.5"), "attach: rejects non-4.x framework with actual value");

            Check(KeepsLinesInOrder(old, once), "attach: keeps original lines and indentation (no reformat)");
            // fixture 줄바꿈은 git 체크아웃 설정(autocrlf)에 따라 LF/CRLF가 된다 → 비교는 LF로.
            Check(once.Replace("\r\n", "\n").Contains("\n    <Reference Include=\"APC.UI\">\n      <HintPath>"), "attach: new reference indented like siblings");
            string crlf = old.Replace("\r\n", "\n").Replace("\n", "\r\n");
            string crlfOnce = CsprojPatcher.Patch(crlf);
            Check(crlfOnce == CsprojPatcher.Patch(crlfOnce) && !crlfOnce.Replace("\r\n", "").Contains("\n"), "attach: CRLF file stays pure CRLF and idempotent");

            string projRef = CsprojPatcher.Patch(File.ReadAllText(Fx(@"csproj\old-with-projectref.csproj")));
            Check(!projRef.Contains("<Reference Include=\"APC.UI\""), "attach: existing APC.UI ProjectReference -> no extra Reference");

            string twoCond = CsprojPatcher.Patch(File.ReadAllText(Fx(@"csproj\old-two-conditional.csproj")));
            Check(!twoCond.Contains(@"..\debug") && !twoCond.Contains(@"..\release"), "attach: rewrites every conditional APC.UI reference");

            string tmp = Path.Combine(Path.GetTempPath(), "apcui-utf16-" + Guid.NewGuid().ToString("N") + ".csproj");
            File.WriteAllText(tmp, old.Replace("encoding=\"utf-8\"", "encoding=\"utf-16\""), System.Text.Encoding.Unicode);
            try
            {
                CsprojPatcher.PatchFile(tmp);
                var bytes = File.ReadAllBytes(tmp);
                Check(bytes[0] == 0xFF && bytes[1] == 0xFE && File.ReadAllText(tmp).Contains("encoding=\"utf-16\""), "attach: keeps file encoding matching its declaration (utf-16)");
                Check(File.ReadAllText(tmp).Contains(@"lib\APC.UI.dll"), "attach: PatchFile writes the patch");
            }
            finally { File.Delete(tmp); }

            Check(Throws<InvalidOperationException>(() => CsprojPatcher.Validate(File.ReadAllText(Fx(@"csproj\sdk.csproj"))), "SDK"), "attach --check: validates without patching");
        }

        /// <summary>before의 모든 줄이 after에 같은 순서로(들여쓰기 포함) 남아 있는가.</summary>
        private static bool KeepsLinesInOrder(string before, string after)
        {
            var a = after.Replace("\r\n", "\n").Split('\n');
            int i = 0;
            foreach (var line in before.Replace("\r\n", "\n").Split('\n'))
            {
                while (i < a.Length && a[i] != line) i++;
                if (i == a.Length) return false;
                i++;
            }
            return true;
        }
    }
}
