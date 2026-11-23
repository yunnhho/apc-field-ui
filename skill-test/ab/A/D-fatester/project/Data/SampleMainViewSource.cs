using System.Collections.Generic;

namespace FATester.Data
{
    /// <summary>화면 확인·render용 샘플 데이터.</summary>
    internal sealed class SampleMainViewSource : IMainViewSource
    {
        public IList<ChannelRow> GetChannels()
        {
            return new[]
            {
                new ChannelRow("CH1", ChannelRow.Testing, "", 62, "FA-2611-0042", "열충격", "42.3", "8.6", "125.0", "35.0", "310", "500", ""),
                new ChannelRow("CH2", ChannelRow.Done, "", 100, "FA-2611-0038", "고온고습", "38.7", "5.2", "85.0", "85.0", "96", "96", ""),
                new ChannelRow("CH3", ChannelRow.Error, "인가 실패", 37, "FA-2611-0045", "전기특성", "61.5", "112.4", "25.0", "45.0", "12", "30", "E-304"),
                new ChannelRow("CH4", ChannelRow.Waiting, "", 0, "", "", "", "", "", "", "", "", ""),
            };
        }

        public IList<DefectRow> GetDefects()
        {
            return new[]
            {
                new DefectRow("FA-2611-0041", "와이어 본드 단선", "Pad 12", "15:42:18"),
                new DefectRow("FA-2611-0036", "계면 박리", "Die 가장자리", "15:21:05"),
                new DefectRow("FA-2611-0033", "누설전류 과다", "Pin 7", "14:58:47"),
                new DefectRow("FA-2611-0031", "솔더 크랙", "Ball B4", "14:30:09"),
                new DefectRow("FA-2611-0029", "보이드", "Die Attach 중앙", "13:47:51"),
                new DefectRow("FA-2611-0024", "패키지 균열", "몰드 모서리", "13:12:33"),
                new DefectRow("FA-2611-0022", "단락", "Pin 3 - Pin 4", "12:39:26"),
                new DefectRow("FA-2611-0017", "계면 박리", "리드프레임 하단", "11:55:02"),
                new DefectRow("FA-2611-0013", "와이어 본드 단선", "Pad 5", "11:08:44"),
                new DefectRow("FA-2611-0009", "솔더 크랙", "Ball D2", "10:26:19"),
                new DefectRow("FA-2611-0006", "누설전류 과다", "Pin 11", "09:47:58"),
                new DefectRow("FA-2611-0002", "보이드", "Die Attach 가장자리", "09:05:30"),
                new DefectRow("FA-2610-0187", "패키지 균열", "몰드 상단", "08:41:12"),
                new DefectRow("FA-2610-0183", "단락", "Pin 8 - Pin 9", "08:16:47"),
                new DefectRow("FA-2610-0176", "솔더 크랙", "Ball A1", "07:52:03"),
                new DefectRow("FA-2610-0171", "와이어 본드 단선", "Pad 20", "07:19:38"),
                new DefectRow("FA-2610-0165", "계면 박리", "Die 모서리", "06:48:55"),
                new DefectRow("FA-2610-0160", "누설전류 과다", "Pin 2", "06:11:21"),
                new DefectRow("FA-2610-0154", "보이드", "Die Attach 하단", "05:37:09"),
                new DefectRow("FA-2610-0149", "솔더 크랙", "Ball C6", "05:02:44"),
            };
        }
    }
}
