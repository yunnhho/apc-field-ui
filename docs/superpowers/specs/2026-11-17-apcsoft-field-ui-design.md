# APCSOFT Field Program UI Baseline — Design

Written: 2026-11-17 · Status: approved (2026-11-17)

## 1. Intent

Build a **ready-to-use bundle of controls, styles and images** for the field programs (WinForms) APCSOFT builds anew for each customer.
The goal: whatever the customer asks for, "swap the theme file and image files and assemble the controls" yields screens of consistent APCSOFT quality.

The target environment is a single **work PC**. (Kiosk style was excluded from scope on 2026-11-17.)

| | Work PC |
|---|---|
| Input | Mouse, keyboard, barcode scanner |
| Usage | Seated, repetitive input, mostly grids and forms |
| Reference resolution | 1920×1080 (resizable window) |

## 2. Goals / Non-goals / Anti-goals

**Goals**
- WinForms control library `APC.UI.dll`: a new project only needs to reference it.
- Every image — buttons, lamps, window buttons, logo — is a **separate image file**, replaceable per customer.
- PC screen templates per function (job start, production entry, defect registration, equipment status, history lookup) in a demo app.

**Non-goals**
- No refactoring of programs already delivered.
- No WPF or web version.
- No integration with commercial controls (DevExpress etc.).
- No brand color system. APCSOFT red is used **only in the logo**.
- No kiosk/touch UI (large button size set, full-screen lock, number pad).

**Anti-goals** (counted as failure even if it works)
- Changing a customer theme requires modifying library code.
- The meaning of status colors (normal/warning/alarm/stop) differs per customer. This is a field safety issue.
- Barcode input leaks into the wrong input field.

## 3. Constraints

- **Target .NET Framework 4.0**; must run on 4.0–4.8. So no `async/await`, `System.Text.Json`, or per-monitor DPI APIs. C# language version up to 7.3.
- Zero external NuGet dependencies by default. JSON parsing uses `DataContractJsonSerializer` (built into net40).
- No mandatory test project. Instead, build the demo gallery app and check visually. Only logic parts (scan detection, theme loading, image resolution) get minimal self-checks.

## 4. Approaches considered

### 4.1 Implementation — **A chosen**
| | Description | Verdict |
|---|---|---|
| **A. Own custom control library** | Inherit standard controls and draw in OnPaint. Theme managed in one file. | **Chosen.** No license cost, the raised style can be expressed as far as needed, and customers are served by swapping theme and images only. |
| B. Standard controls + theme applier | Walk controls on form load and apply colors. | Least code, but limited raised look and press feedback. |
| C. Commercial suite skin | DevExpress/Telerik | License cost, and deliverables are locked to a vendor. |

### 4.2 Visual style — **B (continue the existing raised style) chosen**
| | Description | Verdict |
|---|---|---|
| Flat | Solid surfaces and thin borders | Not chosen |
| **Continue existing raised style** | Glossy lamps, bevel/gradient buttons, gradient headers | **Chosen.** Continuity with delivered programs came first. |
| Mixed | Flat screens with raised lamps only | Not chosen |

Comparison mockups: `.superpowers/brainstorm/735-1794878105/content/visual-style.html`.

**Consequence of the raised style**: gradients are hard to reuse by changing colors alone. So *buttons, lamps and window buttons are drawn as image files (9-slice), and text, surface and border colors are managed in the theme file.* Customer adaptation covers two things: swapping the image set and swapping theme values.

## 5. Design

### 5.1 Three layers
1. **Invariant principles** (fixed in `APC.UI` code; customers cannot change them)
   - Status color meaning: normal/running = green, warning = yellow, alarm = red, stopped/unused = gray, info = blue
   - Minimum control height: 28px (at 96dpi)
   - Barcode scan feedback: success = green flash + short beep, failure = red flash + long beep
   - Destructive actions (delete, end job) always go through a confirmation step.
2. **Theme** (`themes/<customer>/theme.json` + `assets/` in the same folder)
   - Colors: background gradient (start/end), header gradient, border, text (default/weak/inverse), grid (header/divider/selected row — one row background color, no alternating)
   - Font: family (default `맑은 고딕`), size
   - Logo file path
   - Status colors may adjust **lightness only**; hue is fixed. Validated on load.
3. **Patterns**: PC screen templates per function (§5.5)

### 5.2 Repository layout
```
/assets-src/            source SVGs (editable masters)
/themes/default/        theme.json + assets/*.png  (APCSOFT default theme)
/src/APC.UI/            control library (net40)
/src/APC.UI.Demo/       gallery app: control catalog + function templates
/tools/                 SVG → PNG batch conversion script
/Resources/             original delivered images (reference, do not modify)
/docs/                  design and guide documents
```

### 5.3 Image specification
- **Format**: PNG (32-bit, transparent). Sources are SVGs in `assets-src/`.
- **Name**: `{kind}_{name}_{state}@{scale}.png`
  - kind: `btn`, `lamp`, `win` (window button), `icon`, `logo`
  - state: `normal`, `hover`, `pressed`, `disabled`. Lamps use meaning instead of state: `run`, `warn`, `alarm`, `stop`, `info`
  - scale: `@1x`, `@1.5x`, `@2x`
  - e.g. `btn_primary_pressed@2x.png`, `lamp_alarm@1x.png`, `win_close_hover@1.5x.png`
- **9-slice**: resizable button images record corner margins in the theme file (`"slice": [8,8,8,8]`) so corners and gloss do not break when stretched.
- **Scale selection**: at startup decide the scale from `Graphics.DpiX` and pick the nearest image set; fall back to `@1x` if the file is missing. Declare system DPI awareness in the app manifest.
- **Migrating existing images** (keep the shape, redraw)
  | Existing | New name |
  |---|---|
  | `1.png` red | `lamp_alarm` |
  | `2.png` green | `lamp_run` |
  | `3.png` gray | `lamp_stop` |
  | `4.png` yellow | `lamp_warn` |
  | `btn_close(_over).jpg` | `win_close_{normal,hover,pressed,disabled}` — stronger hover difference |
  | `btn_minimize(_over).jpg` | `win_minimize_{…}` |
  | `APCSOFT.png` | `logo_apcsoft@{1x,1.5x,2x}` |
  - The info (blue) lamp `lamp_info` is newly drawn.

### 5.4 First control set
| Control | Base | Role |
|---|---|---|
| `ApcForm` | `Form` | Borderless window. Gradient title bar, logo, `win_*` image buttons. |
| `ApcButton` | `Button` | 9-slice image button. 4 states. Variants `Primary`/`Normal`/`Danger`. |
| `ApcLamp` | `Control` | Status lamp. Uses `lamp_*` images by `Status`; can blink on alarm. |
| `ApcScanBox` | `TextBox` | Barcode input. Receives scans regardless of focus via the form's `KeyPreview`. Short key intervals (threshold in the theme, default 50ms) mean a scan. Enter terminates and raises `Scanned`. |
| `ApcGrid` | `DataGridView` | Applies the theme style. Provides a status column (`ApcLampColumn`). |
| `ApcTheme` | static class | Theme load/validation, image resolution (scale selection), change event |

Controls not listed (checkbox, combo, etc.) are added when a real template needs them.

### 5.5 Screen templates (demo app)
| Function | PC screen |
|---|---|
| Job start | Work order grid and detail form |
| Production entry | Scan → add grid row → F9 save |
| Defect registration | Type combo, quantity, scan input |
| Equipment status | Equipment list grid with lamps |
| History lookup | Period filter, grid, slot for an Excel export button |

## 6. Left to the implementer
- SVG → PNG conversion tool (Inkscape CLI, resvg, etc.)
- Gradient color values and gloss details, based on the tone of the existing `Resources/` images
- Double buffering and flicker prevention inside controls
- How the demo app switches screens

## 7. Verification
- Build `APC.UI.Demo` and check all 4 states of every control on the gallery screen.
- Copy `themes/default` into `themes/sample` with changed colors and images. Confirm switching works with only the `--theme sample` argument, no code changes.
- `ApcScanBox`: confirm human typing and scanner input are distinguished, with a self-check (key interval simulation) and a real scanner.
- Confirm images do not break at 125%/150%/200% scale.
