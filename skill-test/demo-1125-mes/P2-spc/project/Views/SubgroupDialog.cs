using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using ShaftSpcMonitor.Data;

namespace ShaftSpcMonitor.Views
{
    /// <summary>
    /// 부분군 측정값 입력: 값 5개를 표에 바로 입력(측정기·스캐너 값 + Enter → 다음 칸). 입력하는 대로 X̄·R을 보인다.
    /// 5개가 다 차야 저장 버튼이 켜지고(이유는 버튼 옆 글), 규격 밖 값이 있으면 저장 때 재측정 확인을 거친다.
    /// </summary>
    internal sealed class SubgroupDialog : ApcForm
    {
        private const int ValueCol = 1, JudgeCol = 2;
        private readonly Characteristic _item;
        private readonly ApcGrid _grid = new ApcGrid { ReadOnly = false, ClearInitialSelection = false, SelectionMode = DataGridViewSelectionMode.CellSelect };
        private readonly ApcValueCard _xbar = new ApcValueCard { Title = "X̄", Unit = "mm", ShowLamp = true, Height = ApcTheme.Px(110) };
        private readonly ApcValueCard _range = new ApcValueCard { Title = "R", Unit = "mm", ShowLamp = true, Height = ApcTheme.Px(110) };
        private readonly Label _message = new Label { AutoSize = false, Height = ApcTheme.Px(ApcLayout.FieldHeight), TextAlign = ContentAlignment.MiddleLeft };
        private readonly ApcButton _save = new ApcButton { Text = "저장", Variant = ApcButtonVariant.Primary, Enabled = false };

        /// <summary>render용 샘플: 축 지름 A, 4개 입력(그중 하나는 규격 밖).</summary>
        public SubgroupDialog() : this(new Characteristic("축 지름 A", 25.000, 0.300, 0.200), 126, new DateTime(2026, 11, 25, 14, 0, 0))
        {
            double[] sample = { 25.041, 25.087, 25.312, 25.012 };
            for (int i = 0; i < sample.Length; i++) _grid.Rows[i].Cells[ValueCol].Value = sample[i].ToString("0.000", CultureInfo.InvariantCulture);
            Recalculate();
        }

        public SubgroupDialog(Characteristic item, int number, DateTime slot)
        {
            _item = item;
            SetupDialog("부분군 측정값 입력", 620);

            AddFieldRow("측정 항목", ReadOnlyText(item.Name + " · 부분군 #" + number + " · " + slot.ToString("yyyy-MM-dd HH:mm")));
            AddFieldRow("규격", ReadOnlyText(Fmt(item.Lsl) + " ~ " + Fmt(item.Usl) + " mm"));

            AddContent(SectionTitle("측정값 (mm) · 값 입력 후 Enter"));
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "no", HeaderText = "번호", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "value", HeaderText = "측정값(mm)", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _grid.Columns.Add(new ApcLampColumn { Name = "judge", HeaderText = "규격 판정", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            for (int i = 1; i <= Characteristic.SubgroupSize; i++) _grid.Rows.Add(i.ToString(), "", null);
            _grid.CurrentCell = _grid.Rows[0].Cells[ValueCol];
            _grid.Height = ApcGrid.HeightForRows(Characteristic.SubgroupSize);
            _grid.CellValueChanged += (s, e) => { if (e.ColumnIndex == ValueCol) Recalculate(); };
            _grid.CellEndEdit += (s, e) => Recalculate();
            AddContent(_grid);

            AddContent(SectionTitle("부분군 결과"));
            AddContentRow(_xbar, _range);
            AddContent(_message);

            _save.Click += (s, e) => Save();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, _save);
            Recalculate();
        }

        /// <summary>저장한 측정값 5개(입력 순서).</summary>
        public double[] Values { get; private set; }

        private static string Fmt(double v) { return SpcAnalysis.F(v); }

        private static ApcTextBox ReadOnlyText(string text) { return new ApcTextBox { Text = text, ReadOnly = true }; }

        /// <summary>칸 글자를 숫자로. 비었거나 숫자가 아니면 null.</summary>
        private double? ValueAt(int row)
        {
            double v;
            string text = Convert.ToString(_grid.Rows[row].Cells[ValueCol].Value, CultureInfo.InvariantCulture);
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? (double?)v : null;
        }

        private bool OutOfSpec(double v) { return v > _item.Usl || v < _item.Lsl; }

        private void Recalculate()
        {
            var vals = new double[Characteristic.SubgroupSize];
            int count = 0, bad = 0;
            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                double? v = ValueAt(i);
                var cell = _grid.Rows[i].Cells[JudgeCol];
                var valueCell = _grid.Rows[i].Cells[ValueCol];
                valueCell.Style.ForeColor = Color.Empty;
                valueCell.Style.Font = null;
                if (v == null)
                {
                    bool typed = Convert.ToString(valueCell.Value).Trim().Length > 0;
                    cell.Value = typed ? new ApcLampValue(StatusKind.Warn, "숫자가 아님") : null;
                    continue;
                }
                vals[count++] = v.Value;
                bool outSpec = OutOfSpec(v.Value);
                if (outSpec)
                {
                    bad++;
                    valueCell.Style.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                    valueCell.Style.Font = ApcTheme.GetFont(FontStyle.Bold);
                }
                cell.Value = new ApcLampValue(outSpec ? StatusKind.Alarm : StatusKind.Run, outSpec ? (v.Value > _item.Usl ? "USL 초과" : "LSL 미달") : "규격 내");
            }
            ShowResult(vals.Take(count).ToArray(), bad);
        }

        private void ShowResult(double[] v, int bad)
        {
            bool full = v.Length == Characteristic.SubgroupSize;
            _xbar.Value = v.Length == 0 ? "" : SpcAnalysis.F(v.Average());
            _range.Value = v.Length < 2 ? "" : SpcAnalysis.F(v.Max() - v.Min());
            bool xOut = full && (v.Average() > _item.XUcl || v.Average() < _item.XLcl);
            bool rOut = full && v.Max() - v.Min() > _item.RUcl;
            _xbar.Status = !full ? StatusKind.Stop : xOut ? StatusKind.Alarm : StatusKind.Run;
            _range.Status = !full ? StatusKind.Stop : rOut ? StatusKind.Alarm : StatusKind.Run;
            _xbar.Footer = (full ? "" : "잠정 · ") + "관리 한계 " + Fmt(_item.XLcl) + " ~ " + Fmt(_item.XUcl);
            _range.Footer = (full ? "" : "잠정 · ") + "관리 한계 0.000 ~ " + Fmt(_item.RUcl);

            _save.Enabled = full;
            if (!full)
            {
                _message.ForeColor = ApcTheme.C(ApcTheme.Current.Colors.TextMuted);
                _message.Text = "5개를 모두 입력해야 저장할 수 있습니다 (" + v.Length + "/5)" + (bad > 0 ? " · 규격 이탈 " + bad + "개" : "");
            }
            else if (bad > 0)
            {
                _message.ForeColor = ApcTheme.StatusText(StatusKind.Alarm);
                _message.Text = "규격 이탈 " + bad + "개 · 저장하려면 재측정 확인이 필요합니다";
            }
            else
            {
                _message.ForeColor = ApcTheme.StatusText(StatusKind.Run);
                _message.Text = "모두 규격 안입니다 · 저장할 수 있습니다";
            }
        }

        private void Save()
        {
            _grid.EndEdit();
            var vals = Enumerable.Range(0, Characteristic.SubgroupSize).Select(ValueAt).ToArray();
            if (vals.Any(v => v == null)) { ApcDialog.Warn(this, "측정값 5개를 모두 숫자로 입력하세요."); return; }
            double[] values = vals.Select(v => v.Value).ToArray();
            int bad = values.Count(OutOfSpec);
            if (bad > 0 && !ApcDialog.Confirm(this, "규격을 벗어난 값이 " + bad + "개 있습니다. 같은 부품을 다시 측정해 확인했습니까?", "재측정 확인", ApcButtonVariant.Primary)) return;
            Values = values;
            DialogResult = DialogResult.OK;
        }
    }
}
