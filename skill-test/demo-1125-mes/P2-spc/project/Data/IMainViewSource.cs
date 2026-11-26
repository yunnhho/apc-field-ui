using System;
using System.Collections.Generic;

namespace ShaftSpcMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(DB, 측정기)은 이 인터페이스를 구현해 넣는다.
    /// 관리 한계는 Phase I에서 고정한 값이며 화면에서 바꾸는 동작은 두지 않는다(4M 변경 승인 후 별도 절차).
    /// </summary>
    internal interface IMainViewSource
    {
        IList<Characteristic> Characteristics { get; }
        /// <summary>항목의 부분군(시간순, 마지막이 최신).</summary>
        IList<Subgroup> GetSubgroups(int item);
        DateTime Now { get; }
        DateTime NextMeasurement { get; }
        /// <summary>부분군 저장. 다음 측정 예정은 주기만큼 뒤로 간다.</summary>
        void AddSubgroup(int item, double[] values);
    }
}
