using System;
using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using MoldingShiftGantt.Data;

namespace MoldingShiftGantt.Views
{
    /// <summary>메인 화면: 지연·달성률 요약 → 간트(Fill) → 선택한 작업 상세. 데이터는 IMainViewSource에서만 받는다.</summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const double AchievementWarnPercent = 90.0;   // 지연 기준(10% 부족)과 같은 선

        private readonly IMainViewSource _source;
        private readonly GanttBoard _board = new GanttBoard();
        private readonly ApcJudgePanel _judge = new ApcJudgePanel();
        private readonly ApcValueCard _rate = new ApcValueCard { Title = "교대 계획 달성률", Unit = "%", Dock = DockStyle.Fill };
        private readonly ApcValueCard _down = new ApcValueCard { Title = "고장 정지", Unit = "대", Dock = DockStyle.Fill };
        private readonly ApcInfoStrip _jobStrip = new ApcInfoStrip { Dock = DockStyle.Fill };
        private readonly ApcInfoStrip _qtyStrip = new ApcInfoStrip { Dock = DockStyle.Fill };

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base("사출 성형기 교대 생산 계획")
        {
            _source = source;
            _judge.Dock = DockStyle.Fill;
            _jobStrip.SetItems("작업 지시", "성형기", "품번", "금형");
            _qtyStrip.SetItems("계획(개)", "실적(개)", "사이클타임(s)", "예상 종료");
            _board.JobClicked += (s, e) => ShowDetail(e.Machine, e.Order);

            var legend = new ApcStatusLegend();
            legend.SetItems(new ApcLampValue(StatusKind.Info, "계획 작업"), new ApcLampValue(StatusKind.Run, "실적"),
                new ApcLampValue(StatusKind.Warn, "금형 교체"), new ApcLampValue(StatusKind.Alarm, "고장"));
            legend.Dock = DockStyle.Right;
            var insert = Button("긴급 작업 끼워넣기", ApcButtonVariant.Primary, 220);
            insert.Click += (s, e) => OpenInsert();

            // Fill 먼저, 아래 줄부터 위로
            Body.Controls.Add(Titled("성형기 8대 교대 간트 (08:00~20:00)", _board, legend, insert));
            var qtyRow = Columns(_qtyStrip.Height, new Control[] { _qtyStrip });
            var jobRow = Columns(_jobStrip.Height, new Control[] { _jobStrip });
            qtyRow.Dock = DockStyle.Bottom; jobRow.Dock = DockStyle.Bottom;
            Body.Controls.Add(jobRow);
            Body.Controls.Add(qtyRow);
            Body.Controls.Add(Columns(S(130), new Control[] { _judge, _rate, _down }, new[] { 50f, 25f, 25f }));
            Reload();
        }

        /// <summary>데이터를 다시 읽어 화면 전체를 갱신(주기 갱신·끼워넣기 확정 뒤).</summary>
        public void Reload()
        {
            var machines = _source.GetMachines();
            _board.SetData(_source);

            var delayed = new List<string>();
            double planned = 0; int actual = 0, downCount = 0; string downName = "";
            Machine first = null;
            foreach (var m in machines)
            {
                planned += _source.PlannedQtyAtNow(m);
                actual += _source.ActualQty(m);
                if (_source.IsDelayed(m)) { delayed.Add(m.Name); if (first == null) first = m; }
                if (_source.IsDown(m)) { downCount++; downName = m.Name; }
            }

            if (delayed.Count > 0)
            {
                _judge.Status = StatusKind.Alarm;
                _judge.Text = "지연 설비 " + delayed.Count + "대";
                _judge.SubText = string.Join(", ", delayed.ToArray());
            }
            else { _judge.Status = StatusKind.Run; _judge.Text = "지연 설비 없음"; _judge.SubText = ""; }

            double pct = planned > 0 ? actual * 100.0 / planned : 0;
            _rate.Value = pct.ToString("0.0");
            _rate.Status = pct < AchievementWarnPercent ? StatusKind.Warn : StatusKind.Run;
            _rate.Footer = "계획 " + ((int)Math.Round(planned)).ToString("#,0") + " · 실적 " + actual.ToString("#,0");

            _down.Value = downCount.ToString();
            _down.Status = downCount > 0 ? StatusKind.Alarm : StatusKind.Run;
            _down.Footer = downCount > 0 ? downName : "";

            var target = first ?? machines[0];
            var wo = _source.GetRunningJob(target);
            if (wo != null) ShowDetail(target, wo);
        }

        private void ShowDetail(Machine m, WorkOrder wo)
        {
            var d = _source.GetJobDetail(m, wo);
            _board.Select(wo);
            _jobStrip.SetValue("작업 지시", wo.Code);
            _jobStrip.SetValue("성형기", m.Name);
            _jobStrip.SetValue("품번", wo.Part);
            _jobStrip.SetValue("금형", wo.Mold + " · " + wo.MoldTon + " t");
            _qtyStrip.SetValue("계획(개)", wo.Qty.ToString("#,0"));
            _qtyStrip.SetValue("실적(개)", d.ActualQty.ToString("#,0"));
            _qtyStrip.SetValue("사이클타임(s)", wo.CycleSec.ToString("0.0"));
            int t = _source.ShiftStartHour * 60 + d.ExpectedEndMin;
            _qtyStrip.SetValue("예상 종료", d.ExpectedEndMin < 0 ? "고장 복구 후" : (t / 60 % 24).ToString("00") + ":" + (t % 60).ToString("00"));
        }

        private void OpenInsert()
        {
            using (var dlg = new UrgentInsertDialog(_source))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.InsertUrgent(dlg.TargetMachine, dlg.Order, dlg.Position, dlg.ApproverBadge);
                Reload();
            }
        }
    }
}
