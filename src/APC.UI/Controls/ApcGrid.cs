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
    /// DataGridView with the theme style. Gradient header, one row background color (no alternating), full-row selection.
    /// No row is selected automatically until the user clicks the table or moves with the keys (CurrentRow = null) —
    /// buttons that act "on the selected row" should do nothing while CurrentRow is null.
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

        /// <summary>Table height that fits the header + rows rows with no empty space or scrollbar (scaled px). Use it for table heights in dialog AddContent.</summary>
        public static int HeightForRows(int rows) { return HeightForRows(rows, false); }

        /// <summary>Same as HeightForRows; columnGroups = true for a table with a group header (AddColumnGroup, two header tiers).</summary>
        public static int HeightForRows(int rows, bool columnGroups)
        {
            return HeaderHeight() * (columnGroups ? 2 : 1) + ApcTheme.MinHeight * Math.Max(0, rows) + 2;
        }

        private readonly List<ColumnGroup> _groups = new List<ColumnGroup>();

        /// <summary>
        /// Group header: one group caption cell above columns firstColumn~lastColumn (column indexes, inclusive) (e.g. "온도 (℃)" on top, "NI"·"SN" below).
        /// The first call makes the header two tiers; ungrouped column captions are drawn centered across both tiers. Call after all columns are added.
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
            FitRowHeights();
        }

        private bool _fillRows;

        /// <summary>
        /// true for a fixed-row-count table (10 tightening points, 5 floors) filling a Fill cell: the rows share the table height (never lower than the default row height).
        /// Tables whose rows grow, such as history and lists, keep false (fill them with sample rows).
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool FillRows
        {
            get { return _fillRows; }
            set { _fillRows = value; if (value) FitRowHeights(); else foreach (DataGridViewRow row in Rows) row.Height = RowTemplate.Height; }
        }

        private void FitRowHeights()
        {
            if (!_fillRows || Rows.Count == 0) return;
            int border = BorderStyle == BorderStyle.FixedSingle ? 2 : BorderStyle == BorderStyle.Fixed3D ? 4 : 0;
            int body = ClientSize.Height - border - (ColumnHeadersVisible ? ColumnHeadersHeight : 0);
            int h = Math.Max(RowTemplate.Height, body / Rows.Count);
            foreach (DataGridViewRow row in Rows) if (row.Height != h) row.Height = h;
        }

        /// <summary>The default width of 100px clips code values (WO-24117-01 etc.) in Korean fonts.</summary>
        protected override void OnColumnAdded(DataGridViewColumnEventArgs e)
        {
            if (!(e.Column is ApcLampColumn) && e.Column.Width == 100)
                e.Column.Width = (int)Math.Round(130 * ApcTheme.Scale);
            base.OnColumnAdded(e);
        }

        /// <summary>
        /// DataGridView adjusts Fill column widths incrementally from the previous width, so an error caught once at a narrow width (around 100px) before showing
        /// persists (the first column stayed narrow and grew 3px on every query). On resize the widths are redistributed by FillWeight
        /// (widths the user dragged are kept because they live in FillWeight).
        /// </summary>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            FitRowHeights();
            foreach (DataGridViewColumn c in Columns)
            {
                if (c.AutoSizeMode != DataGridViewAutoSizeColumnMode.Fill) continue;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        private static int InkLine { get { return Math.Max(2, ApcTheme.Px(2)); } }

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
            }
            // 표 머리 아래 2px 먹색 선(Steel + Ink): 머리와 본문을 분명히 가른다.
            using (var ink = new SolidBrush(ApcTheme.C(c.Text)))
                e.Graphics.FillRectangle(ink, e.CellBounds.Left, e.CellBounds.Bottom - InkLine, e.CellBounds.Width, InkLine);
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
        /// Header text exactly centered in the cell. Default painting shifts it 1–2px left by the sort-arrow slot and inner padding.
        /// Centers excluding the 1px right divider and bottom line. Custom-drawn parts such as the group header use this too.
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
            FitRowHeights();
            for (int r = e.RowIndex; r < e.RowIndex + e.RowCount && r < Rows.Count; r++) FitLampText(r, -1);
            // 현재 셀은 RowsAdded 이벤트 뒤에 잡히므로 처리 후로 미룬다.
            if (!_userSelected && IsHandleCreated) BeginInvoke((MethodInvoker)ClearAutoSelection);
        }
        protected override void OnRowsRemoved(DataGridViewRowsRemovedEventArgs e) { base.OnRowsRemoved(e); FitRowHeights(); }
        protected override void OnCellValueChanged(DataGridViewCellEventArgs e)
        {
            base.OnCellValueChanged(e);
            if (e.RowIndex >= 0) FitLampText(e.RowIndex, e.ColumnIndex);
        }

        // 램프 + 문구 칸(ApcLampValue)은 문구가 다 보이게 열 최소 폭을 넓힌다. column < 0이면 그 행의 램프 열 전부.
        private void FitLampText(int row, int column)
        {
            if (ApcTheme.Current == null || Rows[row].IsNewRow) return;
            foreach (DataGridViewColumn c in Columns)
            {
                if (!(c is ApcLampColumn) || (column >= 0 && c.Index != column)) continue;
                var v = Rows[row].Cells[c.Index].Value as ApcLampValue;
                if (v == null) continue;
                int need = ApcLampColumn.WidthFor(v.Text, c.InheritedStyle.Font ?? Font);
                if (need > c.MinimumWidth) c.MinimumWidth = need;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e) { _userSelected = true; base.OnMouseDown(e); }
        protected override void OnKeyDown(KeyEventArgs e) { _userSelected = true; base.OnKeyDown(e); }

        /// <summary>
        /// true (default): no row is selected until the user clicks. For input tables (e.g. a dialog whose cells are filled by scanning)
        /// where the app sets CurrentCell and takes input immediately, set false.
        /// </summary>
        [System.ComponentModel.DefaultValue(true)]
        public bool ClearInitialSelection { get; set; } = true;

        /// <summary>In an editable table, read-only columns get a gray background — so it is obvious which cells take input.</summary>
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
