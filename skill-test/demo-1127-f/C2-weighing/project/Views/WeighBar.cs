using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;

namespace RubberWeighing.Views
{
    /// <summary>
    /// 계량 진행 막대: 목표 -5 % ~ +5 % 확대 구간. 허용 범위 구간(띠), 현재값까지 채움, 현재값 위치(가는 선).
    /// 표준 Label만 Panel 안에 배치한다(직접 그리지 않음).
    /// </summary>
    internal sealed class WeighBar : Panel
    {
        private const double TrackShare = 0.55;     // 막대 높이 / 전체 높이
        private const double FillShare = 0.5;       // 아래 절반: 채움 / 위 절반: 허용 범위 띠
        private const int MarkerPx = 4;             // 현재값 선 두께(6 px 이하)

        private readonly Panel _track = new Panel();
        private readonly Label _band = new Label();
        private readonly Label _fill = new Label();
        private readonly Label _marker = new Label();
        private readonly Label _left = new Label();
        private readonly Label _right = new Label();
        private readonly Label _center = new Label();
        private decimal _lower, _upper, _scaleMin, _scaleMax;
        private decimal? _value;
        private StatusKind _kind = StatusKind.Stop;

        public WeighBar()
        {
            BackColor = Color.Transparent;
            _track.BackColor = ApcTheme.C(ApcTheme.Current.Colors.GridLine);
            _marker.BackColor = ApcTheme.C(ApcTheme.Current.Colors.Text);
            foreach (var l in new[] { _left, _right, _center })
            {
                l.AutoSize = false;
                l.Font = ApcTheme.GetFont();
                l.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text);
                l.TextAlign = ContentAlignment.MiddleLeft;
                Controls.Add(l);
            }
            _right.TextAlign = ContentAlignment.MiddleRight;
            _center.TextAlign = ContentAlignment.MiddleCenter;
            _track.Controls.Add(_marker);
            _track.Controls.Add(_fill);
            _track.Controls.Add(_band);
            _marker.BringToFront();
            Controls.Add(_track);
        }

        /// <summary>value = null이면 채움·위치선을 숨긴다(LOT 스캔 전).</summary>
        public void SetState(decimal lower, decimal upper, decimal scaleMin, decimal scaleMax, decimal? value, StatusKind kind, string leftText, string centerText, string rightText)
        {
            _lower = lower; _upper = upper; _scaleMin = scaleMin; _scaleMax = scaleMax; _value = value; _kind = kind;
            _left.Text = leftText;
            _center.Text = centerText;
            _right.Text = rightText;
            _band.BackColor = ApcTheme.Status(StatusKind.Run);
            _fill.BackColor = ApcTheme.Status(kind);
            Relayout();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Relayout();
        }

        private int X(decimal v)
        {
            if (_scaleMax <= _scaleMin) return 0;
            decimal ratio = Math.Max(0m, Math.Min(1m, (v - _scaleMin) / (_scaleMax - _scaleMin)));
            return (int)Math.Round((double)ratio * _track.Width);
        }

        private void Relayout()
        {
            int trackH = (int)(Height * TrackShare);
            _track.SetBounds(0, 0, Width, trackH);
            int third = Width / 3;
            _left.SetBounds(0, trackH, third, Height - trackH);
            _center.SetBounds(third, trackH, third, Height - trackH);
            _right.SetBounds(2 * third, trackH, Width - 2 * third, Height - trackH);

            int bandL = X(_lower);
            int laneH = (int)(trackH * FillShare);
            _band.SetBounds(bandL, 0, Math.Max(1, X(_upper) - bandL), trackH - laneH);

            bool shown = _value.HasValue && _value.Value >= _scaleMin;   // 구간 왼쪽보다 작으면 채움 없음(투입 중)
            _fill.Visible = _marker.Visible = shown;
            if (!shown) return;
            int fillH = laneH;
            int vx = X(_value.Value);
            _fill.SetBounds(0, trackH - fillH, vx, fillH);
            int mw = ApcTheme.Px(MarkerPx);
            _marker.SetBounds(Math.Max(0, Math.Min(_track.Width - mw, vx - mw / 2)), 0, mw, trackH);
        }
    }
}
