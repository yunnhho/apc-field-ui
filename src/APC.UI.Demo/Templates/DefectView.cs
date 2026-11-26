using System;
using System.Windows.Forms;
using APC.UI.Controls;

namespace APC.UI.Demo.Templates
{
    /// <summary>불량 등록: 유형 + 수량 + LOT(스캔 또는 입력) → 등록.</summary>
    internal sealed class DefectView : ApcTemplateView
    {
        private const int SampleRows = 34;
        private static readonly string[] Types = { "미성형", "버(Burr)", "변색", "치수 불량", "이물" };

        private readonly ApcComboBox _type = new ApcComboBox();
        private readonly ApcNumericBox _qty = new ApcNumericBox { Minimum = 1, Maximum = 9999, Value = 1 };
        private readonly ApcScanBox _lot = new ApcScanBox();
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };

        public DefectView() : base("불량 등록 · 사출기 03")
        {
            _type.Items.AddRange(Types);
            _type.SelectedIndex = 0;
            _type.Width = S(160);
            _qty.Width = S(150);
            _lot.Width = S(220);

            var add = Button("등록", ApcButtonVariant.Primary);
            add.Click += (s, e) => Register();

            _grid.Columns.Add("time", "시각");
            _grid.Columns.Add("type", "불량 유형");
            _grid.Columns.Add("qty", "수량");
            _grid.Columns.Add("lot", "LOT");
            // 짧은 값 열뿐이면 남는 폭을 고르게 나눈다(한 열만 Fill이면 나머지가 오른쪽 끝으로 밀린다).
            foreach (DataGridViewColumn c in _grid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            // 샘플: 오늘 등록된 불량을 표 높이만큼. 실제로는 데이터 인터페이스가 준다.
            var at = new DateTime(2026, 11, 18, 11, 40, 0);
            for (int i = 0; i < SampleRows; i++, at = at.AddMinutes(-9))
                _grid.Rows.Add(at.ToString("HH:mm:ss"), _type.Items[i % _type.Items.Count], 1 + i % 3, "LOT-241118-" + (180 - i).ToString("000"));

            Body.Controls.Add(_grid);
            Body.Controls.Add(Row(Caption("유형"), _type, Caption("수량"), _qty, Caption("LOT"), _lot, add));
            Body.Controls.SetChildIndex(_grid, 0);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _lot.Attach(form);
        }

        private void Register()
        {
            if (_lot.Text.Trim().Length == 0)
            {
                _lot.Flash(false);
                ApcDialog.Warn(FindForm(), "LOT를 스캔하거나 입력하세요.");
                return;
            }
            _grid.Rows.Insert(0, DateTime.Now.ToString("HH:mm:ss"), _type.SelectedItem, _qty.Value, _lot.Text.Trim());
            _lot.Clear();
            _qty.Value = 1;
        }
    }
}
