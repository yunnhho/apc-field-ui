using System;
using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using ShipPackVerifier.Data;

namespace ShipPackVerifier.Views
{
    /// <summary>
    /// 박스 라벨 재출력: 박스·사유 선택 + 반장 사원증 스캔. 반장 확인 전에는 [재출력]이 꺼져 있다.
    /// 반장 확인은 호출한 쪽이 넘긴 함수(데이터 쪽 규칙)로만 하고, 화면에서 풀 수 있는 선택 칸은 없다.
    /// </summary>
    internal sealed class ReprintDialog : ApcForm
    {
        private static readonly string[] Reasons = { "라벨 훼손", "오출력", "고객 요청" };

        private readonly Func<string, string> _findSupervisor;
        private readonly ApcComboBox _box = new ApcComboBox();
        private readonly ApcComboBox _reason = new ApcComboBox();
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly ApcTextBox _supervisor = new ApcTextBox { ReadOnly = true };
        private readonly ApcButton _confirm = new ApcButton { Text = "재출력", Variant = ApcButtonVariant.Primary };
        private readonly Label _note;
        private string _badge = "";

        public string BoxNo { get { return (string)_box.SelectedItem; } }
        public string Reason { get { return (string)_reason.SelectedItem; } }
        public string SupervisorBadge { get { return _badge; } }

        /// <summary>render용: 완료 박스 6개, 반장 스캔 전(막힌 상태).</summary>
        public ReprintDialog() : this(SampleBoxes(), new SampleMainViewSource().FindSupervisor) { }

        public ReprintDialog(IList<PackedBox> boxes, Func<string, string> findSupervisor)
        {
            _findSupervisor = findSupervisor;
            SetupDialog("박스 라벨 재출력", 560);

            foreach (PackedBox b in boxes) _box.Items.Add(b.BoxNo);
            if (_box.Items.Count > 0) _box.SelectedIndex = 0;
            _reason.Items.AddRange(Reasons);
            _reason.SelectedIndex = 0;
            _supervisor.Text = "";

            AddFieldRow("박스", _box);
            AddFieldRow("사유", _reason);
            AddFieldRow("반장 사원증", _scan);
            AddFieldRow("확인한 반장", _supervisor);

            var cancel = new ApcButton { Text = "취소" };
            cancel.DialogResult = DialogResult.Cancel;
            _confirm.DialogResult = DialogResult.OK;
            SetFooter(cancel, _confirm);
            _note = SetFooterNote("");

            _scan.Attach(this);
            _scan.Scanned += OnSupervisorScanned;
            UpdateConfirm();
        }

        private void OnSupervisorScanned(object sender, ScanEventArgs e)
        {
            string name = _findSupervisor(e.Code);
            if (name == null)
            {
                e.Accepted = false;
                _badge = "";
                _supervisor.Text = "";
                UpdateConfirm();
                BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(this, "반장 사원증이 아닙니다: " + e.Code)));
                return;
            }
            _badge = e.Code;
            _supervisor.Text = name;
            UpdateConfirm();
        }

        private void UpdateConfirm()
        {
            bool ready = _badge.Length > 0 && _box.SelectedIndex >= 0 && _reason.SelectedIndex >= 0;
            _confirm.Enabled = ready;
            _note.Text = ready ? "" : "반장 사원증 스캔 전 · 재출력 불가";
        }

        private static IList<PackedBox> SampleBoxes()
        {
            var list = new List<PackedBox>();
            var at = new DateTime(2026, 11, 26, 9, 45, 12);
            for (int i = 6; i >= 1; i--) list.Add(new PackedBox("SO-1126-0142-B" + i.ToString("00"), 24, at.AddMinutes(-(i * 3 + i % 2)).AddSeconds(i * 7)));
            return list;
        }
    }
}
