using System;
using System.Drawing;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>
    /// 보기용 상세 dialog 골격: 센서 표(램프가 상태) + 추이 + 오른쪽 아래 닫기. 설비 상태 문구는 메인 화면에 있으므로 되풀이하지 않는다.
    /// 값은 메인 화면에서 넘겨받는다(자체 데이터 인터페이스 없음). 매개변수 없는 생성자는 render용 샘플.
    /// </summary>
    internal sealed class EquipmentDetailDialog : ApcForm
    {
        public EquipmentDetailDialog() : this("건조로2",
            new[] { new object[] { StatusKind.Alarm, "로 내 온도", "168.0", "160.0", "℃" }, new object[] { StatusKind.Run, "배기 팬", "1450", "1450", "rpm" } },
            SampleTrend(), 160, 165) { }

        /// <param name="sensors">행마다 { StatusKind, 센서, 현재값, 설정값, 단위 }</param>
        public EquipmentDetailDialog(string name, object[][] sensors, double[] trend, double setPoint, double upper)
        {
            SetupDialog(name + " 상세", 760, 560);

            // 표 높이 = 머리줄 + 센서 수만큼(빈 줄 없이). 남는 높이는 추이 그래프(Fill)가 쓴다.
            int gridH = ApcGrid.HeightForRows(sensors.Length);
            var grid = new ApcGrid { Height = gridH, ReadOnly = true };
            grid.Columns.Add(new ApcLampColumn());
            grid.Columns.Add("name", "센서");
            grid.Columns.Add("value", "현재값");
            grid.Columns.Add("set", "설정값");
            grid.Columns.Add("unit", "단위");
            grid.Columns["name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (var row in sensors) grid.Rows.Add(row);

            var chart = new ApcTrendChart { Reference = setPoint, UpperLimit = upper, XLabels = new[] { "-60분", "-30분", "현재" } };
            chart.SetValues(trend);

            AddContent(grid);
            AddContent(SectionTitle("최근 1시간 온도 추이 (회색 점선 = 설정값, 빨간 점선 = 상한)"));
            AddContentFill(chart);
            var close = new ApcButton { Text = "닫기", DialogResult = DialogResult.OK };
            SetFooter(null, close);   // Esc·Enter = 닫기
            AcceptButton = close;
        }

        private static double[] SampleTrend()
        {
            var v = new double[61];
            for (int i = 0; i <= 60; i++) v[i] = 160 + Math.Sin(Math.Min(i, 45) / 5.0) + (i > 45 ? (i - 45) * (8.0 - Math.Sin(9.0)) / 15 : 0);   // 마지막 점 = 168.0(현재값과 같게)
            return v;
        }
    }
}
