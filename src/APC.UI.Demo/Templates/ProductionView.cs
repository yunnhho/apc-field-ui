using System;
using System.Windows.Forms;
using APC.UI.Controls;
using APC.UI.Theming;

namespace APC.UI.Demo.Templates
{
    /// <summary>실적 입력: 스캔 → 그리드 행 추가 → F9 저장. 행 삭제는 확인창.</summary>
    internal sealed class ProductionView : ApcTemplateView
    {
        private const int SampleRows = 32;
        private const string LotPrefix = "LOT-";

        private readonly ApcScanBox _scan = new ApcScanBox();
        private readonly ApcGrid _grid = new ApcGrid { Dock = DockStyle.Fill, ReadOnly = true };

        public ProductionView() : base("실적 입력 · 사출기 03")
        {
            _scan.Width = S(320);
            _scan.Scanned += OnScanned;

            var delete = Button("행 삭제", ApcButtonVariant.Danger);
            delete.Click += (s, e) => DeleteSelected();
            var save = Button("저장 (F9)", ApcButtonVariant.Primary);
            save.Click += (s, e) => Save();

            _grid.Columns.Add(new ApcLampColumn());
            _grid.Columns.Add("lot", "LOT");
            _grid.Columns.Add("item", "품목");
            _grid.Columns.Add("qty", "수량");
            _grid.Columns.Add("time", "스캔 시각");
            // 짧은 값 열뿐이면 남는 폭을 고르게 나눈다(한 열만 Fill이면 나머지가 오른쪽 끝으로 밀린다).
            foreach (DataGridViewColumn c in _grid.Columns) if (!(c is ApcLampColumn)) c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            // 샘플: 미저장(Warn) 몇 건 + 저장된(Run) 실적을 표 높이만큼. 실제로는 데이터 인터페이스가 준다.
            var at = new DateTime(2026, 11, 18, 11, 40, 0);
            for (int i = 0; i < SampleRows; i++, at = at.AddMinutes(-4))
                _grid.Rows.Add(i < 2 ? StatusKind.Warn : StatusKind.Run, LotPrefix + "241118-" + (260 - i).ToString("000"), "하우징 A-120", 120, at.ToString("HH:mm:ss"));

            var hint = Caption("바코드를 스캔하세요 (포커스 위치와 무관). 직접 입력 후 Enter도 가능");
            hint.Font = ApcTheme.GetFont();
            _scan.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; if (!AddLot(_scan.Text.Trim())) _scan.Flash(false); } };

            // 도킹은 나중에 추가한 컨트롤부터 → 스캔 줄이 맨 위, 그리드가 남은 영역
            Body.Controls.Add(_grid);
            Body.Controls.Add(Row(delete, save));
            Body.Controls.Add(Row(Caption("스캔"), _scan, hint));
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var form = FindForm();
            if (form != null) _scan.Attach(form);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F9) { Save(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // 스캔 피드백은 ApcScanBox가 e.Accepted로 한 번만 낸다.
        private void OnScanned(object sender, ScanEventArgs e) { e.Accepted = AddLot(e.Code); }

        /// <summary>스캔 처리(빨간 깜빡임)가 끝난 뒤 경고창을 띄운다 — 스캔 이벤트 안에서 모달을 열지 않는다.</summary>
        private void WarnAfterScan(string message)
        {
            BeginInvoke((MethodInvoker)(() => ApcDialog.Warn(FindForm(), message)));
        }

        private bool AddLot(string code)
        {
            if (!code.StartsWith(LotPrefix, StringComparison.OrdinalIgnoreCase))
            {
                WarnAfterScan("LOT 바코드가 아닙니다: " + code);
                return false;
            }
            foreach (DataGridViewRow row in _grid.Rows)
                if ((string)row.Cells[1].Value == code)
                {
                    WarnAfterScan("이미 스캔한 LOT: " + code);
                    return false;
                }
            _grid.Rows.Insert(0, StatusKind.Warn, code, "하우징 A-120", 120, DateTime.Now.ToString("HH:mm:ss"));
            _scan.Clear();
            return true;
        }

        private void DeleteSelected()
        {
            if (_grid.CurrentRow == null) return;
            if (ApcDialog.Confirm(FindForm(), _grid.CurrentRow.Cells[1].Value + " 실적을 삭제할까요?"))
                _grid.Rows.Remove(_grid.CurrentRow);
        }

        private void Save()
        {
            foreach (DataGridViewRow row in _grid.Rows) row.Cells[0].Value = StatusKind.Run;
        }
    }
}
