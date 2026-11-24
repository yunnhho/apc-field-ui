using System.Drawing;
using APC.UI.Controls;
using APC.UI.Drawing;
using APC.UI.Theming;

namespace APC.UI.Checks
{
    internal static partial class Program
    {
        private static void RunDisplayChecks()
        {
            double lo, hi, step;
            ApcTrendChart.NiceRange(157.6, 176.4, 5, out lo, out hi, out step);
            Check(lo == 155 && hi == 180 && step == 5, "trend: nice range 155~180 step 5 (was " + lo + "~" + hi + " / " + step + ")");
            ApcTrendChart.NiceRange(5, 5, 5, out lo, out hi, out step);
            Check(lo < 5 && hi > 5 && step > 0, "trend: flat data still gets a range");
            ApcTrendChart.NiceRange(0, 0.37, 5, out lo, out hi, out step);
            Check(lo == 0 && hi >= 0.37 && step < 0.2, "trend: small values get fractional steps");
            using (var chart = new ApcTrendChart())
            {
                chart.SetValues(new[] { 1.0, double.NaN, 2.0 });
                Check(chart.ValueCount == 2, "trend: NaN (missing) values skipped");
                chart.UpperLimit = double.NaN;
                chart.Reference = double.PositiveInfinity;
                Check(chart.UpperLimit == null && chart.Reference == null, "trend: NaN/infinite limit means no line");
            }

            Check(ApcTheme.Px(100) == (int)System.Math.Round(100 * ApcTheme.Scale), "theme: Px scales like template S()");

            using (var legend = new ApcStatusLegend())
            {
                legend.SetItems(new ApcLampValue(StatusKind.Run, "관리선 내"));
                int one = legend.Width;
                legend.SetItems(new ApcLampValue(StatusKind.Run, "관리선 내"), new ApcLampValue(StatusKind.Alarm, "SPEC 초과"));
                Check(one > 0 && legend.Width > one && legend.Width == legend.PreferredWidth, "legend: width follows items");
            }
            using (var info = new ApcInfoStrip())
            {
                info.SetItems("공정", "설비");
                info.SetValue("설비", "12인치 적층기");
                Check(info.GetValue("설비") == "12인치 적층기" && info.GetValue("공정") == "", "info strip: values by name, unset = empty");
                bool threw = false;
                try { info.SetValue("없는 이름", "x"); } catch (System.ArgumentException) { threw = true; }
                Check(threw, "info strip: unknown name rejected");
            }


            var c = ApcTheme.Current.Colors;
            Check(Cards.EmphasisColor(StatusKind.Run) == ApcTheme.C(c.Text), "card: normal value uses text color (not green)");
            Check(Cards.EmphasisColor(StatusKind.Alarm) == ApcTheme.Status(StatusKind.Alarm), "card: alarm value uses alarm color");
            Check(Cards.EmphasisColor(StatusKind.Stop) == ApcTheme.C(c.TextMuted), "card: stopped value is muted");
            Check(ApcJudgePanel.ForeFor(StatusKind.Warn) == ApcTheme.C(c.Text), "judge: dark text on yellow");
            Check(ApcJudgePanel.ForeFor(StatusKind.Alarm) == ApcTheme.C(c.TextInverse), "judge: white text on red");

            using (var card = new ApcValueCard { Status = StatusKind.Alarm, Title = "건조로 2", Value = "176.4", Unit = "℃" })
            using (var bmp = new Bitmap(card.Width, card.Height))
            {
                card.DrawToBitmap(bmp, new Rectangle(Point.Empty, card.Size));
                Check(bmp.GetPixel(1, card.Height / 2).ToArgb() == ApcTheme.Status(StatusKind.Alarm).ToArgb(), "card: alarm draws thick alarm border");
            }
            CheckSteelInkSurfaces();
            CheckHeaderTools();
            // 같은 높이 카드는 Footer 유무와 관계없이 값이 같은 높이에 그려진다(한 줄 카드의 값이 들쭉날쭉하지 않게).
            Check(ValueTop(new ApcValueCard { Value = "88" }) == ValueTop(new ApcValueCard { Value = "88", Footer = "NG / 검사 수" }), "card: value sits at same height with or without footer");
            // 값이 두 개인 카드(온도 + Footer 습도): 카드 상태는 경고여도 정상인 주 값은 보통 글자색.
            Check(!HasColor(new ApcValueCard { Value = "22.9", Status = StatusKind.Warn, ValueStatus = StatusKind.Run }, ApcTheme.Status(StatusKind.Warn), false), "card: ValueStatus overrides value color");
            Check(HasColor(new ApcValueCard { Value = "22.9", Status = StatusKind.Warn }, ApcTheme.Status(StatusKind.Warn), false), "card: value follows Status when ValueStatus unset");
            Check(!HasColor(new ApcValueCard { Footer = "습도 51.3 %RH · 정상", Status = StatusKind.Alarm, FooterStatus = StatusKind.Run }, ApcTheme.Status(StatusKind.Alarm), true), "card: FooterStatus overrides footer color");
            // Footer 보조 값이 여러 개: 항목마다 제 색(정상 습도는 회색, 알람 파티클만 빨강).
            var alarm = ApcTheme.Status(StatusKind.Alarm);
            var parts = new ApcValueCard { Status = StatusKind.Alarm };
            parts.SetFooterParts(new[] { "습도 43.8 %RH", "파티클 4,210" }, new[] { StatusKind.Run, StatusKind.Alarm });
            Check(parts.Footer == "습도 43.8 %RH · 파티클 4,210", "card: SetFooterParts joins parts into Footer");
            Check(HasColor(parts, alarm, true), "card: alarm footer part drawn in alarm color");
            var normal = new ApcValueCard { Status = StatusKind.Alarm };
            normal.SetFooterParts(new[] { "습도 43.8 %RH", "파티클 820" }, new[] { StatusKind.Run, StatusKind.Run });
            Check(!HasColor(normal, alarm, true), "card: normal footer parts stay muted on alarm card");
            using (var tile = new ApcStatusTile { Status = StatusKind.Run, Text = "전처리" })
            using (var bmp = new Bitmap(tile.Width, tile.Height))
            {
                tile.DrawToBitmap(bmp, new Rectangle(Point.Empty, tile.Size));
                Check(bmp.GetPixel(0, tile.Height / 2).ToArgb() == ApcTheme.C(c.Border).ToArgb(), "tile: normal border when idle");
                Check(tile.TabStop, "tile: reachable by Tab");
                Check(Near(bmp.GetPixel(4, 4), ApcTheme.C(c.Panel[0]), 4), "tile: panel background when not selected");
                tile.Selected = true;
                tile.DrawToBitmap(bmp, new Rectangle(Point.Empty, tile.Size));
                Check(bmp.GetPixel(4, 4).ToArgb() == ApcTheme.C(c.GridSelected).ToArgb(), "tile: selected uses grid selection background");
            }
        }

        private static Bitmap Draw(ApcValueCard card)
        {
            card.Size = new Size(ApcTheme.Px(300), ApcTheme.Px(150));
            var bmp = new Bitmap(card.Width, card.Height);
            card.DrawToBitmap(bmp, new Rectangle(Point.Empty, card.Size));
            card.Dispose();
            return bmp;
        }

        // 제목 띠 아래 ~ Footer 자리 위에서 진한 글자 픽셀이 처음 나오는 줄
        private static int ValueTop(ApcValueCard card)
        {
            int titleH = ApcTheme.Px(36) + 2, bottom = card.Height - ApcTheme.Px(32);
            using (var bmp = Draw(card))
                for (int y = titleH; y < bottom; y++)
                    for (int x = ApcTheme.Px(14); x < bmp.Width / 2; x++)
                        if (bmp.GetPixel(x, y).GetBrightness() < 0.3f) return y;
            return -1;
        }

        // 값 영역(footer=false) 또는 Footer 줄(footer=true) 안에 그 색 픽셀이 있나. 테두리는 피한다.
        private static bool HasColor(ApcValueCard card, Color color, bool footer)
        {
            using (var bmp = Draw(card))
            {
                int split = bmp.Height - ApcTheme.Px(32);
                int top = footer ? split + 2 : ApcTheme.Px(36) + 2, bottom = footer ? bmp.Height - ApcTheme.Px(4) : split - 1;
                for (int y = top; y < bottom; y++)
                    for (int x = ApcTheme.Px(14); x < bmp.Width - ApcTheme.Px(14); x++)
                        if (bmp.GetPixel(x, y).ToArgb() == color.ToArgb()) return true;
            }
            return false;
        }

        /// <summary>Steel + Ink: 카드는 패널 그라데이션, 묶음 라벨 옆 먹색 막대, 헤더 띠는 밝은 금속 톤.</summary>
        private static void CheckSteelInkSurfaces()
        {
            var c = ApcTheme.Current.Colors;
            using (var card = new ApcValueCard { Status = StatusKind.Run, Title = "건조로 1", Value = "170.0", Unit = "℃" })
            using (var bmp = new Bitmap(card.Width, card.Height))
            {
                card.DrawToBitmap(bmp, new Rectangle(Point.Empty, card.Size));
                Check(Near(bmp.GetPixel(card.Width - 4, card.Height - 4), ApcTheme.C(c.Panel[1]), 4), "card: panel gradient fill");
            }
            using (var group = new ApcGroup { Text = "LOT 바코드", Size = new Size(300, 120) })
            using (var bmp = new Bitmap(group.Width, group.Height))
            {
                group.DrawToBitmap(bmp, new Rectangle(Point.Empty, group.Size));
                Check(bmp.GetPixel(ApcTheme.Px(10) + 1, group.TitleHeight / 2).ToArgb() == ApcTheme.C(c.Brand).ToArgb(), "group: brand bar beside label");
            }
            using (var bar = new ApcHeaderBar { Title = "체결기 #1", StatusText = "가동중", Size = new Size(600, 40) })
            using (var bmp = new Bitmap(bar.Width, bar.Height))
            {
                bar.DrawToBitmap(bmp, new Rectangle(Point.Empty, bar.Size));
                var px = bmp.GetPixel(2, bar.Height / 2);
                Check(px.R > 200 && px.G > 200 && px.B > 200, "header bar: light background");
                bool blank = true;
                for (int x = 2; x < bar.ClockBounds.Left - 2 && blank; x++)
                    for (int y = 4; y < bar.Height - 4 && blank; y++)
                        blank = bmp.GetPixel(x, y).ToArgb() == bmp.GetPixel(2, y).ToArgb();
                Check(blank, "header bar: nothing but the clock (no title, no status)");
                Check(System.Text.RegularExpressions.Regex.IsMatch(bar.ClockText, @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$"), "header bar: clock shows yyyy-MM-dd HH:mm:ss");
                Check(bar.ClockBounds.Right <= bar.Width - ApcTheme.Px(12), "header bar: clock at right");
            }
            using (var v = new SectionProbe())
            {
                v.Title.Size = v.Title.PreferredSize;
                using (var bmp = new Bitmap(v.Title.Width, v.Title.Height))
                {
                    v.Title.DrawToBitmap(bmp, new Rectangle(Point.Empty, v.Title.Size));
                    Check(bmp.GetPixel(1, v.Title.Height / 2).ToArgb() == ApcTheme.C(c.Brand).ToArgb(), "section title: brand bar on the left");
                }
            }
        }

        private static void CheckHeaderTools()
        {
            using (var v = new TabProbe())
            {
                Check(v.Tabs.Parent != null && v.Tabs.Parent.Parent == v.Bar, "header tools: tabs go into the header strip");
                Check(v.Tabs.Bottom == v.Bar.Height, "header tools: tab bottom touches header bottom line");
                Check(v.Note.Parent == v.Tabs.Parent && v.Note.Left > v.Tabs.Right, "header tools: extra controls follow the tabs");
            }
        }

        private sealed class TabProbe : ApcTemplateView
        {
            public readonly ApcTabStrip Tabs = new ApcTabStrip();
            public readonly System.Windows.Forms.Label Note = Caption("ⓘ 안내");
            public ApcHeaderBar Bar { get { return Header; } }
            public TabProbe() : base("probe")
            {
                Tabs.SetTabs(new[] { "1라인", "2라인" });
                Tabs.Width = Tabs.PreferredTabsWidth;
                HeaderTools(Tabs, Note);
            }
        }

        private sealed class SectionProbe : ApcTemplateView
        {
            public readonly System.Windows.Forms.Label Title = SectionTitle("최근 알람 이력");
            public SectionProbe() : base("probe") { }
        }

        private static bool Near(Color a, Color b, int tol)
        {
            return System.Math.Abs(a.R - b.R) <= tol && System.Math.Abs(a.G - b.G) <= tol && System.Math.Abs(a.B - b.B) <= tol;
        }
    }
}
