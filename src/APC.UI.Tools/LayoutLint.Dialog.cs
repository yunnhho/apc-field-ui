using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace APC.UI.Tools
{
    /// <summary>
    /// 글꼴·이름표 열·행 간격·dialog 하단 버튼 점검. 기준값은 APC.UI.Theming.ApcLayout과 같다
    /// (APC.UI 사본이 둘일 수 있어 참조하지 않는다 — ApcLayout을 바꾸면 아래 상수도 함께).
    /// </summary>
    public static partial class LayoutLint
    {
        private const int Pad = 16, GroupGap = 16, FooterButtonWidth = 110, FooterButtonHeight = 40;
        private const int Tol = 1;   // 배율 반올림 오차
        private const int CaptionGapMax = 12;   // 열 제목 줄과 그 아래 입력칸 사이(배율 2배까지)

        private static void CheckFont(Control c, string path, List<string> issues, Theme theme)
        {
            if (theme == null || !(c is Label || IsApc(c))) return;
            if (c.Font.Name == theme.Font.Name && Math.Abs(c.Font.SizeInPoints - theme.Font.SizeInPoints) <= 0.1f) return;
            issues.Add(path + ": 테마와 다른 글꼴(" + c.Font.Name + " " + c.Font.SizeInPoints + "pt, 테마 " + theme.Font.Name + " "
                       + theme.Font.SizeInPoints + "pt) — Font를 직접 만들지 말고 ApcTheme.GetFont(). 큰 글자(현재값·수량·판정·시각)는 "
                       + "ApcValueCard·ApcJudgePanel·ApcStatusTile이 정한다");
        }

        // 같은 줄 오른쪽에 입력칸이 있는 이름표는 보통 굵기(굵게는 묶음·구획 제목만)
        private static void CheckBoldCaption(Control label, Control field, string path, List<string> issues)
        {
            if (!(label is Label) || !label.Font.Bold || !IsInput(field) || field.Left < label.Right - Tol || !Overlaps(label, field)) return;
            issues.Add(path + ": 입력 행 이름표가 굵게 " + Describe(label) + " — 보통 굵기(ApcTheme.GetFont())로");
        }

        private static bool IsInput(Control c)
        {
            // 상속 단계와 상관없이(고객 프로젝트의 MyBox : MyBaseBox : ApcTextBox도)
            return !Is(c, "ApcButton") && FieldTypes.Any(t => Is(c, t));
        }

        private static bool Overlaps(Control a, Control b)
        {
            int ca = a.Top + a.Height / 2, cb = b.Top + b.Height / 2;
            return a.Top < b.Bottom && b.Top < a.Bottom && Math.Abs(ca - cb) <= Math.Max(a.Height, b.Height) / 2;
        }

        // Apc 컨트롤 안(입력칸 속 TextBox 등)은 보지 않는다. 묶음 상자와 Titled() 칸(제목 + 표)은 담는 그릇이라 들어간다.
        private static bool Enters(Control c) { return !IsApc(c) || Is(c, "ApcGroup") || c.GetType().Name == "TitledPanel"; }

        private static IEnumerable<Control> Containers(Control c)
        {
            yield return c;
            foreach (Control child in c.Controls)
                if (child.Visible && child.Width > 0 && Enters(child))
                    foreach (var d in Containers(child)) yield return d;
        }

        // 이름표 + 입력칸 행: 줄의 맨 왼쪽 이름표와 그 오른쪽 첫 입력칸(위→아래)
        private static List<KeyValuePair<Label, Control>> FieldRows(Control parent)
        {
            var kids = parent.Controls.Cast<Control>().Where(x => x.Visible && x.Width > 0 && x.Height > 0).ToList();
            var labels = kids.OfType<Label>().ToList();
            var rows = new List<KeyValuePair<Label, Control>>();
            foreach (var l in labels)
            {
                if (labels.Any(o => o != l && Overlaps(o, l) && o.Left < l.Left)) continue;
                var field = kids.Where(f => IsInput(f) && f.Left >= l.Right - Tol && Overlaps(l, f)).OrderBy(f => f.Left).FirstOrDefault();
                if (field != null) rows.Add(new KeyValuePair<Label, Control>(l, field));
            }
            return rows.OrderBy(r => r.Value.Top).ToList();
        }

        // 입력칸 왼쪽 끝(부모마다, dialog는 전체 — 열 제목 줄이 있는 표 묶음은 그 묶음 안에서만)과 행 간격(부모마다)
        private static void CheckFieldRows(Control root, bool dialog, List<string> issues)
        {
            var all = new List<KeyValuePair<Control, int>>();
            foreach (var parent in Containers(root))
            {
                var rows = FieldRows(parent);
                if (rows.Count == 0) continue;
                var lefts = rows.Select(r => r.Value.Left).ToList();
                bool table = HasCaptionRow(parent, rows);
                if ((!dialog || table) && lefts.Max() - lefts.Min() > Tol)
                    issues.Add(Describe(parent) + ": 입력칸 왼쪽 끝 어긋남(" + string.Join(", ", lefts) + ") — 이름표 열 폭을 하나로");
                // 사이에 다른 요소(묶음·표·구획 제목)가 낀 간격은 행 간격이 아니다
                var gaps = Enumerable.Range(1, rows.Count - 1)
                    .Where(i => !Between(parent, rows, rows[i - 1].Value.Bottom, rows[i].Value.Top))
                    .Select(i => rows[i].Value.Top - rows[i - 1].Value.Bottom).ToList();
                if (gaps.Count >= 2 && gaps.Max() - gaps.Min() > Tol)
                    issues.Add(Describe(parent) + ": 입력 행 간격 다름(" + string.Join(", ", gaps) + "px) — 행 간격 하나로(ApcLayout.RowGap)");
                if (!table)
                    foreach (var r in rows)
                        all.Add(new KeyValuePair<Control, int>(r.Value, root.PointToClient(parent.PointToScreen(r.Value.Location)).X));
            }
            if (dialog && all.Count > 1 && all.Max(a => a.Value) - all.Min(a => a.Value) > Tol)
                issues.Add(Describe(root) + ": 입력칸 왼쪽 끝 어긋남(" + string.Join(", ", all.Select(a => Describe(a.Key) + " " + a.Value))
                           + ") — dialog 전체 이름표 열 하나(AddFieldRow)");
        }

        // 열 제목 줄(AddCaptionRow): 행에 속하지 않은 이름표가 입력칸 바로 위에 같은 왼쪽 끝으로 놓였다
        private static bool HasCaptionRow(Control parent, List<KeyValuePair<Label, Control>> rows)
        {
            var fields = rows.Select(r => r.Value).ToList();
            return parent.Controls.OfType<Label>().Any(l => l.Visible && rows.All(r => r.Key != l)
                && parent.Controls.Cast<Control>().Any(f => IsInput(f) && Math.Abs(f.Left - l.Left) <= Tol && f.Top >= l.Bottom && f.Top - l.Bottom <= CaptionGapMax)
                && fields.Count > 0);
        }

        private static bool Between(Control parent, List<KeyValuePair<Label, Control>> rows, int top, int bottom)
        {
            return parent.Controls.Cast<Control>().Any(c => c.Visible && c.Height > 0 && c.Top >= top && c.Bottom <= bottom
                                                            && rows.All(r => r.Key != c && r.Value != c));
        }

        private static bool IsDialog(Control root)
        {
            if (!(root is Form) || !Is(root, "ApcForm")) return false;
            var p = root.GetType().GetProperty("Resizable");
            return p != null && !(bool)p.GetValue(root, null);
        }

        // dialog 하단 버튼 줄: 높이 고정·폭 110(글자가 길면 글자만큼), 아래 여백 Pad, 위 내용과 GroupGap
        private static void CheckFooter(Form root, List<string> issues, Theme theme)
        {
            var leaves = new List<KeyValuePair<Control, Rectangle>>();
            CollectLeaves(root, root, leaves);
            var buttons = leaves.Where(l => Is(l.Key, "ApcButton")).ToList();
            if (buttons.Count == 0) return;
            int bottom = buttons.Max(b => b.Value.Bottom);
            var footer = buttons.Where(b => bottom - b.Value.Bottom <= Tol).ToList();
            int top = footer.Min(b => b.Value.Top);
            string path = Describe(root);

            int w = theme.Px(FooterButtonWidth), h = theme.Px(FooterButtonHeight);
            // 폭 기준은 110, 글자가 길면 글자 + 양쪽 여백(ApcForm.FooterW와 같은 식)
            Func<Control, int> wantW = c => Math.Max(w, TextRenderer.MeasureText(c.Text, c.Font).Width + theme.Px(24));
            foreach (var b in footer.Where(b => Math.Abs(b.Value.Width - wantW(b.Key)) > Tol || Math.Abs(b.Value.Height - h) > Tol))
                issues.Add(path + ": 하단 버튼 크기 " + Describe(b.Key) + " " + b.Value.Width + "x" + b.Value.Height + "(기준 " + wantW(b.Key) + "x" + h + ") — SetFooter로");

            var cb = (Rectangle)root.GetType().GetProperty("ContentBounds").GetValue(root, null);
            int margin = cb.Bottom - bottom, pad = theme.Px(Pad);
            if (Math.Abs(margin - pad) > Tol)
                issues.Add(path + ": 하단 여백 " + margin + "px(기준 " + pad + ") — ClientSize를 손으로 정하지 말고 SetupDialog 높이 자동으로");

            var above = leaves.Where(l => footer.All(f => f.Key != l.Key) && l.Value.Bottom <= top + Tol).ToList();
            if (above.Count == 0) return;
            int gap = top - above.Max(l => l.Value.Bottom), want = theme.Px(GroupGap);
            if (gap > want + Tol)
                issues.Add(path + ": 하단 버튼 위 빈 공간 " + gap + "px(기준 " + want + ") — SetupDialog 높이 0(자동) 또는 AddContentFill. 카드 격자는 AddContentRow(직접 짠 패널은 자식 Margin이 빈 공간을 만든다)");
            else if (gap < want - Tol)
                issues.Add(path + ": 하단 버튼 위 간격 " + gap + "px(기준 " + want + ")");
        }

        // 배치 패널은 건너뛰고 실제 요소(이름표·Apc 컨트롤·표 등)만 root 좌표로
        private static void CollectLeaves(Control root, Control parent, List<KeyValuePair<Control, Rectangle>> leaves)
        {
            foreach (Control c in parent.Controls)
            {
                if (!c.Visible || c.Width <= 0 || c.Height <= 0) continue;
                if (!IsApc(c) && !(c is Label) && !(c is DataGridView) && c.Controls.Count > 0)
                {
                    CollectLeaves(root, c, leaves);
                    continue;
                }
                var loc = root.PointToClient(parent.PointToScreen(c.Location));
                leaves.Add(new KeyValuePair<Control, Rectangle>(c, new Rectangle(loc, c.Size)));
            }
        }

        /// <summary>검사 대상 화면이 쓰는 APC.UI 사본의 테마(글꼴·배율). 없거나 테마를 안 불렀으면 null → 글꼴·하단 버튼 점검 생략.</summary>
        private sealed class Theme
        {
            public Font Font;
            private MethodInfo _px;

            public int Px(int v) { return (int)_px.Invoke(null, new object[] { v }); }

            public static Theme From(Control root)
            {
                var asm = FindApcAssembly(root);
                var t = asm == null ? null : asm.GetType("APC.UI.Theming.ApcTheme");
                if (t == null) return null;
                var get = t.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m => m.Name == "GetFont");
                var px = t.GetMethod("Px", new[] { typeof(int) });
                if (get == null || px == null) return null;
                try
                {
                    var args = get.GetParameters().Select(p => p.ParameterType == typeof(FontStyle) ? (object)FontStyle.Regular : p.DefaultValue).ToArray();
                    return new Theme { Font = (Font)get.Invoke(null, args), _px = px };
                }
                catch (TargetInvocationException) { return null; }
            }

            private static Assembly FindApcAssembly(Control c)
            {
                for (var t = c.GetType(); t != null; t = t.BaseType)
                    if (t.Assembly.GetName().Name == ApcAssembly) return t.Assembly;
                foreach (Control child in c.Controls)
                {
                    var a = FindApcAssembly(child);
                    if (a != null) return a;
                }
                return null;
            }
        }
    }
}
