using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using MoldingShiftGantt.Data;

namespace MoldingShiftGantt.Views
{
    /// <summary>
    /// 긴급 작업 끼워넣기: 작업 지시 스캔 → 대상 성형기·위치 선택 → 뒤 작업 밀림 미리보기 → 확정.
    /// 금형 톤수 &gt; 성형기 톤수면 확정 불가. 납기 오늘 작업이 밀리면 생산관리 사원증을 스캔해야 확정된다(성형기·위치를 바꾸면 스캔은 다시).
    /// </summary>
    internal class UrgentInsertDialog : ApcForm
    {
        private const int PreviewRows = 6;
        private const int DialogWidth = 940;

        private readonly IMainViewSource _src;
        private readonly Machine[] _machines;
        private readonly ApcScanBox _woScan = new ApcScanBox(), _badgeScan = new ApcScanBox();
        private readonly ApcComboBox _machineCombo = new ApcComboBox(), _positionCombo = new ApcComboBox();
        private readonly ApcInfoStrip _woStrip = new ApcInfoStrip(), _timeStrip = new ApcInfoStrip();
        private readonly ApcGrid _grid = new ApcGrid { ReadOnly = true };
        private readonly Label _badgeNote = FieldCaption("");
        private readonly ApcButton _confirm = new ApcButton { Text = "끼워넣기 확정", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;

        private WorkOrder _order;
        private InsertPreview _preview;
        private bool _badgeOk, _loading;
        private string _badgeCode;

        /// <summary>render용 기본값: 납기 오늘 작업이 밀려 사원증 대기(확정 불가) 상태.</summary>
        public UrgentInsertDialog() : this(new SampleMainViewSource(), "WO-261126-902", 5, 0) { }

        public UrgentInsertDialog(IMainViewSource src) : this(src, null, 0, 0) { }

        protected UrgentInsertDialog(IMainViewSource src, string presetCode, int presetMachine, int presetPosition)
        {
            _src = src;
            _machines = src.GetMachines();
            SetupDialog("긴급 작업 끼워넣기", DialogWidth);

            _woStrip.SetItems("품번", "금형", "수량(개)", "납기");
            _timeStrip.SetItems("사이클타임(s)", "작업 시간(분)", "금형 톤수(t)", "성형기 톤수(t)");
            _badgeNote.AutoSize = false; _badgeNote.Width = ApcTheme.Px(360);
            _woScan.Width = ApcTheme.Px(300); _badgeScan.Width = ApcTheme.Px(300);

            AddContent(SectionTitle("작업 지시"));
            AddFieldRow("작업 지시 스캔", _woScan, FieldCaption("WO-yyMMdd-nnn"));
            AddContent(_woStrip);
            AddContent(_timeStrip);
            AddContent(SectionTitle("끼워 넣을 곳"));
            AddFieldRow("대상 성형기", _machineCombo);
            AddFieldRow("끼워 넣을 위치", _positionCombo);
            AddContent(SectionTitle("뒤 작업 밀림 미리보기"));
            BuildGrid();
            AddContent(_grid);
            AddContent(SectionTitle("납기 오늘 작업 승인"));
            AddFieldRow("생산관리 사원증", _badgeScan, _badgeNote);

            var cancel = new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel };
            SetFooter(cancel, _confirm);
            _note = SetFooterNote("");

            foreach (var m in _machines) _machineCombo.Items.Add(m.Name + "   " + m.Ton + " t");
            _loading = true; _machineCombo.SelectedIndex = presetMachine; _loading = false;
            FillPositions(presetPosition);

            _woScan.Attach(this); _badgeScan.Attach(this);   // 스캔은 먼저 Attach한 작업 지시 칸이 받고, 사원증은 그 칸에 포커스가 있을 때 받는다
            _woScan.Scanned += OnWoScanned;
            _badgeScan.Scanned += OnBadgeScanned;
            _machineCombo.SelectedIndexChanged += (s, e) => { if (_loading) return; FillPositions(0); Evaluate(); };
            _positionCombo.SelectedIndexChanged += (s, e) => { if (!_loading) Evaluate(); };
            _confirm.Click += (s, e) => Confirm();

            if (presetCode != null) { _woScan.Text = presetCode; ApplyOrder(src.FindWorkOrder(presetCode)); }
            else Evaluate();
        }

        public Machine TargetMachine { get { return _machines[_machineCombo.SelectedIndex]; } }
        public WorkOrder Order { get { return _order; } }
        public int Position { get { return _positionCombo.SelectedIndex; } }
        public string ApproverBadge { get { return _badgeOk ? _badgeCode : null; } }

        private void BuildGrid()
        {
            _grid.Height = ApcGrid.HeightForRows(PreviewRows);
            _grid.Columns.Add("kind", "구분");
            _grid.Columns.Add("wo", "작업");
            _grid.Columns.Add("part", "품번");
            _grid.Columns.Add("due", "납기");
            _grid.Columns.Add("old", "기존 시작");
            _grid.Columns.Add("new", "새 시작");
            _grid.Columns.Add("push", "밀림(분)");
            foreach (DataGridViewColumn c in _grid.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns["wo"].FillWeight = 160;
        }

        // ---- 스캔 -----------------------------------------------------------------

        private void OnWoScanned(object sender, ScanEventArgs e)
        {
            var wo = _src.FindWorkOrder(e.Code);
            if (wo == null)
            {
                e.Accepted = false;
                BeginInvoke(new Action(() => ApcDialog.Warn(this, "등록되지 않은 작업 지시입니다 · " + e.Code)));
                return;
            }
            ApplyOrder(wo);
            if (_preview != null && _preview.NeedsApproval && _preview.ToolingFits) _badgeScan.Focus();
        }

        private void OnBadgeScanned(object sender, ScanEventArgs e)
        {
            if (_preview == null || !_preview.NeedsApproval)
            {
                e.Accepted = false;
                BeginInvoke(new Action(() => ApcDialog.Warn(this, "승인이 필요한 상태가 아닙니다 · 사원증은 납기 오늘 작업이 밀릴 때만 받습니다")));
                return;
            }
            var r = _src.CheckApprovalBadge(e.Code);
            e.Accepted = r.Accepted;
            _badgeOk = r.Accepted; _badgeCode = r.Accepted ? e.Code : null;
            Evaluate();
            if (!r.Accepted) BeginInvoke(new Action(() => ApcDialog.Warn(this, r.Message + " · " + e.Code)));
        }

        private void ApplyOrder(WorkOrder wo)
        {
            _order = wo;
            _woStrip.SetValue("품번", wo.Part);
            _woStrip.SetValue("금형", wo.Mold);
            _woStrip.SetValue("수량(개)", wo.Qty.ToString("#,0"));
            _woStrip.SetValue("납기", wo.DueToday ? "오늘" : "내일 이후");
            _timeStrip.SetValue("사이클타임(s)", wo.CycleSec.ToString("0.0"));
            _timeStrip.SetValue("작업 시간(분)", wo.DurationMin.ToString("#,0"));
            _timeStrip.SetValue("금형 톤수(t)", wo.MoldTon.ToString());
            Evaluate();
        }

        // ---- 선택·미리보기 ---------------------------------------------------------

        private void FillPositions(int select)
        {
            _loading = true;
            _positionCombo.Items.Clear();
            foreach (var p in _src.GetInsertPositions(TargetMachine)) _positionCombo.Items.Add(p);
            _positionCombo.SelectedIndex = Math.Min(select, _positionCombo.Items.Count - 1);
            _loading = false;
        }

        private string Hm(int min)
        {
            int t = _src.ShiftStartHour * 60 + min;
            return (t / 60 % 24).ToString("00") + ":" + (t % 60).ToString("00");
        }

        /// <summary>선택이 바뀔 때마다 미리보기·확정 가능 여부를 다시 계산한다. 승인 스캔은 선택이 바뀌면 무효.</summary>
        private void Evaluate()
        {
            _badgeOk = false; _badgeCode = null;
            _timeStrip.SetValue("성형기 톤수(t)", TargetMachine.Ton.ToString());
            _grid.Rows.Clear();
            _preview = null;
            if (_order == null) { UpdateState(); return; }

            _preview = _src.PreviewInsert(TargetMachine, _order, Position);
            foreach (var r in _preview.Rows)
            {
                int i = _grid.Rows.Add(
                    r.IsUrgent ? "긴급 작업" : (r.IsChangeover ? "금형 교체" : "기존 작업"),
                    r.Label, r.Part,
                    r.IsChangeover ? "" : (r.DueToday ? "오늘" : "내일 이후"),
                    r.OldStartMin < 0 ? "" : Hm(r.OldStartMin), Hm(r.NewStartMin),
                    r.IsUrgent || r.IsChangeover ? "" : r.PushMin.ToString("#,0"));
                if (r.DueToday && r.PushMin > 0 && !r.IsUrgent)
                {
                    var st = _grid.Rows[i].Cells["push"].Style;
                    st.ForeColor = ApcTheme.StatusText(StatusKind.Alarm); st.Font = ApcTheme.GetFont(FontStyle.Bold);
                    _grid.Rows[i].Cells["due"].Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                    _grid.Rows[i].Cells["due"].Style.Font = ApcTheme.GetFont(FontStyle.Bold);
                }
            }
            UpdateState();
        }

        private void UpdateState()
        {
            bool needs = _preview != null && _preview.ToolingFits && _preview.NeedsApproval;
            _badgeScan.Enabled = needs;
            if (!needs) _badgeScan.Text = "";
            else if (!_badgeOk) _badgeScan.Text = "";
            _badgeNote.Text = needs ? (_badgeOk ? "승인됨 · 생산관리" : "스캔 필요") : "필요 없음";

            string reason = ""; bool alarm = false, ok = false;
            if (_order == null) reason = "작업 지시를 스캔하세요";
            else if (!_preview.ToolingFits)
            {
                reason = "금형 " + _order.MoldTon + " t > " + TargetMachine.Name + " " + TargetMachine.Ton + " t · 끼워 넣을 수 없음"; alarm = true;
            }
            else if (_preview.NeedsApproval && !_badgeOk)
            {
                reason = "납기 오늘 " + _preview.DueTodayPushed + " 밀림 · 생산관리 사원증 스캔 필요"; alarm = true;
            }
            else
            {
                ok = true;
                if (_preview.OverflowMin > 0) reason = "교대 종료(20:00) 넘김 " + _preview.OverflowMin + "분";
            }
            _note.Text = reason;
            _note.ForeColor = alarm ? ApcTheme.StatusText(StatusKind.Alarm) : SystemColors.ControlText;
            _confirm.Enabled = ok;
        }

        private void Confirm()
        {
            if (!_confirm.Enabled) return;
            string msg = _order.Code + "을(를) " + TargetMachine.Name + " · " + _positionCombo.SelectedItem + " 자리에 끼워 넣습니다.";
            if (!ApcDialog.Confirm(this, msg, "확정", ApcButtonVariant.Primary)) return;
            DialogResult = DialogResult.OK;
        }
    }

    /// <summary>render용: 금형 톤수가 성형기보다 커서 막힌 상태(IM-02 180 t에 320 t 금형).</summary>
    internal sealed class UrgentInsertDialogMismatch : UrgentInsertDialog
    {
        public UrgentInsertDialogMismatch() : base(new SampleMainViewSource(), "WO-261126-902", 1, 0) { }
    }
}
