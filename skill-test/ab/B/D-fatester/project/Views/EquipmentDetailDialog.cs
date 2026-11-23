using System.Globalization;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FATester.Data;

namespace FATester.Views
{
    /// <summary>설비 상세 dialog: 채널 상태 줄 + 챔버 온도·습도·누적 사이클·최근 오류 코드. 값은 메인 화면에서 받는다.</summary>
    internal sealed class EquipmentDetailDialog : ApcForm
    {
        public EquipmentDetailDialog() : this(new SampleMainViewSource().GetChannels()[2]) { }

        public EquipmentDetailDialog(ChannelInfo c)
        {
            SetupDialog(c.Name + " 설비 상세", 800);
            string reason;
            StatusKind kind = Judge.Channel(c, out reason);
            AddContent(new ApcHeaderBar
            {
                Title = c.Name + (c.SampleId == "" ? "" : " · " + c.SampleId + " · " + c.TestItem),
                Status = kind,
                StatusText = c.ProcessText + (reason != null ? " · " + reason : "")
            });
            AddContentRow(Card("챔버 온도", c.ChamberTemp.ToString("0.0", CultureInfo.InvariantCulture), "℃", ""),
                          Card("챔버 습도", c.ChamberHumidity.ToString("0.0", CultureInfo.InvariantCulture), "%RH", ""));
            AddContentRow(Card("누적 사이클", c.Cycles.ToString("#,0", CultureInfo.InvariantCulture), "회", ""),
                          Card("최근 오류 코드", c.LastErrorCode, "", c.LastErrorText));
            var close = new ApcButton { Text = "닫기", DialogResult = DialogResult.OK };
            SetFooter(null, close);
        }

        private static ApcValueCard Card(string title, string value, string unit, string footer)
        {
            return new ApcValueCard { Title = title, Value = value, Unit = unit, Footer = footer, Status = StatusKind.Run, Height = ApcTheme.Px(110) };
        }
    }
}
