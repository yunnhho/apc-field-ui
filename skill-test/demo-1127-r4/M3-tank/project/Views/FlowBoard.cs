using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 공정 흐름도. 표준 Label만 Panel 안에 배치한다(그리기 없음): 탱크 틀 + 레벨 채움, 배관, 밸브, 펌프.
    /// 위치·크기는 Resize 때 다시 계산한다.
    /// </summary>
    internal sealed class FlowBoard : Panel
    {
        private const int PipeThick = 22, ValveW = 84, ValveH = 28, LineThick = 3;

        private TankInfo[] _tanks = new TankInfo[0];
        private ValveInfo[] _valves = new ValveInfo[0];
        private PumpInfo[] _pumps = new PumpInfo[0];

        /// <summary>밸브 표시를 누르면 밸브 번호를 알린다.</summary>
        public event Action<string> ValveClicked;

        public FlowBoard()
        {
            BackColor = ApcTheme.C(ApcTheme.Current.Colors.Panel[0]);
            Resize += (s, e) => Rebuild();
        }

        public void SetData(TankInfo[] tanks, ValveInfo[] valves, PumpInfo[] pumps)
        {
            _tanks = tanks; _valves = valves; _pumps = pumps;
            Rebuild();
        }

        private bool ValveOpen(string id)
        {
            foreach (var v in _valves) if (v.Id == id) return v.IsOpen;
            return false;
        }

        private bool PumpRunning(string id)
        {
            foreach (var p in _pumps) if (p.Id == id) return p.Running;
            return false;
        }

        private static int Px(int v) { return ApcTheme.Px(v); }

        private void Rebuild()
        {
            if (_tanks.Length < 4 || Width <= 0 || Height <= 0) return;
            SuspendLayout();
            foreach (Control c in Controls) c.Dispose();
            Controls.Clear();

            int W = Width, H = Height;
            int nameH = Px(26), titleH = nameH * 2, pipe = Px(PipeThick), vh = Px(ValveH), vw = Px(ValveW);
            int up = Px(26), low = Px(6);          // 스텁 위(화살표) 토막 / 밸브 아래 토막
            int stub = up + vh + low;
            int tankTop = titleH + Px(4);
            int tankBottom = Math.Max(tankTop + Px(80), H - stub - pipe - Px(8));
            int tankH = tankBottom - tankTop, tw = (int)(W * 0.14);
            int pipeTop = tankBottom + stub;
            int h2 = pipe / 2, hv = vw / 2;

            int[] tankLeft = { (int)(W * .03), (int)(W * .20), (int)(W * .46), (int)(W * .72) };
            int x1 = tankLeft[0] + tw / 2, x2 = tankLeft[1] + tw / 2;
            int x3in = tankLeft[2] + tw / 4, x3out = tankLeft[2] + tw * 3 / 4;
            int x4in = tankLeft[3] + tw / 4, x4out = tankLeft[3] + tw * 3 / 4;
            int xP1 = (int)(W * .36), xV3 = (int)(W * .43), xP2 = (int)(W * .625), xV5 = (int)(W * .70);
            int xShip = (int)(W * .90);

            bool f1 = ValveOpen("V1") && PumpRunning("P1");
            bool f2 = ValveOpen("V2") && PumpRunning("P1");
            bool ff = f1 || f2;
            bool f3 = ff && ValveOpen("V3");
            bool f4 = ValveOpen("V4") && PumpRunning("P2");
            bool f5 = f4 && ValveOpen("V5");
            bool f6 = ValveOpen("V6");

            // 배관: 밸브·펌프 자리와 서로 겹치지 않게 토막으로 끊는다.
            Hor(x1 + h2, x2 - h2, pipeTop, f1);
            Hor(x2 + h2, xP1 - hv, pipeTop, ff);
            Hor(xP1 + hv, xV3 - hv, pipeTop, ff);
            Hor(xV3 + hv, x3in - h2, pipeTop, f3);
            Hor(x3out + h2, xP2 - hv, pipeTop, f4);
            Hor(xP2 + hv, xV5 - hv, pipeTop, f4);
            Hor(xV5 + hv, x4in - h2, pipeTop, f5);
            Hor(x4out + h2, xShip, pipeTop, f6);
            int lowTop = tankBottom + up + vh;
            Ver(x1, tankBottom, up, f1, "▼"); Ver(x1, lowTop, low + pipe, f1, null);
            Ver(x2, tankBottom, up, f2, "▼"); Ver(x2, lowTop, low + pipe, f2, null);
            Ver(x3out, tankBottom, up, f4, "▼"); Ver(x3out, lowTop, low + pipe, f4, null);
            Ver(x4out, tankBottom, up, f6, "▼"); Ver(x4out, lowTop, low + pipe, f6, null);
            Ver(x3in, tankBottom, stub + pipe, f3, "▲");
            Ver(x4in, tankBottom, stub + pipe, f5, "▲");

            var ship = new Label
            {
                Text = "출하", TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White, ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text), Font = ApcTheme.GetFont(),
                Bounds = new Rectangle(xShip, pipeTop - Px(4), W - xShip - Px(2), pipe + Px(8)),
            };
            Controls.Add(ship);

            for (int i = 0; i < 4; i++) AddTank(_tanks[i], tankLeft[i], tw, tankTop, tankH, nameH);

            int stubMid = tankBottom + up, pipeMid = pipeTop + pipe / 2 - vh / 2;
            AddValve("V1", x1, stubMid); AddValve("V2", x2, stubMid);
            AddValve("V3", xV3, pipeMid); AddValve("V4", x3out, stubMid);
            AddValve("V5", xV5, pipeMid); AddValve("V6", x4out, stubMid);
            AddPump("P1", xP1, pipeMid); AddPump("P2", xP2, pipeMid);
            ResumeLayout();
        }

        private Label MakePipe(Rectangle r, bool flowing)
        {
            var l = new Label
            {
                Bounds = r, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter,
                BackColor = ApcTheme.Status(flowing ? StatusKind.Run : StatusKind.Stop),
                ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text), Font = ApcTheme.GetFont(),
            };
            Controls.Add(l);
            return l;
        }

        private void Hor(int x0, int x1, int top, bool flowing)
        {
            if (x1 <= x0) return;
            var l = MakePipe(new Rectangle(x0, top, x1 - x0, Px(PipeThick)), flowing);
            if (flowing)
            {
                int n = Math.Max(1, (x1 - x0) / Px(36));
                l.Text = string.Join("  ", System.Linq.Enumerable.Repeat("▶", n));
            }
        }

        private void Ver(int cx, int top, int length, bool flowing, string glyph)
        {
            int pipe = Px(PipeThick);
            var l = MakePipe(new Rectangle(cx - pipe / 2, top, pipe, length), flowing);
            if (flowing && glyph != null) l.Text = glyph;
            l.TextAlign = ContentAlignment.TopCenter;
        }

        private void AddTank(TankInfo t, int left, int tw, int top, int h, int nameH)
        {
            var color = ApcTheme.C(ApcTheme.Current.Colors.Text);
            Controls.Add(new Label
            {
                Text = t.Id + " " + t.Name, Font = ApcTheme.GetFont(FontStyle.Bold), ForeColor = color,
                Bounds = new Rectangle(left, 0, tw, nameH), TextAlign = ContentAlignment.MiddleLeft,
            });
            // 값마다 자기 판정 색: 레벨, 온도(T3만 판정)
            var lvKind = ProcessRules.LevelStatus(t.Level);
            var lv = new Label
            {
                Text = ProcessRules.Pct(t.Level), Font = ApcTheme.GetFont(FontStyle.Bold), AutoSize = true,
                ForeColor = ApcTheme.StatusText(lvKind), Location = new Point(left, nameH),
            };
            Controls.Add(lv);
            var tk = ProcessRules.TempStatus(t.Id, t.Temperature);
            Controls.Add(new Label
            {
                Text = ProcessRules.Temp(t.Temperature) + " ℃", Font = ApcTheme.GetFont(tk.HasValue ? FontStyle.Bold : FontStyle.Regular),
                AutoSize = true, ForeColor = tk.HasValue ? ApcTheme.StatusText(tk.Value) : color,
                Location = new Point(left + Px(80), nameH),
            });

            var frame = new Panel { Bounds = new Rectangle(left, top, tw, h), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            Controls.Add(frame);
            int inner = h - 2, iw = tw - 2;   // 테두리 1px씩
            int fillH = inner * t.Level / 100;
            frame.Controls.Add(new Label { Bounds = new Rectangle(0, inner - fillH, iw, fillH), BackColor = ApcTheme.Status(lvKind) });
            AddLine(frame, inner, iw, ProcessRules.LevelAlarm, StatusKind.Alarm);
            AddLine(frame, inner, iw, ProcessRules.LevelWarn, StatusKind.Warn);
            AddLine(frame, inner, iw, ProcessRules.LevelLow, StatusKind.Warn);
        }

        private static void AddLine(Panel frame, int inner, int iw, int level, StatusKind kind)
        {
            var l = new Label { Bounds = new Rectangle(0, inner - inner * level / 100 - 1, iw, Px(LineThick)), BackColor = ApcTheme.StatusText(kind) };
            frame.Controls.Add(l);
            l.BringToFront();
        }

        private void AddValve(string id, int cx, int top)
        {
            bool open = ValveOpen(id);
            int vw = Px(ValveW);
            var l = new Label
            {
                Text = id + (open ? " 열림" : " 닫힘"), TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle,
                BackColor = ApcTheme.Status(open ? StatusKind.Run : StatusKind.Stop),
                ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text), Font = ApcTheme.GetFont(FontStyle.Bold),
                Bounds = new Rectangle(cx - vw / 2, top, vw, Px(ValveH)), Cursor = Cursors.Hand,
            };
            l.Click += (s, e) => { var h = ValveClicked; if (h != null) h(id); };
            Controls.Add(l);
            l.BringToFront();
        }

        private void AddPump(string id, int cx, int top)
        {
            bool run = PumpRunning(id);
            int vw = Px(ValveW);
            var l = new Label
            {
                Text = id + (run ? " 가동" : " 정지"), TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle,
                BackColor = ApcTheme.Status(run ? StatusKind.Run : StatusKind.Stop),
                ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text), Font = ApcTheme.GetFont(FontStyle.Bold),
                Bounds = new Rectangle(cx - vw / 2, top, vw, Px(ValveH)),
            };
            Controls.Add(l);
            l.BringToFront();
        }
    }
}
