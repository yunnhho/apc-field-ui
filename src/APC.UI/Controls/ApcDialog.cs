using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    /// <summary>
    /// 되돌릴 수 없는 동작(삭제, 작업 종료 등) 확인. 기본 포커스와 Enter·Esc는 모두 '취소' —
    /// 바코드 스캔의 Enter나 습관적 Enter로 실수 확인되지 않게 한다.
    /// </summary>
    public static class ApcDialog
    {
        /// <summary>파괴적 동작 확인: 확인 버튼은 빨간 "확인".</summary>
        public static bool Confirm(IWin32Window owner, string message)
        {
            return Confirm(owner, message, "확인", ApcButtonVariant.Danger);
        }

        /// <summary>확인 버튼 글자·종류 지정. 저장처럼 위험하지 않은 확인은 ("저장", Primary).</summary>
        public static bool Confirm(IWin32Window owner, string message, string okText, ApcButtonVariant okVariant)
        {
            using (var dlg = Create(message, okText, okVariant))
                return dlg.ShowDialog(owner) == DialogResult.OK;
        }

        /// <summary>
        /// 경고창(입력 오류·조작 경고: 기간 역순, 빈 LOT, 잘못된·중복 스캔 등). 제목 "경고" + 노란 램프.
        /// 버튼은 "확인" 하나, Enter·Esc로 닫힌다.
        /// </summary>
        public static void Warn(IWin32Window owner, string message)
        {
            using (var dlg = CreateAlert(message, true)) dlg.ShowDialog(owner);
        }

        /// <summary>안내창(연결 결과·완료 알림처럼 잘못된 것이 없는 안내). 제목 "알림" + 파란 램프. 경고는 <see cref="Warn"/>.</summary>
        public static void Alert(IWin32Window owner, string message)
        {
            using (var dlg = CreateAlert(message, false)) dlg.ShowDialog(owner);
        }

        internal static ApcForm Create(string message)
        {
            return Create(message, "확인", ApcButtonVariant.Danger);
        }

        internal static ApcForm CreateAlert(string message, bool warning)
        {
            var dlg = Create(message, "확인", ApcButtonVariant.Primary);
            dlg.TitleText = warning ? "경고" : "알림";
            AddKindIcon(dlg, warning ? "lamp_warn" : "lamp_info");
            ApcButton ok = null;
            foreach (Control c in dlg.Controls)
            {
                var b = c as ApcButton;
                if (b == null) continue;
                if (b.DialogResult == DialogResult.OK) ok = b;
            }
            var cancel = (Control)dlg.CancelButton;
            ok.Bounds = cancel.Bounds;   // 버튼 하나는 오른쪽 끝(취소 자리)에
            dlg.Controls.Remove(cancel);
            cancel.Dispose();
            ok.TabIndex = 0;
            dlg.AcceptButton = dlg.CancelButton = ok;
            dlg.Shown += (sender, e) => ok.Focus();
            return dlg;
        }

        /// <summary>메시지 왼쪽에 종류 램프(경고 노랑 / 알림 파랑) — 색만이 아니라 제목 글자로도 구분된다.</summary>
        private static void AddKindIcon(ApcForm dlg, string lamp)
        {
            if (ApcTheme.Current == null) return;
            Label label = null;
            foreach (Control c in dlg.Controls) if (c is Label) label = (Label)c;
            int size = ApcTheme.Px(24), gap = ApcTheme.Px(12);
            var icon = new PictureBox
            {
                Image = ApcTheme.GetImage(lamp),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Bounds = new Rectangle(label.Left, label.Top + (label.Height - size) / 2, size, size),
            };
            label.SetBounds(icon.Right + gap, label.Top, label.Right - icon.Right - gap, label.Height);
            dlg.Controls.Add(icon);
        }

        internal static ApcForm Create(string message, string okText, ApcButtonVariant okVariant)
        {
            float s = ApcTheme.Scale;
            int pad = (int)(16 * s), btnW = (int)(96 * s), btnH = Math.Max(ApcTheme.MinHeight, (int)(34 * s));

            var dlg = new ApcForm
            {
                TitleText = "확인",
                Resizable = false,
                MinimizeBox = false,
                ShowLogo = false,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size((int)(420 * s), (int)(190 * s)),
            };

            var cancel = new ApcButton { Text = "취소", Variant = ApcButtonVariant.Normal, DialogResult = DialogResult.Cancel, TabIndex = 0 };
            var ok = new ApcButton { Text = okText, Variant = okVariant, DialogResult = DialogResult.OK, TabIndex = 1 };
            var anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            // [확정][취소]: 취소가 오른쪽 끝
            cancel.SetBounds(dlg.ClientSize.Width - pad - btnW, dlg.ClientSize.Height - pad - btnH, btnW, btnH);
            ok.SetBounds(cancel.Left - (int)(8 * s) - btnW, cancel.Top, btnW, btnH);
            ok.Anchor = cancel.Anchor = anchor;

            var label = new Label
            {
                Text = message,
                AutoSize = false,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Bounds = Rectangle.FromLTRB(pad, dlg.ContentBounds.Top + pad, dlg.ClientSize.Width - pad, ok.Top - pad),
                TabIndex = 2,
            };
            if (ApcTheme.Current != null) label.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.Text);

            dlg.Controls.Add(label);
            dlg.Controls.Add(cancel);
            dlg.Controls.Add(ok);
            dlg.AcceptButton = cancel;
            dlg.CancelButton = cancel;
            dlg.Shown += (sender, e) => cancel.Focus();
            return dlg;
        }
    }
}
