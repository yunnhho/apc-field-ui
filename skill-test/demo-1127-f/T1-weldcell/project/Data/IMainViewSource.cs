namespace SpotWeldMonitor.Data
{
    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB, 시리얼)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>셀 6대(WR-01~WR-06), 화면 순서.</summary>
        WeldCell[] GetCells();

        /// <summary>가동률·NG 수·드레싱 대기 셀 수.</summary>
        LineValues GetLineValues();

        /// <summary>오늘 알람 이력, 최신이 앞.</summary>
        AlarmRecord[] GetAlarmHistory();

        /// <summary>그 셀의 최근 1시간 용접 전류(kA), 과거가 앞. 마지막 점 = 셀의 마지막 용접 전류.</summary>
        double[] GetCurrentTrend(string cellName);

        /// <summary>팁 교체 등록. 셀이 가동 중이면 소스도 거부한다. 성공 시 true.</summary>
        bool RegisterTipChange(string cellName, string badge);
    }
}
