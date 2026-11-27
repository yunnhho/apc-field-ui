using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;
using TankFlow.Data;

namespace TankFlow.Views
{
    /// <summary>
    /// 공정 흐름도. 표준 Label/Panel만 배치한다(그리기 없음). 탱크·밸브·펌프 위치는 ApcLayout.Slots에서 얻는다.
    /// 열 4 x 행 2 칸: T1/T2는 1열 위·아래, T3·T4는 두 행에 걸친 한 칸, 마지막 열은 출하.
    /// </summary>
    internal sealed class FlowBoard : Panel
    {
        private const double TankWidthShare = 0.30;   // 칸 폭 대비 탱크 폭
        private const double TankHeightShare = 0.96;
        private const double ItemFill = 0.7;          // 밸브·펌프·화살표 칸 대비 크기
        private const int TitleLines = 3, TitleLineHeight = 24, PipeThickness = 8, LineThickness = 2;   // 공통 px(Scale 곱함)
        private const string Arrow = "▶";

        private sealed class TankParts
        {
            public Panel Frame; public Label Fill, AlarmLine, Name, Level, Temp;
        }

        private readonly Dictionary<string, TankParts> _tanks = new Dictionary<string, TankParts>();
        private readonly Dictionary<string, Label> _valves = new Dictionary<string, Label>();
        private readonly Dictionary<string, Label> _pumps = new Dictionary<string, Label>();
        private readonly Label[] _pipes = new Label[6];     // 0 T1→P1, 1 T2→P1, 2 P1 세로, 3 P1→T3, 4 T3→T4, 5 T4→출하
        private readonly Label[] _arrows = new Label[6];    // 같은 순서(2번 없음)
        private readonly Label _ship;
        private IList<TankInfo> _tankData;
        private IList<ValveInfo> _valveData;
        private IList<PumpInfo> _pumpData;

        public event Action<string> ValveClicked;

        public FlowBoard()
        {
            for (int i = 0; i < _pipes.Length; i++)
            {
                _pipes[i] = new Label { AutoSize = false };
                Controls.Add(_pipes[i]);
                _arrows[i] = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = ApcTheme.GetFont(FontStyle.Bold) };
                Controls.Add(_arrows[i]);
            }
            _ship = new Label { AutoSize = false, Text = "출하", TextAlign = ContentAlignment.MiddleCenter, Font = ApcTheme.GetFont(FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, ForeColor = TextColor() };
            Controls.Add(_ship);
        }

        private static Color TextColor() { return ApcTheme.C(ApcTheme.Current.Colors.Text); }

        public void Bind(IList<TankInfo> tanks, IList<ValveInfo> valves, IList<PumpInfo> pumps)
        {
            _tankData = tanks; _valveData = valves; _pumpData = pumps;
            foreach (var t in tanks) if (!_tanks.ContainsKey(t.Id)) CreateTank(t.Id);
            foreach (var v in valves) if (!_valves.ContainsKey(v.Id)) CreateItem(_valves, v.Id, true);
            foreach (var p in pumps) if (!_pumps.ContainsKey(p.Id)) CreateItem(_pumps, p.Id, false);
            ApplyLayout();
        }

        private void CreateTank(string id)
        {
            var parts = new TankParts();
            parts.Frame = new Panel { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            parts.Fill = new Label { AutoSize = false };
            parts.AlarmLine = new Label { AutoSize = false, BackColor = ApcTheme.Status(StatusKind.Alarm) };
            parts.Frame.Controls.Add(parts.Fill);
            parts.Frame.Controls.Add(parts.AlarmLine);
            parts.Name = TitleLabel(FontStyle.Bold);
            parts.Level = TitleLabel(FontStyle.Bold);
            parts.Temp = TitleLabel(FontStyle.Regular);
            Controls.Add(parts.Frame);
            Controls.Add(parts.Name); Controls.Add(parts.Level); Controls.Add(parts.Temp);
            _tanks[id] = parts;
        }

        private static Label TitleLabel(FontStyle style)
        {
            return new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = ApcTheme.GetFont(style), ForeColor = TextColor() };
        }

        private void CreateItem(Dictionary<string, Label> map, string id, bool clickable)
        {
            var l = new Label { AutoSize = false, Text = id, TextAlign = ContentAlignment.TopCenter, Font = ApcTheme.GetFont(FontStyle.Bold),
                ForeColor = TextColor(), BorderStyle = BorderStyle.FixedSingle };
            if (clickable)
            {
                l.Cursor = Cursors.Hand;
                l.Click += (s, e) => { var h = ValveClicked; if (h != null) h(id); };
            }
            Controls.Add(l);
            map[id] = l;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyLayout();
        }

        private static int Px(int v) { return ApcTheme.Px(v); }

        private static Rectangle Shrink(Rectangle cell)
        {
            int w = (int)(cell.Width * TankWidthShare), h = (int)(cell.Height * TankHeightShare);
            return new Rectangle(cell.X + (cell.Width - w) / 2, cell.Y + (cell.Height - h) / 2, w, h);
        }

        private void ApplyLayout()
        {
            if (_tankData == null || ClientSize.Width < 10 || ClientSize.Height < 10) return;
            SuspendLayout();
            var cells = ApcLayout.Slots(ClientRectangle, 4, 2, 1.0, false);   // 행 단위: 0~3 위, 4~7 아래
            Rectangle t1 = Shrink(cells[0]), t2 = Shrink(cells[4]);
            Rectangle t3 = Rectangle.Union(Shrink(cells[1]), Shrink(cells[5]));
            Rectangle t4 = Rectangle.Union(Shrink(cells[2]), Shrink(cells[6]));
            Rectangle ship = Shrink(cells[3]);
            int titleH = Px(TitleLineHeight) * TitleLines;
            PlaceTank("T1", t1, titleH); PlaceTank("T2", t2, titleH); PlaceTank("T3", t3, titleH); PlaceTank("T4", t4, titleH);
            int yT1 = t1.Y + titleH + (t1.Height - titleH) / 2, yT2 = t2.Y + titleH + (t2.Height - titleH) / 2;
            int yMid = (yT1 + yT2) / 2;
            _ship.Bounds = new Rectangle(ship.X, yMid - Px(TitleLineHeight), ship.Width, Px(TitleLineHeight) * 2);

            // 탱크 사이 칸을 3등분: 밸브 · 펌프 · 밸브 (화살표는 항목 사이, 배관 위)
            var g1 = ApcLayout.Slots(Rectangle.FromLTRB(t1.Right, ClientRectangle.Top, t3.Left, ClientRectangle.Bottom), 3, 1, ItemFill, true);
            var g2 = ApcLayout.Slots(Rectangle.FromLTRB(t3.Right, ClientRectangle.Top, t4.Left, ClientRectangle.Bottom), 3, 1, ItemFill, true);
            var g3 = ApcLayout.Slots(Rectangle.FromLTRB(t4.Right, ClientRectangle.Top, ship.Left, ClientRectangle.Bottom), 3, 1, ItemFill, true);
            int s = g1[0].Width;
            int xPump = g1[1].X;   // 세로 배관은 펌프 상자 왼쪽 끝(글자를 가리지 않게)

            int pipe = Px(PipeThickness);
            SetPipe(0, Rectangle.FromLTRB(t1.Right, yT1 - pipe / 2, xPump, yT1 + pipe / 2));
            SetPipe(1, Rectangle.FromLTRB(t2.Right, yT2 - pipe / 2, xPump, yT2 + pipe / 2));
            SetPipe(2, Rectangle.FromLTRB(xPump - pipe / 2, yT1 - pipe / 2, xPump + pipe / 2, yT2 + pipe / 2));
            SetPipe(3, Rectangle.FromLTRB(xPump, yMid - pipe / 2, t3.Left, yMid + pipe / 2));
            SetPipe(4, Rectangle.FromLTRB(t3.Right, yMid - pipe / 2, t4.Left, yMid + pipe / 2));
            SetPipe(5, Rectangle.FromLTRB(t4.Right, yMid - pipe / 2, ship.Left, yMid + pipe / 2));

            PlaceItem(_valves, "V1", g1[0], yT1); PlaceItem(_valves, "V2", g1[0], yT2);
            PlaceItem(_pumps, "P1", g1[1], yMid); PlaceItem(_valves, "V3", g1[2], yMid);
            PlaceItem(_valves, "V4", g2[0], yMid); PlaceItem(_pumps, "P2", g2[1], yMid); PlaceItem(_valves, "V5", g2[2], yMid);
            PlaceItem(_valves, "V6", g3[0], yMid);
            PlaceArrow(0, g1[0], g1[1], yT1, pipe); PlaceArrow(1, g1[0], g1[1], yT2, pipe); PlaceArrow(3, g1[1], g1[2], yMid, pipe);
            PlaceArrow(4, g2[1], g2[2], yMid, pipe); PlaceArrow(5, g3[0], g3[1], yMid, pipe);
            UpdateVisuals();
            foreach (var l in _pipes) l.SendToBack();
            foreach (var l in _valves.Values) l.BringToFront();
            foreach (var l in _pumps.Values) l.BringToFront();
            ResumeLayout();
        }

        private void PlaceTank(string id, Rectangle unit, int titleH)
        {
            TankParts p;
            if (!_tanks.TryGetValue(id, out p)) return;
            int line = Px(TitleLineHeight);
            p.Name.SetBounds(unit.X, unit.Y, unit.Width, line);
            p.Level.SetBounds(unit.X, unit.Y + line, unit.Width, line);
            p.Temp.SetBounds(unit.X, unit.Y + 2 * line, unit.Width, line);
            p.Frame.Bounds = new Rectangle(unit.X, unit.Y + titleH, unit.Width, unit.Height - titleH);
            int ch = p.Frame.ClientSize.Height, cw = p.Frame.ClientSize.Width;
            TankInfo t = FlowRules.FindTank(_tankData, id);
            int level = t == null ? 0 : t.Level;
            int fh = (int)(ch * level / 100.0);
            p.Fill.SetBounds(0, ch - fh, cw, fh);
            int lineY = ch - (int)(ch * FlowRules.AlarmLevel / 100.0);
            p.AlarmLine.SetBounds(0, lineY, cw, Px(LineThickness));
            p.AlarmLine.BringToFront();
        }

        private void SetPipe(int i, Rectangle r) { _pipes[i].Bounds = r; _pipes[i].SendToBack(); }

        private static void PlaceItem(Dictionary<string, Label> map, string id, Rectangle slot, int centerY)
        {
            Label l;
            if (!map.TryGetValue(id, out l)) return;
            l.Bounds = new Rectangle(slot.X, centerY - slot.Height / 2, slot.Width, slot.Height);
            l.BringToFront();
        }

        // 화살표는 두 항목 사이 빈틈, 배관 바로 위(겹치지 않게)
        private void PlaceArrow(int i, Rectangle from, Rectangle to, int pipeY, int pipe)
        {
            int w = to.X - from.Right, h = from.Height / 2;
            _arrows[i].Bounds = new Rectangle(from.Right, pipeY - pipe / 2 - h, w, h);
        }

        private bool IsOpen(string id)
        {
            foreach (var v in _valveData) if (v.Id == id) return v.IsOpen;
            return false;
        }

        private bool Running(string id)
        {
            var p = FlowRules.FindPump(_pumpData, id);
            return p != null && p.Running;
        }

        private void UpdateVisuals()
        {
            foreach (var t in _tankData)
            {
                TankParts p;
                if (!_tanks.TryGetValue(t.Id, out p)) continue;
                var kind = FlowRules.LevelKind(t.Level);
                p.Name.Text = t.Id + " " + t.Role;
                p.Level.Text = t.Level + " %";
                p.Level.ForeColor = ApcTheme.StatusText(kind);
                p.Temp.Text = t.Temp.ToString("0.0") + " ℃";
                p.Temp.ForeColor = t.HasTempSpec ? ApcTheme.StatusText(FlowRules.TempKind(t.Temp)) : TextColor();
                p.Fill.BackColor = ApcTheme.Status(kind == StatusKind.Run ? StatusKind.Info : kind);
            }
            foreach (var v in _valveData) PaintItem(_valves, v.Id, v.IsOpen);
            foreach (var p in _pumpData) PaintItem(_pumps, p.Id, p.Running);

            bool p1 = Running("P1"), feed = p1 && IsOpen("V3") && (IsOpen("V1") || IsOpen("V2"));
            bool[] flow =
            {
                feed && IsOpen("V1"), feed && IsOpen("V2"), feed, feed,
                Running("P2") && IsOpen("V4") && IsOpen("V5"), IsOpen("V6"),
            };
            for (int i = 0; i < _pipes.Length; i++)
            {
                _pipes[i].BackColor = ApcTheme.Status(flow[i] ? StatusKind.Info : StatusKind.Stop);
                _arrows[i].Text = flow[i] && i != 2 ? Arrow : "";
                _arrows[i].ForeColor = ApcTheme.StatusText(StatusKind.Info);
            }
        }

        private static void PaintItem(Dictionary<string, Label> map, string id, bool on)
        {
            Label l;
            if (map.TryGetValue(id, out l)) l.BackColor = ApcTheme.Status(on ? StatusKind.Run : StatusKind.Stop);
        }
    }
}
