using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>모든 기능 화면의 공통 틀: Body 하나. 프로그램 이름·기능명·현재 시각은 창 제목 줄(ApcForm)에 있다.</summary>
    public class ApcTemplateView : UserControl
    {
        /// <summary>
        /// 내용 영역(Padding 16px). 도킹 순서: 같은 Dock(Top 등)끼리는 나중에 Add한 것이 바깥(위)에 온다.
        /// Fill을 먼저 Add하고, Top 줄들은 화면 아래쪽 것부터 위쪽 것 순서로 Add한다(또는 SetChildIndex(fill, 0)).
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
        /// 이 화면이 떠 있는 창의 타이틀바(최소화 왼쪽)에 설정 버튼을 둔다. 설정 화면이 있는 화면만 생성자에서 한 번 부른다.
        /// 화면이 창에 붙을 때 켜고, 화면을 Dispose하면 끈다(갤러리처럼 화면을 바꿔 끼우는 창에서도 남지 않는다).
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

        /// <summary>배율 적용 px. ApcForm dialog 등 이 클래스 밖에서는 ApcTheme.Px.</summary>
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
        /// 구획 제목 + 그 아래를 채우는 내용(표·그래프)을 한 칸으로. Columns()의 칸이나 Body의 Fill에 그대로 넣는다
        /// (제목 Dock=Top, 내용 Dock=Fill — 도킹 순서를 화면 코드가 맞추지 않아도 된다).
        /// </summary>
        protected static Panel Titled(string title, Control content)
        {
            if (content == null) throw new ArgumentNullException("content");
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var head = SectionTitle(title);
            head.AutoSize = false;
            head.Dock = DockStyle.Top;
            head.Height = head.PreferredHeight;
            head.TextAlign = ContentAlignment.MiddleLeft;
            content.Dock = DockStyle.Fill;
            panel.Controls.Add(content);   // Fill 먼저, Top은 나중에 Add한 것이 위
            panel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = S(ApcLayout.RowGap), BackColor = Color.Transparent });
            panel.Controls.Add(head);
            return panel;
        }

        /// <summary>구획 제목(굵게, 예: "최근 알람 이력"). 입력칸 앞 이름표는 Caption(보통 굵기).</summary>
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

        /// <summary>구획 제목: 왼쪽 3px 브랜드색 막대 + 굵은 글자(ApcGroup 제목과 같은 표시).</summary>
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
        /// 카드·타일·판정판을 한 줄에 나란히(Dock=Top). percents가 없으면 같은 폭. 칸 간격 12px(마지막 칸 0), 줄 아래 간격 12px.
        /// height는 칸 높이(배율 적용한 값, 예: S(150)).
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

        /// <summary>왼쪽→오른쪽으로 쌓는 한 줄(필터·버튼 줄).</summary>
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
