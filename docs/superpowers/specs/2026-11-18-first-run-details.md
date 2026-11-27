# First-Run Detail Standards (2026-11-18)

Goal: the `apc-field-ui` skill's first run produces a result with no further fix requests from the user.
Basis: simulation rounds 1–6 reports and the user's UI feedback (corners, alignment, button order, numeric fields, upper-limit line, first-row selection, combo width, row overflow, table scrollbar). Every visual problem was visible in the render PNG, but the agent let it pass.

## User decisions

| Item | Decision |
|---|---|
| Self-check after render | SKILL.md checklist + automatic checks in `APC.UI.Tools render` |
| Automatic check items | Clipped text, overflow/overlap, alignment on the same row, standard controls, table scrollbar, dialog button order |
| Date and time | Date `yyyy-MM-dd`, same-day history time `HH:mm:ss`, multi-day `yyyy-MM-dd HH:mm:ss` |
| Table column alignment | All centered. Only long text (alarm messages, remarks) left aligned |
| No value (waiting, before communication) | Empty |
| Window title | Screen name only |
| Decimal places | Default table per unit (below). The requirement document wins if it specifies |
| Font size | Keep theme `fontSize` 10 |
| Sample data | Tables fill the visible rows without a scrollbar; mostly normal, with at least one Warn, Alarm and Stop mixed in |

## Default decimal places

| Unit | Format |
|---|---|
| ℃, %RH, kN, bar, kPa, m/min, A, V | `0.0` |
| mm | `0.00` |
| Defect rate, utilization % | `0.0` |
| Quantity (EA) | `#,0` |

## Handled along the way (convenience)

- Make the `Columns()` helper protected static on `ApcTemplateView` (currently copied in `EquipmentView` and `JudgeView`).
- When render `--size` is omitted for a Form, use the form's own size.
- SKILL.md: template copy guidance (namespace, `internal`, constants → `IMainViewSource`), `mkdir -p` the work folder, check the build warning count.
