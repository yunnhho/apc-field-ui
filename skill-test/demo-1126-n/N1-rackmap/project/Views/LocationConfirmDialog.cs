using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FgRackLocator.Data;

namespace FgRackLocator.Views
{
    /// <summary>
    /// 위치 확정: 스캔한 팔레트 정보(읽기 전용) + 추천/대상 위치 + 위치 라벨 스캔으로 확정.
    /// 입고는 추천과 다른 칸 라벨, 출고는 FIFO 위반(더 오래된 팔레트 있음)을 막는다. FIFO는 담당 사원증 스캔 시 예외.
    /// </summary>
    internal sealed class LocationConfirmDialog : ApcForm
    {
        private const string BadgePrefix = "EMP-";
        private const string LabelPrefix = "LOC-";

        private readonly ScanResult _result;
        private readonly IMainViewSource _source;
        private readonly ApcScanBox _label = new ApcScanBox();
        private readonly ApcScanBox _badge = new ApcScanBox();
        private readonly ApcButton _confirm = new ApcButton { Text = "위치 확정", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;
        private Color _noteColor;
        private string _labelCode;
        private string _badgeCode;

        /// <summary>FIFO 예외로 허용한 사원증(없으면 null).</summary>
        public string ApproverBadge { get { return _badgeCode; } }

        // render용: 출고인데 더 오래된 팔레트가 있어 막힌 상태
        public LocationConfirmDialog() : this(SampleFifo(), SampleSource()) { }

        private static IMainViewSource SampleSource() { return new SampleMainViewSource(); }

        private static ScanResult SampleFifo() { return new SampleMainViewSource().Evaluate(SampleMainViewSource.DemoOutboundFifo); }

        public LocationConfirmDialog(ScanResult result, IMainViewSource source)
        {
            _result = result;
            _source = source;
            bool inbound = result.Kind == ScanKind.Inbound;
            Pallet p = result.Pallet;

            SetupDialog(inbound ? "위치 확정 · 입고" : "위치 확정 · 출고", 780);

            var info = new ApcInfoStrip();
            info.SetItems("팔레트", "품번", "중량(kg)", "입고");
            info.SetValue("팔레트", p.Code);
            info.SetValue("품번", p.PartNo);
            info.SetValue("중량(kg)", p.WeightKg.ToString("0"));
            info.SetValue("입고", p.InboundDate.ToString("MM-dd"));
            AddContent(info);

            var where = new ApcTextBox { ReadOnly = true, Text = result.Location };
            AddFieldRow(inbound ? "추천 위치" : "출고 위치", where);

            if (result.Older != null)
            {
                var bar = new ApcNoticeBar
                {
                    Status = StatusKind.Warn,
                    Text = "더 오래된 팔레트 먼저",
                    Detail = result.Older.Location + " · " + result.Older.Code + " · " + result.Older.InboundDate.ToString("MM-dd") + " 입고"
                };
                AddContent(bar);
            }

            AddFieldRow("위치 라벨", _label);
            if (result.Older != null) AddFieldRow("담당 사원증", _badge);

            _label.Scanned += OnScanned;
            _badge.Scanned += OnScanned;
            _label.Attach(this);
            _badge.Attach(this);

            _note = SetFooterNote("");
            _noteColor = _note.ForeColor;
            var cancel = new ApcButton { Text = "취소" };
            cancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            _confirm.Click += (s, e) => DialogResult = DialogResult.OK;
            SetFooter(cancel, _confirm);
            UpdateState();
        }

        private bool FifoBlocked { get { return _result.Older != null && _badgeCode == null; } }

        private void OnScanned(object sender, ScanEventArgs e)
        {
            bool isBadge = e.Code.StartsWith(BadgePrefix, StringComparison.Ordinal);
            string error = isBadge ? CheckBadge(e.Code) : CheckLabel(e.Code);
            e.Accepted = error == null;
            BeginInvoke((Action)(() =>
            {
                if (error != null) { ApcDialog.Warn(this, error); return; }
                // 스캔 코드는 초점 칸에 들어오므로 종류에 맞는 칸으로 옮긴다
                _label.Text = _labelCode ?? "";
                _badge.Text = _badgeCode ?? "";
                UpdateState();
            }));
        }

        private string CheckLabel(string code)
        {
            if (!code.StartsWith(LabelPrefix, StringComparison.Ordinal)) return "위치 라벨이 아닙니다 · " + code;
            if (code != RackLayout.LabelOf(_result.Location))
                return "추천 위치와 다른 칸입니다 · " + code.Substring(LabelPrefix.Length) + " ≠ " + _result.Location;
            _labelCode = code;
            return null;
        }

        private string CheckBadge(string code)
        {
            if (_result.Older == null) return "사원증 예외가 필요한 작업이 아닙니다";
            if (!_source.IsAuthorizedBadge(code)) return "담당 사원증이 아닙니다 · " + code;
            _badgeCode = code;
            return null;
        }

        private void UpdateState()
        {
            string reason = "";
            bool alarm = false;
            if (FifoBlocked) { reason = "FIFO 위반 · 담당 사원증 스캔 필요"; alarm = true; }
            else if (_labelCode == null) reason = "위치 라벨을 스캔하세요";
            _confirm.Enabled = reason == "";
            _note.Text = reason;
            _note.ForeColor = alarm ? ApcTheme.StatusText(StatusKind.Alarm) : _noteColor;
        }
    }
}
