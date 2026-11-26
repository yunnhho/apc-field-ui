using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>
    /// 연마기 불량 방지 시스템(references/FailureAnalysis.png → 현장 수정본) 재구성: 본문 맨 위 한 줄에 설비군 전환 버튼(ApcTabStrip) + 오른쪽 남은 폭에 시험 모드 알림 띠(ApcNoticeBar)(시각은 창 제목 줄), 설정은 타이틀바 톱니(UseSettingsButton),
    /// 2단 헤더 그리드(B1/B2 묶음)가 화면 폭을 채우고 행 높이를 줄여 전 행이 스크롤 없이 보임(설비 입력·확인이 주 업무), 행 동작은 아이콘 열(ApcIconColumn: 할 수 없으면 회색), 상태는 램프 + 문구 한 칸(ApcLampValue).
    /// </summary>
    internal sealed class FailureAnalysisView : ApcTemplateView
    {
        private const int MaxRowHeight = 34;   // 멀리서도 읽히는 높이. 넓은 화면에선 이 높이로
        private const int ColCurrent = 9, ColSend = 11, ColReset = 12, ColStatus = 13;
        private static readonly string[] Groups = { "저속 연마기 ST-GR-01~03", "저속 연마기 ST-GR-04~06", "고속 연마기 ST-GB-01~08" };

        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly ApcTabStrip _tabs = new ApcTabStrip();

        public FailureAnalysisView() : base("연마기 불량 방지 시스템")
        {
            _tabs.SetTabs(Groups);   // 첫 탭이 선택된다. 표 내용은 아래 SelectGroup(0)에서
            _tabs.Width = _tabs.PreferredTabsWidth;
            // 시험 모드는 판정이 아닌 지속 상태 → 알림 띠(ApcNoticeBar)
            var testMode = new ApcNoticeBar { Status = StatusKind.Info, Text = "시험 모드", Detail = "가상 장비로 동작합니다. 실제 설비에 전송되지 않습니다." };

            UseSettingsButton((s, e) => OpenSettings());   // 설정은 화면 안 버튼이 아니라 창 타이틀바(최소화 왼쪽) 톱니

            BuildColumns();
            _grid.CellClick += OnCellClick;

            // 탭 옆에 알림 띠: 띠가 따로 한 줄을 차지하지 않아 표에 높이가 더 간다
            int gap = S(ApcLayout.RowGap);
            var top = new Panel { Dock = DockStyle.Top, Height = _tabs.Height + gap, Padding = new Padding(0, 0, 0, gap) };
            testMode.Dock = DockStyle.Fill;
            _tabs.Dock = DockStyle.Left;
            top.Controls.Add(testMode);                                       // Fill 먼저
            top.Controls.Add(new Panel { Dock = DockStyle.Left, Width = gap });   // 탭과 띠 사이 간격
            top.Controls.Add(_tabs);

            Body.Controls.Add(_grid);   // Fill 먼저
            Body.Controls.Add(top);
            SelectGroup(0);
            _tabs.SelectedIndexChanged += (s, e) => SelectGroup(_tabs.SelectedIndex);
            _grid.Resize += (s, e) => FitRows();
        }

        /// <summary>설정 진입: 관리자 확인 → 설정 dialog(샘플은 비밀번호 검사 생략).</summary>
        private void OpenSettings()
        {
            var owner = FindForm();
            using (var pw = new AdminPasswordDialog())
                if (pw.ShowDialog(owner) != DialogResult.OK) return;
            using (var dlg = new SettingsDialog()) dlg.ShowDialog(owner);
        }

        private void BuildColumns()
        {
            _grid.Columns.Add("no", "No");
            _grid.Columns.Add("name", "설비명칭");
            foreach (var b in new[] { "b1", "b2" })
            {
                _grid.Columns.Add(b + "spec", "SPEC");
                _grid.Columns.Add(b + "lot", "LOT No");
                _grid.Columns.Add(b + "barrel", "바렐");
            }
            _grid.Columns.Add("target", "운전목표시간");
            _grid.Columns.Add("current", "현재운전시간");
            _grid.Columns.Add("rpm", "RPM");
            _grid.Columns.Add(new ApcIconColumn { Name = "send", HeaderText = "전송", IconName = "icon_play" });
            _grid.Columns.Add(new ApcIconColumn { Name = "reset", HeaderText = "초기화", IconName = "icon_reset" });
            _grid.Columns.Add(new ApcLampColumn { Name = "status", HeaderText = "상태" });

            // 짧은 값 열은 고정 폭, 설비명·SPEC·LOT(긴 코드)가 남은 폭을 비율로 나눠 화면 폭을 채운다(오른쪽 빈 공간·코드 잘림 없음).
            int[] fixedW = { 50, 0, 0, 0, 56, 0, 0, 56, 130, 130, 80, 72, 72, 130 };
            float[] weights = { 0, 30, 26, 16, 0, 26, 16, 0, 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < fixedW.Length; i++)
            {
                var col = _grid.Columns[i];
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (fixedW[i] > 0) { col.Width = S(fixedW[i]); continue; }
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.FillWeight = weights[i];
            }
            foreach (var name in new[] { "target", "current", "rpm" })
                _grid.Columns[name].DefaultCellStyle.Font = ApcTheme.GetFont(FontStyle.Bold);   // 작업자가 보는 값 강조

            _grid.AddColumnGroup("B1", 2, 4);
            _grid.AddColumnGroup("B2", 5, 7);
        }

        private void SelectGroup(int index)
        {
            string prefix = index == 2 ? "고속 연마기 ST-GB-" : "저속 연마기 ST-GR-";
            int firstMachine = index == 1 ? 4 : 1;
            int machines = index == 2 ? 8 : 3;
            _grid.Rows.Clear();
            int no = 0;
            for (int m = 0; m < machines; m++)
            {
                string machine = prefix + (firstMachine + m).ToString("00");
                if (index == 2) { _grid.Rows.Add(SampleRow(++no, machine)); continue; }   // 고속은 열·단 없이 1대 1행
                for (int col = 1; col <= 2; col++)
                    for (int stage = 1; stage <= 4; stage++)
                        _grid.Rows.Add(SampleRow(++no, machine + " (" + col + "열 " + stage + "단)"));
            }
            FitRows();
        }

        /// <summary>전 행이 스크롤 없이 보이게 행 높이를 표 높이에 맞춤(최대 34px, 최소 테마 MinHeight — 그보다 행이 많으면 스크롤).</summary>
        private void FitRows()
        {
            int rows = _grid.Rows.Count;
            if (rows == 0) return;
            int fit = (_grid.ClientSize.Height - _grid.ColumnHeadersHeight - 2) / rows;
            int h = Math.Max(ApcTheme.MinHeight, Math.Min(S(MaxRowHeight), fit));
            _grid.RowTemplate.Height = h;
            foreach (DataGridViewRow row in _grid.Rows) row.Height = h;
        }

        /// <summary>앞쪽 몇 행만 가동 데이터, 나머지는 끊김 — 원본 캡처(전부 끊김)보다 상태 표현을 보여주기 위해.</summary>
        private static object[] SampleRow(int no, string name)
        {
            switch (no)
            {
                case 1: return Row(no, name, "Q3225X7S106K500ARLNN01F", "P052609404712", "2", "S3216X5R106K500NRINN02F", "M052610084612", "1", "02:00:00", "01:12:40", "370", StatusKind.Run, "가동");
                case 2: return Row(no, name, "Q2012X7R475K250NRENN03F", "P052610056015", "1", "", "", "", "01:30:00", "01:28:05", "370", StatusKind.Warn, "종료 임박");
                case 3: return Row(no, name, "S2012X5R226M250NRENN03F", "S052607407714", "1", "", "", "", "02:00:00", "00:41:12", "352", StatusKind.Alarm, "RPM 이탈");
                case 4: return Row(no, name, "", "", "", "", "", "", "", "", "", StatusKind.Stop, "대기");
                default: return Row(no, name, "", "", "", "", "", "", "", "", "", StatusKind.Stop, "끊김");
            }
        }

        private static object[] Row(int no, string name, string spec1, string lot1, string barrel1, string spec2, string lot2, string barrel2,
            string target, string current, string rpm, StatusKind status, string statusText)
        {
            // 전송·초기화: LOT가 들어간 행만 누를 수 있다(파란 아이콘), 빈 행·끊김은 회색
            bool hasLot = lot1.Length > 0 || lot2.Length > 0;
            return new object[] { no, name, spec1, lot1, barrel1, spec2, lot2, barrel2, target, current, rpm, hasLot, hasLot, new ApcLampValue(status, statusText) };
        }

        private void OnCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _grid.Rows[e.RowIndex];
            if (!ApcIconColumn.IsEnabled(row.Cells[e.ColumnIndex])) return;   // 회색 아이콘은 눌러도 아무 일 없음
            if (e.ColumnIndex == ColSend)
            {
                row.Cells[ColStatus].Value = new ApcLampValue(StatusKind.Run, "전송됨");
            }
            else if (e.ColumnIndex == ColReset && ApcDialog.Confirm(FindForm(), row.Cells[1].Value + "의 운전 시간을 초기화할까요?"))
            {
                row.Cells[ColCurrent].Value = "00:00:00";
            }
        }
    }
}
