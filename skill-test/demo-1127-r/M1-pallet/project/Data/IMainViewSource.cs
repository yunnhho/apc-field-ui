namespace PalletPacker.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(DB, 라벨 프린터 등)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        WorkOrder GetWorkOrder();
        PalletState GetPallet();

        /// <summary>박스 라벨 한 건 적재. 품번 불일치·중복·형식 오류·팔레트 가득은 거부.</summary>
        ScanResult ScanBox(string code);

        /// <summary>팔레트 마감 기록. 성공하면 true.</summary>
        bool ClosePallet(string palletLabel, string badge, decimal totalKg);
    }
}
