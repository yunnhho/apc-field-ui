using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CarburizeFurnaceMonitor.Data;

namespace CarburizeFurnaceMonitor.Views
{
    /// <summary>
    /// 침탄 열처리로 4기 배치 모니터링 + 배치 종료 판정 진입.
    /// 위에서부터: 지금 가장 급한 것(판정판) + 로 타일 → 선택 로의 현재값·배치 정보 → 공정 단계·프로파일 → 최근 배치 이력.
    /// 데이터는 IMainViewSource에서만 받는다(기본: 샘플 데이터).
    /// </summary>
    internal sealed class MainView : ApcTemplateView
    {
        private const int FurnaceCount = 4, StepRows = 5;
        private const string ScreenTitle = "침탄 열처리로 배치 모니터링";

        private readonly IMainViewSource _source;
        private readonly ApcJudgePanel _panel = new ApcJudgePanel { Dock = DockStyle.Fill };
        private readonly ApcStatusTile[] _tiles = new ApcStatusTile[FurnaceCount];
        private readonly ApcValueCard _tempCard = Card("로 온도"), _cpCard = Card("카본 포텐셜"), _oilCard = Card("유조 온도");
        private readonly ApcInfoStrip _strip = new ApcInfoStrip();
        private readonly Label _reason = Caption("");
        private readonly ApcButton _judgeButton = Button("배치 종료 판정", ApcButtonVariant.Primary, 170);
        private readonly ApcGrid _steps = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcTrendChart _tempChart = new ApcTrendChart { Dock = DockStyle.Fill };
        private readonly ApcTrendChart _cpChart = new ApcTrendChart { Dock = DockStyle.Fill };
        private readonly ApcGrid _history = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private Panel _batchPanel;
        private IList<FurnaceInfo> _furnaces;
        private string _selectedId = "CF-3";

        public MainView() : this(new SampleMainViewSource()) { }

        public MainView(IMainViewSource source) : base(ScreenTitle)
        {
            _source = source;
            _strip.SetItems("배치 번호", "품번", "적재 수량 (개)", "레시피");
            BuildStepColumns();
            BuildHistoryColumns();
            _judgeButton.Click += (s, e) => OpenJudgeDialog();

            _batchPanel = Titled("현재 배치", _strip, _reason, _judgeButton);
            var topCells = new Control[FurnaceCount + 1];
            topCells[0] = _panel;
            for (int i = 0; i < FurnaceCount; i++)
            {
                var tile = new ApcStatusTile { Dock = DockStyle.Fill };
                tile.Click += (s, e) => { _selectedId = ((ApcStatusTile)s).Tag as string; Reload(); };
                _tiles[i] = tile;
                topCells[i + 1] = tile;
            }

            int valueRowHeight = Math.Max(S(110), TitledHeight(_strip.Height, true));
            int stepRowHeight = TitledHeight(ApcGrid.HeightForRows(StepRows));
            // Fill 먼저, Top 줄은 아래쪽부터
            Body.Controls.Add(Titled("최근 배치 이력", _history));
            Body.Controls.Add(Columns(stepRowHeight, new Control[] { Titled("공정 단계 진행", _steps), Titled("로 온도 (℃) · 점선 = 설정·허용", _tempChart), Titled("CP (%) · 점선 = 설정·허용", _cpChart) }, new[] { 46f, 27f, 27f }));
            Body.Controls.Add(Columns(valueRowHeight, new Control[] { _tempCard, _cpCard, _oilCard, _batchPanel }, new[] { 15f, 15f, 15f, 55f }));
            Body.Controls.Add(Columns(S(96), topCells, new[] { 40f, 15f, 15f, 15f, 15f }));
            Reload();
        }

        private static ApcValueCard Card(string title)
        {
            return new ApcValueCard { Title = title, ShowLamp = true, Dock = DockStyle.Fill };
        }

        /// <summary>데이터를 다시 읽어 화면 전체를 맞춘다(선택 로 변경·종료 판정 뒤).</summary>
        private void Reload()
        {
            _furnaces = _source.GetFurnaces();
            FurnaceInfo selected = FindFurnace(_selectedId);
            IList<StepInfo> steps = _source.GetSteps(selected.Recipe);
            ShowPanel();
            ShowTiles();
            ShowCards(selected, steps);
            ShowBatch(selected);
            ShowSteps(selected, steps);
            ShowProfile(selected, steps);
            ShowHistory();
        }

        private FurnaceInfo FindFurnace(string id)
        {
            foreach (var f in _furnaces) if (f.Id == id) return f;
            return _furnaces[0];
        }

        // 레시피는 4기 모두 GR-20이라 선택 로의 단계표로 전 로를 판정한다(레시피가 다르면 로마다 GetSteps).
        private IList<StepInfo> StepsOf(FurnaceInfo f) { return _source.GetSteps(f.HasBatch ? f.Recipe : "GR-20"); }

        private void ShowPanel()
        {
            foreach (var f in _furnaces)
            {
                string reason = Judging.AlarmReason(f, StepsOf(f));
                if (reason.Length == 0) continue;
                SetPanel(StatusKind.Alarm, f.Id + " 알람", reason);
                return;
            }
            foreach (var f in _furnaces)
            {
                if (!f.IsFinished) continue;
                SetPanel(StatusKind.Warn, f.Id + " 종료 판정 대기", f.Id + "를 선택하고 [배치 종료 판정]을 누르세요");
                return;
            }
            SetPanel(StatusKind.Run, "이상 없음", "알람·판정 대기 없음");
        }

        private void SetPanel(StatusKind status, string text, string sub)
        {
            _panel.Status = status;
            _panel.Text = text;
            _panel.SubText = sub;
        }

        private void ShowTiles()
        {
            for (int i = 0; i < FurnaceCount; i++)
            {
                FurnaceInfo f = _furnaces[i];
                _tiles[i].Tag = f.Id;
                _tiles[i].Status = Judging.FurnaceStatus(f, StepsOf(f));
                _tiles[i].Text = f.Id + "  " + Judging.PhaseName(f);
                _tiles[i].Selected = f.Id == _selectedId;
            }
        }

        private void ShowCards(FurnaceInfo f, IList<StepInfo> steps)
        {
            StepInfo step = Judging.CurrentStep(f, steps);
            _tempCard.Value = f.Temp.HasValue ? Judging.FormatTemp(f.Temp.Value) : "";
            _tempCard.Unit = "℃";
            _tempCard.Status = Judging.TempStatus(f, steps);
            _tempCard.Footer = step != null && step.SetTemp.HasValue ? "설정 " + Judging.FormatTemp(step.SetTemp.Value) + " · 허용 ±" + Judging.FormatTemp(Judging.TempTolerance) : "";

            _cpCard.Value = f.Cp.HasValue ? Judging.FormatCp(f.Cp.Value) : "";
            _cpCard.Unit = "%";
            _cpCard.Status = Judging.CpStatus(f, steps);
            _cpCard.Footer = step != null && step.SetCp.HasValue ? "설정 " + Judging.FormatCp(step.SetCp.Value) + " · 허용 ±" + Judging.FormatCp(Judging.CpTolerance) : "";

            _oilCard.Value = f.OilTemp.HasValue ? Judging.FormatTemp(f.OilTemp.Value) : "";
            _oilCard.Unit = "℃";
            _oilCard.Status = Judging.OilStatus(f, steps);
            _oilCard.Footer = "유냉 기준 " + Judging.FormatTemp(steps[StepRows - 1].OilMin.Value) + " ~ " + Judging.FormatTemp(steps[StepRows - 1].OilMax.Value);
        }

        private void ShowBatch(FurnaceInfo f)
        {
            _batchPanel.Text = "현재 배치 · " + f.Id;
            _strip.SetValue("배치 번호", f.BatchNo);
            _strip.SetValue("품번", f.PartNo);
            _strip.SetValue("적재 수량 (개)", f.HasBatch ? string.Format("{0} ({1}단 × {2})", f.Quantity, Judging.TraysPerBatch, Judging.GearsPerTray) : "");
            _strip.SetValue("레시피", f.Recipe);
            _judgeButton.Enabled = f.IsFinished;
            _reason.Text = f.IsFinished ? "" : f.HasBatch ? "공정 진행 중 · 종료 판정 불가" : "배치 없음 · 종료 판정 불가";
        }

        private void OpenJudgeDialog()
        {
            FurnaceInfo f = FindFurnace(_selectedId);
            if (!f.IsFinished) return;   // 막힌 상태는 버튼이 꺼져 있어도 여기서 한 번 더 막는다
            using (var dlg = new BatchJudgeDialog(f, _source.GetAlarms(f.BatchNo), _source.VerifyTechnicianBadge))
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                _source.CompleteBatch(dlg.Result);
            }
            Reload();
        }

        private void BuildStepColumns()
        {
            _steps.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            _steps.Columns.Add("name", "단계");
            _steps.Columns.Add("temp", "설정 온도(℃)");
            _steps.Columns.Add("cp", "설정 CP(%)");
            _steps.Columns.Add("set", "설정 시간");
            _steps.Columns.Add("elapsed", "경과");
            _steps.Columns.Add("pct", "진행(%)");
            foreach (DataGridViewColumn c in _steps.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void ShowSteps(FurnaceInfo f, IList<StepInfo> steps)
        {
            _steps.Rows.Clear();
            for (int i = 0; i < steps.Count; i++)
            {
                StepInfo s = steps[i];
                bool done = f.HasBatch && i < f.StepIndex;
                bool current = f.HasBatch && i == f.StepIndex;
                int elapsed = done ? s.SetMinutes : current ? f.StepElapsedMinutes : 0;
                StatusKind kind = done ? StatusKind.Run : current ? (Judging.AlarmReason(f, steps).Length > 0 ? StatusKind.Alarm : StatusKind.Run) : StatusKind.Stop;
                string word = done ? "완료" : current ? (kind == StatusKind.Alarm ? "알람" : "진행 중") : "대기";
                string tempText = s.SetTemp.HasValue ? Judging.FormatTemp(s.SetTemp.Value) : "유조 " + Judging.FormatTemp(s.OilMin.Value) + " ~ " + Judging.FormatTemp(s.OilMax.Value);
                _steps.Rows.Add(new ApcLampValue(kind, word), s.Name, tempText, s.SetCp.HasValue ? Judging.FormatCp(s.SetCp.Value) : "",
                    Judging.FormatMinutes(s.SetMinutes), done || current ? Judging.FormatMinutes(elapsed) : "", done || current ? (elapsed * 100 / s.SetMinutes).ToString() : "");
            }
        }

        private void ShowProfile(FurnaceInfo f, IList<StepInfo> steps)
        {
            ProfileData p = _source.GetProfile(f.Id);
            StepInfo step = Judging.CurrentStep(f, steps);
            string[] labels = { Judging.FormatTime(f.StartTime), "현재" };
            Fill(_tempChart, p.Temp, step == null ? null : step.SetTemp, Judging.TempTolerance, labels, f.HasBatch);
            Fill(_cpChart, p.Cp, step == null ? null : step.SetCp, Judging.CpTolerance, labels, f.HasBatch);
        }

        private static void Fill(ApcTrendChart chart, double[] values, decimal? set, decimal tolerance, string[] labels, bool hasBatch)
        {
            chart.SetValues(values);
            chart.XLabels = hasBatch ? labels : new string[0];
            chart.Reference = set.HasValue ? (double?)set.Value : null;
            chart.UpperLimit = set.HasValue ? (double?)(set.Value + tolerance) : null;
            chart.LowerLimit = set.HasValue ? (double?)(set.Value - tolerance) : null;
        }

        private void BuildHistoryColumns()
        {
            _history.Columns.Add("furnace", "로");
            _history.Columns.Add("batch", "배치");
            _history.Columns.Add("part", "품번");
            _history.Columns.Add("start", "시작");
            _history.Columns.Add("end", "종료");
            _history.Columns.Add(new ApcLampColumn { HeaderText = "판정" });
            _history.Columns.Add("hrc", "표면경도(HRC)");
            _history.Columns.Add("depth", "유효경화깊이(mm)");
            _history.AddColumnGroup("경도 시편 결과", 6, 7);
            foreach (DataGridViewColumn c in _history.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void ShowHistory()
        {
            _history.Rows.Clear();
            foreach (var h in _source.GetHistory())
            {
                int row = _history.Rows.Add(h.Furnace, h.BatchNo, h.PartNo, h.Start, h.End,
                    new ApcLampValue(h.Passed ? StatusKind.Run : StatusKind.Alarm, h.Passed ? "합격" : "불합격"),
                    Judging.FormatHardness(h.Hardness), Judging.FormatDepth(h.Depth));
                if (h.Hardness < Judging.HardnessMin || h.Hardness > Judging.HardnessMax) Emphasize(_history.Rows[row].Cells["hrc"]);
                if (h.Depth < Judging.DepthMin || h.Depth > Judging.DepthMax) Emphasize(_history.Rows[row].Cells["depth"]);
            }
        }

        private static void Emphasize(DataGridViewCell cell)
        {
            cell.Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            cell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
        }
    }
}
