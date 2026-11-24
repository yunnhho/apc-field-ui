---
name: apc-field-ui
description: Use when building or modifying an APCSOFT field PC program screen in WinForms (.NET Framework 4.x) — 현장 PC 프로그램, MES·스마트팩토리 현장 화면, 설비 모니터링, 라인별/센서 모니터링, OEE·가동률, SPC, 수율·불량, LOT·바코드 스캔, 트래킹(Track-in/out), 체결, 작업 시작·실적·불량 입력, 공정 설비 프로그램(Failure Analysis 등), 새 화면·새 프로젝트, 기존 APC.UI 화면 수정·dialog 추가, APC.UI, ApcForm, ApcGrid. Not for WPF, web, or .NET 6/8 projects.
---

# APC 현장 UI 화면 만들기

> **소스(`.cs`)는 Write 도구로 한 파일씩 쓴다. Bash heredoc 금지** — 한글 heredoc은 따옴표 파싱 오류로 일부만 또는 아무것도 안 만들어진다(시연에서 경고를 읽고도 다섯 차례 실패). 일부만 바꿀 때는 Edit.

APCSOFT 현장 프로그램 화면은 **APC.UI 라이브러리**로 만든다. 원본 저장소: `C:\apc-field-ui` (Git Bash 경로 `/c/apc-field-ui`. 아래 명령·경로는 이 경로를 그대로 쓴다. 다른 위치에 클론했으면 `tools/install-skill.sh`가 이 문서의 경로를 실제 위치로 바꿔 설치한다).
결과물: 샘플 데이터가 들어간 **메인 화면 하나 + (필요하면) dialog** 껍데기와, 데이터 연결용 **인터페이스 자리**. 실제 PLC/DB/시리얼 연결 코드는 만들지 않는다.

## 1. 입력 확인

| 필수 | 선택 |
|---|---|
| **목적**: 모니터링(라인 전체 / 설비 부착 센서 등) 또는 공정 설비 프로그램(Failure Analysis 등) | 예전 프로그램 캡처 |
| **메인 기능** | 고객사 요구서 파일 |
| **상세 dialog 유무** | 추가 상세 요청 |

필수 중 빠진 것만 한 번에 묻는다. 셋 다 있으면 바로 진행한다. 요구서가 있으면 메인 기능 목록을 거기서 보완한다. 고객사명이 없으면 `default`로 하고 그렇게 했다고 말한다. 고객사명이 있어도 저장소에 그 테마가 없으면 `new-project.sh`가 `알림: ... themes/default 를 복사했습니다`를 출력한다 — 이것도 사용자에게 전한다.

## 2. 프로젝트 판별과 준비

먼저 저장소 도구가 빌드돼 있어야 한다: `/c/apc-field-ui/src/APC.UI.Tools/bin/Debug/APC.UI.Tools.exe`가 없으면 `bash /c/apc-field-ui/build.sh`를 실행한다(`ALL PASSED` 확인).

| 작업 폴더 상태 | 할 일 |
|---|---|
| csproj 없음 | `bash /c/apc-field-ui/tools/new-project.sh <폴더> <프로젝트명> <고객사> [--framework v4.x]` (기본 v4.0, v4.0~v4.8.1. 폴더가 없으면 스크립트가 만든다) |
| 구형 csproj, `TargetFrameworkVersion` v4.x, WinForms | 같은 명령에 `--into-existing` (dll·테마·참조만 추가, 재실행 시 dll만 교체) |
| 이미 APC.UI 참조 있음(`lib/APC.UI.dll`, 기존 화면 수정·dialog 추가) | 스크립트를 바로 돌리지 않는다. dll 버전(`powershell -c "(Get-Item lib/APC.UI.dll).VersionInfo.FileVersion"`)을 `/c/apc-field-ui/src/APC.UI/Properties/AssemblyInfo.cs`의 `AssemblyFileVersion`과 비교한다. 같으면 그대로 진행. 다르면 `docs/api.md`(최신 기준)와 맞지 않는 API가 있을 수 있으니 사용자에게 알리고, 교체를 원하면 `--into-existing`으로 dll만 바꾼 뒤 빌드해 기존 화면이 깨지지 않는지 확인한다 |
| SDK 형식(`<Project Sdk=`), net6/8, WPF, 웹 | **중단.** APC.UI는 .NET Framework 4.x WinForms 전용이라고 알린다 |

`--into-existing`일 때 스크립트가 하지 않는 두 가지를 직접 한다:
- `Main`에서 첫 폼을 띄우기 전에 `ApcTheme.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"themes\<고객사>"))`를 호출하고 `ThemeException`이면 메시지를 보여 주고 끝낸다. 없으면 앱이 시작하자마자 죽는다(render는 테마를 직접 로드하므로 이 누락을 잡지 못한다).
- csproj가 `<Compile Include="파일.cs" />`를 하나씩 나열하면, 새로 만든 `.cs`마다 항목을 추가한다(`**\*.cs` 와일드카드면 불필요).

코드는 **그 프로젝트의** `LangVersion`/`TargetFrameworkVersion`을 따른다(v4.x면 C# 7.3까지: `record`, `init`, `string?`, switch 식, `new()` 금지).

맨 위 경고대로 `.cs`는 Write로 쓴다. heredoc을 이미 시도해 실패했으면 남은 파일을 믿지 말고 전부 Write로 다시 쓴다.

## 3. API 파악

`/c/apc-field-ui/docs/api.md`(자동 생성 API 요약)를 읽는다. 컨트롤 소스 전체를 읽지 않는다. 배치 참고용 템플릿은 아래에서 고른 1~2개만 `/c/apc-field-ui/src/APC.UI.Demo/Templates/`에서 읽는다.

## 4. 배치

배치는 생성자 코드로만 한다(디자이너 파일 없음). 크기는 배율 적용: 화면(`ApcTemplateView`)에서는 `S(px)`, dialog(`ApcForm`) 등 그 밖에서는 `ApcTheme.Px(px)`. 캡처가 있으면 원본의 정보 배치(무엇이 어디)는 유지하고 모양만 APC.UI로 바꾼다.

### 4a. 메인 화면

| 목적 | 출발 템플릿 |
|---|---|
| 모니터링: 라인 전체(설비 타일 + 현재값 카드 + 알람 이력, 타일 누르면 상세) | `EquipmentView` |
| 모니터링: 센서 값 카드·추이 | `TempHumiView` |
| 공정 설비 프로그램: 설비 한 대 판정(스캔 → OK/NG, 측정값, 금일 집계, 이력) | `JudgeView` |
| 공정 설비 프로그램: 여러 설비 분석표 | `FailureAnalysisView` |
| 작업 입력: 시작·실적·불량 | `WorkStartView`, `ProductionView`, `DefectView` |
| 조회 | `HistoryView` |

- **템플릿 가져오기**: 템플릿은 데모 프로젝트의 `internal sealed` 클래스라 참조할 수 없다. 읽고 프로젝트에 새 파일로 옮겨 쓴다 — 네임스페이스를 프로젝트 것으로, 클래스 이름을 화면에 맞게, 코드에 박힌 샘플 값(배열·`Rows.Add` 반복)은 `SampleMainViewSource`로 옮기고 화면은 `IMainViewSource`에서 받는다. `Columns()`·`Row()`·`Caption()`·`SectionTitle()`·`Button()`·`S()`는 `ApcTemplateView`에 있으니 옮기지 않는다.
- 맞는 템플릿이 없거나 추가 요청이 있으면 `ApcTemplateView`(헤더 + Body + 알람바) 위에 **기존 Apc 컨트롤로 새 배치**를 짠다.
- `new-project.sh`가 만드는 `MainView`·`IMainViewSource`는 빈 틀(TODO)이다. 여기에 채운다. `MainForm.TitleText`도 화면 이름으로 바꾼다(기본값은 영문 프로젝트명).
- **도킹 순서**: 같은 Dock끼리는 나중에 `Add`한 것이 바깥(위)에 온다. `Body`에는 Fill 컨트롤을 먼저 `Add`하고, Top 줄은 화면 아래쪽 것부터 위쪽 것 순서로 `Add`한다(또는 `Body.Controls.SetChildIndex(fill, 0)`).
- 카드·타일·판정판을 한 줄에: `ApcTemplateView`의 `Columns(S(높이), cells)`(같은 폭) 또는 `Columns(S(높이), cells, new[] { 50f, 25f, 25f })`(퍼센트 폭). 칸 간격·줄 간격은 헬퍼가 정한다. 한 줄은 **높이 + 12**를 차지한다(줄 아래 간격 포함) — `Columns()` 여러 줄을 Panel 하나에 담으면 그 Panel 높이는 `(S(높이) + S(12)) × 줄 수`.
- `SectionTitle()`·`Caption()`은 Dock이 없는 Label이다. `Body`에 바로 넣을 때는 `Dock = DockStyle.Top`을 준다(안 주면 위 줄과 겹친다. 시연 두 차례 연속). `Row()` 안에 넣을 때는 그대로 둔다.
- 타일을 누르면 아래 카드·그래프가 그 설비를 보여 주는 화면은 그 타일만 `Selected = true`(나머지 false). 표 행을 고르는 화면은 `ApcGrid { ClearInitialSelection = false }` + 그 행 선택. 선택 표시가 없으면 어느 설비 값인지 화면에서 모른다.
- `Columns()` 밖의 고정 높이 표·그래프를 `Body`에 Top으로 쌓을 때는 `Columns(S(높이), new Control[] { grid })`로 감싼다(Dock·줄 아래 간격을 헬퍼가 준다). 그냥 Add하면 Dock이 없어 겹치거나 아래 간격이 없다.
- `ApcGroup` 안에 `Columns()` 줄을 넣을 때 그룹 높이는 `group.HeightFor((S(높이) + S(12)) × 줄 수)`. 짐작한 숫자로 맞추지 않는다.
- **설정 버튼은 화면 안에 두지 않는다.** 설정 화면이 있는 프로그램만 화면 생성자에서 `UseSettingsButton((s, e) => OpenSettings())` → 창 타이틀바 최소화 왼쪽에 톱니 버튼이 생긴다(화면 Dispose 때 꺼짐). 설정이 없으면 부르지 않는다. 타이틀바는 설정 전용 — 설정이 아닌 dialog(레시피·LOT 입력 등)를 여는 버튼은 관련 구획 제목 줄 오른쪽에 일반 `ApcButton`(`Row(제목, 버튼)`). 열기 순서: `AdminPasswordDialog` → `SettingsDialog`(`FailureAnalysisView.OpenSettings`). 화면 없이 창만 있으면 `ApcForm.ShowSettings = true` + `SettingsClick`.
- `ApcGrid`는 사용자가 누르기 전엔 선택 행이 없다(`CurrentRow == null`). "선택한 행" 버튼은 null이면 아무것도 하지 않게 한다.

### 4b. dialog

- **dialog는 `ApcForm`을 상속한 별도 폼**이다(`ApcDialog`는 확인창만 띄우는 static 클래스라 상속할 수 없다). 생성자는 아래 헬퍼 표대로 `SetupDialog`부터 부른다(폼 속성·크기를 따로 설정하지 않는다). 메인에서 `dlg.ShowDialog(FindForm())`.
- 템플릿에서 출발한다(모두 `src/APC.UI.Demo/Templates/`, 갤러리 "dialog 템플릿"에서 열어 볼 수 있다). 메인 화면과 같은 방법으로 옮겨 쓴다:

| dialog 유형 | 템플릿 |
|---|---|
| 보기용 상세(상태 줄 + 센서 표 + 추이 + 닫기) | `EquipmentDetailDialog` |
| 입력 폼(구획 폼: `AddContent(SectionTitle("판정 기준"))`로 나누고 그 아래 `AddFieldRow`. 상·하한 짝은 한 행 `AddFieldRow("하중 (kN)", 하한, FieldCaption("~"), 상한)`, 숫자칸 고정 폭, 짝 없는 숫자 행은 trailing에 흐린 허용 범위. 검증 `Warn`(틀린 칸으로 커서) → `Confirm("저장")` → 결과 속성 → OK). LOT 번호·모델·작업자처럼 몇 칸 입력도 여기 | `RecipeDialog` |
| 관리자 비밀번호 확인(설정 진입 전 등). 설정 화면 없이 이것만 요청되면 지키려는 동작 이름의 일반 버튼("기준값 변경")을 두고, 누르면 이 dialog → 통과 시 그 동작 | `AdminPasswordDialog` |
| 칸·슬롯별 LOT 입력(위 설비 이름 줄, 아래 입력 표에 바로 스캔 — 입력 중인 행은 연한 파랑, 스캔 Enter → 다음 칸, 넣은 칸 시간, 칸 수 가변, 빈 칸 Warn). 행마다 같은 항목을 넣는 설정(구역별 상하한 등)도 이 입력 표 방식 | `LotInputDialog` |
| 통신·장비 설정(블록마다 한 행인 묶음 + 칸 제목 줄 `AddCaptionRow` + 공통 설정 + 비밀번호 변경) | `SettingsDialog` |

- **dialog 배치는 `ApcForm` 헬퍼로만 한다.** 숫자(여백·행 높이·버튼 크기·`ClientSize`)를 직접 정하지 않는다 — 치수는 `ApcLayout`이 정하고 표시 직전에 한 번에 배치된다. 생성자에서 위→아래 순서로 부른다:

| 헬퍼 | 하는 일 |
|---|---|
| `SetupDialog(title, width[, height])` | 제목·크기 고정·작업표시줄 숨김·부모 가운데. 폭은 배율 전 px. height 생략 = 내용으로 자동 |
| `AddFieldRow(caption, field[, trailing...])` | 이름표 + 입력칸 한 줄. 이름표 열은 dialog 전체에 하나(가장 긴 이름표 폭) → 입력칸 왼쪽 끝이 모두 같다. trailing이 없으면 입력칸이 남은 폭을 채우고, 있으면(`FieldCaption("신호")`, 입력칸, 버튼…) 각자 `Width`로 왼→오 |
| `AddGroup(title)` → `AddFieldRow(group, ...)` | 관련 행 묶음(`ApcGroup`). 묶음 사이 16 > 행 사이 12로 구분된다 |
| `AddCaptionRow(group, "블럭", "COM", "신호", ...)` | 묶음 안 칸 제목 줄. 바로 다음 `AddFieldRow(group, ...)` 행의 [이름표, 입력칸, trailing...] 위에 순서대로 놓인다(`""` = 제목 없음). 여러 행이 같은 칸을 가지면(블록별 포트·신호·로직) 행마다 `FieldCaption`을 되풀이하지 말고 이것으로 한 번만 |
| `AddContent(c)` / `AddContentFill(c)` | 표·상태 줄·그래프. `AddContent`는 `c.Height`(`ApcTheme.Px`) 그대로, `AddContentFill`은 남은 높이(`SetupDialog`에 height 필수, 하나만). 표 높이는 `ApcGrid.HeightForRows(행 수)` |
| `AddContentRow(c1, c2, ...)` | 카드 여러 개를 한 줄에 같은 폭·같은 높이(가장 큰 `c.Height`)로. 2×2는 두 번 부른다. 카드 격자를 `TableLayoutPanel`로 직접 짜지 않는다(자식 기본 `Margin` 3px 때문에 render "빈 공간" 점검이 나온다) |
| `SectionTitle(text)` | 구획 제목(굵게 + 아래 폭 전체 구분선). `AddContent(SectionTitle("..."))`. 상자(`AddGroup`) 없이 입력 행을 나눌 때 |
| `SetFooter(cancel, confirm[, left...])` | 오른쪽 아래 **[확정][취소]**(취소가 오른쪽 끝, 110×40), 왼쪽 보조 버튼(`비밀번호 변경` 등). Esc = 취소. 보기용은 `SetFooter(null, 닫기)` + 닫기에 `DialogResult = OK`(그래야 Esc도 닫기). 확정 버튼 하나뿐인 입력 dialog는 Esc가 없다 — 취소 버튼을 둔다. Enter(`AcceptButton`)는 스캔이 없는 dialog에서만 직접 지정 |

- 헬퍼는 **생성자에서만** 부른다(표시 직전 한 번 배치, 그 뒤 호출은 예외). 이름표는 `AddFieldRow`가 만든다. `Label`·`Font`·`Panel` 버튼 줄을 직접 만들지 않는다. 한 dialog가 폭에 안 들어가면(render "부모 밖으로 넘침") `SetupDialog` 폭을 늘린다.
- `Columns()`·`Row()`·`S()`는 `ApcTemplateView` 것이라 dialog에서는 쓸 수 없다(dialog 크기는 `ApcTheme.Px`).

### 4c. 컨트롤

| 화면 요소 | 컨트롤 |
|---|---|
| 글자 입력 / 바코드 | `ApcTextBox` / `ApcScanBox`: 화면의 `OnHandleCreated`에서 `var f = FindForm(); if (f != null) _scan.Attach(f);`(생성자엔 폼이 없다) |
| 선택 목록 / 숫자 / 날짜 | `ApcComboBox` / `ApcNumericBox`(Minimum·Maximum·DecimalPlaces·Increment, ▲/▼는 칸 안 오른쪽에 위아래로) / `ApcDateBox` |
| 버튼 | `ApcButton`(Normal·Primary·Danger) |
| 표 | `ApcGrid` + 상태 열 `ApcLampColumn`(셀 값 = `StatusKind`, 열 제목 "상태"). 상태 문구가 짧으면(가동·대기·전송됨) 셀 값 `new ApcLampValue(kind, "문구")`로 램프 + 문구를 한 칸에 — 이때는 따로 글 열을 두지 않는다. 행마다 누르는 동작(전송·초기화)은 버튼을 그리지 말고 `ApcIconColumn { IconName = "icon_play" | "icon_reset" }`(셀 값 true = 파란 아이콘, false = 회색 = 지금 못 함), `CellClick`에서 `ApcIconColumn.IsEnabled(cell)`이 false면 아무것도 하지 않는다. 램프 색이 무엇인지 같은 행의 글 열로도 읽혀야 한다 — 그 열 제목은 "판정"·"내용"("상태"는 램프 열과 겹친다). 기준 이탈 값 셀은 `cell.Style.ForeColor = ApcTheme.Status(kind)` + 굵게(선택 행에서도 색이 유지된다) |
| 읽기 전용 값 한 줄(현재 바코드·LOT·작업자) | `ApcTextBox { ReadOnly = true }`(회색 바탕) 또는 `ApcValueCard` |
| 큰 현재값(센서·수량·불량률) | `ApcValueCard`(Title·Value·Unit·Footer·Status). 값 글자 크기는 카드 높이를 따른다 — 나란한 카드는 같은 높이로(`Columns()`·`AddContentRow`가 맞춘다). 값이 두 개(온도 + 습도)면 주 값은 `Value`, 보조 값은 `Footer`("습도 57.0 %RH · 경고"). `Status`(램프·테두리)는 둘 중 나쁜 쪽, `ValueStatus`·`FooterStatus`는 각 항목의 판정 — 안 주면 정상인 온도가 노란 글자로 나온다. Footer에 보조 값이 둘 이상(습도 · 파티클)이면 `FooterStatus` 대신 `SetFooterParts(new[]{"습도 43.8 %RH","파티클 4,210 개/m³"}, new[]{StatusKind.Run, StatusKind.Alarm})` — 항목마다 제 색으로 그린다 |
| 설비 상태 한 칸, 누르면 상세 | `ApcStatusTile`(Text=설비명·Status·StatusText, `Click`) |
| OK/NG 판정 크게 | `ApcJudgePanel`(Text=큰 글자·SubText·Status: OK=Run, NG=Alarm, 대기=Stop) |
| 추이 그래프 | `ApcTrendChart`(`SetValues`, XLabels, Reference, UpperLimit/LowerLimit) |
| 상태 램프 | `ApcLamp` |
| 설정 묶음(제목 있는 상자, GroupBox 대신) | `ApcGroup`(Text=제목, 안은 Dock/FlowLayoutPanel) |
| 탭(설비군·라인 전환, TabControl 대신) | `ApcTabStrip`(`SetTabs(...)`, `Width = PreferredTabsWidth`, `SelectedIndexChanged`에서 내용 전환). 버튼을 탭처럼 쓰지 않는다(선택 탭이 실행 버튼과 같은 모양이 됨). 탭 아래끝을 표·내용 위끝에 붙인다 — `FailureAnalysisView` |
| 입력 표(칸에 바로 입력·스캔) | `ApcGrid { ReadOnly = false, ClearInitialSelection = false }` + 열별 `ReadOnly`(읽기 전용 열은 자동 회색, `SelectionMode = CellSelect`면 입력 중인 행의 읽기 전용 칸은 연한 파랑) |

- **표준 컨트롤 금지**: `Button`/`TextBox`/`ComboBox`/`NumericUpDown`/`DateTimePicker`/`DataGridView`/`TabControl`/`Form`과 `Control`을 상속해 직접 그린 카드·타일·그래프를 만들지 않는다. 위 표의 Apc 컨트롤을 쓴다. 표준으로 쓰는 것은 `Label`과 배치용 `Panel`/`FlowLayoutPanel`/`TableLayoutPanel`뿐. 표에 없는 요소가 꼭 필요하면 만들지 말고 사용자에게 알린다(라이브러리에 추가할 후보).
- 확인: 되돌릴 수 없는 동작은 `ApcDialog.Confirm(this, "...")`(빨간 확인), 저장처럼 위험하지 않은 것은 `ApcDialog.Confirm(this, "...", "저장", ApcButtonVariant.Primary)`. **경고 메시지는 하단 알람바가 아니라 경고창**: 입력 오류·조작 경고(기간 역순, 빈 LOT, 잘못된·중복 스캔, 이미 진행 중인 작업 등)는 메인 화면에서도 `ApcDialog.Warn(FindForm(), "...")`(제목 "경고" + 노란 램프, dialog 안에서는 `this`). 스캔 화면도 같다 — 스캔 이벤트 처리 중이면 `BeginInvoke`로 처리가 끝난 뒤 띄운다(`ProductionView.WarnAfterScan`). 잘못된 것이 없는 안내(연결 결과·완료)는 `ApcDialog.Alert`(제목 "알림" + 파란 램프). 알람바 `Alarm.Push("...")`는 설비 이상·NG 판정(빨강)만.
- 상태 표시는 `StatusKind`(Run/Warn/Alarm/Stop/Info) + `ApcLamp`/`ApcLampColumn`. 색은 테마가 정한다.

## 4-1. 디테일 기준 (사용자 확정, 요구서가 다르게 정하면 요구서)

| 항목 | 기준 |
|---|---|
| 창 제목 | 화면 이름만(`MainForm.TitleText`, 예: "도장 라인 모니터링"). 고객사·버전 붙이지 않음. 화면 헤더 제목(`base("...")`)도 같은 이름 |
| 날짜·시각 | 날짜 `yyyy-MM-dd`, 당일 이력 시각 `HH:mm:ss`, 여러 날에 걸친 이력 `yyyy-MM-dd HH:mm:ss` |
| 소수 자리 | ℃·%RH·kN·bar·kPa·m/min·A·V·Pa·µA → `0.0`, mm → `0.00`, 불량률·가동률·수율 % → `0.0`, mΩ·nA → `0.0`, 수량·파티클(개/ft³) → `#,0`(정수), 시간 설정(s·min) → 정수(측정한 사이클 타임 s는 `0.0`. 측정값과 비교하는 상·하한·경고값도 측정값 자리수 "상한 28.0 s" — 정수는 사람이 넣는 목표·설정값만). 카드 값·표·알람 문구·기준 문구 모두 같은 자리수. 여기 없는 단위(rpm·Hz·kg 등)는 요구서를 따르고, 요구서에도 없으면 사용자에게 묻는다 |
| 표 정렬 | `ApcGrid` 기본이 가운데(헤더 포함). 긴 글 열(알람 내용·비고·사유)만 `DefaultCellStyle.Alignment = MiddleLeft`. 숫자 열도 가운데 |
| 표 열 폭 | 긴 글 열이 있으면 그 열 하나만 `AutoSizeMode = Fill`. 짧은 값 열(ID·시각·숫자·판정)뿐이면 램프 열 빼고 모두 Fill로 고르게 나눈다(한 열만 Fill이면 나머지가 오른쪽 끝으로 밀린다. render가 "표 폭 절반 넘게 차지"로 잡는다) |
| 값 없음(대기·통신 전) | 빈칸(정지 설비라도 읽은 값이 있으면 그 값). `ApcValueCard.Value = ""`(값·단위 모두 안 그림), 표 셀은 `""`. `-`, `--`, `0` 쓰지 않음. 값이 없으면 판정하지 않는다 — 판정 함수에 넣지 말고 카드 `Status = Stop`, 값에서 나온 이상 사유 문구 없음(공정 상태 문구 "대기"·"통신 끊김"은 쓴다) |
| 대기 중 직전 값 | 판정 화면이 대기(READY)인데 측정값 카드에 직전 검사 값을 남기면 값을 남긴 카드마다 `Footer`에 출처를 적는다(예: "직전 검사 CELL0260418 · 10:15:00"). 출처 없이 남기지 않는다 |
| 공정 상태 → 상태색 | 아래 "상태색" 목록 |
| 헤더 요약 | 아래 "헤더 요약" 목록 |
| 상태 경계값 | 상·하한은 허용 한계다: 상한과 같은 값은 알람·NG가 아니다 — **초과**부터 NG·알람(하한은 미만부터). 경고 근접은 시작값과 같은 값부터 경고(`>=`/`<=`)라, 경고 구간이 있으면 상한과 같은 값은 경고("상한 근접 60.0 %RH (상한 60.0)"), 없으면 OK. 문구도 같게: 기준 "5.0 이하 OK"·"기준 50.0 이하 · 경고 45.0 이상"(규칙 설명 "같으면 OK"는 화면에 쓰지 않는다), 알람 "상한 초과 65.4 ℃ (상한 65.0)" — 상한과 같은 값에 "초과"·"도달 알람"을 쓰지 않는다. 상태 문구·램프·카드 색이 같은 판정 함수 하나를 쓴다. 같은 측정 항목(예: 사이클 타임)은 메인 타일·카드, 알람 이력, 상세 dialog 표에서 모두 그 함수 하나로 판정한다(이력에서 25.9 s가 경고인데 상세 표에서 26.3 s가 정상이면 틀린 것) |
| 글꼴 | 테마 글꼴·`fontSize` 그대로(12). `new Font(...)`·`FontSize * 배수` 금지, 글자는 `ApcTheme.GetFont()`만(굵게 `ApcTheme.GetFont(FontStyle.Bold)`). 굵게는 구획 제목(`SectionTitle`)·묶음 제목(`ApcGroup`)·값 강조에만 — 입력칸 앞 이름표(`Caption`, `AddFieldRow`)는 보통 굵기. 판정판·카드 큰 글자는 컨트롤이 정함 |
| 단위 표기 | 입력은 이름표 괄호 `하중 상한 (kN)`, 표시는 `ApcValueCard.Unit`·표 열 제목 `최대 하중(kN)`. 값 칸 안에 단위를 붙이지 않음 |
| 간격 | 숫자로 정하지 않는다. 화면은 `Columns()`·`Row()`, dialog는 4b 헬퍼가 정한다(값은 `ApcLayout`) |
| 샘플 데이터 | 아래 "샘플 데이터" 목록 |

**상태색** (공정 상태 → `StatusKind`)
- 대기·정지·통신 끊김 = `Stop`
- 가동·진행 중(시험 중·측정 중)·정상 완료(PASS·OK) = `Run`
- 한계 근접·확인 필요 = `Warn`
- 불량 판정(FAIL·NG)·설비 오류(인가 실패·통신 오류 등 작업이 멈추는 것) = `Alarm`
- 설정 변경·교대 같은 기록 = `Info`
- 설비 타일·표 행의 상태 = 공정 상태와 값 판정 중 나쁜 쪽. 문구는 "공정 상태 · 사유"(예: "시험중 · 저항 한계 근접"). 사유가 여럿이면 가장 나쁜 것 하나(같으면 화면 항목 순서 앞쪽).
- 카드 `Status`는 그 카드 값의 판정만. 판정 기준이 없는 값(진행률·수량, 기준이 주어지지 않은 측정값)은 값이 갱신 중이면 `Run`, 멈췄으면(설비 오류·대기·정지, 값 없음) `Stop` — 설비 오류를 진행률 카드에 빨강으로 칠하지 않는다(설비 상태는 타일이 보인다). 예외: 판정 화면의 NG 수 카드는 NG > 0이면 `Alarm`(`JudgeView`와 같이). 정지·대기 설비가 읽은 값은 판정하지 않는다 — 값만 보이고 카드·셀은 `Stop`(기본 글자색), 타일·행·헤더는 공정 상태(정지·대기)를 따른다(멈춘 설비가 식어 하한 아래인 것은 알람이 아니다. 빨간 셀이 보이는데 헤더 알람 수에 없으면 틀린 것). 정상 완료한 설비의 진행률·최종값은 `Run`.
- **알람바(`Alarm.Push`)는 늘 빨강**이라 거기 올리는 사건은 램프·헤더 요약도 `Alarm`이다(오류 램프는 노랑인데 알람바는 빨강이면 틀린 것).

**헤더 요약** (`Header` 오른쪽)
- 설비 한 대 화면(판정·시험기 한 대)은 집계 대신 그 설비 상태: "대기"(Stop) / "가동중"(Run) / "NG 발생"(Alarm, 가장 최근 판정이 NG일 때)(`JudgeView`와 같이).
- 헤더 램프(`Header.Status`)는 요약에 든 가장 나쁜 상태.
- 정상 외 상태를 모두 센다: "알람 1 · 경고 2 · 정지 1"(정지·통신 끊김·오류도 빼지 않는다, 0인 것은 생략). 모두 정상이면 "정상". 설비·구역 하나를 위 상태 규칙의 상태 하나로 센다. 대기와 정지는 둘 다 `Stop`이지만 화면 문구가 다르면 문구대로 따로 센다("대기 1 · 정지 1"). 요청이 정상 수까지 원하면 맨 앞에 "정상 4 · …". 순서는 (정상) → 알람 → 경고 → 정지 → 대기 → 통신 끊김.
- 수는 지금 화면에 보이는 범위(선택한 라인·페이지)의 것이고 범위 이름을 앞에 붙인다: "라인 A · 알람 1 · 경고 2". 전체 합계는 "전체 · …".

**샘플 데이터**
- 행 수: 이력·목록 표는 표 높이만큼 채운다. 짐작하지 말고 render가 알려 주는 수로 맞춘다(참고 줄 "표 아래가 빔 … N개로", 점검 줄 "표 세로 스크롤바 … N개로"). 겹침·넘침이 같이 나오면 그것부터 고치고 다시 render해서 나온 수를 쓴다(배치가 바뀌면 표 높이도 바뀐다). 설비 목록처럼 개수가 정해진 표는 그대로.
- 시간 순서: 같은 설비의 이상 → 복귀 행은 시각 순서가 맞아야 한다(복귀가 그 이상보다 늦게).
- 다양성: 늘린 행은 같은 문구·같은 간격을 되풀이하지 않는다(설비·문구·값·시각 간격을 섞는다. 시각만 바꾼 복사 행이 3개 이상이면 render가 "참고: … 되풀이"로 알린다).
- 상태: 정상(Run) 위주에 그 화면에서 뜻이 있는 이상 상태를 섞는다. 모니터링은 Warn·Alarm·Stop 각각 하나 이상, 판정은 NG(Alarm) 몇 건(판정 화면에 근거 없는 Warn을 만들지 않는다).
- 경계값: 판정 기준·상하한과 **같은 값**인 행을 하나 이상 넣어 경계값 판정이 화면에서 보이게 한다(예: 상한 5.0 Pa → 5.0 Pa OK 한 행. 경고 구간이 있어 상한이 경고 안에 들면 경고 시작 45.0 → 45.0 경고 한 행).
- 집계: 금일 생산·OK·NG·불량률은 서로 맞고 이력 행 수 이상.

## 5. 새 그림이 필요할 때만

1. 프로젝트의 `assets-src/`에 SVG를 만든다. 이름 규칙 `{btn|lamp|win|icon|logo}_{이름}_{normal|hover|pressed|disabled}.svg`(램프·로고는 상태 없음), `<svg width="..">` 필수. 기존 SVG(`/c/apc-field-ui/assets-src/`)를 본떠 그린다.
2. `/c/apc-field-ui/tools/node_modules`가 없으면 먼저 `cd /c/apc-field-ui/tools && npm install`(한 번만). 그다음
   `node /c/apc-field-ui/tools/render-assets.mjs --out <프로젝트>/themes/<고객사>/assets --override <프로젝트>/assets-src`
3. 기존 이름을 대체한 그림은 컨트롤이 자동으로 쓴다. 새 이름은 `ApcTheme.GetImage("icon_xxx_normal")`로 쓴다.

## 6. 데이터 자리

메인 화면은 `Data/IMainViewSource`(인터페이스)에만 의존하고, `Data/SampleMainViewSource`가 샘플을 준다. 화면에 맞게 인터페이스 메서드와 행 모델을 바꾼다. 화면에는 **매개변수 없는 생성자**(샘플 사용)를 남긴다 — render가 쓴다. dialog는 메인 화면에서 값을 넘겨받고 자체 인터페이스를 두지 않는다. dialog에도 **샘플 값을 넣는 매개변수 없는 생성자**를 둔다 — 없으면 render할 수 없다. 샘플 값은 서로 맞춘다(알람 문구의 기준값 = 설정값 표 = 그래프 기준선, 추이 마지막 점 = 현재값).

## 7. 완료 확인

1. MSBuild로 빌드하고 UTF-8 로그의 마지막 요약을 읽는다:
   ```
   . /c/apc-field-ui/tools/env.sh && "$MSBUILD" <sln> -v:m -nologo -noconlog "-flp:logfile=build.log;verbosity=minimal;encoding=utf-8;summary"; tail -8 build.log
   ```
   - `env.sh`가 설치된 Visual Studio/Build Tools에서 MSBuild를 찾는다. 못 찾으면 그 메시지를 사용자에게 전한다.
   - 콘솔 대신 로그 파일을 읽는 이유: 한글 VS의 MSBuild 콘솔 출력은 CP949와 UTF-8이 섞여 깨진다(`PreferredUILang`·`iconv`로는 안 된다).
   - `logfile`은 프로젝트 폴더 안 상대 경로(`build.log`)로. `../` 같은 상위 경로는 드라이브 루트로 풀려 정리(rm)가 막힌다.
   - 요약 `경고 0개`·`오류 0개`(영문 VS면 `0 Warning(s)`·`0 Error(s)`)를 확인한다. 경고도 고친다.
2. 메인 폼과 각 dialog를 PNG로(형식 이름은 네임스페이스 없이 클래스 이름만, 예: `MainForm`, `RecipeDialog`):
   `/c/apc-field-ui/src/APC.UI.Tools/bin/Debug/APC.UI.Tools.exe render <bin/Debug/앱.exe> MainForm <bin/Debug/themes/고객사> <out.png>` (Windows 경로는 `cygpath -w`)
   `--size`는 생략한다: 크기 고정 dialog(`Resizable = false`)는 자기 크기, 메인 폼·화면은 1920×1080으로 그린다. 출력 폴더는 없으면 만든다.
   예: `T=/c/apc-field-ui/src/APC.UI.Tools/bin/Debug/APC.UI.Tools.exe; $T render "$(cygpath -w bin/Debug/App.exe)" RecipeDialog "$(cygpath -w "bin/Debug/themes/고객사")" "$(cygpath -w out/RecipeDialog.png)"`
   캡처를 받았다면 `--compare <캡처.png>` → `<out>.compare.png`
3. **render 점검 0건까지 고친다.** render는 배치를 검사해 문제마다 `점검: <컨트롤 경로>: <문제>` 한 줄과 마지막 줄 `점검 N건`을 출력한다(검사 항목은 render가 정한다. 이 문서에 따로 나열하지 않는다). `점검:` 줄을 모두 고치고 다시 render해서 `점검 0건`을 확인한다. `참고:` 줄은 건수에 들지 않지만 4-1 "샘플 데이터" 기준대로 판단한다.
4. PNG를 직접 열어 도구가 못 보는 것을 눈으로 확인한다. 걸리면 고치고 다시 render:
   - 4-1의 모든 항목(표와 그 아래 상태색·헤더 요약·샘플 데이터 목록)이 화면에서 지켜졌는가
   - 6단계 샘플 값 정합
   - 상태가 색만이 아니라 글(상태 문구·판정 글자)로도 보이는가
   - 큰 빈 공간·한쪽으로 쏠린 배치가 없는가(dialog 빈 공간은 4b `SetupDialog` height 규칙을 다시 본다)
5. 사용자에게 PNG 경로와 함께 보여 주고, 마지막 render의 `점검 0건`·빌드 경고 0을 보고한다. 요구서에 없어 정한 기준값(상·하한, 경고 시작값, 단위 소수 자리)은 "정한 기준값" 목록으로 함께 알린다 — 사용자가 고칠 수 있게. 판정 함수 옆 상수 한 곳에 둔다.

x86 앱: 위 도구는 64비트로 돌아서 `PlatformTarget`이 x86인 앱이나 생성자에서 32비트 네이티브 DLL을 부르는 화면은 "어셈블리를 읽을 수 없습니다"로 실패한다. 그때는 같은 인자로 32비트 도구 `/c/apc-field-ui/src/APC.UI.Tools/bin/Debug-x86/APC.UI.Tools.exe`를 쓴다(`build.sh`가 함께 만든다). 32비트 도구로도 실패하면(네이티브 DLL·장비가 이 PC에 없음 등) 빌드 성공까지만 확인하고 render를 못 한 이유를 사용자에게 말한다.

## 하지 않는 것

| 하지 않음 | 대신 |
|---|---|
| 표준 입력·표·폼 컨트롤, `Control` 상속 직접 그리기 | 4c 표의 Apc 컨트롤 |
| 상태색 색상 변경(run=초록, warn=노랑, alarm=빨강, stop=회색, info=파랑) | 명도만 고객사 `theme.json`에서 |
| `/c/apc-field-ui/themes/default`, `/c/apc-field-ui/assets-src/*.svg` 수정 | 프로젝트의 `themes/<고객사>`, `assets-src/` |
| 컨트롤 API 추측 | `docs/api.md` 확인 |
| 앱을 실행해 띄워 두기 | 확인은 render로. 실행했다면 끝내기 전에 닫는다(떠 있는 exe가 `bin` 폴더를 잠가 다음 빌드·정리가 실패한다) |
