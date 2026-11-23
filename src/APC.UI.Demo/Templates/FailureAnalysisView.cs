using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>
    /// 연마기 불량 방지 시스템(references/FailureAnalysis.png → 현장 수정본) 재구성: 설비군 탭 + 시험 모드 안내 + 시계, 설정은 타이틀바 톱니(UseSettingsButton),
    /// 2단 헤더 그리드(B1/B2 묶음)가 화면 폭을 채움, 행 동작은 아이콘 열(ApcIconColumn: 할 수 없으면 회색), 상태는 램프 + 문구 한 칸(ApcLampValue).
    /// </summary>
    internal sealed class FailureAnalysisView : ApcTemplateView
    {
        private const int ColCurrent = 9, ColSend = 11, ColReset = 12, ColStatus = 13;
        private static readonly string[] Groups = { "저속 연마기 ST-GR-01~03", "저속 연마기 ST-GR-04~06", "고속 연마기 ST-GB-01~08" };

        private readonly GroupHeaderGrid _grid = new GroupHeaderGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly Label _clock = Caption("");
        private readonly Timer _clockTimer = new Timer { Interval = 1000 };
        private readonly ApcTabStrip _tabs = new ApcTabStrip();

        public FailureAnalysisView() : base("연마기 불량 방지 시스템")
        {
            var tabRow = Row();
            _tabs.SetTabs(Groups);   // 첫 탭이 선택된다. 표 내용은 아래 SelectGroup(0)에서
            _tabs.Width = _tabs.PreferredTabsWidth;
            _tabs.Margin = new Padding(0, S(46) - _tabs.Height, 0, 0);   // 탭 아래끝이 표 위끝에 닿게
            tabRow.Controls.Add(_tabs);
            var testMode = Caption("ⓘ 시험 모드 : 가상 장비로 동작합니다.");
            testMode.ForeColor = ApcTheme.Status(StatusKind.Info);
            testMode.Margin = new Padding(S(12), S(8), 0, 0);
            tabRow.Controls.Add(testMode);

            UseSettingsButton((s, e) => OpenSettings());   // 설정은 화면 안 버튼이 아니라 창 타이틀바(최소화 왼쪽) 톱니
            // 시계: 어두운 상자에 굵은 흰 글자(멀리서도 읽히게). 폭은 글자 기준 고정(초가 바뀌어도 흔들리지 않게)
            _clock.AutoSize = false;
            _clock.Size = new Size(S(200), S(ApcLayout.FieldHeight));
            _clock.TextAlign = ContentAlignment.MiddleCenter;
            _clock.Font = ApcTheme.GetFont(FontStyle.Bold);
            _clock.BackColor = ApcTheme.C(ApcTheme.Current.Colors.Header[1]);
            _clock.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.TextInverse);
            _clock.Margin = Padding.Empty;
            var right = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, BackColor = Color.Transparent };
            right.Controls.Add(_clock);
            var top = new Panel { Dock = DockStyle.Top, Height = S(46), BackColor = Color.Transparent };
            tabRow.Dock = DockStyle.Fill;
            top.Controls.Add(tabRow);
            top.Controls.Add(right);

            BuildColumns();
            _grid.CellClick += OnCellClick;

            Body.Controls.Add(_grid);
            Body.Controls.Add(top);

            _clockTimer.Tick += (s, e) => _clock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _clockTimer.Start();
            _clock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SelectGroup(0);
            _tabs.SelectedIndexChanged += (s, e) => SelectGroup(_tabs.SelectedIndex);
        }

        /// <summary>설정 진입: 관리자 확인 → 설정 dialog(샘플은 비밀번호 검사 생략).</summary>
        private void OpenSettings()
        {
            var owner = FindForm();
            using (var pw = new AdminPasswordDialog())
                if (pw.ShowDialog(owner) != DialogResult.OK) return;
            using (var dlg = new SettingsDialog()) dlg.ShowDialog(owner);
        }

        private void BuildColumns()
        {
            _grid.Columns.Add("no", "No");
            _grid.Columns.Add("name", "설비명칭");
            foreach (var b in new[] { "b1", "b2" })
            {
                _grid.Columns.Add(b + "spec", "SPEC");
                _grid.Columns.Add(b + "lot", "LOT No");
                _grid.Columns.Add(b + "barrel", "바렐");
            }
            _grid.Columns.Add("target", "운전목표시간");
            _grid.Columns.Add("current", "현재운전시간");
            _grid.Columns.Add("rpm", "RPM");
            _grid.Columns.Add(new ApcIconColumn { Name = "send", HeaderText = "전송", IconName = "icon_play" });
            _grid.Columns.Add(new ApcIconColumn { Name = "reset", HeaderText = "초기화", IconName = "icon_reset" });
            _grid.Columns.Add(new ApcLampColumn { Name = "status", HeaderText = "상태" });

            // 짧은 값 열은 고정 폭, 설비명·SPEC·LOT(긴 코드)가 남은 폭을 비율로 나눠 화면 폭을 채운다(오른쪽 빈 공간·코드 잘림 없음).
            int[] fixedW = { 50, 0, 0, 0, 56, 0, 0, 56, 130, 130, 80, 72, 72, 130 };
            float[] weights = { 0, 30, 26, 16, 0, 26, 16, 0, 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < fixedW.Length; i++)
            {
                var col = _grid.Columns[i];
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (fixedW[i] > 0) { col.Width = S(fixedW[i]); continue; }
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.FillWeight = weights[i];
            }
            _grid.RowTemplate.Height = S(34);   // 24행이 1080 화면에 스크롤 없이, 멀리서도 읽히는 높이
            foreach (var name in new[] { "target", "current", "rpm" })
                _grid.Columns[name].DefaultCellStyle.Font = ApcTheme.GetFont(FontStyle.Bold);   // 작업자가 보는 값 강조

            _grid.ColumnGroups.Add(new ColumnGroup("B1", 2, 4));
            _grid.ColumnGroups.Add(new ColumnGroup("B2", 5, 7));
        }

        private void SelectGroup(int index)
        {
            string prefix = index == 2 ? "고속 연마기 ST-GB-" : "저속 연마기 ST-GR-";
            int firstMachine = index == 1 ? 4 : 1;
            int machines = index == 2 ? 8 : 3;
            _grid.Rows.Clear();
            int no = 0;
            for (int m = 0; m < machines; m++)
                for (int col = 1; col <= 2; col++)
                    for (int stage = 1; stage <= 4; stage++)
                    {
                        no++;
                        string name = prefix + (firstMachine + m).ToString("00") + " (" + col + "열 " + stage + "단)";
                        _grid.Rows.Add(SampleRow(no, name));
                    }

            int alarms = 0, warns = 0, waiting = 0, offline = 0;
            foreach (DataGridViewRow r in _grid.Rows)
            {
                var v = (ApcLampValue)r.Cells[ColStatus].Value;
                if (v.Status == StatusKind.Alarm) { alarms++; Alarm.Push(r.Cells[1].Value + ": " + v.Text); }
                else if (v.Status == StatusKind.Warn) warns++;
                else if (v.Text == "대기") waiting++;
                else if (v.Text == "끊김") offline++;
            }
            Header.Status = alarms > 0 ? StatusKind.Alarm : warns > 0 ? StatusKind.Warn : offline + waiting > 0 ? StatusKind.Stop : StatusKind.Run;
            var parts = new List<string> { Groups[index] };
            if (alarms > 0) parts.Add("알람 " + alarms);
            if (warns > 0) parts.Add("경고 " + warns);
            if (waiting > 0) parts.Add("대기 " + waiting);
            if (offline > 0) parts.Add("끊김 " + offline);
            Header.StatusText = string.Join(" · ", parts);
        }

        /// <summary>앞쪽 몇 행만 가동 데이터, 나머지는 끊김 — 원본 캡처(전부 끊김)보다 상태 표현을 보여주기 위해.</summary>
        private static object[] SampleRow(int no, string name)
        {
            switch (no)
            {
                case 1: return Row(no, name, "Q3225X7S106K500ARLNN01F", "P052609404712", "2", "S3216X5R106K500NRINN02F", "M052610084612", "1", "02:00:00", "01:12:40", "370", StatusKind.Run, "가동");
                case 2: return Row(no, name, "Q2012X7R475K250NRENN03F", "P052610056015", "1", "", "", "", "01:30:00", "01:28:05", "370", StatusKind.Warn, "종료 임박");
                case 3: return Row(no, name, "S2012X5R226M250NRENN03F", "S052607407714", "1", "", "", "", "02:00:00", "00:41:12", "352", StatusKind.Alarm, "RPM 이탈");
                case 4: return Row(no, name, "", "", "", "", "", "", "", "", "", StatusKind.Stop, "대기");
                default: return Row(no, name, "", "", "", "", "", "", "", "", "", StatusKind.Stop, "끊김");
            }
        }

        private static object[] Row(int no, string name, string spec1, string lot1, string barrel1, string spec2, string lot2, string barrel2,
            string target, string current, string rpm, StatusKind status, string statusText)
        {
            // 전송·초기화: LOT가 들어간 행만 누를 수 있다(파란 아이콘), 빈 행·끊김은 회색
            bool hasLot = lot1.Length > 0 || lot2.Length > 0;
            return new object[] { no, name, spec1, lot1, barrel1, spec2, lot2, barrel2, target, current, rpm, hasLot, hasLot, new ApcLampValue(status, statusText) };
        }

        private void OnCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _grid.Rows[e.RowIndex];
            if (!ApcIconColumn.IsEnabled(row.Cells[e.ColumnIndex])) return;   // 회색 아이콘은 눌러도 아무 일 없음
            if (e.ColumnIndex == ColSend)
            {
                row.Cells[ColStatus].Value = new ApcLampValue(StatusKind.Run, "전송됨");
            }
            else if (e.ColumnIndex == ColReset && ApcDialog.Confirm(FindForm(), row.Cells[1].Value + "의 운전 시간을 초기화할까요?"))
            {
                row.Cells[ColCurrent].Value = "00:00:00";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _clockTimer.Dispose();
            base.Dispose(disposing);
        }

        internal sealed class ColumnGroup
        {
            public ColumnGroup(string text, int first, int last) { Text = text; First = first; Last = last; }
            public string Text { get; private set; }
            public int First { get; private set; }
            public int Last { get; private set; }
            public bool Contains(int col) { return col >= First && col <= Last; }
        }

        /// <summary>
        /// 2단 헤더(묶음 제목 + 하위 컬럼).
        /// ponytail: 데모 전용. 다른 현장 프로그램에서도 필요해지면 ApcGrid로 올린다.
        /// </summary>
        private sealed class GroupHeaderGrid : ApcGrid
        {
            public readonly List<ColumnGroup> ColumnGroups = new List<ColumnGroup>();

            public GroupHeaderGrid()
            {
                ColumnHeadersHeight = ApcTheme.MinHeight * 2;
            }

            private ColumnGroup GroupOf(int col)
            {
                foreach (var g in ColumnGroups) if (g.Contains(col)) return g;
                return null;
            }

            protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
            {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0 && GroupOf(e.ColumnIndex) != null)
                {
                    // 묶음 컬럼: 위 절반은 OnPaint에서 묶음 제목, 아래 절반에 하위 제목
                    var c = ApcTheme.Current.Colors;
                    Gradients.Fill(e.Graphics, e.CellBounds, c.GridHeader);
                    var half = e.CellBounds.Height / 2;
                    var sub = new Rectangle(e.CellBounds.X, e.CellBounds.Y + half, e.CellBounds.Width, e.CellBounds.Height - half);
                    using (var pen = new Pen(ApcTheme.C(c.Border)))
                    {
                        e.Graphics.DrawLine(pen, sub.Right - 1, sub.Top, sub.Right - 1, sub.Bottom - 1);   // 머리줄 구분선은 끝까지 닿게(ApcGrid와 같음)
                        e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                    }
                    ApcGrid.DrawHeaderText(e.Graphics, sub, Convert.ToString(e.Value), ColumnHeadersDefaultCellStyle.Font);
                    e.Handled = true;
                    return;
                }
                if (e.RowIndex == -1 && e.ColumnIndex >= 0) e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                base.OnCellPainting(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var c = ApcTheme.Current.Colors;
                foreach (var g in ColumnGroups)
                {
                    // 화면에 보이는 하위 컬럼 헤더만 합친다(가로 스크롤로 일부·전부 숨을 수 있음).
                    var span = Rectangle.Empty;
                    for (int col = g.First; col <= g.Last; col++)
                    {
                        var r = GetCellDisplayRectangle(col, -1, false);
                        if (r.Width > 0) span = span.IsEmpty ? r : Rectangle.Union(span, r);
                    }
                    if (span.IsEmpty) continue;
                    var top = new Rectangle(span.Left, span.Top, span.Width, ColumnHeadersHeight / 2);
                    Gradients.Fill(e.Graphics, top, c.GridHeader);
                    using (var pen = new Pen(ApcTheme.C(c.Border)))
                    {
                        e.Graphics.DrawLine(pen, top.Left, top.Bottom - 1, top.Right, top.Bottom - 1);
                        e.Graphics.DrawLine(pen, top.Right - 1, top.Top, top.Right - 1, top.Bottom);
                    }
                    ApcGrid.DrawHeaderText(e.Graphics, top, g.Text, ColumnHeadersDefaultCellStyle.Font);
                }
            }
        }
    }
}
