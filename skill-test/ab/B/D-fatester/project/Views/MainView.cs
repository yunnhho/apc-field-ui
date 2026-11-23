using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FATester.Data;

namespace FATester.Views
{
    /// <summary>메인 화면: 채널 타일 → 진행률 카드 → 저항 카드 → 누설전류 카드 → 불량 분류 표. 데이터는 IMainViewSource에서만.</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int CardHeight = 112;
        private readonly IList<ChannelInfo> _channels;

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("FA 시험기 채널 모니터링")
        {
            _channels = source.GetChannels();
            var tiles = new List<Control>();
            var progress = new List<Control>();
            var resistance = new List<Control>();
            var leakage = new List<Control>();
            int alarm = 0, warn = 0, stop = 0, wait = 0;

            foreach (var c in _channels)
            {
                string reason;
                StatusKind kind = Judge.Channel(c, out reason);
                string text = c.ProcessText + (reason != null ? " · " + reason : c.Process == StatusKind.Alarm ? " · " + c.LastErrorCode : "");
                var tile = new ApcStatusTile { Text = c.Name, Status = kind, StatusText = text, Dock = DockStyle.Fill };
                var channel = c;
                tile.Click += (s, e) => OpenDetail(channel);
                tiles.Add(tile);
                if (kind == StatusKind.Alarm) { alarm++; Alarm.Push(c.Name + ": 오류 " + c.LastErrorCode + " " + c.LastErrorText.Substring(0, c.LastErrorText.IndexOf(" ("))); }
                else if (kind == StatusKind.Warn) warn++;
                else if (c.ProcessText == "대기") wait++;
                else if (kind == StatusKind.Stop) stop++;

                bool live = c.ProcessText == "시험중" || c.ProcessText == "완료";
                progress.Add(Card(c.Name + " 진행률", c.SampleId == "" ? "" : c.Progress.ToString("#,0", CultureInfo.InvariantCulture), "%",
                    live ? StatusKind.Run : StatusKind.Stop, c.SampleId == "" ? "시료 없음" : c.SampleId + " · " + c.TestItem));
                resistance.Add(Card(c.Name + " 저항", Fmt(c.Resistance), "mΩ", ValueKind(c, c.Resistance, Judge.Resistance(c.Resistance ?? 0)),
                    "기준 " + Judge.ResistanceUpper.ToString("0.0") + " 이하 · 경고 " + Judge.ResistanceWarn.ToString("0.0") + " 이상"));
                leakage.Add(Card(c.Name + " 누설전류", Fmt(c.Leakage), "nA", ValueKind(c, c.Leakage, Judge.Leakage(c.Leakage ?? 0)),
                    "기준 " + Judge.LeakageUpper.ToString("0.0") + " 이하 · 경고 " + Judge.LeakageWarn.ToString("0.0") + " 이상"));
            }

            var parts = new List<string> { "전체" };
            if (alarm > 0) parts.Add("알람 " + alarm);
            if (warn > 0) parts.Add("경고 " + warn);
            if (stop > 0) parts.Add("정지 " + stop);
            if (wait > 0) parts.Add("대기 " + wait);
            Header.Status = alarm > 0 ? StatusKind.Alarm : warn > 0 ? StatusKind.Warn : stop + wait > 0 ? StatusKind.Stop : StatusKind.Run;
            Header.StatusText = parts.Count == 1 ? "전체 · 정상" : string.Join(" · ", parts);

            var grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
            grid.Columns.Add("id", "시료 ID");
            grid.Columns.Add("mode", "불량 모드");
            grid.Columns.Add("loc", "위치");
            grid.Columns.Add("time", "판정 시각");
            foreach (DataGridViewColumn col in grid.Columns) col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (var d in source.GetDefects()) grid.Rows.Add(d.SampleId, d.Mode, d.Location, d.JudgedAt);

            Body.Controls.Add(grid);
            Body.Controls.Add(Row(SectionTitle("불량 분류 결과")));
            Body.Controls.Add(Columns(S(CardHeight), leakage.ToArray()));
            Body.Controls.Add(Columns(S(CardHeight), resistance.ToArray()));
            Body.Controls.Add(Columns(S(CardHeight), progress.ToArray()));
            Body.Controls.Add(Columns(S(80), tiles.ToArray()));
        }

        // 시험중인 채널의 값만 판정, 완료는 최종값(Run), 오류·대기는 값만 보이고 Stop
        private static StatusKind ValueKind(ChannelInfo c, double? v, StatusKind judged)
        {
            if (!v.HasValue || c.ProcessText == "오류" || c.ProcessText == "대기") return StatusKind.Stop;
            return c.ProcessText == "시험중" ? judged : StatusKind.Run;
        }

        private static string Fmt(double? v) { return v.HasValue ? v.Value.ToString("0.0", CultureInfo.InvariantCulture) : ""; }

        private static ApcValueCard Card(string title, string value, string unit, StatusKind status, string footer)
        {
            return new ApcValueCard { Title = title, Value = value, Unit = unit, Status = status, Footer = footer, ShowLamp = true, Dock = DockStyle.Fill };
        }

        private void OpenDetail(ChannelInfo c)
        {
            using (var dlg = new EquipmentDetailDialog(c)) dlg.ShowDialog(FindForm());
        }
    }
}
