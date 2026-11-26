using System;
using System.Collections.Generic;
using System.Linq;

namespace DoorTrimJisShipping.Data
{
    /// <summary>앞좌석 도어 트림 사양: 색상 4 x 스피커 2 x 좌우 2 = 16개 품번. 사양 코드 -> 품번 매핑 테이블.</summary>
    internal sealed class Spec
    {
        public Spec(string color, string speaker, string side, string part)
        {
            Color = color; Speaker = speaker; Side = side; Part = part;
        }
        public string Color { get; private set; }
        public string Speaker { get; private set; }
        public string Side { get; private set; }
        public string Part { get; private set; }
        public string Code { get { return Color + "-" + Speaker + "-" + Side; } }
    }

    internal static class SpecMap
    {
        public static readonly IList<Spec> All = Build();

        private static IList<Spec> Build()
        {
            var colors = new[] { "BK", "GY", "BE", "BR" };      // 블랙·그레이·베이지·브라운
            var speakers = new[] { "S2", "S4" };                // 2스피커·4스피커
            var sides = new[] { "L", "R" };
            var list = new List<Spec>();
            for (int c = 0; c < colors.Length; c++)
                for (int s = 0; s < speakers.Length; s++)
                    for (int d = 0; d < sides.Length; d++)
                        list.Add(new Spec(colors[c], speakers[s], sides[d], "82307-AB" + (c + 1) + (s + 1) + (d + 1)));
            return list;
        }

        public static Spec ByCode(string code) { return All.First(s => s.Code == code); }

        /// <summary>같은 색·좌우, 스피커만 다른 사양(오투입 샘플).</summary>
        public static Spec OtherSpeaker(Spec x) { return All.First(s => s.Color == x.Color && s.Side == x.Side && s.Speaker != x.Speaker); }
    }

    internal enum SeqState { New, Changed, Cancelled, Missing }

    /// <summary>OEM 서열 수신 한 줄. Missing = 결번(수신 건너뜀) 자리 표시.</summary>
    internal sealed class SeqRow
    {
        public int Seq { get; set; }
        public string Vin { get; set; }
        public Spec Spec { get; set; }
        public DateTime Eta { get; set; }
        public SeqState State { get; set; }
    }

    internal enum SlotVerdict { Waiting, Ok, Ng }

    /// <summary>대차 한 칸: 할당 서열, 지정 품번, 스캔한 부품, 판정.</summary>
    internal sealed class SlotRow
    {
        public int Slot { get; set; }
        public int Seq { get; set; }
        public Spec Spec { get; set; }
        public string ScannedPart { get; set; }
        public string ScannedSerial { get; set; }
        public SlotVerdict Verdict { get; set; }
        public string NgReason { get; set; }
    }

    internal sealed class ScanResult
    {
        public bool Accepted { get; set; }   // false = 라벨 형식 오류·중복 시리얼 등으로 칸에 넣지 않음
        public string Message { get; set; }
    }

    internal sealed class SubstituteRecord
    {
        public DateTime At { get; set; }
        public int Slot { get; set; }
        public int Seq { get; set; }
        public string OldSerial { get; set; }
        public string NewSerial { get; set; }
        public string Reason { get; set; }
    }
}
