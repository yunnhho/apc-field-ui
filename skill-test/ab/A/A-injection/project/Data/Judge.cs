using APC.UI.Theming;

namespace InjectionMonitor.Data
{
    /// <summary>판정 결과. Phrase = "상한 초과" 같은 사유(정상이면 null), Item = 항목명.</summary>
    internal sealed class Verdict
    {
        public Verdict(StatusKind status, string item, string phrase)
        {
            Status = status; Item = item; Phrase = phrase;
        }

        public StatusKind Status { get; private set; }
        public string Item { get; private set; }
        public string Phrase { get; private set; }
    }

    /// <summary>
    /// 판정 함수 한 곳. 상한과 같은 값은 알람이 아니다(초과부터), 경고는 시작값과 같은 값부터.
    /// 기준값은 요구서에 없어 정한 값이다 — 아래 상수를 고치면 화면 전체가 따라간다.
    /// </summary>
    internal sealed class Judge
    {
        // 정한 기준값(레시피 값에서의 간격)
        public const double TempAlarmBand = 5.0;          // 금형 온도: 설정 ± 5.0 ℃ 밖이면 알람
        public const double TempWarnBand = 4.0;           // 설정 ± 4.0 ℃ 이상 벗어나면 경고
        public const double PressureWarnMargin = 14.0;    // 압력 경고 시작 = 상한 - 14.0 bar
        public const int CycleAlarmOver = 3;              // 사이클 타임 상한 = 목표 + 3 s
        public const int CycleWarnOver = 2;               // 사이클 타임 경고 시작 = 목표 + 2 s

        private readonly Recipe _recipe;

        public Judge(Recipe recipe) { _recipe = recipe; }

        public double TempUpper { get { return _recipe.MoldTempSet + TempAlarmBand; } }
        public double TempLower { get { return _recipe.MoldTempSet - TempAlarmBand; } }
        public double PressureWarn { get { return _recipe.PressureUpper - PressureWarnMargin; } }
        public double CycleUpper { get { return _recipe.CycleTarget + CycleAlarmOver; } }
        public double CycleWarn { get { return _recipe.CycleTarget + CycleWarnOver; } }

        public Verdict Temp(double v)
        {
            const string item = "금형 온도";
            if (v > TempUpper) return new Verdict(StatusKind.Alarm, item, "상한 초과");
            if (v < TempLower) return new Verdict(StatusKind.Alarm, item, "하한 미달");
            if (v >= _recipe.MoldTempSet + TempWarnBand) return new Verdict(StatusKind.Warn, item, "상한 근접");
            if (v <= _recipe.MoldTempSet - TempWarnBand) return new Verdict(StatusKind.Warn, item, "하한 근접");
            return new Verdict(StatusKind.Run, item, null);
        }

        public Verdict Pressure(double v)
        {
            const string item = "사출 압력";
            if (v > _recipe.PressureUpper) return new Verdict(StatusKind.Alarm, item, "상한 초과");
            if (v >= PressureWarn) return new Verdict(StatusKind.Warn, item, "상한 근접");
            return new Verdict(StatusKind.Run, item, null);
        }

        public Verdict Cycle(double v)
        {
            const string item = "사이클 타임";
            if (v > CycleUpper) return new Verdict(StatusKind.Alarm, item, "상한 초과");
            if (v >= CycleWarn) return new Verdict(StatusKind.Warn, item, "상한 근접");
            return new Verdict(StatusKind.Run, item, null);
        }

        /// <summary>설비 한 대 = 공정 상태와 값 판정 중 나쁜 쪽. 가동이 아니거나 값이 없으면 판정하지 않는다(Stop).</summary>
        public Verdict Machine(Machine m)
        {
            if (!m.Running || !m.HasValue) return new Verdict(StatusKind.Stop, null, null);
            Verdict worst = Temp(m.Temp);
            foreach (var v in new[] { Pressure(m.Pressure), Cycle(m.Cycle) })
                if (Rank(v.Status) > Rank(worst.Status)) worst = v;
            return worst;
        }

        /// <summary>타일 문구: "알람 · 금형 온도 상한 초과" / "경고 · ..." / "가동 · 정상" / 대기·정지.</summary>
        public string TileText(Machine m)
        {
            var v = Machine(m);
            if (!m.Running) return m.ProcessText;
            if (v.Status == StatusKind.Alarm) return "알람 · " + v.Item + " " + v.Phrase;
            if (v.Status == StatusKind.Warn) return "경고 · " + v.Item + " " + v.Phrase;
            return "가동 · 정상";
        }

        private static int Rank(StatusKind k)
        {
            return k == StatusKind.Alarm ? 2 : k == StatusKind.Warn ? 1 : 0;
        }
    }
}
