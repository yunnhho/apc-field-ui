using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using APC.UI.Tools;

namespace APC.UI.Checks
{
    internal static partial class Program
    {
        private static readonly string[] Templates =
        {
            "EquipmentView", "TempHumiView", "JudgeView", "FailureAnalysisView", "TrendMatrixView", "WorkStartView", "ProductionView",
            "DefectView", "HistoryView", "EquipmentDetailDialog", "RecipeDialog", "AdminPasswordDialog", "LotInputDialog", "SettingsDialog",
        };

        private static void RunLintChecks()
        {
            ApcTheme.Load(Path.Combine(Root, Path.Combine("themes", "default")));
            CheckLintCatches();

            // 템플릿은 스킬의 출발점이다 → 점검 0건이어야 첫 실행도 0건에서 시작한다.
            string demo = Path.Combine(Root, @"src\APC.UI.Demo\bin\Debug\APC.UI.Demo.exe");
            string theme = Path.Combine(Root, @"themes\default");
            foreach (var name in Templates)
            {
                var issues = new List<string>();
                using (Renderer.Render(demo, name, theme, null, issues)) { }
                foreach (var i in issues) Console.WriteLine("      " + i);
                Check(issues.Count(i => !LayoutLint.IsNote(i)) == 0, "lint: template " + name + " has no layout issues");
            }
            using (var bmp = Renderer.Render(demo, "RecipeDialog", theme, null, null))
                Check(bmp.Width < 1920, "render: fixed-size dialog without --size uses its own size");
        }

        private static void CheckLintCatches()
        {
            Check(Lint(p => p.Controls.Add(new Label { Text = "아주 긴 이름표 글자입니다", AutoSize = false, Width = 40, Height = 20 })).Any(i => i.Contains("글자 잘림")), "lint: clipped label");
            Check(Lint(p => p.Controls.Add(new ApcButton { Text = "레시피 불러오기", Width = 40 })).Any(i => i.Contains("글자 잘림")), "lint: clipped button");
            Check(Lint(p => { var c = new ApcComboBox { Width = 60 }; c.Items.Add("모델 A-1234567 긴 이름"); p.Controls.Add(c); }).Any(i => i.Contains("글자 잘림")), "lint: narrow combo");
            Check(Lint(p => p.Controls.Add(new ApcButton { Text = "A", Left = 420, Width = 60 })).Any(i => i.Contains("넘침")), "lint: overflow parent");
            Check(Lint(p => { p.Controls.Add(new ApcButton { Text = "A", Width = 60 }); p.Controls.Add(new ApcButton { Text = "B", Left = 30, Width = 60 }); }).Any(i => i.Contains("겹침")), "lint: overlap");
            Check(Lint(p => { p.Controls.Add(new ApcTextBox { Top = 3, Width = 100 }); p.Controls.Add(new ApcButton { Text = "B", Left = 120, Width = 60, Height = 34 }); }).Any(i => i.Contains("어긋남")), "lint: misaligned row");
            Check(Lint(p => p.Controls.Add(new TextBox())).Any(i => i.Contains("표준 TextBox")), "lint: standard control");
            Check(Lint(p => { var t = new ApcTextBox { Text = "6205-2RS", Width = 150 }; p.Controls.Add(t); t.HideSelection = false; t.SelectAll(); }).Any(i => i.Contains("선택된 채")), "lint: fully selected text reported");
            Check(Lint(p => { var n = new ApcNumericBox { DecimalPlaces = 1, Value = 50m, Width = 150 }; p.Controls.Add(n); n.TextBox.HideSelection = false; n.TextBox.SelectAll(); }).Any(i => i.Contains("선택된 채")), "lint: fully selected text inside ApcNumericBox reported");
            using (var f = new ApcForm { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-32000, -32000) })
            {
                var n = new ApcNumericBox { DecimalPlaces = 1, Value = 50m, Top = 60 };
                f.Controls.Add(n);
                f.Show();
                Application.DoEvents();
                // ActiveControl이 비면(창 활성화 안 됨) 아무것도 검증하지 않으므로 실패로 본다.
                Check(f.ActiveControl == n && n.TextBox.TextLength > 0 && n.TextBox.SelectionLength == 0, "ApcForm: first ApcNumericBox is not shown fully selected");
            }
            Check(Lint(p => p.Controls.Add(new ApcButton { Text = "A", Width = 0 })).Any(i => i.Contains("크기 0")), "lint: collapsed control reported");
            Check(Lint(p => { p.Controls.Add(new ApcButton { Text = "취소", Width = 70 }); p.Controls.Add(new ApcButton { Text = "저장", Left = 80, Width = 70 }); }).Any(i => i.Contains("버튼 순서")), "lint: cancel before confirm");
            Check(Lint(p =>
            {
                var g = new ApcGrid { Width = 300, Height = 100 };
                g.Columns.Add("a", "A");
                for (int r = 0; r < 20; r++) g.Rows.Add("x");
                p.Controls.Add(g);
            }).Any(i => i.Contains("세로 스크롤바")), "lint: grid scrollbar");
            Check(Lint(p =>
            {
                var g = new ApcGrid { Width = 300, Height = 250, ReadOnly = true };
                g.Columns.Add("a", "A");
                g.Rows.Add("x");
                p.Controls.Add(g);
            }).Any(i => LayoutLint.IsNote(i) && i.Contains("표 아래가 빔")), "lint: underfilled read-only grid is a note");
            Check(Lint(p => p.Controls.Add(new ApcStatusTile { Text = "사출기 3 금형 온도 조절기 (2차)", Status = StatusKind.Warn, Width = 160, Height = 80 }))
                .Any(i => i.Contains("말줄임")), "lint: ellipsized tile name");
            Check(!Lint(p => p.Controls.Add(new ApcStatusTile { Text = "사출기 3", Width = 160, Height = 80 })).Any(i => i.Contains("말줄임")), "lint: fitting tile text passes");
            CheckGridFitsMatchesScrollbar();
            // 겹침이 있으면 표 높이가 바뀔 것이라 행 수 참고를 내지 않는다(B 시연: 겹침 고친 뒤 18행 → 17행)
            Check(!Lint(p =>
            {
                var g = new ApcGrid { Width = 300, Height = 250, ReadOnly = true };
                g.Columns.Add("a", "A");
                g.Rows.Add("x");
                p.Controls.Add(g);
                p.Controls.Add(new ApcButton { Text = "A", Width = 60 });
            }).Any(i => i.Contains("표 아래가 빔")), "lint: underfilled note withheld while layout issues remain");
            // 램프 + 문구 칸: 좁은 램프 열의 문구 잘림(B 시연: "OK"가 "O…")
            Func<int, List<string>> lintLamp = w => Lint(p =>
            {
                var g = new ApcGrid { Dock = DockStyle.Fill };
                var lamp = new ApcLampColumn { Width = w };
                g.Columns.Add(lamp); g.Columns.Add("id", "설비");
                g.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                g.Rows.Add(new ApcLampValue(StatusKind.Alarm, "알람"), "A-01");
                p.Controls.Add(g);
            });
            Check(lintLamp(56).Any(i => i.Contains("'알람' 말줄임")), "lint: clipped lamp value text");
            Check(!lintLamp(120).Any(i => i.Contains("말줄임")), "lint: fitting lamp value text passes");
            // 이력 샘플: 시각만 다르고 나머지가 같은 행이 되풀이되면 참고로 알린다(C 시연: 19행 중 7쌍).
            Func<string[][], List<string>> lintHistory = rows => Lint(p =>
            {
                var g = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
                g.Columns.Add("t", "시각"); g.Columns.Add("s", "센서"); g.Columns.Add("v", "값");
                foreach (var r in rows) g.Rows.Add(r);
                p.Controls.Add(g);
            });
            var repeated = new[] { new[] { "14:52", "A-06", "24.6" }, new[] { "14:41", "B-01", "38.9" }, new[] { "14:30", "A-02", "24.3" }, new[] { "14:20", "B-04", "42.5" },
                                   new[] { "13:10", "A-06", "24.6" }, new[] { "12:55", "B-01", "38.9" }, new[] { "12:31", "A-02", "24.3" }, new[] { "11:48", "B-04", "42.5" } };
            Check(lintHistory(repeated).Any(i => LayoutLint.IsNote(i) && i.Contains("되풀이")), "lint: repeated sample rows noted");
            var varied = new[] { new[] { "14:52", "A-06", "24.6" }, new[] { "14:41", "B-01", "38.9" }, new[] { "14:30", "A-06", "24.1" }, new[] { "14:20", "B-04", "42.5" }, new[] { "13:10", "A-02", "24.6" } };
            Check(!lintHistory(varied).Any(i => i.Contains("되풀이")), "lint: varied sample rows pass");
            Check(Lint(p =>
            {
                var g = new ApcGrid { Width = 440, Height = 200 };
                g.Columns.Add("t", "시각"); g.Columns.Add("id", "셀 ID"); g.Columns.Add("v", "판정");
                g.Columns[0].Width = 80; g.Columns[2].Width = 60;
                g.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                p.Controls.Add(g);
            }).Any(i => i.Contains("절반 넘게")), "lint: wide centered short-value column");
            Check(Lint(p =>
            {
                var g = new ApcGrid { Width = 440, Height = 200 };
                g.Columns.Add(new ApcLampColumn()); g.Columns.Add("id", "구역"); g.Columns.Add("st", "상태");
                foreach (DataGridViewColumn c in g.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                p.Controls.Add(g);
            }).Any(i => i.Contains("중복")), "lint: duplicate column header");
            using (var g = new ApcGrid())
                Check(g.DefaultCellStyle.Alignment == DataGridViewContentAlignment.MiddleCenter && g.ColumnHeadersDefaultCellStyle.Alignment == DataGridViewContentAlignment.MiddleCenter, "grid: cells and headers centered by default");
            Check(Lint(p => { p.Controls.Add(new ApcButton { Text = "저장", Width = 70 }); p.Controls.Add(new ApcButton { Text = "취소", Left = 80, Width = 70 }); }).Count == 0, "lint: clean row passes");
            CheckDialogLintCatches();
        }

        private static void CheckDialogLintCatches()
        {
            int h = ApcTheme.Px(ApcLayout.FieldHeight);
            Check(Lint(p => { p.Controls.Add(new ApcButton { Text = "A", Width = 60 }); p.Controls.Add(new Label { Text = "큰 글자", Font = new Font("Arial", 14f), AutoSize = false, Bounds = new Rectangle(0, 60, 200, 40) }); })
                .Any(i => i.Contains("테마와 다른 글꼴")), "lint: off-theme font");
            Check(Lint(p => { p.Controls.Add(Caption("모델명", 0, h, FontStyle.Bold)); p.Controls.Add(new ApcTextBox { Bounds = new Rectangle(110, 0, 150, h) }); })
                .Any(i => i.Contains("이름표가 굵게")), "lint: bold field caption");
            Check(Lint(p => { AddLintRow(p, 0, 120, h); AddLintRow(p, h + 12, 140, h); })
                .Any(i => i.Contains("왼쪽 끝 어긋남")), "lint: misaligned field left edges");
            Check(Lint(p => { AddLintRow(p, 0, 120, h); AddLintRow(p, h + 12, 120, h); AddLintRow(p, 2 * h + 32, 120, h); })
                .Any(i => i.Contains("행 간격 다름")), "lint: uneven row gaps");
            Check(LintDialog((f, cb) =>
            {
                int pad = ApcTheme.Px(16), bh = ApcTheme.Px(40), y = cb.Bottom - pad - bh;
                AddLintRow(f, y - ApcTheme.Px(16) - h, pad + 120, h);
                f.Controls.Add(new ApcButton { Text = "취소", Bounds = new Rectangle(cb.Right - pad - ApcTheme.Px(228), y, ApcTheme.Px(100), bh) });
                f.Controls.Add(new ApcButton { Text = "저장", Bounds = new Rectangle(cb.Right - pad - ApcTheme.Px(120), y, ApcTheme.Px(120), bh) });
            }).Any(i => i.Contains("하단 버튼 크기")), "lint: footer button size");
            Check(LintDialog((f, cb) =>
            {
                int pad = ApcTheme.Px(16), bw = ApcTheme.Px(110), bh = ApcTheme.Px(40), y = cb.Bottom - pad - bh;
                AddLintRow(f, cb.Top + pad, pad + 120, h);
                f.Controls.Add(new ApcButton { Text = "취소", Bounds = new Rectangle(cb.Right - pad - bw * 2 - ApcTheme.Px(8), y, bw, bh) });
                f.Controls.Add(new ApcButton { Text = "저장", Bounds = new Rectangle(cb.Right - pad - bw, y, bw, bh) });
            }).Any(i => i.Contains("빈 공간")), "lint: empty space above footer");
            using (var d = new FieldDialog())
            {
                ShowOffscreen(d);
                var issues = LayoutLint.Inspect(d);
                foreach (var i in issues) Console.WriteLine("      " + i);
                Check(issues.Count == 0, "lint: helper dialog is clean");
            }
            // 고객 프로젝트가 두 단계 이상 상속한 입력칸도 입력칸으로 본다
            Check(Lint(p => { p.Controls.Add(Caption("모델명", 0, h, FontStyle.Bold)); p.Controls.Add(new DeepTextBox { Bounds = new Rectangle(110, 0, 150, h) }); })
                .Any(i => i.Contains("이름표가 굵게")), "lint: deep ApcTextBox subclass is an input");
            // 큰 표시 글자(4-1: 화면에서 Font를 키우지 않음)는 걸리되, 큰 글자는 표시 컨트롤로 하라고 안내한다
            Check(Lint(p => { p.Controls.Add(new ApcButton { Text = "A", Left = 300, Width = 60 }); p.Controls.Add(new Label { Text = "12:00", Font = new Font(ApcTheme.Current.FontFamily, 24f), AutoSize = false, Bounds = new Rectangle(0, 0, 200, 50) }); })
                .Any(i => i.Contains("테마와 다른 글꼴") && i.Contains("ApcValueCard")), "lint: large display text points to display controls");
        }

        // "표 아래가 빔 … N행까지 들어감"의 N행을 넣으면 스크롤바도 참고도 없어야 한다(둘이 다른 수를 세면 에이전트가 맞출 수 없다).
        private static void CheckGridFitsMatchesScrollbar()
        {
            Func<int, List<string>> lintRows = n => Lint(p =>
            {
                var g = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
                g.Columns.Add("a", "A");
                for (int r = 0; r < n; r++) g.Rows.Add("x");
                p.Controls.Add(g);
            });
            var note = lintRows(1).FirstOrDefault(i => i.Contains("표 아래가 빔"));
            int fits = note == null ? 0 : int.Parse(System.Text.RegularExpressions.Regex.Match(note, @"(\d+)행까지").Groups[1].Value);
            var full = lintRows(fits);
            foreach (var i in full) Console.WriteLine("      " + i);
            Check(fits > 1 && full.Count == 0, "lint: grid rows suggested by note fit without scrollbar");
        }

        private class MidTextBox : ApcTextBox { }
        private sealed class DeepTextBox : MidTextBox { }

        private static Label Caption(string text, int y, int h, FontStyle style)
        {
            return new Label { Text = text, AutoSize = false, Font = ApcTheme.GetFont(style), Bounds = new Rectangle(0, y, 100, h) };
        }

        private static void AddLintRow(Control parent, int y, int fieldLeft, int h)
        {
            parent.Controls.Add(Caption("이름", y, h, FontStyle.Regular));
            parent.Controls.Add(new ApcTextBox { Bounds = new Rectangle(fieldLeft, y, 150, h) });
        }

        private static List<string> LintDialog(Action<ApcForm, Rectangle> build)
        {
            using (var f = new ApcForm { Resizable = false, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-32000, -32000), ClientSize = new Size(ApcTheme.Px(460), ApcTheme.Px(300)) })
            {
                build(f, f.ContentBounds);
                f.Show();
                Application.DoEvents();
                return LayoutLint.Inspect(f);
            }
        }

        private static List<string> Lint(Action<Panel> build)
        {
            using (var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-32000, -32000), Size = new Size(460, 300) })
            {
                var panel = new Panel { Dock = DockStyle.Fill };
                host.Controls.Add(panel);
                build(panel);
                host.Show();
                Application.DoEvents();
                return LayoutLint.Inspect(panel);
            }
        }
    }
}
