using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace FinalTestYieldMonitor
{
    internal sealed class MainForm : ApcForm
    {
        public MainForm()
        {
            float s = ApcTheme.Scale;
            TitleText = "Final Test 수율·Bin 모니터링";   // TODO: 창 제목을 프로그램 이름으로(예: "도장 라인 모니터링")
            FunctionText = "테스트 구역 전체";        // TODO: 제목 아랫줄 기능명(예: "작업 시작 · LOT 스캔"). 비우면 한 줄 제목
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size((int)(1920 * s), (int)(1080 * s));
            MinimumSize = new Size((int)(1024 * s), (int)(640 * s));
            Controls.Add(new Views.MainView());
        }
    }
}
