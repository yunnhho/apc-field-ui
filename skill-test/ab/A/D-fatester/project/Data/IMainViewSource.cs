using System.Collections.Generic;

namespace FATester.Data
{
    /// <summary>메인 화면 데이터 자리. 실제 연결(PLC, DB, 시리얼)은 이 인터페이스를 구현해 넣는다.</summary>
    internal interface IMainViewSource
    {
        /// <summary>CH1~CH4 현재 상태·시료·측정값·챔버 값.</summary>
        IList<ChannelRow> GetChannels();

        /// <summary>불량 분류 결과(최근 판정이 위).</summary>
        IList<DefectRow> GetDefects();
    }
}
