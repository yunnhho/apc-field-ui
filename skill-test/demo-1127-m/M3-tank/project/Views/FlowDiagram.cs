using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 공정 흐름도. 표준 Label을 Panel 안에 계산한 위치로 조립한다(OnPaint·Graphics 그리기 없음).
    /// 탱크 = 테두리 Label + 레벨 채움 Label, 배관 = 가는 Label(흐르면 Run색, 아니면 Stop색) + ▶▼ 글자, 밸브·펌프 = 상태색 Label + 상태 글자.
    /// 밸브 상자를 누르면 ValveClicked(밸브 번호).
    /// </summary>
    internal sealed class FlowDiagram : Panel
    {
        // 가로 위치(폭 비율): 탱크 중심, 밸브·펌프 중심
        private const double XRaw = 0.085, XV12 = 0.19, XMerge = 0.25, XP1 = 0.31, XV3 = 0.395, XT3 = 0.51,
            XV4 = 0.625, XP2 = 0.705, XV5 = 0.785, XT4 = 0.895;

        private IList<TankInfo> _tanks;
        private IList<ValveInfo> _valves;
        private IList<PumpInfo> _pumps;
        private List<Control> _front, _mid, _back;

        public event Action<string> ValveClicked;

        public FlowDiagram() { BackColor = Color.Transparent; }

        public void SetData(IList<TankInfo> tanks, IList<ValveInfo> valves, IList<PumpInfo> pumps)
        {
            _tanks = tanks; _valves = valves; _pumps = pumps;
            Rebuild();
        }

        protected override void OnResize(EventArgs eventargs) { base.OnResize(eventargs); Rebuild(); }

        private static int P(int px) { return ApcTheme.Px(px); }
        private static Color Ink { get { return ApcTheme.C(ApcTheme.Current.Colors.Text); } }

        private bool Open(string valveId)
        {
            foreach (var v in _valves) if (v.Id == valveId) return v.IsOpen;
            return false;
        }

        private ValveInfo Valve(string id)
        {
            foreach (var v in _valves) if (v.Id == id) return v;
            return null;
        }

        private void Rebuild()
        {
            if (_tanks == null || ClientSize.Width < P(300) || ClientSize.Height < P(300)) return;
            SuspendLayout();
            while (Controls.Count > 0) { var old = Controls[0]; Controls.Remove(old); old.Dispose(); }
            _front = new List<Control>(); _mid = new List<Control>(); _back = new List<Control>();

            int w = ClientSize.Width, h = ClientSize.Height;
            int pad = P(16), titleH = P(52), tw = P(100), gap = P(12);
            int tankTop = pad + titleH, tankBottom = h - pad - P(110);
            int section = (tankBottom - pad - gap) / 2;
            int top1 = pad + titleH, bottom1 = pad + section;
            int top2 = pad + section + gap + titleH, bottom2 = tankBottom;
            int yT1 = (top1 + bottom1) / 2, yT2 = (top2 + bottom2) / 2, yMain = (tankTop + tankBottom) / 2;
            int cRaw = (int)(w * XRaw), cT3 = (int)(w * XT3), cT4 = (int)(w * XT4);
            int xMerge = (int)(w * XMerge);
            int vw = P(48), vh = P(34), ps = P(52), pt = P(8);

            bool p1 = Pump("P1"), p2 = Pump("P2");
            bool f1 = p1 && Open("V1"), f2 = p1 && Open("V2"), fin = f1 || f2;
            bool fOut1 = fin && Open("V3");
            bool fIn2 = p2 && Open("V4"), fOut2 = fIn2 && Open("V5");
            var t4 = _tanks[3];
            bool f6 = Open("V6") && t4.Level > 0;

            Tank(_tanks[0], cRaw, top1, bottom1, tw, titleH);
            Tank(_tanks[1], cRaw, top2, bottom2, tw, titleH);
            Tank(_tanks[2], cT3, tankTop, tankBottom, tw, titleH);
            Tank(t4, cT4, tankTop, tankBottom, tw, titleH);

            int xV12 = (int)(w * XV12), xP1 = (int)(w * XP1), xV3 = (int)(w * XV3), xV4 = (int)(w * XV4),
                xP2 = (int)(w * XP2), xV5 = (int)(w * XV5);

            // 원료 → 밸브 → 합류 → P1
            HPipe(cRaw + tw / 2, xV12 - vw / 2, yT1, f1, false);
            HPipe(xV12 + vw / 2, xMerge + pt / 2, yT1, f1, true);
            HPipe(cRaw + tw / 2, xV12 - vw / 2, yT2, f2, false);
            HPipe(xV12 + vw / 2, xMerge + pt / 2, yT2, f2, true);
            VPipe(xMerge, yT1 + pt / 2, yMain - pt / 2, f1, "▼");
            VPipe(xMerge, yMain + pt / 2, yT2 - pt / 2, f2, "▲");
            HPipe(xMerge - pt / 2, xP1 - ps / 2, yMain, fin, true);
            HPipe(xP1 + ps / 2, xV3 - vw / 2, yMain, fOut1, true);
            HPipe(xV3 + vw / 2, cT3 - tw / 2, yMain, fOut1, true);
            // T3 → V4 → P2 → V5 → T4
            HPipe(cT3 + tw / 2, xV4 - vw / 2, yMain, fIn2, true);
            HPipe(xV4 + vw / 2, xP2 - ps / 2, yMain, fIn2, true);
            HPipe(xP2 + ps / 2, xV5 - vw / 2, yMain, fOut2, true);
            HPipe(xV5 + vw / 2, cT4 - tw / 2, yMain, fOut2, true);

            ValveBox(Valve("V1"), xV12, yT1, vw, vh);
            ValveBox(Valve("V2"), xV12, yT2, vw, vh);
            ValveBox(Valve("V3"), xV3, yMain, vw, vh);
            ValveBox(Valve("V4"), xV4, yMain, vw, vh);
            ValveBox(Valve("V5"), xV5, yMain, vw, vh);
            PumpBox("P1", p1, xP1, yMain, ps);
            PumpBox("P2", p2, xP2, yMain, ps);

            // T4 → V6 → 출하
            int v6cy = tankBottom + P(16) + vh / 2;
            VPipe(cT4, tankBottom, v6cy - vh / 2, f6, null);
            ValveBox(Valve("V6"), cT4, v6cy, vw, vh);
            AddText(f6 ? "▼ 출하" : "출하", cT4 - P(45), v6cy + vh / 2 + P(28), P(90), P(26), FontStyle.Bold);
            AddText("온도 기준 " + ProcessRules.BlendTempMin.ToString("0.0") + "~" + ProcessRules.Deg(ProcessRules.BlendTempMax),
                cT3 - P(110), tankBottom + P(8), P(220), P(24), FontStyle.Regular);

            foreach (var c in _front) Controls.Add(c);
            foreach (var c in _mid) Controls.Add(c);
            foreach (var c in _back) Controls.Add(c);
            ResumeLayout();
        }

        private bool Pump(string id)
        {
            foreach (var p in _pumps) if (p.Id == id) return p.Running;
            return false;
        }

        private Label AddText(string text, int x, int y, int w, int h, FontStyle style)
        {
            var l = new Label { Text = text, Bounds = new Rectangle(x, y, w, h), TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, BackColor = Color.Transparent, ForeColor = Ink, Font = ApcTheme.GetFont(style) };
            _front.Add(l);
            return l;
        }

        private void Tank(TankInfo t, int cx, int top, int bottom, int tw, int titleH)
        {
            var levelKind = ProcessRules.LevelKind(t.Level);
            var kind = ProcessRules.Worse(levelKind, ProcessRules.TempKind(t));
            int lw = P(150), lh = P(26);
            AddText(t.Name, cx - lw / 2, top - titleH, lw, lh, FontStyle.Bold);
            var value = AddText(ProcessRules.Pct(t.Level) + " · " + ProcessRules.Deg(t.Temp), cx - lw / 2, top - lh, lw, lh, FontStyle.Bold);
            value.ForeColor = ApcTheme.StatusText(kind);

            TankBody(t.Level, levelKind, cx - tw / 2, top, tw, bottom - top);
        }

        // 탱크 몸통: 테두리 4개 + 안쪽을 알람선(90 %)·채움 경계에서 잘라 겹치지 않게 조각 Label로 쌓는다.
        private void TankBody(int level, StatusKind levelKind, int x, int top, int tw, int th)
        {
            int bw = Math.Max(2, P(2)), lineH = Math.Max(2, P(2));
            var edge = ApcTheme.C(ApcTheme.Current.Colors.BorderStrong ?? ApcTheme.Current.Colors.Border);
            _back.Add(Block(x, top, tw, bw, edge));
            _back.Add(Block(x, top + th - bw, tw, bw, edge));
            _back.Add(Block(x, top + bw, bw, th - 2 * bw, edge));
            _back.Add(Block(x + tw - bw, top + bw, bw, th - 2 * bw, edge));

            int ix = x + bw, iw = tw - 2 * bw, iTop = top + bw, iBottom = top + th - bw, inner = iBottom - iTop;
            int yLine = iTop + inner * (100 - ProcessRules.LevelAlarm) / 100;
            int yFill = iBottom - (int)(inner * Math.Min(100, level) / 100.0);
            var cuts = new List<int> { iTop, yLine, yLine + lineH, yFill, iBottom };
            cuts.Sort();
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                int a = cuts[i], b = cuts[i + 1];
                if (b <= a) continue;
                Color c = a == yLine ? ApcTheme.Status(StatusKind.Alarm) : a >= yFill ? ApcTheme.Status(levelKind) : Color.White;
                _mid.Add(Block(ix, a, iw, b - a, c));
            }
        }

        private static Label Block(int x, int y, int w, int h, Color c)
        {
            return new Label { AutoSize = false, Bounds = new Rectangle(x, y, w, h), BackColor = c };
        }

        private void HPipe(int x1, int x2, int y, bool flow, bool arrow)
        {
            int t = P(8);
            if (x2 <= x1) return;
            _back.Add(new Label { AutoSize = false, Bounds = new Rectangle(x1, y - t / 2, x2 - x1, t), BackColor = ApcTheme.Status(flow ? StatusKind.Run : StatusKind.Stop) });
            if (flow && arrow && x2 - x1 >= P(26))
                AddText("▶", (x1 + x2) / 2 - P(11), y - t / 2 - P(24), P(22), P(22), FontStyle.Regular);
        }

        private void VPipe(int x, int y1, int y2, bool flow, string arrow)
        {
            int t = P(8);
            if (y2 <= y1) return;
            _back.Add(new Label { AutoSize = false, Bounds = new Rectangle(x - t / 2, y1, t, y2 - y1), BackColor = ApcTheme.Status(flow ? StatusKind.Run : StatusKind.Stop) });
            if (flow && arrow != null && y2 - y1 >= P(26))
                AddText(arrow, x + t, (y1 + y2) / 2 - P(11), P(22), P(22), FontStyle.Regular);
        }

        private void ValveBox(ValveInfo v, int cx, int cy, int vw, int vh)
        {
            var box = new Label { Text = v.Id, AutoSize = false, Bounds = new Rectangle(cx - vw / 2, cy - vh / 2, vw, vh), TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle, Font = ApcTheme.GetFont(FontStyle.Bold), ForeColor = Ink, Cursor = Cursors.Hand,
                BackColor = ApcTheme.Status(v.IsOpen ? StatusKind.Run : StatusKind.Stop) };
            string id = v.Id;
            box.Click += (s, e) => { var h = ValveClicked; if (h != null) h(id); };
            _front.Add(box);
            AddText(v.IsOpen ? "열림" : "닫힘", cx - vw / 2, cy + vh / 2 + P(2), vw, P(22), FontStyle.Regular);
        }

        private void PumpBox(string id, bool running, int cx, int cy, int size)
        {
            _front.Add(new Label { Text = id, AutoSize = false, Bounds = new Rectangle(cx - size / 2, cy - size / 2, size, size), TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle, Font = ApcTheme.GetFont(FontStyle.Bold), ForeColor = Ink,
                BackColor = ApcTheme.Status(running ? StatusKind.Run : StatusKind.Stop) });
            AddText(running ? "가동" : "정지", cx - size / 2, cy + size / 2 + P(2), size, P(22), FontStyle.Regular);
        }
    }
}
