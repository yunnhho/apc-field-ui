using System.Collections.Generic;
using APC.UI.Theming;

namespace FATester.Data
{
    /// <summary>화면 확인·render용 샘플 데이터.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        public IList<ChannelInfo> GetChannels()
        {
            // CH1 저항 45.0 = 경고 시작값(경고). CH3 오류 채널의 값은 멈춘 값이라 판정하지 않는다.
            return new List<ChannelInfo>
            {
                new ChannelInfo("CH1", StatusKind.Run, "시험중", 62, "FA-2611-0412", "열충격", 45.0, 38.6, 125.0, 12.0, 1860, "E-114", "챔버 온도 편차 (2026-11-19 22:41:05)"),
                new ChannelInfo("CH2", StatusKind.Run, "완료", 100, "FA-2611-0409", "고온고습", 32.4, 41.2, 85.0, 85.0, 1000, "E-203", "습도 센서 응답 지연 (2026-11-19 14:08:32)"),
                new ChannelInfo("CH3", StatusKind.Alarm, "오류", 37, "FA-2611-0415", "전기특성", 28.7, 63.0, 25.0, 45.0, 412, "E-231", "프로브 접촉 불량 (2026-11-20 09:12:40)"),
                new ChannelInfo("CH4", StatusKind.Stop, "대기", 0, "", "", null, null, 25.0, 45.0, 2250, "E-102", "도어 열림 감지 (2026-11-18 17:55:19)"),
            };
        }

        public IList<DefectRow> GetDefects()
        {
            return new List<DefectRow>
            {
                new DefectRow("FA-2611-0407", "와이어 본드 단선", "Pin 14", "09:48:12"),
                new DefectRow("FA-2611-0402", "다이 크랙", "Die 좌상단", "09:21:57"),
                new DefectRow("FA-2611-0398", "패키지 박리", "몰드 컴파운드 우측", "08:56:03"),
                new DefectRow("FA-2611-0391", "솔더 보이드", "Ball B7", "08:30:44"),
                new DefectRow("FA-2611-0386", "리드 변형", "Pin 3", "08:02:19"),
                new DefectRow("FA-2611-0379", "와이어 본드 단선", "Pin 22", "07:41:36"),
                new DefectRow("FA-2611-0373", "패키지 박리", "다이 패드 하단", "07:15:08"),
                new DefectRow("FA-2611-0368", "다이 크랙", "Die 중앙", "06:52:51"),
                new DefectRow("FA-2611-0361", "솔더 보이드", "Ball C2", "06:30:27"),
                new DefectRow("FA-2611-0355", "리드 변형", "Pin 9", "06:04:14"),
                new DefectRow("FA-2611-0349", "와이어 본드 단선", "Pin 31", "05:38:46"),
                new DefectRow("FA-2611-0344", "패키지 박리", "몰드 컴파운드 상단", "05:11:02"),
                new DefectRow("FA-2611-0338", "다이 크랙", "Die 우하단", "04:47:35"),
            };
        }
    }
}
