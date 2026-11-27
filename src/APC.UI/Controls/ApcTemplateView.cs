using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>Common frame of every function screen: a single Body. Program name, function name and current time are on the window title row (ApcForm).</summary>
    public class ApcTemplateView : UserControl
    {
        /// <summary>
        /// Content area (Padding 16px). Docking order: among controls with the same Dock (Top etc.), the one added later goes outside (on top).
        /// Add Fill first, then add Top rows from the bottom of the screen upward (or SetChildIndex(fill, 0)).
        /// </summary>
        protected readonly Panel Body = new Panel { Dock = DockStyle.Fill };

        protected ApcTemplateView(string title)
        {
            Dock = DockStyle.Fill;
            BackColor = ApcTheme.C(ApcTheme.Current.Colors.Background[0]);
            Font = ApcTheme.GetFont();   // 자식(헤더·카드·입력칸·이름표)이 어떤 창에 올라가도 테마 글꼴을 물려받게
            Text = title ?? "";
            Body.Padding = new Padding(S(ApcLayout.Pad));
            Controls.Add(Body);
        }

        private EventHandler _onSettings;
        private ApcForm _settingsHost;

        /// <summary>
        /// Puts a settings button in the title bar (left of minimize) of the window hosting this screen. Call once in the constructor, only for screens that have a settings screen.
        /// Turned on when the screen is attached to a window and off when the screen is disposed (does not linger in windows that swap screens, like the gallery).
        /// </summary>
        protected void UseSettingsButton(EventHandler onClick)
        {
            if (onClick == null) throw new ArgumentNullException("onClick");
            _onSettings = onClick;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_onSettings == null || _settingsHost != null) return;
            _settingsHost = FindForm() as ApcForm;
            if (_settingsHost == null) return;
            _settingsHost.ShowSettings = true;
            _settingsHost.SettingsClick += _onSettings;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _settingsHost != null)
            {
                _settingsHost.SettingsClick -= _onSettings;
                _settingsHost.ShowSettings = false;
                _settingsHost = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>Scaled px. Outside this class (ApcForm dialogs etc.) use ApcTheme.Px.</summary>
        protected static int S(int px) { return ApcTheme.Px(px); }

        protected static Label Caption(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = ApcTheme.GetFont(),   // 입력 행 이름표는 보통 굵기(굵게는 묶음·구획 제목만)
                ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text),
                Margin = new Padding(0, S(8), S(6), 0),
                BackColor = Color.Transparent,
            };
        }

        /// <summary>
        /// Section title + content filling below it (table or chart) as one cell. Put it directly in a Columns()/FillColumns() cell or Body's Fill
        /// (title Dock=Top, content Dock=Fill — screen code does not need to manage docking order).
        /// tools are buttons etc. placed at the right end of the title row (Button(...)). Change the title later with panel.Text = "...".
        /// </summary>
        protected static Panel Titled(string title, Control content, params Control[] tools)
        {
            if (content == null) throw new ArgumentNullException("content");
            var head = SectionTitle(title);
            head.AutoSize = false;
            head.Dock = DockStyle.Fill;
            head.TextAlign = ContentAlignment.MiddleLeft;
            bool hasTools = tools != null && tools.Length > 0;
            var bar = new Panel { Dock = DockStyle.Top, Height = TitleBarHeight(head, hasTools), BackColor = Color.Transparent };
            bar.Controls.Add(head);
            if (hasTools)
            {
                var right = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.RightToLeft, BackColor = Color.Transparent, Padding = Padding.Empty, Margin = Padding.Empty };
                for (int i = tools.Length - 1; i >= 0; i--)   // RightToLeft: 마지막 것이 오른쪽 끝
                {
                    tools[i].Dock = DockStyle.None;   // FlowLayoutPanel 안의 Dock은 "줄 높이로 늘이기"라 혼자 있는 범례가 높이 0으로 접힌다
                    tools[i].Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    tools[i].Margin = new Padding(i == tools.Length - 1 ? 0 : S(ApcLayout.ItemGap), 0, 0, 0);
                    right.Controls.Add(tools[i]);
                }
                bar.Height = Math.Max(bar.Height, tools.Max(t => t.Height));
                // 버튼보다 낮은 것(꺼진 버튼 이유 Caption 등)은 제목 줄 세로 가운데에
                foreach (var t in tools) t.Margin = new Padding(t.Margin.Left, Math.Max(0, (bar.Height - t.Height) / 2), 0, 0);
                bar.Controls.Add(right);
            }
            var panel = new TitledPanel(head) { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            content.Dock = DockStyle.Fill;
            panel.Controls.Add(content);   // Fill 먼저, Top은 나중에 Add한 것이 위
            panel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = S(ApcLayout.RowGap), BackColor = Color.Transparent });
            panel.Controls.Add(bar);
            return panel;
        }

        /// <summary>
        /// Total height of a fixed-height Titled() cell = title row + gap + contentHeight. For a table cell: Columns(TitledHeight(ApcGrid.HeightForRows(n)), ...).
        /// hasTools = true if the title row holds buttons (tools) (the title row is taller by the button height).
        /// </summary>
        protected static int TitledHeight(int contentHeight, bool hasTools = false)
        {
            using (var head = SectionTitle("가")) return TitleBarHeight(head, hasTools) + S(ApcLayout.RowGap) + contentHeight;
        }

        private static int TitleBarHeight(Label head, bool hasTools)
        {
            return hasTools ? Math.Max(head.PreferredHeight, S(ApcLayout.FieldHeight)) : head.PreferredHeight;
        }

        /// <summary>Titled() cell: Text is the title text (to change the title per scanned Lot).</summary>
        private sealed class TitledPanel : Panel
        {
            private readonly Label _head;
            public TitledPanel(Label head) { _head = head; }
            public override string Text { get { return _head.Text; } set { _head.Text = value; } }
        }

        /// <summary>Section title (bold, e.g. "최근 알람 이력"). Captions before input fields use Caption (regular weight).</summary>
        protected static Label SectionTitle(string text)
        {
            var l = new SectionLabel
            {
                Text = text,
                AutoSize = true,
                Font = ApcTheme.GetFont(FontStyle.Bold),
                ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text),
                Margin = new Padding(0, S(8), S(6), 0),
                Padding = new Padding(S(SectionLabel.BaseBar + SectionLabel.BaseGap), 0, 0, 0),
                BackColor = Color.Transparent,
            };
            return l;
        }

        /// <summary>Section title: 3px brand-color bar on the left + bold text (same look as the ApcGroup title).</summary>
        private sealed class SectionLabel : Label
        {
            internal const int BaseBar = 3, BaseGap = 7;

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (ApcTheme.Current == null) return;
                int barH = Math.Min(Height, TextRenderer.MeasureText("가", Font).Height);
                using (var mark = new SolidBrush(ApcTheme.C(ApcTheme.Current.Colors.Brand)))
                    e.Graphics.FillRectangle(mark, 0, (Height - barH) / 2, S(BaseBar), barH);
            }
        }

        protected static ApcButton Button(string text, ApcButtonVariant variant, int width = 110)
        {
            return new ApcButton { Text = text, Variant = variant, Width = S(width), Height = S(ApcLayout.FieldHeight), Margin = new Padding(0, 0, S(ApcLayout.ItemGap), 0) };
        }

        /// <summary>
        /// Cards, tiles and judge panels side by side in one row (Dock=Top). Without percents all cells have equal width. Cell gap 12px (0 after the last), 12px below the row.
        /// height is the cell height (scaled, e.g. S(150)).
        /// </summary>
        protected static TableLayoutPanel Columns(int height, Control[] cells, float[] percents = null)
        {
            if (cells == null || cells.Length == 0) throw new ArgumentException("cells가 비었습니다.", "cells");
            if (percents != null && percents.Length != cells.Length) throw new ArgumentException("percents 개수가 cells와 다릅니다.", "percents");
            var t = new TableLayoutPanel { Dock = DockStyle.Top, Height = height + S(ApcLayout.RowGap), ColumnCount = cells.Length, RowCount = 1, Padding = new Padding(0, 0, 0, S(ApcLayout.RowGap)), BackColor = Color.Transparent };
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            for (int i = 0; i < cells.Length; i++)
            {
                t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, percents == null ? 100f / cells.Length : percents[i]));
                cells[i].Dock = DockStyle.Fill;
                cells[i].Margin = new Padding(0, 0, i == cells.Length - 1 ? 0 : S(ApcLayout.RowGap), 0);
                t.Controls.Add(cells[i], i, 0);
            }
            return t;
        }

        /// <summary>
        /// Side-by-side cells that fill all remaining height (Dock=Fill) — for two list tables next to each other (cells are usually Titled(...)).
        /// It is Fill in Body, so add it before the Top rows.
        /// </summary>
        protected static TableLayoutPanel FillColumns(Control[] cells, float[] percents = null)
        {
            var t = Columns(0, cells, percents);
            t.Dock = DockStyle.Fill;
            t.Padding = Padding.Empty;
            return t;
        }

        /// <summary>A row stacked left to right (filter and button rows).</summary>
        protected static FlowLayoutPanel Row(params Control[] controls)
        {
            var row = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 0, 0, S(ApcLayout.RowGap)),
                BackColor = Color.Transparent,
            };
            // 입력칸(기본 Margin 3)과 버튼(Margin 0)이 섞이면 위아래가 어긋난다 → 입력·버튼은 위 0, 오른쪽 간격만 둔다. 이름표는 자기 Margin(글자 높이 맞춤) 유지.
            foreach (var c in controls)
                if (!(c is Label)) c.Margin = new Padding(0, 0, Math.Max(c.Margin.Right, S(ApcLayout.ItemGap)), 0);
            row.Controls.AddRange(controls);
            return row;
        }
    }
}
