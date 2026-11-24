using System;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>
    /// 칸(슬롯)별 LOT 입력 dialog 골격(참고: Failure Analysis LOT 입력 — 설비마다 칸 수가 다르다).
    /// 위: 설비 띠(ApcHeaderBar) — 왼쪽 설비 이름, 오른쪽 진행 "스캔 n / 전체"(다 차면 초록 "입력 완료").
    /// 가운데: 칸마다 카드 한 장(ApcFieldCard, 한 줄에 최대 4장) — 제목 띠에 칸 이름·SPEC, 안에 LOT(스캔)·바렐번호, 아래 상태 줄 "스캔 대기" → 램프 + 스캔 시각.
    /// 바코드 스캐너는 키보드처럼 들어오므로 첫 칸 LOT에 커서를 두고 받는다. 스캔의 Enter → 다음 칸 LOT로(마지막 칸이면 확인 버튼).
    /// 입력 중인 카드는 파란 테두리라 지금 어느 칸을 스캔하는지 보인다. 확인 시 빈 LOT이 있으면 Warn 후 그 칸으로, 닫지 않음.
    /// 결과는 Lots / Barrels 속성(칸 순서).
    /// </summary>
    internal sealed class LotInputDialog : ApcForm
    {
        private const int CardsPerRow = 4;

        private readonly string[] _slots;
        private readonly ApcFieldCard[] _cards;
        private readonly ApcTextBox[] _lots, _barrels;
        private readonly ApcHeaderBar _header = new ApcHeaderBar();
        private readonly ApcButton _ok = new ApcButton { Text = "확인", Variant = ApcButtonVariant.Primary };

        public LotInputDialog() : this("고속 연마기 ST-GB-02", new[] { "B1", "B2", "B3", "B4" }, new[] { "Ø32 · 0.8μm", "Ø32 · 0.8μm", "Ø40 · 0.4μm", "Ø40 · 0.4μm" }) { }

        public LotInputDialog(string equipment, string[] slots, string[] specs)
        {
            _slots = slots;
            _cards = new ApcFieldCard[slots.Length];
            _lots = new ApcTextBox[slots.Length];
            _barrels = new ApcTextBox[slots.Length];
            SetupDialog("LOT 입력", 1000);

            _header.Title = equipment;
            AddContent(_header);

            for (int i = 0; i < slots.Length; i++)
            {
                int slot = i;
                _lots[i] = new ApcTextBox { ImeMode = ImeMode.Disable };   // 한글 IME 상태에서도 바코드가 자모로 조합되지 않게
                _barrels[i] = new ApcTextBox();
                _lots[i].KeyDown += (s, e) =>
                {
                    if (e.KeyCode != Keys.Enter) return;
                    e.SuppressKeyPress = true;   // 띵 소리 없이
                    MarkScanned(slot);
                    if (slot + 1 < _slots.Length) _lots[slot + 1].Focus(); else _ok.Focus();
                };
                _lots[i].Leave += (s, e) => MarkScanned(slot);   // 손으로 넣고 다른 칸을 눌러도 반영

                _cards[i] = new ApcFieldCard { Text = slots[i], SubTitle = i < specs.Length ? specs[i] : "" };
                _cards[i].AddField("LOT No (스캔)", _lots[i]);
                _cards[i].AddField("바렐번호", _barrels[i]);
                MarkScanned(i);
            }
            for (int i = 0; i < _cards.Length; i += CardsPerRow)
                AddContentRow(_cards.Skip(i).Take(CardsPerRow).Cast<Control>().ToArray());

            _ok.Click += (s, e) => Confirm();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _ok);   // Enter는 스캔이 쓴다 → AcceptButton 없음
            Shown += (s, e) => _lots[0].Focus();
        }

        public string[] Lots { get; private set; }
        public string[] Barrels { get; private set; }

        // 카드 상태 줄과 설비 띠 진행 표시를 LOT 칸 내용에 맞춘다. 시각은 처음 들어온 때 한 번만(같은 값으로 다시 Enter해도 유지).
        private void MarkScanned(int slot)
        {
            var card = _cards[slot];
            bool filled = _lots[slot].Text.Trim().Length > 0;
            if (!filled) { card.FooterStatus = StatusKind.Stop; card.Footer = "스캔 대기"; }
            else if (card.FooterStatus != StatusKind.Run) { card.FooterStatus = StatusKind.Run; card.Footer = "스캔 " + DateTime.Now.ToString("HH:mm:ss"); }

            int done = _lots.Count(t => t != null && t.Text.Trim().Length > 0);
            bool all = done == _slots.Length;
            _header.Status = all ? StatusKind.Run : StatusKind.Stop;
            _header.StatusText = (all ? "입력 완료 " : "스캔 ") + done + " / " + _slots.Length;
        }

        private void Confirm()
        {
            var lots = _lots.Select(t => t.Text.Trim()).ToArray();
            int empty = Array.FindIndex(lots, l => l.Length == 0);
            if (empty >= 0)
            {
                ApcDialog.Warn(this, _slots[empty] + " 칸의 LOT를 스캔하세요.");
                _lots[empty].Focus();
                return;
            }
            Lots = lots;
            Barrels = _barrels.Select(t => t.Text.Trim()).ToArray();
            DialogResult = DialogResult.OK;
        }
    }
}
