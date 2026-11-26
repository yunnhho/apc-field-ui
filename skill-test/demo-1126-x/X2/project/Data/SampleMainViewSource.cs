using System;
using System.Collections.Generic;

namespace Polish.Data
{
    /// <summary>화면 확인·render용 샘플 데이터. 설비 56칸 = 14호기 × 4칸(-1~-4).</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const int GreenSeconds = 3600, FiredSeconds = 7200;   // 그린칩 1시간 · 소성칩 2시간
        private const int PositionsPerUnit = 4;

        private static readonly string[] Names =
            { "저속연마 1~3호기", "저속연마 4~6호기", "고속연마 1~4호기", "고속연마 5~8호기" };
        private static readonly int[] UnitsPerGroup = { 3, 3, 4, 4 };
        private static readonly string[] GreenCodes =
            { "Q3216X7R105K101NRINN02F", "Q3225X7S106K500ARLNN01F", "Q2012X7R475K250NRENN03F" };
        private static readonly string[] FiredCodes = { "S1005X5R104K160NRKNN01F", "S2012X5R226M250NRENN03F" };

        public string[] GroupNames { get { return Names; } }

        public IList<EquipmentRow> GetEquipment(int group)
        {
            int first = 0;
            for (int i = 0; i < group; i++) first += UnitsPerGroup[i] * PositionsPerUnit;
            bool low = group < 2;
            int firstUnit = group == 1 ? 4 : group == 3 ? 5 : 1;
            var rows = new List<EquipmentRow>();
            for (int u = 0; u < UnitsPerGroup[group]; u++)
                for (int p = 1; p <= PositionsPerUnit; p++)
                {
                    string name = (low ? "저속연마 " : "고속연마 ") + (firstUnit + u) + "호기 -" + p;
                    rows.Add(Build(name, low, first + rows.Count));
                }
            return rows;
        }

        public PolishCondition FindCondition(string lot)
        {
            switch (lot)
            {
                case "B052610003412":
                case "B052610003587": return new PolishCondition("그린칩", GreenCodes[0], GreenSeconds);
                case "B052610004871": return new PolishCondition("소성칩", FiredCodes[0], FiredSeconds);
                default: return null;
            }
        }

        // TODO: 실제로는 담당자 계정/비밀번호 저장소에서 확인한다. 샘플은 빈 값만 거부.
        public bool CheckEngineerPassword(string password) { return !string.IsNullOrEmpty(password); }

        public bool SendCondition(string equipment, string[] lots, string[] barrels, PolishCondition condition) { return true; }

        private static EquipmentRow Build(string name, bool low, int g)
        {
            if (g == 20 || g == 37) return new EquipmentRow(name, low, EquipPhase.Offline, new string[0], new string[0], null, 0, null);
            int slots = low ? 2 : 4;
            EquipPhase phase = Phase(g);
            if (phase == EquipPhase.Idle) return new EquipmentRow(name, low, EquipPhase.Idle, new string[0], new string[0], null, 0, null);

            int lotCount = 1 + g % slots;
            var lots = new string[lotCount];
            var barrels = new string[lotCount];
            for (int i = 0; i < lotCount; i++)
            {
                lots[i] = "B05261" + (3000 + g * 177 + i * 41).ToString("0000000");
                barrels[i] = ((g * 7 + i * 5) % 40 + 1).ToString("00");
            }
            bool fired = g % 4 == 3;
            PolishCondition cond = fired
                ? new PolishCondition("소성칩", FiredCodes[g % FiredCodes.Length], FiredSeconds)
                : new PolishCondition("그린칩", GreenCodes[g % GreenCodes.Length], GreenSeconds);
            if (phase == EquipPhase.Sent) return new EquipmentRow(name, low, phase, lots, barrels, cond, 0, null);

            int elapsed = 200 + g * 733 % (cond.TargetSeconds - 600);
            int rpm = low ? 362 + g * 5 % 17 : 560 + g * 11 % 37;   // 저속 362~378 (기준 안), 고속은 기준 없음
            if (g == 0) { elapsed = 1800; rpm = 380; }                              // 상한과 같음 → 정상
            if (g == 2) { elapsed = 2460; rpm = 352; }                              // 하한 밖 → 알람
            if (g == 3) { elapsed = cond.TargetSeconds - 120; rpm = 345; }          // 종료 3분 전 → 알람 없음
            return new EquipmentRow(name, low, phase, lots, barrels, cond, elapsed, rpm);
        }

        private static EquipPhase Phase(int g)
        {
            if (g == 0 || g == 2 || g == 3) return EquipPhase.Running;
            switch (g % 7)
            {
                case 0: case 3: case 5: return EquipPhase.Running;
                case 1: return EquipPhase.Sent;
                default: return EquipPhase.Idle;
            }
        }
    }
}
