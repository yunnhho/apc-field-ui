# Fixed Dialog Layout Rules (2026-11-18)

## Intent
Reduce the number of fixes needed on the `apc-field-ui` skill's first output. What the user fixes repeatedly: dialog text (size, weight, font), separation of rows and elements, misaligned layout and spacing.
Cause: dialogs had no shared sizes or helpers, so the 5 templates each hard-coded their own numbers (margins 14/16/20/24, rows 34/48/52, button height 34/40/44 · width 100–150 · gap 8/10/12, captions regular/bold, `FontSize*1.3f`). The agent copies one of them or invents numbers.

## Goals / Non-goals / Anti-goals
- Goal: the library decides dialog sizes, fonts and alignment, leaving the agent no room to choose. Violations are caught by the render check.
- Non-goals: redesigning main-screen layout (only unifying spacing values), new controls, changing theme colors or font size.
- Anti-goals: so many helpers that the agent picks the wrong one. Code that bypasses the helpers passing the check.

## Approach (user choice: B)
| Option | Description | Verdict |
|---|---|---|
| A | Add only a rule table to SKILL.md 4-1, align the templates | Written rules have been ignored so far |
| **B** | Shared size constants + `ApcForm` dialog helpers + rewritten templates + new render checks | Adopted. The agent does not choose numbers |
| C | Render checks only | Even when caught, the fix is guessed again |

## Fixed values (unscaled px)
| Item | Value |
|---|---|
| Dialog outer margin | 16 |
| Input field height | `ApcTextBox.BaseHeight` (34) |
| Between input rows | 12 |
| Between groups (`ApcGroup`) | 16 |
| Caption ↔ field | 12 |
| Footer buttons | 110×40, gap 8, 16 from content |
| Font | Theme font and size only. Bold only for group titles (drawn by `ApcGroup`) and screen section titles; input row captions are regular |
| Caption column | One per dialog. Width = longest caption, left aligned → every field starts at the same left edge |
| Units | Input: in the caption's parentheses `하중 상한 (kN)`; display: `ApcValueCard.Unit` or table column header |
| Main screen `Row()` spacing | 10 → 12 (same as `Columns()`) |

## Design
1. `APC.UI.Theming.ApcLayout` (public static): the values above as constants. `ApcTemplateView`, the `ApcForm` helpers and `LayoutLint` all use it.
2. `ApcForm` protected helpers (for dialogs; call order = top to bottom):
   - `SetupDialog(string title, int width)` — TitleText, Resizable=false, MinimizeBox=false, ShowInTaskbar=false, CenterParent, width (unscaled px).
   - `AddFieldRow(string caption, Control field)` — one caption + field row. Field width defaults to the remaining width, can be set.
   - `AddGroup(string title)` → target for adding rows inside it (same caption column inside groups).
   - `AddContent(Control c, int height)` — free element such as a table or chart. Omitting height fills the remaining height.
   - `SetFooter(ApcButton cancel, ApcButton confirm, params ApcButton[] left)` — [Cancel][Confirm] on the right, helpers on the left. Sets `CancelButton` automatically. View-only dialogs with confirm only pass cancel=null.
   - Height is computed from content right before showing (if `AddContent` fills the remaining height, the height argument of `SetupDialog` is used). Never fit `ClientSize` by hand.
   - The docking-order trap (later Add goes on top) is handled inside the helpers.
3. Rewrite the 5 templates (`RecipeDialog`, `SettingsDialog`, `AdminPasswordDialog`, `LotInputDialog`, `EquipmentDetailDialog`) with the helpers. Keep the information layout; fix only numbers and fonts.
4. Add render checks (`LayoutLint`) — for dialogs (`ApcForm`):
   - Font: a `Label`/Apc control with a font name or size different from the theme. Bold input row captions.
   - Caption column: the first field's left edge differs among input rows in the same parent.
   - Row spacing: vertical gaps between consecutive input rows in the same parent differ.
   - Footer buttons: buttons in the bottom row differ in size from each other or from the fixed value.
   - Outer margin: distance from content edges (left, right, bottom) to the form edge differs from the fixed margin (empty space from a hand-set ClientSize).
5. Add tests in `APC.UI.Checks` for the helpers (caption column alignment, button order, automatic height) and the new check items. Keep render findings at 0 for existing templates.
6. SKILL.md: change 4b to "dialogs are built only with the helpers + fixed value table", add font and unit rows to 4-1. Reinstall with `install-skill.sh`.
7. Public API added → `AssemblyInfo` 1.7.0.0.

## Builder's call
- Helper internals (panel structure, when height is computed), check tolerance (±1px), default field width rule.
- Rows with several caption + field pairs on one line, like `SettingsDialog`: an `AddFieldRow` overload or `AddContent` — the caption column check looks only at the first field of the row.

## Done when
`./build.sh` ALL PASSED, render of the 5 templates reports `점검 0건`, and a deliberately misaligned dialog sample is caught by the new checks.
