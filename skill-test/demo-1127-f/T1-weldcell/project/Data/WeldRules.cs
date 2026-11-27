using System.Globalization;
using APC.UI.Theming;

namespace SpotWeldMonitor.Data
{
    /// <summary>판정 기준과 판정 함수. 타일·판정판·이력·상세 dialog가 모두 이 함수를 쓴다.</summary>
    internal static class WeldRules
    {
        public const double CurrentMinKa = 9.5, CurrentMaxKa = 11.5;   // 경계 포함 OK
        public const int TipLimitShots = 3000, TipDressingShots = 2700;  // 2,700 이상 경고, 3,000 도달 알람
        public const double AvailabilityTarget = 85.0;                   // 아래면 경고

        public static string Count(int n) { return n.ToString("#,0", CultureInfo.InvariantCulture); }
        public static string Ka(double v) { return v.ToString("0.0", CultureInfo.InvariantCulture); }
        public static string Percent(double v) { return v.ToString("0.0", CultureInfo.InvariantCulture); }

        public static string StateWord(CellState s)
        {
            switch (s)
            {
                case CellState.Running: return "가동";
                case CellState.Waiting: return "대기";
                case CellState.Alarm: return "알람";
                default: return "정지";
            }
        }

        public static StatusKind CurrentStatus(double ka)
        {
            return ka > CurrentMaxKa || ka < CurrentMinKa ? StatusKind.Alarm : StatusKind.Run;
        }

        public static StatusKind TipStatus(int shots)
        {
            if (shots >= TipLimitShots) return StatusKind.Alarm;
            return shots >= TipDressingShots ? StatusKind.Warn : StatusKind.Run;
        }

        public static StatusKind AvailabilityStatus(double percent)
        {
            return percent < AvailabilityTarget ? StatusKind.Warn : StatusKind.Run;
        }

        static StatusKind ProcessStatus(CellState s)
        {
            switch (s)
            {
                case CellState.Running: return StatusKind.Run;
                case CellState.Alarm: return StatusKind.Alarm;
                default: return StatusKind.Stop;
            }
        }

        static int Rank(StatusKind k)
        {
            switch (k)
            {
                case StatusKind.Alarm: return 3;
                case StatusKind.Warn: return 2;
                case StatusKind.Stop: return 1;
                default: return 0;
            }
        }

        /// <summary>셀 상태 = 공정 상태와 값 판정 중 나쁜 쪽. 대기·정지 셀의 값은 판정하지 않는다.</summary>
        public static StatusKind CellStatus(WeldCell c)
        {
            if (c.State == CellState.Waiting || c.State == CellState.Stopped) return StatusKind.Stop;
            return Worse(Worse(ProcessStatus(c.State), TipStatus(c.TipShots)),
                c.LastCurrentKa.HasValue ? CurrentStatus(c.LastCurrentKa.Value) : StatusKind.Run);
        }

        static StatusKind Worse(StatusKind a, StatusKind b) { return Rank(b) > Rank(a) ? b : a; }

        /// <summary>가장 나쁜 판정의 사유 한 줄. 사유가 없으면(정상) null.</summary>
        public static string Reason(WeldCell c)
        {
            if (c.State == CellState.Waiting) return "대기 중";
            if (c.State == CellState.Stopped) return "정지";
            if (c.LastCurrentKa.HasValue && CurrentStatus(c.LastCurrentKa.Value) == StatusKind.Alarm)
                return "용접 전류 이탈 " + Ka(c.LastCurrentKa.Value) + " kA (기준 " + Ka(CurrentMinKa) + "~" + Ka(CurrentMaxKa) + ")";
            if (c.TipShots >= TipLimitShots) return "팁 사용 " + Count(c.TipShots) + " 타점 (한계 " + Count(TipLimitShots) + ")";
            if (c.TipShots >= TipDressingShots) return "팁 드레싱 대기 " + Count(c.TipShots) + " 타점";
            return c.State == CellState.Alarm ? "설비 알람" : null;
        }

        // 판정판 순위: 알람 > 정지 > 경고
        static int PanelRank(StatusKind k) { return k == StatusKind.Alarm ? 3 : k == StatusKind.Stop ? 2 : k == StatusKind.Warn ? 1 : 0; }

        static bool OnPanel(WeldCell c) { return c.State != CellState.Waiting && PanelRank(CellStatus(c)) > 0; }

        /// <summary>라인에서 가장 나쁜 셀(알람 > 정지 > 경고, 대기 제외). 같으면 화면 순서 첫째. 없으면 null.</summary>
        public static WorstCell Worst(WeldCell[] cells)
        {
            WorstCell worst = null;
            foreach (var c in cells)
            {
                if (!OnPanel(c)) continue;
                var k = CellStatus(c);
                if (worst == null || PanelRank(k) > PanelRank(worst.Status)) worst = new WorstCell(c, k, Reason(c));
            }
            return worst;
        }

        /// <summary>판정판에 오르는 셀 중 가장 나쁜 단계와 같은 단계인 셀 수.</summary>
        public static int SameLevelCount(WeldCell[] cells, StatusKind kind)
        {
            int n = 0;
            foreach (var c in cells) if (OnPanel(c) && CellStatus(c) == kind) n++;
            return n;
        }
    }
}
