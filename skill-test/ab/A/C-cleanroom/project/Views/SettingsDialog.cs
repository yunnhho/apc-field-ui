using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CleanroomMonitor.Data;

namespace CleanroomMonitor.Views
{
    /// <summary>설정 dialog: 구역별 온도·습도 상하한, 파티클 상한을 표에 바로 입력하고 데이터 저장 주기를 정한다.</summary>
    internal sealed class SettingsDialog : ApcForm
    {
        private const int ColZone = 0, ColTempLow = 1, ColTempHigh = 2, ColHumiLow = 3, ColHumiHigh = 4, ColParticle = 5;
        private const int MaxSaveIntervalSec = 3600;

        private readonly ApcGrid _grid = new ApcGrid { ReadOnly = false, ClearInitialSelection = false, EditMode = DataGridViewEditMode.EditOnEnter,
            SelectionMode = DataGridViewSelectionMode.CellSelect, MultiSelect = false, ScrollBars = ScrollBars.None };
        private readonly ApcNumericBox _interval = new ApcNumericBox { Minimum = 1, Maximum = MaxSaveIntervalSec, DecimalPlaces = 0, Increment = 1 };
        private readonly int _rows;

        public SettingsDialog() : this(new SampleMainViewSource().GetLimits(), 10) { }

        public SettingsDialog(IList<ZoneLimits> limits, int saveIntervalSec)
        {
            _rows = limits.Count;
            SetupDialog("설정", 960);
            AddContent(SectionTitle("구역별 기준"));

            AddColumn("구역", true).Width = ApcTheme.Px(100);
            AddColumn("온도 하한(℃)", false);
            AddColumn("온도 상한(℃)", false);
            AddColumn("습도 하한(%RH)", false);
            AddColumn("습도 상한(%RH)", false);
            AddColumn("파티클 상한(개/ft³)", false);
            foreach (DataGridViewColumn c in _grid.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (c.Index != ColZone) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            foreach (var l in limits)
                _grid.Rows.Add(l.Zone, l.TempLow.ToString("0.0"), l.TempHigh.ToString("0.0"),
                    l.HumiLow.ToString("0.0"), l.HumiHigh.ToString("0.0"), l.ParticleHigh.ToString("#,0"));
            _grid.Height = ApcGrid.HeightForRows(_rows);
            AddContent(_grid);

            _interval.Value = saveIntervalSec;
            AddFieldRow("데이터 저장 주기 (s)", _interval);

            var ok = new ApcButton { Text = "확정", Variant = ApcButtonVariant.Primary };
            ok.Click += (s, e) => Confirm();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, ok);
            Shown += (s, e) => { _grid.CurrentCell = _grid.Rows[0].Cells[ColTempLow]; _grid.Focus(); };
        }

        public IList<ZoneLimits> Limits { get; private set; }
        public int SaveIntervalSec { get; private set; }

        private DataGridViewColumn AddColumn(string header, bool readOnly)
        {
            int i = _grid.Columns.Add("c" + _grid.Columns.Count, header);
            var c = _grid.Columns[i];
            c.ReadOnly = readOnly;
            return c;
        }

        private void Confirm()
        {
            _grid.EndEdit();
            _interval.Commit();
            var result = new List<ZoneLimits>();
            for (int i = 0; i < _rows; i++)
            {
                ZoneLimits l = ReadRow(i);
                if (l == null) return;
                result.Add(l);
            }
            if (!ApcDialog.Confirm(this, "설정을 저장합니다.", "저장", ApcButtonVariant.Primary)) return;
            Limits = result;
            SaveIntervalSec = (int)_interval.Value;
            DialogResult = DialogResult.OK;
        }

        /// <summary>한 행을 읽고 검증한다. 잘못이면 경고창을 띄우고 null.</summary>
        private ZoneLimits ReadRow(int i)
        {
            var cells = _grid.Rows[i].Cells;
            string zone = Convert.ToString(cells[ColZone].Value);
            double tl, th, hl, hh, p;
            if (!TryNum(cells[ColTempLow], out tl) || !TryNum(cells[ColTempHigh], out th) ||
                !TryNum(cells[ColHumiLow], out hl) || !TryNum(cells[ColHumiHigh], out hh) ||
                !TryNum(cells[ColParticle], out p) || p < 1)
            {
                ApcDialog.Warn(this, zone + " 구역에 숫자가 아닌 값이 있습니다.");
                return null;
            }
            if (tl >= th || hl >= hh)
            {
                ApcDialog.Warn(this, zone + " 구역의 하한은 상한보다 작아야 합니다.");
                return null;
            }
            return new ZoneLimits(zone, tl, th, hl, hh, (int)p);
        }

        private static bool TryNum(DataGridViewCell cell, out double value)
        {
            return double.TryParse(Convert.ToString(cell.Value), NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out value);
        }
    }
}
