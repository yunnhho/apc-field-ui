# Fixed Dialog Layout Rules Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** library helpers decide dialog sizes, fonts and alignment, and the render check catches violations.

**Architecture:** shared size constants `ApcLayout` → dialog helpers in an `ApcForm` partial (stacked in call order, laid out once in `OnLoad`) → rewrite the 5 templates → 5 rules in `LayoutLint` → SKILL.md.

**Tech Stack:** .NET Framework 4.0, C# 7.3, WinForms. Checks live in `src/APC.UI.Checks` (console, `Check(bool, name)`).

**Spec:** `docs/superpowers/specs/2026-11-18-dialog-layout-rules-design.md`

## Global Constraints
- C# 7.3, no NuGet additions. Done = `./build.sh` → `ALL PASSED`.
- Fixed values (unscaled px): margin 16, between input rows 12, between groups 16, caption↔field 12, between elements 8, footer buttons 110×40 with gap 8, field height `ApcTextBox.BaseHeight` (34).
- Font: theme font and size only. Bold only for `ApcGroup` titles and section titles (`SectionTitle`).
- Footer buttons [Cancel][Confirm], Confirm at the far right.
- Public API change → `src/APC.UI/Properties/AssemblyInfo.cs` 1.7.0.0.

## Review Focus
- A row with a very long caption (e.g. "압입 속도 상한 (mm/s)") → even with a wider caption column, every field has the same left edge. (Task 2 test)
- A dialog mixing rows inside and outside a group → same caption column width. (Task 2 test)
- Fill content with `SetupDialog` height 0 / two Fills → `InvalidOperationException` (never a silent 0 height). (Task 2 test)
- Scale 1.25 (high DPI) → helper results match `ApcTheme.Px` and the check tolerance is ±1px. (Task 4: compare everything via `ApcTheme.Px`, tolerance 1)
- A view-only dialog with a single button (close only) → `SetFooter(null, close)` works and is not flagged. (Tasks 2 and 3)

---

### Task 1: `ApcLayout` constants and unified main-screen spacing

**Files:**
- Create: `src/APC.UI/Theming/ApcLayout.cs`
- Modify: `src/APC.UI/Controls/ApcTemplateView.cs` (Body.Padding 16, `Caption`/`Button` Margin 8, `Columns` 12, `Row` 10→12)
- Modify: `src/APC.UI/APC.UI.csproj` (Compile item)
- Test: `src/APC.UI.Checks/DialogLayoutChecks.cs` (new) + `Run("dialog-layout", RunDialogLayoutChecks)` in `Program.cs` (before lint), csproj Compile item

**Interfaces:**
- Produces: `public static class ApcLayout { public const int Pad = 16, RowGap = 12, GroupGap = 16, LabelGap = 12, ItemGap = 8, FooterButtonWidth = 110, FooterButtonHeight = 40; public const int FieldHeight = ApcTextBox.BaseHeight; }` (namespace `APC.UI.Theming`)

- [ ] **Step 1:** Failing test — in `RunDialogLayoutChecks()`: `Check(ApcLayout.Pad == 16 && ApcLayout.RowGap == 12 && ApcLayout.GroupGap == 16 && ApcLayout.FooterButtonWidth == 110 && ApcLayout.FooterButtonHeight == 40, "layout: fixed values")`. Theme: `ApcTheme.Load(Path.Combine(Root, @"themes\default"))`.
- [ ] **Step 2:** `./build.sh` → confirm the compile error (`ApcLayout` missing).
- [ ] **Step 3:** Write `ApcLayout`; replace the numbers in `ApcTemplateView` with the constants (gap below `Row` 10→`RowGap`).
- [ ] **Step 4:** `./build.sh` → `ALL PASSED`. If main template lint breaks from the spacing change (overflow, scrollbar), fix that template's height.
- [ ] **Step 5:** Commit `refactor: shared sizes ApcLayout, main-screen row gap unified to 12`

### Task 2: `ApcForm` dialog helpers

**Files:**
- Modify: `src/APC.UI/Controls/ApcForm.cs` (add `partial`, call `LayoutDialog()` in `OnLoad`)
- Create: `src/APC.UI/Controls/ApcForm.Dialog.cs`
- Test: `src/APC.UI.Checks/DialogLayoutChecks.cs`

**Interfaces:**
- Consumes: `ApcLayout` (Task 1)
- Produces (all protected on `ApcForm`; size arguments are unscaled px):
  - `void SetupDialog(string title, int width, int height = 0)` — TitleText, Resizable=false, MinimizeBox=false, ShowInTaskbar=false, CenterParent, width. height 0 = auto from content.
  - `void AddFieldRow(string caption, Control field, params Control[] trailing)` — without trailing the field fills the remaining width; with trailing, field and trailing keep their own Width, left to right with `ItemGap`.
  - `ApcGroup AddGroup(string title)` / `void AddFieldRow(ApcGroup group, string caption, Control field, params Control[] trailing)`
  - `void AddContent(Control c)` — keeps its own Height (caller sets it with `ApcTheme.Px`), width = content width.
  - `void AddContentFill(Control c)` — remaining height. `InvalidOperationException` if `height == 0` or on a second call.
  - `void SetFooter(ApcButton cancel, ApcButton confirm, params ApcButton[] left)` — cancel may be null. `CancelButton = cancel ?? confirm` (view-only close also responds to Esc). Does not touch AcceptButton.
  - `static Label SectionTitle(string text)` — bold theme font, height `Px(28)`, BottomLeft alignment.
  - `static Label FieldCaption(string text)` — trailing caption inside a row: regular theme font, width = text width + `Px(4)`, MiddleLeft.
- Layout rules (`LayoutDialog`, once in OnLoad): start y = `ContentBounds.Top + Px(Pad)`, x = `ContentBounds.Left + Px(Pad)`, content width = `ContentBounds.Width - 2*Px(Pad)`. Caption column width = max `TextRenderer.MeasureText` of all row captions (including inside groups) + `Px(4)`; captions are MiddleLeft, regular weight, height `Px(FieldHeight)`. Gap between blocks: input row → input row `RowGap`, after `SectionTitle` `ItemGap`, otherwise `GroupGap`. Group height = group vertical Padding + sum of row heights + gaps. The footer row sits `GroupGap` below the last block; buttons `FooterButtonWidth×FooterButtonHeight`, confirm at the far right, cancel `ItemGap` to its left, left buttons from x to the right. With height 0, set `ClientSize.Height` to `Pad` + `Padding.Bottom` below the footer buttons. Use SetBounds, not docking (no Add-order trap).

- [ ] **Step 1:** Failing test — two `ApcForm`-derived test dialogs in Checks: (a) `SetupDialog("t", 460)` + 2 rows ("모델명", "압입 속도 상한 (mm/s)") + 1 row inside `AddGroup("묶음")` + `SetFooter(취소, 저장)`; (b) `SetupDialog("t", 600, 400)` + `AddContent(grid h=Px(100))` + `AddContentFill(chart)` + `SetFooter(null, 닫기)`. After an off-screen `Show()` + `DoEvents()`:
  - `"dialog: field left edges equal"` — the three fields have the same screen Left (including inside the group, `PointToScreen`).
  - `"dialog: row gap is RowGap"` — row1.Top - row0.Bottom == Px(12).
  - `"dialog: footer order and size"` — cancel.Right + Px(8) == save.Left, save.Right == ContentBounds.Right - Px(16), both buttons Px(110)×Px(40), `CancelButton == cancel`.
  - `"dialog: auto height"` — save.Bottom == ContentBounds.Bottom - Px(16), save.Top - last block Bottom == Px(16).
  - `"dialog: fill content"` — (b) chart.Bottom + Px(16) == close.Top, `CancelButton == close`.
  - `"dialog: fill without height throws"` / `"dialog: second fill throws"` — `InvalidOperationException`.
- [ ] **Step 2:** `./build.sh` → confirm the compile error.
- [ ] **Step 3:** Implement `ApcForm.Dialog.cs` (layout rules above).
- [ ] **Step 4:** `./build.sh` → `ALL PASSED`.
- [ ] **Step 5:** Commit `feat: ApcForm dialog helpers (SetupDialog, AddFieldRow, AddGroup, AddContent, SetFooter)`

### Task 3: Rewrite the 5 dialog templates with the helpers

**Files:**
- Modify: `src/APC.UI.Demo/Templates/{RecipeDialog,SettingsDialog,AdminPasswordDialog,LotInputDialog,EquipmentDetailDialog}.cs`

**Interfaces:**
- Consumes: all Task 2 helpers.

Keep the information layout and behavior (validation, Confirm, result properties, Enter/Esc); replace numbers, fonts and hand-built Panel/TableLayoutPanel/button rows with the helpers:
- `RecipeDialog`: `SetupDialog("레시피 편집", 460)`, 6 × `AddFieldRow`, `SetFooter(취소, 저장)`. Delete `AddRow`, `_y`, `ClientSize`.
- `SettingsDialog`: `SetupDialog("시스템 설정", 680)`, per block `AddGroup` + `AddFieldRow(group, "포트", combo, FieldCaption("신호"), combo, ..., 연결 시도)`. Trailing captions use `FieldCaption`. `SetFooter(취소, 저장, 비밀번호 변경)`.
- `AdminPasswordDialog`: `SetupDialog(title, 400)`, `AddFieldRow("비밀번호", _password)`, `SetFooter(취소, 확인)`, keep `AcceptButton = ok`.
- `LotInputDialog`: equipment line as `AddFieldRow("설비", value Label)` (regular weight, delete `FontSize*1.3f`), table via `AddContent` (height = header + row count), `SetFooter(취소, 확인)`.
- `EquipmentDetailDialog`: `SetupDialog(name + " 상세", 760, 560)`, `AddContent(head)`, `AddContent(grid)`, `AddContent(SectionTitle("최근 1시간 온도 추이 …"))`, `AddContentFill(chart)`, `SetFooter(null, 닫기)`.

- [ ] **Step 1:** Rewrite the five files.
- [ ] **Step 2:** `./build.sh` → `ALL PASSED` (existing lint, including 0 findings for the 5 templates).
- [ ] **Step 3:** Render the five dialogs and check the PNGs visually (caption column alignment, button size, empty space):
  `T=src/APC.UI.Tools/bin/Debug/APC.UI.Tools.exe; for d in RecipeDialog SettingsDialog AdminPasswordDialog LotInputDialog EquipmentDetailDialog; do $T render "$(cygpath -w src/APC.UI.Demo/bin/Debug/APC.UI.Demo.exe)" $d "$(cygpath -w themes/default)" "$(cygpath -w out/$d.png)"; done` → each reports `점검 0건`.
- [ ] **Step 4:** Commit `refactor: dialog templates use ApcForm helpers — unified sizes and fonts`

### Task 4: 5 render check rules

**Files:**
- Modify: `src/APC.UI.Tools/LayoutLint.cs` (there may be two copies of APC.UI loaded, so do not reference `ApcLayout`; keep the same values as private consts — same as the existing approach, with a comment to keep them equal to `ApcLayout`)
- Test: `src/APC.UI.Checks/LintChecks.cs` (add to `CheckLintCatches`)

**Interfaces:**
- Consumes: `ApcTheme.Current.FontFamily/FontSize`, `ApcTheme.Px`.
- Rules (the message keywords are what tests match):
  1. All screens: a `Label` or Apc control whose `Font.Name`/`Font.Size` differs from the theme → `"테마와 다른 글꼴"`.
  2. All screens: a bold Label with an input control (`FieldTypes` excluding `ApcButton`) to its right on the same row → `"이름표가 굵게"`.
  3. Within one parent, the Left of "the first field right of a caption" differs (tolerance 1) → `"입력칸 왼쪽 끝 어긋남"`. In dialogs (defined below), compare in screen coordinates even across parents.
  4. Within one parent, vertical gaps between those rows (3 or more) differ → `"행 간격 다름"`.
  5. Only in dialogs (root is `ApcForm` and its `Resizable` property (via reflection) is false), the bottom button row: button size ≠ `Px(110)×Px(40)` → `"하단 버튼 크기"`; empty space above the button row ≠ `Px(16)` or bottom margin ≠ `Px(16)` → `"하단 버튼 위 빈 공간"` / `"하단 여백"`.

- [ ] **Step 1:** Failing tests (existing `Lint(Action<Panel>)` + new `LintDialog(Action<ApcForm>)`: show a Resizable=false ApcForm off-screen):
  - `"lint: off-theme font"` — `new Label { Font = new Font("Arial", 14f) }` → `테마와 다른 글꼴`.
  - `"lint: bold field caption"` — bold Label (0,0,100,34) + ApcTextBox (110,0) → `이름표가 굵게`.
  - `"lint: misaligned field left edges"` — two rows, field Left 120/140 → `왼쪽 끝 어긋남`.
  - `"lint: uneven row gaps"` — three rows, gaps 12/20 → `행 간격 다름`.
  - `"lint: footer button size"` — dialog footer [취소 100×40][저장 120×40] → `하단 버튼 크기`.
  - `"lint: empty space above footer"` — 80px between the last row and the button row → `빈 공간`.
  - `"lint: helper dialog is clean"` — lint Task 2's test dialog (a) → 0 findings.
- [ ] **Step 2:** `./build.sh` → confirm the new items FAIL.
- [ ] **Step 3:** Implement the rules in `LayoutLint` (inside `Walk`/`CheckSiblings`; 3 and 5 once from the root in `Inspect`).
- [ ] **Step 4:** `./build.sh` → `ALL PASSED` (including 0 findings for the 13 templates; if a main template trips rule 1 or 2, fix the template).
- [ ] **Step 5:** Commit `feat: render checks — font, caption column, row gap, footer buttons`

### Task 5: SKILL.md, version, reinstall

**Files:**
- Modify: `skills/apc-field-ui/SKILL.md` (4b, 4-1, 7-3 check list), `src/APC.UI/Properties/AssemblyInfo.cs` (1.7.0.0), `docs/README.md` (if it mentions the helpers)

- [ ] **Step 1:** Change SKILL.md 4b to: "Dialogs are built only by inheriting `ApcForm` + the helpers (`SetupDialog` → `AddFieldRow`/`AddGroup`/`AddContent`/`AddContentFill` → `SetFooter`, top to bottom). Never hand-build `SetBounds`, `ClientSize` or a button row Panel." One line per helper signature; keep the template table. Delete the old sentence "`Columns()`, `Row()`, `S()` are unavailable in dialogs → do it by hand like `AddRow`".
- [ ] **Step 2:** Add rows to the 4-1 table: font (theme font and size only, bold only for group and section titles, never create `Font` directly), units (input in caption parentheses, display via `Unit` or column header), spacing (helpers and `ApcLayout` instead of raw numbers). Add the 5 new checks to the 7-3 check list.
- [ ] **Step 3:** AssemblyInfo 1.7.0.0, `./build.sh` → `ALL PASSED`, `bash tools/install-skill.sh`.
- [ ] **Step 4:** Commit `docs: dialog helper, font and unit rules in the skill (APC.UI 1.7.0.0)`
