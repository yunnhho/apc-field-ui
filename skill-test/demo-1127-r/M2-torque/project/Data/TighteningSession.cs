using System;

namespace TorqueSequence.Data
{
    /// <summary>체결 기준·순서·형식 규칙(한 곳). 토크·각도 경계값은 OK.</summary>
    internal static class TorqueRules
    {
        public const decimal TorqueMin = 9.0m, TorqueMax = 11.0m;
        public const int AngleMin = 30, AngleMax = 60;
        public const int NgBlockCount = 2;   // 같은 포인트 NG 2회째면 재체결 불가

        public static readonly int[] Sequence = { 1, 6, 2, 7, 3, 8, 4, 9, 5, 10 };

        public static bool IsPartSerial(string code)
        {
            // HC-yyMMdd-nnnn
            DateTime d;
            return code != null && code.Length == 14 && code.StartsWith("HC-") && code[9] == '-'
                && IsDigits(code, 10, 4)
                && DateTime.TryParseExact(code.Substring(3, 6), "yyMMdd", null, System.Globalization.DateTimeStyles.None, out d);
        }

        public static bool IsBadge(string code)
        {
            return code != null && code.Length == 9 && code.StartsWith("EMP-") && IsDigits(code, 4, 5);
        }

        public static bool IsForemanBadge(string code) { return IsBadge(code) && code[4] == '9'; }

        /// <summary>NG 사유(문자열), OK면 null. 토크를 먼저 본다.</summary>
        public static string NgReason(decimal torque, int angle)
        {
            if (torque > TorqueMax) return "토크 상한 초과 " + torque.ToString("0.0") + " N·m (상한 " + TorqueMax.ToString("0.0") + ")";
            if (torque < TorqueMin) return "토크 하한 미달 " + torque.ToString("0.0") + " N·m (하한 " + TorqueMin.ToString("0.0") + ")";
            if (angle > AngleMax) return "각도 상한 초과 " + angle + "° (상한 " + AngleMax + ")";
            if (angle < AngleMin) return "각도 하한 미달 " + angle + "° (하한 " + AngleMin + ")";
            return null;
        }

        private static bool IsDigits(string s, int start, int count)
        {
            for (int i = start; i < start + count; i++) if (s[i] < '0' || s[i] > '9') return false;
            return true;
        }
    }

    /// <summary>한 포인트의 체결 기록(번호는 1~10).</summary>
    internal sealed class PointRecord
    {
        public decimal? Torque;
        public int? Angle;
        public DateTime? Time;
        public bool? Ok;        // null = 아직 체결 안 함
        public int NgCount;     // 이 포인트 누적 NG 횟수
    }

    internal enum SubmitResult { Ok, Ng, NotReady, OutOfOrder, NgPending, Complete, Scrapped }

    /// <summary>부품 한 개의 체결 진행 상태. 화면은 이 상태가 허용할 때만 체결값을 받는다(막는 규칙이 여기 한 곳).</summary>
    internal sealed class TighteningSession
    {
        private readonly PointRecord[] _points = new PointRecord[11];
        private bool _approved;

        public TighteningSession() { for (int i = 1; i <= 10; i++) _points[i] = new PointRecord(); }

        public string Serial { get; set; }
        public string Badge { get; set; }
        public bool Scrapped { get; private set; }
        public PointRecord Point(int no) { return _points[no]; }

        public bool Ready { get { return Serial != null && Badge != null; } }

        /// <summary>다음에 체결할 포인트(NG 재체결 대기 포인트 포함). 다 끝나면 0.</summary>
        public int NextPoint
        {
            get
            {
                foreach (int p in TorqueRules.Sequence) if (_points[p].Ok != true) return p;
                return 0;
            }
        }

        public bool Complete { get { return NextPoint == 0; } }
        public bool InProgress { get { for (int i = 1; i <= 10; i++) if (_points[i].Ok.HasValue) return true; return false; } }

        /// <summary>재체결 승인을 기다리는 NG 포인트. 없으면 0.</summary>
        public int PendingNg
        {
            get
            {
                int p = NextPoint;
                return p != 0 && _points[p].Ok == false && !_approved ? p : 0;
            }
        }

        /// <summary>같은 포인트 NG 2회째 → 재체결 불가, 부품 불량 처리.</summary>
        public bool NeedsScrap { get { int p = PendingNg; return p != 0 && _points[p].NgCount >= TorqueRules.NgBlockCount; } }

        public bool Approved { get { return _approved; } }

        /// <summary>체결값을 받을 수 있는지. 못 받는 이유를 reason에 준다.</summary>
        public bool CanTighten(out string reason)
        {
            reason = null;
            if (Scrapped) reason = "부품 불량 처리됨";
            else if (!Ready) reason = Serial == null ? "부품 시리얼 스캔 필요" : "사원증 스캔 필요";
            else if (Complete) reason = "체결 완료";
            else if (NeedsScrap) reason = PendingNg + "번 NG 2회 · 부품 불량 처리 필요";
            else if (PendingNg != 0) reason = PendingNg + "번 NG · 재체결 승인 필요";
            return reason == null;
        }

        public SubmitResult Submit(int point, decimal torque, int angle, DateTime time)
        {
            string reason;
            if (Scrapped) return SubmitResult.Scrapped;
            if (!Ready) return SubmitResult.NotReady;
            if (Complete) return SubmitResult.Complete;
            if (!CanTighten(out reason)) return SubmitResult.NgPending;
            if (point != NextPoint) return SubmitResult.OutOfOrder;

            PointRecord r = _points[point];
            bool ok = TorqueRules.NgReason(torque, angle) == null;
            r.Torque = torque; r.Angle = angle; r.Time = time; r.Ok = ok;
            if (!ok) r.NgCount++;
            _approved = false;
            return ok ? SubmitResult.Ok : SubmitResult.Ng;
        }

        /// <summary>반장 승인으로 NG 포인트를 다시 체결할 수 있게 한다. 2회째 NG면 거부.</summary>
        public bool Approve()
        {
            int p = PendingNg;
            if (p == 0 || NeedsScrap) return false;
            _approved = true;
            return true;
        }

        public void Scrap() { if (NeedsScrap) Scrapped = true; }

        /// <summary>새 부품으로 바꾼다(체결이 끝났거나 불량 처리된 뒤에만).</summary>
        public bool CanChangeSerial { get { return Serial == null || Complete || Scrapped || !InProgress; } }
    }
}
