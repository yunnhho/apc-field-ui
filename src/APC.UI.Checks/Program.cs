using System;
using System.IO;
using APC.UI.Theming;

namespace APC.UI.Checks
{
    // ponytail: assert-based self-check runner, no test framework (net40, no NuGet). Swap to xUnit if a test project is ever added.
    internal static partial class Program
    {
        private static int _failed;
        private static int _passed;

        internal static string Root;

        [STAThread]
        private static int Main()
        {
            Root = FindRoot();
            Run("theme", RunThemeChecks);
            Run("asset", RunAssetChecks);
            Run("button", RunButtonChecks);
            Run("lamp", RunLampChecks);
            Run("scan", RunScanChecks);
            Run("form", RunFormChecks);
            Run("grid", RunGridChecks);
            Run("review-fix", RunReviewFixChecks);
            Run("input", RunInputChecks);
            Run("display", RunDisplayChecks);
            Run("dialog-layout", RunDialogLayoutChecks);
            Run("lint", RunLintChecks);
            Run("tools", RunToolChecks);  // 마지막: render가 전역 테마를 바꾼다

            Console.WriteLine(_failed == 0 ? "ALL PASSED (" + _passed + ")" : "FAILED " + _failed + " / " + (_passed + _failed));
            return _failed;
        }

        private static void Run(string area, Action checks)
        {
            try { checks(); }
            catch (Exception ex) { Check(false, area + ": crashed: " + ex.GetType().Name + ": " + ex.Message); }
        }

        internal static void Check(bool ok, string name)
        {
            if (ok) { _passed++; return; }
            _failed++;
            Console.WriteLine("FAIL  " + name);
        }

        internal static bool Throws<T>(Action action, string mustContain) where T : Exception
        {
            try { action(); return false; }
            catch (T ex) { return ex.Message.Contains(mustContain); }
        }

        internal static string Fx(string name)
        {
            return Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fixtures"), name);
        }

        private static string FindRoot()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "APC.UI.sln"))) dir = dir.Parent;
            if (dir == null) throw new InvalidOperationException("APC.UI.sln not found above " + AppDomain.CurrentDomain.BaseDirectory);
            return dir.FullName;
        }

        private static void RunThemeChecks()
        {
            var t = ThemeLoader.Load(Path.Combine(Root, Path.Combine("themes", "default")));
            Check(t.FontFamily == "맑은 고딕", "theme: korean fontFamily round-trips");
            Check(t.FontSize == 12, "theme: font size 12 (현장 확정)");
            Check(t.Scan.MaxKeyIntervalMs == 50 && t.Scan.MinLength == 4, "theme: scan defaults");
            Check(ThemeLoader.GetStatusColor(t, StatusKind.Alarm) == ThemeLoader.ParseColor("#DC2626"), "theme: alarm color");
            Check(Throws<ThemeException>(() => ThemeLoader.Load(Fx("missing")), "theme.json"), "theme: missing file names file");
            Check(Throws<ThemeException>(() => ThemeLoader.Load(Fx("syntax")), "theme.json"), "theme: bad json");
            Check(Throws<ThemeException>(() => ThemeLoader.Load(Fx("no-status")), "status"), "theme: missing field named");
            Check(Throws<ThemeException>(() => ThemeLoader.Load(Fx("alarm-blue")), "status.alarm"), "theme: alarm hue changed rejected");
            Check(!Throws<ThemeException>(() => ThemeLoader.Load(Fx("alarm-dark")), ""), "theme: darker alarm red accepted");
            Check(!Throws<ThemeException>(() => ThemeLoader.Load(Fx("bom")), ""), "theme: UTF-8 BOM (Notepad/PowerShell) accepted");
            Check(t.Colors.Brand == "#E60012" && t.Colors.BorderStrong == "#9AA7B4" && t.Colors.Panel[0] == "#FDFDFE", "theme: steel-ink tokens");
            var old = ThemeLoader.Load(Path.Combine(Root, Path.Combine("themes", "sample")));
            Check(old.Colors.Panel[0] == "#FFFFFF" && old.Colors.Panel[1] == "#FFFFFF", "theme: missing panel defaults to white");
            Check(old.Colors.BorderStrong == old.Colors.Border, "theme: missing borderStrong = border");
            Check(old.Colors.Brand == old.Colors.Text, "theme: missing brand = text (no red)");
        }
    }
}
