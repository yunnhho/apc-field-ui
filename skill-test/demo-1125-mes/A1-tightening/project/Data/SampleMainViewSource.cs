using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace CaliperTighteningStation.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 기준 시각 2026-11-25 14:07, E05 우회 13:52~15:52.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const string Prefix = "58210-AB100-261125-1-";
        private static readonly DateTime Day = new DateTime(2026, 11, 25);
        private BypassInfo _bypass = new BypassInfo("E05", Day.AddHours(15).AddMinutes(52), "초품 검사 진행 중, 4M 승인 대기");

        public StationSnapshot Current() { return Build(Prefix + "000185", false); }

        public StationSnapshot Scan(string serial)
        {
            return Build(serial, serial.EndsWith("999"));   // 샘플: 끝이 999인 시리얼은 E03 NG
        }

        public BypassInfo GetBypass() { return _bypass; }
        public bool VerifyAdmin(string password) { return password.Length > 0; }   // 샘플: 실제는 권한 DB
        public void StartBypass(string item, int minutes, string reason)
        {
            _bypass = new BypassInfo(item, DateTime.Now.AddMinutes(minutes), reason);
        }

        public int TodayTotal { get { return 184; } }
        public int TodayOk { get { return 176; } }
        public int TodayNg { get { return 8; } }

        private StationSnapshot Build(string serial, bool blocked)
        {
            var il = new List<InterlockRow>
            {
                new InterlockRow("E01", "등록된 시리얼인가", StatusKind.Run, "OK"),
                new InterlockRow("E02", "이전 공정 OK 완료", StatusKind.Run, "OK"),
                blocked ? new InterlockRow("E03", "미해결 NG 이력 없음", StatusKind.Alarm, "미해결 NG 이력 있음 · 수리 공정 확인")
                        : new InterlockRow("E03", "미해결 NG 이력 없음", StatusKind.Run, "OK"),
                new InterlockRow("E04", "투입 부품이 BOM과 일치", StatusKind.Run, "OK"),
                _bypass != null && _bypass.Items == "E05"
                    ? new InterlockRow("E05", "4M 승인 · 초품 합격", StatusKind.Warn, "우회 · 4M 변경 승인 대기")
                    : new InterlockRow("E05", "4M 승인 · 초품 합격", StatusKind.Run, "OK"),
            };
            var bolts = new List<BoltRow>();
            if (!blocked)
            {
                bolts.Add(new BoltRow(1, 1, 25.3, 178, 1));
                bolts.Add(new BoltRow(2, 3, 24.0, 150, 1));   // 하한과 같은 값 → OK
                bolts.Add(new BoltRow(3, 2, 23.6, 171, 1));   // 토크 낮음 → 재체결
                bolts.Add(new BoltRow(4, 4, null, null, 0));
            }
            return new StationSnapshot(serial, il, bolts);
        }

        public IList<HistoryRow> History()
        {
            var by = "E05 우회 · 추가 검사";
            return new List<HistoryRow>
            {
                H(14, 5, 41, 184, StatusKind.Run, "OK", by),
                H(14, 3, 20, 183, StatusKind.Run, "OK", by),
                H(14, 1, 55, 182, StatusKind.Alarm, "NG", "볼트 3 각도 높음 · 재체결 초과 · " + by),
                H(13, 59, 12, 181, StatusKind.Run, "OK", by),
                new HistoryRow(Day.AddHours(13).AddMinutes(56).AddSeconds(30), Prefix + "000204", StatusKind.Alarm, "인터락 NG", "E03 미해결 NG 이력"),
                H(13, 55, 2, 180, StatusKind.Run, "OK", by),
                H(13, 52, 41, 179, StatusKind.Run, "OK", by),
                H(13, 50, 36, 178, StatusKind.Run, "OK", ""),
                H(13, 48, 10, 177, StatusKind.Run, "OK", ""),
                H(13, 45, 57, 176, StatusKind.Alarm, "NG", "볼트 1 토크 높음 · 재체결 초과"),
                H(13, 43, 29, 175, StatusKind.Run, "OK", ""),
                H(13, 41, 3, 174, StatusKind.Run, "OK", ""),
                H(13, 38, 44, 173, StatusKind.Run, "OK", ""),
                H(13, 36, 20, 172, StatusKind.Run, "OK", ""),
            };
        }

        private static HistoryRow H(int h, int m, int s, int no, StatusKind st, string result, string note)
        {
            return new HistoryRow(Day.AddHours(h).AddMinutes(m).AddSeconds(s), Prefix + no.ToString("000000"), st, result, note);
        }
    }
}
