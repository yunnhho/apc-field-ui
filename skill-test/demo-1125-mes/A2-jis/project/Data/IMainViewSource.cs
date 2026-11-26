using System;
using System.Collections.Generic;

namespace DoorTrimJisShipping.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(OEM 서열 수신, DB, 스캐너 후처리)은 이 인터페이스를 구현해 넣는다.
    /// 라벨 형식: "품번/시리얼" (예: 82307-AB111/SN2611250001).
    /// </summary>
    internal interface IMainViewSource
    {
        DateTime Now();
        DateTime Deadline();
        IList<SeqRow> GetSequences();
        IList<SlotRow> GetSlots();
        IList<SubstituteRecord> GetSubstituteHistory();

        /// <summary>다음 빈 칸에 부품 라벨 판정. 형식 오류·중복 시리얼은 Accepted=false.</summary>
        ScanResult Scan(string label);

        /// <summary>NG 칸에 버퍼 재고 부품을 넣고 이력을 남긴다. 검증 실패 시 오류 문구, 성공 시 null.</summary>
        string ValidateSubstitute(int slot, string label);
        void Substitute(int slot, string label, string reason);

        void ConfirmShipment();
    }
}
