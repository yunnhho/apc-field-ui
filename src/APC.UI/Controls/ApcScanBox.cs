using System;
using System.Collections.Generic;
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

        /// <summary>Set to false when the app rejects the code → failure feedback (red + alarm sound). Default true.</summary>
        public bool Accepted { get; set; }
    }

    /// <summary>
    /// Barcode input field. After Attach(form) it receives scans wherever focus is on that form.
    /// When input is detected as a scan, the focused field is restored to its value before the burst, the code moves here, and Scanned is raised.
    /// The scan's Enter is swallowed so it does not press the form's default button (AcceptButton).
    /// Feedback happens once after Scanned is handled: success/failure per e.Accepted. A rejected code stays selected in the field so the next scan overwrites it (not cleared).
    /// A field that is not attached never raises Scanned (plain input field). Typed text + Enter is not Scanned either (scanner-speed input only) —
    /// if manual entry must be accepted, handle KeyDown Enter separately.
    /// With several scan fields on one form (mold + employee badge), attach all and give each an Accepts (which codes it takes):
    /// a code goes to the field that accepts it (regardless of focus or scan order). A code no field accepts goes to the focused scan field (or the first attached one) and the app rejects it.
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

        // 같은 폼의 스캔 칸끼리 코드를 넘겨주기 위한 목록(UI 스레드에서만 쓴다).
        private static readonly List<ApcScanBox> AttachedBoxes = new List<ApcScanBox>();

        public event EventHandler<ScanEventArgs> Scanned;

        /// <summary>
        /// Whether this field takes the code (e.g. <c>c => c.StartsWith("EMP-")</c>). Give one per field when a form has two or more scan fields —
        /// a code the focused field does not accept moves to the field on the same form whose Accepts takes it, and that field raises Scanned.
        /// null = takes every code that arrives here, but does not take over codes other fields refused (set Accepts for that).
        /// </summary>
        public Predicate<string> Accepts { get; set; }

        private bool _scanOnly;

        /// <summary>
        /// Scan-only field: cannot be typed, edited or pasted into (ReadOnly). Looks like a normal input field (white) — it reads as a place to scan.
        /// Use for fields that "accept scanned values only", such as employee badges and labels. Text is the scanned value.
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool ScanOnly
        {
            get { return _scanOnly; }
            set { _scanOnly = value; ReadOnly = value; ResetFieldColor(); }
        }

        internal override bool LooksReadOnly { get { return ReadOnly && !_scanOnly; } }

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
        /// Starts receiving scans on this form. A screen (UserControl) has no form yet in its constructor, so
        /// call it from the screen's OnHandleCreated via FindForm(): <c>var f = FindForm(); if (f != null) _scan.Attach(f);</c>
        /// A dialog (ApcForm) is the form itself, so call <c>_scan.Attach(this)</c> in its constructor.
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
            if (!AttachedBoxes.Contains(this)) AttachedBoxes.Add(this);
            _hostClosed = (s, e) => { Application.RemoveMessageFilter(this); AttachedBoxes.Remove(this); };
            host.FormClosed += _hostClosed;
        }

        /// <summary>Success = green flash + short beep, failure = red flash + alarm sound.</summary>
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

        /// <summary>On 64-bit, WParam can exceed int range (timer and pointer messages). Take only the low 32 bits that key messages use.</summary>
        internal static int WParamOf(IntPtr wParam)
        {
            return unchecked((int)wParam.ToInt64());
        }

        /// <summary>Filter body. true = swallow the message. timeMs = message time.</summary>
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
            _snapBox = box;
            if (_snapBox == null) return;
            _snapText = box.Text;
            _snapStart = box.SelectionStart;
            _snapLength = box.SelectionLength;
        }

        private void OnScanned(string code, TextBoxBase focused)
        {
            var target = RouteTarget(code);
            RestoreSnapshot(focused, target);
            target.Receive(code);
        }

        /// <summary>If this field does not accept the code, the field on the same form that does (enabled and visible). Otherwise this field.</summary>
        private ApcScanBox RouteTarget(string code)
        {
            if (Accepts == null || Accepts(code)) return this;
            foreach (var box in AttachedBoxes)
                if (box != this && box._host == _host && box.Enabled && box.Visible && box.FindForm() == _host && box.Accepts != null && box.Accepts(code))
                    return box;
            return this;
        }

        private void Receive(string code)
        {
            Text = code;
            SelectAll();
            var args = new ScanEventArgs(code);
            var handler = Scanned;
            if (handler != null) handler(this, args);
            Flash(args.Accepted);
        }

        /// <summary>If scanned characters landed in a field other than the receiving one (including overwritten selections and filtered characters), restores its value from before the burst.</summary>
        private void RestoreSnapshot(TextBoxBase focused, ApcScanBox target)
        {
            var box = _snapBox;
            _snapBox = null;
            if (box == null || box == target || box != focused || box.IsDisposed || box.ReadOnly) return;
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
                AttachedBoxes.Remove(this);
                ApcTheme.Changed -= OnThemeChanged;
                _flashTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
