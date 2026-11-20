namespace CleanroomMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(센서 수집, DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>구역 8곳(A1~A4, B1~B4)의 현재값·추이.</summary>
        ZoneReading[] GetZones();

        /// <summary>구역별 기준 + 데이터 저장 주기.</summary>
        MonitorSettings GetSettings();

        /// <summary>설정 dialog에서 저장한 값을 반영한다.</summary>
        void SaveSettings(MonitorSettings settings);
    }
}
