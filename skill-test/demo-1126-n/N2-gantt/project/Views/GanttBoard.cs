using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;
using MoldingShiftGantt.Data;

namespace MoldingShiftGantt.Views
{
    internal sealed class JobClickedEventArgs : EventArgs
    {
        public JobClickedEventArgs(Machine machine, WorkOrder order) { Machine = machine; Order = order; }
        public readonly Machine Machine;
        public readonly WorkOrder Order;
    }

    /// <summary>
    /// 성형기 × 시간 간트. 표준 컨트롤에 그리는 요소가 없어 Panel 위에 Label(막대·눈금·현재 시각선)을 올려 배치만 한다
    /// (직접 그리기 없음). 한 설비 = 위 계획 줄 + 아래 실적 줄.
    /// </summary>
    internal sealed class GanttBoard : Panel
    {
        private const int NameColumnPx = 150, HeaderPx = 30, RowGapPx = 4, BarGapPx = 6, RightMarginPx = 40, TickHalfPx = 30, MinutesPerTick = 60;

        private sealed class Bar
        {
            public Label Label; public int Row, SubRow, StartMin, EndMin;
            public Machine Machine; public WorkOrder Order; public string ShortText, FullText;
        }

        private readonly List<Bar> _bars = new List<Bar>();
        private readonly List<Label> _ticks = new List<Label>(), _gridLines = new List<Label>(), _rowLines = new List<Label>();
        private readonly List<Label> _names = new List<Label>(), _stats = new List<Label>();
        private readonly List<Label> _nowSegs = new List<Label>();
        private readonly Label _nowText = new Label();
        private int _machineCount, _shiftMin, _nowMin, _startHour;
        private WorkOrder _selected;

        public event EventHandler<JobClickedEventArgs> JobClicked;

        public GanttBoard()
        {
            BackColor = ApcTheme.C("#FFFFFF");
            DoubleBuffered = true;
        }

        public void SetData(IMainViewSource src)
        {
            SuspendLayout();
            Controls.Clear();
            _bars.Clear(); _ticks.Clear(); _gridLines.Clear(); _rowLines.Clear(); _names.Clear(); _stats.Clear();
            var machines = src.GetMachines();
            _machineCount = machines.Length; _shiftMin = src.ShiftMinutes; _nowMin = src.NowMin; _startHour = src.ShiftStartHour;

            // 먼저 Add한 것이 위에 겹친다: 현재 시각선 → 막대 → 글자 → 눈금선
            _nowSegs.Clear();
            Style(_nowText, ApcTheme.C("#1B1F24"), "현재 " + Hm(_nowMin));
            _nowText.ForeColor = Color.White; _nowText.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(_nowText);
            for (int r = 0; r < machines.Length; r++)
            {
                var seg = new Label(); Style(seg, ApcTheme.C("#1B1F24"), ""); _nowSegs.Add(seg); Controls.Add(seg);
            }

            for (int r = 0; r < machines.Length; r++) AddMachine(src, machines[r], r);
            for (int h = 0; h <= _shiftMin / MinutesPerTick; h++)
            {
                var t = new Label { Text = Hm(h * MinutesPerTick), TextAlign = ContentAlignment.MiddleCenter, AutoSize = false, Font = ApcTheme.GetFont() };
                _ticks.Add(t); Controls.Add(t);
                for (int r = 0; r < machines.Length; r++)
                {
                    var g = new Label(); Style(g, ApcTheme.C("#8A94A0"), ""); _gridLines.Add(g); Controls.Add(g);
                }
            }
            for (int r = 0; r <= machines.Length; r++)
            {
                var l = new Label(); Style(l, ApcTheme.C("#B8C0CA"), ""); _rowLines.Add(l); Controls.Add(l);
            }
            ResumeLayout(true);
            PerformLayout();
        }

        public void Select(WorkOrder wo)
        {
            _selected = wo;
            foreach (var b in _bars)
                b.Label.BorderStyle = (wo != null && b.Order == wo && b.Label.Tag != null) ? BorderStyle.FixedSingle : BorderStyle.None;
        }

        private void AddMachine(IMainViewSource src, Machine m, int row)
        {
            bool delayed = src.IsDelayed(m), down = src.IsDown(m);
            var name = new Label
            {
                Text = m.Name + "   " + m.Ton + " t", AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Font = ApcTheme.GetFont(FontStyle.Bold)
            };
            double planned = src.PlannedQtyAtNow(m);
            string state = down ? "고장 정지" : (delayed ? "지연 " : "달성 ") + (planned > 0 ? (src.ActualQty(m) * 100.0 / planned).ToString("0.0") + " %" : "");
            var stat = new Label { Text = state, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Font = ApcTheme.GetFont() };
            if (delayed || down) { stat.ForeColor = ApcTheme.StatusText(StatusKind.Alarm); stat.Font = ApcTheme.GetFont(FontStyle.Bold); }
            _names.Add(name); _stats.Add(stat);
            Controls.Add(name); Controls.Add(stat);

            foreach (var p in m.Plan)
            {
                bool chg = p.Kind == BlockKind.Changeover;
                string text = chg ? "교체 " + (p.EndMin - p.StartMin) + "분" : p.Wo.Part + " · " + p.Wo.Qty.ToString("#,0");
                AddBar(m, row, 0, p.StartMin, p.EndMin, chg ? StatusKind.Warn : StatusKind.Info, text, chg ? null : p.Wo);
            }
            foreach (var a in m.Actual)
            {
                string text;
                StatusKind kind;
                if (a.Kind == BlockKind.Downtime) { kind = StatusKind.Alarm; text = "고장 " + Hm(a.StartMin) + (a.Running ? "~" : "~" + Hm(a.EndMin)); }
                else if (a.Kind == BlockKind.Changeover) { kind = StatusKind.Warn; text = a.Running ? "교체 중" : "교체 " + (a.EndMin - a.StartMin) + "분"; }
                else { kind = StatusKind.Run; text = a.Qty.ToString("#,0"); }
                AddBar(m, row, 1, a.StartMin, a.EndMin, kind, text, a.Kind == BlockKind.Job ? a.Wo : null);
            }
        }

        private void AddBar(Machine m, int row, int sub, int start, int end, StatusKind kind, string text, WorkOrder order)
        {
            var lb = new Label
            {
                Text = text, AutoSize = false, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleCenter, Font = ApcTheme.GetFont(),
                BackColor = ApcTheme.Status(kind), ForeColor = (kind == StatusKind.Warn || kind == StatusKind.Run) ? Color.Black : Color.White,
                UseMnemonic = false, Tag = order
            };
            if (start < _nowMin && _nowMin < end)
            {
                // 현재 시각선이 막대 위에 겹치지 않게 둘로 나눈다(글자는 긴 쪽에)
                var right = CloneLabel(lb);
                bool leftBigger = _nowMin - start >= end - _nowMin;
                if (leftBigger) right.Text = ""; else lb.Text = "";
                RegisterBar(m, row, sub, start, _nowMin, lb, order, null);
                RegisterBar(m, row, sub, _nowMin, end, right, order, null);
                return;
            }
            RegisterBar(m, row, sub, start, end, lb, order, kind == StatusKind.Alarm ? "고장" : null);
        }

        private static Label CloneLabel(Label src)
        {
            return new Label
            {
                AutoSize = false, AutoEllipsis = true, TextAlign = src.TextAlign, Font = src.Font, BackColor = src.BackColor,
                ForeColor = src.ForeColor, UseMnemonic = false, Tag = src.Tag, Text = src.Text
            };
        }

        private void RegisterBar(Machine m, int row, int sub, int start, int end, Label lb, WorkOrder order, string shortText)
        {
            var bar = new Bar { Label = lb, Row = row, SubRow = sub, StartMin = start, EndMin = end, Machine = m, Order = order, ShortText = shortText };
            if (order != null)
            {
                lb.Cursor = Cursors.Hand;
                lb.Click += (s, e) => { if (JobClicked != null) JobClicked(this, new JobClickedEventArgs(bar.Machine, bar.Order)); };
            }
            _bars.Add(bar);
            Controls.Add(lb);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_machineCount == 0 || Width <= 0) return;
            int nameW = ApcTheme.Px(NameColumnPx), head = ApcTheme.Px(HeaderPx), gap = ApcTheme.Px(RowGapPx), barGap = ApcTheme.Px(BarGapPx);
            int areaW = Width - nameW - ApcTheme.Px(RightMarginPx);
            int rowH = (Height - head) / _machineCount;
            int barH = (rowH - gap - barGap) / 2;
            Func<int, int> x = min => nameW + (int)((long)areaW * min / _shiftMin);

            int nx = x(_nowMin);
            int guideTop = barH + gap / 2;
            for (int h = 0; h < _ticks.Count; h++)
            {
                int gx = x(h * MinutesPerTick);
                int half = ApcTheme.Px(TickHalfPx);
                _ticks[h].SetBounds(gx - half, 0, half * 2, head);
                _ticks[h].Visible = Math.Abs(gx - nx) > ApcTheme.Px(52);   // 현재 시각 글자와 겹치면 감춘다
                for (int r = 0; r < _machineCount; r++)
                    _gridLines[h * _machineCount + r].SetBounds(gx, head + r * rowH + guideTop, 1, barGap);
            }
            for (int r = 0; r < _machineCount; r++)
            {
                int top = head + r * rowH;
                _names[r].SetBounds(ApcTheme.Px(8), top + gap / 2, nameW - ApcTheme.Px(8), barH);
                _stats[r].SetBounds(ApcTheme.Px(8), top + gap / 2 + barH + barGap, nameW - ApcTheme.Px(8), barH);
            }
            for (int r = 0; r < _rowLines.Count; r++) _rowLines[r].SetBounds(0, head + r * rowH, Width, 1);
            foreach (var b in _bars)
            {
                int top = head + b.Row * rowH + gap / 2 + b.SubRow * (barH + barGap);
                int left = x(b.StartMin) + (b.StartMin == _nowMin ? 2 : 0);
                int right = x(b.EndMin) - (b.EndMin == _nowMin ? 3 : 1);
                b.Label.SetBounds(left, top, Math.Max(2, right - left), barH);
                if (b.ShortText != null)
                {
                    if (b.FullText == null) b.FullText = b.Label.Text;
                    b.Label.Text = TextRenderer.MeasureText(b.FullText, b.Label.Font).Width + ApcTheme.Px(8) <= b.Label.Width ? b.FullText : b.ShortText;
                }
            }
            for (int r = 0; r < _nowSegs.Count; r++) _nowSegs[r].SetBounds(nx - 1, head + r * rowH + 1, 3, rowH - 1);
            _nowText.SetBounds(nx - ApcTheme.Px(48), 0, ApcTheme.Px(96), head - ApcTheme.Px(4));
        }

        private string Hm(int min)
        {
            int t = _startHour * 60 + min;
            return (t / 60 % 24).ToString("00") + ":" + (t % 60).ToString("00");
        }

        private static void Style(Label l, Color back, string text)
        {
            l.AutoSize = false; l.BackColor = back; l.Text = text; l.Font = ApcTheme.GetFont();
        }
    }
}
