# Steel + Ink Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** reskin the APC.UI default theme and control drawing to the mockup `visual-direction-v2.html` (B Steel + C type). API and screen composition stay mostly unchanged.

**Architecture:** add 3 new color tokens (`panel`, `borderStrong`, `brand`) to `ThemeColors`. An older theme.json without the keys loads with defaults derived from existing values. Then fix OnPaint of `ApcForm` (two-line title), `ApcGroup`, `Cards`, `FieldFrame`, `ApcHeaderBar`, `ApcGrid`, and redraw the button SVGs.

**Tech Stack:** .NET Framework 4.0 / C# 7.3 WinForms, `APC.UI.Checks` (own Check runner), `tools/render-assets.mjs` (resvg).

**Spec:** `docs/superpowers/specs/2026-11-24-steel-ink-redesign-design.md`

## Global Constraints

- C# 7.3, no NuGet additions. Build and checks via `./build.sh`; the result must be `ALL PASSED`.
- Do not change the 5 status colors (`#16A34A`, `#EAB308`, `#DC2626`, `#9CA3AF`, `#2563EB`).
- Font: 맑은 고딕 (Regular/Bold) only.
- Never edit `themes/*/assets/*.png` directly. Edit `assets-src/*.svg`, then generate with `node tools/render-assets.mjs`.
- **The `brand` color (`#E60012`, measured from the logo PNG) is referenced in exactly one place: the function name in `ApcForm`.**
- Keep the danger button (`btn_danger_*`), focus blue (`FieldFrame.FocusColor`) and the blue selected-row family.
- The public API changes, so bump `AssemblyVersion` from `1.19.0.0` → `1.20.0.0`.

## Color values (final version of spec 6.1)

| Key | Value |
|---|---|
| `background` | `["#E9EDF1","#DDE3E9"]` |
| `titleBar` | `["#FBFCFD","#E8EDF2"]` |
| `header` | `["#3F5A78","#2A4058"]` (navy for tab emphasis and InfoStrip name cells. HeaderBar no longer uses it) |
| `gridHeader` | `["#F7F9FB","#E4E9EE"]` |
| `panel` *(new)* | `["#FDFDFE","#F1F4F7"]` |
| `border` | `#B9C3CD` |
| `borderStrong` *(new)* | `#9AA7B4` |
| `text` | `#1A1F26` |
| `textMuted` | `#5D6B7A` |
| `textInverse` | `#FFFFFF` |
| `gridLine` | `#E3E8ED` |
| `gridSelected` | `#CFE0F5` (unchanged) |
| `brand` *(new)* | `#E60012` |

## Review Focus

1. **Older theme.json** (`themes/sample`, customer folders) without the new keys: loading succeeds and it looks as before (white cards, existing border, brand = text color). → Task 1 checks.
2. **Setting `FunctionText` back to empty or null**: title bar height and `Padding` return to the one-line height. Otherwise screen content overlaps upward. → Task 2 checks.
3. **`FunctionText` on a dialog (`ShowLogo=false`)**: no red line may appear (spec 6.2). → Task 2 checks.
4. **Alarm card border**: on the new panel gradient the 3px alarm border pixels must still be the alarm color. The existing check `card: alarm draws thick alarm border` must keep passing. → Task 3.
5. **Long program name and function name in a narrow window (1024 wide)**: no overlap with the buttons; ellipsized (…). → verified with render in Task 2.

---

### Task 1: Add color tokens and replace default theme values

**Files:**
- Modify: `src/APC.UI/Theming/ThemeData.cs` (`ThemeColors`)
- Modify: `src/APC.UI/Theming/ThemeLoader.cs` (fill defaults at the end of `Validate`)
- Modify: `themes/default/theme.json` (values from the table above)
- Test: `src/APC.UI.Checks/Program.cs` (`RunThemeChecks`)

**Interfaces:**
- Produces: `ThemeColors.Panel : string[]` (`"panel"`), `ThemeColors.BorderStrong : string` (`"borderStrong"`), `ThemeColors.Brand : string` (`"brand"`). Never null after Load.

- [ ] **Step 1: Write failing checks** — add to `RunThemeChecks`:
```csharp
Check(t.Colors.Brand == "#E60012" && t.Colors.BorderStrong == "#9AA7B4" && t.Colors.Panel[0] == "#FDFDFE", "theme: steel-ink tokens");
var old = ThemeLoader.Load(Path.Combine(Root, Path.Combine("themes", "sample")));
Check(old.Colors.Panel[0] == "#FFFFFF" && old.Colors.Panel[1] == "#FFFFFF", "theme: missing panel defaults to white");
Check(old.Colors.BorderStrong == old.Colors.Border, "theme: missing borderStrong = border");
Check(old.Colors.Brand == old.Colors.Text, "theme: missing brand = text (no red)");
```
(First confirm `themes/sample/theme.json` lacks the new keys. If it has them, make a copy without them under `fixtures/` and use that.)
- [ ] **Step 2:** `./build.sh` → the checks above FAIL (including compile errors).
- [ ] **Step 3: Implement** — add 3 properties such as `[DataMember(Name = "panel")]` to `ThemeColors`. Add a private `ApplyDefaults(ThemeColors c)` at the end of `ThemeLoader.Validate`: null → the defaults above; present → validate format with `Pair`/`ColorField`. Update `themes/default/theme.json` per the table.
- [ ] **Step 4:** `./build.sh` → `ALL PASSED`. The screens may look half-changed at this point.
- [ ] **Step 5:** Commit `feat: panel, borderStrong, brand theme tokens; Steel colors in the default theme`

### Task 2: Two-line window title (`ApcForm.FunctionText`)

**Files:**
- Modify: `src/APC.UI/Controls/ApcForm.cs` (`TitleBarHeight`, the title part of `OnPaint`, where `Padding` is set at line 325)
- Modify: `src/APC.UI.Demo/GalleryForm.cs`, `tools/project-template/MainForm.cs` (example values)
- Test: `src/APC.UI.Checks/FormChecks.cs`

**Interfaces:**
- Consumes: `ThemeColors.Brand` (Task 1)
- Produces: `public string FunctionText { get; set; }` (default `""`, null → `""`; setting it updates height and Padding + Invalidate)

- [ ] **Step 1: Failing checks** — add to `RunFormChecks`:
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
- [ ] **Step 3: Implement**
  - Height: base 54 when `ShowLogo && FunctionText != ""`, otherwise 40 (`BaseTitleHeight`). Whenever the height changes, reset `Padding = new Padding(1, TitleBarHeight + 1, 1, 1)` (wrap the existing line-325 logic in a method).
  - Logo mode drawing: logo → 12px gap → 1px vertical line (`#C4CCD5`, height 28) → 12px gap → title **left aligned** (delete the centering logic). With a function name, draw two lines: top `TitleFont()` (Bold ×1.5) + `text`, bottom Bold, theme size −1pt + `brand`. Both lines use `TextClip.Draw` + `EndEllipsis` to clip before the right-side buttons.
  - Leave `ShowLogo=false` as is.
- [ ] **Step 4:** `./build.sh` → `ALL PASSED`.
- [ ] **Step 5:** Put `FunctionText = "컨트롤 갤러리"` in `GalleryForm`, and `FunctionText = ""` (screen function name) with a comment in `tools/project-template/MainForm.cs`. Open the gallery at 1024×768 and visually confirm long titles ellipsize (Review Focus 5).
- [ ] **Step 6:** Commit `feat: ApcForm.FunctionText — logo | program name / function name two-line title`

### Task 3: Redraw panels, labels, fields, table header, header band

**Files:**
- Modify: `src/APC.UI/Controls/ApcGroup.cs` (OnPaint)
- Modify: `src/APC.UI/Drawing/Cards.cs` (`DrawFrame`)
- Modify: `src/APC.UI/Drawing/FieldFrame.cs` (unfocused border color in `DrawBorder`)
- Modify: `src/APC.UI/Controls/ApcGrid.cs` (header row in `OnCellPainting`, group header `OnPaint`)
- Modify: `src/APC.UI/Controls/ApcHeaderBar.cs` (OnPaint)
- Test: `src/APC.UI.Checks/DisplayChecks.cs`, `GridChecks.cs`

**Interfaces:**
- Consumes: `Panel`, `BorderStrong` (Task 1)

- [ ] **Step 1: Failing checks** (pixel tests, following the existing `DrawToBitmap` approach):
  - `group: ink bar beside label` — drawing `ApcGroup { Text = "LOT 바코드", Size = 300×120 }`, the pixel at the bar left of the label is the `text` color.
  - `grid: header has 2px ink underline` — for a one-column `ApcGrid`, the 2px below the header cell are the `text` color.
  - `card: panel gradient fill` — the pixel inside the top (2px down) of `ApcValueCard` (Status=Run) is `panel[0]` (not white `#FFFFFF`).
  - `header bar: light with ink title` — the pixel at (2, Height/2) of `ApcHeaderBar` is within the `titleBar` range and not navy (`#2A4058`).
- [ ] **Step 2:** `./build.sh` → 4 FAIL.
- [ ] **Step 3: Implement** (values per spec 6.3–6.6)
  - `ApcGroup`: `panel` gradient, 1px `border`, 1px white highlight on top. Remove the title band gradient; put a 3px `text` bar + Bold label at the top left. Do not change title height (`TitleHeight`) or Padding (composition stays).
  - `Cards.DrawFrame`: fill the `panel` gradient instead of `g.Clear(White)` (selected state stays `gridSelected`). Keep the alarm and highlight border logic.
  - `FieldFrame`: unfocused border becomes `borderStrong`; inner shadow alpha 48 → 64.
  - `ApcGrid` header: keep the existing gradient and vertical dividers; the bottom line becomes 2px `text`. Text Bold `text` (if already Bold, change only the color).
  - `ApcHeaderBar`: background becomes the `titleBar` gradient with a 1px `border` line below. Title and status text use `text`. Lamp placement unchanged.
- [ ] **Step 4:** `./build.sh` → `ALL PASSED`. The existing `card: alarm draws thick alarm border` must pass too (Review Focus 4).
- [ ] **Step 5:** Commit `feat: panels, section labels, fields, table header, header band in Steel + Ink`

### Task 4: Button and window button SVGs; tab and InfoStrip tone

**Files:**
- Modify: `assets-src/btn_primary_{normal,hover,pressed,disabled}.svg`, `assets-src/btn_normal_*.svg`, `assets-src/win_*_*.svg` (colors only)
- Regenerate: `themes/default/assets/*.png` (`node tools/render-assets.mjs`)
- Check: `src/APC.UI/Controls/ApcTabStrip.cs`, `ApcInfoStrip.cs` (they use `header`, so the Task 1 value makes them navy automatically. Only confirm their text is `textInverse`)

- [ ] **Step 1:** `btn_primary_normal`: gradient `#4A6687 → #3F5A78 → #2A4058`, border `#213347`, corners `rx="3"`, white top highlight opacity 0.2. hover one step lighter, pressed one step darker without the highlight. disabled desaturated toward `#A9B4C0`.
- [ ] **Step 2:** `btn_normal_*`: `#FDFDFE → #DFE5EB`, border `#9AA7B4`, `rx="3"`. Per-state changes follow the lightness differences of the existing files.
- [ ] **Step 3:** Shift the grays of `win_*` to the `#5D6B7A`/`#B9C3CD` tone (keep the red close-hover).
- [ ] **Step 4:** Run `node tools/render-assets.mjs`, then `./build.sh` → `ALL PASSED` (including AssetChecks and nine-slice checks).
- [ ] **Step 5:** Commit `feat: button and window button SVGs in navy/metal tone`

### Task 5: Wrap-up — docs, skill, version, visual check

**Files:**
- Modify: `src/APC.UI/Properties/AssemblyInfo.cs` (`1.20.0.0`, File version too)
- Modify: `docs/api.md` (`ApcForm.FunctionText`, the 3 new `ThemeColors` keys and their default rule). When `./build.sh` regenerates the api doc, check the result.
- Modify: the table at line 128 of `skills/apc-field-ui/SKILL.md`: "Window title | screen name only (`TitleText`) … function name in `FunctionText` (e.g. "작업 시작 · LOT 스캔"), not used in dialogs". Add FunctionText next to the TitleText guidance at line 63.
- Run: `bash tools/install-skill.sh`

- [ ] **Step 1:** Apply the docs and version above.
- [ ] **Step 2:** `./build.sh` → `ALL PASSED`, 0 render findings (`참고` lines do not count).
- [ ] **Step 3:** Check brand usage: `grep -rn "\.Brand" src/APC.UI --include=*.cs` → only `ApcForm.cs` and `ThemeLoader.cs` (defaults).
- [ ] **Step 4:** Render-capture the gallery and the 14 templates. Compare that alarm cards and lamps do not stand out less than in the old captures. Show the captures to the user and confirm the impression matches the mockup.
- [ ] **Step 5:** Run `bash tools/install-skill.sh`, then commit `docs: FunctionText and theme token docs, skill update (APC.UI 1.20.0.0)`
