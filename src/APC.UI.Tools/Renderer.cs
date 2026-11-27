using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace APC.UI.Tools
{
    /// <summary>렌더 실패. 메시지만 보고 원인을 알 수 있게 쓴다.</summary>
    public sealed class RenderException : Exception
    {
        public RenderException(string message, Exception inner = null) : base(message, inner) { }
    }

    /// <summary>화면(UserControl/Form)을 테마를 적용해 오프스크린으로 그린다.</summary>
    public static class Renderer
    {
        private const int MaxCandidates = 5;
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static Bitmap Render(string assemblyPath, string typeName, string themeDir, Size size)
        {
            return Render(assemblyPath, typeName, themeDir, size, null);
        }

        /// <summary>size가 null이면 크기 고정 폼(dialog)은 자기 크기, 그 밖(화면·메인 창)은 1920×1080. issues가 있으면 배치 점검(LayoutLint) 결과를 채운다.</summary>
        public static Bitmap Render(string assemblyPath, string typeName, string themeDir, Size? size, List<string> issues)
        {
            Assembly asm;
            try { asm = AssemblyLoader.Load(assemblyPath); }
            catch (Exception ex) when (ex is IOException || ex is BadImageFormatException)
            {
                throw new RenderException("어셈블리를 읽을 수 없습니다: " + assemblyPath + " (" + ex.Message + ")", ex);
            }

            var type = FindType(asm, typeName);
            LoadTheme(type, themeDir);
            var ctor = type.GetConstructor(AnyInstance, null, Type.EmptyTypes, null);
            if (ctor == null) throw new RenderException(type.FullName + "에 매개변수 없는 생성자가 없습니다. 샘플 데이터를 쓰는 기본 생성자를 두세요.");

            Control view;
            try { view = (Control)ctor.Invoke(null); }
            catch (TargetInvocationException ex)
            {
                throw new RenderException(type.FullName + " 생성 실패: " + ex.InnerException.Message + Where(ex.InnerException, asm), ex.InnerException);
            }
            using (view) return Draw(view, size ?? (IsFixedDialog(view) ? (Size?)null : new Size(1920, 1080)), issues);
        }

        // 크기를 바꿀 수 없는 폼(dialog)만 자기 크기로 그린다. 메인 창처럼 늘어나는 폼은 현장 기준 1920×1080.
        // 예외가 난 앱 코드 위치(앱 어셈블리의 첫 프레임). pdb가 있으면 파일:줄까지.
        public static string Where(Exception ex, Assembly app)
        {
            var frame = new System.Diagnostics.StackTrace(ex, true).GetFrames()?
                .FirstOrDefault(f => f.GetMethod() != null && f.GetMethod().DeclaringType != null && f.GetMethod().DeclaringType.Assembly == app);
            if (frame == null) return "";
            var m = frame.GetMethod();
            string file = frame.GetFileName();
            return " (위치: " + m.DeclaringType.Name + "." + m.Name + (file != null ? " " + Path.GetFileName(file) + ":" + frame.GetFileLineNumber() : "") + ")";
        }

        private static bool IsFixedDialog(Control view)
        {
            var form = view as Form;
            if (form == null) return false;
            var resizable = form.GetType().GetProperty("Resizable", typeof(bool));   // ApcForm(사본이 둘일 수 있어 이름으로)
            if (resizable != null) return !(bool)resizable.GetValue(form, null);
            return form.FormBorderStyle != FormBorderStyle.Sizable && form.FormBorderStyle != FormBorderStyle.SizableToolWindow;
        }

        /// <summary>두 이미지를 높은 쪽 높이에 맞춰 비율대로 늘린 뒤 가로로 붙인다.</summary>
        public static Bitmap SideBySide(Image left, Image right)
        {
            int h = Math.Max(left.Height, right.Height);
            int wl = left.Width * h / left.Height;
            int wr = right.Width * h / right.Height;
            var bmp = new Bitmap(wl + wr, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.White);
                g.DrawImage(left, 0, 0, wl, h);
                g.DrawImage(right, wl, 0, wr, h);
            }
            return bmp;
        }

        // 화면이 상속한 바로 그 APC.UI의 ApcTheme을 불러야 같은 정적 테마 상태를 쓴다(한 프로세스에 APC.UI 사본이 둘일 수 있다).
        private static void LoadTheme(Type view, string themeDir)
        {
            Assembly apcUi = null;
            for (var t = view; t != null && apcUi == null; t = t.BaseType)
                if (t.Assembly.GetName().Name == "APC.UI") apcUi = t.Assembly;
            if (apcUi == null) throw new RenderException(view.FullName + "은(는) APC.UI 컨트롤을 상속하지 않습니다.");

            var load = apcUi.GetType("APC.UI.Theming.ApcTheme", true).GetMethod("Load", new[] { typeof(string) });
            try { load.Invoke(null, new object[] { themeDir }); }
            catch (TargetInvocationException ex)
            {
                throw new RenderException("테마 로드 실패 (" + themeDir + "): " + ex.InnerException.Message, ex.InnerException);
            }
        }

        private static Type FindType(Assembly asm, string name)
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }

            var controls = types.Where(t => typeof(Control).IsAssignableFrom(t) && !t.IsAbstract).ToArray();
            var exact = controls.FirstOrDefault(t => t.FullName == name);
            if (exact != null) return exact;

            var byName = controls.Where(t => t.Name == name).ToArray();
            if (byName.Length == 1) return byName[0];
            if (byName.Length > 1)
                throw new RenderException("이름이 같은 형식이 여러 개입니다. 전체 이름을 쓰세요: " + string.Join(", ", byName.Select(t => t.FullName).ToArray()));

            var near = controls
                .OrderBy(t => Distance(t.Name.ToLowerInvariant(), name.ToLowerInvariant()))
                .ThenBy(t => t.Name, StringComparer.Ordinal)
                .Take(MaxCandidates)
                .Select(t => t.Name)
                .ToArray();
            throw new RenderException("형식을 찾을 수 없습니다: " + name + ". 비슷한 형식: " + string.Join(", ", near));
        }

        private static int Distance(string a, string b)
        {
            var d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            return d[a.Length, b.Length];
        }

        // DrawToBitmap draws siblings in reverse z-order (the back control lands on top), so the PNG
        // disagreed with the running app wherever controls overlap. Reverse every child list, draw,
        // then reverse back. Layout is suspended so the swap never re-docks anything.
        private static void DrawInZOrder(Control host, Bitmap bmp, Size size)
        {
            var containers = Descendants(host).Where(c => c.Controls.Count > 1).ToList();
            foreach (var c in containers) c.SuspendLayout();
            try
            {
                foreach (var c in containers) ReverseChildren(c);
                try { host.DrawToBitmap(bmp, new Rectangle(Point.Empty, size)); }
                finally { foreach (var c in containers) ReverseChildren(c); }
            }
            finally { foreach (var c in containers) c.ResumeLayout(false); }
        }

        private static void ReverseChildren(Control parent)
        {
            var kids = parent.Controls.Cast<Control>().ToArray();
            for (int i = 0; i < kids.Length; i++) parent.Controls.SetChildIndex(kids[i], kids.Length - 1 - i);
        }

        private static IEnumerable<Control> Descendants(Control c)
        {
            yield return c;
            foreach (Control k in c.Controls)
                foreach (var d in Descendants(k)) yield return d;
        }

        // ponytail: 화면 밖에 잠깐 띄워서 그린다. 창 크기는 OS 최대 창 크기(대개 화면 크기)로 잘릴 수 있다; 더 큰 캡처가 필요하면 PrintWindow 방식으로 바꾼다.
        private static Bitmap Draw(Control view, Size? requested, List<string> issues)
        {
            var form = view as Form;
            Form host = form ?? new Form();
            try
            {
                if (form != null) form.TopLevel = true;
                else
                {
                    host.FormBorderStyle = FormBorderStyle.None;
                    view.Dock = DockStyle.Fill;
                    host.Controls.Add(view);
                }
                host.ShowInTaskbar = false;
                host.StartPosition = FormStartPosition.Manual;
                host.Location = new Point(-32000, -32000);
                if (requested.HasValue) host.Size = requested.Value;
                host.Show();
                if (requested.HasValue) host.Size = requested.Value; // Load 이벤트에서 크기를 바꾸는 폼 대비
                Application.DoEvents();
                var size = host.Size;   // 크기 미지정 Form은 Load·DPI 배율이 끝난 뒤의 실제 크기
                if (issues != null)
                {
                    try { issues.AddRange(LayoutLint.Inspect(view)); }
                    catch (Exception ex) { issues.Add(LayoutLint.NotePrefix + "배치 점검 실패(" + ex.GetType().Name + ": " + ex.Message + ") — PNG를 눈으로 확인"); }
                }

                var bmp = new Bitmap(size.Width, size.Height);
                DrawInZOrder(host, bmp, size);
                return bmp;
            }
            finally
            {
                if (form == null)
                {
                    host.Controls.Remove(view);
                    host.Dispose();
                }
                else form.Hide();
            }
        }
    }
}
