# APC.UI API (v1.18.0.0)

> build.sh가 자동 생성한다. 직접 고치지 않는다.

## ApcAlarmBar

`APC.UI.Controls` · class : Panel

하단 알람 띠. 들어온 순서대로 쌓고, 여러 개면 3초마다 순환 표시. '확인'은 현재 보이는 알람 하나를 지운다. 비면 숨는다.
- `ApcAlarmBar()`
- `const int CycleIntervalMs`
- `event EventHandler Acknowledged`
- `int Count { get; }`
- `string CurrentMessage { get; }`
- `void AcknowledgeCurrent()`
- `void Push(string message)`

## ApcButton

`APC.UI.Controls` · class : Button

9-slice 이미지 버튼. 이미지: btn_{variant}_{normal|hover|pressed|disabled}
- `ApcButton()`
- `ApcButtonVariant Variant { get; set; }`

## ApcButtonField

`APC.UI.Controls` · class : UserControl

입력칸 + 칸 안 오른쪽 버튼 영역(콤보 화살표와 같은 모양). 테두리·포커스 테두리는 이 컨트롤이 그리고, 안쪽 입력칸은 테두리 없이 들어간다. 버튼 영역은 위아래로 나눌 수 있다(숫자칸 ▲▼). ApcNumericBox·ApcDateBox의 바탕.
- `ApcTextBox TextBox { get; }` — 안쪽 입력칸(포커스 지정·스캔 복원 대상).

## ApcButtonVariant

`APC.UI.Controls` · enum

- Normal, Primary, Danger

## ApcComboBox

`APC.UI.Controls` · class : ComboBox

테마 선택 목록(드롭다운 전용, 직접 입력 없음). 닫힌 상태는 입력칸 모양 + 오른쪽 그라데이션 화살표 버튼으로 직접 그리고, 펼친 목록은 항목 높이 28px×배율 이상(장갑 낀 손가락·마우스 모두 누르기 쉽게). 높이는 ItemHeight로 정해진다.
- `ApcComboBox()`

## ApcDateBox

`APC.UI.Controls` · class : ApcButtonField

날짜 입력: 입력칸(yyyy-MM-dd) + 칸 안 오른쪽 ▼ 버튼 영역(콤보와 같은 모양). 누르면 달력이 펼쳐지고, 날짜를 누르면 닫힌다. 직접 입력은 yyyy-MM-dd / yyyyMMdd를 받아 Enter 또는 칸을 떠날 때 확정, 틀리면 이전 값. ↑↓ 키는 하루씩.
- `ApcDateBox()`
- `DateTime Value { get; set; }`
- `const string Format`
- `event EventHandler ValueChanged`
- `protected void OnValueChanged(EventArgs e)`
- `static readonly DateTime MaxDate` — 받는 범위. 달력 이동·하루 이동이 DateTime 끝에서 예외를 내지 않게.
- `static readonly DateTime MinDate` — 받는 범위. 달력 이동·하루 이동이 DateTime 끝에서 예외를 내지 않게.
- `void Commit()` — 입력칸 글자를 확정한다. 날짜가 아니면 이전 값을 다시 보여 준다.

## ApcDialog

`APC.UI.Controls` · static class

되돌릴 수 없는 동작(삭제, 작업 종료 등) 확인. 기본 포커스와 Enter·Esc는 모두 '취소' — 바코드 스캔의 Enter나 습관적 Enter로 실수 확인되지 않게 한다.
- `static bool Confirm(IWin32Window owner, string message)` — 파괴적 동작 확인: 확인 버튼은 빨간 "확인".
- `static bool Confirm(IWin32Window owner, string message, string okText, ApcButtonVariant okVariant)` — 확인 버튼 글자·종류 지정. 저장처럼 위험하지 않은 확인은 ("저장", Primary).
- `static void Alert(IWin32Window owner, string message)` — 안내창(연결 결과·완료 알림처럼 잘못된 것이 없는 안내). 제목 "알림" + 파란 램프. 경고는 .
- `static void Warn(IWin32Window owner, string message)` — 경고창(입력 오류·조작 경고: 기간 역순, 빈 LOT, 잘못된·중복 스캔 등). 제목 "경고" + 노란 램프. 버튼은 "확인" 하나, Enter·Esc로 닫힌다.

## ApcForm

`APC.UI.Controls` · class : Form

테두리 없는 APCSOFT 창. 타이틀바(로고·제목·[설정]·최소화·닫기 이미지 버튼), 드래그 이동, 더블클릭 최대화, 가장자리 리사이즈. 최대화해도 작업표시줄을 가리지 않는다.
- `ApcForm()`
- `Rectangle ContentBounds { get; }` — 타이틀바·1px 테두리를 뺀 내용 영역(클라이언트 좌표) = Padding 안쪽.
- `bool Resizable { get; set; }`
- `bool ShowLogo { get; set; }` — 타이틀바 왼쪽 로고 + 가운데 큰 제목(메인 창). false면 로고 없이 왼쪽 제목(dialog, SetupDialog가 끈다).
- `bool ShowSettings { get; set; }` — 타이틀바 최소화 왼쪽에 설정(톱니) 버튼. 설정 화면이 있는 창에서만 켠다. 누르면 . 화면(ApcTemplateView)에서는 직접 켜지 말고 UseSettingsButton을 쓴다.
- `event EventHandler SettingsClick` — 타이틀바 설정 버튼을 눌렀다(가 true일 때만).
- `int ResizeGrip { get; }` — 가장자리 리사이즈 감지 폭(px).
- `int TitleBarHeight { get; }`
- `protected ApcGroup AddGroup(string title)` — 제목 있는 묶음(ApcGroup). 행은 AddFieldRow(group, ...)로 넣는다.
- `protected CreateParams CreateParams { get; }`
- `protected Label SectionTitle(string text)` — 구획 제목(굵게 + 아래 폭 전체 구분선). AddContent로 넣으면 아래 요소와의 간격이 좁다. 상자(AddGroup) 없이 입력 행을 나눌 때 쓴다.
- `protected static Label FieldCaption(string text)` — 행 안 trailing 이름표(보통 굵기, 글자 폭).
- `protected void AddCaptionRow(ApcGroup group, params string[] captions)` — 묶음 안 열 제목 줄. 바로 다음 AddFieldRow(group, ...) 행의 [이름표, 입력칸, trailing...] 위에 순서대로 맞춰 놓인다("" = 그 칸 제목 없음). 여러 행이 같은 칸(포트·신호·로직…)을 가질 때 행마다 FieldCaption을 되풀이하지 않고 위에 한 번만 쓴다.
- `protected void AddContent(Control c)` — 표·판정판 등 자유 요소. 높이는 c.Height(ApcTheme.Px로 지정), 폭은 내용 폭.
- `protected void AddContentFill(Control c)` — 남은 높이를 채우는 요소(그래프 등). SetupDialog에 높이를 줘야 하고 한 dialog에 하나만.
- `protected void AddContentRow(params Control[] cells)` — 카드 여러 개를 한 줄에 같은 폭·같은 높이(가장 큰 c.Height)로. 칸 간격은 ApcLayout.ItemGap. 2×2는 두 번 부른다.
- `protected void AddFieldRow(ApcGroup group, string caption, Control field, params Control[] trailing)` — 묶음 안 한 줄. 이름표 열은 묶음 밖 행과 같은 위치에 맞춘다.
- `protected void AddFieldRow(string caption, Control field, params Control[] trailing)` — 이름표 + 입력칸 한 줄. trailing이 없으면 입력칸이 남은 폭을 채우고, 있으면 모두 자기 Width로 왼→오.
- `protected void SetFooter(ApcButton cancel, ApcButton confirm, params ApcButton[] left)` — 하단 버튼 줄: 오른쪽 [확정][취소](취소가 끝, cancel은 null 가능), 왼쪽 보조 버튼. Esc = 취소(없으면 확정).
- `protected void SetupDialog(string title, int width, int height = 0)` — dialog 기본 속성과 크기(배율 전 px). height 0 = 내용으로 자동(AddContentFill을 쓰면 필수).
- `string TitleText { get; set; }`

## ApcGrid

`APC.UI.Controls` · class : DataGridView

테마 스타일을 입힌 DataGridView. 헤더 그라데이션, 행 배경 한 색(번갈아 칠하지 않음), 행 전체 선택. 사용자가 표를 누르거나 키로 움직이기 전에는 첫 행이 저절로 선택되지 않는다(CurrentRow = null) — "선택한 행에 대해" 동작하는 버튼은 CurrentRow가 null이면 아무것도 하지 않게 둔다.
- `ApcGrid()`
- `bool ClearInitialSelection { get; set; }` — true(기본)면 사용자가 누르기 전엔 선택 행이 없다. 입력 표(스캔으로 칸을 채우는 dialog 등)처럼 앱이 CurrentCell을 정해 두고 바로 입력받아야 하면 false.
- `static int HeightForRows(int rows)` — 머리줄 + 행 rows개가 빈 줄·스크롤바 없이 들어가는 표 높이(배율 적용 px). dialog의 AddContent 표 높이에 쓴다.
- `static void DrawHeaderText(Graphics g, Rectangle cell, string text, Font font)` — 머리줄 글자를 칸 정가운데에. 기본 그리기는 정렬 화살표 자리·안쪽 여백만큼 왼쪽으로 1~2px 치우친다. 오른쪽 구분선·아래 선 1px은 빼고 가운데를 잡는다. 묶음 머리줄 등 직접 그리는 곳도 이것을 쓴다.

## ApcGroup

`APC.UI.Controls` · class : Panel

제목 띠가 있는 묶음 상자(설정 dialog의 블록 등, 표준 GroupBox 대신). 제목 띠는 값 카드와 같은 그리드 헤더 그라데이션. 안에 넣는 컨트롤은 Padding(제목 띠 아래 + 여백) 안쪽에 배치된다 — Dock이나 FlowLayoutPanel(Dock=Fill)을 쓰면 된다.
- `ApcGroup()`
- `int HeightFor(int contentHeight)` — 안 내용 높이(배율 적용 px)를 담는 묶음 높이 = 제목 띠 + 위아래 여백 + 내용. 화면에서 Columns() 줄을 담으면 내용 = 줄 높이 합.
- `string Text { get; set; }` — 제목 띠 글자.

## ApcHeaderBar

`APC.UI.Controls` · class : Panel

화면 상단 띠: 왼쪽 제목(설비명 등), 오른쪽 상태 램프 + 상태 문구.
- `ApcHeaderBar()`
- `StatusKind Status { get; set; }`
- `string StatusText { get; set; }`
- `string Title { get; set; }`

## ApcIconCell

`APC.UI.Controls` · class : DataGridViewTextBoxCell

- `ApcIconCell()`
- `Type ValueType { get; }`

## ApcIconColumn

`APC.UI.Controls` · class : DataGridViewColumn

행 동작 아이콘 열(전송 ▶, 초기화 ↻ 등). 셀 값 true = 파란 아이콘(누를 수 있음), false = 회색 아이콘(지금은 못 함), 그 밖 = 빈 칸. 이미지는 테마의 "{IconName}_normal" / "{IconName}_disabled". 누름은 표의 CellClick에서 IsEnabled(셀)로 확인해 처리한다.
- `ApcIconColumn()`
- `static bool IsEnabled(DataGridViewCell cell)` — 셀이 누를 수 있는 상태(값 true)인지.
- `string IconName { get; set; }` — 테마 이미지 이름 앞부분(예: "icon_play", "icon_reset").

## ApcJudgePanel

`APC.UI.Controls` · class : Control

판정 표시판: 상태색 바탕에 큰 글자(OK / NG / READY 등) + 아래 한 줄 안내. 판정 화면의 가장 큰 요소로 쓴다. OK=Run, NG=Alarm, 대기=Stop, 확인 필요=Warn. 글자 크기는 높이에 맞춘다.
- `ApcJudgePanel()`
- `StatusKind Status { get; set; }`
- `string SubText { get; set; }` — 큰 글자 아래 한 줄(예: "시리얼을 스캔하세요", "하중 19.4 kN > 상한 18.0").
- `string Text { get; set; }` — 큰 글자(OK / NG / READY 등).

## ApcLamp

`APC.UI.Controls` · class : Control

상태 램프. 이미지: lamp_{run|warn|alarm|stop|info}. Blink는 Alarm일 때만 동작.
- `ApcLamp()`
- `StatusKind Status { get; set; }`
- `bool Blink { get; set; }`
- `const int BlinkIntervalMs`

## ApcLampCell

`APC.UI.Controls` · class : DataGridViewTextBoxCell

- `ApcLampCell()`
- `Type ValueType { get; }`

## ApcLampColumn

`APC.UI.Controls` · class : DataGridViewColumn

상태 램프 컬럼. 셀 값 StatusKind = 램프만(가운데), ApcLampValue = 램프 + 문구("전송됨", "대기") 한 칸(램프는 왼쪽 고정, 문구는 바로 옆 — 행마다 램프 위치가 같게), 그 외(null 등) = 빈 셀.
- `ApcLampColumn()`

## ApcLampValue

`APC.UI.Controls` · class

램프 + 문구 셀 값(ApcLampColumn). 정렬은 상태 → 문구 순.
- `ApcLampValue(StatusKind status, string text)`
- `StatusKind Status { get; }`
- `int CompareTo(object obj)`
- `string Text { get; }`

## ApcNumericBox

`APC.UI.Controls` · class : ApcButtonField

숫자 입력: 칸 안 오른쪽에 ▲(위)/▼(아래) 버튼 영역(콤보 화살표와 같은 모양). 버튼·↑↓ 키·마우스 휠은 Increment만큼, 직접 입력은 Enter 또는 칸을 떠날 때 확정. 범위 밖은 Minimum/Maximum으로 잘리고, 숫자가 아니면 이전 값으로 되돌린다. 소수 자릿수는 DecimalPlaces.
- `ApcNumericBox()`
- `decimal Increment { get; set; }`
- `decimal Maximum { get; set; }`
- `decimal Minimum { get; set; }`
- `decimal Value { get; set; }`
- `event EventHandler ValueChanged`
- `int DecimalPlaces { get; set; }`
- `protected void OnValueChanged(EventArgs e)`
- `void Commit()` — 입력칸 글자를 확정한다. 숫자가 아니면 이전 값을 다시 보여 준다.

## ApcScanBox

`APC.UI.Controls` · class : ApcTextBox

바코드 입력란. Attach(form) 하면 그 폼 어디에 포커스가 있든 스캔을 받는다. 스캔으로 판정되면 포커스 칸을 버스트 시작 전 값으로 되돌리고 코드를 여기로 옮긴 뒤 Scanned를 낸다. 스캔의 Enter는 삼켜서 폼의 기본 버튼(AcceptButton)을 누르지 않게 한다. 피드백은 Scanned 처리 후 한 번: e.Accepted에 따라 성공/실패.
- `ApcScanBox()`
- `const int FlashMs`
- `event EventHandler<ScanEventArgs> Scanned`
- `void Attach(Form host)` — 이 폼에서 스캔을 받기 시작한다. 화면(UserControl) 생성자에서는 아직 폼이 없으므로 화면의 OnHandleCreated에서 FindForm()으로 부른다: var f = FindForm(); if (f != null) _scan.Attach(f);
- `void Flash(bool success)` — 성공 = 초록 플래시 + 짧은 비프, 실패 = 빨강 플래시 + 경고음.

## ApcStatusTile

`APC.UI.Controls` · class : Control

설비 타일: 램프 + 설비명 + 상태 문구. 누르면 Click(상세 dialog 열기 등). 마우스를 올리거나 포커스면 파란 테두리, 알람이면 굵은 알람색 테두리. Tab으로 옮겨 Enter·Space로도 누른다.
- `ApcStatusTile()`
- `StatusKind Status { get; set; }`
- `bool Selected { get; set; }` — 선택 표시(바탕이 표 선택 행 색). 타일을 눌러 아래 카드·그래프가 그 설비를 보여 줄 때 그 타일 하나만 true.
- `string StatusText { get; set; }`
- `string Text { get; set; }` — 설비명(굵게, 램프 오른쪽 윗줄).

## ApcTabStrip

`APC.UI.Controls` · class : Control

탭 줄(설비군·라인 전환 등). 선택 탭은 흰 바탕 + 위쪽 강조선 + 굵은 글자로 아래 내용과 이어져 보이고, 나머지는 묶음 제목 띠와 같은 회색 그라데이션이다 — 실행 버튼(파란 Primary)과 모양이 겹치지 않는다. 탭 폭은 글자에 맞춰 자동(잘리지 않음). 내용 전환은 에서 화면이 한다. 클릭 또는 포커스 상태에서 ←/→로 바꾼다. 높이는 테마 입력칸 높이.
- `ApcTabStrip()`
- `event EventHandler SelectedIndexChanged` — 선택이 바뀌면(클릭·←/→·SelectedIndex 대입) 발생.
- `int PreferredTabsWidth { get; }` — 모든 탭을 담는 데 필요한 폭(배치용). 탭 폭 합.
- `int SelectedIndex { get; set; }` — 선택된 탭 번호(없으면 -1). 범위 밖이면 ArgumentOutOfRangeException.
- `int TabCount { get; }`
- `string GetTabText(int index)`
- `void SetTabs(params string[] tabs)` — 탭 글자 목록을 한 번에 정한다. 탭이 있으면 첫 탭이 선택된다(이벤트 발생).

## ApcTemplateView

`APC.UI.Controls` · class : UserControl

모든 기능 화면의 공통 틀: 상단 ApcHeaderBar, 하단 ApcAlarmBar, 가운데 Body.
- `protected ApcTemplateView(string title)`
- `protected readonly ApcAlarmBar Alarm`
- `protected readonly ApcHeaderBar Header`
- `protected readonly Panel Body` — 내용 영역(Padding 16px). 도킹 순서: 같은 Dock(Top 등)끼리는 나중에 Add한 것이 바깥(위)에 온다. Fill을 먼저 Add하고, Top 줄들은 화면 아래쪽 것부터 위쪽 것 순서로 Add한다(또는 SetChildIndex(fill, 0)).
- `protected static ApcButton Button(string text, ApcButtonVariant variant, int width = 110)`
- `protected static FlowLayoutPanel Row(params Control[] controls)` — 왼쪽→오른쪽으로 쌓는 한 줄(필터·버튼 줄).
- `protected static Label Caption(string text)`
- `protected static Label SectionTitle(string text)` — 구획 제목(굵게, 예: "최근 알람 이력"). 입력칸 앞 이름표는 Caption(보통 굵기).
- `protected static TableLayoutPanel Columns(int height, Control[] cells, float[] percents = null)` — 카드·타일·판정판을 한 줄에 나란히(Dock=Top). percents가 없으면 같은 폭. 칸 간격 12px(마지막 칸 0), 줄 아래 간격 12px. height는 칸 높이(배율 적용한 값, 예: S(150)).
- `protected static int S(int px)` — 배율 적용 px. ApcForm dialog 등 이 클래스 밖에서는 ApcTheme.Px.
- `protected void UseSettingsButton(EventHandler onClick)` — 이 화면이 떠 있는 창의 타이틀바(최소화 왼쪽)에 설정 버튼을 둔다. 설정 화면이 있는 화면만 생성자에서 한 번 부른다. 화면이 창에 붙을 때 켜고, 화면을 Dispose하면 끈다(갤러리처럼 화면을 바꿔 끼우는 창에서도 남지 않는다).

## ApcTextBox

`APC.UI.Controls` · class : TextBox

테마 입력칸. 오목한 테두리·포커스 테두리는 비클라이언트 영역에 직접 그리고, 글자는 높이 가운데에 온다. 높이를 자유롭게 줄 수 있다(기본 34px×배율, 최소 28px×배율). TextBox를 상속하므로 Text·ReadOnly·MaxLength 등은 그대로. Multiline이면 스크롤바 자리가 필요해 Windows 기본 한 줄 테두리로 돌아간다.
- `ApcTextBox()`
- `const int BaseHeight`
- `protected void ResetFieldColor()` — 바탕색을 상태(사용 가능·읽기 전용)에 맞춘다. 스캔 플래시 등으로 바꾼 색을 되돌릴 때도 쓴다.

## ApcTrendChart

`APC.UI.Controls` · class : Control

추이 꺾은선. SetValues로 시간순 값(왼쪽=과거), XLabels로 가로축 글자(양끝·가운데 등, 고르게 배치). Reference = 설정값(회색 점선), UpperLimit/LowerLimit = 상·하한(알람색 점선). 세로축은 값과 기준선을 모두 담게 자동으로 잡는다.
- `ApcTrendChart()`
- `double? LowerLimit { get; set; }`
- `double? Reference { get; set; }` — null·NaN·무한대 = 선 없음.
- `double? UpperLimit { get; set; }`
- `string[] XLabels { get; set; }`
- `void SetValues(IEnumerable<double> values)`

## ApcValueCard

`APC.UI.Controls` · class : Control

현재값 카드: 제목 띠(그리드 헤더 그라데이션, ShowLamp면 오른쪽에 램프) + 큰 값·단위 + 아래 한 줄(기준·범위·수신 시각 등). 값 색은 Status로: 경고/알람만 상태색, 정지는 흐리게. 알람이면 굵은 알람색 테두리. Value가 비면 값·단위 모두 빈칸. 값이 두 개인 카드는 ValueStatus/FooterStatus로 값·Footer 글자색을 그 항목 판정대로 따로 준다(램프·테두리는 Status).
- `ApcValueCard()`
- `HorizontalAlignment ValueAlign { get; set; }`
- `StatusKind Status { get; set; }`
- `StatusKind? FooterStatus { get; set; }` — Footer 글자색만 따로 정할 때(Footer에 적은 보조 값이 정상인데 카드는 알람). null이면 Status.
- `StatusKind? ValueStatus { get; set; }` — 큰 값 글자색만 따로 정할 때(값이 두 개인 카드: 카드는 습도 경고인데 온도 값은 정상). null이면 Status.
- `bool ShowLamp { get; set; }`
- `string Footer { get; set; }` — 아래 한 줄. 비우면 줄이 없다.
- `string Title { get; set; }`
- `string Unit { get; set; }`
- `string Value { get; set; }`
- `void SetFooterParts(string[] texts, StatusKind[] statuses)` — Footer에 보조 값이 여러 개일 때(습도 · 파티클) 항목마다 제 판정 색으로 그린다. " · "로 이어 붙이고, Footer는 이은 글이 된다. FooterStatus 하나로는 정상인 항목까지 알람색이 되므로 이걸 쓴다. Footer를 다시 대입하면 항목 색은 지워진다.

## ScanEventArgs

`APC.UI.Controls` · class : EventArgs

- `ScanEventArgs(string code)`
- `bool Accepted { get; set; }` — 앱이 코드를 거부하면 false로 둔다 → 실패 피드백(빨강 + 경고음). 기본 true.
- `string Code { get; }`

## Gradients

`APC.UI.Drawing` · static class

- `static void Fill(Graphics g, Rectangle r, Color top, Color bottom)`
- `static void Fill(Graphics g, Rectangle r, string[] pair)` — 세로 그라데이션. pair = [위, 아래] (#RRGGBB)

## NineSlice

`APC.UI.Drawing` · static class

모서리는 그대로, 변·가운데만 늘려 그린다. 둥근 모서리·광택이 크기와 무관하게 유지된다.
- `static void Draw(Graphics g, Image img, Rectangle dest, Padding slice, float imageScale)`

## ScanDetector

`APC.UI.Input` · class

바코드 스캐너 입력 판별. 스캐너는 키를 사람보다 훨씬 빠르게 연달아 보내고 Enter로 끝낸다. 모든 키 간격(Enter 포함)이 maxKeyIntervalMs 이하이고 길이가 minLength 이상이면 스캔.
- `ScanDetector(int maxKeyIntervalMs, int minLength)`
- `const char Terminator`
- `int Length { get; }` — 현재 쌓인 버스트 길이. Feed 직후 1이면 새 버스트가 시작된 것.
- `string Feed(char c, long timestampMs)` — 글자 하나를 넣는다. Enter에서 스캔으로 판정되면 코드, 아니면 null.
- `void Reset()`

## ApcLayout

`APC.UI.Theming` · static class

화면·dialog 공통 치수(배율 전 px, 쓸 때 ApcTheme.Px). ApcTemplateView·ApcForm dialog 헬퍼가 쓰고, APC.UI.Tools의 LayoutLint가 같은 값으로 점검한다(값을 바꾸면 LayoutLint 상수도 함께).
- `const int FieldHeight` — 입력칸·이름표 높이.
- `const int FooterButtonHeight`
- `const int FooterButtonWidth`
- `const int GroupGap` — 묶음(ApcGroup)·블록 사이, 내용과 하단 버튼 줄 사이.
- `const int ItemGap` — 한 줄 안 요소 사이(버튼·입력칸).
- `const int LabelGap` — 이름표 열과 입력칸 사이.
- `const int Pad` — dialog·화면 바깥 여백.
- `const int RowGap` — 입력 행 사이, 메인 화면 줄 사이.

## ApcTheme

`APC.UI.Theming` · static class

현재 테마 한 곳. 앱 시작 시 Load 한 번, 이후 컨트롤들이 여기서 색·폰트·이미지를 가져간다.
- `const int BaseMinHeight`
- `static Color C(string hex)`
- `static Color Status(StatusKind kind)`
- `static Font GetFont(FontStyle style = 0)`
- `static Image GetImage(string baseName)` — 캐시된 이미지. 호출 측에서 Dispose 하지 않는다.
- `static ThemeData Current { get; }`
- `static event EventHandler Changed`
- `static float Dpi { get; }`
- `static float ImageScale(Image img)` — 이미지가 몇 배율 파일인지(9-slice 여백 환산용).
- `static float Scale { get; }`
- `static int MinHeight { get; }`
- `static int Px(int px)` — 96dpi 기준 px을 현재 배율로. 화면·dialog 어디서나 크기·여백에 쓴다(ApcTemplateView의 S()와 같다).
- `static string AssetsDir { get; }`
- `static string ThemeDir { get; }`
- `static void Load(string themeDir)`

## AssetResolver

`APC.UI.Theming` · static class

이미지 이름 → 실제 파일 경로. 규칙: {baseName}@{1x|1.5x|2x}.png
- `const float BaseDpi`
- `const string FallbackScale`
- `static string PickScale(float dpi)` — 경계값(125%, 175%)은 큰 쪽을 택한다 — 축소가 확대보다 덜 흐리다.
- `static string Resolve(string assetsDir, string baseName, float dpi)`

## ScanSettings

`APC.UI.Theming` · class

- `ScanSettings()`
- `int MaxKeyIntervalMs { get; set; }`
- `int MinLength { get; set; }`

## StatusColors

`APC.UI.Theming` · class

- `StatusColors()`
- `string Alarm { get; set; }`
- `string Info { get; set; }`
- `string Run { get; set; }`
- `string Stop { get; set; }`
- `string Warn { get; set; }`

## StatusKind

`APC.UI.Theming` · enum

상태 의미. 색상(hue)은 고객사 테마에서도 바꿀 수 없다.
- Run, Warn, Alarm, Stop, Info

## ThemeColors

`APC.UI.Theming` · class

- `ThemeColors()`
- `string Border { get; set; }`
- `string GridLine { get; set; }`
- `string GridSelected { get; set; }`
- `string Text { get; set; }`
- `string TextInverse { get; set; }`
- `string TextMuted { get; set; }`
- `string[] Background { get; set; }` — 그라데이션 [시작, 끝]
- `string[] GridHeader { get; set; }`
- `string[] Header { get; set; }`
- `string[] TitleBar { get; set; }` — 창 타이틀바(로고가 올라가므로 밝은 톤)

## ThemeData

`APC.UI.Theming` · class

theme.json 매핑. JSON 키는 camelCase.
- `ScanSettings Scan { get; set; }`
- `StatusColors Status { get; set; }`
- `ThemeColors Colors { get; set; }`
- `ThemeData()`
- `int FontSize { get; set; }`
- `int[] ButtonSlice { get; set; }`
- `string FontFamily { get; set; }`
- `string Logo { get; set; }`
- `string Name { get; set; }`

## ThemeException

`APC.UI.Theming` · class : Exception

theme.json을 읽을 수 없거나 규칙 위반. 메시지에 파일 경로와 필드명을 담는다.
- `ThemeException(string message)`
- `ThemeException(string message, Exception inner)`

## ThemeLoader

`APC.UI.Theming` · static class

- `const string FileName`
- `static Color GetStatusColor(ThemeData t, StatusKind k)`
- `static Color ParseColor(string hex)`
- `static ThemeData Load(string themeDir)`
