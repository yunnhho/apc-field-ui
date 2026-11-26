using System.Collections.Generic;
using APC.UI.Theming;

namespace Polishing.Data
{
    /// <summary>설비 진행 상태. 판정(StatusKind)과는 UnitJudge가 합친다.</summary>
    internal enum UnitState { Waiting, Sent, Running, Done, Offline }

    /// <summary>스캔한 Lot 하나를 SAP·연마 기준정보로 조회한 결과(작업 조건 포함).</summary>
    internal sealed class LotRecipe
    {
        public LotRecipe(string lot, string materialCode, string chipType, int chipQty, int barrels, int timeMin)
        {
            Lot = lot; MaterialCode = materialCode; ChipType = chipType; ChipQty = chipQty; Barrels = barrels; TimeMin = timeMin;
        }

        public string Lot { get; private set; }
        public string MaterialCode { get; private set; }
        public string ChipType { get; private set; }
        public int ChipQty { get; private set; }
        public int Barrels { get; private set; }
        /// <summary>작업 시간 SV(분) — 컨트롤러 400001~2로 전송.</summary>
        public int TimeMin { get; private set; }

        /// <summary>자재코드 + 칩 구분 + 바렐수 + 시간이 같으면 같은 작업 조건(한 설비에 함께 넣을 수 있다).</summary>
        public bool SameCondition(LotRecipe other)
        {
            return other != null && MaterialCode == other.MaterialCode && ChipType == other.ChipType && Barrels == other.Barrels && TimeMin == other.TimeMin;
        }
    }

    /// <summary>설비 한 대의 현재 값(실시간 data). PV가 없으면 null(빈칸).</summary>
    internal sealed class UnitInfo
    {
        public UnitInfo(string name, int group, UnitState state, IList<string> lots, string materialCode, string chipType,
            int barrels, int svMin, double? pvMin, int rpmSv, int? rpmPv)
        {
            Name = name; Group = group; State = state; Lots = lots; MaterialCode = materialCode; ChipType = chipType;
            Barrels = barrels; SvMin = svMin; PvMin = pvMin; RpmSv = rpmSv; RpmPv = rpmPv;
        }

        public string Name { get; private set; }
        /// <summary>0 = 고속, 1 = 저속</summary>
        public int Group { get; private set; }
        public UnitState State { get; private set; }
        public IList<string> Lots { get; private set; }
        public string MaterialCode { get; private set; }
        public string ChipType { get; private set; }
        public int Barrels { get; private set; }
        public int SvMin { get; private set; }
        public double? PvMin { get; private set; }
        public int RpmSv { get; private set; }
        public int? RpmPv { get; private set; }
    }

    /// <summary>SAP 리턴(ZWORK3033_A) 전송 이력 한 줄. 구분: 작업 조건 / 시작 5분 후 / 완료.</summary>
    internal sealed class SendRecord
    {
        public SendRecord(string time, string unit, string lot, string kind, StatusKind status, string result)
        {
            Time = time; Unit = unit; Lot = lot; Kind = kind; Status = status; Result = result;
        }

        public string Time { get; private set; }
        public string Unit { get; private set; }
        public string Lot { get; private set; }
        public string Kind { get; private set; }
        public StatusKind Status { get; private set; }
        public string Result { get; private set; }
    }

    /// <summary>한 설비의 판정 결과. Reason은 비어 있을 수 있다(사유 없음).</summary>
    internal sealed class UnitVerdict
    {
        public UnitVerdict(StatusKind status, string stateWord, string reason)
        {
            Status = status; StateWord = stateWord; Reason = reason;
        }

        public StatusKind Status { get; private set; }
        public string StateWord { get; private set; }
        public string Reason { get; private set; }
    }
}
