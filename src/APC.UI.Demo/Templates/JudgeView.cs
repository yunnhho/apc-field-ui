using System;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>
    /// 설비 한 대 판정 화면: 모델 선택 + 스캔 → 판정판(OK/NG) + 측정값 카드 2 → 금일 집계 카드 4 → 판정 이력.
    /// 스캔 한 건마다 측정·판정해 위에서부터 갱신한다.
    /// </summary>
    internal sealed class JudgeView : ApcTemplateView
    {
        private const double LoadMin = 8.0, LoadMax = 12.0, DispMin = 4.80, DispMax = 5.20;

        private readonly ApcComboBox _model = new ApcComboBox();
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Dock = DockStyle.Fill, Text = "READY", SubText = "시리얼을 스캔하세요" };
        private readonly ApcValueCard _load = Card("최대 하중", "kN", "기준 8.0 ~ 12.0 kN");
        private readonly ApcValueCard _disp = Card("최종 변위", "mm", "기준 4.80 ~ 5.20 mm");
        private readonly ApcValueCard _total = Card("금일 생산", "EA", "");
        private readonly ApcValueCard _ok = Card("OK", "EA", "");
        private readonly ApcValueCard _ng = Card("NG", "EA", "");
        private readonly ApcValueCard _rate = Card("불량률", "%", "NG / 생산");
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private const int SampleHistoryRows = 19;
        private readonly Random _sample = new Random(7);
        private int _okCount = 405, _ngCount = 7;

        public JudgeView() : base("베어링 압입 판정 · 압입기 01")
        {
            _model.Items.AddRange(new object[] { "6205-2RS", "6204-ZZ", "6306-2RS" });
            _model.SelectedIndex = 0;
            _model.Width = S(180);
            _scan.Width = S(300);
            _scan.Scanned += (s, e) => e.Accepted = Judge(e.Code);
            _judge.Margin = new Padding(0, 0, S(12), 0);

            _grid.Columns.Add(new ApcLampColumn());
            _grid.Columns.Add("serial", "시리얼");
            _grid.Columns.Add("time", "시각");
            _grid.Columns.Add("load", "최대 하중(kN)");
            _grid.Columns.Add("disp", "최종 변위(mm)");
            _grid.Columns.Add("result", "판정");
            // 짧은 값 열뿐이면 남는 폭을 고르게 나눈다(한 열만 Fill이면 나머지가 오른쪽 끝으로 밀린다).
            foreach (DataGridViewColumn c in _grid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            // 샘플 이력: 표 높이만큼, 금일 집계(OK 405 · NG 7)를 넘지 않게. 실제로는 데이터 인터페이스가 준다.
            var at = new DateTime(2026, 11, 18, 10, 15, 0);
            // 값·간격 주기를 서로 다르게(13·17·19) 해서 행이 되풀이로 보이지 않게 한다.
            for (int i = 0; i < SampleHistoryRows; i++, at = at.AddSeconds(-(41 + i * 7 % 19)))
            {
                bool ng = i % 9 == 1;
                double load = ng ? 12.4 + (i % 4) / 10.0 : 10.0 + (i * 7 % 13) / 10.0;
                double disp = 4.95 + (i * 5 % 17) / 100.0;
                _grid.Rows.Add(ng ? StatusKind.Alarm : StatusKind.Run, "BP" + (2411045 - i), at.ToString("HH:mm:ss"),
                    load.ToString("0.0"), disp.ToString("0.00"), ng ? "NG · 하중 상한" : "OK");
            }

            var recipe = Button("레시피 편집", ApcButtonVariant.Normal, 130);
            recipe.Click += (s, e) => EditRecipe();

            // Top 도킹은 나중에 Add한 것이 위 → 이력(Fill) 먼저, 그다음 아래 줄부터
            Body.Controls.Add(_grid);
            Body.Controls.Add(Columns(S(130), new Control[] { _total, _ok, _ng, _rate }, new[] { 25f, 25f, 25f, 25f }));
            Body.Controls.Add(Columns(S(220), new Control[] { _judge, _load, _disp }, new[] { 50f, 25f, 25f }));
            Body.Controls.Add(Row(Caption("모델"), _model, Caption("시리얼"), _scan, recipe));
            ShowCounts();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);   // 생성자 시점엔 폼이 없다
        }

        private void EditRecipe()
        {
            using (var dlg = new RecipeDialog())
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                // 실제로는 데이터 인터페이스(source.SaveRecipe 등)에 넘긴다. 여기서는 카드 기준 줄만 갱신.
                _load.Footer = "기준 " + dlg.LoadMin + " ~ " + dlg.LoadMax + " kN";
                _disp.Footer = "기준 " + dlg.DispMin + " ~ " + dlg.DispMax + " mm";
            }
        }

        private bool Judge(string serial)
        {
            // 샘플 측정값. 실제는 데이터 인터페이스(PLC 등)에서 받는다.
            double load = Math.Round(9 + _sample.NextDouble() * 4, 1), disp = Math.Round(4.85 + _sample.NextDouble() * 0.3, 2);
            string reason = load > LoadMax ? "하중 " + load + " kN > 상한 " + LoadMax
                          : load < LoadMin ? "하중 " + load + " kN < 하한 " + LoadMin
                          : disp > DispMax || disp < DispMin ? "변위 " + disp + " mm 범위 밖" : null;
            bool ok = reason == null;
            if (ok) _okCount++; else _ngCount++;

            _judge.Text = ok ? "OK" : "NG";
            _judge.Status = ok ? StatusKind.Run : StatusKind.Alarm;
            _judge.SubText = ok ? serial : serial + " · " + reason;
            _load.Value = load.ToString("0.0", CultureInfo.InvariantCulture);
            _load.Status = load > LoadMax || load < LoadMin ? StatusKind.Alarm : StatusKind.Run;
            _disp.Value = disp.ToString("0.00", CultureInfo.InvariantCulture);
            _disp.Status = disp > DispMax || disp < DispMin ? StatusKind.Alarm : StatusKind.Run;
            _grid.Rows.Insert(0, ok ? StatusKind.Run : StatusKind.Alarm, serial, DateTime.Now.ToString("HH:mm:ss"),
                _load.Value, _disp.Value, ok ? "OK" : "NG");
            ShowCounts();
            return true;   // 판정 결과와 무관하게 스캔 자체는 정상 수신
        }

        private void ShowCounts()
        {
            int total = _okCount + _ngCount;
            _total.Value = total.ToString();
            _ok.Value = _okCount.ToString();
            _ng.Value = _ngCount.ToString();
            _ng.Status = _ngCount > 0 ? StatusKind.Alarm : StatusKind.Run;
            _rate.Value = total == 0 ? "" : (100.0 * _ngCount / total).ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static ApcValueCard Card(string title, string unit, string footer)
        {
            return new ApcValueCard { Title = title, Unit = unit, Footer = footer, Dock = DockStyle.Fill, Margin = new Padding(0, 0, S(12), 0) };
        }
    }
}
