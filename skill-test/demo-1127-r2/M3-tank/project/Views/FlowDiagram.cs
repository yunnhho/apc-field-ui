using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 공정 흐름도: 표준 Label/Panel만 조립한다(직접 그리지 않음). 탱크 = 틀 Panel + 레벨 채움(틀의 자식),
    /// 배관 = 두께 6px Label, 밸브·펌프 = 상태색 Label. 색의 뜻은 화면의 범례와 글자("열림", "가동")로 읽힌다.
    /// </summary>
    internal sealed class FlowDiagram : Panel
    {
        // 가로 위치(폭 비율). 탱크 폭 0.13.
        private const double TankW = 0.13, T1X = 0.01, P1X = 0.285, T3X = 0.42, P2X = 0.65, T4X = 0.76;
        private const double V1X = 0.2125, V3X = 0.365, V4X = 0.5875, V5X = 0.7175, V6X = 0.93, ExitX = 0.965;

        private IList<TankInfo> _tanks = new List<TankInfo>();
        private IList<PumpInfo> _pumps = new List<PumpInfo>();
        private IList<ValveInfo> _valves = new List<ValveInfo>();

        public FlowDiagram() { Resize += (s, e) => Rebuild(); }

        public void Apply(IList<TankInfo> tanks, IList<PumpInfo> pumps, IList<ValveInfo> valves)
        {
            _tanks = tanks; _pumps = pumps; _valves = valves;
            Rebuild();
        }

        private static int Px(int v) { return ApcTheme.Px(v); }
        private int X(double f) { return (int)(ClientSize.Width * f); }

        private bool PumpOn(string id) { var p = _pumps.FirstOrDefault(x => x.Id == id); return p != null && p.Running; }
        private bool Open(string id) { var v = _valves.FirstOrDefault(x => x.Id == id); return v != null && v.IsOpen; }
        private bool Flows(string valveId)
        {
            var v = _valves.FirstOrDefault(x => x.Id == valveId);
            return v != null && v.IsOpen && (v.PumpId == null || PumpOn(v.PumpId));
        }

        private void Rebuild()
        {
            if (ClientSize.Width < 50 || ClientSize.Height < 50 || _tanks.Count == 0) return;
            SuspendLayout();
            foreach (Control c in Controls.Cast<Control>().ToList()) { Controls.Remove(c); c.Dispose(); }

            int h = ClientSize.Height, gap = Px(12), half = (h - gap) / 2;
            int tw = X(TankW);
            int y1 = half / 2, y2 = half + gap + half / 2, ym = h / 2;
            int xc = X(P1X);

            // 배관 → 밸브·펌프 → 탱크 순으로 올려 위에 오게 한다.
            Pipe(X(T1X) + tw, xc, y1, Flows("V1"));
            Pipe(X(T1X) + tw, xc, y2, Flows("V2"));
            VPipe(xc, y1, y2, (Flows("V1") || Flows("V2")) && PumpOn("P1"));
            Pipe(xc, X(T3X), ym, Flows("V3"));
            Pipe(X(T3X) + tw, X(P2X), ym, Flows("V4"));
            Pipe(X(P2X), X(T4X), ym, Flows("V5"));
            Pipe(X(T4X) + tw, X(ExitX), ym, Flows("V6"));

            Valve("V1", X(V1X), y1); Valve("V2", X(V1X), y2); Valve("V3", X(V3X), ym);
            Valve("V4", X(V4X), ym); Valve("V5", X(V5X), ym); Valve("V6", X(V6X), ym);
            Pump("P1", xc, ym); Pump("P2", X(P2X), ym);
            var exit = new Label { Text = "출하", AutoSize = true, BackColor = Color.Transparent };
            exit.Location = new Point(X(ExitX) + Px(4), ym - Px(10));
            Controls.Add(exit);

            Tank("T1", X(T1X), 0, tw, half); Tank("T2", X(T1X), half + gap, tw, half);
            Tank("T3", X(T3X), 0, tw, h); Tank("T4", X(T4X), 0, tw, h);
            ResumeLayout();
        }

        private void Pipe(int xa, int xb, int y, bool flowing)
        {
            int t = Px(6);
            Controls.Add(new Label { BackColor = ApcTheme.Status(flowing ? StatusKind.Run : StatusKind.Stop), Bounds = new Rectangle(xa, y - t / 2, xb - xa, t) });
        }

        private void VPipe(int x, int ya, int yb, bool flowing)
        {
            int t = Px(6);
            Controls.Add(new Label { BackColor = ApcTheme.Status(flowing ? StatusKind.Run : StatusKind.Stop), Bounds = new Rectangle(x - t / 2, ya, t, yb - ya) });
        }

        private Label Badge(string text, StatusKind kind, int cx, int cy, int w, int hgt)
        {
            var l = new Label
            {
                Text = text, TextAlign = ContentAlignment.MiddleCenter, BackColor = ApcTheme.Status(kind),
                BorderStyle = BorderStyle.FixedSingle, Bounds = new Rectangle(cx - w / 2, cy - hgt / 2, w, hgt)
            };
            Controls.Add(l);
            return l;
        }

        private void Valve(string id, int cx, int cy)
        {
            bool open = Open(id);
            Badge(id + (open ? " 열림" : " 닫힘") + (Flows(id) ? " ▶" : ""), open ? StatusKind.Run : StatusKind.Stop, cx, cy, Px(104), Px(34));
        }

        private void Pump(string id, int cx, int cy)
        {
            bool on = PumpOn(id);
            Badge(id + (on ? " 가동" : " 정지"), on ? StatusKind.Run : StatusKind.Stop, cx, cy, Px(92), Px(40));
        }

        private void Tank(string id, int x, int y, int w, int hgt)
        {
            var t = _tanks.First(k => k.Id == id);
            var level = ProcessRules.LevelStatus(t.Level);
            var worst = (StatusKind)System.Math.Max((int)level, (int)ProcessRules.TempStatus(t));
            var frame = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Bounds = new Rectangle(x, y, w, hgt) };
            int top = Px(4);
            var name = new Label { Text = id + " " + t.Name, AutoSize = false, Bounds = new Rectangle(Px(6), top, w - Px(12), Px(26)) };
            var value = new Label
            {
                Text = t.Level + " %  " + t.Temp.ToString("0.0") + " ℃", AutoSize = false, Font = ApcTheme.GetFont(FontStyle.Bold),
                Bounds = new Rectangle(Px(6), top + Px(26), w - Px(12), Px(28)),
                ForeColor = worst == StatusKind.Run ? name.ForeColor : ApcTheme.StatusText(worst)
            };
            int areaTop = top + Px(26) + Px(30), area = hgt - areaTop - Px(4);
            int fill = area * t.Level / 100;
            var fillLabel = new Label { BackColor = ApcTheme.Status(level), Bounds = new Rectangle(Px(4), areaTop + area - fill, w - Px(8), fill) };
            frame.Controls.Add(name); frame.Controls.Add(value); frame.Controls.Add(fillLabel);
            int lineY = areaTop + area - area * ProcessRules.LevelAlarm / 100;
            var line = new Label { BackColor = ApcTheme.Status(StatusKind.Alarm), Bounds = new Rectangle(Px(4), lineY, w - Px(8), Px(2)) };
            frame.Controls.Add(line);
            line.BringToFront();
            Controls.Add(frame);
        }
    }
}
