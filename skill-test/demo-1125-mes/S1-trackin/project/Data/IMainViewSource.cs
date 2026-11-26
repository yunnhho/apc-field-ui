using System;
using System.Collections.Generic;

namespace WireBondEapTerminal.Data
{
    internal enum HsmsState { NotConnected, NotSelected, Selected }
    internal enum GemControl { Offline, OnlineLocal, OnlineRemote }
    internal enum EquipState { Idle, Run, Alarm }
    internal enum VerifyState { Ok, Ng, NotChecked }
    internal enum EventKind { ProcessStart, ProcessEnd, AlarmSet, AlarmClear, RemoteCommand }

    /// <summary>장비 한 대 상태 (HSMS·GEM·공정).</summary>
    internal sealed class EquipmentInfo
    {
        public string Name;
        public HsmsState Hsms;
        public string LinktestAt;       // HH:mm:ss, 마지막 Linktest 성공 시각
        public GemControl Gem;
        public EquipState State;
        public string StateDetail;      // 카드 아래 한 줄
    }

    /// <summary>진행 중인 Lot.</summary>
    internal sealed class RunningLot
    {
        public string LotId, Product, Recipe;
        public int Strips, DoneStrips;
        public bool ProcessEnded;       // S6F11 Process End 수신 → Track-out 가능
        public double Percent { get { return Strips == 0 ? 0 : 100.0 * DoneStrips / Strips; } }
    }

    /// <summary>대기 Lot 목록 한 줄 (RTD 순위 0 = 제외).</summary>
    internal sealed class WaitingLot
    {
        public int Rank;                // 0 = Hold·Q-time 초과로 순위 제외
        public double Score;
        public string LotId, Product;
        public int QRemainMinutes;      // Q-time 남은 분 (초과면 0)
        public string HoldReason;       // null = Hold 아님 (Q-time 초과 포함 문구)
    }

    internal sealed class CheckRow
    {
        public string Group, Item, Reason;
        public VerifyState State;
    }

    internal sealed class TrackInResult
    {
        public string LotId;
        public List<CheckRow> Checks = new List<CheckRow>();
        public bool Passed;
        public string BlockReason;      // 첫 NG 항목 문구, 통과면 null
    }

    internal sealed class EventRow
    {
        public EventKind Kind;
        public DateTime At;
        public string Message, Text;    // Message = S6F11 / S5F1 / S2F41
    }

    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(HSMS/GEM, MES, RMS)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        EquipmentInfo GetEquipment();
        RunningLot GetRunningLot();                 // 진행 Lot 없으면 null
        IList<WaitingLot> GetWaitingLots();         // RTD 순위순, 제외 Lot은 뒤
        IList<EventRow> GetEvents();                // 최신이 앞
        string LastScannedLot { get; }              // 화면을 처음 열 때 보여 줄 직전 스캔(없으면 null)
        TrackInResult Verify(string lotId);         // 모르는 Lot이면 null. 검증 순서는 구현이 정한다
        void TrackIn(string lotId);                 // S2F41 PP-SELECT → START
    }
}
