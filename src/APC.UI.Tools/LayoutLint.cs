using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace APC.UI.Tools
{
    /// <summary>
    /// 배치가 끝난(화면에 띄운) 컨트롤 트리를 검사해 사람이 render PNG에서 지적하던 문제를 글로 돌려준다:
    /// 글자 잘림, 부모 밖 넘침·형제 겹침, 같은 줄 입력칸·버튼 위·높이 어긋남, 표준 컨트롤, 표 스크롤바·열 폭 쏠림, [확인][취소] 순서, 전체 선택된 글자, Apc 표시 컨트롤의 말줄임(…).
    /// APC.UI 내부(입력칸 속 TextBox, 표 스크롤바 등)는 들어가지 않는다. APC.UI 사본이 둘일 수 있어 형식은 이름으로 비교한다.
    /// </summary>
    public static partial class LayoutLint
    {
        private const string ApcAssembly = "APC.UI";
        private const int TextPad = 8;   // 버튼·이름표 안쪽 여유(양쪽 합)
        private static readonly HashSet<string> Forbidden = new HashSet<string>
        {
            "Button", "TextBox", "MaskedTextBox", "RichTextBox", "ComboBox", "ListBox", "NumericUpDown",
            "DateTimePicker", "DataGridView", "GroupBox", "CheckedListBox", "ListView", "TabControl", "CheckBox", "RadioButton",
        };
        private static readonly HashSet<string> FieldTypes = new HashSet<string>
        {
            "ApcTextBox", "ApcScanBox", "ApcComboBox", "ApcNumericBox", "ApcDateBox", "ApcButton", "ApcCheckBox",
        };
        private static readonly HashSet<string> ConfirmTexts = new HashSet<string> { "확인", "저장", "적용", "등록", "삭제" };

        /// <summary>문제가 아닌 참고(판단은 사람·에이전트 몫). 건수에 넣지 않는다.</summary>
        public const string NotePrefix = "참고: ";

        public static bool IsNote(string issue) { return issue.StartsWith(NotePrefix, StringComparison.Ordinal); }

        public static List<string> Inspect(Control root)
        {
            var issues = new List<string>();
            var theme = Theme.From(root);
            Walk(root, root.GetType().Name, issues, theme);
            bool dialog = IsDialog(root);
            CheckFieldRows(root, dialog, issues);
            if (dialog && theme != null) CheckFooter((Form)root, issues, theme);
            CheckClipped(root, issues);
            // 겹침·넘침을 고치면 표 높이가 바뀐다 → 그때 센 행 수는 틀린다. 점검이 0건일 때만 알린다.
            if (issues.Any(i => !IsNote(i))) issues.RemoveAll(i => IsNote(i) && i.Contains(UnderfilledNote));
            return issues;
        }

        // Apc 표시 컨트롤(타일·카드·헤더 등)은 글자를 직접 그려 말줄임(…)한다. 한 번 그려 보며 APC.UI가 기록한 잘림을 받는다.
        private static void CheckClipped(Control root, List<string> issues)
        {
            var apc = AllControls(root).Select(c => c.GetType().Assembly).FirstOrDefault(a => a.GetName().Name == ApcAssembly);
            var log = apc == null ? null : apc.GetType("APC.UI.Drawing.TextClip").GetField("Log", BindingFlags.Static | BindingFlags.NonPublic);
            if (log == null || root.Width <= 0 || root.Height <= 0) return;
            var clipped = new List<Tuple<Control, string>>();
            log.SetValue(null, clipped);
            try { using (var bmp = new Bitmap(root.Width, root.Height)) root.DrawToBitmap(bmp, new Rectangle(Point.Empty, root.Size)); }
            finally { log.SetValue(null, null); }
            foreach (var c in clipped.Where(x => x.Item1.Visible).Distinct())
                issues.Add(PathOf(c.Item1, root) + ": 글자 " + c.Item2 + " — 문구를 줄이거나 칸을 넓힌다");
        }

        private static IEnumerable<Control> AllControls(Control c)
        {
            yield return c;
            foreach (Control k in c.Controls)
                foreach (var d in AllControls(k)) yield return d;
        }

        private static string PathOf(Control c, Control root)
        {
            var parts = new List<string>();
            for (var x = c; x != null && x != root; x = x.Parent) parts.Insert(0, Describe(x));
            parts.Insert(0, root.GetType().Name);
            return string.Join(" > ", parts.ToArray());
        }

        private static void Walk(Control c, string path, List<string> issues, Theme theme)
        {
            var shown = c.Controls.Cast<Control>().Where(x => x.Visible).ToList();
            // 빈 이름표(아직 채우지 않은 안내 글)는 접혀도 보이는 것이 없다
            foreach (var z in shown.Where(x => (x.Width <= 0 || x.Height <= 0) && !(x is Label && x.Text.Length == 0)))
                issues.Add(path + " > " + Describe(z) + ": 크기 0으로 접힘(" + z.Width + "x" + z.Height + ") — 자리가 모자란다");
            var visible = shown.Where(x => x.Width > 0 && x.Height > 0).ToList();
            CheckSiblings(c, visible, path, issues);
            foreach (var child in visible)
            {
                string p = path + " > " + Describe(child);
                CheckOne(child, p, issues);
                CheckFont(child, p, issues, theme);
                if (Enters(child)) Walk(child, p, issues, theme);
            }
        }

        private static void CheckOne(Control c, string path, List<string> issues)
        {
            var t = c.GetType();
            if (t.Namespace == "System.Windows.Forms" && Forbidden.Contains(t.Name))
                issues.Add(path + ": 표준 " + t.Name + " — Apc 컨트롤로 바꾼다");
            else if (t.BaseType == typeof(Control) && t.Assembly != typeof(Control).Assembly && !IsApc(c))
                issues.Add(path + ": Control을 직접 상속해 그림 — Apc 표시 컨트롤로 바꾼다");

            if (c is DataGridView grid) CheckGrid(grid, path, issues);
            // 숫자칸·날짜칸은 안쪽 입력칸까지 본다(Walk는 Apc 컨트롤 안으로 들어가지 않는다).
            var boxes = c is TextBoxBase ? new[] { (TextBoxBase)c } : IsApc(c) ? c.Controls.OfType<TextBoxBase>().ToArray() : new TextBoxBase[0];
            if (boxes.Any(tb => tb.SelectionLength > 0 && tb.SelectionLength == tb.TextLength && (tb.Focused || !tb.HideSelection)))
                issues.Add(path + ": 글자 전체가 선택된 채 표시(파란 칠) — 첫 포커스 칸은 SelectionStart = Text.Length로");
            int need = TextWidth(c);
            if (need > 0 && need > c.ClientSize.Width)
                issues.Add(path + ": 글자 잘림(필요 " + need + "px > 폭 " + c.ClientSize.Width + "px)");
        }

        // 잘림 판정에 쓸 필요 폭. 0이면 검사하지 않는다.
        private static int TextWidth(Control c)
        {
            if (c is Label l && !l.AutoSize && l.Text.Length > 0)
            {
                // 여러 줄 이름표는 높이로 본다
                var wrapped = TextRenderer.MeasureText(l.Text, l.Font, new Size(l.ClientSize.Width, 0), TextFormatFlags.WordBreak);
                return wrapped.Height > l.ClientSize.Height ? TextRenderer.MeasureText(l.Text, l.Font).Width : 0;
            }
            if (Is(c, "ApcButton") && c.Text.Length > 0)
                return TextRenderer.MeasureText(c.Text, c.Font).Width + TextPad;
            if (c is ComboBox combo && combo.Items.Count > 0)
            {
                int widest = combo.Items.Cast<object>().Max(i => TextRenderer.MeasureText(combo.GetItemText(i), combo.Font).Width);
                // 화살표 영역(FieldFrame.ButtonWidth와 같은 식) + 글자 양쪽 여백 7px씩(ApcComboBox.OnPaint의 pad, 100% 배율)
                return widest + Math.Max(c.Height - 2, 24) + 2 * 7;
            }
            return 0;
        }

        private static void CheckGrid(DataGridView grid, string path, List<string> issues)
        {
            // 실제 스크롤바 컨트롤을 본다(숨긴 행·이미 스크롤된 표도 맞게).
            if (grid.Controls.OfType<VScrollBar>().Any(v => v.Visible))
                issues.Add(path + ": 표 세로 스크롤바(행 " + grid.RowCount + " 중 " + grid.DisplayedRowCount(false) + "행 보임) — 샘플 행을 " + grid.DisplayedRowCount(false) + "개로 줄이거나 표를 키운다");
            if (grid.Controls.OfType<HScrollBar>().Any(h => h.Visible))
                issues.Add(path + ": 표 가로 스크롤바 — 열 폭을 줄이거나 Fill로");

            // 가운데 정렬 열(짧은 값: ID·숫자·판정)이 표 폭 절반을 넘으면 옆 열이 끝으로 밀린다. 긴 글 열(MiddleLeft)만 넓게.
            var cols = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Visible).ToList();
            foreach (var col in cols)
                if (cols.Count >= 3 && col.Width * 2 > grid.ClientSize.Width && col.InheritedStyle.Alignment != DataGridViewContentAlignment.MiddleLeft)
                    issues.Add(path + ": 열 '" + col.HeaderText + "'가 표 폭 절반 넘게 차지(" + col.Width + "/" + grid.ClientSize.Width + "px) — 짧은 값 열뿐이면 램프 열 빼고 모두 Fill로 고르게, 긴 글 열이면 MiddleLeft로");

            // 램프 열(기본 제목 "상태")과 같은 제목의 글 열은 어느 열이 무엇인지 모른다. 묶음 머리글 표의 하위 열 반복은 정상이라 램프 열 중복만 본다.
            foreach (var dup in cols.GroupBy(x => x.HeaderText.Trim()).Where(g => g.Key.Length > 0 && g.Count() > 1 && g.Any(x => x.GetType().Name == "ApcLampColumn")))
                issues.Add(path + ": 열 제목 '" + dup.Key + "' 중복 — 램프 열 기본 제목이 '상태'이니 글 열은 '내용'·'판정' 등으로");

            // 읽기 전용 표(이력·목록)는 샘플로 높이를 채운다. 입력 표(ReadOnly=false)는 칸 수가 정해져 있어 제외.
            if (!grid.ReadOnly || grid.RowTemplate.Height <= 0) return;
            // DataGridView는 테두리를 클라이언트 영역 안에 그린다 → 테두리만큼 빼야 스크롤바 판정과 같은 행 수가 나온다.
            int border = grid.BorderStyle == BorderStyle.FixedSingle ? 2 : grid.BorderStyle == BorderStyle.Fixed3D ? 4 : 0;
            int body = grid.ClientSize.Height - border - (grid.ColumnHeadersVisible ? grid.ColumnHeadersHeight : 0);
            int fits = body / grid.RowTemplate.Height;
            int used = grid.Rows.Cast<DataGridViewRow>().Sum(r => r.Height);   // 행이 높이를 나눠 가진 표(ApcGrid.FillRows)는 비지 않았다
            if (body - used >= grid.RowTemplate.Height)
                issues.Add(NotePrefix + path + ": " + UnderfilledNote + "(행 " + grid.RowCount + ", " + fits + "행까지 들어감) — 이력·목록이면 샘플 행을 " + fits + "개로, 설비 수처럼 정해진 목록이면 FillRows = true");
            int repeats = RepeatedRows(grid);
            if (repeats >= MinRepeatedRows)
                issues.Add(NotePrefix + path + ": 시각·ID만 다르고 나머지가 앞 행과 같은 행 " + repeats + "개(샘플이 되풀이로 보임) — 이력이면 설비·값·문구를 섞는다");
        }

        private const string UnderfilledNote = "표 아래가 빔";
        private const int MinRepeatedRows = 3;   // 우연히 겹치는 한두 행은 넘긴다

        // 값이 모두 다른 열(시각·ID)을 빼고 나머지 칸이 앞 행과 똑같은 행 수.
        // 측정값(소수) 열이 있는 표만 본다 — 모델·수량·판정처럼 원래 겹치는 값만 있는 표는 되풀이가 자연스럽다.
        private static int RepeatedRows(DataGridView grid)
        {
            var rows = grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
            Func<DataGridViewRow, DataGridViewColumn, string> cell = (r, c) => Convert.ToString(r.Cells[c.Index].Value);
            var cols = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible)
                .Where(c => rows.Select(r => cell(r, c)).Distinct().Count() < rows.Count)
                .ToList();
            decimal d;
            bool measured = cols.Any(c => rows.Any(r => cell(r, c).Contains(".") && decimal.TryParse(cell(r, c).Split(' ')[0], out d)));
            if (!measured) return 0;
            var seen = new HashSet<string>();
            return rows.Count(r => !seen.Add(string.Join("\u0001", cols.Select(c => Convert.ToString(r.Cells[c.Index].Value)).ToArray())));
        }

        private const int LineMaxPx = 6;

        /// <summary>글자 없는 얇은 선(Label 조립의 현재 시각선·눈금선·알람선) — 막대·칸 위를 가로지르는 겹침은 의도한 것이다.</summary>
        private static bool IsLine(Control c)
        {
            return c is Label && string.IsNullOrEmpty(c.Text) && Math.Min(c.Width, c.Height) <= LineMaxPx;
        }

        private static void CheckSiblings(Control parent, List<Control> kids, string path, List<string> issues)
        {
            var area = parent.ClientRectangle;
            bool scrolls = parent is ScrollableControl s && s.AutoScroll;
            foreach (var k in kids)
                if (!scrolls && !area.Contains(k.Bounds))
                    issues.Add(path + " > " + Describe(k) + ": 부모 밖으로 넘침(" + Fmt(k.Bounds) + " / 부모 " + area.Width + "x" + area.Height + ")");

            for (int i = 0; i < kids.Count; i++)
                for (int j = i + 1; j < kids.Count; j++)
                {
                    var a = kids[i];
                    var b = kids[j];
                    if (a.Bounds.IntersectsWith(b.Bounds) && !IsLine(a) && !IsLine(b))
                        issues.Add(path + ": " + Describe(a) + "(" + Fmt(a.Bounds) + ")와(과) " + Describe(b) + "(" + Fmt(b.Bounds) + ") 겹침");
                    else if (SameRow(a, b) && (a.Top != b.Top || a.Height != b.Height))
                        issues.Add(path + ": 같은 줄 " + Describe(a) + "(위 " + a.Top + ", 높이 " + a.Height + ")와(과) " + Describe(b) + "(위 " + b.Top + ", 높이 " + b.Height + ") 어긋남");
                    CheckOrder(a, b, path, issues);
                    CheckBoldCaption(a, b, path, issues);
                    CheckBoldCaption(b, a, path, issues);
                }
        }

        private static bool SameRow(Control a, Control b)
        {
            if (!FieldTypes.Contains(a.GetType().Name) && !FieldTypes.Contains(Base(a))) return false;
            if (!FieldTypes.Contains(b.GetType().Name) && !FieldTypes.Contains(Base(b))) return false;
            // 세로 범위가 겹치고 중심선이 가까워야 같은 줄(2열 폼의 엇갈린 행은 제외)
            int ca = a.Top + a.Height / 2, cb = b.Top + b.Height / 2;
            return a.Top < b.Bottom && b.Top < a.Bottom && Math.Abs(ca - cb) < Math.Min(a.Height, b.Height) / 2;
        }

        // [확인][취소]: 취소 버튼이 오른쪽 끝(ApcDialog와 같은 순서)
        private static void CheckOrder(Control a, Control b, string path, List<string> issues)
        {
            if (!Is(a, "ApcButton") || !Is(b, "ApcButton") || a.Top >= b.Bottom || b.Top >= a.Bottom) return;
            var cancel = a.Text == "취소" ? a : b.Text == "취소" ? b : null;
            var confirm = cancel == a ? b : a;
            if (cancel == null || !ConfirmTexts.Contains(confirm.Text)) return;
            if (cancel.Left < confirm.Left)
                issues.Add(path + ": 버튼 순서 [취소][" + confirm.Text + "] — [" + confirm.Text + "][취소]로");
        }

        private static bool IsApc(Control c) { return c.GetType().Assembly.GetName().Name == ApcAssembly; }
        private static string Base(Control c) { return c.GetType().BaseType == null ? "" : c.GetType().BaseType.Name; }

        private static bool Is(Control c, string apcType)
        {
            for (var t = c.GetType(); t != null; t = t.BaseType)
                if (t.Name == apcType && t.Assembly.GetName().Name == ApcAssembly) return true;
            return false;
        }

        private static string Describe(Control c)
        {
            string name = string.IsNullOrEmpty(c.Name) ? c.GetType().Name : c.Name;
            string text = c.Text;
            if (string.IsNullOrEmpty(text) || c is TextBoxBase || c is ComboBox) return name;
            if (text.Length > 12) text = text.Substring(0, 12) + "…";
            return name + " '" + text + "'";
        }

        private static string Fmt(Rectangle r) { return r.X + "," + r.Y + " " + r.Width + "x" + r.Height; }
    }
}
