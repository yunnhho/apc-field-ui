using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>
    /// 라인 전체 모니터링: 설비 타일 한 줄(누르면 상세 dialog) → 현재값 카드 한 줄 → 최근 알람 이력.
    /// 카드·타일은 TableLayoutPanel 퍼센트 열로 같은 폭.
    /// </summary>
    internal sealed class EquipmentView : ApcTemplateView
    {
        private const int SampleHistoryRows = 22;

        // 설비명, 상태(타일은 램프 + 이름만, 사유는 값 카드·알람 이력에)
        private static readonly object[][] Machines =
        {
            new object[] { "전처리", StatusKind.Run },
            new object[] { "하도 부스", StatusKind.Run },
            new object[] { "상도 부스", StatusKind.Warn },
            new object[] { "건조로1", StatusKind.Run },
            new object[] { "건조로2", StatusKind.Alarm },
            new object[] { "냉각 존", StatusKind.Stop },
        };

        // 제목, 값, 단위, 상태, 아래 줄 — 값·기준은 알람 이력·상세 dialog와 같게
        private static readonly object[][] Values =
        {
            new object[] { "하도 부스 온도", "24.5", "℃", StatusKind.Run, "설정 25.0" },
            new object[] { "상도 부스 습도", "68.0", "%RH", StatusKind.Warn, "설정 60.0 · 상한 70.0" },
            new object[] { "건조로1 온도", "142.0", "℃", StatusKind.Run, "설정 140.0" },
            new object[] { "건조로2 온도", "168.0", "℃", StatusKind.Alarm, "설정 160.0 · 상한 165.0" },
            new object[] { "컨베이어 속도", "1.8", "m/min", StatusKind.Run, "설정 1.8" },
        };

        public EquipmentView() : base("도장 라인 모니터링")
        {

            var tiles = new Control[Machines.Length];
            for (int i = 0; i < Machines.Length; i++)
            {
                var m = Machines[i];
                var tile = new ApcStatusTile { Text = (string)m[0], Status = (StatusKind)m[1], Dock = DockStyle.Fill };
                tile.Click += (s, e) => OpenDetail();
                tiles[i] = tile;
            }

            var cards = new Control[Values.Length];
            for (int i = 0; i < Values.Length; i++)
            {
                var v = Values[i];
                cards[i] = new ApcValueCard { Title = (string)v[0], Value = (string)v[1], Unit = (string)v[2], Status = (StatusKind)v[3], Footer = (string)v[4], ShowLamp = true, Dock = DockStyle.Fill };
            }

            var grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            grid.Columns.Add(new ApcLampColumn());
            grid.Columns.Add("time", "시각");
            grid.Columns.Add("unit", "설비");
            grid.Columns.Add("text", "내용");
            grid.Columns["text"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["text"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;   // 긴 글만 왼쪽
            grid.Rows.Add(StatusKind.Alarm, "14:52:10", "건조로2", "온도 상한 초과 168.0 > 165.0 ℃");
            grid.Rows.Add(StatusKind.Warn, "14:41:37", "상도 부스", "습도 상한 근접 68.0 %RH (상한 70.0)");
            grid.Rows.Add(StatusKind.Stop, "13:58:12", "냉각 존", "계획 정지");
            grid.Rows.Add(StatusKind.Warn, "13:20:05", "전처리", "세정액 pH 편차 (6.5 / 설정 7.0)");
            // 샘플은 표 높이만큼 채운다(스크롤바 없이). 실제로는 데이터 인터페이스가 최근 이력을 준다.
            var older = new[]
            {
                new object[] { StatusKind.Run, "건조로1", "온도 정상 복귀 140.2 ℃" },
                new object[] { StatusKind.Warn, "하도 부스", "온도 하한 근접 22.1 ℃ (하한 22.0)" },
                new object[] { StatusKind.Info, "컨베이어", "속도 설정 변경 1.6 → 1.8 m/min" },
                new object[] { StatusKind.Alarm, "건조로2", "버너 착화 실패 (재시도 1회)" },
                new object[] { StatusKind.Run, "건조로2", "버너 재착화 성공" },
            };
            var at = new DateTime(2026, 11, 18, 13, 5, 0);
            for (int i = 0; i < SampleHistoryRows; i++, at = at.AddMinutes(-11))
            {
                var o = older[i % older.Length];
                grid.Rows.Add(o[0], at.ToString("HH:mm:ss"), o[1], o[2]);
            }

            // Top 도킹은 나중에 Add한 것이 위 → 제목 + 이력(Fill) 먼저, 그다음 아래 줄부터
            Body.Controls.Add(Titled("최근 알람 이력", grid));
            Body.Controls.Add(Columns(S(120), cards));
            Body.Controls.Add(Columns(S(90), tiles));
        }

        private void OpenDetail()
        {
            // 실제로는 데이터 인터페이스에서 그 설비의 센서·추이를 받아 넘긴다. 여기서는 샘플 dialog.
            using (var dlg = new EquipmentDetailDialog()) dlg.ShowDialog(FindForm());
        }
    }
}
