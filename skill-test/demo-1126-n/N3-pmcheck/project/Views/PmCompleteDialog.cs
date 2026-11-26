using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using PressPmChecklist.Data;

namespace PressPmChecklist.Views
{
    /// <summary>
    /// 점검 완료 dialog: 설비·점검자·소요 시간·단계별 결과 요약(읽기 전용) + NG 항목 목록.
    /// NG가 있으면 정비 요청을 등록해야 [점검 완료]가 켜진다. 등록 여부는 이 dialog 안의 [정비 요청 등록] 동작으로만 바뀐다.
    /// </summary>
    internal sealed class PmCompleteDialog : ApcForm
    {
        private readonly IMainViewSource _source;
        private readonly string _equipment;
        private readonly IList<StepResult> _ngResults;
        private readonly ApcGrid _ngGrid;
        private readonly ApcButton _register = new ApcButton { Text = "정비 요청 등록", Variant = ApcButtonVariant.Primary };
        private readonly ApcButton _complete = new ApcButton { Text = "점검 완료", DialogResult = DialogResult.OK };
        private readonly Label _note;

        /// <summary>등록된 정비 요청 번호. NG가 없으면 null.</summary>
        public string RequestNo { get; private set; }

        /// <summary>render용 샘플: NG 2건, 정비 요청 미등록(= 점검 완료가 막힌 상태).</summary>
        public PmCompleteDialog() : this(new SampleMainViewSource(), "EQ-PRS-03", "EMP-10482", DateTime.Now.AddMinutes(-3),
            new TimeSpan(0, 13, 48), SampleMainViewSource.CompletedResults(new SampleMainViewSource().GetSteps())) { }

        public PmCompleteDialog(IMainViewSource source, string equipment, string employee, DateTime completedAt, TimeSpan elapsed, IList<StepResult> results)
        {
            _source = source;
            _equipment = equipment;
            _ngResults = results.Where(r => !r.Ok).ToList();

            SetupDialog("점검 완료", 960);

            var info = new ApcInfoStrip();
            info.SetItems("설비", "점검자", "소요 시간", "완료 시각");
            info.SetValue("설비", equipment);
            info.SetValue("점검자", employee);
            info.SetValue("소요 시간", string.Format("{0:00}:{1:00}", (int)elapsed.TotalMinutes, elapsed.Seconds));
            info.SetValue("완료 시각", completedAt.ToString("HH:mm"));
            AddContent(info);

            AddContent(SectionTitle("단계별 결과 (OK " + (results.Count - _ngResults.Count) + " · NG " + _ngResults.Count + ")"));
            AddContent(BuildSummary(results));

            if (_ngResults.Count == 0)
            {
                AddContent(SectionTitle("NG 항목 없음"));
                _ngGrid = null;
            }
            else
            {
                AddContent(SectionTitle("NG 항목"));
                _ngGrid = BuildNgGrid();
                AddContent(_ngGrid);
            }

            _note = SetFooterNote("");
            var cancel = new ApcButton { Text = "돌아가기", DialogResult = DialogResult.Cancel };
            _complete.Variant = ApcButtonVariant.Primary;
            if (_ngResults.Count == 0) SetFooter(cancel, _complete);
            else SetFooter(cancel, _complete, _register);
            _register.Click += (s, e) => Register();
            RefreshState();
        }

        private static ApcGrid BuildSummary(IList<StepResult> results)
        {
            var grid = NewGrid(results.Count);
            AddResultColumns(grid);
            foreach (var r in results) grid.Rows.Add(r.Step.No.ToString(), r.Step.ItemLabel, r.ValueText, r.Step.Standard,
                new ApcLampValue(r.Ok ? StatusKind.Run : StatusKind.Alarm, r.Ok ? "OK" : "NG"), r.At.ToString("HH:mm"));
            MarkNgValues(grid, results);
            return grid;
        }

        private ApcGrid BuildNgGrid()
        {
            var grid = NewGrid(_ngResults.Count);
            grid.Columns.Add("no", "단계");
            grid.Columns.Add("item", "점검 항목");
            grid.Columns.Add("reason", "NG 내용");
            grid.Columns.Add(new ApcLampColumn { HeaderText = "정비 요청" });
            grid.Columns["item"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["reason"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["item"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.Columns["reason"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            foreach (var r in _ngResults) grid.Rows.Add(r.Step.No.ToString(), r.Step.Name, r.NgReason, new ApcLampValue(StatusKind.Warn, "미등록"));
            return grid;
        }

        private static ApcGrid NewGrid(int rows)
        {
            return new ApcGrid { ReadOnly = true, Height = ApcGrid.HeightForRows(rows), ScrollBars = ScrollBars.None };
        }

        private static void AddResultColumns(ApcGrid grid)
        {
            grid.Columns.Add("no", "단계");
            grid.Columns.Add("item", "점검 항목");
            grid.Columns.Add("value", "측정값");
            grid.Columns.Add("std", "기준");
            grid.Columns.Add(new ApcLampColumn { HeaderText = "판정" });
            grid.Columns.Add("time", "시각");
            grid.Columns["item"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["item"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.Columns["std"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }

        private static void MarkNgValues(ApcGrid grid, IList<StepResult> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Ok || !results[i].Value.HasValue) continue;
                var style = grid.Rows[i].Cells["value"].Style;
                style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                style.Font = ApcTheme.GetFont(System.Drawing.FontStyle.Bold);
            }
        }

        private void Register()
        {
            if (!ApcDialog.Confirm(this, "NG " + _ngResults.Count + "건의 정비 요청을 등록합니다.", "등록", ApcButtonVariant.Primary)) return;
            RequestNo = _source.RegisterMaintenanceRequest(_equipment, _ngResults);
            for (int i = 0; i < _ngGrid.Rows.Count; i++) _ngGrid.Rows[i].Cells[3].Value = new ApcLampValue(StatusKind.Run, "등록 " + RequestNo);
            RefreshState();
        }

        private void RefreshState()
        {
            bool blocked = _ngResults.Count > 0 && RequestNo == null;
            _complete.Enabled = !blocked;
            _register.Enabled = _ngResults.Count > 0 && RequestNo == null;
            _register.Variant = blocked ? ApcButtonVariant.Primary : ApcButtonVariant.Normal;
            _note.Text = blocked ? "NG " + _ngResults.Count + "건 · 정비 요청 등록 전 점검 완료 불가" : "";
        }
    }
}
