using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using DieChangeManager.Data;

namespace DieChangeManager.Views
{
    /// <summary>
    /// 금형 교체 등록. 설비 선택 → 제거 금형·지시 금형 자동 표시 → 장착 금형 스캔 → 사원증 스캔 → 예열 확인.
    /// 막는 조건(수리 대기·남은 타수 0 이하·지시 금형 불일치)은 데이터에서 판정하고, 풀 수 있는 입력칸은 없다.
    /// </summary>
    internal sealed class MoldChangeDialog : ApcForm
    {
        private static readonly Regex MoldFormat = new Regex(@"^DM-[A-Z0-9]{4}-\d{2}$");
        private const string PreheatOff = "예열 온도 확인 전 · 온도계로 180~220 ℃ 확인 후 누르세요";
        private const string PreheatOn = "확인 완료 · 180~220 ℃";

        private readonly IMainViewSource _source;
        private readonly System.Collections.Generic.IList<MachineInfo> _machines;
        private readonly ApcComboBox _machine = new ApcComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ApcTextBox _removed = new ApcTextBox { ReadOnly = true };
        private readonly ApcTextBox _next = new ApcTextBox { ReadOnly = true };
        private readonly ApcScanBox _moldScan = new ApcScanBox();
        private readonly ApcInfoStrip _moldInfo = new ApcInfoStrip();
        private readonly ApcScanBox _badgeScan = new ApcScanBox();
        private readonly ApcButton _preheat = new ApcButton { Text = PreheatOff };
        private readonly ApcButton _ok = new ApcButton { Text = "등록", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;
        private MoldInfo _mold;
        private bool _badgeOk;
        private bool _preheated;

        // 샘플: 수리 대기 금형을 스캔한 막힌 상태
        public MoldChangeDialog() : this(new SampleMainViewSource(), "DC-02", "DM-PL55-08", "240318") { }

        public MoldChangeDialog(IMainViewSource source, string machineName, string moldCode = "", string badge = "")
        {
            _source = source;
            _machines = source.GetMachines();
            SetupDialog("금형 교체 등록", 920);

            foreach (var m in _machines) _machine.Items.Add(m.Name);
            AddFieldRow("설비", _machine);
            AddFieldRow("제거 금형", _removed);
            AddFieldRow("지시 금형", _next);
            AddFieldRow("장착 금형 (스캔)", _moldScan);
            _moldInfo.SetItems("금형", "남은 타수", "남은 비율(%)", "상태");
            AddContent(_moldInfo);
            AddFieldRow("작업자 (사원증 스캔)", _badgeScan);
            AddFieldRow("예열 온도", _preheat);

            _note = SetFooterNote("");
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _ok);   // Enter는 스캔이 쓴다 → AcceptButton 없음

            _machine.SelectedIndex = System.Math.Max(0, _machines.ToList().FindIndex(m => m.Name == machineName));
            _machine.SelectedIndexChanged += (s, e) => { _preheated = false; ShowMachine(); };
            _moldScan.TextChanged += (s, e) => { _mold = null; ShowMold(); };
            _moldScan.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; CommitMold(); } };
            _badgeScan.TextChanged += (s, e) => { _badgeOk = false; Recheck(); };
            _badgeScan.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; CommitBadge(); } };
            _preheat.Click += (s, e) => { _preheated = !_preheated; Recheck(); };
            _ok.Click += (s, e) => DialogResult = DialogResult.OK;

            ShowMachine();
            if (moldCode.Length > 0) { _moldScan.Text = moldCode; CommitMold(true); }
            if (badge.Length > 0) { _badgeScan.Text = badge; CommitBadge(true); }
            Shown += (s, e) => (moldCode.Length == 0 ? _moldScan : _badgeScan).Focus();
        }

        public string Machine { get { return (string)_machine.SelectedItem; } }
        public string MoldId { get { return _mold == null ? "" : _mold.Id; } }
        public string Badge { get { return _badgeScan.Text.Trim(); } }

        private MachineInfo Current { get { return _machines[_machine.SelectedIndex]; } }

        private void ShowMachine()
        {
            _removed.Text = Current.MountedId;
            _next.Text = Current.NextMoldId;
            Recheck();
        }

        private void ShowMold()
        {
            _moldInfo.SetValue("금형", _mold == null ? "" : _mold.Id);
            _moldInfo.SetValue("남은 타수", _mold == null ? "" : MoldRules.Count(System.Math.Max(0, _mold.Remaining)));
            _moldInfo.SetValue("남은 비율(%)", _mold == null ? "" : MoldRules.Ratio(_mold));
            _moldInfo.SetValue("상태", _mold == null ? "" : MoldRules.MoldState(_mold));
            Recheck();
        }

        private void CommitMold(bool quiet = false)
        {
            string code = _moldScan.Text.Trim();
            if (code.Length == 0) return;
            var found = MoldFormat.IsMatch(code) ? _source.FindMold(code) : null;
            if (found == null)
            {
                if (!quiet) BeginInvoke(new MethodInvoker(() => ApcDialog.Warn(this, MoldFormat.IsMatch(code) ? "등록되지 않은 금형입니다: " + code : "금형 번호 형식이 아닙니다: " + code)));
                _moldScan.Text = "";
                return;
            }
            _mold = found;
            ShowMold();
            if (!quiet && MoldRules.BlockReason(Current, _mold) == null) _badgeScan.Focus();
        }

        private void CommitBadge(bool quiet = false)
        {
            string code = _badgeScan.Text.Trim();
            if (code.Length == 0) return;
            if (!_source.IsKnownOperator(code))
            {
                if (!quiet) BeginInvoke(new MethodInvoker(() => ApcDialog.Warn(this, "등록되지 않은 사원증입니다: " + code)));
                _badgeScan.Text = "";
                return;
            }
            _badgeOk = true;
            Recheck();
        }

        // 등록 버튼은 모든 조건이 데이터로 충족될 때만 켜진다. 막는 이유는 글로 보인다.
        private void Recheck()
        {
            _preheat.Text = _preheated ? PreheatOn : PreheatOff;
            string block = MoldRules.BlockReason(Current, _mold);
            string wait = _mold == null ? "장착 금형을 스캔하세요"
                : !_badgeOk ? "작업자 사원증을 스캔하세요"
                : !_preheated ? "예열 온도 확인 전" : "";
            _ok.Enabled = block == null && wait.Length == 0;
            _note.Text = block ?? wait;
            _note.ForeColor = block != null ? ApcTheme.StatusText(StatusKind.Alarm) : ApcTheme.C(ApcTheme.Current.Colors.Text);
        }
    }
}
