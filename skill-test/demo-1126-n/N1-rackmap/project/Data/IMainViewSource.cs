using System;
using System.Collections.Generic;

namespace FgRackLocator.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(WMS DB 등)은 이 인터페이스를 구현해 넣는다.
    /// 입고 추천·FIFO 판정(Evaluate)과 확정 반영(Confirm...)까지 여기서 한다 — 화면은 결과만 그린다.
    /// </summary>
    internal interface IMainViewSource
    {
        DateTime Today { get; }
        string CurrentWorker { get; }

        /// <summary>팔레트 바코드로 조회. 모르는 팔레트면 null.</summary>
        Pallet FindPallet(string palletCode);

        /// <summary>해당 위치의 팔레트. 빈칸이면 null.</summary>
        Pallet GetPalletAt(string location);

        /// <summary>입고 추천(빈칸) 또는 출고 대상 위치와 FIFO 판정. 모르는 팔레트면 null.</summary>
        ScanResult Evaluate(string palletCode);

        /// <summary>담당 사원증(FIFO 예외 허용 권한)인가.</summary>
        bool IsAuthorizedBadge(string badge);

        void ConfirmInbound(Pallet pallet, string location);

        /// <summary>approverBadge: FIFO 예외로 출고했으면 허용한 사원증, 아니면 null.</summary>
        void ConfirmOutbound(Pallet pallet, string approverBadge);

        /// <summary>오늘 입출고 이력(최신이 앞).</summary>
        IList<HistoryEntry> GetTodayHistory();
    }
}
