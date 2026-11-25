using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// dialog 배치 헬퍼. 생성자에서 SetupDialog → AddFieldRow/AddGroup/AddContent/AddContentFill(위에서 아래 순서) → SetFooter.
    /// 치수는 ApcLayout이 정하고, 표시 직전(OnLoad) 한 번에 배치한다: 이름표 열 하나(가장 긴 이름표 폭), 입력칸 왼쪽 끝 통일(AddCaptionRow 표 묶음은 따로),
    /// 하단 [확정][취소] 오른쪽(취소가 끝), 높이는 내용으로 계산(Fill 내용이 있으면 SetupDialog 높이).
    /// </summary>
    public partial class ApcForm
    {
        private enum BlockKind { Row, Group, Content, Fill, Title, Cells, Captions }

        private sealed class Block
        {
            public BlockKind Kind;
            public Label Caption;
            public Control Field;
            public Control[] Trailing;
            public ApcGroup Group;
            public readonly List<Block> Rows = new List<Block>();
            public Control Content;
            public Control[] Cells;
        }

        private readonly List<Block> _blocks = new List<Block>();
        private int _dialogWidth, _dialogHeight;
        private bool _laidOut;
        private ApcButton _footerCancel, _footerConfirm;
        private ApcButton[] _footerLeft = new ApcButton[0];
        private Label _footerNote;

        /// <summary>dialog 기본 속성과 크기(배율 전 px). height 0 = 내용으로 자동(AddContentFill을 쓰면 필수).</summary>
        protected void SetupDialog(string title, int width, int height = 0)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException("width");
            RequireNotShown();
            if (height <= 0 && _blocks.Any(b => b.Kind == BlockKind.Fill))
                throw new InvalidOperationException("AddContentFill을 쓴 dialog는 SetupDialog에 높이를 줘야 합니다.");
            TitleText = title;
            Resizable = false;
            MinimizeBox = false;
            ShowLogo = false;   // dialog: 로고·최소화 없이 제목과 닫기만
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            _dialogWidth = width;
            _dialogHeight = height;
            ClientSize = new Size(ApcTheme.Px(width), height > 0 ? ApcTheme.Px(height) : ClientSize.Height);
        }

        /// <summary>이름표 + 입력칸 한 줄. trailing이 없으면 입력칸이 남은 폭을 채우고, 있으면 모두 자기 Width로 왼→오.</summary>
        protected void AddFieldRow(string caption, Control field, params Control[] trailing)
        {
            _blocks.Add(NewRow(caption, field, trailing, this));
        }

        /// <summary>묶음 안 한 줄. 이름표 열은 묶음 밖 행과 같은 위치에 맞춘다.</summary>
        protected void AddFieldRow(ApcGroup group, string caption, Control field, params Control[] trailing)
        {
            if (group == null) throw new ArgumentNullException("group");
            var block = _blocks.FirstOrDefault(b => b.Group == group);
            if (block == null) throw new InvalidOperationException("AddGroup으로 만든 묶음이 아닙니다.");
            block.Rows.Add(NewRow(caption, field, trailing, group));
        }

        /// <summary>
        /// 묶음 안 열 제목 줄. 바로 다음 AddFieldRow(group, ...) 행의 [이름표, 입력칸, trailing...] 위에 순서대로 맞춰 놓인다("" = 그 칸 제목 없음).
        /// 여러 행이 같은 칸(포트·신호·로직…)을 가질 때 행마다 FieldCaption을 되풀이하지 않고 위에 한 번만 쓴다.
        /// </summary>
        protected void AddCaptionRow(ApcGroup group, params string[] captions)
        {
            if (group == null) throw new ArgumentNullException("group");
            RequireSetup();
            var block = _blocks.FirstOrDefault(b => b.Group == group);
            if (block == null) throw new InvalidOperationException("AddGroup으로 만든 묶음이 아닙니다.");
            var labels = (captions ?? new string[0]).Select(t => new Label
            {
                Text = t ?? "", AutoSize = false, TextAlign = ContentAlignment.BottomLeft, BackColor = Color.Transparent,
                Font = ApcTheme.GetFont(), ForeColor = ApcTheme.C(ApcTheme.Current.Colors.TextMuted),
            }).ToArray();
            foreach (var l in labels) group.Controls.Add(l);
            block.Rows.Add(new Block { Kind = BlockKind.Captions, Cells = labels });
        }

        private static int CaptionRowHeight { get { return ApcTheme.Px(20); } }
        private static int CaptionRowGap { get { return ApcTheme.Px(4); } }

        /// <summary>제목 있는 묶음(ApcGroup). 행은 AddFieldRow(group, ...)로 넣는다.</summary>
        protected ApcGroup AddGroup(string title)
        {
            RequireSetup();
            var g = new ApcGroup { Text = title };
            Controls.Add(g);
            _blocks.Add(new Block { Kind = BlockKind.Group, Group = g });
            return g;
        }

        /// <summary>표·판정판 등 자유 요소. 높이는 c.Height(ApcTheme.Px로 지정), 폭은 내용 폭.</summary>
        protected void AddContent(Control c)
        {
            RequireSetup();
            c.Dock = DockStyle.None;   // ApcHeaderBar처럼 기본 Dock이 있는 컨트롤도 헬퍼가 둔 자리에
            Controls.Add(c);
            _blocks.Add(new Block { Kind = c is Label && _sectionTitles.Contains(c) ? BlockKind.Title : BlockKind.Content, Content = c });
        }

        /// <summary>카드 여러 개를 한 줄에 같은 폭·같은 높이(가장 큰 c.Height)로. 칸 간격은 ApcLayout.ItemGap. 2×2는 두 번 부른다.</summary>
        protected void AddContentRow(params Control[] cells)
        {
            RequireSetup();
            if (cells == null || cells.Length == 0) throw new ArgumentException("칸이 하나 이상 있어야 합니다.", "cells");
            foreach (var c in cells) { c.Dock = DockStyle.None; c.Margin = Padding.Empty; Controls.Add(c); }
            _blocks.Add(new Block { Kind = BlockKind.Cells, Cells = cells });
        }

        /// <summary>남은 높이를 채우는 요소(그래프 등). SetupDialog에 높이를 줘야 하고 한 dialog에 하나만.</summary>
        protected void AddContentFill(Control c)
        {
            RequireSetup();
            if (_dialogHeight <= 0) throw new InvalidOperationException("AddContentFill은 SetupDialog에 높이를 줘야 합니다.");
            if (_blocks.Any(b => b.Kind == BlockKind.Fill)) throw new InvalidOperationException("AddContentFill은 한 번만 쓸 수 있습니다.");
            c.Dock = DockStyle.None;
            Controls.Add(c);
            _blocks.Add(new Block { Kind = BlockKind.Fill, Content = c });
        }

        /// <summary>하단 버튼 줄: 오른쪽 [확정][취소](취소가 끝, cancel은 null 가능), 왼쪽 보조 버튼. Esc = 취소(없으면 확정).</summary>
        protected void SetFooter(ApcButton cancel, ApcButton confirm, params ApcButton[] left)
        {
            RequireSetup();
            if (confirm == null) throw new ArgumentNullException("confirm");
            _footerCancel = cancel;
            _footerConfirm = confirm;
            _footerLeft = left ?? new ApcButton[0];
            foreach (var b in _footerLeft.Concat(new[] { cancel, confirm }).Where(b => b != null)) Controls.Add(b);
            // CancelButton으로 지정하면 WinForms가 DialogResult 없는 버튼에 Cancel을 넣어 Click 처리 전에 닫힌다
            // → 확정 버튼은 DialogResult가 이미 있을 때(보기용 닫기 = OK)만 Esc에 묶는다.
            if (cancel != null) CancelButton = cancel;
            else if (confirm.DialogResult != DialogResult.None) CancelButton = confirm;
        }

        /// <summary>
        /// 하단 버튼 줄의 [확정] 바로 왼쪽 안내 글(꺼진 확정 버튼의 이유: "원인 확인 전 · Release 불가").
        /// 반환 Label의 Text를 바꿔 쓴다(이유가 없으면 ""). 글자색 기본, 알람인 이유만 ForeColor = ApcTheme.StatusText(StatusKind.Alarm).
        /// </summary>
        protected Label SetFooterNote(string text = "")
        {
            RequireSetup();
            if (_footerNote == null)
            {
                _footerNote = new Label
                {
                    AutoSize = false, TextAlign = ContentAlignment.MiddleRight, AutoEllipsis = false, BackColor = Color.Transparent,
                    Font = ApcTheme.GetFont(), ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text),
                };
                Controls.Add(_footerNote);
            }
            _footerNote.Text = text ?? string.Empty;
            return _footerNote;
        }

        private const int SectionBar = 3, SectionBarGap = 7;   // 화면 SectionTitle·ApcGroup 제목 막대와 같은 치수
        private readonly HashSet<Control> _sectionTitles = new HashSet<Control>();

        /// <summary>구획 제목(왼쪽 브랜드색 막대 + 굵게 + 아래 폭 전체 구분선 — 묶음·화면 구획 제목과 같은 표시). AddContent로 넣으면 아래 요소와의 간격이 좁다. 상자(AddGroup) 없이 입력 행을 나눌 때 쓴다.</summary>
        protected Label SectionTitle(string text)
        {
            var l = new Label
            {
                Text = text, AutoSize = false, Height = ApcTheme.Px(32), TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent,
                Font = ApcTheme.GetFont(FontStyle.Bold), ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text),
                Padding = new Padding(ApcTheme.Px(SectionBar + SectionBarGap), 0, 0, 0),
            };
            l.Paint += (s, e) =>
            {
                int barH = Math.Min(l.Height, TextRenderer.MeasureText("가", l.Font).Height);
                using (var mark = new SolidBrush(ApcTheme.C(ApcTheme.Current.Colors.Brand)))
                    e.Graphics.FillRectangle(mark, 0, (l.Height - 2 - barH) / 2, ApcTheme.Px(SectionBar), barH);
                // 진한 선 + 아래 흰 선 = 파인 홈(입체 구분선)
                using (var pen = new Pen(ApcTheme.C(ApcTheme.Current.Colors.Border))) e.Graphics.DrawLine(pen, 0, l.Height - 2, l.Width, l.Height - 2);
                e.Graphics.DrawLine(Pens.White, 0, l.Height - 1, l.Width, l.Height - 1);
            };
            _sectionTitles.Add(l);
            return l;
        }

        /// <summary>행 안 trailing 이름표(보통 굵기, 글자 폭).</summary>
        protected static Label FieldCaption(string text)
        {
            var font = ApcTheme.GetFont();
            var label = new Label
            {
                Text = text, AutoSize = false, Width = TextRenderer.MeasureText(text, font).Width + ApcTheme.Px(4),
                TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent,
                Font = font, ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text),
            };
            // 빈 글자로 만들고 나중에 채우는 칸(뒤쪽 안내 등)도 잘리지 않게 글자 폭을 따라간다
            label.TextChanged += (s, e) => label.Width = TextRenderer.MeasureText(label.Text, label.Font).Width + ApcTheme.Px(4);
            return label;
        }

        protected override void OnLoad(EventArgs e)
        {
            if (_dialogWidth > 0) LayoutDialog();   // 가운데 배치(base.OnLoad) 전에 크기를 정한다
            _laidOut = _dialogWidth > 0;
            base.OnLoad(e);
        }

        private void RequireSetup()
        {
            if (_dialogWidth <= 0) throw new InvalidOperationException("SetupDialog를 먼저 호출해야 합니다.");
            RequireNotShown();
        }

        // 배치는 표시 직전(OnLoad) 한 번이라 그 뒤 헬퍼 호출은 반영되지 않는다 → 생성자에서만
        private void RequireNotShown()
        {
            if (_laidOut) throw new InvalidOperationException("dialog 헬퍼는 생성자에서만 부를 수 있습니다(이미 배치됨).");
        }

        private Block NewRow(string caption, Control field, Control[] trailing, Control parent)
        {
            RequireSetup();
            if (field == null) throw new ArgumentNullException("field");
            var label = new Label
            {
                Text = caption, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Transparent,
                Font = ApcTheme.GetFont(), ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text),
            };
            parent.Controls.Add(label);
            parent.Controls.Add(field);
            foreach (var t in trailing ?? new Control[0]) parent.Controls.Add(t);
            return new Block { Kind = BlockKind.Row, Caption = label, Field = field, Trailing = trailing ?? new Control[0] };
        }

        private void LayoutDialog()
        {
            int pad = ApcTheme.Px(ApcLayout.Pad), fieldH = ApcTheme.Px(ApcLayout.FieldHeight);
            int btnW = ApcTheme.Px(ApcLayout.FooterButtonWidth), btnH = ApcTheme.Px(ApcLayout.FooterButtonHeight);
            bool hasFooter = _footerConfirm != null;

            // 이름표 열: 묶음 안 이름표는 묶음 안쪽 여백만큼 좁게 → 입력칸 왼쪽 끝이 dialog 전체에서 같다.
            // 열 제목 줄이 있는 묶음(표처럼 쓰는 묶음)은 제 이름표 열만 쓴다 — 긴 행 이름(블럭명 등)이 다른 묶음의 이름표·입력칸을 벌리지 않게.
            int labelW = 0;
            foreach (var b in _blocks)
            {
                if (b.Kind == BlockKind.Row) labelW = Math.Max(labelW, TextW(b.Caption));
                if (b.Kind == BlockKind.Group && !IsTableGroup(b)) labelW = Math.Max(labelW, GroupLabelW(b));
            }

            int fixedH = 0;
            for (int i = 0; i < _blocks.Count; i++) fixedH += Gap(i) + BlockHeight(_blocks[i], fieldH);
            int footerSpace = hasFooter ? (_blocks.Count > 0 ? ApcTheme.Px(ApcLayout.GroupGap) : 0) + btnH : 0;
            if (_dialogHeight <= 0 && (_blocks.Count > 0 || hasFooter))   // 헬퍼 없이 직접 배치한 dialog는 높이를 건드리지 않는다
                ClientSize = new Size(ClientSize.Width, Padding.Top + pad + fixedH + footerSpace + pad + Padding.Bottom);

            var cb = ContentBounds;
            int x = cb.Left + pad, w = cb.Width - 2 * pad, y = cb.Top + pad;
            int fillH = Math.Max(0, cb.Height - 2 * pad - fixedH - footerSpace);
            for (int i = 0; i < _blocks.Count; i++)
            {
                var b = _blocks[i];
                y += Gap(i);
                int h = b.Kind == BlockKind.Fill ? fillH : BlockHeight(b, fieldH);
                switch (b.Kind)
                {
                    case BlockKind.Row:
                        PlaceRow(b, x, y, x + w, labelW, fieldH);
                        break;
                    case BlockKind.Group:
                        b.Group.SetBounds(x, y, w, h);
                        var gp = b.Group.Padding;
                        int gLabelW = IsTableGroup(b) ? GroupLabelW(b) : labelW;
                        int ry = gp.Top;
                        for (int k = 0; k < b.Rows.Count; k++)
                        {
                            var r = b.Rows[k];
                            if (r.Kind == BlockKind.Captions)
                            {
                                if (k + 1 >= b.Rows.Count || b.Rows[k + 1].Kind != BlockKind.Row)
                                    throw new InvalidOperationException("AddCaptionRow 다음에는 같은 묶음의 AddFieldRow가 와야 합니다.");
                                PlaceCaptions(r, b.Rows[k + 1], gp.Left, w - gp.Right, gLabelW - gp.Left, fieldH, ry);
                                ry += CaptionRowHeight + CaptionRowGap;
                                continue;
                            }
                            PlaceRow(r, gp.Left, ry, w - gp.Right, gLabelW - gp.Left, fieldH);
                            ry += fieldH + ApcTheme.Px(ApcLayout.RowGap);
                        }
                        break;
                    case BlockKind.Cells:
                        int cg = ApcTheme.Px(ApcLayout.ItemGap), n = b.Cells.Length;
                        int cw = (w - cg * (n - 1)) / n;
                        for (int k = 0; k < n; k++)   // 마지막 칸이 나머지 px를 가져 오른쪽 끝이 맞는다
                            b.Cells[k].SetBounds(x + k * (cw + cg), y, k == n - 1 ? w - k * (cw + cg) : cw, h);
                        break;
                    default:
                        b.Content.SetBounds(x, y, w, h);
                        break;
                }
                y += h;
            }

            if (!hasFooter) return;
            int by = cb.Bottom - pad - btnH, gap = ApcTheme.Px(ApcLayout.ItemGap);
            // [확정][취소]: 취소가 오른쪽 끝
            int right;
            if (_footerCancel != null) { int cw = FooterW(_footerCancel, btnW); _footerCancel.SetBounds(cb.Right - pad - cw, by, cw, btnH); right = _footerCancel.Left - gap; }
            else right = cb.Right - pad;
            int fw = FooterW(_footerConfirm, btnW);
            _footerConfirm.SetBounds(right - fw, by, fw, btnH);
            int lx = x;
            foreach (var l in _footerLeft)
            {
                int lw = FooterW(l, btnW);
                l.SetBounds(lx, by, lw, btnH);
                lx += lw + gap;
            }
            if (_footerNote != null) _footerNote.SetBounds(lx, by, Math.Max(0, _footerConfirm.Left - gap - lx), btnH);
        }

        /// <summary>바닥 버튼 폭: 기본 110, 글자가 길면(비밀번호 변경 등, 글꼴 12) 글자 + 양쪽 여백만큼 넓힌다.</summary>
        private static int FooterW(ApcButton b, int min)
        {
            return Math.Max(min, TextRenderer.MeasureText(b.Text, b.Font).Width + ApcTheme.Px(24));
        }

        // 블록 i 앞 간격: 입력 행끼리 RowGap, 구획 제목 다음 ItemGap, 그 밖 GroupGap
        private int Gap(int i)
        {
            if (i == 0) return 0;
            var prev = _blocks[i - 1].Kind;
            var cur = _blocks[i].Kind;
            if (prev == BlockKind.Row && cur == BlockKind.Row) return ApcTheme.Px(ApcLayout.RowGap);
            if (prev == BlockKind.Title) return ApcTheme.Px(ApcLayout.ItemGap);
            return ApcTheme.Px(ApcLayout.GroupGap);
        }

        private static int BlockHeight(Block b, int fieldH)
        {
            switch (b.Kind)
            {
                case BlockKind.Row: return fieldH;
                case BlockKind.Group:
                    int n = b.Rows.Count(r => r.Kind == BlockKind.Row), nc = b.Rows.Count - n;
                    return b.Group.Padding.Vertical + n * fieldH + Math.Max(0, n - 1) * ApcTheme.Px(ApcLayout.RowGap)
                           + nc * (CaptionRowHeight + CaptionRowGap);
                case BlockKind.Fill: return 0;
                case BlockKind.Cells: return b.Cells.Max(c => c.Height);
                default: return b.Content.Height;
            }
        }

        private static void PlaceRow(Block r, int x, int y, int right, int labelW, int fieldH)
        {
            r.Caption.SetBounds(x, y, labelW, fieldH);
            int fx = x + labelW + ApcTheme.Px(ApcLayout.LabelGap);
            if (r.Trailing.Length == 0)
            {
                r.Field.SetBounds(fx, y, right - fx, fieldH);
                return;
            }
            r.Field.SetBounds(fx, y, r.Field.Width, fieldH);
            int tx = r.Field.Right + ApcTheme.Px(ApcLayout.ItemGap);
            foreach (var t in r.Trailing)
            {
                t.SetBounds(tx, y, t.Width, fieldH);
                tx = t.Right + ApcTheme.Px(ApcLayout.ItemGap);
            }
        }

        // 열 제목: 다음 행을 먼저 그 자리(제목 줄 아래)에 놓아 칸 위치를 얻고, 제목을 각 칸 위에 같은 폭으로
        private static void PlaceCaptions(Block captions, Block next, int x, int right, int labelW, int fieldH, int y)
        {
            PlaceRow(next, x, y + CaptionRowHeight + CaptionRowGap, right, labelW, fieldH);
            var targets = new[] { (Control)next.Caption, next.Field }.Concat(next.Trailing).ToArray();
            for (int i = 0; i < captions.Cells.Length; i++)
            {
                if (i >= targets.Length) throw new InvalidOperationException("열 제목이 다음 행의 칸 수보다 많습니다.");
                captions.Cells[i].SetBounds(targets[i].Left, y, targets[i].Width, CaptionRowHeight);
            }
        }

        private static bool IsTableGroup(Block g) { return g.Rows.Any(r => r.Kind == BlockKind.Captions); }

        private static int GroupLabelW(Block g)
        {
            return g.Rows.Where(r => r.Kind == BlockKind.Row).Select(r => TextW(r.Caption) + g.Group.Padding.Left).DefaultIfEmpty(0).Max();
        }

        private static int TextW(Label l) { return TextRenderer.MeasureText(l.Text, l.Font).Width + ApcTheme.Px(4); }
    }
}
