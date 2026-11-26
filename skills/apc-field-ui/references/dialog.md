# Dialogs (`ApcForm`)

Contents: basics · templates · helpers · rules

## Basics

- **A dialog is a separate form inheriting `ApcForm`** (`ApcDialog` is a static class for message boxes only and cannot be inherited).
- The constructor starts with `SetupDialog` per the helper table below (do not set form properties or size yourself).
- Open from the main screen with `dlg.ShowDialog(FindForm())`.
- `Columns()`, `Row()`, `S()` belong to `ApcTemplateView` and are unavailable in dialogs. Dialog sizes use `ApcTheme.Px`.

## Templates

All in `/c/apc-field-ui/src/APC.UI.Demo/Templates/` (also viewable in the gallery under "dialog 템플릿"). Copy them like main-screen templates (rename namespace/class, sample values via the constructor).

| Dialog type | Template |
|---|---|
| Read-only detail (status row + sensor table + trend + close) | `EquipmentDetailDialog` |
| Input form (limits, recipe; a few fields such as LOT number, model, operator) | `RecipeDialog` |
| Admin password check (before settings etc.) | `AdminPasswordDialog` |
| LOT input per slot; settings with the same items per row (limits per zone etc.) | `LotInputDialog` |
| Communication/equipment settings (one row per block in a group + column caption row + common settings + password change) | `SettingsDialog` |

Per type:
- **Input form** (`RecipeDialog`)
  - Split sections with `AddContent(SectionTitle("판정 기준"))`, then `AddFieldRow` below.
  - Lower/upper pair in one row: `AddFieldRow("하중 (kN)", 하한, FieldCaption("~"), 상한)`. Number fields have a fixed width.
  - A single number row shows its allowed range as dim trailing text.
  - Save flow: validate with `Warn` (cursor to the wrong field) → `Confirm("저장")` → result properties → OK.
- **Password check** (`AdminPasswordDialog`): if only this is requested without a settings screen, add a normal button named after the protected action ("기준값 변경"); pressing it opens this dialog, then runs the action on success.
- **LOT input** (`LotInputDialog`): equipment name row on top, input table below that takes scans directly. Row being entered is light blue, scan Enter → next cell, time of each entry, variable slot count, empty slot → `Warn`.

## Helpers

**Lay out dialogs with `ApcForm` helpers only.** Never set margins, row heights, button sizes or `ClientSize` by number — `ApcLayout` owns the sizes and lays out once just before showing. Call in the constructor, top to bottom. Signatures are in api.md (`ApcForm`).

| Helper | Does |
|---|---|
| `SetupDialog(title, width[, height])` | Title, fixed size, centered on parent. Width in unscaled px. No height = size to content |
| `AddFieldRow(caption, field[, trailing...])` | Label + field row (details below) |
| `AddGroup(title)` → `AddFieldRow(group, ...)` | Group of related rows (`ApcGroup`); group gap 16 > row gap 12 |
| `AddCaptionRow(group, "블록", "COM", "신호", ...)` | Column caption row inside a group (details below) |
| `AddContent(c)` / `AddContentFill(c)` | Table, status row, chart. `AddContent` keeps `c.Height` (`ApcTheme.Px`); `AddContentFill` takes the remaining height (needs a `SetupDialog` height, one per dialog). Table height `ApcGrid.HeightForRows(rows)` |
| `AddContentRow(c1, c2, ...)` | Cards in one row, same width and height. 2×2 = call twice |
| `SectionTitle(text)` | Section title; `AddContent(SectionTitle("..."))` to split input rows without a box |
| `SetFooter(cancel, confirm[, left...])` | Bottom-right **[확정][취소]** (details below) |
| `SetFooterNote(text)` | Reason text just left of the confirm button (returns the `Label`) |

- **`AddFieldRow`**
  - One label column for the whole dialog (widest label) → all fields start at the same x.
  - Groups using `AddCaptionRow` keep their own label column (a long block name does not push other groups).
  - No trailing → the field fills the width. With trailing (`FieldCaption("신호")`, fields, buttons…) each uses its own `Width`, left to right.
- **`AddCaptionRow`**: captions sit over the next `AddFieldRow(group, ...)` row's [label, field, trailing...] in order (`""` = none). When rows share columns (port, signal, logic per block), use this once instead of repeating `FieldCaption` per row.
- **`ApcInfoStrip` in a dialog**: at most 4 name-value pairs per strip; more → a second strip below (`AddContent` twice). Short names ("최종(%)").
- **`AddContentRow`**: never build card grids with `TableLayoutPanel` (default child `Margin` 3px triggers the render empty-space check).
- **`SetFooter`**
  - Cancel at the far right, buttons 110×40, helper buttons (`비밀번호 변경`) on the left. Esc = cancel.
  - Read-only: `SetFooter(null, 닫기)` + `DialogResult = OK` on close (so Esc also closes).
  - An input dialog with only a confirm button has no Esc — add a cancel button.
  - Set Enter (`AcceptButton`) only in dialogs without scanning.
  - Disabled confirm button → `Enabled = false` + its reason via `SetFooterNote("원인 확인 전 · Release 불가")` (`""` when enabled; alarm reasons `ForeColor = ApcTheme.StatusText(StatusKind.Alarm)`). Never put the reason in a field row's trailing caption.

## Rules

- Call helpers **only in the constructor** (layout happens once before showing; later calls throw).
- `AddFieldRow` makes the labels. Do not create `Label`, `Font` or `Panel` button rows yourself.
- Render reports "부모 밖으로 넘침" → widen the `SetupDialog` width.
- Render reports big empty space → omit the `SetupDialog` height (size to content) unless the dialog uses `AddContentFill`.
- **Prerequisites come from data, not from the dialog.** When a confirm action depends on earlier steps (cause confirmed, customer approved, re-test done), the dialog receives their state from the caller and shows it read-only (status row, lamp table, `ApcInfoStrip`). Never offer a combo/field that lets the user switch a prerequisite to "완료" and unlock the action. Only the step this dialog performs is input.
- Give the dialog a **parameterless constructor that fills sample values** — render needs it. If the dialog can block its confirm action, the sample starts in the **blocked state** (e.g. Release selected with prerequisites missing) so the PNG shows the disabled button and its reason.
