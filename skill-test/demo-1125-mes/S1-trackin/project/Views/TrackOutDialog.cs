using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using WireBondEapTerminal.Data;

namespace WireBondEapTerminal.Views
{
    /// <summary>
    /// Track-out 수량 확인. 장비 보고(양품·불량)와 작업자 확인 수량, 이전 공정 수량을 맞춰 본다.
    /// 차이가 기준(HoldThreshold) 이상이면 Hold 사유를 골라야 닫힌다 — 조용히 덮지 않는다.
    /// 결과: HoldRequired / HoldReason / WorkerQty. 메인은 OK일 때 읽어 MES에 넘긴다.
    /// </summary>
    internal sealed class TrackOutDialog : ApcForm
    {
        public const int HoldThreshold = 2;   // 기준값(정함): 차이 2 Strip 이상이면 Hold

        private static readonly string[] HoldReasons = { "수량 불일치 · 장비 보고와 작업자 수 차이", "수량 불일치 · 이전 공정 수량과 차이", "수량 불일치 · 원인 확인 필요" };

        private readonly int _prevQty, _equipGood, _equipBad;
        private readonly ApcNumericBox _worker = new ApcNumericBox { Minimum = 0, Maximum = 9999, DecimalPlaces = 0, Increment = 1, Width = ApcTheme.Px(130) };
        private readonly ApcJudgePanel _judge = new ApcJudgePanel { Height = ApcTheme.Px(100) };
        private readonly ApcComboBox _reason = new ApcComboBox();

        public TrackOutDialog() : this("L2611-0471", 400, 394, 6, 391) { }

        public TrackOutDialog(string lotId, int prevQty, int equipGood, int equipBad, int workerQty)
        {
            _prevQty = prevQty; _equipGood = equipGood; _equipBad = equipBad;
            SetupDialog("Track-out 수량 확인", 640);

            _reason.Items.AddRange(HoldReasons);
            _worker.Value = workerQty;
            _worker.ValueChanged += (s, e) => Refresh2();

            var lot = new ApcTextBox { Text = lotId, ReadOnly = true };
            AddFieldRow("Lot ID", lot);

            AddContent(SectionTitle("수량 (Strip)"));
            AddContentRow(Card("이전 공정 수량", prevQty), Card("장비 보고 양품", equipGood), Card("장비 보고 불량", equipBad));

            AddContent(SectionTitle("작업자 확인"));
            AddFieldRow("확인 수량 (Strip)", _worker, Hint("양품 기준 · 장비 양품과 비교"));
            AddContent(_judge);
            AddFieldRow("Hold 사유", _reason);

            var ok = new ApcButton { Text = "Track-out", Variant = ApcButtonVariant.Primary };
            ok.Click += (s, e) => Confirm();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, ok);
            Refresh2();
        }

        public bool HoldRequired { get; private set; }
        public string HoldReason { get; private set; }
        public int WorkerQty { get; private set; }

        // 차이 = 작업자 − 장비 양품, 장비(양품+불량) − 이전 공정 중 큰 쪽. 한 함수로 화면·확정이 같은 판정을 쓴다.
        private int Difference()
        {
            return Math.Max(Math.Abs((int)_worker.Value - _equipGood), Math.Abs(_equipGood + _equipBad - _prevQty));
        }

        private void Refresh2()
        {
            int diff = Difference();
            bool hold = diff >= HoldThreshold;
            _judge.Text = hold ? "HOLD 필요" : "수량 일치";
            _judge.Status = hold ? StatusKind.Alarm : StatusKind.Run;
            _judge.SubText = "차이 " + Format.Qty(diff) + " Strip · 기준 " + Format.Qty(HoldThreshold) + " 이상이면 Hold";
            _reason.Enabled = hold;
            if (!hold) _reason.SelectedIndex = -1;
        }

        private void Confirm()
        {
            _worker.Commit();
            bool hold = Difference() >= HoldThreshold;
            if (hold && _reason.SelectedIndex < 0)
            {
                ApcDialog.Warn(this, "수량 차이가 기준 이상입니다. Hold 사유를 선택하세요.");
                _reason.Focus();
                return;
            }
            string msg = hold ? "Hold 처리하고 Track-out 할까요?" : "Track-out 할까요?";
            if (!ApcDialog.Confirm(this, msg, "Track-out", hold ? ApcButtonVariant.Danger : ApcButtonVariant.Primary)) return;
            HoldRequired = hold;
            HoldReason = hold ? (string)_reason.SelectedItem : null;
            WorkerQty = (int)_worker.Value;
            DialogResult = DialogResult.OK;
        }

        private static ApcValueCard Card(string title, int value)
        {
            return new ApcValueCard { Title = title, Value = Format.Qty(value), Unit = "Strip", Height = ApcTheme.Px(100), Status = StatusKind.Run };
        }

        private static Label Hint(string text)
        {
            var l = FieldCaption(text);
            l.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.TextMuted);
            return l;
        }
    }
}
