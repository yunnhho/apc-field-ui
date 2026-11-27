using System.Globalization;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using SpotWeldMonitor.Data;

namespace SpotWeldMonitor.Views
{
    /// <summary>
    /// 스폿 용접 셀 모니터링: 라인 판정판(가장 나쁜 셀) + 상세 버튼 → 셀 타일 6 → 라인 값 카드 4 → 오늘 알람 이력.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private readonly IMainViewSource _source;
        private readonly WorstCell _worst;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("스폿 용접 셀 모니터링")
        {
            _source = source;
            var cells = source.GetCells();
            var line = source.GetLineValues();
            _worst = WeldRules.Worst(cells);

            // 판정판: 라인 전체에서 가장 나쁜 셀(타일 선택과 무관)
            var judge = new ApcJudgePanel { Dock = DockStyle.Fill };
            var detail = Button("이상 셀 상세 확인", ApcButtonVariant.Primary, 200);
            var reason = Caption("");
            if (_worst == null)
            {
                judge.Status = StatusKind.Run; judge.Text = "전 셀 정상"; judge.SubText = "알람·정지·경고 셀 없음";
                detail.Enabled = false;
                reason.Text = "이상 셀 없음";
            }
            else
            {
                int sameLevel = WeldRules.SameLevelCount(cells, _worst.Status);
                judge.Status = _worst.Status;
                judge.Text = sameLevel > 1 ? _worst.Cell.Name + " 외 " + (sameLevel - 1) + "대 " + Word(_worst.Status)
                                          : _worst.Cell.Name + " " + Word(_worst.Status);
                judge.SubText = _worst.Reason;
            }

            var tiles = new Control[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                var c = cells[i];
                var tile = new ApcStatusTile
                {
                    Text = c.Name + "  " + WeldRules.StateWord(c.State) + "  " + WeldRules.Count(c.ShotsToday) + "타점",
                    Status = WeldRules.CellStatus(c),
                    Dock = DockStyle.Fill,
                };
                tile.Click += (s, e) => OpenDetail(c);
                tiles[i] = tile;
            }

            int totalShots = 0;
            foreach (var c in cells) totalShots += c.ShotsToday;
            var cards = new Control[]
            {
                new ApcValueCard { Title = "가동률", Value = WeldRules.Percent(line.AvailabilityPercent), Unit = "%", Status = WeldRules.AvailabilityStatus(line.AvailabilityPercent), ShowLamp = true, Footer = "목표 " + WeldRules.Percent(WeldRules.AvailabilityTarget) + " 이상", Dock = DockStyle.Fill },
                new ApcValueCard { Title = "오늘 타점 수", Value = WeldRules.Count(totalShots), Unit = "타점", Status = StatusKind.Run, Dock = DockStyle.Fill },
                new ApcValueCard { Title = "용접 NG 수", Value = WeldRules.Count(line.NgCount), Unit = "건", Status = StatusKind.Run, ValueStatus = line.NgCount > 0 ? StatusKind.Alarm : (StatusKind?)null, Dock = DockStyle.Fill },
                new ApcValueCard { Title = "팁 드레싱 대기 셀", Value = WeldRules.Count(line.DressingWaitingCells), Unit = "대", Status = StatusKind.Run, ValueStatus = line.DressingWaitingCells > 0 ? StatusKind.Warn : (StatusKind?)null, Dock = DockStyle.Fill },
            };

            var grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            grid.Columns.Add(new ApcLampColumn());
            grid.Columns.Add("time", "시각");
            grid.Columns.Add("cell", "셀");
            grid.Columns.Add("text", "내용");
            grid.Columns.Add("action", "조치");
            grid.Columns["text"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["text"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.Columns["action"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["action"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            foreach (var r in source.GetAlarmHistory()) grid.Rows.Add(r.Kind, r.Time, r.Cell, r.Text, r.Action);

            detail.Click += (s, e) => { if (_worst != null) OpenDetail(_worst.Cell); };

            // 이력(Fill) 먼저, 위쪽 줄은 화면 아래쪽 것부터
            Body.Controls.Add(Titled("오늘 알람 이력", grid));
            Body.Controls.Add(Columns(S(110), cards));
            Body.Controls.Add(Columns(S(90), tiles));
            Body.Controls.Add(Row(detail, reason));
            Body.Controls.Add(Columns(S(190), new Control[] { judge }));
        }

        private static string Word(StatusKind k) { return k == StatusKind.Alarm ? "알람" : k == StatusKind.Stop ? "정지" : "경고"; }

        private void OpenDetail(WeldCell cell)
        {
            using (var dlg = new CellDetailDialog(cell, _source.GetCurrentTrend(cell.Name)))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (!_source.RegisterTipChange(dlg.CellName, dlg.Badge))
                    ApcDialog.Warn(FindForm(), dlg.CellName + " 팁 교체 등록에 실패했습니다.");
                else
                    ApcDialog.Alert(FindForm(), dlg.CellName + " 팁 교체를 등록했습니다.");
            }
        }
    }
}
