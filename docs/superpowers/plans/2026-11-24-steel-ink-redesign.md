# Steel + Ink 리디자인 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** APC.UI 기본 테마와 컨트롤 그리기를 시안 `visual-direction-v2.html`(B Steel + C 글자)로 다시 입힌다. API와 화면 구성은 거의 그대로 둔다.

**Architecture:** 새 색 토큰 3개(`panel`, `borderStrong`, `brand`)를 `ThemeColors`에 추가한다. 키가 없는 예전 theme.json은 기존 값에서 유도한 기본값으로 읽는다. 이어서 `ApcForm`(2단 제목), `ApcGroup`, `Cards`, `FieldFrame`, `ApcHeaderBar`, `ApcGrid`의 OnPaint를 고치고, 버튼 SVG를 다시 그린다.

**Tech Stack:** .NET Framework 4.0 / C# 7.3 WinForms, `APC.UI.Checks`(자체 Check 러너), `tools/render-assets.mjs`(resvg).

**Spec:** `docs/superpowers/specs/2026-11-24-steel-ink-redesign-design.md`

## Global Constraints

- C# 7.3, NuGet 추가 금지. 빌드와 점검은 `./build.sh`로 하고, 결과는 `ALL PASSED`여야 한다.
- 상태색 5개(`#16A34A`, `#EAB308`, `#DC2626`, `#9CA3AF`, `#2563EB`)는 바꾸지 않는다.
- 글꼴은 맑은 고딕(Regular/Bold)만 쓴다.
- `themes/*/assets/*.png`는 직접 편집하지 않는다. `assets-src/*.svg`를 고친 뒤 `node tools/render-assets.mjs`로 만든다.
- **`brand` 색(`#E60012`, 로고 PNG에서 측정)은 `ApcForm`의 기능명 한 곳에서만 참조한다.**
- 위험 버튼(`btn_danger_*`), 포커스 파랑(`FieldFrame.FocusColor`), 선택 행 파랑 계열은 유지한다.
- public API가 바뀌므로 `AssemblyVersion`을 `1.19.0.0` → `1.20.0.0`으로 올린다.

## 색 값 (spec 6.1 확정판)

| 키 | 값 |
|---|---|
| `background` | `["#E9EDF1","#DDE3E9"]` |
| `titleBar` | `["#FBFCFD","#E8EDF2"]` |
| `header` | `["#3F5A78","#2A4058"]` (탭 강조·InfoStrip 이름 칸용 남색. HeaderBar는 더 이상 쓰지 않음) |
| `gridHeader` | `["#F7F9FB","#E4E9EE"]` |
| `panel` *(신규)* | `["#FDFDFE","#F1F4F7"]` |
| `border` | `#B9C3CD` |
| `borderStrong` *(신규)* | `#9AA7B4` |
| `text` | `#1A1F26` |
| `textMuted` | `#5D6B7A` |
| `textInverse` | `#FFFFFF` |
| `gridLine` | `#E3E8ED` |
| `gridSelected` | `#CFE0F5` (유지) |
| `brand` *(신규)* | `#E60012` |

## Review Focus

1. **예전 theme.json**(`themes/sample`, 고객사 폴더)에 새 키가 없을 때: 로드가 성공하고 예전 모습(흰 카드, 기존 테두리, 브랜드 = 글자색)으로 보여야 한다. → Task 1 점검.
2. **`FunctionText`를 빈 문자열이나 null로 되돌릴 때**: 타이틀바 높이와 `Padding`이 한 줄 높이로 돌아와야 한다. 이게 안 되면 화면 내용이 위로 겹친다. → Task 2 점검.
3. **dialog(`ShowLogo=false`)에 `FunctionText`를 넣을 때**: 붉은 줄이 나오면 안 된다(spec 6.2). → Task 2 점검.
4. **알람 카드 테두리**: 새 패널 그라데이션 위에서도 3px 알람 테두리 픽셀이 알람색이어야 한다. 기존 점검 `card: alarm draws thick alarm border`가 계속 통과해야 한다. → Task 3.
5. **좁은 창(1024폭)에서 긴 프로그램 이름과 기능명**: 버튼과 겹치지 않고 말줄임(…)으로 처리되어야 한다. → Task 2에서 render로 확인.

---

### Task 1: 색 토큰 추가와 기본 테마 값 교체

**Files:**
- Modify: `src/APC.UI/Theming/ThemeData.cs` (`ThemeColors`)
- Modify: `src/APC.UI/Theming/ThemeLoader.cs` (`Validate` 끝에 기본값 채우기)
- Modify: `themes/default/theme.json` (위 표의 값)
- Test: `src/APC.UI.Checks/Program.cs` (`RunThemeChecks`)

**Interfaces:**
- Produces: `ThemeColors.Panel : string[]` (`"panel"`), `ThemeColors.BorderStrong : string` (`"borderStrong"`), `ThemeColors.Brand : string` (`"brand"`). Load 뒤에는 항상 null이 아니다.

- [ ] **Step 1: 실패하는 점검 작성** — `RunThemeChecks`에 추가:
```csharp
Check(t.Colors.Brand == "#E60012" && t.Colors.BorderStrong == "#9AA7B4" && t.Colors.Panel[0] == "#FDFDFE", "theme: steel-ink tokens");
var old = ThemeLoader.Load(Path.Combine(Root, Path.Combine("themes", "sample")));
Check(old.Colors.Panel[0] == "#FFFFFF" && old.Colors.Panel[1] == "#FFFFFF", "theme: missing panel defaults to white");
Check(old.Colors.BorderStrong == old.Colors.Border, "theme: missing borderStrong = border");
Check(old.Colors.Brand == old.Colors.Text, "theme: missing brand = text (no red)");
```
(`themes/sample/theme.json`에 새 키가 없는지 먼저 확인한다. 있으면 `fixtures/`에 새 키가 없는 사본을 만들어 쓴다.)
- [ ] **Step 2:** `./build.sh` → 위 점검 FAIL(컴파일 에러 포함).
- [ ] **Step 3: 구현** — `ThemeColors`에 `[DataMember(Name = "panel")]` 등 3개 속성을 추가한다. `ThemeLoader.Validate` 끝에 private `ApplyDefaults(ThemeColors c)`를 둔다. 값이 null이면 위 기본값을 넣고, 값이 있으면 `Pair`/`ColorField`로 형식을 검사한다. `themes/default/theme.json`을 위 표대로 고친다.
- [ ] **Step 4:** `./build.sh` → `ALL PASSED`. 이 시점에는 화면이 반쯤 바뀐 상태여도 된다.
- [ ] **Step 5:** 커밋 `feat: theme에 panel·borderStrong·brand 토큰, 기본 테마 Steel 색`

### Task 2: 창 제목 2단 (`ApcForm.FunctionText`)

**Files:**
- Modify: `src/APC.UI/Controls/ApcForm.cs` (`TitleBarHeight`, `OnPaint` 제목 부분, `Padding` 설정 지점 325행)
- Modify: `src/APC.UI.Demo/GalleryForm.cs`, `tools/project-template/MainForm.cs` (예시 값 넣기)
- Test: `src/APC.UI.Checks/FormChecks.cs`

**Interfaces:**
- Consumes: `ThemeColors.Brand` (Task 1)
- Produces: `public string FunctionText { get; set; }` (기본 `""`, null → `""`, set 시 높이·Padding 갱신 + Invalidate)

- [ ] **Step 1: 실패하는 점검** — `RunFormChecks`에 추가:
```csharp
using (var f = new ApcForm())
{
    int one = f.TitleBarHeight;
    f.FunctionText = "작업 시작 · LOT 스캔";
    Check(f.TitleBarHeight > one && f.Padding.Top == f.TitleBarHeight + 1, "form: function text grows title bar");
    f.FunctionText = null;
    Check(f.FunctionText == "" && f.TitleBarHeight == one && f.Padding.Top == one + 1, "form: clearing function text restores height");
    f.ShowLogo = false; f.FunctionText = "x";
    Check(f.TitleBarHeight == one, "form: dialog ignores function text");
}
```
- [ ] **Step 2:** `./build.sh` → FAIL.
- [ ] **Step 3: 구현**
  - 높이: `ShowLogo && FunctionText != ""`이면 기준값 54, 아니면 40(`BaseTitleHeight`). 높이가 바뀔 때마다 `Padding = new Padding(1, TitleBarHeight + 1, 1, 1)`을 다시 설정한다(기존 325행 로직을 메서드로 묶는다).
  - 로고 모드 그리기: 로고 → 12px 간격 → 1px 세로선(`#C4CCD5`, 높이 28) → 12px 간격 → 제목을 **왼쪽 정렬**한다(가운데 정렬 로직 삭제). 기능명이 있으면 2단으로 그린다. 윗줄은 `TitleFont()`(Bold ×1.5) + `text`, 아랫줄은 Bold, 테마 크기 −1pt + `brand`. 두 줄 모두 `TextClip.Draw` + `EndEllipsis`로 오른쪽 버튼 앞에서 자른다.
  - `ShowLogo=false`는 지금 그대로 둔다.
- [ ] **Step 4:** `./build.sh` → `ALL PASSED`.
- [ ] **Step 5:** `GalleryForm`에 `FunctionText = "컨트롤 갤러리"`를, `tools/project-template/MainForm.cs`에 주석과 함께 `FunctionText = ""`(화면 기능명)를 넣는다. 갤러리를 1024×768로 띄워 긴 제목이 말줄임 되는지 눈으로 확인한다(Review Focus 5).
- [ ] **Step 6:** 커밋 `feat: ApcForm.FunctionText — 로고 | 프로그램 이름 / 기능명 2단 제목`

### Task 3: 패널·라벨·입력칸·표 머리·헤더 띠 다시 그리기

**Files:**
- Modify: `src/APC.UI/Controls/ApcGroup.cs` (OnPaint)
- Modify: `src/APC.UI/Drawing/Cards.cs` (`DrawFrame`)
- Modify: `src/APC.UI/Drawing/FieldFrame.cs` (`DrawBorder`의 비포커스 테두리 색)
- Modify: `src/APC.UI/Controls/ApcGrid.cs` (`OnCellPainting` 머리줄, 묶음 머리 `OnPaint`)
- Modify: `src/APC.UI/Controls/ApcHeaderBar.cs` (OnPaint)
- Test: `src/APC.UI.Checks/DisplayChecks.cs`, `GridChecks.cs`

**Interfaces:**
- Consumes: `Panel`, `BorderStrong` (Task 1)

- [ ] **Step 1: 실패하는 점검** (픽셀 검사, 기존 `DrawToBitmap` 방식을 따른다):
  - `group: ink bar beside label` — `ApcGroup { Text = "LOT 바코드", Size = 300×120 }`을 그렸을 때, 라벨 왼쪽 막대 위치의 픽셀이 `text` 색이다.
  - `grid: header has 2px ink underline` — 열 1개짜리 `ApcGrid`의 머리 셀 아래 2px 픽셀이 `text` 색이다.
  - `card: panel gradient fill` — `ApcValueCard`(Status=Run) 안쪽 위(2px 아래) 픽셀이 `panel[0]`이다(흰색 `#FFFFFF`가 아님).
  - `header bar: light with ink title` — `ApcHeaderBar`의 (2, Height/2) 픽셀이 `titleBar` 범위에 있고 남색(`#2A4058`)이 아니다.
- [ ] **Step 2:** `./build.sh` → 4개 FAIL.
- [ ] **Step 3: 구현** (값은 spec 6.3~6.6을 따른다)
  - `ApcGroup`: `panel` 그라데이션, 1px `border`, 위쪽 1px 흰 하이라이트. 제목 띠 그라데이션을 없애고, 왼쪽 위에 3px `text` 막대 + Bold 라벨을 둔다. 제목 높이(`TitleHeight`)와 Padding은 바꾸지 않는다(구성 유지).
  - `Cards.DrawFrame`: `g.Clear(White)` 대신 `panel` 그라데이션을 칠한다(선택 상태는 기존대로 `gridSelected`). 알람과 하이라이트 테두리 로직은 그대로 둔다.
  - `FieldFrame`: 비포커스 테두리를 `borderStrong`으로 바꾸고, 안쪽 그림자 알파를 48 → 64로 올린다.
  - `ApcGrid` 머리: 기존 그라데이션과 세로 구분선은 유지하고, 아래선을 2px `text`로 바꾼다. 글자는 Bold `text`로 한다(이미 Bold면 색만 바꾼다).
  - `ApcHeaderBar`: 배경을 `titleBar` 그라데이션으로 바꾸고 아래에 1px `border`를 긋는다. 제목과 상태 문구는 `text` 색으로 한다. 램프 배치는 그대로 둔다.
- [ ] **Step 4:** `./build.sh` → `ALL PASSED`. 기존 `card: alarm draws thick alarm border`도 통과해야 한다(Review Focus 4).
- [ ] **Step 5:** 커밋 `feat: 패널·구역 라벨·입력칸·표 머리·헤더 띠를 Steel + Ink로`

### Task 4: 버튼·창 버튼 SVG와 탭·InfoStrip 톤

**Files:**
- Modify: `assets-src/btn_primary_{normal,hover,pressed,disabled}.svg`, `assets-src/btn_normal_*.svg`, `assets-src/win_*_*.svg`(색만)
- Regenerate: `themes/default/assets/*.png` (`node tools/render-assets.mjs`)
- Check: `src/APC.UI/Controls/ApcTabStrip.cs`, `ApcInfoStrip.cs` (이들은 `header`를 쓰므로 Task 1 값으로 자동으로 남색이 된다. 글자가 `textInverse`인지만 확인)

- [ ] **Step 1:** `btn_primary_normal`: 그라데이션 `#4A6687 → #3F5A78 → #2A4058`, 테두리 `#213347`, 모서리 `rx="3"`, 위쪽 흰 하이라이트 opacity 0.2. hover는 한 단계 밝게, pressed는 한 단계 어둡게 하고 하이라이트를 없앤다. disabled는 채도를 빼서 `#A9B4C0` 계열로 한다.
- [ ] **Step 2:** `btn_normal_*`: `#FDFDFE → #DFE5EB`, 테두리 `#9AA7B4`, `rx="3"`. 상태별 변화는 기존 파일의 명도 차이를 따른다.
- [ ] **Step 3:** `win_*`의 회색 계열을 `#5D6B7A`/`#B9C3CD` 톤으로 맞춘다(닫기 hover 빨강은 유지).
- [ ] **Step 4:** `node tools/render-assets.mjs` 실행 후 `./build.sh` → `ALL PASSED`(AssetChecks와 nine-slice 점검 포함).
- [ ] **Step 5:** 커밋 `feat: 버튼·창 버튼 SVG를 남색/금속 톤으로`

### Task 5: 마무리 — 문서·스킬·버전·시각 확인

**Files:**
- Modify: `src/APC.UI/Properties/AssemblyInfo.cs` (`1.20.0.0`, File 버전도 같이)
- Modify: `docs/api.md` (`ApcForm.FunctionText`, `ThemeColors` 새 키 3개와 기본값 규칙). `./build.sh`가 api 문서를 갱신하면 그 결과를 확인한다.
- Modify: `skills/apc-field-ui/SKILL.md` 128행 표: "창 제목 | 화면 이름만 (`TitleText`) … 기능명은 `FunctionText`(예: "작업 시작 · LOT 스캔"), dialog에는 쓰지 않음". 63행 TitleText 안내 옆에 FunctionText를 추가한다.
- Run: `bash tools/install-skill.sh`

- [ ] **Step 1:** 위 문서와 버전을 반영한다.
- [ ] **Step 2:** `./build.sh` → `ALL PASSED`, render 점검 0건(참고 줄은 무관).
- [ ] **Step 3:** brand 사용처를 확인한다: `grep -rn "\.Brand" src/APC.UI --include=*.cs` → `ApcForm.cs`와 `ThemeLoader.cs`(기본값)만 나와야 한다.
- [ ] **Step 4:** 갤러리와 템플릿 14개를 render 캡처한다. 알람 카드와 램프가 예전 캡처보다 덜 띄지 않는지 비교한다. 캡처를 사용자에게 보여 주고 시안과 같은 인상인지 확인받는다.
- [ ] **Step 5:** `bash tools/install-skill.sh` 실행 후 커밋 `docs: FunctionText·테마 토큰 문서, 스킬 갱신 (APC.UI 1.20.0.0)`
