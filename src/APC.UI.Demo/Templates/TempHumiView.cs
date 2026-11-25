using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>
    /// references/TempHumi.png 재구성: 상단 연결 정보 + 페이지 이동, 센서 카드 격자.
    /// 원본의 빨강(온도)/파랑(습도) 값 색은 상태색과 겹쳐서 쓰지 않는다 — 상태는 카드 램프·테두리(ApcValueCard.Status).
    /// </summary>
    internal sealed class TempHumiView : ApcTemplateView
    {
        private static readonly object[][] Sensors =
        {
            // 이름, 온도, 습도, 상태, 하단 문구
            new object[] { "LineA SensorNM0", null, null, StatusKind.Stop, "수신 대기 중..." },
            new object[] { "LineA SensorNM1", 23.4, 41.0, StatusKind.Run, "11:20:10 수신" },
            new object[] { "LineA SensorNM2", 24.1, 39.5, StatusKind.Run, "11:20:10 수신" },
            new object[] { "LineB SensorNM0", 29.8, 58.2, StatusKind.Warn, "온도 상한 근접 (기준 30.0℃)" },
            new object[] { "LineB SensorNM1", 33.6, 61.0, StatusKind.Alarm, "온도 상한 초과 (기준 30.0℃)" },
            new object[] { "LineB SensorNM2", null, null, StatusKind.Stop, "연결 끊김 · 10:58:02 마지막 수신" },
        };

        public TempHumiView() : base("온습도 모니터링")
        {

            var prev = Button("◀ 이전", ApcButtonVariant.Normal, 80);
            var next = Button("다음 ▶", ApcButtonVariant.Normal, 80);
            prev.Enabled = next.Enabled = false;   // 1 / 1 페이지
            var page = Caption("1 / 1");
            page.Margin = new Padding(S(4), S(8), S(12), 0);
            var log = Button("로그", ApcButtonVariant.Normal, 80);
            log.Margin = new Padding(S(24), 0, 0, 0);
            var paging = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, BackColor = Color.Transparent };
            paging.Controls.AddRange(new Control[] { prev, page, next, log });

            var info = Row(Info("SMG IP"), Value("192.168.100.180"), Info("Port"), Value("502"), Info("데이터 수집 주기"), Value("10초"));
            var infoBar = new Panel { Dock = DockStyle.Top, Height = S(46), BackColor = Color.Transparent };
            info.Dock = DockStyle.Fill;
            infoBar.Controls.Add(info);
            infoBar.Controls.Add(paging);

            var cards = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };
            foreach (var s in Sensors)
                cards.Controls.Add(SensorCard((string)s[0], s[1] as double?, s[2] as double?, (StatusKind)s[3], (string)s[4]));

            Body.Controls.Add(cards);
            Body.Controls.Add(infoBar);
        }

        private static Label Info(string text)
        {
            var l = Caption(text);
            l.Font = ApcTheme.GetFont();
            l.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.TextMuted);
            l.Margin = new Padding(0, S(8), S(6), 0);
            return l;
        }

        private static Label Value(string text)
        {
            var l = Caption(text);
            l.Font = ApcTheme.GetFont(FontStyle.Bold);   // 값 강조(이름표 아님)
            l.Margin = new Padding(0, S(8), S(28), 0);
            return l;
        }

        /// <summary>센서 하나 = 값 카드(온도 크게, 습도·수신 시각은 아래 줄). 값이 없으면 빈칸.</summary>
        private static ApcValueCard SensorCard(string title, double? temp, double? humi, StatusKind status, string footer)
        {
            return new ApcValueCard
            {
                Title = title,
                Value = temp.HasValue ? temp.Value.ToString("0.0") : "",
                Unit = "℃",
                Footer = (humi.HasValue ? "습도 " + humi.Value.ToString("0.0") + " %RH · " : "") + footer,
                Status = status,
                ShowLamp = true,
                Size = new Size(S(372), S(170)),
                Margin = new Padding(0, 0, S(16), S(16)),
            };
        }
    }
}
