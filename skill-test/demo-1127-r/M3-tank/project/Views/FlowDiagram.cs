using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 공정 흐름도: 표준 Label·Panel을 설계 좌표(960x350)로 배치해 조립한다(OnPaint 그리기 없음).
    /// 색은 모두 ApcTheme 상태색. 상태 갱신은 Update로, 밸브를 누르면 ValveClicked.
    /// </summary>
    internal sealed class FlowDiagram : Panel
    {
        private const float DesignW = 960f;
        private const float DesignH = 350f;
        private const float PipeThickness = 6f;
        private const float FrameTop = 50f, FrameHeight = 200f, FrameWidth = 100f;
        private static readonly Color Ink = Color.FromArgb(30, 30, 30);

        private sealed class Item
        {
            public Control C; public float X, Y, W, H;
        }

        private sealed class TankParts
        {
            public string Id;
            public Label Name, Value, Temp;
            public Panel Frame;
            public Label Fill, WarnLine, AlarmLine;
        }

        private readonly List<Item> _items = new List<Item>();
        private readonly List<TankParts> _tanks = new List<TankParts>();
        private readonly Dictionary<string, Label> _valves = new Dictionary<string, Label>();
        private readonly Dictionary<string, Label> _pumps = new Dictionary<string, Label>();
        private readonly List<KeyValuePair<Label, Func<bool>>> _flowParts = new List<KeyValuePair<Label, Func<bool>>>();
        private readonly Dictionary<string, bool> _open = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _running = new Dictionary<string, bool>();
        private string _selected = "";
        private bool _built;

        public event Action<string> ValveClicked;

        public FlowDiagram()
        {
            BackColor = SystemColors.Control;
            Font = ApcTheme.GetFont();
        }

        public void Update(IList<TankInfo> tanks, IList<ValveInfo> valves, IList<PumpInfo> pumps)
        {
            if (!_built) Build();
            foreach (ValveInfo v in valves) _open[v.Id] = v.Open;
            foreach (PumpInfo p in pumps) _running[p.Id] = p.Running;
            foreach (TankParts t in _tanks) ShowTank(t, ProcessRules.FindTank(tanks, t.Id));
            foreach (ValveInfo v in valves) ShowValve(v);
            foreach (PumpInfo p in pumps)
            {
                Label l = _pumps[p.Id];
                l.Text = p.Id + "\r\n" + (p.Running ? "가동" : "정지");
                l.BackColor = ApcTheme.Status(p.Running ? StatusKind.Run : StatusKind.Stop);
            }
            foreach (KeyValuePair<Label, Func<bool>> f in _flowParts) ShowFlow(f.Key, f.Value());
            PlaceAll();
        }

        public void SetSelected(string valveId)
        {
            _selected = valveId ?? "";
            foreach (KeyValuePair<string, Label> kv in _valves)
            {
                bool sel = kv.Key == _selected;
                kv.Value.BorderStyle = sel ? BorderStyle.Fixed3D : BorderStyle.FixedSingle;
                kv.Value.Font = ApcTheme.GetFont(sel ? FontStyle.Bold : FontStyle.Regular);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PlaceAll();
        }

        // ---- 조립 ----
        private void Build()
        {
            _built = true;
            AddTank("T1", "원료 탱크", 30);
            AddTank("T2", "원료 탱크", 170);
            AddTank("T3", "배합 탱크", 470);
            AddTank("T4", "저장 탱크", 810);

            Func<bool> feed1 = () => Runs("P1") && IsOpen("V1");
            Func<bool> feed2 = () => Runs("P1") && IsOpen("V2");
            Func<bool> any = () => feed1() || feed2();
            Func<bool> toT3 = () => any() && IsOpen("V3");
            Func<bool> b1 = () => Runs("P2") && IsOpen("V4");
            Func<bool> b2 = () => b1() && IsOpen("V5");
            Func<bool> ship = () => IsOpen("V6");

            AddPipe(80, 250, 80, 300, feed1);
            AddPipe(220, 250, 220, 300, feed2);
            AddPipe(80, 300, 220, 300, feed1);
            AddPipe(220, 300, 330, 300, any);
            AddPipe(330, 300, 520, 300, toT3);
            AddPipe(520, 250, 520, 300, toT3);
            AddPipe(570, 225, 695, 225, b1);
            AddPipe(695, 225, 810, 225, b2);
            AddPipe(860, 250, 860, 300, ship);
            AddPipe(860, 300, 915, 300, ship);

            AddArrow(150, 285, "▶", feed1);
            AddArrow(285, 285, "▶", any);
            AddArrow(480, 285, "▶", toT3);
            AddArrow(536, 271, "▲", toT3);
            AddArrow(646, 198, "▶", b1);
            AddArrow(903, 285, "▶", ship);

            AddValve("V1", 80, 275); AddValve("V2", 220, 275); AddValve("V3", 410, 300);
            AddValve("V4", 612, 225); AddValve("V5", 768, 225); AddValve("V6", 860, 275);
            AddPump("P1", 330, 300); AddPump("P2", 695, 225);

            Label ship2 = NewLabel("출하", ContentAlignment.MiddleCenter);
            ship2.BorderStyle = BorderStyle.FixedSingle;
            Place(ship2, 915, 288, 44, 24);
        }

        private bool IsOpen(string id) { bool v; return _open.TryGetValue(id, out v) && v; }
        private bool Runs(string id) { bool v; return _running.TryGetValue(id, out v) && v; }

        private Label NewLabel(string text, ContentAlignment align)
        {
            var l = new Label { Text = text, TextAlign = align, AutoSize = false, Font = ApcTheme.GetFont(), ForeColor = Ink };
            return l;
        }

        private void Place(Control c, float x, float y, float w, float h)
        {
            _items.Add(new Item { C = c, X = x, Y = y, W = w, H = h });
            Controls.Add(c);
        }

        private void AddTank(string id, string name, float left)
        {
            var t = new TankParts { Id = id };
            float cx = left + FrameWidth / 2;
            t.Name = NewLabel(id + " " + name, ContentAlignment.MiddleCenter);
            t.Value = NewLabel("", ContentAlignment.MiddleRight);
            t.Temp = NewLabel("", ContentAlignment.MiddleLeft);
            Place(t.Name, cx - 68, 2, 136, 24);
            Place(t.Value, cx - 72, 26, 68, 24);
            Place(t.Temp, cx - 2, 26, 68, 24);
            t.Frame = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            t.Fill = new Label { AutoSize = false, Text = "" };
            t.WarnLine = new Label { AutoSize = false, Text = "", BackColor = ApcTheme.Status(StatusKind.Warn) };
            t.AlarmLine = new Label { AutoSize = false, Text = "", BackColor = ApcTheme.Status(StatusKind.Alarm) };
            t.Frame.Controls.Add(t.Fill);
            t.Frame.Controls.Add(t.WarnLine);
            t.Frame.Controls.Add(t.AlarmLine);
            t.WarnLine.BringToFront();
            t.AlarmLine.BringToFront();
            Place(t.Frame, left, FrameTop, FrameWidth, FrameHeight);
            _tanks.Add(t);
        }

        private void AddPipe(float x1, float y1, float x2, float y2, Func<bool> flowing)
        {
            var l = new Label { AutoSize = false, Text = "" };
            float t = PipeThickness, h = t / 2;
            Place(l, Math.Min(x1, x2) - h, Math.Min(y1, y2) - h, Math.Abs(x2 - x1) + t, Math.Abs(y2 - y1) + t);
            _flowParts.Add(new KeyValuePair<Label, Func<bool>>(l, flowing));
        }

        private void AddArrow(float cx, float cy, string glyph, Func<bool> flowing)
        {
            var l = NewLabel(glyph, ContentAlignment.MiddleCenter);
            l.BackColor = BackColor;
            l.Tag = glyph;
            Place(l, cx - 15, cy - 9, 30, 18);
            _flowParts.Add(new KeyValuePair<Label, Func<bool>>(l, flowing));
        }

        private void AddValve(string id, float cx, float cy)
        {
            Label l = NewLabel(id, ContentAlignment.MiddleCenter);
            l.BorderStyle = BorderStyle.FixedSingle;
            l.Cursor = Cursors.Hand;
            string vid = id;
            l.Click += delegate { Action<string> h = ValveClicked; if (h != null) h(vid); };
            _valves[id] = l;
            Place(l, cx - 40, cy - 14, 80, 28);
        }

        private void AddPump(string id, float cx, float cy)
        {
            Label l = NewLabel(id, ContentAlignment.MiddleCenter);
            l.BorderStyle = BorderStyle.FixedSingle;
            _pumps[id] = l;
            Place(l, cx - 28, cy - 23, 56, 46);
        }

        // ---- 상태 반영 ----
        private void ShowTank(TankParts t, TankInfo info)
        {
            if (info == null) return;
            StatusKind lk = ProcessRules.LevelKind(info.Level);
            StatusKind tk = ProcessRules.TempOut(info) ? StatusKind.Alarm : StatusKind.Run;
            t.Name.Text = info.Id + " " + info.Name;
            t.Name.Font = ApcTheme.GetFont(FontStyle.Bold);
            SetValueText(t.Value, ProcessRules.LevelText(info.Level) + "  ·", lk);
            SetValueText(t.Temp, ProcessRules.TempText(info.Temp), tk);
            t.Fill.BackColor = ApcTheme.Status(lk == StatusKind.Run ? StatusKind.Info : lk);
            t.Fill.Tag = info.Level;
        }

        private static void SetValueText(Label l, string text, StatusKind kind)
        {
            l.Text = text;
            l.ForeColor = kind == StatusKind.Run ? Ink : ApcTheme.StatusText(kind);
            l.Font = ApcTheme.GetFont(kind == StatusKind.Run ? FontStyle.Regular : FontStyle.Bold);
        }

        private void ShowValve(ValveInfo v)
        {
            Label l = _valves[v.Id];
            l.Text = v.Id + " " + (v.Open ? "열림" : "닫힘");
            l.BackColor = ApcTheme.Status(v.Open ? StatusKind.Run : StatusKind.Stop);
        }

        private static void ShowFlow(Label l, bool flowing)
        {
            if (l.Tag is string)
            {
                l.Text = flowing ? (string)l.Tag : "";
                l.Visible = flowing;
                return;
            }
            l.BackColor = ApcTheme.Status(flowing ? StatusKind.Run : StatusKind.Stop);
        }

        // ---- 배치(설계 좌표 → 실제 크기) ----
        private void PlaceAll()
        {
            if (!_built || Width <= 0 || Height <= 0) return;
            float f = Math.Min(Width / DesignW, Height / DesignH);
            float ox = (Width - DesignW * f) / 2, oy = (Height - DesignH * f) / 2;
            foreach (Item it in _items)
                it.C.SetBounds((int)(ox + it.X * f), (int)(oy + it.Y * f), Math.Max(1, (int)(it.W * f)), Math.Max(1, (int)(it.H * f)));
            foreach (TankParts t in _tanks) PlaceLevel(t);
        }

        private static void PlaceLevel(TankParts t)
        {
            int cw = t.Frame.ClientSize.Width, ch = t.Frame.ClientSize.Height;
            double level = t.Fill.Tag is double ? (double)t.Fill.Tag : 0;
            int fh = (int)Math.Round(ch * level / 100.0);
            t.Fill.SetBounds(0, ch - fh, cw, fh);
            int line = Math.Max(2, ApcTheme.Px(2));
            t.WarnLine.SetBounds(0, (int)Math.Round(ch * (1 - ProcessRules.LevelWarn / 100.0)), cw, line);
            t.AlarmLine.SetBounds(0, (int)Math.Round(ch * (1 - ProcessRules.LevelAlarm / 100.0)) - line, cw, line);
        }
    }
}
