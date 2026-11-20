using System.Collections.Generic;

namespace FATester.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(시험기 PLC, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>CH1~CH4 현재 상태.</summary>
        IList<ChannelInfo> GetChannels();

        /// <summary>불량 분류 결과(최근 순).</summary>
        IList<DefectRow> GetDefects();
    }
}
