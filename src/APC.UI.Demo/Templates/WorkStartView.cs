using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>작업 시작: 작업 지시 그리드 + 선택 지시 상세 + 시작 버튼.</summary>
    internal sealed class WorkStartView : ApcTemplateView
    {
        private static readonly object[][] Orders =
        {
            new object[] { StatusKind.Stop, "WO-24117-01", "하우징 A-120", 1200, "사출기 03" },
            new object[] { StatusKind.Stop, "WO-24117-02", "커버 B-045", 800, "사출기 03" },
            new object[] { StatusKind.Warn, "WO-24117-03", "브래킷 C-310", 450, "프레스 01" },
            new object[] { StatusKind.Run, "WO-24116-09", "하우징 A-120", 1500, "사출기 02" },
            new object[] { StatusKind.Alarm, "WO-24116-07", "커버 B-045", 600, "사출기 01" },
        };

        private const int SampleExtraOrders = 28;
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly Label _detail = Caption("작업 지시를 선택하세요");

        public WorkStartView() : base("작업 시작 · 사출기 03")
        {
            _grid.Columns.Add(new ApcLampColumn());
            _grid.Columns.Add("no", "지시번호");
            _grid.Columns.Add("item", "품목");
            _grid.Columns.Add("qty", "지시수량");
            _grid.Columns.Add("line", "설비");
            // 짧은 값 열뿐이면 남는 폭을 고르게 나눈다(한 열만 Fill이면 나머지가 오른쪽 끝으로 밀린다).
            foreach (DataGridViewColumn c in _grid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (var o in Orders) _grid.Rows.Add(o);
            // 샘플은 표 높이만큼 채운다(대기 지시). 실제로는 데이터 인터페이스가 준다.
            string[] items = { "하우징 A-120", "커버 B-045", "브래킷 C-310" };
            for (int i = 0; i < SampleExtraOrders; i++)
                _grid.Rows.Add(StatusKind.Stop, "WO-24118-" + (i + 1).ToString("00"), items[i % items.Length], (4 + i % 5) * 100, "사출기 0" + (i % 3 + 1));
            _grid.SelectionChanged += (s, e) => ShowDetail();

            var start = Button("▶ 작업 시작", ApcButtonVariant.Primary, 140);
            start.Click += (s, e) => StartSelected();

            var bottom = Row(_detail);
            bottom.Dock = DockStyle.Bottom;
            bottom.Padding = new Padding(0, S(12), 0, 0);
            bottom.Controls.Add(start);

            Body.Controls.Add(_grid);
            Body.Controls.Add(bottom);
        }

        private void ShowDetail()
        {
            if (_grid.CurrentRow == null) return;
            var r = _grid.CurrentRow.Cells;
            _detail.Text = r[1].Value + "  " + r[2].Value + "  " + r[3].Value + "개  @" + r[4].Value;
        }

        private void StartSelected()
        {
            if (_grid.CurrentRow == null) return;
            var cell = _grid.CurrentRow.Cells[0];
            if ((StatusKind)cell.Value == StatusKind.Run) { ApcDialog.Warn(FindForm(), "이미 진행 중인 작업입니다: " + _grid.CurrentRow.Cells[1].Value); return; }
            cell.Value = StatusKind.Run;
        }
    }
}
