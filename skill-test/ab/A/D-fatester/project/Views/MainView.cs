using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FATester.Data;

namespace FATester.Views
{
    /// <summary>메인 화면. 채널 타일(누르면 설비 상세) → 채널 현재값 표 → 불량 분류 결과 표.</summary>
    internal sealed class MainView : ApcTemplateView
    {
        public const string ScreenTitle = "FA 시험기 모니터링";

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base(ScreenTitle)
        {
            var channels = source.GetChannels();
            var defects = source.GetDefects();

            int alarms = 0, waiting = 0;
            var tiles = new Control[channels.Count];
            for (int i = 0; i < channels.Count; i++)
            {
                var ch = channels[i];
                var tile = new ApcStatusTile { Text = ch.Name, Status = ch.Status, StatusText = ch.StatusText + (ch.State == ChannelRow.Waiting ? "" : " · " + ch.Progress + "%"), Dock = DockStyle.Fill };
                tile.Click += (s, e) => OpenDetail(ch);
                tiles[i] = tile;
                if (ch.Status == StatusKind.Alarm) { alarms++; Alarm.Push(ch.Name + ": " + ch.StatusText + " (" + ch.ErrorCode + ")"); }
                else if (ch.State == ChannelRow.Waiting) waiting++;
            }
            var parts = new List<string> { "전체" };
            if (alarms > 0) parts.Add("알람 " + alarms);
            if (waiting > 0) parts.Add("대기 " + waiting);
            if (parts.Count == 1) parts.Add("정상");
            Header.Status = alarms > 0 ? StatusKind.Alarm : waiting > 0 ? StatusKind.Stop : StatusKind.Run;
            Header.StatusText = string.Join(" · ", parts);

            var defectGrid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            defectGrid.Columns.Add("id", "시료 ID");
            defectGrid.Columns.Add("mode", "불량 모드");
            defectGrid.Columns.Add("loc", "위치");
            defectGrid.Columns.Add("time", "판정 시각");
            foreach (DataGridViewColumn c in defectGrid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (var d in defects) defectGrid.Rows.Add(d.SampleId, d.Mode, d.Location, d.JudgedAt);

            // Fill 먼저, Top 줄은 아래쪽 것부터
            Body.Controls.Add(defectGrid);
            var defectTitle = SectionTitle("불량 분류 결과");
            defectTitle.Dock = DockStyle.Top;
            Body.Controls.Add(defectTitle);
            Body.Controls.Add(Columns(ApcGrid.HeightForRows(channels.Count), new Control[] { BuildChannelGrid(channels) }));
            var chTitle = SectionTitle("채널별 시험 현황");
            chTitle.Dock = DockStyle.Top;
            Body.Controls.Add(chTitle);
            Body.Controls.Add(Columns(S(90), tiles));
        }

        private static ApcGrid BuildChannelGrid(IList<ChannelRow> channels)
        {
            var grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            grid.Columns.Add(new ApcLampColumn { Name = "state", HeaderText = "상태" });
            grid.Columns.Add("ch", "채널");
            grid.Columns.Add("id", "현재 시료 ID");
            grid.Columns.Add("item", "시험 항목");
            grid.Columns.Add("progress", "진행률(%)");
            grid.Columns.Add("res", "저항(mΩ)");
            grid.Columns.Add("leak", "누설전류(nA)");
            for (int i = 1; i < grid.Columns.Count; i++) grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["state"].Width = ApcTheme.Px(130);
            foreach (var ch in channels)
                grid.Rows.Add(new ApcLampValue(ch.Status, ch.State), ch.Name, ch.SampleId, ch.TestItem,
                    ch.State == ChannelRow.Waiting ? "" : ch.Progress.ToString(), ch.Resistance, ch.Leakage);
            return grid;
        }

        private void OpenDetail(ChannelRow ch)
        {
            using (var dlg = new EquipmentDetailDialog(ch)) dlg.ShowDialog(FindForm());
        }
    }
}
