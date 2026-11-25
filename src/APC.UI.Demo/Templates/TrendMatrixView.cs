using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>
    /// 호기 × 관리 항목 모니터링(references/legacy 적층·도금·프레스 모니터링 재구성):
    /// 정보판(공정·설비·관리 항목) → 호기별 현재값 표(묶음 머리줄, 칸마다 램프 + 값) → 구획 제목 + 상태색 범례 → 호기 줄 × 항목 칸 추이 그래프.
    /// 판정은 Judge 하나(표 램프·호기 상태가 같은 판정). 정지 호기의 값은 판정하지 않는다(Stop).
    /// </summary>
    internal sealed class TrendMatrixView : ApcTemplateView
    {
        private const int TrendPoints = 30;
        private static readonly float[] TrendPercents = { 0f, 100f / 3, 100f / 3, 100f / 3 };   // 0번(호기 타일)은 TileWidth 고정폭
        private const int TileWidth = 150;   // 램프 + "1호기"가 1280 폭에서도 안 잘리게(타일 부제 없음)

        /// <summary>관리 항목: 이름, 단위, 설정값, 관리선(±), SPEC(±). 정한 기준값 — 요구서가 있으면 그 값으로.</summary>
        private sealed class Item
        {
            public Item(string name, string unit, double set, double control, double spec) { Name = name; Unit = unit; Set = set; Control = control; Spec = spec; }
            public string Name { get; private set; }
            public string Unit { get; private set; }
            public double Set { get; private set; }
            public double Control { get; private set; }
            public double Spec { get; private set; }
        }

        private static readonly Item[] Items =
        {
            new Item("적층 Head", "℃", 180.0, 3.0, 6.0),
            new Item("프레스 Table", "℃", 80.0, 2.0, 4.0),
            new Item("박리 Table", "℃", 40.0, 2.0, 4.0),
            new Item("1 Layer", "s", 25.0, 3.0, 5.0),
        };

        // 호기, 가동 여부, 항목별 현재값(Items 순서). 2호기 Head 183.0 = 관리선과 같은 값 → 관리선 내(경계값 예시).
        private static readonly object[][] Units =
        {
            new object[] { "1호기", true, new[] { 181.2, 80.4, 39.6, 24.8 } },
            new object[] { "2호기", true, new[] { 183.0, 82.6, 40.3, 25.9 } },
            new object[] { "3호기", true, new[] { 186.8, 80.9, 41.1, 26.2 } },
            new object[] { "4호기", false, new[] { 96.4, 52.3, 31.8, 0.0 } },
        };

        public TrendMatrixView() : base("12인치 적층기 모니터링")
        {
            var info = new ApcInfoStrip();
            info.SetItems("공정", "설비", "관리 항목", "작업 LOT");
            info.SetValue("공정", "적층 공정");
            info.SetValue("설비", "12인치 적층기 1~4호기");
            info.SetValue("관리 항목", "온도 3종 · Cycle time");
            info.SetValue("작업 LOT", "LMC2611240317");

            var grid = BuildGrid();
            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Run, "관리선 내"), new ApcLampValue(StatusKind.Warn, "관리선 이탈 (SPEC 내)"),
                new ApcLampValue(StatusKind.Alarm, "SPEC 초과"), new ApcLampValue(StatusKind.Stop, "정지"));
            legend.Dock = DockStyle.Right;
            var title = SectionTitle("관리 항목 추이 (최근 30분)");
            title.AutoSize = false;
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Margin = Padding.Empty;
            var titleRow = new Panel { Dock = DockStyle.Top, Height = S(36) + S(ApcLayout.RowGap), Padding = new Padding(0, 0, 0, S(ApcLayout.RowGap)), BackColor = Color.Transparent };
            titleRow.Controls.Add(title);   // Fill 먼저
            titleRow.Controls.Add(legend);

            var heads = new Control[TrendPercents.Length];
            heads[0] = ChartHead("");
            for (int i = 1; i < heads.Length; i++) heads[i] = ChartHead(Items[i - 1].Name + " 온도 (" + Items[i - 1].Unit + ")");

            // Top 도킹은 나중에 Add한 것이 위 → 아래 줄(마지막 호기)부터
            for (int u = Units.Length - 1; u >= 0; u--) Body.Controls.Add(FixTile(Columns(S(130), TrendRow(u), TrendPercents)));   // 130: 1280x1024에서 4줄이 들어가는 높이
            Body.Controls.Add(FixTile(Columns(S(28), heads, TrendPercents)));
            Body.Controls.Add(titleRow);
            Body.Controls.Add(Columns(ApcGrid.HeightForRows(Units.Length, true), new Control[] { grid }));
            Body.Controls.Add(Columns(info.Height, new Control[] { info }));
        }

        /// <summary>첫 칸(호기 타일)을 고정폭으로: 비율이면 좁은 화면에서 호기 이름이 잘린다.</summary>
        private static TableLayoutPanel FixTile(TableLayoutPanel t)
        {
            t.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, S(TileWidth));
            return t;
        }

        /// <summary>관리 항목 판정 하나: SPEC 밖 = Alarm, 관리선 밖 = Warn, 그 안 = Run. 선과 같은 값은 안쪽(초과부터 이탈).</summary>
        private static StatusKind Judge(Item item, double value)
        {
            double off = Math.Abs(Math.Round(value - item.Set, 1));
            if (off > item.Spec) return StatusKind.Alarm;
            return off > item.Control ? StatusKind.Warn : StatusKind.Run;
        }

        private static bool Running(int unit) { return (bool)Units[unit][1]; }
        private static double[] Values(int unit) { return (double[])Units[unit][2]; }

        /// <summary>호기 상태 = 공정 상태(정지)와 값 판정 중 나쁜 쪽, 문구는 "가동 · 가장 나쁜 사유".</summary>
        private static ApcLampValue UnitState(int unit)
        {
            if (!Running(unit)) return new ApcLampValue(StatusKind.Stop, "정지");
            var worst = StatusKind.Run;
            string reason = null;
            for (int i = 0; i < Items.Length; i++)
            {
                var k = Judge(Items[i], Values(unit)[i]);
                if (Severity(k) <= Severity(worst)) continue;
                worst = k;
                reason = Items[i].Name + (k == StatusKind.Alarm ? " SPEC 초과" : " 관리선 이탈");
            }
            return new ApcLampValue(worst, reason == null ? "가동" : "가동 · " + reason);
        }

        private static int Severity(StatusKind k) { return k == StatusKind.Alarm ? 3 : k == StatusKind.Warn ? 2 : k == StatusKind.Stop ? 1 : 0; }

        private static ApcGrid BuildGrid()
        {
            var grid = new ApcGrid { ReadOnly = true };
            grid.Columns.Add("unit", "호기");
            grid.Columns.Add(new ApcLampColumn { Name = "status", HeaderText = "상태" });
            foreach (var item in Items) grid.Columns.Add(new ApcLampColumn { HeaderText = item.Name });
            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            grid.Columns["status"].FillWeight = 200;   // "가동 · 적층 Head SPEC 초과"가 잘리지 않게
            grid.AddColumnGroup("온도 (℃)", 2, 4);
            grid.AddColumnGroup("Cycle time (s)", 5, 5);

            for (int u = 0; u < Units.Length; u++)
            {
                var row = new object[2 + Items.Length];
                row[0] = Units[u][0];
                row[1] = UnitState(u);
                for (int i = 0; i < Items.Length; i++)
                {
                    double v = Values(u)[i];
                    // 정지 호기: 읽은 값은 보이되 판정하지 않는다(Stop). Cycle time은 정지 중 값이 없어 빈칸.
                    string text = !Running(u) && Items[i].Unit == "s" ? "" : v.ToString("0.0");
                    row[2 + i] = new ApcLampValue(Running(u) ? Judge(Items[i], v) : StatusKind.Stop, text);
                }
                grid.Rows.Add(row);
            }
            return grid;
        }

        private Control[] TrendRow(int unit)
        {
            var state = UnitState(unit);
            var cells = new Control[TrendPercents.Length];
            // 줄 머리 = 호기 타일(램프 + 호기, 누를 동작이 없어 TabStop 끔). 사유는 위 표의 상태 칸에 있다.
            cells[0] = new ApcStatusTile { Text = (string)Units[unit][0], Status = state.Status, TabStop = false };
            for (int i = 1; i < cells.Length; i++)
            {
                var item = Items[i - 1];
                var chart = new ApcTrendChart
                {
                    Reference = item.Set,
                    UpperLimit = item.Set + item.Spec,
                    LowerLimit = item.Set - item.Spec,
                    XLabels = new[] { "-30분", "-15분", "현재" },
                };
                chart.SetValues(Trend(unit, i - 1));
                cells[i] = chart;
            }
            return cells;
        }

        /// <summary>샘플 추이: 설정값 둘레를 천천히 흔들다가 마지막 점이 현재값(표와 같은 값). 정지 호기는 식어 내려가는 곡선.</summary>
        private static double[] Trend(int unit, int item)
        {
            double now = Values(unit)[item], set = Items[item].Set;
            var list = new List<double>(TrendPoints);
            for (int p = 0; p < TrendPoints; p++)
            {
                double t = p / (double)(TrendPoints - 1);
                double v = Running(unit)
                    ? set + (now - set) * t + Items[item].Control * 0.4 * Math.Sin((p + unit * 7 + item * 3) * 0.45) * (1 - t)
                    : set + (now - set) * t;
                list.Add(Math.Round(v, 1));
            }
            return list.ToArray();
        }

        private static Label ChartHead(string text)
        {
            var l = Caption(text);
            l.Font = ApcTheme.GetFont(FontStyle.Bold);
            l.AutoSize = false;
            l.TextAlign = ContentAlignment.MiddleCenter;
            l.Margin = Padding.Empty;
            return l;
        }
    }
}
