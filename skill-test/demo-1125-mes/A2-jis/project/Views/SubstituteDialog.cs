using System;
using System.Collections.Generic;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using DoorTrimJisShipping.Data;

namespace DoorTrimJisShipping.Views
{
    /// <summary>
    /// 서열 부품 대체 dialog: NG·결품 칸에 버퍼 재고 부품 라벨을 스캔하고 사유를 고른다. 확정하면 대체 이력이 남는다(호출 쪽이 저장).
    /// 입력은 스캔 1회 + 사유 선택. 지정 품번과 다른 부품·중복 시리얼은 경고창으로 막는다.
    /// </summary>
    internal sealed class SubstituteDialog : ApcForm
    {
        private static readonly string[] Reasons = { "품번 불일치", "외관 NG", "라벨 불량", "결품", "기타" };
        private const int HistoryRows = 3;
        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly ApcComboBox _reason = new ApcComboBox();

        // 샘플: 8칸 NG(스피커 오투입)
        public SubstituteDialog() : this(new SampleMainViewSource().GetSlots()[7], new SampleMainViewSource().GetSubstituteHistory(), (s, l) => null) { }

        /// <param name="validate">(칸 번호, 라벨) -> 오류 문구 또는 null. 데이터 인터페이스의 ValidateSubstitute를 넘긴다.</param>
        public SubstituteDialog(SlotRow slot, IList<SubstituteRecord> history, Func<int, string, string> validate)
        {
            SetupDialog("서열 부품 대체", 960);
            Slot = slot.Slot;

            AddContent(SectionTitle("대체 대상"));
            AddFieldRow("칸 · 서열", ReadOnly("칸 " + slot.Slot.ToString("00") + " · 서열 " + slot.Seq + " · 사양 " + slot.Spec.Code));
            AddFieldRow("지정 품번", ReadOnly(slot.Spec.Part));
            AddFieldRow("현재 상태", ReadOnly(slot.Verdict == SlotVerdict.Ng ? "NG · " + slot.NgReason : "결품 · 부품 없음"));

            AddContent(SectionTitle("버퍼 재고 부품"));
            _scan.Width = ApcTheme.Px(420);
            AddFieldRow("대체 부품 라벨 (스캔)", _scan);
            _reason.Width = ApcTheme.Px(240);
            _reason.Items.AddRange(Reasons);
            _reason.SelectedIndex = slot.Verdict == SlotVerdict.Ng ? 0 : 3;
            AddFieldRow("대체 사유", _reason, FieldCaption("버퍼 재고는 같은 품번만 대체할 수 있습니다"));

            AddContent(SectionTitle("최근 대체 이력"));
            AddContent(History(history));

            var ok = new ApcButton { Text = "대체 확정", Variant = ApcButtonVariant.Primary };
            ok.Click += (s, e) => Confirm();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, ok);   // Enter는 스캔이 쓴다 → AcceptButton 없음

            _scan.Scanned += (s, e) =>
            {
                string err = validate(Slot, e.Code);
                e.Accepted = err == null;
                if (err == null) return;
                BeginInvoke((Action)(() => { _scan.Text = ""; ApcDialog.Warn(this, err); }));
            };
            Shown += (s, e) => { _scan.Attach(this); _scan.Focus(); };
        }

        public int Slot { get; private set; }
        public string ReplacementLabel { get; private set; }
        public string Reason { get; private set; }

        private void Confirm()
        {
            if (_scan.Text.Trim().Length == 0)
            {
                ApcDialog.Warn(this, "대체할 부품 라벨을 스캔하세요.");
                _scan.Focus();
                return;
            }
            ReplacementLabel = _scan.Text.Trim();
            Reason = (string)_reason.SelectedItem;
            DialogResult = DialogResult.OK;
        }

        private static ApcTextBox ReadOnly(string text) { return new ApcTextBox { ReadOnly = true, Text = text }; }

        private static ApcGrid History(IList<SubstituteRecord> history)
        {
            var g = new ApcGrid { ReadOnly = true, ScrollBars = ScrollBars.None };
            g.Columns.Add("time", "시각");
            g.Columns.Add("slot", "칸");
            g.Columns.Add("seq", "서열 번호");
            g.Columns.Add("old", "기존 시리얼");
            g.Columns.Add("new", "대체 시리얼");
            g.Columns.Add("reason", "사유");
            foreach (DataGridViewColumn c in g.Columns) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            for (int i = 0; i < history.Count && i < HistoryRows; i++)
            {
                var h = history[i];
                g.Rows.Add(h.At.ToString("HH:mm:ss"), h.Slot.ToString("00"), h.Seq, h.OldSerial, h.NewSerial, h.Reason);
            }
            g.Height = ApcGrid.HeightForRows(HistoryRows);
            return g;
        }
    }
}
