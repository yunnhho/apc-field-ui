using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 테마 스타일을 입힌 DataGridView. 헤더 그라데이션, 행 배경 한 색(번갈아 칠하지 않음), 행 전체 선택.
    /// 사용자가 표를 누르거나 키로 움직이기 전에는 첫 행이 저절로 선택되지 않는다(CurrentRow = null) —
    /// "선택한 행에 대해" 동작하는 버튼은 CurrentRow가 null이면 아무것도 하지 않게 둔다.
    /// </summary>
    public class ApcGrid : DataGridView
    {
        public ApcGrid()
        {
            DoubleBuffered = true;
            EnableHeadersVisualStyles = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            RowHeadersVisible = false;
            AllowUserToAddRows = false;
            AllowUserToResizeRows = false;
            BorderStyle = BorderStyle.FixedSingle;
            CellBorderStyle = DataGridViewCellBorderStyle.Single;   // 세로선도 — 열이 많은 현장 표에서 칸 경계가 보이게
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            // 현장 표 기준: 전부 가운데. 긴 글(알람 문구·비고) 열만 그 열에서 MiddleLeft로.
            DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            ApcTheme.Changed += OnThemeChanged;
            ApplyTheme();
        }

        /// <summary>머리줄 + 행 rows개가 빈 줄·스크롤바 없이 들어가는 표 높이(배율 적용 px). dialog의 AddContent 표 높이에 쓴다.</summary>
        public static int HeightForRows(int rows) { return HeightForRows(rows, false); }

        /// <summary>HeightForRows와 같고, 묶음 머리줄(AddColumnGroup)이 있는 표면 columnGroups = true(머리줄 2단).</summary>
        public static int HeightForRows(int rows, bool columnGroups)
        {
            return HeaderHeight() * (columnGroups ? 2 : 1) + ApcTheme.MinHeight * Math.Max(0, rows) + 2;
        }

        private readonly List<ColumnGroup> _groups = new List<ColumnGroup>();

        /// <summary>
        /// 묶음 머리줄: 열 firstColumn~lastColumn(열 번호, 포함) 위에 묶음 제목 한 칸("온도 (℃)" 위, 아래 칸에 "NI"·"SN").
        /// 처음 부르면 머리줄이 2단이 되고, 묶이지 않은 열 제목은 두 단에 걸쳐 가운데에 그린다. 열을 모두 추가한 뒤 부른다.
        /// </summary>
        public void AddColumnGroup(string text, int firstColumn, int lastColumn)
        {
            if (firstColumn < 0 || lastColumn < firstColumn || lastColumn >= Columns.Count)
                throw new ArgumentOutOfRangeException("lastColumn", "묶음 열 범위가 잘못됐습니다: " + firstColumn + "~" + lastColumn + " (열 " + Columns.Count + "개)");
            if (_groups.Any(g => firstColumn <= g.Last && lastColumn >= g.First))
                throw new ArgumentException("다른 묶음과 열이 겹칩니다: " + firstColumn + "~" + lastColumn, "firstColumn");
            _groups.Add(new ColumnGroup(text ?? string.Empty, firstColumn, lastColumn));
            ColumnHeadersHeight = HeaderHeight() * 2;
            Invalidate();
        }

        private ColumnGroup GroupOf(int column)
        {
            foreach (var g in _groups) if (column >= g.First && column <= g.Last) return g;
            return null;
        }

        private sealed class ColumnGroup
        {
            public ColumnGroup(string text, int first, int last) { Text = text; First = first; Last = last; }
            public string Text { get; private set; }
            public int First { get; private set; }
            public int Last { get; private set; }
        }

        private static int HeaderHeight() { return (int)Math.Round(ApcTheme.MinHeight * 1.15); }

        private void ApplyTheme()
        {
            RowTemplate.Height = ApcTheme.MinHeight;
            ColumnHeadersHeight = HeaderHeight() * (_groups.Count > 0 ? 2 : 1);
            if (ApcTheme.Current == null) return;

            var c = ApcTheme.Current.Colors;
            var text = ApcTheme.C(c.Text);
            Font = ApcTheme.GetFont();
            BackgroundColor = Color.White;
            GridColor = ApcTheme.C(c.Border);   // 칸 선은 테두리 색 그대로 — 연한 선은 현장 모니터에서 안 보인다
            DefaultCellStyle.ForeColor = text;
            DefaultCellStyle.SelectionBackColor = ApcTheme.C(c.GridSelected);
            DefaultCellStyle.SelectionForeColor = text;
            ColumnHeadersDefaultCellStyle.Font = ApcTheme.GetFont(FontStyle.Bold);
            ColumnHeadersDefaultCellStyle.ForeColor = text;
            ColumnHeadersDefaultCellStyle.SelectionBackColor = ApcTheme.C(c.GridHeader[1]);
            foreach (DataGridViewRow row in Rows) row.Height = RowTemplate.Height;
        }

        /// <summary>기본 폭 100px은 한글 폰트에서 코드값(WO-24117-01 등)이 잘린다.</summary>
        protected override void OnColumnAdded(DataGridViewColumnEventArgs e)
        {
            if (!(e.Column is ApcLampColumn) && e.Column.Width == 100)
                e.Column.Width = (int)Math.Round(130 * ApcTheme.Scale);
            base.OnColumnAdded(e);
        }

        /// <summary>
        /// DataGridView의 Fill 열 폭은 이전 폭에서 조금씩 고쳐 가는 방식이라, 화면에 뜨기 전 좁은 폭(100px대)에서 한 번 잡힌
        /// 오차가 남는다(첫 열만 좁았다가 조회 때마다 3px씩 늘어나던 문제). 크기가 바뀌면 FillWeight대로 다시 나눈다
        /// (사용자가 끌어서 바꾼 폭은 FillWeight에 남으므로 유지된다).
        /// </summary>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            foreach (DataGridViewColumn c in Columns)
            {
                if (c.AutoSizeMode != DataGridViewAutoSizeColumnMode.Fill) continue;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            base.OnCellPainting(e);
            if (e.Handled || e.RowIndex != -1 || ApcTheme.Current == null) return;

            var c = ApcTheme.Current.Colors;
            // 묶인 열은 아래 단에만 제목. 위 단(묶음 제목)은 OnPaint가 여러 열에 걸쳐 그린다.
            var text = e.CellBounds;
            if (e.ColumnIndex >= 0 && GroupOf(e.ColumnIndex) != null)
            {
                int half = text.Height / 2;
                text = new Rectangle(text.X, text.Y + half, text.Width, text.Height - half);
            }
            Gradients.Fill(e.Graphics, e.CellBounds, c.GridHeader);
            using (var pen = new Pen(ApcTheme.C(c.Border)))
            {
                // 표 머리줄 구분선은 위아래 끝까지 닿게. 끝이 떨어진 구분선은 설정창 같은 곳의 칸 제목 줄 전용이다.
                e.Graphics.DrawLine(pen, text.Right - 1, text.Top, text.Right - 1, text.Bottom - 1);
                e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            }
            var col = e.ColumnIndex >= 0 ? Columns[e.ColumnIndex] : null;
            if (col == null || col.HeaderCell.SortGlyphDirection != SortOrder.None)
                e.Paint(e.CellBounds, DataGridViewPaintParts.ContentForeground);   // 정렬 화살표는 기본 그리기에 맡김
            else
                DrawHeaderText(e.Graphics, text, Convert.ToString(e.FormattedValue), e.CellStyle.Font);
            e.Handled = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_groups.Count == 0 || ApcTheme.Current == null || !ColumnHeadersVisible) return;
            var c = ApcTheme.Current.Colors;
            foreach (var g in _groups)
            {
                // 화면에 보이는 열 머리만 합친다(가로 스크롤로 일부·전부 숨을 수 있음).
                var span = Rectangle.Empty;
                for (int col = g.First; col <= g.Last; col++)
                {
                    if (!Columns[col].Visible) continue;
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
                DrawHeaderText(e.Graphics, top, g.Text, ColumnHeadersDefaultCellStyle.Font);
            }
        }

        /// <summary>
        /// 머리줄 글자를 칸 정가운데에. 기본 그리기는 정렬 화살표 자리·안쪽 여백만큼 왼쪽으로 1~2px 치우친다.
        /// 오른쪽 구분선·아래 선 1px은 빼고 가운데를 잡는다. 묶음 머리줄 등 직접 그리는 곳도 이것을 쓴다.
        /// </summary>
        public static void DrawHeaderText(Graphics g, Rectangle cell, string text, Font font)
        {
            if (string.IsNullOrEmpty(text) || ApcTheme.Current == null) return;
            var inner = new Rectangle(cell.X, cell.Y, cell.Width - 1, cell.Height - 1);
            TextRenderer.DrawText(g, text, font, inner, ApcTheme.C(ApcTheme.Current.Colors.Text),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                | TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }

        // DataGridView는 화면에 뜰 때·행이 추가될 때 첫 행을 저절로 선택해 파란 줄이 생긴다(모니터링 표에서 '선택된 행'으로 오해).
        // 사용자가 표를 누르거나 키로 움직이기 전까지는 저절로 생긴 선택을 지운다. 앱이 코드로 고른 선택은 그 뒤 그대로 둔다.
        private bool _userSelected;

        private void ClearAutoSelection()
        {
            if (_userSelected || !IsHandleCreated) return;
            if (!ClearInitialSelection) { SkipReadOnlyStart(); return; }
            ClearSelection();
            if (CurrentCell != null && !IsCurrentCellInEditMode) CurrentCell = null;
        }

        // 입력 표의 시작 칸이 읽기 전용 열(구역·슬롯 이름)에 잡히면 같은 행의 첫 입력 칸으로 옮긴다.
        private void SkipReadOnlyStart()
        {
            var cell = CurrentCell;
            if (ReadOnly || cell == null || !Columns[cell.ColumnIndex].ReadOnly || IsCurrentCellInEditMode) return;
            var first = Columns.Cast<DataGridViewColumn>().Where(c => c.Visible && !c.ReadOnly).OrderBy(c => c.DisplayIndex).FirstOrDefault();
            if (first != null) CurrentCell = Rows[cell.RowIndex].Cells[first.Index];
        }

        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); ClearAutoSelection(); }
        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); ClearAutoSelection(); }
        protected override void OnEnter(EventArgs e) { base.OnEnter(e); ClearAutoSelection(); }   // 폼이 뜨며 첫 포커스를 줄 때 여기서 선택된다
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); ClearAutoSelection(); }
        protected override void OnRowsAdded(DataGridViewRowsAddedEventArgs e)
        {
            base.OnRowsAdded(e);
            // 현재 셀은 RowsAdded 이벤트 뒤에 잡히므로 처리 후로 미룬다.
            if (!_userSelected && IsHandleCreated) BeginInvoke((MethodInvoker)ClearAutoSelection);
        }
        protected override void OnMouseDown(MouseEventArgs e) { _userSelected = true; base.OnMouseDown(e); }
        protected override void OnKeyDown(KeyEventArgs e) { _userSelected = true; base.OnKeyDown(e); }

        /// <summary>
        /// true(기본)면 사용자가 누르기 전엔 선택 행이 없다. 입력 표(스캔으로 칸을 채우는 dialog 등)처럼
        /// 앱이 CurrentCell을 정해 두고 바로 입력받아야 하면 false.
        /// </summary>
        [System.ComponentModel.DefaultValue(true)]
        public bool ClearInitialSelection { get; set; } = true;

        /// <summary>편집 가능한 표에서 읽기 전용 열은 회색 바탕 — 어느 칸에 입력하는지 한눈에 보이게.</summary>
        protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
        {
            base.OnCellFormatting(e);
            if (ApcTheme.Current == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            // 셀에 상태색 글자(기준 이탈 강조)를 줬으면 선택 행에서도 그 색을 유지한다 — 선택 글자색에 묻히지 않게.
            if (e.CellStyle.ForeColor != DefaultCellStyle.ForeColor) e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
            if (ReadOnly) return;
            if (!Columns[e.ColumnIndex].ReadOnly) return;
            // 입력 중인 행의 읽기 전용 칸은 연한 파랑(입력칸 포커스 바탕) → 어느 행을 넣는지 멀리서도 보인다
            bool current = CurrentCell != null && CurrentCell.RowIndex == e.RowIndex && SelectionMode == DataGridViewSelectionMode.CellSelect;
            e.CellStyle.BackColor = current ? FieldFrame.FocusFill : ApcTheme.C(ApcTheme.Current.Colors.Background[0]);
            e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
        }

        private int _lastCurrentRow = -1;

        protected override void OnCurrentCellChanged(EventArgs e)
        {
            base.OnCurrentCellChanged(e);
            if (ReadOnly) return;
            int row = CurrentCell == null ? -1 : CurrentCell.RowIndex;
            if (row == _lastCurrentRow) return;
            if (_lastCurrentRow >= 0 && _lastCurrentRow < Rows.Count) InvalidateRow(_lastCurrentRow);
            if (row >= 0) InvalidateRow(row);
            _lastCurrentRow = row;
        }

        // 셀 글자(TextRenderer)는 10pt=13.33px를 14px로 올려 그리고, 입력 칸(네이티브 TextBox)은 13px로 내려 그려
        // 편집 중인 칸만 글자가 작고 위로 뜬다 → 입력 칸에는 같은 크기를 정수 px로 올린 글꼴을 준다.
        private Font _editFont;

        protected override void OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e)
        {
            base.OnEditingControlShowing(e);
            if (e.Control is TextBox) e.CellStyle.Font = EditFont(e.CellStyle.Font ?? Font);   // 편집 전용 스타일 사본(셀 표시엔 영향 없음)
        }

        private Font EditFont(Font cell)
        {
            float dpi;
            using (var g = CreateGraphics()) dpi = g.DpiY;
            float px = (float)Math.Ceiling(cell.SizeInPoints * dpi / 72f);
            if (_editFont == null || _editFont.Size != px || _editFont.Style != cell.Style || _editFont.Name != cell.Name)
            {
                if (_editFont != null) _editFont.Dispose();
                _editFont = new Font(cell.FontFamily, px, cell.Style, GraphicsUnit.Pixel);
            }
            return _editFont;
        }

        private void OnThemeChanged(object sender, EventArgs e) { ApplyTheme(); Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ApcTheme.Changed -= OnThemeChanged;
                if (_editFont != null) _editFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
