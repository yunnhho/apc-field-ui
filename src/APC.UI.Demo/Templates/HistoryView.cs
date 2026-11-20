using System;
using System.Windows.Forms;
using APC.UI.Controls;

namespace APC.UI.Demo.Templates
{
    /// <summary>이력 조회: 기간 필터 + 그리드 + 엑셀 내보내기(자리만, 비활성).</summary>
    internal sealed class HistoryView : ApcTemplateView
    {
        private readonly ApcDateBox _from = new ApcDateBox();
        private readonly ApcDateBox _to = new ApcDateBox();
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };

        public HistoryView() : base("이력 조회")
        {
            _from.Value = DateTime.Today.AddDays(-10);   // 샘플: 11일 × 설비 3대 = 표 높이만큼
            _to.Value = DateTime.Today;
            _from.Width = _to.Width = S(160);

            var search = Button("조회", ApcButtonVariant.Primary, 90);
            search.Click += (s, e) => Search();
            var export = Button("엑셀 내보내기", ApcButtonVariant.Normal, 130);
            export.Enabled = false;

            _grid.Columns.Add("date", "일자");
            _grid.Columns.Add("line", "설비");
            _grid.Columns.Add("item", "품목");
            _grid.Columns.Add("good", "양품");
            _grid.Columns.Add("bad", "불량");
            // 짧은 값 열뿐이면 남는 폭을 고르게 나눈다(한 열만 Fill이면 나머지가 오른쪽 끝으로 밀린다).
            foreach (DataGridViewColumn c in _grid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            Body.Controls.Add(_grid);
            Body.Controls.Add(Row(Caption("기간"), _from, Caption("~"), _to, search, export));
            Body.Controls.SetChildIndex(_grid, 0);
            Search();
        }

        private void Search()
        {
            if (_from.Value.Date > _to.Value.Date)
            {
                ApcDialog.Warn(FindForm(), "시작일이 종료일보다 늦습니다.");   // 조작·입력 경고는 경고창(Warn), 알람바는 설비 이상만
                return;
            }
            _grid.Rows.Clear();
            int seed = 0;
            for (var d = _to.Value.Date; d >= _from.Value.Date; d = d.AddDays(-1))
            {
                for (int m = 1; m <= 3; m++)   // 샘플: 하루 × 설비 3대
                {
                    seed++;
                    _grid.Rows.Add(d.ToString("yyyy-MM-dd"), "사출기 0" + m, "하우징 A-120", (1100 + seed * 13 % 200).ToString("#,0"), seed * 7 % 23);
                }
            }
        }
    }
}
