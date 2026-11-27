using System.Text;

namespace APC.UI.Input
{
    /// <summary>
    /// Barcode scanner input detection. A scanner sends keys much faster than a person and ends with Enter.
    /// A scan = every key interval (including Enter) at most maxKeyIntervalMs and length at least minLength.
    /// </summary>
    public sealed class ScanDetector
    {
        public const char Terminator = '\r';

        private readonly int _maxKeyIntervalMs;
        private readonly int _minLength;
        private readonly StringBuilder _buffer = new StringBuilder();
        private long _lastTime;

        public ScanDetector(int maxKeyIntervalMs, int minLength)
        {
            _maxKeyIntervalMs = maxKeyIntervalMs;
            _minLength = minLength;
        }

        /// <summary>Feeds one character. On Enter returns the code if detected as a scan, otherwise null.</summary>
        public string Feed(char c, long timestampMs)
        {
            long gap = timestampMs - _lastTime;   // 음수 = 시계 되감김(TickCount 순환) → 버스트 아님
            bool inBurst = _buffer.Length > 0 && gap >= 0 && gap <= _maxKeyIntervalMs;
            _lastTime = timestampMs;

            if (c == Terminator)
            {
                string code = inBurst && _buffer.Length >= _minLength ? _buffer.ToString() : null;
                _buffer.Length = 0;
                return code;
            }

            if (!inBurst) _buffer.Length = 0;   // 느린 키 = 사람 입력, 새로 시작
            _buffer.Append(c);
            return null;
        }

        /// <summary>Current burst length. 1 right after Feed means a new burst started.</summary>
        public int Length { get { return _buffer.Length; } }

        public void Reset()
        {
            _buffer.Length = 0;
            _lastTime = 0;
        }
    }
}
