# Steel + Ink Redesign — Design

Date: 2026-11-24 · Status: approved

## 1. Intent and reason

The current APC.UI screens are not hard to use, but they look "made by a company without a designer" and monotonous. The default theme stacks only gray gradients (`#E6E6E6→#CFCFCF`, header `#5A6A7E→#2F3B4A`, border `#888888`) with no brand color and no text hierarchy.

The goal is **a refined reskin of the look only**. At a glance, these three must read together:
1. The maker (APCSOFT)
2. The program's purpose (e.g. "체결 공정 관리")
3. The function being viewed (e.g. "작업 시작 · LOT 스캔")

No flashy colors. The refined impression comes from restrained tones, solid depth and clear text hierarchy.

Users: workers, foremen and quality staff of varied age and skill. Environment: a regular PC, or a kiosk-style desktop with scanner, keyboard and mouse. A new product with no existing customers.

## 2. Goals · Non-goals · Failure conditions

**Goals**
- Reproduce in WinForms the impression of the browser mockup `visual-direction-v2.html` (B Steel + C type).
- At the top of every window, logo, program name and function name read as one line.
- The 14 demo templates and the gallery draw in the new look.

**Non-goals**
- Changing screen composition (what is where). The user said they are happy with the composition.
- New controls or new fonts.
- Dark mode, flat panels. Both were excluded before and stay excluded.

**Failure conditions (counted as failure even if it works)**
- Brand red spreads beyond the logo and function name (buttons, selected rows, tabs, backgrounds) and gets confused with alarm red.
- Alarm and warning indications stand out less than before.
- It still looks "all gray". The criteria: has the tone shifted to blue-gray, and is the weight difference between titles and labels visible?

## 3. Constraints

- .NET Framework 4.0 / C# 7.3, no NuGet additions.
- Do not change the hue of `theme.json` status colors (run/warn/alarm/stop/info).
- Keep the font 맑은 고딕 (Regular/Bold). Kiosks must not need an extra font installed.
- Do not edit `themes/*/assets/*.png` directly. Edit `assets-src/*.svg`, then regenerate with `node tools/render-assets.mjs`.
- Done when: `./build.sh` prints `ALL PASSED` and render reports 0 findings.
- The public API changes, so bump the `AssemblyInfo.cs` version. If the skill document is affected, update SKILL.md and reinstall.

## 4. Approaches considered

| Approach | Description | Verdict |
|---|---|---|
| **1. Repaint the existing library** | Change colors and SVGs in `themes/default` and fix the control drawing code. API mostly unchanged | **Chosen.** Fastest, and existing checks act as a safety net. Composition stays, so there is no reason to rebuild |
| 2. New library | Build 26 new controls alongside APC.UI | Several times the work for the same look as 1 |
| 3. Swap colors and art only | Change only `theme.json` and SVGs | Cannot produce the two-line title, label bars, or bold header rule, so it diverges from the mockup |

The user chose 1.

## 5. User decisions

- **Red only in identity areas (option A).** Only the logo and the function name on the title row. Never on controls.
- **Brand color is the logo's red family.** The logo image is used as is.
- **Visual direction: B (Steel) + C (Ink) type.**
  - From B (Steel): blue-gray metal panels, square corners (2–3px), inset input fields, navy primary buttons.
  - From C (Ink): bold titles, red function name, section labels with an ink-color bar, table header with a bold ink rule.
- Keep the light raised family, WinForms, and the status color rules.

## 6. Design baseline (values taken from the mockup)

### 6.1 Color tokens

| Use | Value | Existing key / new |
|---|---|---|
| Window background | `#E9EDF1 → #DDE3E9` | `background` |
| Title bar | `#FBFCFD → #E8EDF2` + 1px `#B9C3CD` below | `titleBar` |
| Panel and card background | `#FDFDFE → #F1F4F7`, 1px white highlight on top | **new `panel`** |
| General border | `#B9C3CD` | `border` |
| Input field border | `#9AA7B4` | **new `borderStrong`** |
| Text / ink emphasis | `#1A1F26` | `text` (also the label bar and table header rule) |
| Secondary text | `#5D6B7A` | `textMuted` |
| Table header | `#F7F9FB → #E4E9EE` + 2px `text` below | `gridHeader` |
| Table lines | `#E3E8ED` | `gridLine` |
| Brand | The actual red of the logo PNG (mockup `#D8232A`) | **new `brand`** |
| Primary button | `#3F5A78 → #2A4058`, border `#213347` | SVG `btn_primary_*` |
| Secondary button | `#FDFDFE → #DFE5EB`, border `#9AA7B4` | SVG `btn_normal_*` |

The 5 status colors stay as they are.

### 6.2 Window title row (`ApcForm`)

```
[APCSOFT logo] │ 체결 공정 관리            (existing right-side items: status, time) [_][x]
               │ 작업 시작 · LOT 스캔
```

- Logo → 1px vertical divider (`#C4CCD5`) → two-line title, all left aligned. Today the title is centered when the logo is on; change that.
- Top line: program name (Bold, about +6pt over the base size). Bottom line: function name (Bold, about −1pt, `brand` color).
- **New property `ApcForm.FunctionText`**. When empty, the title stays one line as today and the title bar does not grow.
- Dialogs (`ShowLogo=false`) keep the one-line left title as today, with no red.

### 6.3 Sections and cards

- `ApcGroup`: remove the gradient title band. Put a **3px ink bar + Bold label** at the top left inside the card background (`panel`).
- Card controls (users of `Cards.DrawFrame`: value cards, status tiles, etc.): `panel` gradient + 1px `border` + white top highlight instead of solid white. Keep the thick alarm border rule.

### 6.4 Input fields and buttons

- Input fields (`FieldFrame`): white background, `borderStrong` border; keep the inner top shadow (inset), slightly darker. Square corners.
- Keep the current blue focus color. If it looks too close to the navy primary button, the builder adjusts it.
- Redraw button SVGs in navy/secondary. Corner radius about 3px.
- The danger button (`btn_danger`) stays in the alarm red family. Danger is a status meaning, so it is an exception to the brand rule.

### 6.5 Tables (`ApcGrid`)

- Header: `gridHeader` gradient, Bold ink text, 2px ink rule below.
- Rows: 1px `gridLine`. Selected row color (`gridSelected`) only adjusts lightness to the blue-gray tone (stays blue).

### 6.6 Screen header band (`ApcHeaderBar`)

- Change the current dark navy band to a light metal tone (`titleBar` family). Ink Bold text, 1px `border` below.
- Lamp and status text stay where they are (composition unchanged).

## 7. Left to the builder (with criteria)

- Fine ± lightness adjustments of the values above. Criterion: same impression side by side with the mockup `visual-direction-v2.html`.
- Corner radius, shadow strength and padding px of each control.
- What to do when an older theme.json (`themes/sample`) lacks the new keys (`panel`, `borderStrong`, `brand`). Recommended: defaults derived from existing keys. Must not reject loading.
- Controls not mentioned above (`ApcTabStrip`, calendar, combo, trend chart, etc.) only match tone with the same tokens.
- Fill `FunctionText` in demo templates that have a function name.

## 8. Verification

1. `./build.sh` → `ALL PASSED`.
2. Capture the 14 templates and the gallery with `APC.UI.Tools render` and confirm 0 render findings. Put the captures next to the mockup for user review.
3. Brand color usage: grep that the `brand` key is referenced only in the `ApcForm` title row.
4. Compare captures to confirm alarm cards and alarm lamps do not stand out less on the new background.
5. Record the new public API (`FunctionText`, new ThemeColors properties) in `docs/api.md` and bump the version. Add the function name rule to SKILL.md's window title rule (line 128) and reinstall the skill.

## 9. Second revision (user feedback, 2026-11-24)

1. Button corner radius 3px → 2px (the same problem as the 1.5.0.0 feedback came back). Add an `AssetChecks` check that `btn_*.svg` radius ≤ 2.
2. The left bar of group boxes (`ApcGroup`) and section titles (`SectionTitle()`) is **brand red**. This widens section 5's "red only in the function name" to: function name + title bars. Still never on buttons, selection, or backgrounds.
3. Logo height 22 → 28px; window buttons 12px from the right edge.
4. Screen header band (`ApcHeaderBar`): do not draw title text (duplicates the window title row). Current time at the right end (`yyyy-MM-dd HH:mm:ss`, updated every second), status lamp + text to its left. Replaces 6.6's "title in ink Bold".

## 10. Third revision (user feedback, 2026-11-24, APC.UI 1.21.0.0)

- Remove all running-status display (lamp, text) from the header band. The band shows only the current time at the right end. `Header.Status`/`StatusText` only keep values for compatibility. Replaces "status to its left" in section 9 item 4. Status is shown by the body (tiles, cards, lamps, alarm band).
- Screens that need tabs put them at the left of the header band, not in the body: `ApcTemplateView.HeaderTools(params Control[])` (band 48px, tab bottoms on the band's bottom line, everything else vertically centered).
