using System;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using APC.UI.Input;
using APC.UI.Theming;

namespace APC.UI.Controls
{
    public sealed class ScanEventArgs : EventArgs
    {
        public ScanEventArgs(string code) { Code = code; Accepted = true; }
        public string Code { get; private set; }

        /// <summary>앱이 코드를 거부하면 false로 둔다 → 실패 피드백(빨강 + 경고음). 기본 true.</summary>
        public bool Accepted { get; set; }
    }

    /// <summary>
    /// 바코드 입력란. Attach(form) 하면 그 폼 어디에 포커스가 있든 스캔을 받는다.
    /// 스캔으로 판정되면 포커스 칸을 버스트 시작 전 값으로 되돌리고 코드를 여기로 옮긴 뒤 Scanned를 낸다.
    /// 스캔의 Enter는 삼켜서 폼의 기본 버튼(AcceptButton)을 누르지 않게 한다.
    /// 피드백은 Scanned 처리 후 한 번: e.Accepted에 따라 성공/실패. 거부된 코드는 칸에 선택된 채 남아 다음 스캔이 덮어쓴다(지우지 않는다).
    /// Attach하지 않은 칸은 Scanned를 내지 않는다(평범한 입력칸). 손으로 친 글자 + Enter도 Scanned가 아니다(스캐너 속도 입력만) —
    /// 수기 입력을 받아야 하면 KeyDown Enter를 따로 처리한다.
    /// 한 폼에 스캔 칸이 여럿(금형 + 사원증)이면 모두 Attach한다: 포커스된 스캔 칸이 받고, 포커스가 다른 곳이면 먼저 Attach한 칸이 받는다.
    /// 받은 칸의 Scanned에서 다음 스캔 칸으로 Focus()를 옮기면 순서대로 스캔된다.
    /// </summary>
    public class ApcScanBox : ApcTextBox, IMessageFilter
    {
        public const int FlashMs = 300;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_CHAR = 0x0102;
        private const int VK_RETURN = 0x0D;

        private readonly Timer _flashTimer = new Timer { Interval = FlashMs };
        private ScanDetector _detector;
        private Form _host;
        private FormClosedEventHandler _hostClosed;

        // 버스트가 시작될 때의 포커스 칸 상태 — 스캔이면 이걸로 되돌린다.
        private TextBoxBase _snapBox;
        private string _snapText;
        private int _snapStart;
        private int _snapLength;

        public event EventHandler<ScanEventArgs> Scanned;

        public ApcScanBox()
        {
            ImeMode = ImeMode.Disable;   // 한글 IME 상태에서도 바코드가 자모로 조합되지 않게
            _flashTimer.Tick += (s, e) => { _flashTimer.Stop(); ResetFieldColor(); };
            ApcTheme.Changed += OnThemeChanged;
            ResetDetector();
        }

        internal bool? LastFlashSuccess { get; private set; }
        internal int FlashCount { get; private set; }

        /// <summary>
        /// 이 폼에서 스캔을 받기 시작한다. 화면(UserControl) 생성자에서는 아직 폼이 없으므로
        /// 화면의 OnHandleCreated에서 FindForm()으로 부른다: <c>var f = FindForm(); if (f != null) _scan.Attach(f);</c>
        /// dialog(ApcForm)는 폼 자신이므로 생성자에서 <c>_scan.Attach(this)</c>로 부른다.
        /// </summary>
        public void Attach(Form host)
        {
            if (host == null) throw new ArgumentNullException("host");
            if (_host != null)
            {
                Application.RemoveMessageFilter(this);
                _host.FormClosed -= _hostClosed;   // 이전 폼이 나중에 닫히며 새 폼용 필터를 지우지 않게
            }
            _host = host;
            ResetDetector();
            Application.AddMessageFilter(this);
            _hostClosed = (s, e) => Application.RemoveMessageFilter(this);
            host.FormClosed += _hostClosed;
        }

        /// <summary>성공 = 초록 플래시 + 짧은 비프, 실패 = 빨강 플래시 + 경고음.</summary>
        public void Flash(bool success)
        {
            LastFlashSuccess = success;
            FlashCount++;
            if (ApcTheme.Current != null)
            {
                var c = ApcTheme.Status(success ? StatusKind.Run : StatusKind.Alarm);
                BackColor = ControlPaint.LightLight(c);
            }
            (success ? SystemSounds.Beep : SystemSounds.Hand).Play();
            _flashTimer.Stop();
            _flashTimer.Start();
        }

        [DllImport("user32.dll")]
        private static extern int GetMessageTime();

        bool IMessageFilter.PreFilterMessage(ref Message m)
        {
            if (_host == null || Form.ActiveForm != _host || !Visible || !Enabled) return false;
            var focused = FindFocused(_host);
            if (focused is ApcScanBox && focused != this) return false;   // 포커스된 다른 스캔 박스에 양보

            // 처리 시각이 아니라 메시지 발생 시각: UI가 바빠 키가 몰려 처리돼도 사람 입력을 스캔으로 오인하지 않는다.
            return Process(m.Msg, WParamOf(m.WParam), (uint)GetMessageTime(), focused as TextBoxBase);
        }

        /// <summary>64비트에서 WParam은 int 범위를 넘을 수 있다(타이머·포인터 메시지). 키 메시지가 쓰는 하위 32비트만 취한다.</summary>
        internal static int WParamOf(IntPtr wParam)
        {
            return unchecked((int)wParam.ToInt64());
        }

        /// <summary>필터 본체. true = 메시지를 삼킴. timeMs = 메시지 발생 시각.</summary>
        internal bool Process(int msg, int wParam, long timeMs, TextBoxBase focused)
        {
            if (msg == WM_CHAR)
            {
                char c = (char)wParam;
                if (c == ScanDetector.Terminator) return false;
                _detector.Feed(c, timeMs);
                if (_detector.Length == 1) Snapshot(focused);   // 디스패치 전이라 아직 글자가 들어가지 않은 상태
                return false;
            }

            if (msg == WM_KEYDOWN && wParam == VK_RETURN)
            {
                string code = _detector.Feed(ScanDetector.Terminator, timeMs);
                if (code == null) return false;
                // KEYDOWN을 삼키면 TranslateMessage가 생략돼 WM_CHAR '\r'은 만들어지지 않는다.
                OnScanned(code, focused);
                return true;
            }
            return false;
        }

        private void Snapshot(TextBoxBase box)
        {
            _snapBox = box == this ? null : box;
            if (_snapBox == null) return;
            _snapText = box.Text;
            _snapStart = box.SelectionStart;
            _snapLength = box.SelectionLength;
        }

        private void OnScanned(string code, TextBoxBase focused)
        {
            RestoreSnapshot(focused);
            Text = code;
            SelectAll();
            var args = new ScanEventArgs(code);
            var handler = Scanned;
            if (handler != null) handler(this, args);
            Flash(args.Accepted);
        }

        /// <summary>스캔 글자가 다른 칸에 들어갔으면(선택 영역 덮어쓰기·필터된 글자 포함) 버스트 전 값으로 되돌린다.</summary>
        private void RestoreSnapshot(TextBoxBase focused)
        {
            var box = _snapBox;
            _snapBox = null;
            if (box == null || box != focused || box.IsDisposed || box.ReadOnly) return;
            if (box.Text != _snapText) box.Text = _snapText;
            box.Select(_snapStart, _snapLength);
        }

        private static Control FindFocused(ContainerControl container)
        {
            Control c = container.ActiveControl;
            var inner = c as ContainerControl;
            while (inner != null && inner.ActiveControl != null) { c = inner.ActiveControl; inner = c as ContainerControl; }
            return c;
        }

        private void ResetDetector()
        {
            var scan = ApcTheme.Current == null ? null : ApcTheme.Current.Scan;
            _detector = scan == null ? new ScanDetector(50, 4) : new ScanDetector(scan.MaxKeyIntervalMs, scan.MinLength);
        }

        private void OnThemeChanged(object sender, EventArgs e) { ResetDetector(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Application.RemoveMessageFilter(this);
                ApcTheme.Changed -= OnThemeChanged;
                _flashTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
