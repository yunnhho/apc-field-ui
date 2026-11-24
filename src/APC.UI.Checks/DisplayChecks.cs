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

            using (var card = new ApcFieldCard { Width = ApcTheme.Px(240) })
            {
                var a = new ApcTextBox();
                var b = new ApcTextBox();
                card.AddField("LOT", a);
                int one = card.Height;
                card.AddField("바렐", b);
                Check(one == ApcFieldCard.HeightFor(1) && card.Height == ApcFieldCard.HeightFor(2) && card.Height > one, "field card: height grows with fields");
                card.Footer = "스캔 08:12:33";
                card.FooterStatus = StatusKind.Run;
                Check(card.Height == ApcFieldCard.HeightFor(2), "field card: footer change keeps height");
                Check(a.Left == b.Left && a.Width == b.Width && a.Right < card.Width && b.Top > a.Bottom && b.Bottom < card.Height,
                      "field card: fields stacked inside card at same width");
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
                Check(bmp.GetPixel(4, 4).ToArgb() == Color.White.ToArgb(), "tile: white background when not selected");
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
    }
}
