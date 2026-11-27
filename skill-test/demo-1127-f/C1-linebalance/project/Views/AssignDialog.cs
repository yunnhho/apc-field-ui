using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using LineBalanceBoard.Data;

namespace LineBalanceBoard.Views
{
    /// <summary>
    /// 작업자 배치 변경. 공정 정보는 읽기 전용, 사원증은 스캔만(ScanOnly), 그 작업자의 공정 자격을 표로 보인다.
    /// 자격 없음·만료·다른 공정 배치 중이면 [배치] 버튼이 꺼지고 이유가 버튼 왼쪽에 나온다.
    /// </summary>
    internal sealed class AssignDialog : ApcForm
    {
        private static readonly Regex BadgePattern = new Regex(@"^EMP-\d{5}$");

        private readonly IMainViewSource _source;
        private readonly ProcessInfo _process;
        private readonly ApcScanBox _badge = new ApcScanBox { ScanOnly = true };
        private readonly ApcTextBox _name = new ApcTextBox { ReadOnly = true };
        private readonly ApcGrid _quals = new ApcGrid { ReadOnly = true };
        private readonly ApcButton _ok = new ApcButton { Text = "배치", Variant = ApcButtonVariant.Primary };
        private readonly System.Windows.Forms.Label _note;

        /// <summary>확정된 작업자 사번(취소면 null).</summary>
        public string AssignedWorkerId { get; private set; }

        // render용 샘플: 자격이 만료된 작업자를 스캔한 막힌 상태
        public AssignDialog() : this(new SampleMainViewSource(), "OP40", "EMP-10417") { }

        public AssignDialog(IMainViewSource source, string processCode, string scannedBadge = null)
        {
            _source = source;
            foreach (var p in source.GetProcesses()) if (p.Code == processCode) _process = p;
            SetupDialog("작업자 배치 변경", 760);

            var current = source.FindWorker(_process.WorkerId);
            var strip = new ApcInfoStrip();
            strip.SetItems("공정", "작업 내용", "사이클 타임(초)", "현재 작업자");
            strip.SetValue("공정", _process.Code);
            strip.SetValue("작업 내용", _process.Work);
            strip.SetValue("사이클 타임(초)", BalanceRules.Seconds(_process.CycleTenths));
            strip.SetValue("현재 작업자", current == null ? "" : current.Name);
            AddContent(strip);

            AddFieldRow("사원증 (스캔)", _badge);
            AddFieldRow("작업자", _name);

            AddContent(SectionTitle("공정별 자격"));
            _quals.Columns.Add(new ApcLampColumn());
            _quals.Columns.Add("code", "공정");
            _quals.Columns.Add("work", "작업 내용");
            _quals.Columns.Add("expiry", "만료일");
            _quals.Columns["work"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _quals.Columns["work"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            _quals.Height = ApcGrid.HeightForRows(8);
            AddContent(_quals);
            ShowQualifications(null);

            _note = SetFooterNote("사원증을 스캔하세요");
            _ok.Enabled = false;
            _ok.Click += (s, e) => Confirm();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _ok);

            _badge.Accepts = c => BadgePattern.IsMatch(c);
            _badge.Attach(this);
            _badge.Scanned += OnScanned;
            if (scannedBadge != null) Apply(scannedBadge);
        }

        private void OnScanned(object sender, ScanEventArgs e)
        {
            string message = Apply(e.Code);
            if (message == null) return;
            e.Accepted = false;
            BeginInvoke((Action)(() => ApcDialog.Warn(this, message)));
        }

        // 스캔 코드를 반영한다. 등록 안 된 사원증이면 경고 문구를 돌려준다(다음 스캔이 덮어쓴다).
        private string Apply(string code)
        {
            var worker = BadgePattern.IsMatch(code) ? _source.FindWorker(code) : null;
            if (worker == null)
            {
                Reset("등록되지 않은 사원증입니다 · " + code);
                return "등록되지 않은 사원증입니다.";
            }
            _badge.Text = worker.Id;
            _name.Text = worker.Name;
            ShowQualifications(worker);
            _note.Text = BlockReason(worker);
            _ok.Enabled = _note.Text.Length == 0;
            _note.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
            return null;
        }

        private void Reset(string note)
        {
            _name.Text = "";
            ShowQualifications(null);
            _ok.Enabled = false;
            _note.Text = note;
            _note.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
        }

        // 배치를 막는 이유. 빈 문자열이면 배치 가능.
        private string BlockReason(WorkerInfo worker)
        {
            QualState q = BalanceRules.Qualification(worker, _process.Code, _source.Today);
            if (q == QualState.None) return _process.Code + " 자격 없음 · 배치 불가";
            if (q == QualState.Expired) return _process.Code + " 자격 만료 " + worker.QualExpiry[_process.Code] + " · 배치 불가";
            string at = _source.FindAssignment(worker.Id);
            if (at == _process.Code) return "이미 " + at + " 배치 중";
            if (at.Length > 0) return at + " 배치 중 · 배치 불가";
            return "";
        }

        private void ShowQualifications(WorkerInfo worker)
        {
            _quals.Rows.Clear();
            foreach (var p in _source.GetProcesses())
            {
                QualState q = worker == null ? QualState.None : BalanceRules.Qualification(worker, p.Code, _source.Today);
                string expiry = q == QualState.None ? "" : worker.QualExpiry[p.Code];
                var lamp = q == QualState.Valid ? new ApcLampValue(StatusKind.Run, "유효")
                    : q == QualState.Expired ? new ApcLampValue(StatusKind.Alarm, "만료")
                    : new ApcLampValue(StatusKind.Stop, "없음");
                _quals.Rows.Add(lamp, p.Code, p.Work, expiry);
            }
        }

        private void Confirm()
        {
            if (!ApcDialog.Confirm(this, _process.Code + " 작업자를 " + _name.Text + "(으)로 변경할까요?", "배치", ApcButtonVariant.Primary)) return;
            if (!_source.Assign(_process.Code, _badge.Text))
            {
                ApcDialog.Warn(this, "배치 저장에 실패했습니다.");
                return;
            }
            AssignedWorkerId = _badge.Text;
            DialogResult = DialogResult.OK;
        }
    }
}
