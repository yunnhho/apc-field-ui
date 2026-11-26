using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FinalTestYieldMonitor.Data;

namespace FinalTestYieldMonitor.Views
{
    /// <summary>
    /// 수율 기준 미달 Lot 처리(OCAP). Lot·수율·주요 Soft Bin은 읽기 전용, 판정과 사유만 입력한다.
    /// 원인 확인·고객 승인·장비 사용 중지가 끝나지 않으면 Release를 막고 이유를 띠에 보인다.
    /// </summary>
    internal sealed class LowYieldLotDialog : ApcForm
    {
        private const string Hold = "Hold 유지", Extra = "추가 검사", Release = "Release", Scrap = "Scrap";
        private const string CauseNo = "미확인", CauseYes = "확인 완료", ApproveNo = "승인 대기", ApproveYes = "승인됨";

        private readonly string _testerId;
        private readonly LotInfo _lot;
        private readonly bool _machineStopped;
        private readonly ApcGrid _steps = new ApcGrid { ReadOnly = true, Height = ApcGrid.HeightForRows(5) };
        private readonly ApcComboBox _cause = new ApcComboBox(), _approval = new ApcComboBox(), _disposition = new ApcComboBox();
        private readonly ApcTextBox _reason = new ApcTextBox();
        private readonly ApcNoticeBar _gate = new ApcNoticeBar();
        private readonly ApcButton _confirm = new ApcButton { Text = "판정 확정", Variant = ApcButtonVariant.Primary };

        public LowYieldLotDialog() : this("T04", new SampleMainViewSource().GetTesters()[3].Lot, true) { }

        public LowYieldLotDialog(string testerId, LotInfo lot, bool machineStopped)
        {
            _testerId = testerId; _lot = lot; _machineStopped = machineStopped;
            SetupDialog("수율 기준 미달 Lot 처리", 940);

            var info = new ApcInfoStrip { Height = ApcTheme.Px(40) };
            info.SetItems("테스터", "Lot ID", "고객", "최종 수율 (%)", "기준 (%)");
            info.SetValue("테스터", testerId);
            info.SetValue("Lot ID", lot.LotId);
            info.SetValue("고객", lot.Customer);
            info.SetValue("최종 수율 (%)", YieldJudge.Text(lot.FinalYield));
            info.SetValue("기준 (%)", YieldJudge.MinFinalYield.ToString("0.0"));
            AddContent(info);

            AddContent(SectionTitle("주요 Soft Bin"));
            AddContent(SoftBinGrid());

            AddContent(SectionTitle("OCAP 진행"));
            _steps.Columns.Add(new ApcLampColumn { HeaderText = "상태" });
            _steps.Columns.Add("step", "단계");
            _steps.Columns["step"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            for (int i = 0; i < 5; i++) _steps.Rows.Add(null, "");
            AddContent(_steps);

            AddContent(SectionTitle("판정"));
            _cause.Items.AddRange(new object[] { CauseNo, CauseYes });
            _approval.Items.AddRange(new object[] { ApproveNo, ApproveYes });
            _disposition.Items.AddRange(new object[] { Hold, Extra, Release, Scrap });
            _cause.SelectedIndex = 0; _approval.SelectedIndex = 0; _disposition.SelectedIndex = 0;
            _cause.SelectedIndexChanged += (s, e) => Reload();
            _approval.SelectedIndexChanged += (s, e) => Reload();
            _disposition.SelectedIndexChanged += (s, e) => Reload();
            AddFieldRow("원인 확인", _cause);
            AddFieldRow("고객 승인", _approval);
            AddFieldRow("판정", _disposition);
            AddFieldRow("사유", _reason);
            AddContent(_gate);

            _confirm.Click += (s, e) => Save();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _confirm);
            Reload();
        }

        public string Disposition { get; private set; }
        public string Reason { get; private set; }

        private ApcGrid SoftBinGrid()
        {
            var grid = new ApcGrid { ReadOnly = true, Height = ApcGrid.HeightForRows(_lot.SoftBinTop.Count) };
            grid.Columns.Add("sb", "Soft Bin");
            grid.Columns.Add("hb", "Hard Bin");
            grid.Columns.Add("cause", "원인");
            grid.Columns.Add("qty", "수량");
            foreach (DataGridViewColumn c in grid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (var s in _lot.SoftBinTop) grid.Rows.Add(s.SoftBin.ToString(), s.HardBin.ToString(), s.Cause, s.Count.ToString("#,0"));
            return grid;
        }

        /// <summary>Release를 막는 이유들. 비면 처리 가능.</summary>
        private List<string> ReleaseBlockers()
        {
            var list = new List<string>();
            if (!_machineStopped) list.Add("장비 사용 중지 미완료");
            if ((string)_cause.SelectedItem != CauseYes) list.Add("원인 확인 필요");
            if ((string)_approval.SelectedItem != ApproveYes) list.Add("고객 승인 필요");
            return list;
        }

        private void SetStep(int row, bool done, string text)
        {
            _steps.Rows[row].Cells[0].Value = done ? StatusKind.Run : StatusKind.Warn;
            _steps.Rows[row].Cells[1].Value = text + (done ? " · 완료" : " · 대기");
        }

        private void Reload()
        {
            SetStep(0, true, "이탈 · 알람 확인");
            SetStep(1, true, "Lot Hold");
            SetStep(2, _machineStopped, _testerId + " 사용 중지");
            SetStep(3, (string)_cause.SelectedItem == CauseYes, "원인 확인");
            SetStep(4, (string)_approval.SelectedItem == ApproveYes, "고객 처리 승인");

            var blockers = (string)_disposition.SelectedItem == Release ? ReleaseBlockers() : new List<string>();
            if (blockers.Count > 0)
            {
                _gate.Status = StatusKind.Alarm;
                _gate.Text = "Release 불가";
                _gate.Detail = string.Join(" · ", blockers.ToArray());
                _confirm.Enabled = false;
            }
            else
            {
                _gate.Status = StatusKind.Info;
                _gate.Text = "선택 가능";
                _gate.Detail = "판정 " + (string)_disposition.SelectedItem;
                _confirm.Enabled = true;
            }
        }

        private void Save()
        {
            string choice = (string)_disposition.SelectedItem;
            // 화면이 막는다: 단계를 건너뛴 Release는 확정 불가(버튼도 꺼져 있지만 한 번 더 확인)
            if (choice == Release && ReleaseBlockers().Count > 0) { ApcDialog.Warn(this, "단계를 건너뛴 Release는 할 수 없습니다."); return; }
            if (_reason.Text.Trim().Length == 0) { ApcDialog.Warn(this, "사유를 입력하세요."); _reason.Focus(); return; }

            string message = "Lot " + _lot.LotId + " 판정: " + choice + ". 확정할까요?";
            bool ok = choice == Scrap
                ? ApcDialog.Confirm(this, message)
                : ApcDialog.Confirm(this, message, "판정 확정", ApcButtonVariant.Primary);
            if (!ok) return;

            Disposition = choice; Reason = _reason.Text.Trim();
            DialogResult = DialogResult.OK;
        }
    }
}
