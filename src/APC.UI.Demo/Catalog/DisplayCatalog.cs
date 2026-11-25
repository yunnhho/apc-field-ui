using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Catalog
{
    /// <summary>표시 컨트롤 카탈로그: 설비 타일, 값 카드, 판정 표시판, 추이 그래프 × 상태.</summary>
    internal sealed class DisplayCatalog : ApcTemplateView
    {
        public DisplayCatalog() : base("표시 컨트롤")
        {
            Body.AutoScroll = true;

            var chart = new ApcTrendChart { Dock = DockStyle.Top, Height = S(200), Reference = 160, UpperLimit = 175 };
            var values = new double[61];
            for (int i = 0; i <= 60; i++) values[i] = 166 + 8 * Math.Sin(i / 9.0) + i * 0.1;
            chart.SetValues(values);
            chart.XLabels = new[] { "-60분", "-30분", "현재" };

            var info = new ApcInfoStrip();
            info.SetItems("공정", "설비", "작업 LOT");
            info.SetValue("공정", "적층 공정");
            info.SetValue("설비", "12인치 적층기 2호기");
            info.SetValue("작업 LOT", "LMC2611240317");
            var legend = new ApcStatusLegend { Dock = DockStyle.Top };
            legend.SetItems(new ApcLampValue(StatusKind.Run, "관리선 내"), new ApcLampValue(StatusKind.Warn, "관리선 이탈 (SPEC 내)"),
                new ApcLampValue(StatusKind.Alarm, "SPEC 초과"), new ApcLampValue(StatusKind.Stop, "정지"));

            // Top 도킹은 나중 추가가 위 → 보여줄 순서의 역순으로 추가
            Body.Controls.Add(legend);
            Body.Controls.Add(Row(Caption("상태색 범례 (ApcStatusLegend): 구획 제목 줄 오른쪽에 Dock = Right")));
            Body.Controls.Add(info);
            Body.Controls.Add(Row(Caption("이름-값 정보판 (ApcInfoStrip): 헤더 아래 공정·설비·LOT")));
            Body.Controls.Add(chart);
            Body.Controls.Add(Row(Caption("추이 그래프 (ApcTrendChart): 설정값 회색 점선, 상한 빨간 점선")));

            var judges = Flow();
            judges.Controls.Add(Judge("READY", "시리얼을 스캔하세요", StatusKind.Stop));
            judges.Controls.Add(Judge("OK", "하중 15.7 kN · 변위 12.1 mm", StatusKind.Run));
            judges.Controls.Add(Judge("NG", "하중 19.4 kN > 상한 18.0", StatusKind.Alarm));
            judges.Controls.Add(Judge("확인", "센서 응답 지연", StatusKind.Warn));
            Body.Controls.Add(judges);
            Body.Controls.Add(Row(Caption("판정 표시판 (ApcJudgePanel)")));

            var cards = Flow();
            cards.Controls.Add(new ApcValueCard { Height = S(130), Title = "하도 부스 온도", Value = "23.4", Unit = "℃", Footer = "기준 20.0 ~ 26.0" });
            cards.Controls.Add(new ApcValueCard { Height = S(130), Title = "상도 부스 습도", Value = "67.5", Unit = "%", Status = StatusKind.Warn, Footer = "상한 70% 근접" });
            cards.Controls.Add(new ApcValueCard { Height = S(130), Title = "건조로 2 온도", Value = "176.4", Unit = "℃", Status = StatusKind.Alarm, ShowLamp = true, Footer = "상한 175.0 초과" });
            cards.Controls.Add(new ApcValueCard { Height = S(130), Title = "냉각 존 풍량", Value = "", Unit = "m³/min", Status = StatusKind.Stop, ShowLamp = true, Footer = "연결 끊김" });
            cards.Controls.Add(new ApcValueCard { Height = S(130), Title = "금일 생산", Value = "148", ValueAlign = HorizontalAlignment.Center });
            var bay = new ApcValueCard { Height = S(130), Title = "BAY-08", Value = "22.2", Unit = "℃", Status = StatusKind.Alarm, ValueStatus = StatusKind.Run, ShowLamp = true };
            bay.SetFooterParts(new[] { "습도 43.8 %RH", "파티클 4,210" }, new[] { StatusKind.Run, StatusKind.Alarm });   // 항목마다 제 색
            cards.Controls.Add(bay);
            Body.Controls.Add(cards);
            Body.Controls.Add(Row(Caption("값 카드 (ApcValueCard)")));


            var tiles = Flow();
            tiles.Controls.Add(Tile("전처리", StatusKind.Run));
            tiles.Controls.Add(Tile("상도 부스", StatusKind.Warn));
            tiles.Controls.Add(Tile("건조로 2", StatusKind.Alarm));
            tiles.Controls.Add(Tile("냉각 존", StatusKind.Stop));
            tiles.Controls.Add(Tile("검사기", StatusKind.Info));
            Body.Controls.Add(tiles);
            Body.Controls.Add(Row(Caption("설비 타일 (ApcStatusTile): 마우스를 올리면 파란 테두리, 누르면 Click → 선택(Selected) 바탕")));

            var lowTiles = Flow();
            lowTiles.Controls.Add(new ApcStatusTile { Text = "축 지름 A", Status = StatusKind.Alarm, Size = new Size(S(200), S(ApcLayout.FieldHeight)), TabStop = false });
            lowTiles.Controls.Add(new ApcStatusTile { Text = "축 지름 B", Status = StatusKind.Run, Size = new Size(S(200), S(ApcLayout.FieldHeight)), TabStop = false });
            Body.Controls.Add(lowTiles);
            Body.Controls.Add(Row(Caption("낮은 타일 (입력칸 높이): 램프는 20px 아래로 줄지 않는다")));

            var events = new ApcGrid { ReadOnly = true };
            events.Columns.Add("t", "시각");
            events.Columns.Add("m", "내용");
            events.Columns["m"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            events.Rows.Add("13:41:07", "Process Start · L2611-0471");
            events.Rows.Add("13:38:25", "Process End · L2611-0466");
            var waits = new ApcGrid { ReadOnly = true };
            waits.Columns.Add("lot", "Lot ID");
            waits.Columns.Add("q", "Q-time 남음");
            foreach (DataGridViewColumn col in waits.Columns) col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            waits.Rows.Add("L2611-0482", "1:35");
            waits.Rows.Add("L2611-0485", "2:10");
            Body.Controls.Add(Columns(S(170), new Control[] { Titled("대기 Lot", waits), Titled("최근 이벤트", events, Button("전체 보기", ApcButtonVariant.Normal)) }));
            Body.Controls.Add(Row(Caption("제목 + 표 구획 (Titled): 제목 줄 오른쪽 버튼 · Columns()/FillColumns() 칸·Body Fill에 그대로")));

            Body.Controls.Add(new ApcNoticeBar { Text = "인터락 E05 우회 중", Detail = "15:52까지 · 생산분은 추가 검사 대상" });
            Body.Controls.Add(Row(Caption("지속 상태 알림 띠 (ApcNoticeBar): 우회·시험 모드 — 판정판과 크기를 다투지 않는다")));
        }

        private static FlowLayoutPanel Flow()
        {
            return new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, S(8)) };
        }

        private ApcStatusTile Tile(string name, StatusKind status)
        {
            var t = new ApcStatusTile { Text = name, Status = status, Width = S(250) };
            t.Click += (s, e) =>
            {
                foreach (var other in t.Parent.Controls.OfType<ApcStatusTile>()) other.Selected = other == t;
            };
            return t;
        }

        private static ApcJudgePanel Judge(string text, string sub, StatusKind status)
        {
            return new ApcJudgePanel { Text = text, SubText = sub, Status = status, Size = new Size(S(300), S(140)), Margin = new Padding(0, 0, S(12), S(12)) };
        }
    }
}
