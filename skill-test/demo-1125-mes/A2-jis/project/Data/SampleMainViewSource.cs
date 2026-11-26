using System;
using System.Collections.Generic;
using System.Linq;

namespace DoorTrimJisShipping.Data
{
    /// <summary>화면 확인·render용 샘플. 서열 20401~20412가 대차 1~12칸, 20417은 결번, 8칸은 오투입 NG.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        private const int FirstSeq = 20401, SlotCount = 12, MissingSeq = 20417, SequenceRows = 24, NgSlot = 8, OkSlots = 7;
        private readonly DateTime _now = new DateTime(2026, 11, 25, 13, 47, 20);
        private readonly List<SeqRow> _seqs = new List<SeqRow>();
        private readonly List<SlotRow> _slots = new List<SlotRow>();
        private readonly List<SubstituteRecord> _history = new List<SubstituteRecord>();

        public SampleMainViewSource()
        {
            var eta = new DateTime(2026, 11, 25, 14, 12, 0);
            for (int i = 0; i < SequenceRows; i++)
            {
                int seq = FirstSeq + i;
                if (seq == MissingSeq) { _seqs.Add(new SeqRow { Seq = seq, State = SeqState.Missing }); continue; }
                var spec = SpecMap.All[(i * 5 + i / 3) % SpecMap.All.Count];
                _seqs.Add(new SeqRow
                {
                    Seq = seq, Vin = "KMTGA41C7PU" + (318200 + i * 13 + i % 4), Spec = spec,
                    Eta = eta.AddSeconds(i * 70 + i % 3 * 15),
                    State = seq == 20415 ? SeqState.Changed : seq == 20421 ? SeqState.Cancelled : SeqState.New,
                });
            }
            for (int i = 0; i < SlotCount; i++)
            {
                var s = _seqs[i];
                var slot = new SlotRow { Slot = i + 1, Seq = s.Seq, Spec = s.Spec };
                if (slot.Slot <= OkSlots) Fill(slot, s.Spec.Part, "SN26112500" + (30 + slot.Slot));
                else if (slot.Slot == NgSlot) Fill(slot, SpecMap.OtherSpeaker(s.Spec).Part, "SN2611250041");
                _slots.Add(slot);
            }
            _history.Add(new SubstituteRecord { At = new DateTime(2026, 11, 25, 12, 41, 8), Slot = 3, Seq = 20327, OldSerial = "SN2611250012", NewSerial = "SN2611240907", Reason = "외관 NG" });
            _history.Add(new SubstituteRecord { At = new DateTime(2026, 11, 25, 13, 19, 52), Slot = 11, Seq = 20371, OldSerial = "", NewSerial = "SN2611240915", Reason = "결품" });
            _history.Add(new SubstituteRecord { At = new DateTime(2026, 11, 25, 13, 36, 4), Slot = 6, Seq = 20376, OldSerial = "SN2611250058", NewSerial = "SN2611240921", Reason = "품번 불일치" });
            var older = new[] { "외관 NG", "품번 불일치", "라벨 불량", "결품", "외관 NG", "품번 불일치" };
            for (int i = 0; i < older.Length; i++)
                _history.Add(new SubstituteRecord { At = new DateTime(2026, 11, 25, 7, 20, 0).AddMinutes(i * 37 + i * i * 5).AddSeconds(i * 19 % 50), Slot = (i * 5) % 12 + 1, Seq = 20100 + i * 41 + i % 3,
                    OldSerial = older[i] == "결품" ? "" : "SN26112400" + (10 + i * 7), NewSerial = "SN26112409" + (30 + i * 3), Reason = older[i] });
            _history.Sort((a, b) => b.At.CompareTo(a.At));   // 최근이 위
        }

        public DateTime Now() { return _now; }
        public DateTime Deadline() { return new DateTime(2026, 11, 25, 14, 0, 0); }
        public IList<SeqRow> GetSequences() { return _seqs; }
        public IList<SlotRow> GetSlots() { return _slots; }
        public IList<SubstituteRecord> GetSubstituteHistory() { return _history; }

        public ScanResult Scan(string label)
        {
            string part, serial;
            if (!Parse(label, out part, out serial)) return Reject("라벨 형식이 아닙니다 (품번/시리얼): " + label);
            var dup = Shipped(serial);
            if (dup != null) return Reject("같은 시리얼이 이미 " + dup + " 칸에 있습니다: " + serial);
            var slot = _slots.FirstOrDefault(s => s.Verdict == SlotVerdict.Waiting);
            if (slot == null) return Reject("모든 칸을 스캔했습니다.");
            Fill(slot, part, serial);
            return new ScanResult { Accepted = true };
        }

        public string ValidateSubstitute(int slot, string label)
        {
            string part, serial;
            if (!Parse(label, out part, out serial)) return "라벨 형식이 아닙니다 (품번/시리얼).";
            var target = _slots[slot - 1];
            if (part != target.Spec.Part) return "품번 " + part + " 은(는) 지정 품번 " + target.Spec.Part + " 와 다릅니다.";
            var dup = Shipped(serial);
            return dup != null ? "같은 시리얼이 이미 " + dup + " 칸에 있습니다." : null;
        }

        public void Substitute(int slot, string label, string reason)
        {
            string part, serial;
            Parse(label, out part, out serial);
            var s = _slots[slot - 1];
            _history.Insert(0, new SubstituteRecord { At = _now, Slot = slot, Seq = s.Seq, OldSerial = s.ScannedSerial ?? "", NewSerial = serial, Reason = reason });
            Fill(s, part, serial);
        }

        public void ConfirmShipment()
        {
            // 샘플: 확정하면 다음 대차를 위해 칸을 비운다. 실제는 출하 이력 저장(시리얼 중복 출하 차단 포함).
            foreach (var s in _slots) { s.Verdict = SlotVerdict.Waiting; s.ScannedPart = null; s.ScannedSerial = null; s.NgReason = null; }
        }

        private static void Fill(SlotRow slot, string part, string serial)
        {
            slot.ScannedPart = part;
            slot.ScannedSerial = serial;
            bool ok = part == slot.Spec.Part;
            slot.Verdict = ok ? SlotVerdict.Ok : SlotVerdict.Ng;
            slot.NgReason = ok ? null : "품번 " + part + " 이(가) 지정 " + slot.Spec.Part + " 와 다름";
        }

        private string Shipped(string serial)
        {
            var hit = _slots.FirstOrDefault(s => s.ScannedSerial == serial);
            return hit == null ? null : hit.Slot.ToString("00");
        }

        private static bool Parse(string label, out string part, out string serial)
        {
            part = serial = null;
            var p = (label ?? "").Trim().Split('/');
            if (p.Length != 2 || p[0].Length == 0 || p[1].Length == 0) return false;
            part = p[0]; serial = p[1];
            return true;
        }

        private static ScanResult Reject(string msg) { return new ScanResult { Accepted = false, Message = msg }; }
    }
}
