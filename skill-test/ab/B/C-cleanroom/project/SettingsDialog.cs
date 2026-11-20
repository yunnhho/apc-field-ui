using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;
using CleanroomMonitor.Data;

namespace CleanroomMonitor
{
    /// <summary>
    /// 설정 dialog: 구역마다 한 행인 입력 표(온도·습도 상하한, 파티클 상한) + 데이터 저장 주기.
    /// 저장 확인 → Result 속성 → OK. 입력 오류는 경고창.
    /// </summary>
    internal sealed class SettingsDialog : ApcForm
    {
        private const int ColTempLow = 1, ColTempHigh = 2, ColHumiLow = 3, ColHumiHigh = 4, ColParticle = 5;
        private const int MaxIntervalSec = 3600;

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private readonly ApcGrid _grid = new ApcGrid { ClearInitialSelection = false, EditMode = DataGridViewEditMode.EditOnEnter,
            SelectionMode = DataGridViewSelectionMode.CellSelect, MultiSelect = false, ScrollBars = ScrollBars.None };
        private readonly ApcNumericBox _interval = new ApcNumericBox { Minimum = 1, Maximum = MaxIntervalSec, DecimalPlaces = 0, Increment = 1 };
        private readonly int _zoneCount;

        public SettingsDialog() : this(new SampleMainViewSource().GetZones(), new SampleMainViewSource().GetSettings()) { }

        public SettingsDialog(ZoneReading[] zones, MonitorSettings settings)
        {
            _zoneCount = zones.Length;
            SetupDialog("설정", 1000);

            int rowH = ApcTheme.Px(44), headH = ApcTheme.Px(40);
            AddContent(SectionTitle("구역별 기준"));
            _grid.RowTemplate.Height = rowH;
            _grid.ColumnHeadersHeight = headH;
            _grid.AllowUserToResizeColumns = false;
            AddColumn("zone", "구역", 100, true).DefaultCellStyle.Font = ApcTheme.GetFont(FontStyle.Bold);
            AddColumn("tlo", "온도 하한(℃)", 160, false);
            AddColumn("thi", "온도 상한(℃)", 160, false);
            AddColumn("hlo", "습도 하한(%RH)", 170, false);
            AddColumn("hhi", "습도 상한(%RH)", 170, false);
            AddColumn("part", "파티클 상한(개/ft³)", 120, false).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (DataGridViewColumn c in _grid.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            for (int i = 0; i < zones.Length; i++)
            {
                var l = settings.Limits[i];
                _grid.Rows.Add(zones[i].Name, ZoneJudge.T(l.TempLow), ZoneJudge.T(l.TempHigh), ZoneJudge.T(l.HumiLow), ZoneJudge.T(l.HumiHigh), ZoneJudge.P(l.ParticleMax));
            }
            _grid.CellEndEdit += (s, e) => Reformat(e.RowIndex, e.ColumnIndex);
            _grid.Height = headH + rowH * zones.Length + 2;
            AddContent(_grid);

            _interval.Value = settings.SaveIntervalSec;
            AddFieldRow("데이터 저장 주기 (초)", _interval);

            var ok = new ApcButton { Text = "저장", Variant = ApcButtonVariant.Primary };
            ok.Click += (s, e) => Save();
            SetFooter(new ApcButton { Text = "취소", DialogResult = DialogResult.Cancel }, ok);
            Shown += (s, e) => { _grid.CurrentCell = _grid.Rows[0].Cells[ColTempLow]; _grid.Focus(); };
        }

        public MonitorSettings Result { get; private set; }

        private static bool TryNumber(object cell, out double value)
        {
            string t = Convert.ToString(cell).Trim().Replace(",", "");
            return double.TryParse(t, NumberStyles.Float, Inv, out value) && value >= 0;
        }

        private void Reformat(int row, int col)
        {
            double v;
            var cell = _grid.Rows[row].Cells[col];
            if (!TryNumber(cell.Value, out v)) return;   // 틀린 값은 저장할 때 경고
            cell.Value = col == ColParticle ? ZoneJudge.P(v) : ZoneJudge.T(v);
        }

        private bool Read(int row, int col, string label, out double v)
        {
            if (TryNumber(_grid.Rows[row].Cells[col].Value, out v)) return true;
            ApcDialog.Warn(this, _grid.Rows[row].Cells[0].Value + " " + label + "에 0 이상의 숫자를 입력하세요.");
            _grid.CurrentCell = _grid.Rows[row].Cells[col];
            return false;
        }

        private void Save()
        {
            _grid.EndEdit();
            var limits = new ZoneLimits[_zoneCount];
            for (int i = 0; i < _zoneCount; i++)
            {
                double tl, th, hl, hh, pm;
                if (!Read(i, ColTempLow, "온도 하한", out tl) || !Read(i, ColTempHigh, "온도 상한", out th)
                    || !Read(i, ColHumiLow, "습도 하한", out hl) || !Read(i, ColHumiHigh, "습도 상한", out hh)
                    || !Read(i, ColParticle, "파티클 상한", out pm)) return;
                string name = Convert.ToString(_grid.Rows[i].Cells[0].Value);
                if (tl >= th) { Reject(i, ColTempLow, name + " 온도 하한은 상한보다 작아야 합니다."); return; }
                if (hl >= hh) { Reject(i, ColHumiLow, name + " 습도 하한은 상한보다 작아야 합니다."); return; }
                if (hh > 100) { Reject(i, ColHumiHigh, name + " 습도 상한은 100 이하여야 합니다."); return; }
                limits[i] = new ZoneLimits(tl, th, hl, hh, pm);
            }
            if (!ApcDialog.Confirm(this, "설정을 저장하시겠습니까?", "저장", ApcButtonVariant.Primary)) return;
            Result = new MonitorSettings(limits, (int)_interval.Value);
            DialogResult = DialogResult.OK;
        }

        private void Reject(int row, int col, string message)
        {
            ApcDialog.Warn(this, message);
            _grid.CurrentCell = _grid.Rows[row].Cells[col];
        }

        private DataGridViewColumn AddColumn(string name, string header, int width, bool readOnly)
        {
            int i = _grid.Columns.Add(name, header);
            var c = _grid.Columns[i];
            c.Width = ApcTheme.Px(width);
            c.ReadOnly = readOnly;
            return c;
        }
    }
}
