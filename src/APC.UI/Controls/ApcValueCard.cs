using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 현재값 카드: 제목 띠(그리드 헤더 그라데이션, ShowLamp면 오른쪽에 램프) + 큰 값·단위 + 아래 한 줄(기준·범위·수신 시각 등).
    /// 값 색은 Status로: 경고/알람만 상태색, 정지는 흐리게. 알람이면 굵은 알람색 테두리. Value가 비면 값·단위 모두 빈칸.
    /// 값이 두 개인 카드는 ValueStatus/FooterStatus로 값·Footer 글자색을 그 항목 판정대로 따로 준다(램프·테두리는 Status).
    /// </summary>
    public class ApcValueCard : Control
    {
        private string _title = "";
        private string _value = "";
        private string _unit = "";
        private string _footer = "";
        private StatusKind _status = StatusKind.Run;
        private StatusKind? _valueStatus, _footerStatus;
        private bool _showLamp;
        private HorizontalAlignment _valueAlign = HorizontalAlignment.Left;

        public ApcValueCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(S(260), S(150));
            Margin = new Padding(0, 0, S(12), S(12));
            ApcTheme.Changed += OnThemeChanged;
        }

        [DefaultValue("")] public string Title { get { return _title; } set { _title = value ?? ""; Invalidate(); } }
        [DefaultValue("")] public string Value { get { return _value; } set { _value = value ?? ""; Invalidate(); } }
        [DefaultValue("")] public string Unit { get { return _unit; } set { _unit = value ?? ""; Invalidate(); } }

        /// <summary>아래 한 줄. 비우면 줄이 없다.</summary>
        [DefaultValue("")] public string Footer { get { return _footer; } set { _footer = value ?? ""; _parts = null; Invalidate(); } }

        /// <summary>
        /// Footer에 보조 값이 여러 개일 때(습도 · 파티클) 항목마다 제 판정 색으로 그린다. " · "로 이어 붙이고, Footer는 이은 글이 된다.
        /// FooterStatus 하나로는 정상인 항목까지 알람색이 되므로 이걸 쓴다. Footer를 다시 대입하면 항목 색은 지워진다.
        /// </summary>
        public void SetFooterParts(string[] texts, StatusKind[] statuses)
        {
            if (texts == null || statuses == null || texts.Length != statuses.Length)
                throw new ArgumentException("texts와 statuses는 같은 개수여야 합니다.");
            _footer = string.Join(Sep, texts);
            _parts = texts.Length == 0 ? null : new KeyValuePair<string, StatusKind>[texts.Length];
            for (int i = 0; i < texts.Length; i++) _parts[i] = new KeyValuePair<string, StatusKind>(texts[i] ?? "", statuses[i]);
            Invalidate();
        }

        private const string Sep = " · ";
        private KeyValuePair<string, StatusKind>[] _parts;

        [DefaultValue(StatusKind.Run)] public StatusKind Status { get { return _status; } set { _status = value; Invalidate(); } }

        /// <summary>큰 값 글자색만 따로 정할 때(값이 두 개인 카드: 카드는 습도 경고인데 온도 값은 정상). null이면 Status.</summary>
        [DefaultValue(null)] public StatusKind? ValueStatus { get { return _valueStatus; } set { _valueStatus = value; Invalidate(); } }

        /// <summary>Footer 글자색만 따로 정할 때(Footer에 적은 보조 값이 정상인데 카드는 알람). null이면 Status.</summary>
        [DefaultValue(null)] public StatusKind? FooterStatus { get { return _footerStatus; } set { _footerStatus = value; Invalidate(); } }
        [DefaultValue(false)] public bool ShowLamp { get { return _showLamp; } set { _showLamp = value; Invalidate(); } }
        [DefaultValue(HorizontalAlignment.Left)] public HorizontalAlignment ValueAlign { get { return _valueAlign; } set { _valueAlign = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (ApcTheme.Current == null) { g.Clear(SystemColors.Control); return; }
            var c = ApcTheme.Current.Colors;
            int pad = S(14), titleH = S(36);
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;

            Cards.DrawFrame(g, Size, _status, false);
            int inset = _status == StatusKind.Alarm ? S(3) : 1;
            Gradients.Fill(g, new Rectangle(inset, inset, Width - 2 * inset, titleH - inset), c.GridHeader);
            using (var pen = new Pen(ApcTheme.C(c.Border))) g.DrawLine(pen, inset, titleH, Width - inset, titleH);

            // 정지(판정 없음)면 램프를 그리지 않는다 — 회색 램프가 지표 카드마다 붙으면 "정지"로 읽힌다. 값은 이미 흐리다.
            bool lampOn = _showLamp && _status != StatusKind.Stop;
            int lamp = lampOn ? S(20) : 0;
            if (lampOn)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(Cards.Lamp(_status), new Rectangle(Width - pad - lamp, (titleH - lamp) / 2, lamp, lamp));
            }
            TextClip.Draw(g, this, _title, ApcTheme.GetFont(FontStyle.Bold), new Rectangle(pad, 0, Width - pad * 2 - lamp, titleH), ApcTheme.C(c.Text), flags);

            // Footer 자리는 Footer가 없어도 늘 뺀다 → 같은 높이 카드는 값 글자 크기·높이가 같다(한 줄에 섞여도 값이 나란하다).
            int footerH = S(32);
            DrawValue(g, Rectangle.FromLTRB(pad, titleH, Width - pad, Height - footerH));

            if (_footer.Length > 0)
            {
                int y = Height - footerH;
                using (var pen = new Pen(ApcTheme.C(c.GridLine))) g.DrawLine(pen, pad, y, Width - pad, y);
                var rect = new Rectangle(pad, y, Width - pad * 2, footerH);
                if (_parts != null) DrawFooterParts(g, rect, flags);
                else TextClip.Draw(g, this, _footer, ApcTheme.GetFont(), rect, FooterColor(_footerStatus ?? _status), flags | AlignFlag(_valueAlign));
            }
        }

        private static Color FooterColor(StatusKind s)
        {
            return s == StatusKind.Warn || s == StatusKind.Alarm ? ApcTheme.StatusText(s) : ApcTheme.C(ApcTheme.Current.Colors.TextMuted);
        }

        // 항목을 이어 그린다. 전체 폭으로 정렬 위치를 잡고, 넘치면 남은 칸에서 말줄임(render 점검에 잡힌다).
        private void DrawFooterParts(Graphics g, Rectangle rect, TextFormatFlags flags)
        {
            var font = ApcTheme.GetFont();
            var measure = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            int total = TextRenderer.MeasureText(g, _footer, font, Size.Empty, measure).Width;
            int x = total >= rect.Width || _valueAlign == HorizontalAlignment.Left ? rect.Left
                  : _valueAlign == HorizontalAlignment.Center ? rect.Left + (rect.Width - total) / 2 : rect.Right - total;
            var draw = flags | TextFormatFlags.NoPadding;
            for (int i = 0; i < _parts.Length && x < rect.Right; i++)
            {
                if (i > 0) x = DrawPart(g, Sep, font, x, rect, FooterColor(StatusKind.Run), draw, measure);
                x = DrawPart(g, _parts[i].Key, font, x, rect, FooterColor(_parts[i].Value), draw, measure);
            }
        }

        private int DrawPart(Graphics g, string text, Font font, int x, Rectangle rect, Color color, TextFormatFlags draw, TextFormatFlags measure)
        {
            TextClip.Draw(g, this, text, font, Rectangle.FromLTRB(x, rect.Top, rect.Right, rect.Bottom), color, draw);
            return x + TextRenderer.MeasureText(g, text, font, Size.Empty, measure).Width;
        }

        private void DrawValue(Graphics g, Rectangle area)
        {
            int sizeBase = area.Height;
            if (_value.Trim().Length == 0) return;   // 값 없음(대기·통신 전)은 빈칸
            string text = _value;
            var muted = ApcTheme.C(ApcTheme.Current.Colors.TextMuted);
            // 값 글자는 영역 높이에 맞춘다(카드를 키우면 값도 커진다).
            float px = Math.Max(12f, Math.Min(sizeBase * 0.55f, S(64)));
            using (var big = new Font(ApcTheme.Current.FontFamily, px, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var unitFont = new Font(ApcTheme.Current.FontFamily, Math.Max(ApcTheme.GetFont().SizeInPoints * g.DpiY / 72f, px * UnitRatio), FontStyle.Bold, GraphicsUnit.Pixel))
            {
                var noPad = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
                var vs = TextRenderer.MeasureText(g, text, big, Size.Empty, noPad);
                var us = _unit.Length > 0 ? TextRenderer.MeasureText(g, _unit, unitFont, Size.Empty, noPad) : Size.Empty;
                int gap = us.Width > 0 ? S(6) : 0;
                int total = vs.Width + gap + us.Width;
                int x = _valueAlign == HorizontalAlignment.Center ? area.Left + (area.Width - total) / 2
                      : _valueAlign == HorizontalAlignment.Right ? area.Right - total : area.Left;
                int y = area.Top + (area.Height - vs.Height) / 2;
                TextRenderer.DrawText(g, text, big, new Point(x, y), Cards.EmphasisColor(_valueStatus ?? _status), noPad);
                if (us.Width > 0)   // 단위는 값의 아래쪽 기준선에 맞춘다
                    TextRenderer.DrawText(g, _unit, unitFont, new Point(x + vs.Width + gap, y + vs.Height - us.Height - S(2)), muted, noPad);
            }
        }

        private const float UnitRatio = 0.5f;   // 단위 = 값 글자의 절반, 굵게(작고 흐리면 %·mm가 안 보인다). 최소 본문 글자 크기.

        private static TextFormatFlags AlignFlag(HorizontalAlignment a)
        {
            return a == HorizontalAlignment.Center ? TextFormatFlags.HorizontalCenter
                 : a == HorizontalAlignment.Right ? TextFormatFlags.Right : TextFormatFlags.Left;
        }

        private void OnThemeChanged(object sender, EventArgs e) { Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ApcTheme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }

        private static int S(int px) { return (int)Math.Round(px * ApcTheme.Scale); }
    }
}
