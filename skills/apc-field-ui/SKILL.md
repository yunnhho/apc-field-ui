---
name: apc-field-ui
description: Builds and modifies APCSOFT field-PC program screens and dialogs in WinForms (.NET Framework 4.x) with the APC.UI library, then verifies them with an MSBuild build and render layout checks. Use when building or modifying an APCSOFT field PC program screen — 현장 PC 프로그램, MES·스마트팩토리 현장 화면, 설비 모니터링, 라인별/센서 모니터링, OEE·가동률, SPC, 수율·불량, LOT·바코드 스캔, 트래킹(Track-in/out), 체결, 작업 시작·실적·불량 입력, 공정 설비 프로그램(Failure Analysis 등), 새 화면·새 프로젝트, 기존 APC.UI 화면 수정·dialog 추가, APC.UI, ApcForm, ApcGrid. Not for WPF, web, or .NET 6/8 projects.
compatibility: Windows + Git Bash, .NET Framework 4.x WinForms, Visual Studio or Build Tools MSBuild, APC.UI repository cloned and built locally; Node.js only when new SVG assets are needed.
---

# APC field UI screens

> **Write `.cs` sources with the Write tool, one file each. Never use a Bash heredoc** — Korean text in a heredoc breaks quote parsing and leaves partial or missing files. If you already used a heredoc, rewrite every file with Write. Use Edit for partial changes.

Screens are built with the **APC.UI library**. Repository: `C:\apc-field-ui` (Git Bash `/c/apc-field-ui`; use the paths below as written).
Deliverable: **one main screen + dialogs if requested**, filled with sample data, plus a **data interface** for later wiring. Do not write real PLC/DB/serial code.
All on-screen text is Korean. Write your final report to the user in Korean.

Reference files (read only when the case applies):
- [references/setup.md](references/setup.md) — the work folder already has a csproj
- [references/dialog.md](references/dialog.md) — creating or changing a dialog
- [references/assets.md](references/assets.md) — a new SVG image is needed
- [references/troubleshooting.md](references/troubleshooting.md) — build, render or run fails

## 1. Inputs

| Required | Optional |
|---|---|
| **Purpose**: monitoring (whole line / sensors on equipment) or process equipment program (Failure Analysis etc.) | Capture of the old program |
| **Main functions** | Customer requirement file |
| **Detail dialogs or not** | Extra details |

Ask for missing required items once, all together. If all three are given, proceed. Fill out the function list from the requirement file if there is one. No customer name → use `default` and say so.

## 2. Setup

- Tools: if `/c/apc-field-ui/src/APC.UI.Tools/bin/Debug/APC.UI.Tools.exe` is missing, run `bash /c/apc-field-ui/build.sh` (expect `ALL PASSED`).
- No csproj in the work folder → `bash /c/apc-field-ui/tools/new-project.sh <folder> <ProjectName> <customer> [--framework v4.x]` (default v4.0). Relay any `알림:` line to the user.
- A csproj already exists (existing project, editing an APC.UI screen) → follow [references/setup.md](references/setup.md).
- C# 7.3 at most (no `record`, `init`, `string?`, switch expressions, target-typed `new()`).

## 3. API

Read only the needed sections of `/c/apc-field-ui/docs/api.md` (generated): see the `Contents:` line at the top, then find a section with `grep -n "^## ApcGrid" /c/apc-field-ui/docs/api.md`. **api.md is the source for usage (properties, methods, arguments)**; this file only decides what to use when. Do not read control sources. Read only the 1–2 templates chosen below from `/c/apc-field-ui/src/APC.UI.Demo/Templates/`.

## 4. Layout

Layout is constructor code only (no designer files). Scale sizes: `S(px)` in a screen (`ApcTemplateView`), `ApcTheme.Px(px)` elsewhere (dialogs). With a capture, keep the original information layout (what goes where) and change only the look.

### 4a. Main screen

| Purpose | Start from template |
|---|---|
| Monitoring: whole line (equipment tiles + value cards + alarm history, tile opens detail) | `EquipmentView` |
| Monitoring: sensor value cards and trends | `TempHumiView` |
| Process equipment: judge one unit (scan → OK/NG, measurements, today's totals, history) | `JudgeView` |
| Process equipment: analysis table for several units | `FailureAnalysisView` |
| Work input: start, output, defects | `WorkStartView`, `ProductionView`, `DefectView` |
| Lookup | `HistoryView` |

- **Copying a template**: templates are `internal sealed` demo classes and cannot be referenced. Copy into a new project file: change namespace and class name, move hard-coded sample values (arrays, repeated `Rows.Add`) into `SampleMainViewSource`, and let the screen read `IMainViewSource`. Do not copy `ApcTemplateView` helpers (`Columns`, `Row`, `Titled`, `S` …).
- No matching template → build a **new layout from existing Apc controls** on `ApcTemplateView`.
- Fill the empty `MainView` / `IMainViewSource` made by `new-project.sh`. Set `MainForm.TitleText` to the screen name and `MainForm.FunctionText` to the current function.
- **Docking**: add the Fill control to `Body` first, then Top rows from the bottom of the screen upward. A fixed row below the Fill cell (tables under a diagram): `Columns(...)` with `Dock = DockStyle.Bottom`, added after the Fill.
- **Constructor order**: create data and every control first, subscribe events (`ValueChanged`, `Resize`, `Scanned`, `SelectedIndexChanged`) last — `Clear()`, `Titled` layout and `Rows.Add` fire them during construction.
- One row of cards/tiles/judge panel: `Columns(S(height), cells[, percents])`. A row takes height + 12 — a Panel or `ApcGroup` holding several rows is `(S(height) + S(12)) × rows` (group: `group.HeightFor(...)`). Never guess heights.
- `SectionTitle()` / `Caption()` added directly to `Body` need `Dock = DockStyle.Top` (otherwise they overlap the row above). Inside `Row()`, leave them.
- **Section title + table/chart** = one `Titled(title, content[, buttons…])` cell placed in a `Columns()` cell or as the `Body` Fill. Title-row buttons go here too — never put `SectionTitle` and a button together in `Row()`. Two list tables side by side filling the remaining height: `FillColumns(...)`. Do not hand-build a TableLayoutPanel.
- **Fixed-height `Titled` cell**: `Columns(TitledHeight(ApcGrid.HeightForRows(n)), cells)` — `TitledHeight(content[, hasTools])` adds the title row and gap. Never measure the title row yourself.
- **Fixed-count table that must fill a Fill cell** (10 bolt results beside a diagram, 8 steps): `grid.FillRows = true` — rows share the height. Never compute row heights in `Resize`, never pad a fixed list with fake rows. Only for about 8+ rows: a short table (5 floors, Bin top 5) beside a Fill cell gets rows several times too tall — put it in a `Columns()` row above, or transpose it (see below).
- **Several tables**: fixed-count tables (checklist, 12 cart slots, Bin top 5) get a fixed height via `ApcGrid.HeightForRows(n)`; only one list table fills the remaining height. Do not put a fixed table beside the Fill list table (the shorter one leaves a gap) — put it in a `Columns()` row above. Other cells in that row match its height.
- **Fixed tables with different row counts in one row** (Hard Bin 8 beside Soft Bin 5): give every cell the taller table's `TitledHeight(ApcGrid.HeightForRows(max))` and leave the shorter table's empty rows — never fill the gap with a card or `ApcNoticeBar`. A table with few categories and short values can instead be transposed (categories as columns, 2–3 rows).
- A fixed-height table/chart stacked in `Body` outside `Columns()` is still wrapped: `Columns(S(height), new Control[] { grid })` (a bare Add overlaps or loses the bottom gap).
- **Selection**: when tapping a tile makes the cards/chart below show that unit, set `Selected = true` on that tile only. When a table row is chosen, `ClearInitialSelection = false` + select the row. A "selected row" button does nothing when `CurrentRow == null`.
- **No settings button inside the screen.** Only a program with a settings screen calls `UseSettingsButton(...)` (title-bar gear). Open order: `AdminPasswordDialog` → `SettingsDialog` (`FailureAnalysisView.OpenSettings`). Buttons opening non-settings dialogs go in the related section's `Titled(..., Button(...))`; a disabled button's reason goes before it: `Titled(title, grid, reason = Caption(""), button)` (left of the button, vertically centered).

### 4b. Dialogs

Read [references/dialog.md](references/dialog.md). Gist: inherit `ApcForm`, constructor starts with `SetupDialog`, lay out with `ApcForm` helpers only (never set margins/sizes by number), provide a parameterless constructor with sample values (for render).

### 4c. Controls

| Element | Control |
|---|---|
| Text input / barcode | `ApcTextBox` / `ApcScanBox` |
| Choice / number / date | `ApcComboBox` / `ApcNumericBox` / `ApcDateBox` |
| Button | `ApcButton` (Normal, Primary, Danger) |
| Confirm check (preheat checked, clamp fastened) | `ApcCheckBox` — `Text` is the full sentence; a blocked confirm follows `CheckedChanged` |
| Table / input table (type or scan into cells) | `ApcGrid` / `ApcGrid { ReadOnly = false, ClearInitialSelection = false }` + per-column `ReadOnly` |
| Read-only single value (current barcode, LOT, operator) | `ApcTextBox { ReadOnly = true }` or `ApcValueCard` |
| Large current value (sensor, count, defect rate) | `ApcValueCard` |
| Equipment state cell, tap for detail | `ApcStatusTile` (no subtitle — the reason lives in one place: judge panel or table) |
| Large OK/NG verdict | `ApcJudgePanel` (OK=Run, NG=Alarm, waiting=Stop). Monitoring screens also put the one "act now" item here |
| Persistent state (interlock bypassed, test mode, manual run) | `ApcNoticeBar` (not a judge panel). In `Body`: `Columns(bar.Height, new Control[] { bar })` |
| Name-value of the current target (process, unit, Lot ID, product, recipe) | `ApcInfoStrip`. Inside `Row()`: `Dock = None` + `Width` ≥ `PreferredWidth` (set values first) |
| Trend chart / status lamp | `ApcTrendChart` / `ApcLamp` |
| Settings group (instead of GroupBox) | `ApcGroup` |
| Tabs (unit group / line switch, instead of TabControl) | `ApcTabStrip` — never fake it with several `ApcButton`s. Top row of body `Row(tabs, ...)` (`FailureAnalysisView`) |

- **Scan fields** (`ApcScanBox`): only attached boxes raise `Scanned` — screen: `OnHandleCreated` + `FindForm()`; dialog: `Attach(this)` in the constructor. Several scan fields in one form (mold + badge): attach all and give each `Accepts` (`_badge.Accepts = c => c.StartsWith("EMP-")`) — a code goes to the box that accepts it whatever has focus or was scanned first, so never route by prefix yourself. A code no box accepts lands in the focused box (else the first attached) for you to reject. Move `Focus()` to the next empty box in `Scanned`. A box that must hold only a scanned value (badge, label): `ScanOnly = true` — no typing or editing, still looks like an input; never compare "scanned value = box text" yourself. Typed text + Enter is not a scan (`Scanned` does not fire); that is how "scan only, no typing" is enforced. A rejected code (`Accepted = false`) stays selected and the next scan replaces it — do not clear it.
- **Table status column**: `ApcLampColumn` (header "상태"). For short status words (가동·대기·전송됨) use `ApcLampValue` (lamp + word in one cell) and no separate text column; the column widens itself to the word (do not set `Width`). Otherwise the lamp must also be readable from a text column in the same row, headed "판정" or "내용".
- **Row actions** (send, reset): no drawn buttons — `ApcIconColumn`. In `CellClick`, ignore when `ApcIconColumn.IsEnabled(cell)` is false.
- **Out-of-spec cell**: `cell.Style.ForeColor = ApcTheme.StatusText(kind)` + bold.
- **Two-value card** (temperature + humidity): main value `Value`, second value `Footer`. `Status` = the worse one; per-item colors `ValueStatus` / `FooterStatus` (omit them and a normal value shows in warning color). Two or more footer values → `SetFooterParts`. Side-by-side cards share one height.
- Text values without a verdict (Lot ID, recipe) go in `ApcInfoStrip`, not `ApcValueCard` (the card bottom stays empty).
- **No standard controls**: no `Button`/`TextBox`/`CheckBox`/`RadioButton`/`ComboBox`/`NumericUpDown`/`DateTimePicker`/`DataGridView`/`TabControl`/`Form`, and no `Control` subclasses drawing their own cards/tiles/charts. Standard ones allowed: `Label` and layout `Panel`/`FlowLayoutPanel`/`TableLayoutPanel`. If an element is missing from the table, do not build it — tell the user.
- **Label assembly (allowed)**: a screen shape no control covers (gantt/timeline bars, cell map, step track) may be assembled from standard `Label`s placed inside a `Panel` — position/size computed, no `OnPaint`/`Graphics` drawing — a `Panel` subclass that only places Labels (`GanttBoard : Panel`) is fine. Fill colors `ApcTheme.Status(kind)`; text on a status fill uses the theme text color `ApcTheme.C(ApcTheme.Current.Colors.Text)`, while `ApcTheme.StatusText(kind)` is only for colored text on a white/panel background; theme font only; meaning carried by color gets an `ApcStatusLegend` or text. Not for things a control already does (buttons, inputs, tables, cards). Name the assembled part in the report's 확인 질문 (배치).
  - Placement: repeated items (bolts, cells, tanks, stations) get their bounds from `ApcLayout.Slots(area, cols, rows, fill, square)` in `OnResize` — evenly spaced, sized as a share of the area (`fill` 0.5~0.7). No fixed px item sizes, no `Math.Min(Px(64), …)` caps, no hand-picked `W * .46` positions. Items on the edge of an outline (bolts around a cover) = `Slots` over the outline's client area; connectors (pipes, arrows) are placed from the slot centers.
  - Layers: a fill inside a frame (tank level, progress) and items drawn on an outline (bolts on a part) are children of the frame/outline `Panel`, not siblings. Lines that cross bars, cells or boxes are textless `Label`s, `BringToFront()`/`SendToBack()` as needed — render allows those to overlap: thin lines (current time, ticks, alarm level) at most 6 px, pipes/connectors at most 24 px thick and at least 5× as long. Draw one continuous pipe under the valves/pumps; never split a bar or pipe to dodge a crossing.
  - Legend on the title row: `Titled("...", content, legend)` (no `Dock` on the legend).
  - Two judged values in one caption (tank "91 % · 55.0 ℃"): one Label per value, each colored by its own verdict — never one color for the whole line.
- **Confirm**: irreversible actions `ApcDialog.Confirm(this, "...")` (red); safe ones like save `ApcDialog.Confirm(this, "...", "저장", ApcButtonVariant.Primary)`.
- **Warnings use the warning box**: input errors and operation warnings (reversed period, empty LOT, wrong/duplicate scan, job already running …) use `ApcDialog.Warn(FindForm(), "...")` even on the main screen (`this` inside a dialog). During a scan event, show it via `BeginInvoke` after handling (`ProductionView.WarnAfterScan`). Notices with nothing wrong (connection result, done) use `ApcDialog.Alert`. Equipment faults and NG show through on-screen lamps/tiles/judge panel (no bottom alarm strip).
- State = `StatusKind` (Run/Warn/Alarm/Stop/Info). The theme owns the colors.

## 4-1. Detail standards (confirmed by the user; a requirement file overrides)

- **Window title**: screen name only (`MainForm.TitleText`, e.g. "도장 라인 모니터링"; same for the screen `base("...")`). No customer or version. The library draws the clock in the title row — no clock or header strip in the screen. Function name goes in `MainForm.FunctionText` (e.g. "작업 시작 · LOT 스캔"); dialogs have none. Never use the brand color in screen code (confused with alarm red).
- **Date/time**: date `yyyy-MM-dd`, same-day history `HH:mm:ss`, multi-day history `yyyy-MM-dd HH:mm:ss`.
- **Decimals** (same in cards, tables, alarm text and limit text)
  - `0.0`: ℃·%RH·kN·bar·kPa·m/min·A·V·Pa·µA, defect/availability/yield %, mΩ·nA, measured cycle time s
  - `0.00`: mm / `#,0`: counts, particles (개/ft³)
  - Integer: targets and settings a person enters (time settings s·min). Limits compared with a measurement use the measurement's decimals ("상한 28.0 s").
  - Units not listed (rpm, Hz, kg …): requirement file; if absent, ask the user.
- **Table alignment**: center by default (numbers too). Only long text columns (alarm text, remarks, reason) `MiddleLeft`.
- **Column width**: with a long text column, only that column `AutoSizeMode = Fill`. With only short value columns, every column except the lamp column is Fill.
- **No value (waiting, before communication)**: empty (`ApcValueCard.Value = ""`, cell `""`). Never `-`, `--`, `0`. A stopped unit that has a reading shows it. No value → no verdict: card `Status = Stop`, no fault reason text (process words "대기", "통신 끊김" are fine).
- **Measured-value input** (`ApcNumericBox` for weight, torque, length): starts empty — `Clear()` at start and after each accepted entry; block confirm until `HasValue` ("총중량 입력 필요"). Never use `Value <= 0` or a `ValueChanged` flag as "not entered". Setting fields keep a value (default).
- **Previous value while waiting**: if a judging screen in waiting state keeps the last inspection value on a card, its `Footer` names the source ("직전 검사 CELL0260418 · 10:15:00"). Never keep it without a source.
- **Limits** — upper/lower limits are allowed bounds.
  - A value equal to the upper limit is not alarm/NG — **above** it is (below the lower limit likewise). Lower-only specs (yield ≥ 95.0 %, Cpk ≥ 1.33): equal passes.
  - Warning starts at its start value inclusive (`>=`/`<=`). With a warning band, a value equal to the upper limit is a warning ("상한 근접 60.0 %RH (상한 60.0)"); without one, OK.
  - Text: limits "5.0 이하 OK", "기준 50.0 이하 · 경고 45.0 이상"; alarms "상한 초과 65.4 ℃ (상한 65.0)". Never "초과" or "도달 알람" for an equal value. Never show rule explanations like "같으면 OK".
  - One judge function per measured item, used by status text, lamp, card color, main tile/card, alarm history and detail dialog table alike.
- **Font**: theme font and size as is. No `new Font(...)` or size multipliers — only `ApcTheme.GetFont()` (bold `GetFont(FontStyle.Bold)`). Bold only for section/group titles and value emphasis — field labels (`Caption`, `AddFieldRow`) are regular.
- **Units**: input labels `하중 상한 (kN)`; display via `ApcValueCard.Unit` or column header `최대 하중(kN)`. Never put the unit inside a value cell. A column mixing items (최대값 of 온도·습도·풍속) → unit in the item cell of the same row (`습도 (%RH)`).
- **Spacing**: never by number. Screens use `Columns()`/`Row()`, dialogs the `ApcForm` helpers.

**Screen hierarchy** (render cannot check these — verify yourself)
- One fact, one place:
  - The most important verdict is one big `ApcJudgePanel` (one per screen; persistent states use `ApcNoticeBar`). If the requirement names "the most important information" (e.g. delivery deadline), put it in the panel's big text or `SubText` — not in a small card beside it.
  - Monitoring several units (tiles + selected-unit cards): the panel speaks for the **whole line** — the worst unit, naming it ("CF-3 알람 / ...") — and does not change with tile selection. The selected unit shows in its tile, cards and charts. Several units at the same worst level → count + the first ones ("금형 교체 필요 2건", SubText lists them).
  - The judge panel sits in the **top row of `Body`** (the first `Columns()` under the title bar), never below tiles or tables. Only the scan/input `Row(...)` may sit above it (`JudgeView`); either order is fine, nothing else goes above.
  - A cause planted in the sample data (unit, Bin, item: "T04 only has high SB23") must be readable on screen as text: one cause in the panel `SubText` and a table column. A cause that exists only in the data source does not count.
  - Do not repeat its reason sentence in tiles, card footers or tables (tiles/cards follow by color only). Do not restate the limit sentence on the card of a value the panel already speaks about.
  - Red border (`Alarm`) only on the faulty target itself. Daily totals (NG 7 EA) never get `Status = Alarm`; at most a red number via `ValueStatus = Alarm`.
- Status color on text = `ApcTheme.StatusText(kind)`. `ApcTheme.Status(kind)` is for fills/lines (yellow/green text is unreadable).
- Buttons: one `Primary` main action per screen, same height as inputs (`Button(...)`). `Danger` only for irreversible or safety-bypassing actions (normal flow is `Primary`). A disabled button gets `Enabled = false` + its reason right beside it as `Caption` (default color; `StatusText(Alarm)` only if the reason is an alarm; `""` when there is none).
- Text: judge `SubText` and card `Footer` have at most two parts (one " · "). No formulas or statistical constants (σ̂, d2). Guidance sentences only if they fit one line. No particle placeholders ("이(가)") — write "품번 불일치 · AB222 ≠ AB212".
- Tiles show the name only. If the requirement names tile states (정상/주의/이탈, 생산/교체 중) or a value, put the state word or value after the name in `Text` (two spaces): "DC-02  교체 예정", "T04  94.1 %" — state words exactly as the requirement lists them, never a reason sentence.
- Titles above tables/charts/input groups: `SectionTitle()`, `Titled()` or `ApcGroup` — never a bolded `Caption`.
- Side-by-side tables keep the default row height. Never paint row backgrounds with status colors — use lamp + bold `StatusText` in the relevant cell.
- No screen methods/helpers named like `Control` members (`Refresh`, `Text` → e.g. `Reload`). No `ShowLamp` on indicator cards without a verdict (counts, OEE).

**Status colors** (process state → `StatusKind`)
- Step tables (OCAP, approval flow) and every per-item sequence marker (step tiles, next cell to fill, next bolt, next row): done = `Run`, the current/next item = `Warn`, not started = `Stop`; the word column or cell text says which ("완료", "확인 중", "다음", "대기"). `Info` is never a "current" color. The judge panel speaks for the whole screen: normal progress ("3층 · 6번 칸") = `Run`.
- 대기·정지·통신 끊김 = `Stop` / running, in progress, normal finish (PASS·OK) = `Run` / near limit, needs check = `Warn` / NG·FAIL, equipment errors that stop work = `Alarm` / records like setting change or shift change = `Info`
- Tile and table row = the worse of process state and value verdict. Row text "공정 상태 · 사유" (e.g. "시험중 · 저항 한계 근접"); tiles use color only. Several reasons → the worst one (tie: first in screen order).
- Card `Status` = verdict of that card's value only:
  - Values without a spec (progress, counts): `Run` while updating, `Stop` when stopped — never paint a progress card red for an equipment error. A daily NG count > 0 keeps `Status = Run` and sets only `ValueStatus = Alarm` (red number, no red border).
  - Readings of a stopped/waiting unit are not judged — card/cell `Stop`, tile/row follow the process state (a cooled unit below its lower limit is not an alarm). Values of a normally finished unit are `Run`.

**Sample data**
- Row count: history/list tables fill their height. Use the number render reports ("표 아래가 빔 … N개로", "표 세로 스크롤바 … N개로"); fix overlap/overflow first and re-render. Fixed-count tables stay as is.
- Time order: fault → recovery rows of the same unit in correct order.
- Variety: no repeated text or intervals (mix units, texts, values, time gaps).
- States: mostly normal with meaningful faults. Monitoring: at least one each of Warn, Alarm, Stop (no Warn if the requirement has no warning stage). Judging: a few NG (no unfounded Warn).
- Limits: at least one row **equal** to a limit (upper 5.0 Pa → a 5.0 Pa OK row; with a warning band, a row at the warning start).

## 5. Data interface

The main screen depends only on `Data/IMainViewSource`; `Data/SampleMainViewSource` supplies samples. Adapt its methods and row models to the screen. Keep a **parameterless constructor** on the screen (uses samples) — render needs it. Dialogs receive values from the main screen and have no interface of their own. Sample values agree (alarm limit = settings table = chart reference line, last trend point = current value).

## 6. Done check

Copy this list into your response and tick each item. On failure, fix and redo from that step.

```
- [ ] verify.sh: build warnings 0, errors 0, every type 점검 0건
- [ ] PNG review: answer each with yes/no (all of 4-1 still applies)
  - [ ] Judge panel in the top row of the screen?
  - [ ] The cause (unit/Bin/item) readable as text on screen?
  - [ ] Every blocked action stays blocked — the user cannot unlock it by changing a combo/field on the screen or dialog?
  - [ ] No empty area below or beside a table/chart, no lopsided row?
  - [ ] No box bigger than its content — a one-number card wider than ~1/4 of the screen, tiles taller than their default `S(96)`, a `Fill` column whose longest text uses under half its width? Fix: narrow it with `Columns(..., percents)` and give the space to the panel/table, keep tiles at a fixed row height and let the Fill table take the rest, set short-text columns to fixed width.
  - [ ] Sample values agree (step 5)?
  - [ ] Every number's unit readable on screen — card unit, column header, or the row's item cell for a mixed column?
  - [ ] Every table lamp column readable as text — `ApcLampValue` word, or a "판정"/"내용" text column in the same row? Color-only tiles have their status said by the judge panel or a table row?
- [ ] Report: PNG paths, 점검 0건, build warnings 0
- [ ] 확인 질문: own report section with the 4 group headings (step 4), every item "A로 했습니다. B로 할까요?"; top ones asked if the user can answer
```

1. **Build + render + checks** in one command:
   `bash /c/apc-field-ui/tools/verify.sh <sln> bin/Debug/<App>.exe <customer> <PNG folder> MainForm <Dialog1> <Dialog2>…`
   - Type names are class names only. With a capture, append `--compare <capture.png>` (shown beside the first type).
   - Fix every `점검:` line and build warning, rerun until the `== 완료` line. `참고:` lines do not count but are judged by 4-1 "Sample data". Render owns the check list.
2. **Open the PNGs** and check what the tool cannot: all of 4-1 (hierarchy, status colors, sample data), step 5 consistency, status readable as text not just color, no big gaps or lopsided layout. Fix and go back to 1.
3. **Report** (in Korean): PNG paths, last `점검 0건`, build warnings 0. Keep limits you chose as constants next to the judge function.
4. **확인 질문** — after the first build, turn every part you decided yourself (not taken from the requirement) into a question for the user. Group them:
   - **정한 기준값**: limits, warning start, boundary inclusion, decimals the requirement did not give.
   - **해석**: wording you could read two ways — e.g. a percent or ratio with no stated base ("경계 10% 안쪽": of the band width or of the nominal value?), "highlight X" when every row matches, which actions count as blocked.
   - **배치**: where two rules conflicted or no example existed and you picked a layout (scan row above the judge panel).
   - **데이터 모양**: things you invented — barcode/label format, reason lists, approver IDs, which fields the data source supplies.

   - Every rule you found ambiguous or conflicting that changed the screen is also a 해석 or 배치 question here — not only a note on the skill. Reread your notes before writing the list.
   - Each question states what you built and one alternative, in the form "A로 했습니다. B로 할까요?", so the user can answer "yes" or pick B: "전처리·프라이머 관리 범위를 베이스·클리어와 같게(21.0~25.0 ℃) 했습니다. 부스별 범위를 따로 둘까요?" No statements without a question, no open questions without B.
   - Order by impact (blocking and verdict rules first). Count before writing: at most 8 — with 9 or more, merge minor ones (e.g. all invented ID formats into one) or move the least important into one closing line. Keep at least one 해석 or 배치 question if you made any such call.
   - In the report this is its own section "확인 질문" with the group names as sub-headings — separate from any "정한 기준값" list the request asks for.
   - If you can ask the user now (an ask-user tool is available and the request did not say questions are impossible), ask the top 4 with it — first option = what you built — and list the rest in the report. Otherwise list all of them in the report under "확인 질문".
   - Apply the answers (constants, layout, texts) and go back to step 1.

## Never

| Never | Instead |
|---|---|
| Change status hues (run=green, warn=yellow, alarm=red, stop=gray, info=blue) | Lightness only, in the customer `theme.json` |
| Edit repository `themes/default` or `assets-src/*.svg` | Project `themes/<customer>`, `assets-src/` |
| Guess a control API | Check `docs/api.md` |
| Leave the app running | Verify with render. If you ran it, close it before finishing (it locks `bin`) |
