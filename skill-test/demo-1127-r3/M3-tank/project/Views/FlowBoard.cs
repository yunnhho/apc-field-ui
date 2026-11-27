using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 공정 흐름도: 표준 Label/Panel만 배치한다(그리기 없음). 탱크 4 · 밸브 6 · 펌프 2 · 배관 조각.
    /// 위치·크기는 Resize에서 계산하고, 값은 SetState로 바꾼다.
    /// </summary>
    internal sealed class FlowBoard : Panel
    {
        private const int TitleLines = 2;
        private static readonly string[] TankIds = { "T1", "T2", "T3", "T4" };
        private static readonly string[] ValveIds = { "V1", "V2", "V3", "V4", "V5", "V6" };

        private sealed class TankParts
        {
            public Panel Frame; public Label Fill, AlarmLine, Name, Level, Temp;
        }

        private readonly Dictionary<string, TankParts> _tanks = new Dictionary<string, TankParts>();
        private readonly Dictionary<string, Label> _valves = new Dictionary<string, Label>();
        private readonly Dictionary<string, Label> _pumps = new Dictionary<string, Label>();
        private readonly Dictionary<string, Label> _pipes = new Dictionary<string, Label>();
        private readonly Dictionary<string, Label> _arrows = new Dictionary<string, Label>();
        private readonly Label _ship = new Label();
        private ProcessSnapshot _snap;

        public event EventHandler<ValveEventArgs> ValveClicked;

        public FlowBoard()
        {
            BackColor = Color.Transparent;
            foreach (string id in TankIds) _tanks[id] = MakeTank();
            foreach (string id in ValveIds) _valves[id] = MakeBox(id, true);
            foreach (string id in new[] { "P1", "P2" }) _pumps[id] = MakeBox(id, false);
            foreach (string id in new[] { "d1", "d1b", "d2", "d2b", "hA", "hB", "hC", "hD", "r3", "d3", "d3b", "hE", "hF", "hG", "r4", "d4", "d4b", "hH" })
            {
                _pipes[id] = new Label { AutoSize = false, Text = "" };
                Controls.Add(_pipes[id]);
            }
            foreach (string id in new[] { "hB", "hD", "r3", "hE", "hG", "r4", "hH" })
            {
                var a = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = ApcTheme.GetFont(FontStyle.Bold), ForeColor = Color.White };
                a.Text = id.StartsWith("r") ? "▲" : "▶";
                _arrows[id] = a;
                Controls.Add(a);
            }
            _ship.AutoSize = false; _ship.Text = "출하"; _ship.TextAlign = ContentAlignment.MiddleCenter;
            _ship.Font = ApcTheme.GetFont(); _ship.BorderStyle = BorderStyle.FixedSingle;
            _ship.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text);
            Controls.Add(_ship);
            foreach (var v in _valves.Values) v.BringToFront();
            foreach (var p in _pumps.Values) p.BringToFront();
            Resize += (s, e) => LayoutBoard();
        }

        private TankParts MakeTank()
        {
            var t = new TankParts();
            t.Frame = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            t.Fill = new Label { AutoSize = false, Text = "" };
            t.AlarmLine = new Label { AutoSize = false, Text = "", BackColor = ApcTheme.Status(StatusKind.Alarm) };
            t.Frame.Controls.Add(t.Fill);
            t.Frame.Controls.Add(t.AlarmLine);
            t.AlarmLine.BringToFront();
            t.Name = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Font = ApcTheme.GetFont() };
            t.Level = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Font = ApcTheme.GetFont(FontStyle.Bold) };
            t.Temp = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Font = ApcTheme.GetFont(FontStyle.Bold) };
            Controls.Add(t.Frame); Controls.Add(t.Name); Controls.Add(t.Level); Controls.Add(t.Temp);
            return t;
        }

        private Label MakeBox(string id, bool clickable)
        {
            var l = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, BorderStyle = BorderStyle.FixedSingle,
                Font = ApcTheme.GetFont(), ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text), Tag = id };
            if (clickable)
            {
                l.Cursor = Cursors.Hand;
                l.Click += (s, e) => { var h = ValveClicked; if (h != null) h(this, new ValveEventArgs(id)); };
            }
            Controls.Add(l);
            return l;
        }

        public void SetState(ProcessSnapshot snap)
        {
            _snap = snap;
            foreach (TankInfo t in snap.Tanks)
            {
                TankParts p = _tanks[t.Id];
                p.Name.Text = t.Name;
                p.Level.Text = ProcessRules.Level(t.Level) + " %";
                p.Temp.Text = ProcessRules.Temp(t.Temp) + " ℃";
                p.Level.ForeColor = ApcTheme.StatusText(ProcessRules.LevelKind(t.Level));
                p.Temp.ForeColor = ApcTheme.StatusText(ProcessRules.TempKind(t.Id, t.Temp));
                p.Fill.BackColor = ApcTheme.Status(ProcessRules.LevelKind(t.Level));
            }
            foreach (ValveInfo v in snap.Valves)
            {
                _valves[v.Id].Text = v.Id + " " + (v.IsOpen ? "열림" : "닫힘");
                _valves[v.Id].BackColor = ApcTheme.Status(v.IsOpen ? StatusKind.Run : StatusKind.Stop);
            }
            foreach (PumpInfo p in snap.Pumps)
            {
                _pumps[p.Id].Text = p.Id + " " + (p.Running ? "가동" : "정지");
                _pumps[p.Id].BackColor = ApcTheme.Status(p.Running ? StatusKind.Run : StatusKind.Stop);
            }
            ApplyFlow();
            LayoutBoard();
        }

        private void ApplyFlow()
        {
            bool v1 = snapOpen("V1"), v2 = snapOpen("V2"), v3 = snapOpen("V3"), v4 = snapOpen("V4"), v5 = snapOpen("V5"), v6 = snapOpen("V6");
            bool p1 = _snap.Pump("P1").Running, p2 = _snap.Pump("P2").Running;
            bool in1 = v1 && p1, in2 = v2 && p1, out1 = p1 && (v1 || v2);
            bool f2 = v4 && p2;
            bool ship = v6 && _snap.Tank("T4").Level > 0;
            var flow = new Dictionary<string, bool>
            {
                { "d1", in1 }, { "d2", in2 }, { "hA", in1 }, { "hB", in1 || in2 }, { "hC", out1 }, { "hD", out1 && v3 },
                { "r3", out1 && v3 }, { "d3", f2 }, { "hE", f2 }, { "hF", f2 }, { "hG", f2 && v5 }, { "r4", f2 && v5 },
                { "d4", ship }, { "hH", ship },
            };
            foreach (string d in new[] { "d1", "d2", "d3", "d4" }) flow[d + "b"] = flow[d];
            foreach (var kv in flow)
            {
                _pipes[kv.Key].BackColor = ApcTheme.Status(kv.Value ? StatusKind.Run : StatusKind.Stop);
                Label a;
                if (_arrows.TryGetValue(kv.Key, out a)) a.Visible = kv.Value;
            }
        }

        private bool snapOpen(string valveId) { return _snap.Valve(valveId).IsOpen; }

        private void LayoutBoard()
        {
            if (_snap == null || Width < 50 || Height < 50) return;
            int W = Width, totalH = Height;
            int pipeT = ApcTheme.Px(6), boxW = ApcTheme.Px(72), boxH = ApcTheme.Px(34);
            int lineH = ApcTheme.Px(24), titleH = lineH * TitleLines;
            int dropH = boxH + ApcTheme.Px(20);
            int tw = Math.Max(ApcTheme.Px(110), (int)(W * 0.115));
            int tankTop = titleH, th = Math.Max(ApcTheme.Px(60), totalH - titleH - dropH - boxH / 2 - ApcTheme.Px(2));
            int tankBot = tankTop + th, hy = tankBot + dropH;

            int[] tx = { X(W, 0.01), X(W, 0.14), X(W, 0.45), X(W, 0.76) };
            for (int i = 0; i < TankIds.Length; i++) PlaceTank(_tanks[TankIds[i]], _snap.Tank(TankIds[i]), tx[i], tankTop, tw, th, lineH);

            int d1 = tx[0] + tw * 3 / 4, d2 = tx[1] + tw * 3 / 4;
            int i3 = tx[2] + tw * 22 / 100, o3 = tx[2] + tw * 78 / 100;
            int i4 = tx[3] + tw * 22 / 100, o4 = tx[3] + tw * 78 / 100;
            int p1x = X(W, 0.31), v3x = X(W, 0.40), p2x = X(W, 0.635), v5x = X(W, 0.72);
            int shipX = X(W, 0.90), shipW = Math.Min(W - shipX, ApcTheme.Px(90));
            int half = boxW / 2, mid = (tankBot + hy) / 2;

            Drop("d1", d1, tankBot, mid, hy, boxH); Drop("d2", d2, tankBot, mid, hy, boxH);
            H("hA", d1, d2, hy); H("hB", d2, p1x - half, hy); H("hC", p1x + half, v3x - half, hy); H("hD", v3x + half, i3, hy);
            V("r3", i3, tankBot, i3, hy);
            Drop("d3", o3, tankBot, mid, hy, boxH); H("hE", o3, p2x - half, hy); H("hF", p2x + half, v5x - half, hy); H("hG", v5x + half, i4, hy);
            V("r4", i4, tankBot, i4, hy);
            Drop("d4", o4, tankBot, mid, hy, boxH); H("hH", o4, shipX, hy);

            Box(_valves["V1"], d1, mid, boxW, boxH); Box(_valves["V2"], d2, mid, boxW, boxH);
            Box(_valves["V3"], v3x, hy, boxW, boxH); Box(_valves["V4"], o3, mid, boxW, boxH);
            Box(_valves["V5"], v5x, hy, boxW, boxH); Box(_valves["V6"], o4, mid, boxW, boxH);
            Box(_pumps["P1"], p1x, hy, boxW, boxH); Box(_pumps["P2"], p2x, hy, boxW, boxH);
            _ship.SetBounds(shipX, hy - boxH / 2, shipW, boxH);
            foreach (string id in new[] { "hB", "hD", "hE", "hG", "hH" }) PlaceArrow(id, true);
            foreach (string id in new[] { "r3", "r4" }) PlaceArrow(id, false);
            foreach (Label pipe in _pipes.Values) pipe.SendToBack();
        }

        private static int X(int w, double f) { return (int)(w * f); }

        private void H(string key, int x1, int x2, int y)
        {
            _pipes[key].SetBounds(x1, y - ApcTheme.Px(3), Math.Max(0, x2 - x1), ApcTheme.Px(6));
        }

        private void V(string key, int x, int y1, int x2, int y2)
        {
            _pipes[key].SetBounds(x - ApcTheme.Px(3), y1, ApcTheme.Px(6), y2 - y1 + ApcTheme.Px(3));
        }

        // 밸브 상자 위·아래로 나눈 세로 배관(상자 글자를 가리지 않게)
        private void Drop(string key, int x, int top, int mid, int hy, int boxH)
        {
            V(key, x, top, x, mid - boxH / 2);
            V(key + "b", x, mid + boxH / 2, x, hy);
        }

        private void PlaceArrow(string key, bool horizontal)
        {
            Rectangle r = _pipes[key].Bounds;
            int size = ApcTheme.Px(30), aw = size + ApcTheme.Px(14);
            Label a = _arrows[key];
            a.SetBounds(key == "hH" ? r.Right - aw : r.X + (r.Width - aw) / 2, r.Y + (r.Height - size) / 2, horizontal ? aw : size, size);
            a.BackColor = _pipes[key].BackColor;
            a.BringToFront();
        }

        private static void Box(Label l, int cx, int cy, int w, int h) { l.SetBounds(cx - w / 2, cy - h / 2, w, h); l.BringToFront(); }

        private void PlaceTank(TankParts p, TankInfo t, int x, int top, int w, int h, int lineH)
        {
            p.Name.SetBounds(x, 0, w, lineH);
            int gap = ApcTheme.Px(8), pad = ApcTheme.Px(12);
            int lw = TextRenderer.MeasureText(p.Level.Text, p.Level.Font).Width + pad;
            int tw = TextRenderer.MeasureText(p.Temp.Text, p.Temp.Font).Width + pad;
            p.Level.SetBounds(x, lineH, lw, lineH);
            p.Temp.SetBounds(x + lw + gap, lineH, tw, lineH);
            p.Frame.SetBounds(x, top, w, h);
            Size inner = p.Frame.ClientSize;
            int fillH = (int)Math.Round(inner.Height * Math.Min(100, Math.Max(0, t.Level)) / 100.0);
            p.Fill.SetBounds(0, inner.Height - fillH, inner.Width, fillH);
            int lineY = (int)Math.Round(inner.Height * (100 - ProcessRules.LevelAlarm) / 100.0);
            p.AlarmLine.SetBounds(0, lineY, inner.Width, ApcTheme.Px(2));
            p.AlarmLine.BringToFront();
        }
    }

    internal sealed class ValveEventArgs : EventArgs
    {
        public ValveEventArgs(string valveId) { ValveId = valveId; }
        public string ValveId { get; private set; }
    }
}
