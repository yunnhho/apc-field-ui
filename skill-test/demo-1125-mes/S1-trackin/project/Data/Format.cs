namespace WireBondEapTerminal.Data
{
    /// <summary>표시 형식: 수량 #,0 · Q-time 남은 시간 h:mm · RTD 점수 0.0.</summary>
    internal static class Format
    {
        public static string Hm(int minutes)
        {
            return (minutes / 60) + ":" + (minutes % 60).ToString("00");
        }

        public static string Qty(int n)
        {
            return n.ToString("#,0");
        }
    }
}
