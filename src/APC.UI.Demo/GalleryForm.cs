using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Demo.Catalog;
using APC.UI.Demo.Templates;
using APC.UI.Theming;

namespace APC.UI.Demo
{
    /// <summary>왼쪽 메뉴(카탈로그 3개 + 기능 템플릿 6개 + 참조 화면), 오른쪽에 선택 화면.</summary>
    internal sealed class GalleryForm : ApcForm
    {
        private readonly Panel _host = new Panel { Dock = DockStyle.Fill };
        private ApcButton _current;

        public GalleryForm()
        {
            float s = ApcTheme.Scale;
            TitleText = "APC.UI 갤러리 — " + ApcTheme.Current.Name;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size((int)(1600 * s), (int)(900 * s));
            MinimumSize = new Size((int)(1024 * s), (int)(640 * s));

            var menu = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = (int)(200 * s),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding((int)(10 * s)),
                BackColor = Color.Transparent,
            };
            AddMenu(menu, "컨트롤 카탈로그", () => new ControlCatalog());
            AddMenu(menu, "표시 컨트롤", () => new DisplayCatalog());
            AddMenu(menu, "dialog 템플릿", () => new DialogCatalog());
            AddMenu(menu, "작업 시작", () => new WorkStartView());
            AddMenu(menu, "실적 입력", () => new ProductionView());
            AddMenu(menu, "불량 등록", () => new DefectView());
            AddMenu(menu, "설비 상태", () => new EquipmentView());
            AddMenu(menu, "판정", () => new JudgeView());
            AddMenu(menu, "이력 조회", () => new HistoryView());
            AddMenu(menu, "[참조] 온습도", () => new TempHumiView());
            AddMenu(menu, "[참조] 연마기 불량방지", () => new FailureAnalysisView());
            AddMenu(menu, "[참조] 호기×항목 추이", () => new TrendMatrixView());

            Controls.Add(_host);
            Controls.Add(menu);
            ((ApcButton)menu.Controls[0]).PerformClick();
        }

        private void AddMenu(FlowLayoutPanel menu, string text, Func<Control> create)
        {
            float s = ApcTheme.Scale;
            var b = new ApcButton { Text = text, Width = menu.Width - (int)(20 * s), Height = (int)(40 * s), Margin = new Padding(0, 0, 0, (int)(6 * s)) };
            b.Click += (sender, e) => Show(b, create());
            menu.Controls.Add(b);
        }

        private void Show(ApcButton button, Control view)
        {
            if (_current != null) _current.Variant = ApcButtonVariant.Normal;
            _current = button;
            _current.Variant = ApcButtonVariant.Primary;

            // 이전 화면은 Dispose — 스캔 입력의 메시지 필터·타이머가 남지 않게
            while (_host.Controls.Count > 0) _host.Controls[0].Dispose();
            _host.Controls.Add(view);
        }
    }
}
