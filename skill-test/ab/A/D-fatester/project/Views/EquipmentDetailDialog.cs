using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using FATester.Data;

namespace FATester.Views
{
    /// <summary>설비 상세 dialog: 상태 줄 + 챔버 온도·습도·누적 사이클·최근 오류 코드. 값은 메인에서 넘겨받는다.</summary>
    internal sealed class EquipmentDetailDialog : ApcForm
    {
        public EquipmentDetailDialog() : this(new SampleMainViewSource().GetChannels()[0]) { }

        public EquipmentDetailDialog(ChannelRow ch)
        {
            SetupDialog(ch.Name + " 설비 상세", 640);

            bool waiting = ch.State == ChannelRow.Waiting;
            // 판정 기준이 없는 값: 갱신 중이면 Run, 멈췄으면(오류·대기) Stop
            var valueKind = ch.State == ChannelRow.Testing || ch.State == ChannelRow.Done ? StatusKind.Run : StatusKind.Stop;
            string recv = waiting ? "" : "수신 15:50:12";
            string cyclesFooter = waiting ? "" : "목표 " + ch.TargetCycles + " 회";

            AddContent(new ApcHeaderBar { Title = ch.Name + (ch.SampleId.Length > 0 ? " · " + ch.SampleId + " · " + ch.TestItem : ""), Status = ch.Status, StatusText = ch.StatusText });
            AddContentRow(
                Card("챔버 온도", ch.ChamberTemp, "℃", valueKind, recv),
                Card("챔버 습도", ch.ChamberHumi, "%RH", valueKind, recv));
            AddContentRow(
                Card("누적 사이클", ch.Cycles, "회", valueKind, cyclesFooter),
                Card("최근 오류 코드", ch.ErrorCode.Length > 0 ? ch.ErrorCode : (waiting ? "" : "없음"), "", ch.ErrorCode.Length > 0 ? StatusKind.Alarm : valueKind, ch.ErrorCode.Length > 0 ? ch.Reason : (waiting ? "" : "이상 없음")));

            var close = new ApcButton { Text = "닫기", DialogResult = DialogResult.OK };
            SetFooter(null, close);
            AcceptButton = close;
        }

        private static ApcValueCard Card(string title, string value, string unit, StatusKind status, string footer)
        {
            return new ApcValueCard { Title = title, Value = value, Unit = unit, Status = status, Footer = footer, ShowLamp = true, Height = ApcTheme.Px(120) };
        }
    }
}
